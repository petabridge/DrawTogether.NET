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

## Rotating Certificates Without a Restart

If `CertificatePath` names a **folder** instead of a file, each node reads the folder in the
background and uses the usable certificate that was issued most recently (latest start date)
for new connections. To rotate, copy the new `.pfx` into the folder. No restart, no config change.
All `.pfx` files in the folder must share the one `CertificatePassword`.

The committed `akka-node.pfx` only allows server authentication, so a folder rejects it:
with mutual TLS (the default) a certificate must allow both server and client authentication.
Make your own test certificates instead, for example with OpenSSL:

```bash
mkdir -p /tmp/akka-tls && cd /tmp/akka-tls
for name in cert-a cert-b; do
  openssl req -x509 -newkey rsa:2048 -nodes -keyout $name.key -out $name.crt -days 365 \
    -subj "/CN=DrawTogether.Akka.Remote" \
    -addext "keyUsage=critical,digitalSignature,keyEncipherment" \
    -addext "extendedKeyUsage=serverAuth,clientAuth"
  openssl pkcs12 -export -inkey $name.key -in $name.crt -passout pass:'Test123!' -out $name.pfx
  sleep 2   # cert-b must start after cert-a
done
mkdir -p tls && cp cert-a.pfx tls/
```

Run the Aspire host with TLS on and the folder as the certificate path (3 replicas):

```bash
dotnet run --project src/DrawTogether.AppHost -- \
  --DrawTogether:TlsCertificatePath=/tmp/akka-tls/tls \
  --DrawTogether:TlsCertificatePassword='Test123!' \
  --DrawTogether:Replicas=3
```

Then `cp /tmp/akka-tls/cert-b.pfx /tmp/akka-tls/tls/`. Within about a minute each node logs:

```text
TLS client and server certificate changed from <A thumbprint> (...) to <B thumbprint> (...)
```

Check what a node presents on a new connection (the remoting port is in the
`Remoting started; listening on addresses` log line):

```bash
openssl s_client -connect localhost:<port> </dev/null 2>/dev/null | openssl x509 -noout -fingerprint -sha1 -dates
```

Open connections keep the old certificate until they close. Remove the old `.pfx` whenever you like.
Since validation is off for these self-signed test certificates (`ValidateCertificates = false`),
nodes on the old and new certificate keep talking during the change.
