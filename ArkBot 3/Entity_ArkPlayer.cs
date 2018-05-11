using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    class Entity_ArkPlayer
    {
        public string name;
        public string id;
        public Entity_ArkPlayerStatus status;

        public static Entity_ArkPlayer ParsePlayerList(string raw, Entity_ArkPlayerStatus status)
        {
            raw = raw.Trim(' ');
            string[] s = raw.Split(',');
            Entity_ArkPlayer player = new Entity_ArkPlayer();
            player.name = s[0].Trim(' ');
            player.id = s[1].Trim(' ');
            player.status = status;
            return player;
        }
    }

    enum Entity_ArkPlayerStatus
    {
        Joined,
        Offline,
        Leaving,
        Joining
    }
}
