using Common.BaseClasses.DataCollection;
using Common.Interfaces.Messaging;
using Common.Models;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramCollector;

public class TelegramDataCollector : BaseDataCollector
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramDataCollector> _logger;
    private readonly TelegramSettings _settings;

    public TelegramDataCollector(
        IMessageProducer<NewcomerData> messageProducer,
        ITelegramBotClient botClient,
        IOptions<TelegramSettings> settings,
        ILogger<TelegramDataCollector> logger) : base(messageProducer)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public override async Task StartAsync()
    {
        await base.StartAsync();

        // Set webhook only if URL is provided
        if (!string.IsNullOrEmpty(_settings.WebhookUrl))
        {
            _logger.LogInformation("Setting webhook to: {WebhookUrl}", _settings.WebhookUrl);
            await _botClient.SetWebhook(
                url: _settings.WebhookUrl,
                secretToken: _settings.SecretToken,
                allowedUpdates: Array.Empty<UpdateType>());
        }
        else
        {
            _logger.LogWarning("Webhook URL not provided. Webhook not set.");
        }
    }

    public override async Task StopAsync()
    {
        await base.StopAsync();
        await _botClient.DeleteWebhook();
        _logger.LogInformation("Webhook removed");
    }

    public async Task HandleUpdateAsync(Update update)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Update received but collector is not running");
            return;
        }

        try
        {
            // Check if the update has a message
            if (update.Message is not { } message)
            {
                _logger.LogDebug("Received update without message");
                return;
            }

            // Only process text messages
            if (message.Text is not { } messageText)
            {
                _logger.LogDebug("Received message without text");
                return;
            }

            _logger.LogInformation("Received message: {MessageText}", messageText);

            // Process the message to extract newcomer data
            var newcomerData = await ParseNewcomerDataAsync(message);

            if (newcomerData != null)
            {
                // Publish to queue
                await PublishNewcomerDataAsync(newcomerData);

                // Send confirmation
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Thank you! The newcomer welcome card is being generated.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Telegram update");
        }
    }

    private async Task<NewcomerData?> ParseNewcomerDataAsync(Message message)
    {
        // This implementation assumes the message follows a specific format or command
        // For example: /newcomer format or a structured message

        var text = message.Text!;

        // Check if this is a newcomer command
        if (text.StartsWith("/newcomer", StringComparison.OrdinalIgnoreCase))
        {
            // Simple parsing - in a real scenario, this would be more robust
            // Expected format: /newcomer Name|Position|Department|Bio|PhotoUrl|Hobby1,Hobby2
            var parts = text.Substring("/newcomer".Length).Trim().Split('|');

            if (parts.Length < 5)
            {
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Invalid format. Please use: /newcomer Name|Position|Department|Bio|PhotoUrl|Hobby1,Hobby2");
                return null;
            }

            var newcomer = new NewcomerData
            {
                FullName = parts[0].Trim(),
                Position = parts[1].Trim(),
                Department = parts[2].Trim(),
                Bio = parts[3].Trim(),
                PhotoUrl = parts[4].Trim(),
                Hobbies = parts.Length > 5 ? parts[5].Split(',').Select(h => h.Trim()).ToArray() : Array.Empty<string>(),
                JoiningDate = DateTime.UtcNow
            };

            return newcomer;
        }

        // Not a newcomer command
        return null;
    }
}