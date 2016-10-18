using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SteamMultiAccount
{
    public class Game
    {
        private Process process;
        public ulong appID;
        public string Name;
        public const string emulatorName = "GameEmulator.exe";
        public Game() { }
        public Game(ulong _appID, string name = "")
        {
            this.appID = _appID;
            this.Name = name;
            if (!System.IO.File.Exists(emulatorName))
                Logging.LogToFile("*ERROR* cant find emulator");
        }
        public Process StartIdle()
        {
            if (appID == 0)
                return null;

            if (process == null && System.IO.File.Exists(emulatorName))
                return process = Process.Start(new ProcessStartInfo(emulatorName, appID.ToString()) { CreateNoWindow = true});
            else
                return process;
        }
        public void StopIdle()
        {
            if (process != null)
            {
                if (!process.HasExited)
                    process.Kill();
                process.Dispose();
                process = null;
            }
        }
        public override bool Equals(object obj)
        {
            if (obj is Game)
                return (obj as Game).appID == this.appID;
            else
                return base.Equals(obj);
        }
        ~Game()
        {
            if (process != null && process.HasExited != true)
                process.Kill();
        }
    }
}
