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
                if (!UserInput.input(out _botName, Program.InputType.BotName))
                {
                    wantclose = true;
                    Close();
                    Dispose();
                    return;
                }
                else
                    botCreated = true;
            
            InitializeComponent();
            Init();
        }
        internal void Init()
        {
            string path = System.IO.Path.Combine(SMAForm.ConfigDirectory, _botName);
            var botConfig = new BotConfig(path);
            botConfig = botConfig.Load();
            propertyGrid = new ConfigPropertyGrid(botConfig);
            Controls.Add(propertyGrid);
            if(botCreated)
            _mainForm.BotListAdd(_botName);
        }

        private void BotSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (string.IsNullOrEmpty(_botName))
                return;
            if(botCreated)
                new Bot(_botName);
            else if(propertyGrid.somethingChange)
            { 
            Bot bot;
            if (!Bot.Bots.TryGetValue(_botName, out bot))
                return;
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
