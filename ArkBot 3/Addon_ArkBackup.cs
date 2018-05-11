using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    class Addon_ArkBackup
    {
        public static async Task BackupArk(DSharpPlus.EventArgs.MessageCreateEventArgs e, string path)
        {
            //First, create a message
            var embed = Tools.GenerateEmbed("Creating Backup...", "Just a sec...", "0% Complete", DSharpPlus.Entities.DiscordColor.Cyan, "https://romanport.com/static/ArkBot/loader.gif");
            var msg = await e.Message.RespondAsync(embed: embed);
            //Create a temporary path
            string output = "";
            //Compress
            ZipFile.CreateFromDirectory(path, output);
        }
    }
}
