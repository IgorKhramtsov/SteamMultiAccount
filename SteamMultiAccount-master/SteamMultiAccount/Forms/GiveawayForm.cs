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
            listBoxSites.Items.Add(bot.gameminerBot);
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

            switch(listBoxSites.SelectedItem.ToString())
            {
                case GameminerBot.Name:
                    if (bot.gameminerBot.isRunning)
                    {
                        bot.gameminerBot.Stop();
                        (sender as Button).Text = "Start";
                    }
                    else
                    {
                        bot.gameminerBot.Run(this).Forget();
                        (sender as Button).Text = "Stop";
                    }
                    break;
            }
        }
    }
}
