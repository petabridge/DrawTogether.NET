var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sql");

var db = sqlServer.AddDatabase("DrawTogetherDb");

var azureStorage = builder.AddAzureStorage("storage")
    .AddBlobs("blobs");

var migrationService = builder.AddProject<Projects.DrawTogether_MigrationService>("MigrationService")
    .WaitFor(db)
    .WithReference(db);

builder.AddProject<Projects.DrawTogether>("DrawTogether")
    .WithReference(db, "DefaultConnection")
    .WithReference(azureStorage, "AzureStorage")
    .WaitForCompletion(migrationService);
   // .WithReplicas(3);

builder
    .Build()
    .Run();
