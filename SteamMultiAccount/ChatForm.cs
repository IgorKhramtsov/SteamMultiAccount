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
    internal partial class ChatForm : Form
    {
        internal ChatForm(Bot bot)
        {
            InitializeComponent();
            if (!bot.FriendList.Any())
            { 
                Dispose();
                Close();
            }
            foreach (var friends in bot.FriendList)
            {
             // TODO: Creating list of Friends and chat functionality   
            }
        }
    }
}
