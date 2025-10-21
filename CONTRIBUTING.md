# Contributing to DrawTogether.NET

Thank you for your interest in contributing to DrawTogether.NET! This guide will help you set up your development environment and understand the project's development workflow.

## Development Setup

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for running dependencies via Aspire)
- [Git](https://git-scm.com/)

### Running Locally

Start the application with all its dependencies using .NET Aspire:

```shell
dotnet run --project src/DrawTogether.AppHost/DrawTogether.AppHost.csproj
```

This automatically:
- Starts SQL Server in a container
- Runs database migrations via the MigrationService
- Launches the DrawTogether web application
- Opens the Aspire dashboard in your browser

The Aspire dashboard shows all running services with their statuses and endpoints.

## Working with Entity Framework Core Migrations

### Important: Migrations Are Auto-Applied

When running via Aspire (the recommended approach), **database migrations are automatically applied** by the `DrawTogether.MigrationService` before the main application starts. You do **not** need to manually run `dotnet ef database update`.

### Creating New Migrations

When you modify the database model (Entity Framework entities), create a new migration:

```shell
cd ./src/DrawTogether/
dotnet ef migrations add YourMigrationName
```

This generates migration files in `src/DrawTogether/Data/Migrations/`.

### Testing Migrations Manually

If you need to test migrations against a standalone database (not via Aspire):

```shell
cd ./src/DrawTogether/
dotnet ef database update
```

### Generating SQL Scripts

To generate a SQL script for manual deployment:

```shell
cd ./src/DrawTogether/
dotnet ef migrations script
```

## Email Configuration (Optional)

DrawTogether.NET can use [MailGun](https://mailgun.com/) for sending emails (via `FluentEmail.Mailgun`). This is **optional** - the application works without email configured.

To enable email:

```shell
cd ./src/DrawTogether/
dotnet user-secrets set "EmailSettings:MailgunDomain" "<your-mailgun-domain>"
dotnet user-secrets set "EmailSettings:MailgunApiKey" "<your-mailgun-api-key>"
```

If these settings are not provided, DrawTogether.NET will run without email functionality.

## Testing

DrawTogether.NET has comprehensive test coverage using xUnit, Playwright, and Aspire integration tests.

### Running Tests

```shell
dotnet test
```

### Test Projects

- **DrawTogether.Tests** - Unit and integration tests
- **DrawTogether.End2End.Tests** - End-to-end Playwright tests

### Testing Documentation

For detailed information on authoring tests:

- [Aspire Integration Tests Guide](./docs/aspire-integration-tests.md) - How to write integration tests using .NET Aspire
- [Blazor Playwright Tests Guide](./docs/blazor-playwright-tests.md) - How to write UI tests using Playwright

## Code Style

- Follow standard C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise

## Pull Request Process

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to your branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### PR Guidelines

- Provide a clear description of the changes
- Reference any related issues
- Ensure all CI checks pass
- Keep PRs focused on a single feature or fix
- Update documentation as needed

## Questions or Issues?

If you have questions or encounter issues:

1. Check the [existing issues](https://github.com/petabridge/DrawTogether.NET/issues)
2. Review the [documentation](./README.md)
3. Open a new issue with a clear description and reproduction steps

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.
