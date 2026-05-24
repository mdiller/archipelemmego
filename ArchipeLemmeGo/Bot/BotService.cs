using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchipeLemmeGo.Bot
{
    public class BotService : BackgroundService
    {
        private readonly ILogger<BotService> _logger;

        public BotService(ILogger<BotService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Discord bot service starting");
            var botManager = new BotManager();
            await botManager.Startup(BotInfo.BotToken, stoppingToken);
        }
    }
}
