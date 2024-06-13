@echo off
REM deploys all Kubernetes services to their staging environment

set namespace=drawtogether
set location=%~dp0environment

echo "Deploying K8s resources from [%location%] into namespace [%namespace%]"

echo "Creating Namespaces..."
kubectl create ns "%namespace%"

echo "Using namespace [%namespace%] going forward..."

echo "Creating configurations from YAML files in [%location%/configs]"
for %%f in (%location%/configs/*.yaml) do (
    echo "Deploying %%~nxf"
    kubectl apply -f "%location%/configs/%%~nxf" -n "%namespace%"
)

echo "Creating environment-specific services from YAML files in [%location%]"
for %%f in (%location%/*.yaml) do (
    echo "Deploying %%~nxf"
    kubectl apply -f "%location%/%%~nxf" -n "%namespace%"
)

echo "Creating all services..."
for %%f in (%~dp0/services/*.yaml) do (
    echo "Deploying %%~nxf"
    kubectl apply -f "%~dp0/services/%%~nxf" -n "%namespace%"
)

echo "All services started... Printing K8s output.."
kubectl get all -n "%namespace%"