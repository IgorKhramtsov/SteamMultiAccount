namespace SteamMultiAccount
{
    public partial class SMAForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.BotList = new System.Windows.Forms.ListBox();
            this.contextMenuStripMain = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelRightSide = new System.Windows.Forms.Panel();
            this.LogBox = new System.Windows.Forms.RichTextBox();
            this.textBoxCommandLine = new System.Windows.Forms.TextBox();
            this.panelLeftSide = new System.Windows.Forms.Panel();
            this.flowLayoutPanelPlugins = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanelMain = new System.Windows.Forms.FlowLayoutPanel();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.labelWallet = new System.Windows.Forms.Label();
            this.buttonConnect = new SteamMultiAccount.ButtonStylized();
            this.ModulePanelFarm = new SteamMultiAccount.ModulePanel();
            this.labelFarming = new System.Windows.Forms.Label();
            this.buttonFarm = new SteamMultiAccount.ButtonStylized();
            this.modulePanelCardsSelling = new SteamMultiAccount.ModulePanel();
            this.labelCardsSelling = new System.Windows.Forms.Label();
            this.buttonStylizedSellCards = new SteamMultiAccount.ButtonStylized();
            this.modulePanelGiveaways = new SteamMultiAccount.ModulePanel();
            this.labelGiveawaysStatus = new System.Windows.Forms.Label();
            this.buttonStylizedGiveawaysOpen = new SteamMultiAccount.ButtonStylized();
            this.panelBotListBack = new System.Windows.Forms.Panel();
            this.timerBotUpdate = new System.Windows.Forms.Timer(this.components);
            this.notifyIconMain = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStripNotifyIcon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.closeToolStripMenuItemClose = new System.Windows.Forms.ToolStripMenuItem();
            this.panelMain = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.ComandsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.createBotsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripMain.SuspendLayout();
            this.panelRightSide.SuspendLayout();
            this.panelLeftSide.SuspendLayout();
            this.flowLayoutPanelPlugins.SuspendLayout();
            this.flowLayoutPanelMain.SuspendLayout();
            this.ModulePanelFarm.SuspendLayout();
            this.modulePanelCardsSelling.SuspendLayout();
            this.modulePanelGiveaways.SuspendLayout();
            this.panelBotListBack.SuspendLayout();
            this.contextMenuStripNotifyIcon.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // BotList
            // 
            this.BotList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.BotList.ContextMenuStrip = this.contextMenuStripMain;
            this.BotList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BotList.FormattingEnabled = true;
            this.BotList.IntegralHeight = false;
            this.BotList.ItemHeight = 17;
            this.BotList.Location = new System.Drawing.Point(0, 0);
            this.BotList.Margin = new System.Windows.Forms.Padding(0);
            this.BotList.Name = "BotList";
            this.BotList.Size = new System.Drawing.Size(119, 359);
            this.BotList.Sorted = true;
            this.BotList.TabIndex = 0;
            this.BotList.SelectedIndexChanged += new System.EventHandler(this.BotList_SelectedIndexChanged);
            this.BotList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BotList_MouseDown);
            // 
            // contextMenuStripMain
            // 
            this.contextMenuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.changeToolStripMenuItem,
            this.deleteToolStripMenuItem});
            this.contextMenuStripMain.Name = "contextMenuStrip1";
            this.contextMenuStripMain.Size = new System.Drawing.Size(116, 70);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            // 
            // changeToolStripMenuItem
            // 
            this.changeToolStripMenuItem.Name = "changeToolStripMenuItem";
            this.changeToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.changeToolStripMenuItem.Text = "Change";
            this.changeToolStripMenuItem.Click += new System.EventHandler(this.changeToolStripMenuItem_Click);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // panelRightSide
            // 
            this.panelRightSide.BackColor = System.Drawing.SystemColors.ButtonShadow;
            this.panelRightSide.Controls.Add(this.LogBox);
            this.panelRightSide.Controls.Add(this.textBoxCommandLine);
            this.panelRightSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRightSide.Location = new System.Drawing.Point(0, 25);
            this.panelRightSide.Margin = new System.Windows.Forms.Padding(0);
            this.panelRightSide.MinimumSize = new System.Drawing.Size(432, 360);
            this.panelRightSide.Name = "panelRightSide";
            this.panelRightSide.Padding = new System.Windows.Forms.Padding(287, 1, 0, 0);
            this.panelRightSide.Size = new System.Drawing.Size(724, 360);
            this.panelRightSide.TabIndex = 1;
            // 
            // LogBox
            // 
            this.LogBox.BackColor = System.Drawing.SystemColors.Window;
            this.LogBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LogBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogBox.HideSelection = false;
            this.LogBox.Location = new System.Drawing.Point(287, 1);
            this.LogBox.Margin = new System.Windows.Forms.Padding(0);
            this.LogBox.Name = "LogBox";
            this.LogBox.ReadOnly = true;
            this.LogBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.LogBox.Size = new System.Drawing.Size(437, 341);
            this.LogBox.TabIndex = 1;
            this.LogBox.Text = "";
            this.LogBox.Click += new System.EventHandler(this.LogBox_Click);
            // 
            // textBoxCommandLine
            // 
            this.textBoxCommandLine.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.textBoxCommandLine.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.textBoxCommandLine.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxCommandLine.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.textBoxCommandLine.Location = new System.Drawing.Point(287, 342);
            this.textBoxCommandLine.Margin = new System.Windows.Forms.Padding(0);
            this.textBoxCommandLine.Name = "textBoxCommandLine";
            this.textBoxCommandLine.Size = new System.Drawing.Size(437, 18);
            this.textBoxCommandLine.TabIndex = 0;
            this.textBoxCommandLine.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxCommandLine_KeyDown);
            // 
            // panelLeftSide
            // 
            this.panelLeftSide.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.panelLeftSide.Controls.Add(this.flowLayoutPanelPlugins);
            this.panelLeftSide.Controls.Add(this.panelBotListBack);
            this.panelLeftSide.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeftSide.Location = new System.Drawing.Point(0, 25);
            this.panelLeftSide.Margin = new System.Windows.Forms.Padding(0);
            this.panelLeftSide.MinimumSize = new System.Drawing.Size(250, 360);
            this.panelLeftSide.Name = "panelLeftSide";
            this.panelLeftSide.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.panelLeftSide.Size = new System.Drawing.Size(286, 360);
            this.panelLeftSide.TabIndex = 2;
            // 
            // flowLayoutPanelPlugins
            // 
            this.flowLayoutPanelPlugins.AutoScroll = true;
            this.flowLayoutPanelPlugins.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelPlugins.BackColor = System.Drawing.SystemColors.Control;
            this.flowLayoutPanelPlugins.Controls.Add(this.flowLayoutPanelMain);
            this.flowLayoutPanelPlugins.Controls.Add(this.ModulePanelFarm);
            this.flowLayoutPanelPlugins.Controls.Add(this.modulePanelCardsSelling);
            this.flowLayoutPanelPlugins.Controls.Add(this.modulePanelGiveaways);
            this.flowLayoutPanelPlugins.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelPlugins.Location = new System.Drawing.Point(120, 1);
            this.flowLayoutPanelPlugins.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelPlugins.Name = "flowLayoutPanelPlugins";
            this.flowLayoutPanelPlugins.Size = new System.Drawing.Size(166, 359);
            this.flowLayoutPanelPlugins.TabIndex = 5;
            // 
            // flowLayoutPanelMain
            // 
            this.flowLayoutPanelMain.AutoSize = true;
            this.flowLayoutPanelMain.BackColor = System.Drawing.SystemColors.Control;
            this.flowLayoutPanelMain.Controls.Add(this.StatusLabel);
            this.flowLayoutPanelMain.Controls.Add(this.labelWallet);
            this.flowLayoutPanelMain.Controls.Add(this.buttonConnect);
            this.flowLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelMain.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelMain.Name = "flowLayoutPanelMain";
            this.flowLayoutPanelMain.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.flowLayoutPanelMain.Size = new System.Drawing.Size(166, 68);
            this.flowLayoutPanelMain.TabIndex = 6;
            // 
            // StatusLabel
            // 
            this.StatusLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.StatusLabel.Location = new System.Drawing.Point(2, 2);
            this.StatusLabel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 0);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(155, 15);
            this.StatusLabel.TabIndex = 2;
            this.StatusLabel.Text = "Farming cards 18 games left";
            // 
            // labelWallet
            // 
            this.labelWallet.AutoSize = true;
            this.labelWallet.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelWallet.Location = new System.Drawing.Point(2, 19);
            this.labelWallet.Margin = new System.Windows.Forms.Padding(2, 2, 2, 0);
            this.labelWallet.Name = "labelWallet";
            this.labelWallet.Size = new System.Drawing.Size(43, 15);
            this.labelWallet.TabIndex = 5;
            this.labelWallet.Text = "Wallet:\r\n";
            // 
            // buttonConnect
            // 
            this.buttonConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.buttonConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonConnect.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.buttonConnect.Location = new System.Drawing.Point(3, 36);
            this.buttonConnect.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(160, 30);
            this.buttonConnect.TabIndex = 4;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.OnOffButton_Click);
            // 
            // ModulePanelFarm
            // 
            this.ModulePanelFarm.AutoSize = true;
            this.ModulePanelFarm.BackColor = System.Drawing.SystemColors.Control;
            this.ModulePanelFarm.Controls.Add(this.labelFarming);
            this.ModulePanelFarm.Controls.Add(this.buttonFarm);
            this.ModulePanelFarm.Location = new System.Drawing.Point(0, 68);
            this.ModulePanelFarm.Margin = new System.Windows.Forms.Padding(0);
            this.ModulePanelFarm.ModuleName = "Farming";
            this.ModulePanelFarm.Name = "ModulePanelFarm";
            this.ModulePanelFarm.NameMargin = 2;
            this.ModulePanelFarm.Padding = new System.Windows.Forms.Padding(0, 20, 0, 2);
            this.ModulePanelFarm.Size = new System.Drawing.Size(166, 71);
            this.ModulePanelFarm.TabIndex = 7;
            // 
            // labelFarming
            // 
            this.labelFarming.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelFarming.Location = new System.Drawing.Point(2, 22);
            this.labelFarming.Margin = new System.Windows.Forms.Padding(2, 2, 2, 0);
            this.labelFarming.Name = "labelFarming";
            this.labelFarming.Size = new System.Drawing.Size(155, 15);
            this.labelFarming.TabIndex = 2;
            this.labelFarming.Text = "Farming cards 18 games left";
            // 
            // buttonFarm
            // 
            this.buttonFarm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.buttonFarm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonFarm.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.buttonFarm.Location = new System.Drawing.Point(3, 39);
            this.buttonFarm.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.buttonFarm.Name = "buttonFarm";
            this.buttonFarm.Size = new System.Drawing.Size(160, 30);
            this.buttonFarm.TabIndex = 3;
            this.buttonFarm.Text = "Farm";
            this.buttonFarm.UseVisualStyleBackColor = true;
            this.buttonFarm.Click += new System.EventHandler(this.buttonFarm_Click);
            // 
            // modulePanelCardsSelling
            // 
            this.modulePanelCardsSelling.AutoSize = true;
            this.modulePanelCardsSelling.BackColor = System.Drawing.SystemColors.Control;
            this.modulePanelCardsSelling.Controls.Add(this.labelCardsSelling);
            this.modulePanelCardsSelling.Controls.Add(this.buttonStylizedSellCards);
            this.modulePanelCardsSelling.Location = new System.Drawing.Point(0, 139);
            this.modulePanelCardsSelling.Margin = new System.Windows.Forms.Padding(0);
            this.modulePanelCardsSelling.ModuleName = "Cards";
            this.modulePanelCardsSelling.Name = "modulePanelCardsSelling";
            this.modulePanelCardsSelling.NameMargin = 2;
            this.modulePanelCardsSelling.Padding = new System.Windows.Forms.Padding(0, 20, 0, 2);
            this.modulePanelCardsSelling.Size = new System.Drawing.Size(166, 71);
            this.modulePanelCardsSelling.TabIndex = 8;
            // 
            // labelCardsSelling
            // 
            this.labelCardsSelling.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelCardsSelling.Location = new System.Drawing.Point(2, 22);
            this.labelCardsSelling.Margin = new System.Windows.Forms.Padding(2, 2, 2, 0);
            this.labelCardsSelling.Name = "labelCardsSelling";
            this.labelCardsSelling.Size = new System.Drawing.Size(155, 15);
            this.labelCardsSelling.TabIndex = 4;
            this.labelCardsSelling.Text = "Selling 1 of 23";
            // 
            // buttonStylizedSellCards
            // 
            this.buttonStylizedSellCards.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.buttonStylizedSellCards.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStylizedSellCards.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.buttonStylizedSellCards.Location = new System.Drawing.Point(3, 39);
            this.buttonStylizedSellCards.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.buttonStylizedSellCards.Name = "buttonStylizedSellCards";
            this.buttonStylizedSellCards.Size = new System.Drawing.Size(160, 30);
            this.buttonStylizedSellCards.TabIndex = 3;
            this.buttonStylizedSellCards.Text = "Sell cards";
            this.buttonStylizedSellCards.UseVisualStyleBackColor = true;
            this.buttonStylizedSellCards.Click += new System.EventHandler(this.buttonStylizedSellCards_Click);
            // 
            // modulePanelGiveaways
            // 
            this.modulePanelGiveaways.AutoSize = true;
            this.modulePanelGiveaways.BackColor = System.Drawing.SystemColors.Control;
            this.modulePanelGiveaways.Controls.Add(this.labelGiveawaysStatus);
            this.modulePanelGiveaways.Controls.Add(this.buttonStylizedGiveawaysOpen);
            this.modulePanelGiveaways.Location = new System.Drawing.Point(0, 210);
            this.modulePanelGiveaways.Margin = new System.Windows.Forms.Padding(0);
            this.modulePanelGiveaways.ModuleName = "Giveaways";
            this.modulePanelGiveaways.Name = "modulePanelGiveaways";
            this.modulePanelGiveaways.NameMargin = 2;
            this.modulePanelGiveaways.Padding = new System.Windows.Forms.Padding(0, 20, 0, 2);
            this.modulePanelGiveaways.Size = new System.Drawing.Size(166, 71);
            this.modulePanelGiveaways.TabIndex = 9;
            // 
            // labelGiveawaysStatus
            // 
            this.labelGiveawaysStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelGiveawaysStatus.Location = new System.Drawing.Point(2, 22);
            this.labelGiveawaysStatus.Margin = new System.Windows.Forms.Padding(2, 2, 2, 0);
            this.labelGiveawaysStatus.Name = "labelGiveawaysStatus";
            this.labelGiveawaysStatus.Size = new System.Drawing.Size(155, 15);
            this.labelGiveawaysStatus.TabIndex = 4;
            this.labelGiveawaysStatus.Text = "Running";
            // 
            // buttonStylizedGiveawaysOpen
            // 
            this.buttonStylizedGiveawaysOpen.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.buttonStylizedGiveawaysOpen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStylizedGiveawaysOpen.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.buttonStylizedGiveawaysOpen.Location = new System.Drawing.Point(3, 39);
            this.buttonStylizedGiveawaysOpen.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.buttonStylizedGiveawaysOpen.Name = "buttonStylizedGiveawaysOpen";
            this.buttonStylizedGiveawaysOpen.Size = new System.Drawing.Size(160, 30);
            this.buttonStylizedGiveawaysOpen.TabIndex = 3;
            this.buttonStylizedGiveawaysOpen.Text = "Open form";
            this.buttonStylizedGiveawaysOpen.UseVisualStyleBackColor = true;
            this.buttonStylizedGiveawaysOpen.Click += new System.EventHandler(this.buttonStylizedGiveawaysOpen_Click);
            // 
            // panelBotListBack
            // 
            this.panelBotListBack.BackColor = System.Drawing.SystemColors.ButtonShadow;
            this.panelBotListBack.Controls.Add(this.BotList);
            this.panelBotListBack.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelBotListBack.Location = new System.Drawing.Point(0, 1);
            this.panelBotListBack.Name = "panelBotListBack";
            this.panelBotListBack.Padding = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.panelBotListBack.Size = new System.Drawing.Size(120, 359);
            this.panelBotListBack.TabIndex = 1;
            // 
            // timerBotUpdate
            // 
            this.timerBotUpdate.Enabled = true;
            this.timerBotUpdate.Interval = 1000;
            this.timerBotUpdate.Tick += new System.EventHandler(this.timerBotUpdate_Tick);
            // 
            // notifyIconMain
            // 
            this.notifyIconMain.ContextMenuStrip = this.contextMenuStripNotifyIcon;
            this.notifyIconMain.Text = "Steam Multi Account";
            this.notifyIconMain.Visible = true;
            this.notifyIconMain.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIconMain_MouseDoubleClick);
            // 
            // contextMenuStripNotifyIcon
            // 
            this.contextMenuStripNotifyIcon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.closeToolStripMenuItemClose});
            this.contextMenuStripNotifyIcon.Name = "contextMenuStripNotifyIcon";
            this.contextMenuStripNotifyIcon.Size = new System.Drawing.Size(104, 26);
            // 
            // closeToolStripMenuItemClose
            // 
            this.closeToolStripMenuItemClose.Name = "closeToolStripMenuItemClose";
            this.closeToolStripMenuItemClose.Size = new System.Drawing.Size(103, 22);
            this.closeToolStripMenuItemClose.Text = "Close";
            this.closeToolStripMenuItemClose.Click += new System.EventHandler(this.closeToolStripMenuItemClose_Click);
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.panelLeftSide);
            this.panelMain.Controls.Add(this.panelRightSide);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Padding = new System.Windows.Forms.Padding(0, 25, 0, 0);
            this.panelMain.Size = new System.Drawing.Size(724, 381);
            this.panelMain.TabIndex = 2;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ComandsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(724, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // ComandsToolStripMenuItem
            // 
            this.ComandsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.createBotsToolStripMenuItem1});
            this.ComandsToolStripMenuItem.Name = "ComandsToolStripMenuItem";
            this.ComandsToolStripMenuItem.Size = new System.Drawing.Size(70, 20);
            this.ComandsToolStripMenuItem.Text = "Comands";
            // 
            // createBotsToolStripMenuItem1
            // 
            this.createBotsToolStripMenuItem1.Name = "createBotsToolStripMenuItem1";
            this.createBotsToolStripMenuItem1.Size = new System.Drawing.Size(152, 22);
            this.createBotsToolStripMenuItem1.Text = "Create Bots";
            this.createBotsToolStripMenuItem1.Click += new System.EventHandler(this.createBotsToolStripMenuItem1_Click);
            // 
            // SMAForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 381);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.panelMain);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.MinimumSize = new System.Drawing.Size(740, 420);
            this.Name = "SMAForm";
            this.Text = "Steam Multi Account";
            this.Shown += new System.EventHandler(this.SMAForm_Shown);
            this.contextMenuStripMain.ResumeLayout(false);
            this.panelRightSide.ResumeLayout(false);
            this.panelRightSide.PerformLayout();
            this.panelLeftSide.ResumeLayout(false);
            this.flowLayoutPanelPlugins.ResumeLayout(false);
            this.flowLayoutPanelPlugins.PerformLayout();
            this.flowLayoutPanelMain.ResumeLayout(false);
            this.flowLayoutPanelMain.PerformLayout();
            this.ModulePanelFarm.ResumeLayout(false);
            this.modulePanelCardsSelling.ResumeLayout(false);
            this.modulePanelGiveaways.ResumeLayout(false);
            this.panelBotListBack.ResumeLayout(false);
            this.contextMenuStripNotifyIcon.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.ListBox BotList;
        private System.Windows.Forms.Panel panelRightSide;
        private System.Windows.Forms.Panel panelLeftSide;
        private System.Windows.Forms.Panel panelBotListBack;
        private System.Windows.Forms.TextBox textBoxCommandLine;
        public System.Windows.Forms.RichTextBox LogBox;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripMain;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.Timer timerBotUpdate;
        private System.Windows.Forms.Label StatusLabel;
        private ButtonStylized buttonConnect;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelPlugins;
        private System.Windows.Forms.Label labelWallet;
        private System.Windows.Forms.NotifyIcon notifyIconMain;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripNotifyIcon;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItemClose;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelMain;
        private ModulePanel ModulePanelFarm;
        private ButtonStylized buttonFarm;
        private ModulePanel modulePanelCardsSelling;
        private ButtonStylized buttonStylizedSellCards;
        public System.Windows.Forms.Label labelCardsSelling;
        public System.Windows.Forms.Label labelFarming;
        private ModulePanel modulePanelGiveaways;
        public System.Windows.Forms.Label labelGiveawaysStatus;
        private ButtonStylized buttonStylizedGiveawaysOpen;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ComandsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem createBotsToolStripMenuItem1;
    }
}

