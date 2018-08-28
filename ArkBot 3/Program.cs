using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using Newtonsoft.Json;
using System.IO;

namespace ArkBot_3
{
    class Program
    {
        public static DiscordClient discord; //Client used to connect
        static string loginToken = ""; //Login token for the bot
        public static string PrefixString = "%"; //Prefix

        public static List<ArkServer> setupInProgressServers = new List<ArkServer>();
        public static List<ArkServer> activeServers = new List<ArkServer>();

        public const int ApplicationVersion = 0;
        public static string savePath = @"C:\Users\Roman\source\repos\New ArkBot\save\"; //Change this

        public static List<PendingArkLink> pendingArkLinks = new List<PendingArkLink>();

        public static ulong ownerId;

        public static float arkPointMult = 1;
        public static int arkUpdateTimeMs = 3000; //How quickly chat will be updated.

        public static bool shutUp = true;
        static Dictionary<ulong, DateTime> nou =
           new Dictionary<ulong, DateTime>();

        static void Main(string[] args)
        {
            //Load token
            loginToken = File.ReadAllText(savePath + "discord.token");
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult(); //Begin
        }



        static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Starting...");
            
            //Load servers. Do a directory listing.
            string[] files = Directory.GetFiles(savePath + "saves\\");
            foreach (string path in files)
            {
                await Program.LoadArkServer(File.ReadAllText(path));
            }

            //First, set up the config.
            DiscordConfiguration dc = new DiscordConfiguration
            {
                Token = loginToken,
                TokenType = TokenType.Bot
            };
            discord = new DiscordClient(dc); //Create client
            discord.Ready += async e =>
            {
                //Fired on connect
                await DiscordUser.AskForOwner();
                Console.WriteLine("Ready!");
            };

            discord.MessageCreated += async e =>
            {


                //Fired on message.
                try {
                    await OnMessage(e);
                } catch(Exception ex)
                {
                    //Throw
                    try
                    {
                        var embed = Tools.DumpEmbedException(ex);
                        await e.Message.RespondAsync(embed: embed);
                    } catch
                    {
                        //Too long!
                        try
                        {
                            var embeds = Tools.DumpMultipleEmbedException(ex);
                            await e.Message.RespondAsync(embed: embeds[0]);
                            await e.Message.RespondAsync(embed: embeds[1]);
                        } catch
                        {
                            //What
                            try
                            {
                                var embed = Tools.GenerateEmbed("Fatal Error! (Uncaught)", ex.StackTrace, ex.Message, DSharpPlus.Entities.DiscordColor.Red);
                                await e.Message.RespondAsync(embed: embed);
                            } catch
                            {
                                //This is getting rediculous
                                Console.WriteLine("An exception was thrown that couldn't be put into Discord.");
                            }
                        }
                    }
                }
            };

            //Actually connect, then hang this async task.
            try
            {
                await discord.ConnectAsync();
            }
            catch
            {
                Console.WriteLine("Uh oh, the bot failed to connect. Is the token correct? ");
            }

            await Task.Delay(-1);
        }

        public static async Task LoadArkServer(string serializedData)
        {
            //Deserialize
            ArkServer server = (ArkServer)Tools.DeserializeObject(serializedData, typeof(ArkServer));
            //Add to list.
            activeServers.Add(server);
            //Init
            await server.Reinit();
        }

        static async Task OnMessage(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            //Stop if this is myself.
            if (e.Author.IsCurrent || e.Author.IsBot)
                return;
            //If this is a dm, use the other function.
            if (e.Guild == null)
            {
                //DM
                await OnDmMessage(e);
                return;
            }
            //First, authorize the user.
            ArkServer selectedServer = null;
            if (activeServers.Count>0)
                selectedServer = activeServers[0];
            DiscordUser user = await DiscordUser.GetUserFromDiscord(e,selectedServer);
            if(e.Message.Content.ToLower().StartsWith(PrefixString+"rcon "))
            {
                await LegacyArkRconConnection.DiscordRawRconCommand(e, user, selectedServer);
            }
            if (e.Message.Content.ToLower().StartsWith(PrefixString + "help"))
            {
                await user.ShowPersonalHelp(e,selectedServer);
            }
            if (e.Message.Content.ToLower().StartsWith(PrefixString + "role"))
            {
                await e.Message.RespondAsync("You're in role **" + user.permissionLevel.ToString() + "**.");
            }
            if (e.Message.Content.ToLower().StartsWith(PrefixString + "link"))
            {
                await user.BeginArkLink(e);
            }
            if (e.Message.Content.ToLower().StartsWith(PrefixString + "stats"))
            {
                await user.SendUserStatus(e);
            }
            if (e.Message.Content.ToLower().StartsWith(PrefixString + "chat"))
            {
                string msg = e.Message.Content.Substring((PrefixString + "chat ").Length);
                if(Tools.WhitelistSafeText(msg,out string badChars))
                {
                    //Check length
                    if(msg.Length>800)
                    {
                        //Bad.
                        var embed = Tools.GenerateEmbed("Too Long!", "Dang it, " + e.Author.Username, "Your chat message is "+(msg.Length-800).ToString()+" characters over the limit of 800!", DSharpPlus.Entities.DiscordColor.Orange);
                        await e.Message.RespondAsync(embed: embed);
                    } else
                    {
                        //Okay to send
                        await selectedServer.SendArkMessage(msg, e.Author.Username);
                        var emoji = DSharpPlus.Entities.DiscordEmoji.FromName(discord,":white_check_mark:");
                        e.Message.CreateReactionAsync(emoji);
                    }
                    
                } else
                {
                    //Bad.
                    var embed = Tools.GenerateEmbed("Illegal Character!", "Dang it, " + e.Author.Username, "The character '" + badChars + "' isn't allowed.", DSharpPlus.Entities.DiscordColor.Orange);
                    await e.Message.RespondAsync(embed: embed);
                }
                
            }
            if (e.Message.Content.ToLower().StartsWith(PrefixString + "list") && !e.Message.Content.ToLower().StartsWith(PrefixString + "listbackup"))
            {
                await selectedServer.ListPlayersCmd(e);
            }
            if (e.Message.Content.ToLower().StartsWith(PrefixString + "backup"))
            {
                await selectedServer.arkIO.BackupServerDiscord(e, @"C:\Program Files (x86)\Steam\steamapps\common\ARK\ShooterGame\Saved\SavedArks\", @"D:\ArkBackups\");
            }

            if (e.Message.Content.ToLower().StartsWith(PrefixString + "listbackup"))
            {
                await selectedServer.arkIO.ListBackupsDiscord(e,  @"D:\ArkBackups\");
            }

            //Save and close user
            user.CloseUser();
        }

        static async Task OnDmMessage(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            //Assume this is for creation
            DiscordUser user = await DiscordUser.GetUserFromDiscord(e);
            await ArkServer.MessageSentToFinshSetup(e, user);
            /*//First, check if this user is in the process of creating an Ark server
            ArkServer[] userCreatingServers = ArkServer.GetArkServersInSetupStageForUser(e.Author.Id);
            if (userCreatingServers.Length == 1)
            {
                //User is creating a server. Go through with the setup of it.
                await userCreatingServers[0].MessageSentDuringSetup(e);
                return;
            }*/
        }

        public static DSharpPlus.Entities.DiscordColor CreateDynamicColor()
        {
            return DSharpPlus.Entities.DiscordColor.Magenta;
        }
    }
}
