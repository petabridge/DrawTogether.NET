using Aspire.Hosting.Azure;
using DrawTogether.AppHost;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);


var drawTogetherAspireConfig = builder.Configuration.GetSection("DrawTogether")
    .Get<DrawTogetherConfiguration>() ?? new DrawTogetherConfiguration();

builder.AddDockerComposePublisher()
    .AddKubernetesPublisher();

// Adding a default password for ease of use - we can get rid of this but for a quick "git clone and run" it makes sense
// have to add this when using data volumes otherwise Aspire will brick itself

var saPassword = builder.AddParameter(
    "sql-sa-password",
    () => "YourStrong!Passw0rd", // *must* satisfy SQL Server complexity rules
    secret: true);

var sqlServer = builder.AddSqlServer("sql", saPassword);

if (drawTogetherAspireConfig.UseVolumes)
{
    // add a persistent data volume that can survive restarts
    sqlServer.WithDataVolume();
}

var db = sqlServer.AddDatabase("DrawTogetherDb");

var tableStorage = builder.ConfigureAkkaManagementStorage(drawTogetherAspireConfig);

var migrationService = builder.AddProject<Projects.DrawTogether_MigrationService>("MigrationService")
    .WaitFor(db)
    .WithReference(db);

var drawTogether = builder.AddProject<Projects.DrawTogether>("DrawTogether")
    .WithReference(db, "DefaultConnection")
    .WaitForCompletion(migrationService)
    .WithEndpoint("pbm", annotation =>
    {
        annotation.Port = 9110;
        
        // not mean to be external, meant to be invoked via the pbm-sidecar
        annotation.IsExternal = false;
        annotation.IsProxied = false;
    });

// enable Akka.Management, if necessary
drawTogether.ConfigureAkkaManagementForApp(drawTogetherAspireConfig);

// https://github.com/petabridge/pbm-sidecar - used to run `pbm` commands on the DrawTogether actor system
var pbmSidecar = builder.AddContainer("pbm-sidecar", "petabridge/pbm:latest")
    .WaitFor(drawTogether);


builder
    .Build()
    .Run();
