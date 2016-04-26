using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using SteamKit2;

namespace SteamMultiAccount
{
    public partial class SMAForm : Form
    {
        internal const string ConfigDirectory = "config";
        internal const string DebugDirectory = "debug";
        internal const string ServerLists = ConfigDirectory + "/servers.json";
        internal const string BotsData = ConfigDirectory + "/botData";
        public SMAForm()
        {
            InitializeComponent();
            StatusLabel.Text = string.Empty;
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);
            if (Directory.Exists(DebugDirectory)) {
                Directory.Delete(DebugDirectory, true);
                Thread.Sleep(1000); // Dirty workaround giving Windows some time to sync
            }
            if (!Directory.Exists(DebugDirectory))
                Directory.CreateDirectory(DebugDirectory);
            if (!Directory.Exists(BotsData))
                Directory.CreateDirectory(BotsData);

            DebugLog.AddListener(new Listener(null));
            DebugLog.Enabled = true;

            textBox1.AutoCompleteCustomSource.AddRange(Bot.Commands.Keys.ToArray());

            CheckBots();//Looking for bot configs
            if (BotList.Items.Count > 0) BotList.SelectedIndex = 0;//Select first element in list
        }

        private void CheckBots()
        {
            if (!Directory.Exists(ConfigDirectory))
                return;
            if(Directory.GetFiles(ConfigDirectory,"*.json").Length>0)
            {
                BotList.BeginUpdate();
                foreach (var configFile in Directory.EnumerateFiles(ConfigDirectory, "*.json"))
                {
                    string botName = Path.GetFileNameWithoutExtension(configFile);
                    switch (botName)
                    {
                        case "servers":
                            continue;
                    }
                    if (botName == null)
                        return;
                    Bot bot = new Bot(botName);
                    BotList.Items.Add(botName);
                }
                BotList.EndUpdate();
            }
        }
        private void LogBox_Click(object sender, EventArgs e)
        {
            if (LogBox.SelectionLength > 0)
                return;
            textBox1.Select();
        }
        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BotSettings SettingsForm = new BotSettings(this);
            if(!SettingsForm.wantclose)
            SettingsForm.Show();
        }
        private void BotList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                int y = e.Y / (sender as ListBox).ItemHeight;
                if (y < (sender as ListBox).Items.Count)
                { 
                    (sender as ListBox).SelectedIndex = y;
                    contextMenuStrip1.Items[1].Visible = true;
                    contextMenuStrip1.Items[2].Visible = true;
                }
                else
                { 
                    (sender as ListBox).SelectedIndex = -1;
                    contextMenuStrip1.Items[1].Visible = false;
                    contextMenuStrip1.Items[2].Visible = false;
                }
            }
        }
        private void changeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BotSettings SettingForm = new BotSettings(this,BotList.SelectedItem.ToString());
            SettingForm.Show();
        }
        private void BotList_SelectedIndexChanged(object sender, EventArgs e)
        {
            Bot bot;
            if ((sender as ListBox).SelectedIndex == -1)
                bot = null;
            else            
            Bot.Bots.TryGetValue((sender as ListBox).SelectedItem.ToString(), out bot);

            UpdateLogBox(bot);
            UpdateStatus(bot);
            CheckButtonsStatus(bot);
        }
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (string.IsNullOrEmpty(textBox1.Text))
                    return;
                Bot bot;
                if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                    return;
                bot.Log(textBox1.Text, LogType.User);
                bot.Response(textBox1.Text);
                UpdateLogBox(bot);
                textBox1.Text = string.Empty;
            }
        }
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                return;
            bot.Delete();
            bot = null;
            BotList.Items.Remove(BotList.SelectedItem);
            if (BotList.Items.Count > 0)
                BotList.SelectedIndex = 0;
            else
                BotList.SelectedIndex = -1;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (BotList.SelectedItem == null)
                return;

            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                return;

            UpdateLogBox(bot);
            UpdateStatus(bot);
            CheckButtonsStatus(bot);
        }

        /*
        //
        // Services
        //
        */
        internal void UpdateLogBox(Bot bot)
        {
            if (bot == null)
            {
                LogBox.Clear();
                return;
            }
            string text = bot.getLogBoxText();
            if (LogBox.Rtf == text)
                return;
            LogBox.Rtf = text;
            LogBox.SelectionStart = LogBox.Text.Length;
            LogBox.ScrollToCaret();
        }
        internal void UpdateStatus(Bot bot)
        {
            if (bot == null)
            { 
                StatusLabel.Text = string.Empty;
                return;
            }
            if (StatusLabel.Text == bot.Status)
                return;
            StatusLabel.Text = bot.Status;
        }
        public void BotListAdd(string botName)
        {
            if (string.IsNullOrEmpty(botName))
                return;
            BotList.Items.Add(botName);
        }
        internal void CheckButtonsStatus(Bot bot)
        {
            if (bot == null)
                return;
            //if (bot.isRunning && bot.steamClient.SteamID != null && bot.steamClient.SteamID.AccountType == EAccountType.Individual)
                //ChatButton.Visible = true;
            //else
                //ChatButton.Visible = false;
        }
    }

    class Listener : IDebugListener
    {
        internal static bool NetHookAlreadyInitialized { get; set; } = false;
        private string FilePath;
        internal Listener(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            FilePath = filePath;
        }
        public void WriteLine(string category, string message)
        {
            Loging.DebugLogToFile(DateTime.Now + " [" + category + "]: " + message);
        }
    }
}
