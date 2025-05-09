using DrawTogether.Data;
using DrawTogether.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSqlServerDbContext<ApplicationDbContext>("DrawTogetherDb");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();