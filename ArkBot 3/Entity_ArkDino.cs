using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    class Entity_ArkDino
    {
        public string name;
        public int level;
        public string dinoClass;
        public string tribe;
        public bool player;

        public Entity_ArkDino(string raw, bool isPlayer)
        {
            //Parse
            try
            {
                //Find where the dash - is. This is between the name and the meta
                int dash = raw.IndexOf('-');
                name = raw.Substring(0, dash - 1); //Set name
                string meta = raw.Substring(dash + 1);
                //The meta var will look something like this: Lvl 36 (Carnotaurus) (The NSA)
                string[] keys = Tools.ParseParentheses(meta); //Split by parathns
                string levelString = keys[0];
                //Parse this to an int, but trim the first four chars
                bool parseOk = int.TryParse(levelString.Substring(4), out level);
                if (!parseOk)
                    level = -1;
                //The next bit is different if you're a dino or a player.
                int len = keys.Length;
                if (keys[keys.Length - 1].Length < 2)
                    len--;
                player = len <= 2;
                if (player)
                {
                    //The only thing here is the tribe.
                    tribe = keys[1];
                    //Done
                }
                else
                {
                    dinoClass = keys[1];
                    if (keys.Length > 2)
                        tribe = keys[2];
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(raw);
            }
        }

        public Entity_ArkDino(string _name)
        {
            name = _name;
        }

        public Entity_ArkDino()
        {

        }

        public string ToNiceString()
        {
            if(player)
            {
                if (tribe.Length < 1)
                    tribe = "wild";
                return name + " (" + tribe + ")";
            }
            return tribe + "'s " + dinoClass;
        }

        public string ToLongString()
        {
            string msg = "";
            if (name != null)
                msg += "Name: " + name+"\r\n";
            if (tribe != null)
                msg += "Tribe: " + tribe + "\r\n";
            if (level != -1)
                msg += "Level " + level.ToString() + "\r\n";
            if (dinoClass != null)
                msg += dinoClass + "\r\n";
            return msg;
        }

        public string TempString()
        {
            return "Name: "+name+"\r\nLevel: "+level.ToString()+"\r\nDino Class: "+dinoClass+"\r\nTribe: "+tribe.ToString()+"\r\nIsPlayer: "+player.ToString();
        }
    }
}
