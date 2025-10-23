#!/usr/bin/env pwsh
# Deploys all Kubernetes services using Kustomize

param(
    [Parameter(HelpMessage="Deployment strategy: statefulset (default) or deployment")]
    [ValidateSet("statefulset", "deployment")]
    [string]$Strategy = "statefulset"
)

$ErrorActionPreference = "Stop"

$namespace = "drawtogether"
$scriptDir = $PSScriptRoot

# Map strategy to overlay directory
$overlayName = switch($Strategy) {
    "statefulset" { "local" }
    "deployment" { "deployment" }
}

$overlayPath = Join-Path (Join-Path $scriptDir "overlays") $overlayName

Write-Host "Deploying K8s resources using Kustomize with strategy [$Strategy] into namespace [$namespace]" -ForegroundColor Cyan
Write-Host "Using overlay: $overlayPath" -ForegroundColor Yellow

# Extract version from Directory.Build.props
$buildPropsPath = Join-Path (Split-Path $scriptDir -Parent) "Directory.Build.props"
if (-not (Test-Path $buildPropsPath)) {
    Write-Error "Directory.Build.props not found at $buildPropsPath"
    exit 1
}

Write-Host "Reading version from Directory.Build.props..." -ForegroundColor Yellow
[xml]$buildProps = Get-Content $buildPropsPath
$version = ($buildProps.Project.PropertyGroup | Where-Object { $_.VersionPrefix } | Select-Object -First 1).VersionPrefix.Trim()
Write-Host "Found version: $version" -ForegroundColor Green

# Check if Docker images exist with current version, build if missing
Write-Host "Checking for Docker images..." -ForegroundColor Yellow
$imagesToCheck = @("drawtogether:$version", "drawtogether-migrationservice:$version")
$needBuild = $false

foreach ($image in $imagesToCheck) {
    docker image inspect $image 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Image $image not found" -ForegroundColor Yellow
        $needBuild = $true
    } else {
        Write-Host "Image $image exists" -ForegroundColor Green
    }
}

if ($needBuild) {
    Write-Host "Building Docker images for version $version..." -ForegroundColor Cyan
    $solutionPath = Split-Path $scriptDir -Parent
    Push-Location $solutionPath
    try {
        dotnet publish --os linux --arch x64 -c Release -t:PublishContainer
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to build Docker images"
            exit 1
        }
        Write-Host "Docker images built successfully" -ForegroundColor Green
    } finally {
        Pop-Location
    }
}

# Update the kustomization.yaml with the current version
$kustomizationPath = Join-Path $overlayPath "kustomization.yaml"
Write-Host "Updating $kustomizationPath with version $version..." -ForegroundColor Yellow

# Read existing kustomization and update only the image tags
$kustomizationContent = Get-Content $kustomizationPath -Raw

# Update image tags using regex to preserve the rest of the file
$kustomizationContent = $kustomizationContent -replace '(- name: drawtogether\s+newTag:\s+")[^"]+(")', "`${1}$version`${2}"
$kustomizationContent = $kustomizationContent -replace '(- name: drawtogether-migrationservice\s+newTag:\s+")[^"]+(")', "`${1}$version`${2}"

# If images section doesn't exist, add it
if ($kustomizationContent -notmatch 'images:') {
    $kustomizationContent += @"

images:
  - name: drawtogether
    newTag: "$version"
  - name: drawtogether-migrationservice
    newTag: "$version"
"@
}

Set-Content -Path $kustomizationPath -Value $kustomizationContent -NoNewline
Write-Host "Kustomization updated successfully" -ForegroundColor Green

# Create namespace if it doesn't exist
Write-Host "Creating namespace [$namespace] if it doesn't exist..." -ForegroundColor Yellow
try {
    kubectl create namespace $namespace 2>&1 | Out-Null
    Write-Host "Namespace [$namespace] created" -ForegroundColor Green
} catch {
    # Namespace already exists, that's fine
}
# Check if namespace exists
kubectl get namespace $namespace 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Namespace [$namespace] is ready" -ForegroundColor Green
} else {
    Write-Error "Failed to create or access namespace [$namespace]"
    exit 1
}

# Delete migrations job if it exists (Jobs are immutable, must recreate for version updates)
Write-Host "Checking for existing migrations job..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"  # Temporarily allow errors
kubectl get job drawtogether-migrations -n $namespace 2>$null
$jobExists = $LASTEXITCODE -eq 0
$ErrorActionPreference = "Stop"  # Restore strict error handling
if ($jobExists) {
    Write-Host "Deleting existing migrations job..." -ForegroundColor Yellow
    kubectl delete job drawtogether-migrations -n $namespace
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to delete migrations job"
        exit 1
    }
    Write-Host "Migrations job deleted" -ForegroundColor Green
} else {
    Write-Host "No existing migrations job found" -ForegroundColor Gray
}

# Apply Kustomize configuration
Write-Host "Applying Kustomize configuration from [$overlayPath]..." -ForegroundColor Yellow
kubectl apply -k $overlayPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to apply Kustomize configuration"
    exit 1
}

Write-Host "All services deployed successfully!" -ForegroundColor Green

# Wait for StatefulSet rollout to complete
Write-Host "`nWaiting for StatefulSet rollout to complete..." -ForegroundColor Yellow
kubectl rollout status statefulset/drawtogether -n $namespace --timeout=5m

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nRollout completed successfully!" -ForegroundColor Green
    Write-Host "`nCurrent resource status:" -ForegroundColor Cyan
    kubectl get all -n $namespace
} else {
    Write-Error "Rollout did not complete successfully within timeout"
    Write-Host "`nCurrent resource status:" -ForegroundColor Yellow
    kubectl get all -n $namespace
    exit 1
}
