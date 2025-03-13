using Common.BaseClasses.DataCollection;
using Common.Interfaces.Messaging;
using Common.Models;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramCollector.Models;

namespace TelegramCollector.Services.DataCollector;

public class TelegramDataCollector : BaseDataCollector
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramDataCollector> _logger;
    private readonly TelegramSettings _settings;
    private CancellationTokenSource? _pollingCts;

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

        // Check if webhook URL is provided
        if (!string.IsNullOrEmpty(_settings.WebhookUrl) && _settings.WebhookUrl.ToLower() != "null")
        {
            await SetWebhookAsync();
        }
        else
        {
            await StartLongPollingAsync();
        }
    }

    public override async Task StopAsync()
    {
        await base.StopAsync();

        // Stop long polling if active
        if (_pollingCts != null)
        {
            await StopLongPollingAsync();
        }
        else
        {
            await RemoveWebhookAsync();
        }
    }

    // Handler for updates from both webhook and long polling
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken = default)
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

            var messageType = CheckMessageType(messageText);

            switch (messageType)
            {
                case TelegramMessageTypes.Newcomer:
                    await ProcessNewcomerDataAsync(message, cancellationToken);
                    break;

                case TelegramMessageTypes.Help:
                    SendHelpCommandMessageAsync();
                    break;

                case TelegramMessageTypes.Start:
                    SendStartCommandMessageAsync();
                    break;

                case TelegramMessageTypes.Unknown:
                    SendUnknownCommandMessageAsync();
                    break;

                default:
                    SendUnknownCommandMessageAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Telegram update");
        }
    }

    private async Task SetWebhookAsync()
    {
        _logger.LogInformation("Setting webhook to: {WebhookUrl}", _settings.WebhookUrl);
        await _botClient.SetWebhook(
            url: _settings.WebhookUrl,
            secretToken: _settings.SecretToken,
            allowedUpdates: Array.Empty<UpdateType>());
    }

    private async Task RemoveWebhookAsync()
    {
        await _botClient.DeleteWebhook();
        _logger.LogInformation("Webhook removed");
    }

    private async Task StartLongPollingAsync()
    {
        _logger.LogInformation("Using long polling mode");
        await RemoveWebhookAsync(); // Make sure webhook is disabled

        // Start long polling
        _pollingCts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(), // receive all update types
            DropPendingUpdates = true // ignore older updates
        };

        // Start receiving updates
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _pollingCts.Token
        );

        _logger.LogInformation("Started long polling for Telegram updates");
    }

    private async Task StopLongPollingAsync()
    {
        _pollingCts.Cancel();
        _pollingCts.Dispose();
        _pollingCts = null;
        _logger.LogInformation("Stopped long polling");
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error while polling for Telegram updates");

        // Delay before retrying to avoid excessive retries on persistent errors
        Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).Wait(cancellationToken);

        return Task.CompletedTask;
    }

    private TelegramMessageTypes CheckMessageType(string messageText)
    {
        // Check if this is a newcomer command
        if (text.StartsWith("/newcomer", StringComparison.OrdinalIgnoreCase))
        {
            return TelegramMessageTypes.Newcomer;
        }
        else if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            return TelegramMessageTypes.Help;
        }
        else if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            return TelegramMessageTypes.Start;
        }
        else
        {
            return TelegramMessageTypes.Unknown;
        }
    }

    private async Task ProcessNewcomerDataAsync(Message message, CancellationToken cancellationToken = default)
    {
        var newcomerData = await ParseNewcomerDataAsync(message, cancellationToken);

        if (newcomerData != null)
        {
            // Publish to queue
            await PublishNewcomerDataAsync(newcomerData);

            // Send confirmation
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Thank you! The newcomer welcome card is being generated.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task<NewcomerData?> ParseNewcomerDataAsync(Message message, CancellationToken cancellationToken = default)
    {
        // This implementation assumes the message follows a specific format or command
        // For example: /newcomer format or a structured message

        var text = message.Text!;

        // Simple parsing - in a real scenario, this would be more robust
        // Expected format: /newcomer Name|Position|Department|Bio|PhotoUrl|Hobby1,Hobby2
        var parts = text.Substring("/newcomer".Length).Trim().Split('|');

        if (parts.Length < 5)
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Invalid format. Please use: /newcomer Name|Position|Department|Bio|PhotoUrl|Hobby1,Hobby2",
                cancellationToken: cancellationToken);
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

    private async Task SendHelpCommandMessageAsync(Message message)
    {
        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Welcome to the Newcomer Welcome Card Generator!\n\n" +
                "Use the /newcomer command with the following format:\n" +
                "/newcomer Name|Position|Department|Bio|PhotoUrl|Hobby1,Hobby2\n\n" +
                "Example:\n" +
                "/newcomer John Doe|Software Engineer|Engineering|I love coding!|https://example.com/photo.jpg|Coding,Gaming,Hiking",
            cancellationToken: cancellationToken);
    }

    private async Task SendStartCommandMessageAsync(Message message)
    {
        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Welcome! I'm the Newcomer Welcome Card Generator bot.\n" +
                  "Use /help to see available commands.",
            cancellationToken: cancellationToken);
    }

    private async Task SendUnknownCommandMessageAsync(Message message)
    {
        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Unknown command.",
            cancellationToken: cancellationToken);
    }
}