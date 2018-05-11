using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    [DataContract]
    class ArkLinkList
    {
        [DataMember]
        public List<ArkLinkItem> users = new List<ArkLinkItem>();

        private static ArkLinkList Load()
        {
            string path = Program.savePath + "linked.list";
            ArkLinkList main = new ArkLinkList();
            if (System.IO.File.Exists(path))
            {
                string raw = System.IO.File.ReadAllText(path);
                ArkLinkList l = (ArkLinkList)Tools.DeserializeObject(raw, typeof(ArkLinkList));
                main.users = l.users;
            }
            return main;
        }

        public static void WriteToUserList(ArkLinkItem i)
        {
            //Load array
            ArkLinkList main = Load();
            //We have our list. See if we're on it
            bool writeNew = true;
            foreach(ArkLinkItem item in main.users)
            {
                if(item.discordId == i.discordId )
                {
                    //Just edit this, save, and exit
                    writeNew = false;
                    item.steamIds = i.steamIds;
                }
            }
            if(writeNew)
            {
                main.users.Add(i);
            }
            //Save back
            string raw = Tools.SerializeObject(main);
            System.IO.File.WriteAllText(Program.savePath + "linked.list", raw);
        }

        public static ulong GetDiscordUserIdBySteamName(string query)
        {
            ArkLinkList main = Load();
            int i = 0;
            //Console.WriteLine("|||||||||||||||||");
            //Console.WriteLine(main.users[0].arkIds + " " + main.users[1].arkIds);
            while (i<main.users.Count)
            {
                //Console.WriteLine("");
                //Console.WriteLine(main.users[i].steamIds + "|");
                //Console.WriteLine(main.users[i].arkIds + "|");
                //Console.WriteLine(query + "|");

                //Console.WriteLine("");
                if (main.users[i].steamIds.Trim(' ') == query.Trim(' ') || main.users[i].arkIds.Trim(' ') == query.Trim(' '))
                {
                    //Console.WriteLine("RETURNING FOR " + query + " " + main.users[i].discordId.ToString());
                    return main.users[i].discordId;
                }
                i++;
            }
            throw new Exception("Not found!");
        }
    }
    
    [DataContract]
    class ArkLinkItem
    {
        [DataMember]
        public string steamIds;
        [DataMember]
        public ulong discordId;
        [DataMember]
        public string arkIds;

        public ArkLinkItem()
        {

        }
        public ArkLinkItem(string _steam, ulong _discord, string _arkIds)
        {
            discordId = _discord;
            steamIds = _steam;
            arkIds = _arkIds;
        }
    }
}
