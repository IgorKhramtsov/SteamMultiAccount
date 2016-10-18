namespace SteamMultiAccount
{
    partial class KeyManager
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
            this.listBoxKeys = new System.Windows.Forms.ListBox();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.panel2 = new System.Windows.Forms.Panel();
            this.textBoxErrorKeys = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelNotActivatedKeys = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxKeys
            // 
            this.listBoxKeys.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listBoxKeys.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxKeys.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.listBoxKeys.FormattingEnabled = true;
            this.listBoxKeys.ItemHeight = 17;
            this.listBoxKeys.Location = new System.Drawing.Point(0, 0);
            this.listBoxKeys.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxKeys.Name = "listBoxKeys";
            this.listBoxKeys.Size = new System.Drawing.Size(384, 200);
            this.listBoxKeys.TabIndex = 0;
            this.listBoxKeys.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBoxKeys_DrawItem);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.BackColor = System.Drawing.SystemColors.Control;
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.listBoxKeys);
            this.splitContainerMain.Panel1MinSize = 34;
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.panel2);
            this.splitContainerMain.Panel2.Controls.Add(this.panel1);
            this.splitContainerMain.Size = new System.Drawing.Size(384, 402);
            this.splitContainerMain.SplitterDistance = 200;
            this.splitContainerMain.SplitterWidth = 10;
            this.splitContainerMain.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.textBoxErrorKeys);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 25);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(384, 167);
            this.panel2.TabIndex = 2;
            // 
            // textBoxErrorKeys
            // 
            this.textBoxErrorKeys.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxErrorKeys.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxErrorKeys.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxErrorKeys.Location = new System.Drawing.Point(0, 0);
            this.textBoxErrorKeys.Multiline = true;
            this.textBoxErrorKeys.Name = "textBoxErrorKeys";
            this.textBoxErrorKeys.ReadOnly = true;
            this.textBoxErrorKeys.Size = new System.Drawing.Size(384, 167);
            this.textBoxErrorKeys.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelNotActivatedKeys);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(384, 25);
            this.panel1.TabIndex = 1;
            // 
            // labelNotActivatedKeys
            // 
            this.labelNotActivatedKeys.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelNotActivatedKeys.Location = new System.Drawing.Point(0, 0);
            this.labelNotActivatedKeys.Name = "labelNotActivatedKeys";
            this.labelNotActivatedKeys.Size = new System.Drawing.Size(384, 25);
            this.labelNotActivatedKeys.TabIndex = 0;
            this.labelNotActivatedKeys.Text = "Not activated keys";
            this.labelNotActivatedKeys.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // KeyManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 402);
            this.Controls.Add(this.splitContainerMain);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MinimumSize = new System.Drawing.Size(400, 39);
            this.Name = "KeyManager";
            this.Text = "Key Manager";
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListBox listBoxKeys;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TextBox textBoxErrorKeys;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelNotActivatedKeys;
    }
}