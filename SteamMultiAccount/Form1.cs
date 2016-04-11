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

namespace SteamMultiAccount
{
    public partial class SMAForm : Form
    {
        internal const string ConfigDirectory = "config";
        internal const string DebugDirectory = "debug";
        public SMAForm()
        {
            InitializeComponent();//Инициализация компонентов формы 
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);
            if (!Directory.Exists(DebugDirectory))
                Directory.CreateDirectory(DebugDirectory);
            CheckBots();//Ищем конфиг файлы ботов
            if (BotList.Items.Count > 0) BotList.SelectedIndex = 0;//Выбор первого элемента в списке
        }

        private void CheckBots()
        {
            if (!Directory.Exists(ConfigDirectory))
                return;
            if(Directory.GetFiles(ConfigDirectory,"*.json").Length>0)
            foreach (var configFile in Directory.EnumerateFiles(ConfigDirectory, "*.json"))
            {
                string botName = Path.GetFileNameWithoutExtension(configFile);
                if (botName == null)
                    return;
                Bot bot = new Bot(botName);
                BotList.Items.Add(botName);
            }
        }

        private void LogBox_Click(object sender, EventArgs e)
        {
            textBox1.Select();
        }

        private void LogBox_Click(object sender, MouseEventArgs e)
        {
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
            if ((sender as ListBox).SelectedIndex == -1)
                LogBox.Clear();
            else
            UpdateLogBox();
        }

        public void BotListAdd(string botName)
        {
            if (string.IsNullOrEmpty(botName))
                return;
            BotList.Items.Add(botName);
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
                bot.Response(textBox1.Text);
                bot.logging.LogUser(textBox1.Text);
                UpdateLogBox();
                textBox1.Text = string.Empty;
            }
        }

        private void UpdateLogBox()
        {
            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                return;
            LogBoxReplace(bot.getLogBox(), LogBox);
        }

        public static void LogBoxReplace(RichTextBox first, RichTextBox second)
        {
            string file = Path.GetTempFileName();
            first.SaveFile(file);
            second.LoadFile(file);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                return;
            bot.BotConfig.Delete();
            bot.Delete();
            bot = null;
            BotList.Items.Remove(BotList.SelectedItem);
            if (BotList.Items.Count > 0)
                BotList.SelectedIndex = 0;
            else
                BotList.SelectedIndex = -1;
        }
    }
}
