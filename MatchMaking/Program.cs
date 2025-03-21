using MatchMaking.Interfaces;
using Matchmaking.Models;
using Matchmaking.Repositories;
using Matchmaking.Services;
using MatchMaking.Tests;
using MatchMaking.Workers;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<MatchmakingConfig>(builder.Configuration.GetSection("Matchmaking"));

var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = new ConfigurationOptions
    {
        EndPoints = { $"{redisSettings.Host}:{redisSettings.Port}" },
        AbortOnConnectFail = false,
        ConnectTimeout = 15000,
        SyncTimeout = 15000,
        KeepAlive = 60,
        DefaultDatabase = 0,
        AllowAdmin = true,
    };
    
    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddHostedService<MatcherWorker>(sp =>
{
    var redisDb = sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
    var matchmakingService = sp.GetRequiredService<MatchmakingService>();
    
    RedisSeeder.Seed(redisDb);

    return new MatcherWorker(
        redisDb,
        sp.GetRequiredService<IOptions<RedisSettings>>().Value.RedisKeys.QueueKey,
        sp.GetRequiredService<IOptions<MatchmakingConfig>>().Value.ProcessBatchSize,
        matchmakingService.ProcessFindMatch
    );
});


builder.Services.AddSingleton<IRepository<Player>, PlayerRepository>();
builder.Services.AddSingleton<IRepository<Match>, MatchRepository>();
builder.Services.AddSingleton<MatchmakingService>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.WebHost.UseUrls("http://0.0.0.0:8080");

var app = builder.Build();

if (app.Environment.IsDevelopment())

{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
