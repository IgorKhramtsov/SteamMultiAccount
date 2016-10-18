using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

namespace SteamMultiAccount
{
    static class Program
    {
        public enum InputType
        {
            BotName
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /* Set embedded browser emulation to IE 10 for ours programm */
            RegistryKey ie_feature_key;
            try { Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"); } catch { }
            ie_feature_key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true);
            string programmName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
            try { ie_feature_key.SetValue(programmName, (int)10001, RegistryValueKind.DWord); } catch { }
            /* Check for dependicies */
            if (!File.Exists("GameEmulator.exe"))
                if (MessageBox.Show("Cant find GameEmulator.exe!") > 0)
                    return;
            if (!File.Exists("CSteamworks.dll"))
                if (MessageBox.Show("Cant find CSteamworks.dll!") > 0)
                    return;
            if (!File.Exists("steam_api.dll"))
                if (MessageBox.Show("Cant find steam_api.dll!") > 0)
                    return;
            if (!File.Exists("SteamKit2.dll"))
                if (MessageBox.Show("Cant find SteamKit2.dll!") > 0)
                    return;
            if (!File.Exists("HtmlAgilityPack.dll"))
                if (MessageBox.Show("Cant find HtmlAgilityPack.dll!") > 0)
                    return;
            if (!File.Exists("Newtonsoft.Json.dll"))
                if (MessageBox.Show("Cant find Newtonsoft.Json.dll!") > 0)
                    return;
            if (!File.Exists("protobuf-net.dll"))
                if (MessageBox.Show("Cant find protobuf-net.dll!") > 0)
                    return;
            if (!File.Exists("SteamKit2.dll"))
                if (MessageBox.Show("Cant find SteamKit2.dll!") > 0)
                    return;
            if (!File.Exists("Steamworks.NET.dll"))
                if (MessageBox.Show("Cant find Steamworks.NET.dll!") > 0)
                    return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SMAForm());

            Environment.Exit(0);
        }
    }
}
