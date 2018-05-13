using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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
        public int rconPort; //These might not be used.
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
        public ArkRconConnection rcon;
        public bool isRconConnected = false;
        public System.Timers.Timer timer; //Used for updates
        public string[] userlistCache = new string[0];
        public ArkServerProcessStatus processStatus = ArkServerProcessStatus.Unknown;
        public Process process = null;
        public DateTime processStartTime = DateTime.UtcNow;
        public ArkIOInterface arkIO;

        //Functions
        //Util
        

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
                        rcon = ArkRconConnection.ConnectToRcon(rconIp, ushort.Parse(rconPort.ToString()), rconPassword).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (rcon.isConnected)
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
                    }
                }
                else
                {

                }
            }).Start();
        }

        private void FetchArkProcess()
        {
            Process[] ps = Process.GetProcessesByName("ShooterGameServer");
            if(ps.Length==1)
            {
                process = ps[0];
                Console.WriteLine("Found Ark process " + process.Id.ToString());
                processStatus = ArkServerProcessStatus.Online;
                processStartTime = DateTime.UtcNow;
            } else
            {
                //Not found.
                process = null;
                processStatus = ArkServerProcessStatus.Unknown;
                Console.WriteLine("Ark process couldn't be found.");
            }
        }

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

        private bool GetIsRconOkay()
        {
            return (processStatus == ArkServerProcessStatus.Online || processStatus == ArkServerProcessStatus.Unknown) && isRconEnabled && isRconConnected;
        }

        private long LastError = 0;

        private async Task Update()
        {
            //Called every second
            try
            {
                if (GetIsRconOkay()) //Only do this if RCON is connected and enabled.
                {
                    //Check for Ark leave/reconnects
                    Entity_ArkPlayer[] onlinePlayers = await GetAndSendPlayerJoinLeaves();

                    //Print log actions
                    ArkLogMsg[] logs = await UpdateGameLog();

                    //Check for chats
                    int chatsSent = await GetAndSendNewChat();

                    //Give out points.
                    float numberOfPoints = Program.arkPointMult * ((float)Program.arkUpdateTimeMs / 1000f);
                    if (onlinePlayers != null)
                    {
                        //Find this user.
                        foreach (Entity_ArkPlayer player in onlinePlayers)
                        {
                            //Get via NAME
                            DiscordUser user = DiscordUser.GetUserByArkName(player.name.Trim(' '));
                            if (user != null)
                            {
                                //Add points
                                user.arkPoints += numberOfPoints;
                                //Save
                                user.CloseUser();
                            }
                        }
                    }
                }
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
            return await rcon.RunCommand(msg);
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
            await RunRCONCommand("ServerChat ["+username+"] " + msg); //POSSIBLE INJECTION ATTACK!
        }

        public async Task<string[]> FetchPlayerList()
        {
            //Run commands
            string raw = await RunRCONCommand("ListPlayers");
            if(raw.ToLower().Contains("no players connected"))
            {
                return new string[0];
            }
            string[] rawArray = Tools.SplitRconByLine(raw);
            List<string> p = new List<string>();
            foreach(string r in rawArray)
            {
                if (r.Length < 3)
                    continue;
                p.Add(r.Substring(r.Split('.').Length));
            }
            return p.ToArray();
        }

        public async Task ListPlayersCmd(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            //Fetch players.
            string players = "";
            int amount = 0;
            foreach(string p in userlistCache)
            {
                if (p.Length < 2)
                    continue;
                //Parse
                Entity_ArkPlayer player = Entity_ArkPlayer.ParsePlayerList(p, Entity_ArkPlayerStatus.Joined);
                players += player.name + "\r\n";
                amount++;
            }
            //Generate
            var embed = Tools.GenerateEmbed("Player Listing", amount.ToString() + " players total", players, DSharpPlus.Entities.DiscordColor.Magenta);
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

            string raw = await rcon.RunCommand("GetGameLog");
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
                //Get Discord server.
                var channel = await Program.discord.GetChannelAsync(notificationChannel);
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
                            var embedThree = Tools.GenerateEmbed(msg.actioner.tribe + " tamed a " + msg.target.name + "!", "Neato :ok_hand:", "Level " + msg.target.level.ToString() + "!", DSharpPlus.Entities.DiscordColor.Yellow);
                            await channel.SendMessageAsync(embed: embedThree);
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





        public void DeInit()
        {
            //Stop
            if(rcon!=null)
                rcon.Dispose();
        }

        public async Task Reinit()
        {
            //Find Ark process
            FetchArkProcess();
            //Set this up again.
            if (isRconEnabled)
            {
                rcon = await ArkRconConnection.ConnectToRcon(rconIp, 27020, rconPassword);
                isRconConnected = true;
            }
            //Set up the timer again
            timer = new System.Timers.Timer(Program.arkUpdateTimeMs);
            timer.Elapsed += async (sender, e) => await Update();
            timer.Start();
            //Connect to Ark IO
            arkIO = new ArkIOInterface("10.0.1.13", 13000, "password");
            Console.WriteLine(arkIO.client.client.Connected);
            
        }

        


        //Setup functions
        public static async Task MessageSentToBeginSetup(DSharpPlus.EventArgs.MessageCreateEventArgs e, DiscordUser user)
        {
               //THIS FUNCTION IS NO LONGER USED! It was replaced by the web interface.
            
            //If we land here, don't assume the user is auth. Check that now.
            if (user.permissionLevel != DiscordUserPermissionLevel.owner)
            {
                //User isn't owner. Stop.
                await e.Message.RespondAsync("You're not the owner of ArkBot, you cannot continue with setup.");
                return;
            }
            //Check if the user already has a server creation in progress.
            if (GetArkServersInSetupStageForUser(user.id).Length != 0)
            {
                await e.Message.RespondAsync("You're already in the process of creating an Ark server. Finish it first! Todo: Add a way to stop creation.");
                return;
            }
            //This function will create an ArkServer class and get it ready for setup.
            //Ask the user the first question over DM.
            var channel = await Program.discord.CreateDmAsync(e.Author);
            var embed = Tools.GenerateEmbed("ArkBot Setup", "", "To setup ArkBot, go to the URL below and reply with the encoded data.", Program.CreateDynamicColor());
            await channel.SendMessageAsync(embed:embed);
            
        }

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
                    var rcon = await ArkRconConnection.ConnectToRcon(s.rconIp, 27020, s.rconPassword);
                    bool isConnected = rcon.isConnected;
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
                    }
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

        public static ArkServer[] GetArkServersInSetupStageForUser(ulong id)
        {
            //Fetch Ark servers that are in the process of being created for a specific user.
            List<ArkServer> output = new List<ArkServer>();
            foreach (ArkServer s in Program.setupInProgressServers)
            {
                if (s.creatorId == id && s.creationProgress!= ArkServerCreationStep.Done)
                {
                    output.Add(s);
                }
            }
            return output.ToArray();
        }

        static string SetupPrintStep(ArkServerCreationStep step)
        {
            return "Step " + ((int)step).ToString() + "/7";
        }

        public async Task MessageSentDuringSetup(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            //THIS FUNCTION IS NO LONGER USED! It was replaced by the web interface.
            
            
            
            //If we land here, WE ASSUME THE USER IS AUTH AND THIS WAS SENT VIA DM

            //Decode the JSON sent.





            //Check what they'd like to do.
            if (creationProgress == ArkServerCreationStep.NamePrompt)
            {
                //They typed in a name. Set it.
                name = e.Message.Content;
                await e.Message.RespondAsync("Cool. The server name is now '" + name + "'.\r\nWould you like to enable RCON for the server? This will add extended features such as chat and server list. [Yes/No]");
                creationProgress = ArkServerCreationStep.RconPrompt;
                return;
            }
            if (creationProgress == ArkServerCreationStep.RconPrompt)
            {
                //Replied to the prompt to enable RCON. See if it's parseable
                bool parseOk = Tools.TryParseYesNo(e.Message.Content, out bool response);
                if (!parseOk)
                {
                    //Bad parse. Let the user know and hault.
                    await e.Message.RespondAsync("Couldn't parse yes/no! Try again.");
                    return;
                }
                //Parse was OK. Set it and respond appropriately.
                isRconEnabled = response;
                if (!isRconEnabled)
                {
                    //RCON isn't enabled. Set some values.
                    rconChatPermissionLevel = DiscordUserPermissionLevel.none;
                    rconPassword = "";
                    rconIp = "";
                    //Skip the RCON steps.
                    await AfterRconSetup(e);
                }
                else
                {
                    //We'll continue asking the user questions about RCON.
                    creationProgress = ArkServerCreationStep.Rcon_ChatPermissionLevel;
                    await e.Message.RespondAsync("What permission level would you like to require for server chat?\r\nOptions: " + Tools.GetPermLevelsString());
                }
                return;
            }
            if (creationProgress == ArkServerCreationStep.Rcon_ChatPermissionLevel)
            {
                //Got response for question about permission level.
                //Try parse
                bool ok = Tools.TryParsePermLevel(e.Message.Content, out DiscordUserPermissionLevel perm);
                if (!ok)
                {
                    //Failed to parse. Try again.
                    await e.Message.RespondAsync("Couldn't parse! " + Tools.GetPermLevelsString() + " Try again.");
                    return;
                }
                //Parse was OK. Set it.
                rconChatPermissionLevel = perm;
                creationProgress = ArkServerCreationStep.Rcon_ServerIP;
                await e.Message.RespondAsync("What is the IP of the server? If you don't know, use 'localhost'.");
                return;
            }
            if (creationProgress == ArkServerCreationStep.Rcon_ServerIP)
            {
                //Set it.
                rconIp = e.Message.Content;
                creationProgress = ArkServerCreationStep.Rcon_ServerPassword;
                await e.Message.RespondAsync("What is the admin password for your server? This will only be sent to your server and is used to log into RCON.");
                return;
            }
            if (creationProgress == ArkServerCreationStep.Rcon_ServerPassword)
            {
                //Last RCON step.
                rconPassword = e.Message.Content;
                //Testing connection
                var msg = await e.Message.RespondAsync("Testing the connection to " + name + "...");
                rcon = await ArkRconConnection.ConnectToRcon(rconIp, 27020, rconPassword);
                if (rcon.isConnected == false)
                {
                    //Failed. Restart RCON setup.
                    await msg.ModifyAsync("RCON connection test failed. Restarting RCON setup...\n```"+rcon.failMsg+"```");
                    await e.Message.RespondAsync("Would you like to enable RCON for the server? This will add extended features such as chat and server list. [Yes / No]");
                    creationProgress = ArkServerCreationStep.RconPrompt;
                }
                else
                {
                    //OK. Allow the user to continue.
                    await msg.ModifyAsync("RCON connection test was OK!");
                    //Use the after rcon setup function.
                    await AfterRconSetup(e);
                }
                return;
            }
            if(creationProgress== ArkServerCreationStep.ServerStartStopPermissionLevel)
            {
                //Parse permission
                bool ok = Tools.TryParsePermLevel(e.Message.Content, out DiscordUserPermissionLevel perm);
                if (!ok)
                {
                    //Failed to parse. Try again.
                    await e.Message.RespondAsync("Couldn't parse! " + Tools.GetPermLevelsString() + " Try again.");
                    return;
                }
                //Parse was OK. Set it.
                serverStartStopPermissionLevel = perm;
                //Ask for the Discord channel
                await e.Message.RespondAsync("Please copy the ID of the Discord channel you'd like to use with this Ark server.\r\nTo get this, enable developer tools in the Discord settings, then right click the channel you'd like and hit \"Copy ID\".");
                creationProgress = ArkServerCreationStep.DiscordChannelPrompt;
                return;
            }
            if (creationProgress == ArkServerCreationStep.DiscordChannelPrompt)
            {
                //Try to parse this.
                var msgStatus = await e.Message.RespondAsync("Testing channel...");
                try
                {
                    ulong id = ulong.Parse(e.Message.Content);
                    var channel = await Program.discord.GetChannelAsync(id);

                    var msg = await Program.discord.SendMessageAsync(channel, "Setting ArkBot channel to this for server '" + name + "'.");
                    //We're good if we land here. The message was sent correctly.
                    notificationChannel = id;
                    await msgStatus.ModifyAsync("The channel was saved correctly.");
                    //Now, set some guild settings.
                    msgStatus = await e.Message.RespondAsync("Creating roles...");
                    try
                    {
                        
                        var guild = channel.Guild;
                        //Look for the roles.
                        var existingRoles = guild.Roles;
                        ulong roleId_admin = 0;
                        ulong roleId_user = 0;
                        foreach(var role in existingRoles)
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
                        adminRoleId = roleId_admin;
                        userRoleId = roleId_user;
                        await msgStatus.ModifyAsync("Permission roles have been created!\r\nAdd the role **" + DiscordUser.ROLE_NAME_USER + "** to standard users.\r\nAdd the role **" + DiscordUser.ROLE_NAME_ADMIN + "** to admins.\r\nUsers without a role will be **standard users**.\r\nYou will be the only user with **owner**.");
                    } catch
                    {
                        await msgStatus.ModifyAsync("Couldn't modify roles. Make sure ArkBot has permission and try again.");
                    }


                    //Save the server.
                    msgStatus = await e.Message.RespondAsync("Saving now...");
                    try
                    {
                        await SaveSettings();
                    } catch
                    {
                        //Error.
                        await msgStatus.ModifyAsync("The server couldn't be saved to the disk. Check to make sure you have a valid save folder.");
                    }
                }
                catch (Exception ex)
                {
                    await msgStatus.ModifyAsync("Failed to set channel. Check the ID and try again.");
                }
            }
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

        public async Task AfterRconSetup(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            await e.Message.RespondAsync("What permission level would you like to require for server start/stop?\r\nOptions: " + Tools.GetPermLevelsString());
            creationProgress = ArkServerCreationStep.ServerStartStopPermissionLevel;
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
