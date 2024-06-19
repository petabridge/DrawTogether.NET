#!/bin/bash
# deploys all Kubernetes services to their staging environment

namespace="drawtogether"
location="$(dirname "$0")/environment"

echo "Deploying K8s resources from [$location] into namespace [$namespace]"

echo "Creating Namespaces..."
kubectl create ns "$namespace"

echo "Using namespace [$namespace] going forward..."

echo "Creating configurations from YAML files in [$location/configs]"
for f in "$location/configs"/*.yaml; do
    echo "Deploying $(basename "$f")"
    kubectl apply -f "$f" -n "$namespace"
done

echo "Creating environment-specific services from YAML files in [$location]"
for f in "$location"/*.yaml; do
    echo "Deploying $(basename "$f")"
    kubectl apply -f "$f" -n "$namespace"
done

echo "Creating all services..."
for f in "$(dirname "$0")/services"/*.yaml; do
    echo "Deploying $(basename "$f")"
    kubectl apply -f "$f" -n "$namespace"
done

echo "All services started... Printing K8s output.."
kubectl get all -n "$namespace"
