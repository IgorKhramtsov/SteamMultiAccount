using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;

namespace SteamMultiAccount
{
    public partial class BotSettings : Form
    {
        public bool wantclose = false;
        private SMAForm _mainForm;
        private string _botName;
        private bool botCreated;
        private ConfigPropertyGrid propertyGrid;
        public BotSettings(SMAForm mainForm, string BotName = "")
        {
            _mainForm = mainForm;
            _botName = BotName;
            if (string.IsNullOrEmpty(_botName))
                GetBotname();

            InitializeComponent();
            Init();
        }
        private void GetBotname()
        {
            if (!UserInput.input(out _botName, Program.InputType.BotName))
            {
                wantclose = true;
                Close();
                return;
            }
            else
            {
                if (!File.Exists(Path.Combine(SMAForm.ConfigDirectory, _botName)))
                    botCreated = true;
                else if (MessageBox.Show("Bot with this name already exist, do u wanna edit this bot?", "Bot already exist", MessageBoxButtons.YesNo) == DialogResult.No)
                    GetBotname();
            }
        }
        internal void Init()
        {
            if (string.IsNullOrWhiteSpace(_botName))
                return;
            string path = Path.Combine(SMAForm.ConfigDirectory, _botName);
            var botConfig = new BotConfig(path);
            botConfig = botConfig.Load();
            if (string.IsNullOrWhiteSpace(botConfig.Login))
                botConfig.Login = _botName;
            propertyGrid = new ConfigPropertyGrid(botConfig);
            Controls.Add(propertyGrid);
            if (botCreated)
                _mainForm.BotListAdd(_botName);
        }

        private void BotSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (string.IsNullOrEmpty(_botName))
                return;

            if (botCreated)
                new Bot(_botName, _mainForm);
            else if (propertyGrid.somethingChange)
            {
                Bot bot;
                if (Bot.Bots.TryGetValue(_botName, out bot))
                    bot.Restart();
            }
        }
    }

    internal sealed class ConfigPropertyGrid : PropertyGrid
    {
        private readonly Config config;
        public bool somethingChange;
        internal ConfigPropertyGrid(Config conf)
        {
            if (conf == null)
                return;
            config = conf;

            SelectedObject = config;
			Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
			Dock = DockStyle.Fill;
			HelpVisible = false;
			ToolbarVisible = false;
        }

        protected override void OnPropertyValueChanged(PropertyValueChangedEventArgs e)
        {
            if (e.OldValue == e)
                return;
            base.OnPropertyValueChanged(e);
            somethingChange = true;
            config.Save();
        }
    }
}
