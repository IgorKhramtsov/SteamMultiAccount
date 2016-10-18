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
    internal partial class UserInput : Form
    {
        private string output;
        private bool SomethingType = false;
        internal UserInput(Program.InputType inputType)
        {
            InitializeComponent();
            switch (inputType)
            {
                case Program.InputType.BotName:
                    label1.Text = "Type bot name";
                    break;
            }
        }

        internal static bool input(out string ret, Program.InputType inputType)
        {
            UserInput form = new UserInput(inputType);

            form.ShowDialog();
            ret = form.output;
            return form.SomethingType;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
                if ((sender as TextBox).Text != string.Empty)
                {
                    output = (sender as TextBox).Text;
                    SomethingType = true;
                    this.Close();
                }
        }

        private void UserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }
    }
}
