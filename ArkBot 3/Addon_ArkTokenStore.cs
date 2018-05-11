using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    class Addon_ArkTokenStore
    {
        public static async Task MainCmd(string cmd, DSharpPlus.EventArgs.MessageCreateEventArgs e,DiscordUser user)
        {
            if(cmd.Length<2)
            {
                //Main
                await StoreFrontMain(e, user);
                return;
            }
        }

        private static Addon_ArkTokenStore_Item[] store = {new Addon_ArkTokenStore_Item("Daytime",1000,"Sets the time in Ark today. It's like magic, but you pay for it!","","SetTimeOfDay 9:00") };

        private static float priceMult = 3;

        public static async Task StoreFrontMain(DSharpPlus.EventArgs.MessageCreateEventArgs e, DiscordUser user)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.ThumbnailUrl = "https://romanport.com/static/ArkBot/assets/store/shopkeeper/shopkeeper_main.png";
            embed.Title = "Aye kid, what're you in the business for?";
            embed.Description = "You've got " + user.arkPoints + " Ark tokens. If you don't have enough tokens, then scram!";
            embed.Footer = new DiscordEmbedBuilder.EmbedFooter();
            embed.Footer.Text = "Seems a bit shady, " + e.Author.Username + ". Maybe we should go away?";

            //Add content
            foreach(Addon_ArkTokenStore_Item item in store )
            {
                embed.AddField("[Item] "+item.name, "**%store buy "+item.name.ToLower()+"**\r\n" + (item.price * priceMult).ToString() + " tokens\r\n" + item.description);
            }
            
            //Send
            await e.Message.RespondAsync(embed: embed);
        }


    }

    class Addon_ArkTokenStore_Item
    {
        public string name;
        public float price;
        public string description;
        public string url;
        public string rconCommand;

        public Addon_ArkTokenStore_Item(string _name, float _price, string _descrip, string _url, string _rcon)
        {
            rconCommand = _rcon;
            url = _url;
            description = _descrip;
            price = _price;
            name = _name;
        }
    }
}
