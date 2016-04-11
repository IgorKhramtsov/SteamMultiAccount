using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Drawing;

namespace SteamMultiAccount
{
    internal class Loging
    {
        private readonly RichTextBox _textbox;
        internal Loging(RichTextBox tbox)
        {
            _textbox = tbox;
        }

        internal void LogError(string message, [CallerMemberName] string FunctionName = "")
        {
            string logMess = "   [ERROR]  " + FunctionName + " () " + message;
            Log(logMess, Color.DarkRed);
        }
        internal void LogInfo(string message, [CallerMemberName] string FunctionName = "")
        {
            string logMess = "    [INFO]    " + FunctionName + " () " + message;
            Log(logMess, Color.Blue);
        }
        internal void LogWarning(string message, [CallerMemberName] string FunctionName = "")
        {
            string logMess = "[WARNING]" + FunctionName + "() " + message;
            Log(logMess, Color.Orange);
        }
        internal void LogDebug(string message, [CallerMemberName] string FunctionName = "")
        {
            string logMess = "   [DEBUG]  " + FunctionName + "() " + message;
            Log(logMess, Color.DarkViolet);
        }

        internal void LogUser(string message)
        {
            string logMess = "   [USER]  " + message;
            Log(logMess, Color.Black);
        }

        internal void Log(string message,Color col)
        {
            if (string.IsNullOrEmpty(message))
                return;
            string loggedMessage = DateTime.Now + " " + message;
            
            _textbox.SelectionStart = _textbox.TextLength;
            _textbox.SelectionLength = 0;

            _textbox.SelectionColor = col;
            _textbox.AppendText(loggedMessage);
            _textbox.SelectionColor = _textbox.ForeColor;
            _textbox.AppendText(Environment.NewLine);
        }

        internal static void LogToFile(string message, string botname="Programm")
        {
            string path = "log.txt";
            string loggingmessage = DateTime.Now + " <"+botname+"> "+ message;
            lock (path)
            {
                try {
                    File.WriteAllText(path, loggingmessage);
                } catch {

                }
            }
        }
    }
}
