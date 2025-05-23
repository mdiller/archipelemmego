using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace ArchipeLemmeGo.Bot
{
    /// <summary>
    /// The manager for the discord bot
    /// </summary>
    public class BotManager
    {
        public async Task Startup(string token)
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages,
                AlwaysDownloadUsers = true,
                UseInteractionSnowflakeDate = false
            };

            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(config))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>() // we'll define this
                .BuildServiceProvider();

            var client = services.GetRequiredService<DiscordSocketClient>();
            var interactions = services.GetRequiredService<InteractionService>();
            var interactionHandler = services.GetRequiredService<InteractionHandler>();

            client.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };
            interactions.InteractionExecuted += interactionHandler.HandleInteractionExecutedAsync;

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await services.GetRequiredService<InteractionHandler>().InitializeAsync();

            await Task.Delay(-1);
        }
    }
}
