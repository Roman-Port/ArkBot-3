using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot_3
{
    class Tools
    {
        //Collection of tools
        public static bool CompareAuth(DiscordUserPermissionLevel user, DiscordUserPermissionLevel source)
        {
            //Returns true/false if the user meets the permission requirements.
            int userInt = (int)user;
            int sourceInt = (int)source;
            return userInt >= sourceInt;
        }

        public static bool TryParseYesNo(string msg, out bool output)
        {
            output = false;
            msg = msg.ToLower();
            //Try parsing with the system first.
            if (bool.TryParse(msg, out output))
            {
                //Cool. System parsed it. return true.
                return true;
            }
            //Try parsing y/n and yes/no
            if (msg == "y" || msg == "yes")
            {
                //yes
                output = true;
                return true;
            }
            if (msg == "n" || msg == "no")
            {
                output = false;
                return true;
            }
            //No idea. Return false
            return false;
        }

        public static string GetPermLevelsString()
        {
            //Just print permission levels
            return "[Anyone / User / Admin / Owner / Nobody]";
        }

        public static bool TryParsePermLevel(string msg, out DiscordUserPermissionLevel output)
        {
            output = DiscordUserPermissionLevel.none;
            return Enum.TryParse<DiscordUserPermissionLevel>(msg.ToLower(), out output);
        }

        public static object DeserializeObject(string value, Type objType)
        {
            try
            {
                //Get a data stream
                MemoryStream mainStream = GenerateStreamFromString(value);

                DataContractJsonSerializer ser = new DataContractJsonSerializer(objType);
                //Load it in.
                mainStream.Position = 0;
                var obj = ser.ReadObject(mainStream);
                return Convert.ChangeType(obj, objType);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Deserialization error.");
            }
            return null;
        }

        public static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        public static string SerializeObject(object obj)
        {
            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(obj.GetType());
            ser.WriteObject(stream1, obj);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            return sr.ReadToEnd();
        }

        public static DiscordEmbedBuilder GenerateEmbed(string title, string footerText, string description, DiscordColor color, string footerUrl = "", string headerUrl = "")
        {
            var footer = new DiscordEmbedBuilder.EmbedFooter();
            footer.Text = footerText;
            var embed = new DiscordEmbedBuilder
            {
                Title = title,
                Footer = footer,
                Description = description,
                Color = color
            };
            if (footerUrl != "")
                embed.Footer.IconUrl = footerUrl;
            if (headerUrl != "")
                embed.ImageUrl = headerUrl;
            return embed;
        }

        public static string[] SplitRconByLine(string raw)
        {
            //This is gross. We load a refrence to a new line from a file.
            string newline = System.IO.File.ReadAllText("newlineRef.txt");
            string[] lines = raw.Split(
                new[] { Environment.NewLine, "\r", "\r\n", newline },
                StringSplitOptions.RemoveEmptyEntries
            );
            return lines;
        }

        public static bool WhitelistSafeText(string input,out string bad)
        {
            char[] allowed = "abcdefghijklmnopqrstuvwxyz1234567890,.?'\"!@#$%^&*()-=_+`~ ABCDEFGHINKLMNOPQRSTUVWXYZ".ToCharArray();
            //Loop through string
            foreach(char c in input)
            {
                //Check if it's OK.
                bool cOkay = allowed.Contains(c);
                if(!cOkay)
                {
                    //Not okay! Tell the user.
                    bad = c.ToString();
                    return false;
                }
            }
            //Ok
            bad = "";
            return true;
        }

        public static string GenerateRandomString(int length, Random rand = null)
        {
            if (rand == null)
                rand = new Random();
            char[] c = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHINKLMNOPQRSTUVWXYZ".ToCharArray();
            string o = "";
            while(o.Length<length)
            {
                o += c[rand.Next(0, c.Length)];
            }
            return o;
        }

        public static string[] ParseParentheses(string raw)
        {
            string[] rawValues = raw.Split('(');
            int i = 0;
            while(i<rawValues.Length)
            {
                string[] split = rawValues[i].Split(')');
                if (split.Length > 1)
                {
                    int endLength = rawValues[i].Length - split[split.Length - 1].Length-1;
                    rawValues[i] = rawValues[i].Substring(0, endLength);
                }
                i++;
            }
            return rawValues;
        }

        public static void DumpException(Exception ex)
        {
            throw ex;
        }

        public static DiscordEmbedBuilder DumpEmbedException(Exception ex, bool writeToConsole = false)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Fatal Error Dump (Exception)";
            embed.AddField("Message", ex.Message, false);
            embed.AddField("Stack Trace", ex.StackTrace, false);
            embed.AddField("Source", ex.Source, false);
            embed.AddField("HResult", ex.HResult.ToString(), false);
            embed.Description = "This exception cannot be recovered from. The bot will continue to run, but the action has been stopped.";
            embed.Color = DSharpPlus.Entities.DiscordColor.Red;
            return embed;
        }

        public static DiscordEmbedBuilder[] DumpMultipleEmbedException(Exception ex)
        {
            //Used for long exceptions
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            DiscordEmbedBuilder embedTwo = new DiscordEmbedBuilder();
            embed.Title = "Fatal Error Dump (Exception)";
            embed.AddField("Message", ex.Message, false);
            embedTwo.AddField("Stack Trace", ex.StackTrace, false);
            embed.AddField("Source", ex.Source, false);
            embed.AddField("HResult", ex.HResult.ToString(), false);
            embed.Description = "This exception cannot be recovered from. The bot will continue to run, but the action has been stopped.";
            embed.Color = DSharpPlus.Entities.DiscordColor.Red;
            embedTwo.Color = DSharpPlus.Entities.DiscordColor.Red;
            return new DiscordEmbedBuilder[] {embed,embedTwo };
        }

        public static string CompareAndUsePlural(string objectName, int count)
        {
            if (count == 1)
                return count.ToString() + " " + objectName;
            return count.ToString() + " " + objectName + "s";
        }
    }
}
