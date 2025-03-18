using MatchMaking.Interfaces;
using Matchmaking.Models;
using Matchmaking.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect($"{redisSettings.Host}:{redisSettings.Port}")
);

builder.Services.AddSingleton<IPlayerRepository, PlayerRepository>();
builder.Services.AddSingleton<Matchmaking.Services.MatchmakingService>();

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