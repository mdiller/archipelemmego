using ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Archipelago.ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Bot;

namespace ArchipeLemmeGo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");


            var token = BotInfo.BotToken;
            var botManager = new BotManager();
            await botManager.Startup(token);

            //var tree = TreeRenderer.MakeSample(); // or build your own using AddChild(...)
            //TreeRenderer.Render(tree, 900, 600, "my_tree.png");
        }
    }
}
