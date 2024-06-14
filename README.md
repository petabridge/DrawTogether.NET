# DrawTogether.NET

A collaborative browser-based drawing program written in .NET.

Please see [REQUIREMENTS](docs/requirements.md) for more information.

## Running Locally

First things first, you will need to launch the dependencies for DrawTogether.NET - make sure you have `docker` installed locally:

**Windows**

```shell
start-dependencies.cmd
```

**Linux**

```shell
start-dependencies.sh
```

This will launch, among other things, a prebuilt SQL Server 2022 instance that has the correct default connection string and `DrawTogether` database required by DrawTogether.NET.

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

## MailGun Configuration

DrawTogether.NET can use [MailGun](https://mailgun.com/) to send outbound emails (via `FluentEmail.Mailgun`) - and the following two secrets need to be configured in order for that sending to work:

```shell
cd ./src/DrawTogether/
dotnet user-secrets set "EmailSettings:MailgunDomain" "<mailgun-domain>"
dotnet user-secrets set "EmailSettings:MailgunApiKey" "<mailgun-api-key>"
```

If these settings are not provided, DrawTogether.NET will simply fall back to not having email available to support ASP.NET Core Identity.
