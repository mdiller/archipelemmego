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

            //var roomInfo = await ArchipelagoService.RegisterRoomInfo("archipelago.gg", 60164);
            //Console.WriteLine(roomInfo.Seed);

            //// await ArchipelagoService.RegisterSlotInfo(roomInfo, "Malcolm1");
            //await ArchipelagoService.RegisterSlotInfo(roomInfo, "Malcolm2");
            //await ArchipelagoService.RegisterSlotInfo(roomInfo, "Malcolm3");


            var token = BotInfo.BotToken;
            var botManager = new BotManager();
            await botManager.Startup(token);
        }
    }
}
