# Test Certificates for Akka.Remote TLS

This directory contains **self-signed test certificates** for demonstrating and testing Akka.Remote TLS functionality.

## ⚠️ Important

**These certificates are for TESTING and DEMONSTRATION ONLY.** They are:
- Self-signed (not from a trusted CA)
- Committed to source control
- Using a known password
- **NOT SECURE for production use**

## Files

- `akka-node.pfx` - PKCS#12 certificate with private key
- `akka-node.cer` - Public certificate only
- Password: `Test123!`

## Certificate Details

- **Subject**: CN=DrawTogether.Akka.Remote
- **DNS Names**: localhost, 127.0.0.1, drawtogether, *.drawtogether.svc.cluster.local
- **Valid**: 2 years from generation
- **Algorithm**: RSA 2048-bit, SHA256

## Usage

### In Tests

```csharp
var certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "akka-node.pfx");
var cert = X509CertificateLoader.LoadPkcs12FromFile(certPath, "Test123!");

var remoteOptions = new RemoteOptions
{
    EnableSsl = true,
    Ssl = new SslOptions
    {
        X509Certificate = cert,
        SuppressValidation = true  // Required for self-signed certs
    }
};
```

### In Configuration

```json
{
  "AkkaSettings": {
    "TlsSettings": {
      "Enabled": true,
      "CertificatePath": "certs/akka-node.pfx",
      "CertificatePassword": "Test123!",
      "ValidateCertificates": false
    }
  }
}
```

## Regenerating Certificates

To generate new test certificates:

```powershell
./scripts/generate-certs.ps1
```

This will create fresh certificates with a 2-year validity period.
