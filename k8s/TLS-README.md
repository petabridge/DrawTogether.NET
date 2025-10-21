# TLS Configuration for Kubernetes Deployment

This document explains how to deploy DrawTogether.NET with TLS enabled for Akka.Remote communication in Kubernetes.

## Overview

The Kubernetes deployment is configured to use TLS for Akka.Remote cluster communication. The TLS certificate is mounted as a Kubernetes Secret and configured via environment variables.

## Prerequisites

- kubectl configured to access your cluster
- The DrawTogether image built and available
- The `drawtogether` namespace created

## Deployment Steps

### 1. Create the TLS Secret

Before deploying the application, you need to create a Kubernetes Secret containing the TLS certificate:

```powershell
# From the k8s directory
.\create-tls-secret.ps1
```

This script will:
- Read the test certificate from `../certs/akka-node.pfx`
- Create a Kubernetes Secret named `akka-tls-cert` in the `drawtogether` namespace
- The certificate will be mounted at `/certs/akka-node.pfx` in the pods

### 2. Deploy the Application

Deploy the application using Kustomize:

```bash
kubectl apply -k overlays/local
```

### 3. Verify TLS is Enabled

Check that pods are running with TLS enabled:

```bash
# Check pod environment variables
kubectl get pods -n drawtogether
kubectl exec drawtogether-0 -n drawtogether -- env | grep TLS

# Expected output should show:
# AkkaSettings__TlsSettings__Enabled=true
# AkkaSettings__TlsSettings__CertificatePath=/certs/akka-node.pfx
# AkkaSettings__TlsSettings__CertificatePassword=Test123!
# AkkaSettings__TlsSettings__ValidateCertificates=false
```

Check the pod logs to verify TLS is active:

```bash
kubectl logs drawtogether-0 -n drawtogether -c drawtogether-app | grep -i "ssl\|tls"
```

You should see Akka.Remote using `akka.ssl.tcp://` protocol instead of `akka.tcp://`.

## Configuration Details

### Environment Variables

The StatefulSet configures TLS through these environment variables (see `base/k8s-web-service.yaml:112-120`):

```yaml
- name: AkkaSettings__TlsSettings__Enabled
  value: "true"
- name: AkkaSettings__TlsSettings__CertificatePath
  value: "/certs/akka-node.pfx"
- name: AkkaSettings__TlsSettings__CertificatePassword
  value: "Test123!"
- name: AkkaSettings__TlsSettings__ValidateCertificates
  value: "false"  # Required for self-signed certificates
```

### Volume Mount

The certificate is mounted as a read-only volume:

```yaml
volumeMounts:
- name: tls-cert
  mountPath: /certs
  readOnly: true

volumes:
- name: tls-cert
  secret:
    secretName: akka-tls-cert
```

## Security Considerations

### Test Certificate Warning

⚠️ **IMPORTANT**: The included certificate (`certs/akka-node.pfx`) is a **self-signed test certificate** and should **ONLY** be used for:
- Local development
- Testing
- Demonstrations

### For Production

For production deployments:

1. **Generate production certificates** from a trusted Certificate Authority (CA)
2. **Update the secret** with your production certificate:
   ```bash
   kubectl create secret generic akka-tls-cert \
     --from-file=akka-node.pfx=/path/to/production/cert.pfx \
     --namespace=drawtogether \
     --dry-run=client -o yaml | kubectl apply -f -
   ```
3. **Enable certificate validation**:
   - Change `AkkaSettings__TlsSettings__ValidateCertificates` to `"true"`
4. **Secure the password**:
   - Store the certificate password in a separate Secret
   - Reference it using `valueFrom.secretKeyRef` instead of plain text

Example secure configuration:

```yaml
# Create password secret
kubectl create secret generic akka-tls-password \
  --from-literal=password=<your-secure-password> \
  --namespace=drawtogether

# Reference in StatefulSet
- name: AkkaSettings__TlsSettings__CertificatePassword
  valueFrom:
    secretKeyRef:
      name: akka-tls-password
      key: password
```

## Disabling TLS

To disable TLS for testing:

1. Edit `k8s/base/k8s-web-service.yaml`
2. Change `AkkaSettings__TlsSettings__Enabled` to `"false"`
3. Redeploy: `kubectl apply -k overlays/local`

## Troubleshooting

### Certificate Not Found Error

If pods fail with "Certificate file not found":
- Verify the secret exists: `kubectl get secret akka-tls-cert -n drawtogether`
- Check the secret contents: `kubectl describe secret akka-tls-cert -n drawtogether`
- Recreate the secret using `create-tls-secret.ps1`

### TLS Handshake Failures

If you see TLS handshake errors in logs:
- Ensure all pods use the same certificate
- Verify `ValidateCertificates` is set to `false` for self-signed certs
- Check that the certificate includes the correct DNS names (see `certs/README.md`)

### Cluster Formation Issues

If the cluster doesn't form with TLS enabled:
- Check Akka.Remote is using `akka.ssl.tcp://` protocol in logs
- Verify port 5055 is accessible between pods
- Ensure the certificate is valid and not expired

## Additional Resources

- [Akka.Remote TLS Documentation](https://getakka.net/articles/remoting/security.html)
- [Akka.Hosting Documentation](https://github.com/akkadotnet/Akka.Hosting)
- See `certs/README.md` for certificate generation details
- See `scripts/generate-certs.ps1` for certificate regeneration
