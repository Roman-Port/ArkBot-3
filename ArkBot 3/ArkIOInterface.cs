using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RomansSimpleNetworking;
using ArkIOInterfaceData;

namespace ArkBot_3
{
    class ArkIOInterface
    {
        ///<summary>
        /// This class is used to interface with the Ark executable.
        /// 
        /// This is used so, in the future, the Ark executable could be on a remote machine.
        /// 
        /// </summary>

        public RSN_Client client;

        Dictionary<int, ArkIOInterface_SavedContext> savedContexts =
            new Dictionary<int, ArkIOInterface_SavedContext>();

        public ArkIOInterface(string ip, Int32 port, string password)
        {
            List<RSN_Client_CallbackConfig> callbacks = new List<RSN_Client_CallbackConfig>();

            //Add the callback Test with ID 1 to the list.
            callbacks.Add(new RSN_Client_CallbackConfig(1, typeof(ArkIOInterfaceRequestData)));

            client = RSN_Client.Connect(callbacks.ToArray(), password, ip, port);
        }

        private void CreateRequest(RequestType type,string strOne, string strTwo, int intOne, int intTwo, RSN_ClientResponse callback = null, ArkIOInterface_SavedContext context = null)
        {
            ArkIOInterfaceRequestData data = new ArkIOInterfaceRequestData();
            data.strArgOne = strOne;
            data.strArgTwo = strTwo;
            data.intArgOne = intOne;
            data.intArgTwo = intTwo;
            data.type = type;
            if (callback == null)
                callback = new RSN_ClientResponse(Callback);
            int id = client.SendData(callback, data);
            if(context!=null)
            {
                savedContexts.Add(id, context);
            }
        }

        private void Callback(object raw)
        {
            ArkIOInterfaceResponse data = ((ArkIOInterfaceRequestData)raw).response;
            Console.WriteLine("test");
        }

        private async Task TryEditMessage(int id, DSharpPlus.Entities.DiscordEmbedBuilder embed)
        {
            try
            {
                ArkIOInterface_SavedContext context = savedContexts[id];
                await context.msg.ModifyAsync(embed: embed);
            } catch
            {
                //Not found
            }
        }

        public void CopyFile(string src, string dest)
        {
            CreateRequest(RequestType.Copy, src, dest, 0, 0);
        }

        public async Task BackupServerDiscord(DSharpPlus.EventArgs.MessageCreateEventArgs e, string src, string destFolder)
        {
            var embed = Tools.GenerateEmbed("Backing Up...", "0% Complete", "Wait a moment. Server is backing up.", DSharpPlus.Entities.DiscordColor.Yellow, "https://romanport.com/static/ArkBot/loader.gif");
            var msg = await e.Message.RespondAsync(embed: embed);
            //Create dest file
            DateTime now = DateTime.Now;
            bool isAm = now.Hour < 12;
            string amPm = "AM";
            if (isAm)
                amPm = "PM";
            string fileName = "Backup at " + (now.Hour%12).ToString() + "_" + now.Minute.ToString() + "_"+amPm+ " " + now.Month.ToString() + "_" + now.Day.ToString() + "_" + now.Year.ToString() + " "+DateTime.UtcNow.Ticks.ToString()+".zip";
            string dest = destFolder.TrimEnd('\\') + "\\" + fileName;
            //Actually commit
            ArkIOInterface_SavedContext context = new ArkIOInterface_SavedContext();
            context.msg = msg;
            CreateRequest(RequestType.Compress, src, dest, 0, 0, new RSN_ClientResponse(BackupServerCallback), context);
        }

        private void BackupServerCallback(object raw)
        {
            ArkIOInterfaceResponse data = ((ArkIOInterfaceRequestData)raw).response;
            var embed = Tools.GenerateEmbed("Backup complete!", "", "Saved!", DSharpPlus.Entities.DiscordColor.Green);
            //Check if it errored
            if(data.hasErrored)
            {
                embed = Tools.GenerateEmbed("Fatal Error While Backing Up", "Ark IO Interface returned an error. No further details are available.", "Couldn't backup ARK!", DSharpPlus.Entities.DiscordColor.Red, headerUrl: "https://romanport.com/static/ArkBot/assets/misc/warning.png");
            }
            TryEditMessage(data.clientId, embed);
        }

        public async Task ListBackupsDiscord(DSharpPlus.EventArgs.MessageCreateEventArgs e, string src)
        {
            var embed = Tools.GenerateEmbed("Fetching...", "", "", DSharpPlus.Entities.DiscordColor.Yellow);
            var msg = await e.Message.RespondAsync(embed: embed);
            ArkIOInterface_SavedContext context = new ArkIOInterface_SavedContext();
            context.msg = msg;
            CreateRequest(RequestType.DirList, src, "", 0, 0, new RSN_ClientResponse(ListBackupsCallback), context);
        }

        private void ListBackupsCallback(object raw)
        {
            ArkIOInterfaceResponse data = ((ArkIOInterfaceRequestData)raw).response;
            //Parse DateTimes for each of these.
            List<long> dates = new List<long>();
            foreach(var date in data.output)
            {
                try
                {
                    string[] r = date.Split(' ');
                    string rawDate = r[r.Length - 1];
                    rawDate = rawDate.Substring(0, rawDate.Length - 4);
                    long l = long.Parse(rawDate);
                    dates.Add(l);
                } catch
                {

                }
            }
            dates.Sort();
            dates.Reverse();
            int i = 0;
            string output = "";
            while(i<dates.Count)
            {
                if (i > 4)
                    break;
                DateTime dt = new DateTime(dates[i]);
                DateTime dt_here = dt.ToLocalTime();
                output += dt.ToShortTimeString() + " at " + dt.ToShortDateString() + " (" + dt_here.ToShortTimeString() + " at " + dt_here.ToShortDateString() + " here)\r\n";
                i++;
            }
            var embed = Tools.GenerateEmbed("Latest Backups", Tools.CompareAndUsePlural("backup",dates.Count)+" (Max 5)", output, DSharpPlus.Entities.DiscordColor.Magenta);
            
            TryEditMessage(data.clientId, embed);
        }
    }

    class ArkIOInterface_SavedContext
    {
        public DSharpPlus.Entities.DiscordMessage msg;
    }
}
