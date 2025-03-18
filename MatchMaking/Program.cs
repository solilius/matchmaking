using Matchmaking.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Bind config
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));

// Load Redis settings
var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();

// Register Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect($"{redisSettings.Host}:{redisSettings.Port}")
);

builder.Services.AddScoped<Matchmaking.Services.MatchmakingService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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