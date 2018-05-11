using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using RomansRconClient;

namespace ArkBot_3
{
    class ArkRconConnection
    {
        //Works with RCON stuff
        public static async Task<ArkRconConnection> ConnectToRcon(string ip, ushort port, string arkPassword)
        {
            var rcon = RconConnection.ConnectToRcon(ip, port, arkPassword);
            var c = new ArkRconConnection(rcon, ip, port, arkPassword); ;
            c.isConnected = rcon.status == RconConnectionStatus.Connected;
            return c;
        }

        //Class stuff
        RconConnection rconClient;
        public bool isConnected;
        public string failMsg;
        public string ip;
        public ushort port;
        public string arkPassword;

        public ArkRconConnection(RconConnection client, string _ip, ushort _port, string _arkPassword)
        {
            //Main constructor.
            rconClient = client;
            isConnected = true;
            ip = _ip;
            port = _port;
            arkPassword = _arkPassword;
        }
        public ArkRconConnection(string msg)
        {
            //Failed constructor.
            failMsg = msg;
            isConnected = false;
        }

        public async Task<string> RunCommand(string msg)
        {
            var response = rconClient.SendCommand(msg);
            if(response.status != RconResponseStatus.Ok)
            {
                //Try to reconnect.
                Console.WriteLine("Connection send error.");
                await ForceFullReconnect();
                response = rconClient.SendCommand(msg);
                if (response.status != RconResponseStatus.Ok)
                {
                    throw new Exception("Failed to run RCON command; The status was not Ok. Use RunRCON function instead to avoid this error.");
                } else
                {
                    //Fixed
                    Console.WriteLine("Reconnected from error.");
                    return response.body;
                }
                    
            }
            return response.body;
        }

        public async Task ForceFullReconnect()
        {
            var obj = await ConnectToRcon(ip, port, arkPassword);
            rconClient = obj.rconClient;
        }

        public async Task<RomansRconClient.RconResponse> RunRCON(string msg)
        {
            //A more advanced version of RunCommand
            return rconClient.SendCommand(msg);
        }

        public void Dispose()
        {
        }

        public async Task DiscordRawRconCommand(DSharpPlus.EventArgs.MessageCreateEventArgs e, DiscordUser user)
        {
            if(user.permissionLevel == DiscordUserPermissionLevel.owner)
            {
                //Ok to run.
                await e.Message.RespondAsync(await RunCommand(e.Message.Content.Substring((Program.PrefixString + "rcon").Length)));
            } else
            {
                //Cannot run becasue of permission level.
                await e.Message.RespondAsync("ERROR - Cannot run raw command because you're not the bot owner!");
            }
        }
    }
}
