#!/usr/bin/env bash
# Deploys container instance into Kubernetes namespace
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )


namespace="drawtogether"

if [ -z $1 ]; then
	echo "No namespace specified. Defaulting to [${namespace}]"
else
	namespace="$1"
	echo "Deploying into K8s namespace [${namespace}]"
fi

kubectl apply -f "drawtogether.sql.yaml" -n "%namespace%"