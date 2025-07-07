using DrawTogether.AppHost;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var drawTogetherAspireConfig = builder.Configuration.GetSection("DrawTogether")
    .Get<DrawTogetherConfiguration>() ?? new DrawTogetherConfiguration();

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

var migrationService = builder.AddProject<Projects.DrawTogether_MigrationService>("MigrationService")
    .WaitFor(db)
    .WithReference(db);

if (drawTogetherAspireConfig.DeployEnvironment == DeployEnvironment.Docker)
{
    var drawTogether = builder.AddDockerfile("DrawTogether-1", "../../", "./src/DrawTogether/DockerFile")
        .WithImage("draw-together", "latest")
        .WithReference(db, "DefaultConnection")
        .ConfigureAkkaManagementForApp(drawTogetherAspireConfig)
        .WaitForCompletion(migrationService);

    foreach (var index in Enumerable.Range(2, drawTogetherAspireConfig.Replicas - 1))
    {
        builder.AddContainer($"DrawTogether-{index}", "draw-together")
            .WithReference(db, "DefaultConnection")
            .ConfigureAkkaManagementForApp(drawTogetherAspireConfig)
            .WaitFor(drawTogether);
    }
    
    // https://github.com/petabridge/pbm-sidecar - used to run `pbm` commands on the DrawTogether actor system
    var pbmSidecar = builder.AddContainer("pbm-sidecar", "petabridge/pbm:latest")
        .WaitFor(drawTogether);
}
else
{
    var drawTogether = builder.AddProject<Projects.DrawTogether>("DrawTogether")
        .WithReplicas(drawTogetherAspireConfig.Replicas)
        .WithReference(db, "DefaultConnection")
        .WaitForCompletion(migrationService)
        .ConfigureAkkaManagementForApp(drawTogetherAspireConfig);

    // https://github.com/petabridge/pbm-sidecar - used to run `pbm` commands on the DrawTogether actor system
    var pbmSidecar = builder.AddContainer("pbm-sidecar", "petabridge/pbm:latest")
        .WaitFor(drawTogether);
}

builder
    .Build()
    .Run();
