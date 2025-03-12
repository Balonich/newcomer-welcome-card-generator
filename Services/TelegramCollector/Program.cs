using Common.Interfaces.Messaging;
using Common.Messaging.RabbitMQ;
using Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Telegram.Bot;
using TelegramCollector;

var builder = WebApplication.CreateBuilder(args);

// Add controller services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // These are good defaults for handling JSON properties in Telegram Bot API
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Add settings
builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<RabbitMQSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

// Register Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<TelegramSettings>>().Value;
    return new TelegramBotClient(settings.BotToken);
});

// Register CardGenerationQueueProducer
builder.Services.AddSingleton<IMessageProducer<NewcomerData>, CardGenerationQueueProducer>();

// Register TelegramDataCollector
builder.Services.AddSingleton<TelegramDataCollector>();

builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddOpenApi(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "Telegram Collector API", Version = "v1" });
// });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Start the Telegram collector service
var telegramDataCollector = app.Services.GetRequiredService<TelegramDataCollector>();
await telegramDataCollector.StartAsync();

// Handle application shutdown
app.Lifetime.ApplicationStopping.Register(async () =>
{
    await telegramDataCollector.StopAsync();
});

app.Run();