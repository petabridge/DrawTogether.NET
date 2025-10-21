#!/usr/bin/env pwsh
# Destroys all K8s services in "drawtogether" namespace

$ErrorActionPreference = "Stop"

$namespace = "drawtogether"

Write-Host "Destroying all resources in namespace [$namespace]..." -ForegroundColor Red

kubectl delete namespace $namespace

if ($LASTEXITCODE -eq 0) {
    Write-Host "Namespace [$namespace] deleted successfully" -ForegroundColor Green
} else {
    Write-Warning "Failed to delete namespace [$namespace] or it doesn't exist"
    exit 1
}
