#!/usr/bin/env pwsh
# Generate self-signed certificates for Akka.Remote TLS testing

$ErrorActionPreference = "Stop"

$outputDir = "certs"
$certPassword = "Test123!"

# Create output directory if it doesn't exist
if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
    Write-Host "Created directory: $outputDir" -ForegroundColor Green
}

# Generate a self-signed certificate for Akka.Remote
Write-Host "Generating self-signed certificate..." -ForegroundColor Yellow

$cert = New-SelfSignedCertificate `
    -Subject "CN=DrawTogether.Akka.Remote" `
    -DnsName "localhost", "127.0.0.1", "drawtogether", "*.drawtogether.svc.cluster.local" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date) `
    -NotAfter (Get-Date).AddYears(2) `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -FriendlyName "DrawTogether Akka.Remote Test Certificate" `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1") # Server Authentication

Write-Host "Certificate created with thumbprint: $($cert.Thumbprint)" -ForegroundColor Green

# Export certificate to PFX file
$pfxPath = Join-Path $outputDir "akka-node.pfx"
$securePassword = ConvertTo-SecureString -String $certPassword -Force -AsPlainText

Export-PfxCertificate `
    -Cert $cert `
    -FilePath $pfxPath `
    -Password $securePassword | Out-Null

Write-Host "Certificate exported to: $pfxPath" -ForegroundColor Green

# Export public key (for verification)
$cerPath = Join-Path $outputDir "akka-node.cer"
Export-Certificate `
    -Cert $cert `
    -FilePath $cerPath | Out-Null

Write-Host "Public key exported to: $cerPath" -ForegroundColor Green

# Remove certificate from certificate store (cleanup)
Remove-Item -Path "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force
Write-Host "Certificate removed from certificate store" -ForegroundColor Gray

Write-Host "`nCertificate generation complete!" -ForegroundColor Green
Write-Host "Password: $certPassword" -ForegroundColor Cyan
Write-Host "`nTo use with DrawTogether, update your appsettings.json:" -ForegroundColor Yellow
Write-Host @"
"AkkaSettings": {
  "TlsSettings": {
    "Enabled": true,
    "CertificatePath": "$pfxPath",
    "CertificatePassword": "$certPassword",
    "ValidateCertificates": false
  }
}
"@ -ForegroundColor White
