# DrawTogether.NET

A collaborative browser-based drawing program written in .NET.

Please see [REQUIREMENTS](docs/requirements.md) for more information.

## UI 
![DrawTogether.NET UI](/docs/images/paintarea-ui.png)

## MailGun Configuration

DrawTogether.NET can use [MailGun](https://mailgun.com/) to send outbound emails (via `FluentEmail.Mailgun`) - and the following two secrets need to be configured in order for that sending to work:

```shell
cd ./src/DrawTogether/
dotnet user-secrets set "EmailSettings:MailgunDomain" "<mailgun-domain>"
dotnet user-secrets set "EmailSettings:MailgunApiKey" "<mailgun-api-key>"
```

If these settings are not provided, DrawTogether.NET will simply fall back to not having email available to support ASP.NET Core Identity.