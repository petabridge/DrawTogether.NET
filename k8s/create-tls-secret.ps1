# Create Kubernetes Secret for Akka.Remote TLS Certificate
# This script creates a secret from the test certificate file

$certPath = Join-Path $PSScriptRoot ".." "certs" "akka-node.pfx"

if (-not (Test-Path $certPath)) {
    Write-Error "Certificate file not found at: $certPath"
    exit 1
}

Write-Host "Creating Kubernetes secret 'akka-tls-cert' from certificate..." -ForegroundColor Green

# Delete the secret if it already exists (to allow updates)
kubectl delete secret akka-tls-cert -n drawtogether 2>$null

# Create the secret from the certificate file
kubectl create secret generic akka-tls-cert `
    --from-file=akka-node.pfx=$certPath `
    --namespace=drawtogether

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Secret 'akka-tls-cert' created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To apply this configuration to your deployment:" -ForegroundColor Cyan
    Write-Host "  kubectl apply -k k8s/overlays/local" -ForegroundColor Yellow
} else {
    Write-Error "Failed to create secret"
    exit 1
}
