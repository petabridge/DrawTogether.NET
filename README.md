# DrawTogether.NET

![DrawTogether Logo](/docs/images/drawtogether-logo-with-text_180x180.png)

A collaborative browser-based drawing program written in .NET. Think of it like "multi-player MS paint."

## Architecture and Motivation 

Want to understand how DrawTogether was built and how it works? Please see our videos on the subject:

* [Building Real-Time Web Applications with Blazor and Akka.NET](https://www.youtube.com/watch?v=jRYVp_lySl8)
* [Application Design with Actors Made Easy: Think Locally, Act Globally](https://www.youtube.com/watch?v=K5qaCnBcy-E)

## Running Locally

First things first, you will need to launch the dependencies for DrawTogether.NET - make sure you have `docker` installed locally - the easiest way to do this is with [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview):

```shell
dotnet run --project src/DrawTogether.AppHost/DrawTogether.AppHost.csproj
```

This will:
- Start the SQL Server database
- Run the migration service to initialize the database
- Launch the DrawTogether web application
- Open the Aspire dashboard in your browser

The Aspire dashboard will show you all running services with their statuses and endpoints.


### Entity Framework Core Migrations

If you need to apply a change to the database model, by default this project uses the SQL Server 2022 launched in the step above. To apply migrations, change your directory to the `DrawTogether` project:

```shell
cd ./src/DrawTogether/
```

And then apply the migrations:

```shell
dotnet ef database update
```

To generate a migration script that you can apply manually (later):

```shell
dotnet ef migrations script
```

### MailGun Configuration

DrawTogether.NET can use [MailGun](https://mailgun.com/) to send outbound emails (via `FluentEmail.Mailgun`) - and the following two secrets need to be configured in order for that sending to work:

```shell
cd ./src/DrawTogether/
dotnet user-secrets set "EmailSettings:MailgunDomain" "<mailgun-domain>"
dotnet user-secrets set "EmailSettings:MailgunApiKey" "<mailgun-api-key>"
```

If these settings are not provided, DrawTogether.NET will simply fall back to not having email available to support ASP.NET Core Identity.

## Deployment

Out of the box DrawTogether.NET supports Kubernetes deployments, however, if you want to run it locally you'll need to make sure that you have [Nginx Ingress](https://kubernetes.github.io/ingress-nginx/deploy/#quick-start) enabled.

This is how to deploy the most recent version of Nginx Ingress on Docker Desktop at the time of writing this:

```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.10.1/deploy/static/provider/cloud/deploy.yaml
```

Next, you will need to build the Docker images for both the application and migration service:

```bash
dotnet publish --os linux --arch x64 -c Release -t:PublishContainer
```

This will tag local Docker images with the following labels:

* `drawtogether:latest` and `drawtogether:{VERSION}`
* `drawtogether-migrationservice:latest` and `drawtogether-migrationservice:{VERSION}`

The version number is pulled from `Directory.Build.props` and automatically applied during deployment.

### Kustomize Deployment

The K8s manifests use [Kustomize](https://kustomize.io/) for configuration management:

* **Base configuration** (`k8s/base/`): Contains all core Kubernetes manifests without version tags
* **Environment overlays** (`k8s/overlays/local/`): Environment-specific configurations that apply version tags

### Deployment Scripts

#### Deploy or Update Services

To deploy or update all services to your local Kubernetes cluster:

```powershell
./k8s/deployAll.ps1
```

This script will:
1. Extract the version from `Directory.Build.props`
2. Update the Kustomize overlay with the current version
3. Create the `drawtogether` namespace if needed
4. Apply all Kubernetes manifests using Kustomize
5. Wait for the StatefulSet rollout to complete (with zero-downtime rolling updates)

**This script is idempotent** - run it for both initial deployment and updates. Kubernetes automatically performs zero-downtime rolling updates when you change the image version.

DrawTogether will be available at http://drawtogether.localdev.me

#### Destroy All Services

To remove all DrawTogether resources from Kubernetes:

```powershell
./k8s/destroyAll.ps1
```

This deletes the entire `drawtogether` namespace and all resources within it.
