using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Steamworks;

namespace GameEmulator
{
    static class Program
    {
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);
        static void Main(string[] args)
        {
            long appID = long.Parse(args[0]);
            //long appID = 570;
            Environment.SetEnvironmentVariable("SteamAppId", appID.ToString());
            if(!SteamAPI.Init())
            {
                return; // If we cant initilize steam api we close the programm
            }
            EmptyWorkingSet(Process.GetCurrentProcess().Handle); //Clean memory
            Application.Run();
        }
    }
}
