using Akka.Aspire.Hosting;
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

var drawTogether = builder.AddProject<Projects.DrawTogether>("DrawTogether")
    .WithReplicas(drawTogetherAspireConfig.Replicas)
    .WithReference(db, "DefaultConnection")
    .WaitForCompletion(migrationService);

if (drawTogetherAspireConfig.UseAkkaManagement)
{
    var redis = builder.AddRedis("akka-discovery");
    var akka = builder.AddAkka("drawtogether").WithClustering(redis);
    drawTogether.WithReference(akka);

    // Gate Aspire readiness on cluster formation + persistence health
    drawTogether.WithHttpHealthCheck("/healthz/ready");
}

// PBM port still needs explicit endpoint since plugin doesn't handle it
drawTogether.WithEndpoint(name: "pbm", protocol: System.Net.Sockets.ProtocolType.Tcp,
    env: "AkkaSettings__PbmOptions__Port");

// https://github.com/petabridge/pbm-sidecar - used to run `pbm` commands on the DrawTogether actor system
var pbmSidecar = builder.AddContainer("pbm-sidecar", "petabridge/pbm:latest")
    .WaitFor(drawTogether);

builder
    .Build()
    .Run();
