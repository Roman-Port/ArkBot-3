using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    class Entity_ArkMessage
    {
        public string username;
        public string steamName;
        public string content;

        public static Entity_ArkMessage ParseSingleMessage(string raw)
        {
            try
            {
                string header = raw.Substring(0, raw.Split(':')[0].Length);
                string content = raw.Substring(raw.Split(':')[0].Length+2);
                Entity_ArkMessage message = new Entity_ArkMessage();
                message.content = content;
                string[] para = header.Split('(');
                string user = header.Substring(header.Length-1-para[para.Length-1].Length).Trim('(').Trim(')');
                string sn = header.Substring(0, header.Length - 1 - para[para.Length - 1].Length);
                message.username = user;
                message.steamName = sn;
                return message;
            } catch
            {
                return null; //Parse error.
            }
        }

        public override string ToString()
        {
            //Convert this to a string for Discord
            string msg = "["+username+"] "+"\r\n"+content;
            return msg;
        }

        public static Entity_ArkMessage[] ParseMultipleMessages(string raw)
        {

            string[] lines = Tools.SplitRconByLine(raw);



            List<Entity_ArkMessage> messages = new List<Entity_ArkMessage>();
            //Go through each line and parse it.
            foreach(string line in lines)
            {
                if (line.Length < 3)
                    continue;
                Entity_ArkMessage msg = ParseSingleMessage(line);
                if (msg != null)
                    messages.Add(msg);
            }
            return messages.ToArray();
        }
    }
}
