using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using SteamKit2;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace SteamMultiAccount
{
    public partial class SMAForm : Form
    {
        internal const string ConfigDirectory = "config/";
        internal const string DebugDirectory = "debug/";
        internal const string ServerList = ConfigDirectory + "/servers.bin";
        internal const string BotsData = ConfigDirectory + "/botData";
        private bool bWantClose = false;
        private FormWindowState _LastState = FormWindowState.Normal;
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

            textBoxCommandLine.AutoCompleteCustomSource.AddRange(Bot.CommandsKeys);
            notifyIconMain.Icon = System.Drawing.SystemIcons.Application;

            // TODO: Getting game from gleam.io
        }

        private async Task StartBots()
        {
            if (!Directory.Exists(ConfigDirectory))
                return;
            if(Directory.GetFiles(ConfigDirectory,"*.json").Length > 0)
            {
                foreach (var configFile in Directory.EnumerateFiles(ConfigDirectory, "*.json"))
                {
                    string botName = Path.GetFileNameWithoutExtension(configFile);
                    switch (botName)
                    {
                        case "Program":
                            continue;
                    }
                    if (botName == null)
                        return;
                    

                    Bot bot = new Bot(botName, this);
                    BotList.BeginUpdate();
                    try
                    {
                        BotList.Invoke(new MethodInvoker(delegate
                        {
                            BotList.Items.Add(botName);
                        }));
                    } catch(Exception e)
                    {
                        Logging.LogToFile("Cant add bot to bot list: " + e);
                    }
                    BotList.EndUpdate();

                    if (BotList.SelectedIndex == -1) BotList.SelectedIndex = 0; // Select first element
                    if (bot.BotConfig.Enabled) await Task.Delay(5000); // Wait 5 sec before start next bot
                }
            }
        }
        /*
         * 
         * Events
         * 
         */
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!bWantClose)
                {
                    e.Cancel = true; // Cancel if user click on X button
                    this.Hide();
                    _LastState = this.WindowState;
                }
            }
            base.OnFormClosing(e);
        }
        protected override void OnShown(EventArgs e)
        {
            labelCardsSelling.Text = "";
            labelFarming.Text = "";
            base.OnShown(e);
        }
        private async void SMAForm_Shown(object sender, EventArgs e)
        {
            await StartBots();
        }
        private void LogBox_Click(object sender, EventArgs e)
        {
            if (LogBox.SelectionLength > 0)
                return;
            textBoxCommandLine.Select();
        }
        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BotSettings SettingsForm = new BotSettings(this);
            if(!SettingsForm.wantclose)
            SettingsForm.Show();
        }
        private void BotList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int y = e.Y / ((ListBox) sender).ItemHeight;
                if (y < ((ListBox) sender).Items.Count)
                { 
                    ((ListBox) sender).SelectedIndex = y;
                    contextMenuStripMain.Items[1].Visible = true;
                    contextMenuStripMain.Items[2].Visible = true;
                }
                else
                { 
                    ((ListBox) sender).SelectedIndex = -1;
                    contextMenuStripMain.Items[1].Visible = false;
                    contextMenuStripMain.Items[2].Visible = false;
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

            UpdateAll(bot);
        }
        private void textBoxCommandLine_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (string.IsNullOrEmpty(textBoxCommandLine.Text))
                    return;
                Bot bot;
                if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                    return;
                bot.Log(textBoxCommandLine.Text, LogType.User);
                bot.Response(textBoxCommandLine.Text);
                UpdateLogBox(bot);
                textBoxCommandLine.Text = string.Empty;
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
        private void timerBotUpdate_Tick(object sender, EventArgs e)
        {
            if (BotList.SelectedItem == null)
                return;

            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                return;
            
            UpdateAll(bot);
        }
        /*
        *
        * Services
        *
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
            if(bot.Status == StatusEnum.Farming)
            { 
                //string text = $"Farming {bot.CurrentFarming.Count} of {bot.GetGamesToFarmCount} games.";
                //if (labelFarming.Text != text)
                    //labelFarming.Text = text;
            }
            if (StatusLabel.Text != Bot.StatusString[(int)bot.Status])
                StatusLabel.Text = Bot.StatusString[(int)bot.Status];
        }
        internal void UpdateWallet(Bot bot)
        {
            if (bot == null || bot.Status == StatusEnum.Disabled || bot.Status == StatusEnum.Connecting || !bot.initialized)
            {
                labelWallet.Text = string.Empty;
                return;
            }
            string walletInfo;
            if (!bot.Wallet.HasWallet)
                walletInfo = "Wallet: dont have";
            else
                walletInfo = "Wallet: " + (float)bot.Wallet.Balance/100 + " " + bot.Wallet.Curency;
            if (labelWallet.Text != walletInfo)
                labelWallet.Text = walletInfo;
        }
        private void UpdateAll(Bot bot)
        {
            UpdateLogBox(bot);
            UpdateStatus(bot);
            UpdateWallet(bot);
            CheckButtonsStatus(bot);
        }
        internal void CheckButtonsStatus(Bot bot)
        {
            if (bot == null)
            {
                buttonConnect.Visible = false;
                ModulePanelFarm.Visible = false;
                modulePanelCardsSelling.Visible = false;
                buttonFarm.Visible = false;
                return;
            }
            buttonConnect.Visible = true;

            bool isReady = (bot.Status != StatusEnum.Connecting && bot.initialized);
            bool isConnected = (isReady && bot.Status != StatusEnum.Disabled && bot.steamClient.IsConnected);
            bool isLoggedIn = (bot.steamClient.IsConnected && bot.steamClient.SteamID?.AccountType == EAccountType.Individual);
            bool isFarming = (bot.Status == StatusEnum.Farming);

            ModulePanelFarm.Visible = isLoggedIn;
            modulePanelCardsSelling.Visible = isLoggedIn;
            modulePanelGiveaways.Visible = isLoggedIn;
            labelCardsSelling.Visible = !(string.IsNullOrEmpty(labelCardsSelling.Text));
            labelFarming.Visible = !(string.IsNullOrEmpty(labelFarming.Text));

            buttonConnect.Enabled = isReady;
            buttonFarm.Enabled = (bot.Status != StatusEnum.RefreshGamesToFarm);

            buttonFarm.Text = (bot.CurrentFarming.Any()) ? "Stop farm" : "Farm";
            buttonConnect.Text = (isLoggedIn) ? "Disconnect" : "Connect";
        }
        public void BotListAdd(string botName)
        {
            if (string.IsNullOrEmpty(botName))
                return;
            BotList.Items.Add(botName);
        }
        private void buttonFarm_Click(object sender, EventArgs e)
        {
            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(),out bot))
                return;
            
            bot.PauseResumeFarm();
        }
        private void OnOffButton_Click(object sender, EventArgs e)
        {
            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                return;
            bot.PauseResume();
        }

        private void closeToolStripMenuItemClose_Click(object sender, EventArgs e)
        {
            bWantClose = true;
            this.Close();
        }
        private void notifyIconMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = _LastState;
            this.Activate();
            this.Show();
        }

        private async void buttonStylizedSellCards_Click(object sender, EventArgs e)
        {
            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                return;
            (sender as Control).Enabled = false;
            await bot.Sellcards().ConfigureAwait(false);
            (sender as Control).Enabled = true;
        }

        private void buttonStylizedGiveawaysOpen_Click(object sender, EventArgs e)
        {
            Bot bot;
            if (!Bot.Bots.TryGetValue(BotList.SelectedItem.ToString(), out bot))
                return;
            GiveawayForm giveawayForm = new GiveawayForm(this, bot.webBot);
            giveawayForm.Show();
        }
    }
    class ModulePanel : FlowLayoutPanel
    {
        private string moduleName;
        private int nameMargin;
        [Category("Appearance")]
        public string ModuleName { get { return moduleName; } set { moduleName = value; Invalidate(); } }
        [Category("Appearance")]
        public int NameMargin { get { return nameMargin; } set { nameMargin = value; Invalidate(); } }
        public ModulePanel()
        {
            nameMargin = 2;
            this.Padding = new Padding(0, 10, 0, 0);
        }
        protected override void OnPaddingChanged(EventArgs e)
        {
            this.Padding = new Padding(
                this.Padding.Left,
                20,
                this.Padding.Right,
                this.Padding.Bottom);
            base.OnPaddingChanged(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Brush textBrush = new SolidBrush(Color.FromArgb(255, 20, 20, 20));
            Font textFont = new Font("Segoe UI", 10F);
            SizeF measureString = e.Graphics.MeasureString(moduleName, textFont);
            e.Graphics.DrawString(moduleName, textFont, textBrush, new PointF(10, 0));
            Brush lineBrush = new SolidBrush(SystemColors.ActiveBorder);
            Pen linePen = new Pen(lineBrush);
            e.Graphics.DrawLine(linePen, new PointF(0, measureString.Height / 2), new PointF(12 - nameMargin, measureString.Height / 2));
            e.Graphics.DrawLine(linePen, new PointF(measureString.Width + 4 + nameMargin, measureString.Height/2), new PointF(e.ClipRectangle.Width, measureString.Height / 2));
        }
    }
    class ButtonStylized:Button
    {
        public ButtonStylized()
        {
            this.Margin = new Padding(3, 2, 3, 0);
            this.FlatStyle = FlatStyle.Flat;
            this.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.BackColor = System.Drawing.Color.FromArgb(255,250, 250, 250);
            this.FlatAppearance.BorderSize = 1;
            this.FlatAppearance.BorderColor = System.Drawing.SystemColors.ActiveBorder;
            this.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(255, 220, 220, 220);
            this.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(255, 240, 240, 240);
            this.SetStyle(ControlStyles.Selectable, false);
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
            Logging.DebugLogToFile(DateTime.Now + " [" + category + "]: " + message);
        }
    }
}
