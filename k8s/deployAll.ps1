#!/usr/bin/env pwsh
# Deploys all Kubernetes services using Kustomize

$ErrorActionPreference = "Stop"

$namespace = "drawtogether"
$scriptDir = $PSScriptRoot
$overlayPath = Join-Path (Join-Path $scriptDir "overlays") "local"

Write-Host "Deploying K8s resources using Kustomize into namespace [$namespace]" -ForegroundColor Cyan

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

# Update the kustomization.yaml with the current version
$kustomizationPath = Join-Path $overlayPath "kustomization.yaml"
Write-Host "Updating $kustomizationPath with version $version..." -ForegroundColor Yellow

$kustomization = @"
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
  - ../../base

images:
  - name: drawtogether
    newTag: "$version"
  - name: drawtogether-migrationservice
    newTag: "$version"
"@

Set-Content -Path $kustomizationPath -Value $kustomization -Force
Write-Host "Kustomization updated successfully" -ForegroundColor Green

# Create namespace if it doesn't exist
Write-Host "Creating namespace [$namespace] if it doesn't exist..." -ForegroundColor Yellow
kubectl create namespace $namespace 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Namespace [$namespace] created" -ForegroundColor Green
} else {
    Write-Host "Namespace [$namespace] already exists" -ForegroundColor Gray
}

# Delete migrations job if it exists (Jobs are immutable, must recreate for version updates)
Write-Host "Checking for existing migrations job..." -ForegroundColor Yellow
kubectl get job drawtogether-migrations -n $namespace 2>$null
if ($LASTEXITCODE -eq 0) {
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
