using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SteamMultiAccount
{
    public partial class GiveawayForm : Form
    {
        WebBot bot;
        SMAForm initializerForm;
        public GiveawayForm()
        {
        }
        internal GiveawayForm(SMAForm initializer, WebBot bot)
        {
            this.bot = bot;
            this.initializerForm = initializer;
            
            InitializeComponent();
        }
        protected override void OnShown(EventArgs e)
        {
            listBoxSites.DisplayMember = "Name";
            listBoxSites.ValueMember = "Name";
            listBoxSites.Items.Add(bot.gameminerBot);
            listBoxSites.Items.Add(bot.steamGiftsBot);
            labelStatus.Text = "";
            labelStatus.Visible = false;
            base.OnShown(e);
        }
        internal void UpdateStatus(string status,string Log = "")
        {
            this.labelStatus.Text = status;
            this.labelStatus.Visible = (!string.IsNullOrWhiteSpace(this.labelStatus.Text));
            if (!(string.IsNullOrWhiteSpace(Log)))
                richTextBoxLog.Text = Log;
        }

        private async void buttonStartStop_Click(object sender, EventArgs e)
        {
            if (listBoxSites.SelectedIndex == -1)
                return;

            GiveawayBot selectedBot = null;
            switch(listBoxSites.SelectedItem.ToString())
            {
                case GameminerBot.Name:
                    selectedBot = bot.gameminerBot;
                    break;
                case SteamGiftsBot.Name:
                    selectedBot = bot.steamGiftsBot;
                    break;
            }
            if(selectedBot == null)
                throw new Exception("Cant recognize bot name");

            selectedBot.StartStop(this).Forget();
            this.buttonStartStop.Text = (this.buttonStartStop.Text == "Start" ? "Stop" : "Start");
        }
    }
}
