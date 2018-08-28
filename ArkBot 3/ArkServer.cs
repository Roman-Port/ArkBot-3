using RomansRconClient2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ArkBot_3
{

    public enum ArkServerProcessStatus
    {
        Offline,
        Unknown,
        Online,
        Starting
    }
    [DataContract]
    class ArkServer
    {
        //Setup stuff
        public ArkServerCreationStep creationProgress;
        public ulong creatorId;
        public long creationStartedDate;

        //Produciton stuff
        [DataMember]
        public bool isRconEnabled;
        [DataMember]
        public DiscordUserPermissionLevel rconChatPermissionLevel;
        [DataMember]
        public DiscordUserPermissionLevel serverStartStopPermissionLevel;
        [DataMember]
        public DiscordUserPermissionLevel backupPermissionLevel;
        [DataMember]
        public string rconPassword;
        [DataMember]
        public string rconIp;
        [DataMember]
        public string name;
        [DataMember]
        public ulong notificationChannel;
        [DataMember]
        public int saveVersion;
        [DataMember]
        public string serverUuid;
        [DataMember]
        public int rconPort; //I don't think this is ever assigned to. You sdhould fix that.
        [DataMember]
        public ulong adminRoleId;
        [DataMember]
        public ulong userRoleId;
        [DataMember]
        public string arkServerPath;
        [DataMember]
        public string arkServerArgs;
        [DataMember]
        public string arkSavePath;
        [DataMember]
        public string backupLocation;

        //While active stuff
        public System.Timers.Timer timer; //Used for updates
        public string[] userlistCache = new string[0];
        public ArkServerProcessStatus processStatus = ArkServerProcessStatus.Unknown;
        public Process process = null;
        public DateTime processStartTime = DateTime.UtcNow;
        public ArkIOInterface arkIO;
        public RconClient rconConnection;

        //Functions
        //Util
        /*
        public async Task Cmd_StartServer(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            var msg = await e.Message.RespondAsync(embed: Tools.GenerateEmbed("Server Starting...", "", "The server \"" + name + "\" is starting...", DSharpPlus.Entities.DiscordColor.Yellow));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;


                //Begin startup
                //StartServer();
                //Keep attempting to connect.
                DateTime start = DateTime.UtcNow;
                if (isRconEnabled)
                {
                    //Rcon will connect.
                    while (true)
                    {
                        //Try to connect.
                        var rcon = ArkRconConnection.ConnectToRcon(rconIp, ushort.Parse(rconPort.ToString()), rconPassword).ConfigureAwait(false).GetAwaiter().GetResult();
                        rconConnection = rcon;
                        if (rcon.isReady)
                        {
                            //Connected
                            var embed = Tools.GenerateEmbed("Server Started!", "", "The server is ready to use and join!", DSharpPlus.Entities.DiscordColor.Green);
                            processStatus = ArkServerProcessStatus.Online;
                            msg.ModifyAsync(embed: embed).ConfigureAwait(false).GetAwaiter().GetResult();
                            break;
                        }
                        TimeSpan time = DateTime.UtcNow - start;
                        if (time.TotalSeconds > 120)
                        {
                            //Timeout.
                            processStatus = ArkServerProcessStatus.Unknown;
                            var embed = Tools.GenerateEmbed("Server Timeout", "Timeout time is 120s.", "The connection timed out. The server could be up, but it is unknown.", DSharpPlus.Entities.DiscordColor.Red);
                            msg.ModifyAsync(embed: embed).ConfigureAwait(false).GetAwaiter().GetResult();
                            break;
                        }
                        Thread.Sleep(5000);
                    }
                }
                else
                {

                }
            }).Start();
        }*/

        public async Task<Entity_ArkMessage[]> FetchMessages(string rawBuffer = "na")
        {
            //If RCON is off or not connected, return a blank array.
            if(isRconEnabled==false)
            {
                return new Entity_ArkMessage[0];
            }
            //Run RCON command if rawbuffer wasn't passed.
            if(rawBuffer=="na")
            {
                //Fetch
                rawBuffer = await RunRCONCommand("GetChat");
            }
            if(rawBuffer=="error")
            {
                //Error. Stop
                return new Entity_ArkMessage[0];
            }
            //Parse
            Entity_ArkMessage[] messageBuffer = Entity_ArkMessage.ParseMultipleMessages(rawBuffer);
            return messageBuffer;
        }

        private long LastError = 0;

        private async Task Update()
        {
            //Called every second
            try
            {
                ArkLogMsg[] logs = await UpdateGameLog();
            } catch (Exception ex)
            {
                //Check we've errored recently.
                DateTime lastError = new DateTime(LastError);
                TimeSpan timeSinceError = DateTime.UtcNow - lastError;
                if(timeSinceError.TotalSeconds>60)
                {
                    //Throw error on Discord
                    LastError = DateTime.UtcNow.Ticks;
                    try
                    {
                        var channel = await Program.discord.GetChannelAsync(notificationChannel);
                        var embed = Tools.GenerateEmbed("Error", ex.Message, "Chat, log, and player status couldn't be updated to place here. Trying again in 3 seconds... (you won't see an error again for another 60 seconds)", DSharpPlus.Entities.DiscordColor.DarkRed);
                        await channel.SendMessageAsync(embed: embed);
                    } catch
                    {
                        Console.WriteLine("Error in update function. The message couldn't even be sent to Discord.");
                    }
                }
            }
        }


        public async Task<string> RunRCONCommand(string msg)
        {
            string output = "";
            if (rconConnection == null)
            {
                throw new Exception("Not connected");
            }
            else
            {
                output = rconConnection.GetResponse(msg);
            }
            return output;
        }
        
        public async Task<int> GetAndSendNewChat()
        {
            //Return number of messages.
            string rawChats = await RunRCONCommand("getchat");
            //Check if we got any
            if(rawChats.ToLower().Contains("server received, but no response!!"))
            {
                //No chats. Return zero.
                return 0;
            }
            //Parse
            Entity_ArkMessage[] messages = Entity_ArkMessage.ParseMultipleMessages(rawChats);
            //Check if we're verifying an ArkLink
            foreach(Entity_ArkMessage message in messages)
            {
                //Check if it matches
                foreach(var e in Program.pendingArkLinks)
                {
                    if(message.content.Contains(e.randId))
                    {
                        //Matches!
                        await DiscordUser.FinishArkLink(message.username, message.steamName,e.msg,e.user.id);
                    }
                }
            }
            //Fetch Discord server
            //Todo: Catch this and alert the owner
            var server = await Program.discord.GetChannelAsync(notificationChannel);
            //Loop through
            foreach (Entity_ArkMessage message in messages)
            {
                //Create an embed
                var embed = Tools.GenerateEmbed("Ark Message from " + message.username, "Steam name: "+message.steamName+ "- Ark server: " + name, message.content, DSharpPlus.Entities.DiscordColor.LightGray);
                //Send this embed to the desired channel
                await server.SendMessageAsync(embed: embed);
            }
            return messages.Length;
        }

        public async Task SendArkMessage(string msg, string username)
        {
            await RunRCONCommand("ServerChat ["+username+"] " + msg); 
        }

        public async Task<string[]> FetchPlayerList()
        {
            //Run commands
            string raw = await RunRCONCommand("ListPlayers");
            if(raw.ToLower().Contains("no players connected"))
            {
                return new string[0];
            }
            string[] list = raw.Split('\n');
            List<string> output = new List<string>();
            foreach(string n in list)
            {
                Regex r = new Regex(@"([A-z])\w+");
                var split = r.Matches(n);
                foreach(var s in split)
                {
                    string ss = s.ToString();
                    if (ss.Length > 2)
                        output.Add(ss);
                }
            }
            return output.ToArray();
        }

        public async Task ListPlayersCmd(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            //Run the RCON command.
            string[] playerList = await FetchPlayerList();
            string outputList = "";
            foreach(string player in playerList)
            {
                outputList += player + "\r\n";
            }
            //Generate
            var embed = Tools.GenerateEmbed("Player Listing", playerList.Length.ToString() + " players total", outputList, DSharpPlus.Entities.DiscordColor.Magenta);
            //Respond
            await e.Message.RespondAsync(embed: embed);
        }

        public async Task<Entity_ArkPlayer[]> GetAndSendPlayerJoinLeaves()
        {
            try
            {
                if(userlistCache==null)
                    userlistCache = new string[0];

                List<string> playersStringList = new List<string>();

                string[] leavingPlayers = new string[0];
                string[] joiningPlayers = new string[0];
                
                List<Entity_ArkPlayer> nowPlayersList = new List<Entity_ArkPlayer>();
                List<Entity_ArkPlayer> playersList = new List<Entity_ArkPlayer>();

                //Parse
                foreach(string s in await FetchPlayerList())
                {
                    var player = Entity_ArkPlayer.ParsePlayerList(s, Entity_ArkPlayerStatus.Joined);
                    nowPlayersList.Add(player);
                    playersStringList.Add(player.name);
                }

                //Convert to array
                string[] players = playersStringList.ToArray();

                //Compare
                leavingPlayers = userlistCache.Except(players).ToArray(); //Find differences
                joiningPlayers = players.Except(userlistCache).ToArray(); //Find differences

                foreach (var n in leavingPlayers)
                {
                    //Parse
                    Entity_ArkPlayer player = new Entity_ArkPlayer();
                    player.status = Entity_ArkPlayerStatus.Leaving;
                    player.name = n;
                    playersList.Add(player);
                }

                foreach (var n in joiningPlayers)
                {
                    //Parse
                    Entity_ArkPlayer player = new Entity_ArkPlayer();
                    player.status = Entity_ArkPlayerStatus.Joining;
                    player.name = n;
                    playersList.Add(player);
                }
                //Reset cache
                userlistCache = players;

                if(playersList.Count>0)
                {
                    //Changes were made. Write them.
                    var server = await Program.discord.GetChannelAsync(notificationChannel); //Get the channel
                    foreach(var n in playersList)
                    {
                        //Default to leaving.
                        DSharpPlus.Entities.DiscordColor color = DSharpPlus.Entities.DiscordColor.Red;
                        string msg = "left";
                        if(n.status== Entity_ArkPlayerStatus.Joining)
                        {
                            //Change to joining
                            color = DSharpPlus.Entities.DiscordColor.Green;
                            msg = "joined";
                        }
                        //Create a message and write
                        var embed = Tools.GenerateEmbed(n.name + " " + msg + " the Ark server!", "Just now", "Server " + name, color);
                        //Write
                        await server.SendMessageAsync(embed: embed);
                    }
                }
                return nowPlayersList.ToArray();
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        private string[] LogLinesCache = new string[0];

        public async Task<string[]> FetchNewLogData()
        {
            //Check if the cache is blank
            if (LogLinesCache == null)
                LogLinesCache = new string[0];

            string raw = await RunRCONCommand("GetGameLog");
            if (raw.Contains("Server received, But no response!!"))
                return new string[0];
            //Split
            string[] rawLines = Tools.SplitRconByLine(raw);
            var newLines = rawLines.Except(LogLinesCache);
            //Got result. Set cache
            LogLinesCache = rawLines;

            return newLines.ToArray();
        }

        public async Task<ArkLogMsg[]> UpdateGameLog()
        {
            try
            {
                string[] rawString = await FetchNewLogData();
                //Parse each of these
                List<ArkLogMsg> logItems = new List<ArkLogMsg>();
                foreach (string raw in rawString)
                {
                    ArkLogMsg msg = ArkLogParser.ParseMessage(raw);
                    if(msg!=null)
                        logItems.Add(msg);
                }
                //Check if we got anything
                if (logItems.Count == 0)
                    return new ArkLogMsg[0];
                DSharpPlus.Entities.DiscordChannel channel = null;
                if (logItems.Count>0)
                {
                    //Get Discord server.
                    channel = await Program.discord.GetChannelAsync(notificationChannel);
                }
                //Foreach
                foreach (ArkLogMsg msg in logItems)
                {
                    switch (msg.type)
                    {
                        case ArkLogMsgType.Death_Generic:
                            string header =  msg.target.tribe + "'s " + msg.target.dinoClass + " died!";
                            if(msg.target.dinoClass == null)
                                header = msg.target.name+" of tribe "+msg.target.tribe+" died!";
                            var embed = Tools.GenerateEmbed(header, "Darn. "+msg.time, msg.target.name + " (Level " + msg.target.level.ToString() + ")", DSharpPlus.Entities.DiscordColor.DarkButNotBlack);
                            await channel.SendMessageAsync(embed: embed);
                            break;
                        case ArkLogMsgType.Death_KilledBy:
                            string headerTwo = msg.target.ToNiceString()+" was killed by "+msg.actioner.ToNiceString();
                            var embedTwo = Tools.GenerateEmbed(headerTwo, "Oh darn!", msg.target.ToLongString(), DSharpPlus.Entities.DiscordColor.DarkButNotBlack);
                            await channel.SendMessageAsync(embed: embedTwo);
                            break;
                        case ArkLogMsgType.NewTame:
                            var embedThree = Tools.GenerateEmbed(msg.actioner.tribe + " tamed a " + msg.target.name + "!", "Nice tame!", "Level " + msg.target.level.ToString() + "!", DSharpPlus.Entities.DiscordColor.Yellow);
                            await channel.SendMessageAsync(embed: embedThree);
                            break;
                        case ArkLogMsgType.Chat:
                            //This is a bit of a jank-fix that might break in the future.
                            string msgContent = msg.messageContent.Substring(3);
                            var embedFour = Tools.GenerateEmbed("Message from " + msg.messageSteamName, msg.ParseTimeString()+" - "+name, msgContent, DSharpPlus.Entities.DiscordColor.LightGray);
                            await channel.SendMessageAsync(embed: embedFour);
                            break;
                        case ArkLogMsgType.DisconnectJoin:
                            var color = DSharpPlus.Entities.DiscordColor.Red;
                            if (msg.messageContent == "joined")
                                color = DSharpPlus.Entities.DiscordColor.Green; //They actually joined.
                            var embedFive = Tools.GenerateEmbed("Player " + msg.messageIngameName + " " + msg.messageContent + " the game", msg.ParseTimeString(), "Server " + name, color);
                            await channel.SendMessageAsync(embed: embedFive);
                            break;
                    }
                }
                return logItems.ToArray();
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return new ArkLogMsg[0];
            }
        }




        /// <summary>
        /// Stop server
        /// </summary>
        public void DeInit()
        {
            
        }


        /// <summary>
        /// Reinitialization
        /// </summary>
        /// <returns></returns>
        public async Task Reinit()
        {
            Console.WriteLine("Initializing ARK server " + name);
            //Set this up again.
            Console.WriteLine("Connecting via RCON...");
            rconConnection = new RconClient(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(rconIp), ushort.Parse(rconPort.ToString())), rconPassword, new RconClient.ReadyCallback((RconClient context, bool okay) =>
            {
                Console.WriteLine("Connected to RCON.");
                bool isRconConnected = okay;
                if (!isRconConnected)
                {
                    Console.WriteLine("Failed to connect to the Ark server via RCON. Check to make sure it's running, or see if your settings are correct.");
                }
                
                //Set up the timer again
                timer = new System.Timers.Timer(Program.arkUpdateTimeMs);
                timer.Elapsed += async (sender, e) => await Update();
                timer.Start();
                //Connect to Ark IO
                try
                {
                    arkIO = new ArkIOInterface("10.0.1.13", 13000, "password"); //Todo: Set this up in a custom  way.
                    if (!arkIO.client.client.Connected)
                    {
                        Console.WriteLine("Failed to connect to the Ark IO interface. Check the settings and try again.");
                    }
                }
                catch
                {
                    arkIO = null;
                    Console.WriteLine("Failed to connect to the Ark IO interface. Check the settings and try again.");
                }
            }));
            
            
        }

        


        //Setup functions
        
        /// <summary>
        /// This function will be called when the encoded string is sent to the bot's DM to setup the server.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// 

        public static async Task MessageSentToFinshSetup(DSharpPlus.EventArgs.MessageCreateEventArgs e, DiscordUser user)
        {
            var embed = Tools.GenerateEmbed("ArkBot Setup", "", "Verifying...", DSharpPlus.Entities.DiscordColor.Red);
            var msg = await e.Message.RespondAsync(embed: embed);
            //Check if user is auth.
            if(user.permissionLevel != DiscordUserPermissionLevel.owner)
            {
                //Not owner!
                embed = Tools.GenerateEmbed("ArkBot Setup", "", "You're not the owner of the bot! Ask the owner to do this action.", DSharpPlus.Entities.DiscordColor.Red);
                await msg.ModifyAsync(embed: embed);
                return;
            }
            //Decode the base 64
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(e.Message.Content));
            //Deserialize
            ArkServer s = (ArkServer)Tools.DeserializeObject(decoded, typeof(ArkServer));
            if(s==null)
            {
                //Failed
                await msg.ModifyAsync(embed: Tools.GenerateEmbed("ArkBot Setup Error", "Keep in mind that a DM chat can only be used to create servers", "The encoded string couldn't be read! Make sure it is copied in it's entirety. ", DSharpPlus.Entities.DiscordColor.Red));
                return;
            }
            //Todo: Verify
            //Todo: Rcon port
            //Test RCON connection IF rcon is enabled
            if(s.isRconEnabled)
            {
                embed = Tools.GenerateEmbed("Testing RCON...", "", "One moment...", DSharpPlus.Entities.DiscordColor.Yellow);
                var testMsg = await e.Message.RespondAsync(embed: embed);
                try
                {
                    /*
                    var rcon = await ArkRconConnection.ConnectToRcon(s.rconIp, s.rconPort, s.rconPassword);
                    bool isConnected = rcon.isReady;
                    if (isConnected)
                    {
                        embed = Tools.GenerateEmbed("RCON test passed!", "", "Thanks for setting up RCON!", DSharpPlus.Entities.DiscordColor.Green);
                        await testMsg.ModifyAsync(embed: embed);
                    }
                    else
                    {
                        embed = Tools.GenerateEmbed("RCON test failed!", "Please try again.", "No other info provided.", DSharpPlus.Entities.DiscordColor.Red);
                        await testMsg.ModifyAsync(embed: embed);
                        return;
                    }*/
                    throw new NotImplementedException();
                } catch (Exception ex)
                {
                    embed = Tools.GenerateEmbed("RCON test failed!", "Please try again.", ex.Message, DSharpPlus.Entities.DiscordColor.Red);
                    await testMsg.ModifyAsync(embed: embed);
                    return;
                }
            }
            //Test the notification channel
            DSharpPlus.Entities.DiscordChannel channel;
            try
            {
                ulong id = s.notificationChannel;
                channel = await Program.discord.GetChannelAsync(id);
                var channelTestMsg = await Program.discord.SendMessageAsync(channel, "Setting ArkBot channel to this for server '" + s.name + "'.");
            }
            catch (Exception ex)
            {
                embed = Tools.GenerateEmbed("Creation Failed!", ex.Message, "The notification channel couldn't be found.", DSharpPlus.Entities.DiscordColor.Red);
                await msg.ModifyAsync(embed: embed);
                await msg.RespondAsync(embed: Tools.DumpEmbedException(ex));
                return;
            }

            //We're good if we land here. The message was sent correctly.
            //Create roles if they don't exist
            try
            {
                var guild = channel.Guild;
                //Look for the roles.
                var existingRoles = guild.Roles;
                ulong roleId_admin = 0;
                ulong roleId_user = 0;
                foreach (var role in existingRoles)
                {
                    if (role.Name == DiscordUser.ROLE_NAME_USER)
                        roleId_user = role.Id;
                    if (role.Name == DiscordUser.ROLE_NAME_ADMIN)
                        roleId_admin = role.Id;
                }
                //Create the roles if they don't exist
                if (roleId_admin == 0)
                    roleId_admin = (await guild.CreateRoleAsync(DiscordUser.ROLE_NAME_ADMIN)).Id;
                if (roleId_user == 0)
                    roleId_user = (await guild.CreateRoleAsync(DiscordUser.ROLE_NAME_USER)).Id;
                //We know the roles exist.
                s.adminRoleId = roleId_admin;
                s.userRoleId = roleId_user;
            } catch (Exception ex)
            {
                embed = Tools.GenerateEmbed("Creation Failed!", ex.Message, "Failed to create roles. Make sure the bot has permission to do so.", DSharpPlus.Entities.DiscordColor.Red);
                await msg.ModifyAsync(embed: embed);
                await msg.RespondAsync(embed: Tools.DumpEmbedException(ex));
                return;
            }

            //Now save
            try
            {
                await s.SaveSettings();
            } catch (Exception ex)
            {
                embed = Tools.GenerateEmbed("Creation Failed!", ex.Message, "Failed to save. Make sure the path used is correct.", DSharpPlus.Entities.DiscordColor.Red);
                await msg.ModifyAsync(embed: embed);
                await msg.RespondAsync(embed: Tools.DumpEmbedException(ex));
                return;
            }

            //Done.
            embed = Tools.GenerateEmbed("Creation Finished!", "", "You may need to reload ArkBot.", DSharpPlus.Entities.DiscordColor.Green);
            await msg.ModifyAsync(embed: embed);
        }


        private async Task SaveSettings()
        {
            //Set some finishing values.
            creationProgress = ArkServerCreationStep.Done;
            saveVersion = Program.ApplicationVersion;
            Random rand = new Random();
            serverUuid = rand.Next(int.MinValue, int.MaxValue).ToString();
            //Save
            string data = Tools.SerializeObject(this);
            //Deinit self
            DeInit();
            //Write this to the disk.
            File.WriteAllText(Program.savePath + "saves/" + serverUuid, data);
            //Reinit self.
            await Program.LoadArkServer(data);
        }
        
    }

    enum ArkServerCreationStep
    {
        Done,
        JustStarted,
        NamePrompt,
        RconPrompt,
        Rcon_ServerIP,
        Rcon_ServerPassword,
        Rcon_ChatPermissionLevel,
        ServerStartStopPermissionLevel,
        DiscordChannelPrompt
    }
}
