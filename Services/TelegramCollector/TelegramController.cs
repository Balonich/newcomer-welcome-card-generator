using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace TelegramCollector;

[Route("api/[controller]")]
[ApiController]
public class TelegramController : ControllerBase
{
    private readonly TelegramDataCollector _telegramDataCollector;
    private readonly ILogger<TelegramController> _logger;
    private readonly TelegramSettings _settings;

    public TelegramController(
        TelegramDataCollector telegramDataCollector,
        IOptions<TelegramSettings> settings,
        ILogger<TelegramController> logger)
    {
        _telegramDataCollector = telegramDataCollector;
        _logger = logger;
        _settings = settings.Value;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Post(
        [FromBody] Update update,
        [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string secretToken)
    {
        // Validate the secret token
        if (!string.IsNullOrEmpty(_settings.SecretToken) && secretToken != _settings.SecretToken)
        {
            _logger.LogWarning("Invalid secret token received");
            return Unauthorized();
        }

        await _telegramDataCollector.HandleUpdateAsync(update);
        return Ok();
    }
}