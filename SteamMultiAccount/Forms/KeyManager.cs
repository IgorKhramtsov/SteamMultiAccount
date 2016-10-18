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
    public partial class KeyManager : Form
    {
        public KeyManager(List<Key> keys)
        {
            InitializeComponent();
            this.listBoxKeys.Items.AddRange(keys.ToArray());
            foreach (Key key in keys)
                if (key.ActivatingResult != CustomHandler.PurchaseResponseCallback.EPurchaseResult.OK)
                    if (key.ActivatingResult != CustomHandler.PurchaseResponseCallback.EPurchaseResult.InvalidKey)
                        if (key.ActivatingResult != CustomHandler.PurchaseResponseCallback.EPurchaseResult.DuplicatedKey)
                            textBoxErrorKeys.AppendText(key.key + Environment.NewLine);
        }
        private void listBoxKeys_DrawItem(object sender, DrawItemEventArgs e)
        {
            Color color = e.BackColor;
            Key key;
            key = listBoxKeys.Items[e.Index] as Key;
            switch(key.ActivatingResult)
            {
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OK:
                    color = Color.LightGreen;
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.DuplicatedKey:
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.InvalidKey:
                    color = Color.FromArgb(255, 102, 102); // Light red
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OnCooldown:
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.AlreadyOwned:
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.BaseGameRequired:
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.RegionLocked:
                    color = Color.FromArgb(255, 165, 0);
                    break;
            }
            e.DrawBackground();
            Graphics graphics = e.Graphics;
            graphics.FillRectangle(new SolidBrush(color), e.Bounds);
            graphics.DrawString(key.ToString(), e.Font, new SolidBrush(e.ForeColor), new Point(e.Bounds.X, e.Bounds.Y));
            e.DrawFocusRectangle();
        }
    }
}
