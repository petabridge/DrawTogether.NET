# Kubernetes Deployment for DrawTogether.NET

This directory contains Kubernetes manifests and deployment scripts for running DrawTogether.NET on Kubernetes.

## Prerequisites

- Docker Desktop with Kubernetes enabled (or any local Kubernetes cluster)
- kubectl installed and configured
- PowerShell Core (pwsh) for running deployment scripts

### Installing PowerShell Core (for Linux/macOS users)

The deployment scripts are written in PowerShell and require PowerShell Core (pwsh).

**Linux (Ubuntu/Debian):**
```bash
# Update package list
sudo apt-get update

# Install PowerShell
sudo apt-get install -y wget apt-transport-https software-properties-common
wget -q "https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb"
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y powershell

# Verify installation
pwsh --version
```

**macOS:**
```bash
# Using Homebrew
brew install --cask powershell

# Verify installation
pwsh --version
```

**Other platforms:** See https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell

## Directory Structure

```
k8s/
├── README.md                  # This file
├── deployAll.ps1             # Deploy or update all services
├── destroyAll.ps1            # Remove all services
├── base/                     # Base Kubernetes manifests (environment-agnostic)
│   ├── kustomization.yaml
│   ├── configs/              # ConfigMaps, RBAC
│   ├── deployments/          # StatefulSets
│   └── services/             # Services, Ingress
└── overlays/                 # Environment-specific configurations
    └── local/                # Local development overlay
        └── kustomization.yaml
```

This project uses [Kustomize](https://kustomize.io/) to manage Kubernetes configurations with a **base/overlays** pattern:
- **base/**: Contains environment-agnostic Kubernetes manifests
- **overlays/local/**: Contains local development-specific settings (image tags, replica counts, etc.)

## Quick Start

### 1. Build Docker Images

First, build the Docker images:

```bash
# From the repository root
dotnet publish --os linux --arch x64 -c Release -t:PublishContainer
```

This creates two Docker images:
- `drawtogether:latest` and `drawtogether:{VERSION}`
- `drawtogether-migrationservice:latest` and `drawtogether-migrationservice:{VERSION}`

The version is automatically pulled from `Directory.Build.props`.

### 2. Install Nginx Ingress Controller

DrawTogether requires an Ingress controller. For Docker Desktop:

```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.10.1/deploy/static/provider/cloud/deploy.yaml
```

Wait for the ingress controller to be ready:

```bash
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=120s
```

### 3. Deploy to Kubernetes

Run the deployment script:

```powershell
./k8s/deployAll.ps1
```

Or if running from the k8s directory:

```powershell
cd k8s
./deployAll.ps1
```

This script will:
1. Extract the version from `Directory.Build.props`
2. Update the Kustomize overlay with the current version
3. Create the `drawtogether` namespace (if it doesn't exist)
4. Delete any existing migrations job (Jobs are immutable)
5. Apply all Kubernetes manifests using Kustomize
6. Wait for the StatefulSet rollout to complete
7. Display the status of all resources

### 4. Access the Application

Once deployed, DrawTogether will be available at:

**http://drawtogether.localdev.me**

> **Note:** `localdev.me` is a public DNS entry that resolves to 127.0.0.1, making it convenient for local development with Ingress.

## Common Operations

### Check Deployment Status

```bash
kubectl get all -n drawtogether
```

### View Logs

```bash
# View logs from a specific pod
kubectl logs drawtogether-0 -n drawtogether -c drawtogether-app

# Follow logs
kubectl logs -f drawtogether-0 -n drawtogether -c drawtogether-app

# View logs from all pods
kubectl logs -l app=drawtogether -n drawtogether -c drawtogether-app
```

### Check Health Endpoints

```bash
# Port-forward to access health endpoints
kubectl port-forward drawtogether-0 -n drawtogether 8080:8080

# Then access:
# http://localhost:8080/healthz       - All health checks
# http://localhost:8080/healthz/live  - Liveness checks only
# http://localhost:8080/healthz/ready - Readiness checks only
```

### Update to New Version

1. Update the version in `Directory.Build.props`
2. Rebuild Docker images:
   ```bash
   dotnet publish --os linux --arch x64 -c Release -t:PublishContainer
   ```
3. Run the deployment script again:
   ```powershell
   ./k8s/deployAll.ps1
   ```

The script is **idempotent** - it performs zero-downtime rolling updates when you run it multiple times.

### Scale the Application

```bash
# Scale to 5 replicas
kubectl scale statefulset drawtogether --replicas=5 -n drawtogether

# Check rollout status
kubectl rollout status statefulset/drawtogether -n drawtogether
```

### Destroy All Resources

To completely remove DrawTogether from Kubernetes:

```powershell
./k8s/destroyAll.ps1
```

This deletes the entire `drawtogether` namespace and all resources within it.

## Troubleshooting

### Pods Not Starting

Check pod events:
```bash
kubectl describe pod drawtogether-0 -n drawtogether
```

Check pod logs:
```bash
kubectl logs drawtogether-0 -n drawtogether -c drawtogether-app
```

### Image Pull Errors

Verify the images exist locally:
```bash
docker images | grep drawtogether
```

If images are missing, rebuild:
```bash
dotnet publish --os linux --arch x64 -c Release -t:PublishContainer
```

### Database Connection Issues

Check if SQL Server is running:
```bash
kubectl get pods -n drawtogether | grep sql
```

Check database logs:
```bash
kubectl logs drawtogether-sql-0 -n drawtogether
```

### Ingress Not Working

Verify Nginx Ingress controller is running:
```bash
kubectl get pods -n ingress-nginx
```

Check Ingress configuration:
```bash
kubectl describe ingress drawtogether-ingress -n drawtogether
```

### RBAC Permission Errors

Check service account and roles:
```bash
kubectl get serviceaccount,role,rolebinding -n drawtogether
```

View pod logs for permission errors:
```bash
kubectl logs drawtogether-0 -n drawtogether -c drawtogether-app | grep -i forbidden
```

## Advanced: Manual Kustomize Usage

If you prefer to use `kubectl` and `kustomize` directly:

### Preview Configuration
```bash
kubectl kustomize ./overlays/local
```

### Apply Configuration
```bash
kubectl apply -k ./overlays/local
```

### Delete Configuration
```bash
kubectl delete -k ./overlays/local
```

## Architecture Notes

### StatefulSet vs Deployment

DrawTogether uses a **StatefulSet** instead of a Deployment because:
- Akka.NET cluster requires stable network identities
- Each pod needs a predictable hostname for cluster formation
- StatefulSets provide ordered deployment and scaling

### RBAC Configuration

The application requires Kubernetes API access for:
- Pod discovery (for Akka.NET cluster formation)
- Reading pod metadata
- Watching pod changes

RBAC is configured in `base/configs/k8s-rbac.yaml`.

### Health Checks

The application exposes three health check endpoints:
- `/healthz` - All health checks (liveness + readiness)
- `/healthz/live` - Liveness probe (self check only)
- `/healthz/ready` - Readiness probe (Akka.Persistence health checks)

## Additional Resources

- [Kustomize Documentation](https://kustomize.io/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Akka.NET Kubernetes Integration](https://getakka.net/articles/clustering/cluster-configuration.html#akka-cluster-bootstrap-for-kubernetes-api)
- [PowerShell Documentation](https://learn.microsoft.com/en-us/powershell/)
