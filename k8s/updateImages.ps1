#!/usr/bin/env pwsh
# Performs a rolling update of container images to a live running cluster

$ErrorActionPreference = "Stop"

$namespace = "drawtogether"
$scriptDir = $PSScriptRoot
$overlayPath = Join-Path (Join-Path $scriptDir "overlays") "local"

Write-Host "Performing rolling update of images in namespace [$namespace]" -ForegroundColor Cyan

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

# Perform rolling update using kubectl set image
Write-Host "`nUpdating StatefulSet image to drawtogether:$version..." -ForegroundColor Yellow
kubectl set image statefulset/drawtogether drawtogether-app=drawtogether:$version -n $namespace

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update StatefulSet image"
    exit 1
}

Write-Host "StatefulSet image updated successfully" -ForegroundColor Green

Write-Host "`nChecking rollout status..." -ForegroundColor Yellow
kubectl rollout status statefulset/drawtogether -n $namespace --timeout=5m

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nRolling update completed successfully!" -ForegroundColor Green
    Write-Host "`nCurrent pod status:" -ForegroundColor Cyan
    kubectl get pods -n $namespace -l app=drawtogether
} else {
    Write-Error "Rolling update did not complete successfully within timeout"
    Write-Host "`nCurrent pod status:" -ForegroundColor Yellow
    kubectl get pods -n $namespace -l app=drawtogether
    exit 1
}

Write-Host "`nNote: The migrations job was not updated. If schema changes are needed, delete and recreate the job manually." -ForegroundColor Gray
