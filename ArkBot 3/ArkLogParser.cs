using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    class ArkLogMsg
    {
        public Entity_ArkDino actioner; //Entity that caused an action
        public Entity_ArkDino target; //Target of the action. May be null.
        public string time;
        public ArkLogMsgType type;
        public string messageContent;
        public string messageSteamName;
        public string messageIngameName;
    }

    enum ArkLogMsgType
    {
        Death_KilledBy,
        Death_Generic,
        NewTame,
        DisconnectJoin,
        Chat,
        Unknown
    }

    class ArkLogParser
    {
        public static ArkLogMsg ParseMessage(string raw)
        {
            try
            {
                //First, extract the data, the time, and the header.
                string nonHeader = raw;//.Substring(30);
                ArkLogMsg msg = new ArkLogMsg();
                msg.time = nonHeader.Substring(0, 19);
                string content = nonHeader.Substring(21);

                //From here, work to identify what type of message this is.
                msg.type = DetermineMsg(content);

                //Deal with each
                switch (msg.type)
                {
                    case ArkLogMsgType.Death_KilledBy:
                        DealWith_Death_KilledBy(msg, content);
                        break;
                    case ArkLogMsgType.Death_Generic:
                        DealWith_Death_Generic(msg, content);
                        break;
                    case ArkLogMsgType.NewTame:
                        DealWith_Tame(msg, content);
                        break;
                    case ArkLogMsgType.Chat:
                        DealWith_Death_Chat(msg, content);
                        break;
                }
                return msg;
            } catch
            {
                return null;
            }


            
        }


        private static ArkLogMsgType DetermineMsg(string content)
        {
            //Check if something was killed by something.
            bool wasKilledBy = content.Contains(" was killed by ");
            if (wasKilledBy)
                return ArkLogMsgType.Death_KilledBy;
            if(content.Contains(" was killed!"))
            {
                return ArkLogMsgType.Death_Generic;
            }
            if(content.Contains(" joined this ARK!") || content.Contains(" left this ARK!"))
            {
                //This was someone leaving and joining
                return ArkLogMsgType.DisconnectJoin;
            }
            if(content.Contains(" Tamed a "))
            {
                return ArkLogMsgType.NewTame;
            }
            if(content.Contains("): "))
            {
                //Chat syntax
                return ArkLogMsgType.Chat;
            }

            //Default
            return ArkLogMsgType.Unknown;
        }

        private static void DealWith_Death_Chat(ArkLogMsg msg, string content)
        {
            //Sandwichman12 (Sandwichman): uUxWU3t7
            msg.messageSteamName = content.Split('(')[0];
            msg.messageIngameName = content.Split('(')[1].Substring(content.Split('(')[1].IndexOf(')'));
            msg.messageContent = content.Substring(content.IndexOf("): "));
        }

        private static void DealWith_Death_KilledBy(ArkLogMsg msg, string content)
        {
            //Parse each target.
            string killedByString = " was killed by ";
            int wasKilledBy = content.IndexOf(killedByString);
            string rawTargetOne = content.Substring(0, wasKilledBy);
            string rawTargetTwo = content.Substring(wasKilledBy + killedByString.Length);

            //Parse
            msg.target = new Entity_ArkDino(rawTargetOne, false);
            msg.actioner = new Entity_ArkDino(rawTargetTwo, false);
            
            //Done!
        }

        private static void DealWith_Death_Generic(ArkLogMsg msg, string content)
        {
            string raw = content.Substring(0,content.IndexOf(" was killed!"));
            msg.target = new Entity_ArkDino(raw, false);
            //Done
        }

        private static void DealWith_Tame(ArkLogMsg msg, string content)
        {
            string tribeRaw = content.Substring(content.IndexOf(" of Tribe ")+ " of Tribe ".Length);
            string tribe = tribeRaw.Substring(0, tribeRaw.IndexOf(" Tamed a "));
            string raw = content.Substring(content.IndexOf(" Tamed a ")+ " Tamed a ".Length).TrimEnd('!');
            string name = content.Substring(content.IndexOf(" of Tribe"));

            //Parse
            msg.target = new Entity_ArkDino(raw,false);
            msg.actioner = new Entity_ArkDino();
            msg.actioner.tribe = tribe;
            msg.actioner.name = name;
        }
    }
}
