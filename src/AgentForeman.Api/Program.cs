using AgentForeman.Api;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Events;
using AgentForeman.Core.State;
using AgentForeman.Infrastructure.Configuration;
using AgentForeman.Infrastructure.State;

var configPath = ResolveConfigPath(args);
var configLoader = new YamlAgentForemanConfigLoader();
var configResult = configLoader.Load(configPath);

if (!configResult.IsValid)
{
    foreach (var error in configResult.Errors)
    {
        Console.Error.WriteLine(error);
    }

    return 1;
}

var config = configResult.Config!;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAgentForemanConfigLoader>(configLoader);
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<IStateStore, PostgresStateStore>();
builder.Services.AddSingleton<IMissionRepository>(_ => new PostgresMissionRepository(config.Database.ConnectionString));
builder.Services.AddSingleton<IMissionEventRecorder>(_ => new PostgresMissionEventRecorder(config.Database.ConnectionString));
builder.Services.AddAgentForemanApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Services.GetRequiredService<IStateStore>().Initialize(config);
app.MapAgentForemanApi();
app.Run();
return 0;

static string ResolveConfigPath(IReadOnlyList<string> args)
{
    for (var index = 0; index < args.Count - 1; index++)
    {
        if (args[index] == "--config")
        {
            return args[index + 1];
        }
    }

    var envPath = Environment.GetEnvironmentVariable("AGENT_FOREMAN_CONFIG");
    return string.IsNullOrWhiteSpace(envPath) ? "agent-foreman.yaml" : envPath;
}

public partial class Program;
