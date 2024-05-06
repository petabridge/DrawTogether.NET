echo off
REM Deploys container instance into Kubernetes namespace

set namespace="drawtogether"

if "%~1"=="" (
	REM No K8S namespace specified
	echo No namespace specified. Defaulting to [%namespace%]
) else (
	set namespace="%~1"
	echo Deploying into K8s namespace [%namespace%]
)

kubectl apply -f "%~dp0/drawtogether.sql.yaml" -n "%namespace%"