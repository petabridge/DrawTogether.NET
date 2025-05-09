var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposePublisher()
    .AddKubernetesPublisher();

var sqlServer = builder.AddSqlServer("sql");

var db = sqlServer.AddDatabase("DrawTogetherDb");

var azureStorage = builder.AddAzureStorage("storage")
    .AddBlobs("blobs");

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

// https://github.com/petabridge/pbm-sidecar - used to run `pbm` commands on the DrawTogether actor system
var pbmSidecar = builder.AddContainer("pbm-sidecar", "petabridge/pbm:latest")
    .WaitFor(drawTogether);


builder
    .Build()
    .Run();
