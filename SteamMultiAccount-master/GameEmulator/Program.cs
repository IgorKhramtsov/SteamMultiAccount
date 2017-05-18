using System;
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
            if ( string.IsNullOrWhiteSpace( args[0] ) )
                return;
            long appID = long.Parse(args[0]);
            Environment.SetEnvironmentVariable("SteamAppId", appID.ToString());
            if( !SteamAPI.IsSteamRunning() || !SteamAPI.Init())
            {
                return; // If we cant initilize steam api, close the program
            }
            EmptyWorkingSet(Process.GetCurrentProcess().Handle); //Clean memory

#if DEBUG
            Process.GetProcessesByName("SteamMultiAccount.vshost")[0].WaitForExit();
#else
            Process.GetProcessesByName("SteamMultiAccount")[0].WaitForExit();
#endif
            return;
        }

    }
}
