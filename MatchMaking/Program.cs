using MatchMaking.Interfaces;
using Matchmaking.Models;
using Matchmaking.Repositories;
using Matchmaking.Services;
using MatchMaking.Workers;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<MatchmakingConfig>(builder.Configuration.GetSection("Matchmaking"));

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