using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.IO;

namespace SteamMultiAccount
{
    internal class Bot
    {
        private const ushort CallbackSleep = 500;

        internal static readonly Dictionary<string,Bot> Bots = new Dictionary<string,Bot>();
        internal readonly string BotName;
        internal readonly string BotPath;
        internal readonly Config BotConfig;
        internal readonly Loging logging;
        private readonly System.Windows.Forms.RichTextBox _logbox;

        internal Bot(string botName)
        {
            if (string.IsNullOrEmpty(botName))
                return;
            BotName = botName;
            BotPath = Path.Combine(SMAForm.ConfigDirectory, BotName);

            _logbox = new System.Windows.Forms.RichTextBox();
            logging = new Loging(_logbox);
            BotConfig = new Config(BotPath);

            BotConfig = BotConfig.Load(logging);

            if (Bots.ContainsKey(BotName))
                return;
            Bots[BotName] = this;
            logging.LogInfo("{" + BotName + "}" + " Succesfully load");
        }
        internal void Response(string message)
        {

        }



        internal System.Windows.Forms.RichTextBox getLogBox()
        {
            return _logbox;
        }
        internal void Delete()
        {
            Bots.Remove(BotName);
        }
    }

}
