using MatchMaking.Interfaces;
using Matchmaking.Models;
using Matchmaking.Repositories;
using Matchmaking.Services;
using MatchMaking.Workers;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect($"{redisSettings.Host}:{redisSettings.Port}")
);

builder.Services.AddHostedService<MatcherWorker>(sp =>
{
    var redisDb = sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
    var matchmakingService = sp.GetRequiredService<MatchmakingService>();

    return new MatcherWorker(
        redisDb,
        sp.GetRequiredService<IOptions<RedisSettings>>(),
        matchmakingService.ProcessFindMatch
    );
});

builder.Services.AddSingleton<IPlayerRepository, PlayerRepository>();
builder.Services.AddSingleton<MatchmakingService>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())

{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();