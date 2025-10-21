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

# Apply Kustomize configuration
Write-Host "Applying Kustomize configuration from [$overlayPath]..." -ForegroundColor Yellow
kubectl apply -k $overlayPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to apply Kustomize configuration"
    exit 1
}

Write-Host "All services started successfully!" -ForegroundColor Green
Write-Host "`nPrinting K8s output:" -ForegroundColor Cyan
kubectl get all -n $namespace
