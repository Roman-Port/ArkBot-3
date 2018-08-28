using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using RomansRconClient2;

namespace ArkBot_3
{
    
    class LegacyArkRconConnection
    {
        //Works with RCON stuff
        /*
        public static async Task<ArkRconConnection> ConnectToRcon(string ip, ushort port, string arkPassword)
        {
            ArkRconConnection c = null;
            bool ready = false;
            var rcon = new RconClient(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), port), arkPassword, new RconClient.ReadyCallback((RconClient context, bool okay) =>
            {
                ready = true;
                
            }));
            c = new ArkRconConnection(rcon, ip, port, arkPassword);
            while (ready == false) ;
            c.isReady = ready;
            Console.WriteLine("Sock ready");
            return c;
        }

        public static async Task<ArkRconConnection> ConnectToRcon(string ip, int port, string arkPassword)
        {
            return await ConnectToRcon(ip, ushort.Parse(port.ToString()), arkPassword);
        }

        //Class stuff
        RconClient rconClient;
        public bool isReady = false;
        public string failMsg;
        public string ip;
        public ushort port;
        public string arkPassword;

        public ArkRconConnection(RconClient client, string _ip, ushort _port, string _arkPassword)
        {
            //Main constructor.
            rconClient = client;
            ip = _ip;
            port = _port;
            arkPassword = _arkPassword;
        }

        public string RunCommand(string msg)
        {
            //Wait to be ready
            Console.WriteLine("what");
            string response = rconClient.GetResponse(msg);
            Console.WriteLine(response);
            return response;
        }*/

        public static async Task DiscordRawRconCommand(DSharpPlus.EventArgs.MessageCreateEventArgs e, DiscordUser user, ArkServer server)
        {
            if(user.permissionLevel == DiscordUserPermissionLevel.owner)
            {
                //Ok to run.
                await e.Message.RespondAsync(server.rconConnection.GetResponse(e.Message.Content.Substring((Program.PrefixString + "rcon ").Length)));
            } else
            {
                //Cannot run becasue of permission level.
                await e.Message.RespondAsync("ERROR - Cannot run raw command because you're not the bot owner!");
            }
        }
    }
}
