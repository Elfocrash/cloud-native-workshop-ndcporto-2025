
var builder = DistributedApplication.CreateBuilder(args);

//builder.AddKubernetesEnvironment("k8s-env");

var dbUsername = builder.AddParameter("postgres-username");
var dbPassword = builder.AddParameter("postgres-password", true);

var mainDb = builder.AddPostgres("main-db", dbUsername, dbPassword, port: 5433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("dometrain");


var cartDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsPreviewEmulator(resourceBuilder =>
    {
        resourceBuilder.WithLifetime(ContainerLifetime.Persistent);
        resourceBuilder.WithDataVolume();
        resourceBuilder.WithDataExplorer();
    })
    .AddCosmosDatabase("cartdb")
    .AddContainer("carts", "/pk");

var redis = builder.AddRedis("redis")
    .WithRedisInsight()
    .WithLifetime(ContainerLifetime.Persistent);

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

builder.AddContainer("prometheus", "prom/prometheus")
    .WithBindMount("../../prometheus", "/etc/prometheus", isReadOnly: true)
    .WithHttpEndpoint(port: 9090, targetPort: 9090);

builder.AddContainer("grafana", "grafana/grafana")
    .WithBindMount("../../grafana/config", "/etc/grafana", isReadOnly: true)
    .WithBindMount("../../grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
    .WithHttpEndpoint(targetPort: 3000, name: "http");

builder.AddProject<Projects.Dometrain_Cart_Processor>("cart-processor")
    .WithReference(cartDb).WaitFor(cartDb);

builder.AddProject<Projects.Dometrain_Monolith_Api>("dometrain-api")
    //.WithReplicas(5)
    .WithReference(mainDb).WaitFor(mainDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(cartDb).WaitFor(cartDb);

var app = builder.Build();
    
app.Run();
