using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using IResult = Discord.Interactions.IResult;

namespace ArchipeLemmeGo.Bot
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;

            _client.Ready += OnReady;
            _client.InteractionCreated += HandleInteraction;
        }

        private async Task OnReady()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _commands.RegisterCommandsGloballyAsync();
            await _commands.RegisterCommandsToGuildAsync(BotInfo.TestGuildId); // per-guild register for testing
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _commands.ExecuteCommandAsync(ctx, _services);
        }

        public async Task InitializeAsync()
        {
            // Placeholder if you want to expand later
        }

        // InteractionHandler.cs
        public async Task HandleInteractionExecutedAsync(ICommandInfo command, IInteractionContext context, IResult result)
        {
            if (result.IsSuccess)
                return;

            if (result.Error == InteractionCommandError.Exception && result is ExecuteResult execResult)
            {
                var ex = execResult.Exception;
                var cause = ex.InnerException ?? ex;

                string messageToSend;
                if (cause is UserError userError)
                {
                    messageToSend = userError.Message;
                }
                else
                {
                    Console.WriteLine($"[Error] Unhandled exception in command '{command.Name}':");
                    Console.WriteLine(ex.ToString());
                    messageToSend = $"EXCEPTION OCCURRED OH NO:\n```\n{cause.GetType().FullName}:\n{cause.Message}\n```";
                }

                if (context.Interaction.HasResponded)
                    await context.Interaction.FollowupAsync(messageToSend);
                else
                    await context.Interaction.RespondAsync(messageToSend);
            }
        }
    }

}
