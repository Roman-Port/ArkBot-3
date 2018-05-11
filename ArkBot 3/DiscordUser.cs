using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    [DataContract]
    class DiscordUser
    {
        public string username;
        public ulong id;
        public DiscordUserPermissionLevel permissionLevel;

        [DataMember]
        public float arkPoints = 0;
        [DataMember]
        public string arkLinkedSteamName = "";
        [DataMember]
        public string arkLinkedUsername = "";
        [DataMember]
        public bool isArkLinked = false;

        public const string ROLE_NAME_USER = "ArkBot-Permission-User";
        public const string ROLE_NAME_ADMIN = "ArkBot-Permission-Admin";

        public static async Task<DiscordUser> GetUserFromDiscord(DSharpPlus.EventArgs.MessageCreateEventArgs e, ArkServer server = null)
        {
            
            //Check if this user has roles.
            DiscordUserPermissionLevel perm = DiscordUserPermissionLevel.anyone;
            if (server != null)
            {
                //Fetch their permission level.
                var guild = e.Guild;
                if (guild != null)
                {
                    var member = await guild.GetMemberAsync(e.Author.Id);

                    CheckIfUserHasRoles(member, server.adminRoleId, server.userRoleId, out bool isAdmin, out bool isUser);

                    if (isUser)
                        perm = DiscordUserPermissionLevel.user;
                    if (isAdmin)
                        perm = DiscordUserPermissionLevel.admin;
                }
            }
            //Check if this user owns the bot.
            if (e.Author.Id == Program.ownerId)
                perm = DiscordUserPermissionLevel.owner;

            return new DiscordUser(e.Author.Username, e.Author.Id, perm);
        }

        public static DiscordUser GetUserByArkName(string name)
        {
            //Get their id
            try
            {
                ulong id = ArkLinkList.GetDiscordUserIdBySteamName(name);
                
                //Create a new object.
                return new DiscordUser("", id, DiscordUserPermissionLevel.anyone);
            } catch
            {
                return null;
            }
        }

        public void CloseUser()
        {
            //Save my settings to the disk.
            string path = Program.savePath + "users\\" + id.ToString();
            string raw = Tools.SerializeObject(this);

            File.WriteAllText(path, raw);
        }

        private static void CheckIfUserHasRoles(DSharpPlus.Entities.DiscordMember member, ulong adminId, ulong userId, out bool isAdmin, out bool isUser) 
        {
            var roles = member.Roles;
            isAdmin = false;
            isUser = false;
            foreach(var role in roles)
            {
                if (role.Id == adminId)
                    isAdmin = true;
                if (role.Id == userId)
                    isUser = true;
            }
        }

        public async Task BeginArkLink(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            //Create a random id
            string id = Tools.GenerateRandomString(8);
            var embed = Tools.GenerateEmbed("Ark Link", "", "To link your Discord account to your Ark user, type **" + id + "** into the Ark chat.", DiscordColor.Magenta);
            var msg = await e.Message.RespondAsync(embed: embed);
            PendingArkLink pal = new PendingArkLink();
            pal.user = this;
            pal.linkPending = true;
            pal.randId = id;
            pal.msg = msg;
            Program.pendingArkLinks.Add(pal);
            
        }

        public static async Task FinishArkLink(string playerName, string steamName, DSharpPlus.Entities.DiscordMessage msg, ulong id)
        {
            //Fetch this user
            DiscordUser u = new DiscordUser("", id, DiscordUserPermissionLevel.admin);
            //We have the user and the player. Finish link
            u.isArkLinked = true;
            u.arkLinkedUsername = playerName;
            u.arkLinkedSteamName = steamName;
            var embed = Tools.GenerateEmbed("Ark Link", "Nice job!", "Ark link completed! You're now linked with " + u.arkLinkedUsername + "!",DiscordColor.Green);
            //Close user to save.
            u.CloseUser();
            //Write to the list of linked users.
            ArkLinkList.WriteToUserList(new ArkLinkItem(steamName.Trim(' '), id,playerName.Trim(' ')));
            await msg.ModifyAsync(embed: embed);
            //Done.
        }

        public async Task SendUserStatus(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Stats",
                Color = DSharpPlus.Entities.DiscordColor.Magenta
            };
            embed.Footer = new DiscordEmbedBuilder.EmbedFooter();
            embed.Footer.Text = "Hey! It's " + e.Author.Username + "! I'm your biggest fan!";
            embed.AddField("Permission Level", permissionLevel.ToString(), true);
            embed.AddField("Ark Tokens", arkPoints.ToString(), true);
            string lnk = "Not Linked! (You should change that with %link)";
            if(isArkLinked)
            {
                lnk = arkLinkedUsername + " (" + arkLinkedSteamName + ")";
            }
            embed.AddField("Ark Link", lnk, true);

            //Respond
            await e.Message.RespondAsync(embed: embed);
        }

        public DiscordUser(string _username, ulong _id, DiscordUserPermissionLevel _perm)
        {
            //Main constructor
            //First, load all of the info we can from the disk if we exist.
            string path = Program.savePath + "users\\" + _id.ToString();
            if (File.Exists(path))
            {
                string raw = File.ReadAllText(path);
                DiscordUser u = (DiscordUser)Tools.DeserializeObject(raw, typeof(DiscordUser));
                //Exchange values
                this.arkLinkedSteamName = u.arkLinkedSteamName;
                this.arkLinkedUsername = u.arkLinkedUsername;
                this.isArkLinked = u.isArkLinked;
                this.arkPoints = u.arkPoints;
            }
            username = _username;
            id = _id;
            permissionLevel = _perm;
        }

        public async Task ShowPersonalHelp(DSharpPlus.EventArgs.MessageCreateEventArgs e, ArkServer server)
        {
            string help = "";
            string prefix = Program.PrefixString;
            help += prefix + "**help** - Shows the menu you're in right now! \r\n";
            help += prefix + "**chat** - If your admin enabled it, sends a chat message to the Ark server. \r\n";
            help += prefix + "**list** - Shows a playerlist of everyone on the Ark server. \r\n";
            var channel = await Program.discord.GetChannelAsync(server.notificationChannel);
            help += "\r\nThe current notification channel for this Ark server is **" + channel.Name + "**.";

            //Create embed
            var embed = Tools.GenerateEmbed("ArkBot Help", "ArkBot by RomanPort - V0.0.0 - romanport.com", help, Program.CreateDynamicColor());
            await e.Message.RespondAsync(embed: embed);
        }

        public static async Task AskForOwner()
        {
            //Fetch owner
            if (File.Exists(Program.savePath + "owner.id"))
            {
                try
                {
                    Program.ownerId = ulong.Parse(File.ReadAllText(Program.savePath + "owner.id"));
                }
                catch
                {
                    File.Delete(Program.savePath + "owner.id");
                }
            }
            else
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine("Please copy in your Discord ID.");
                        string id = Console.ReadLine();
                        Program.ownerId = ulong.Parse(id);
                        var user = await Program.discord.GetUserAsync(Program.ownerId);
                        Console.WriteLine("Thanks, " + user.Username + "! Type %create anywhere to create a server.");
                        //Save
                        File.WriteAllText(Program.savePath + "owner.id", id);
                        break;
                    }
                    catch
                    {
                        Console.WriteLine("Invalid ID!");
                        Console.ReadLine();
                        Console.Clear();
                        continue;
                    }
                }
                //No owner!? Ask in console.
            }
        }
    }

    enum DiscordUserPermissionLevel
    {
        //Holds the permissions for this application.
        anyone,
        user,
        admin,
        owner,
        none
    }

    class PendingArkLink
    {
        public DiscordUser user;
        public string randId = "";
        public bool linkPending = false;
        public DSharpPlus.Entities.DiscordMessage msg;
    }
}
