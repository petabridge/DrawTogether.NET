#### 0.2.11 October 23rd 2024 ####

- Added Deployment-based Kubernetes overlay as alternative to StatefulSet for reduced rebalancing overhead
- Refactored K8s configuration to be DRY - split base resources into separate files
- Added `-Strategy` parameter to deploy.ps1 to choose between StatefulSet and Deployment strategies
- Fixed deployment script error handling for better reliability

#### 0.2.2 May 2nd 2024 ####

Added support for loading configuration via Msft.Ext-compatible environment variables, `appSettings.json`, and `appSettings.{ASPNETCORE_ENVIRONMENT}.json`