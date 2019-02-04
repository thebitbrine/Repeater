using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repeater
{
    class Program
    {
        public Stopwatch Uptime = null;
        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(Console.Title) == false)
                Console.Title ="[Repeater] Initializing...";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Program p = new Program();
            p.Run(args);
        }

        public void Run(string[] args)
        {
            Uptime = new Stopwatch();
            Uptime.Start();

            try
            {
                Dictionary<string, string> Configs = GetConfigs(Rooter("data/configs.cfg"));

                if (args.Length == 4)
                {
                    PrintLine("Initializing main thread.");
                    var thread = new System.Threading.Thread(() => RunEXE(args[0], args[1], int.Parse(args[2]), bool.Parse(args[3].ToLower()), 32));
                    PrintLine("Starting main thread...");
                    thread.Start();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Configs["FULLPATH"]) == false && string.IsNullOrWhiteSpace(Configs["INTERVAL"]) == false && string.IsNullOrWhiteSpace(Configs["WAITFOREXIT"]) == false)
                    {
                        PrintLine("Initializing main thread.");
                        int InnerPriority = 32; if (Configs["PRIORITY"] != null) int.TryParse(Configs["PRIORITY"], out InnerPriority);
                        var thread = new System.Threading.Thread(() => RunEXE(Configs["FULLPATH"], Configs["ARGUMENTS"], int.Parse(Configs["INTERVAL"]), bool.Parse(Configs["WAITFOREXIT"].ToLower()), InnerPriority));
                        PrintLine("Starting main thread...");
                        thread.Start();
                    }
                    else
                        Console.WriteLine("Args: FullPath, Arguments, Interval, WaitForExit (True,False)");
                }
            }
            catch (Exception ex) { PrintLine("ERR: " + ex.Message); PrintLine("DMP: " + ex.StackTrace); }
            
        }



        public void RunEXE(string FullPath, string Arguments, int Interval, bool WaitForExit, int Priority)
        {
            string AppName = FullPath.Substring(FullPath.LastIndexOf('\\') + 1).Replace("\"", "").Replace(".exe","");
            PrintLine("Initializing " + AppName + "...");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            //p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = FullPath;
            p.StartInfo.Arguments = Arguments;
            int TotalCycles = 0;
            int MaxTime = 0;
            int MinTime = int.MaxValue;
            int AvgTime = 0;
            bool AutoInterval = false;
            int IntervalAddition = 10;
            if (Interval < 0) { Interval = Interval * -1; IntervalAddition = Interval; AutoInterval = true; Console.WriteLine(Tag("Adaptive Interval: ON (" + IntervalAddition + " ms)")); }
            

            while (true)
            {
                try
                {
                    Stopwatch SW = new Stopwatch();
                    Console.WriteLine(Tag("Starting..."));
                    SW.Start();
                    p.Start();
                    p.PriorityClass = (ProcessPriorityClass)Priority;
                    if (WaitForExit == true)
                    {
                        Console.WriteLine(Tag("Waiting To Exit..."));
                        Console.ForegroundColor = ConsoleColor.Gray;
                        p.WaitForExit();
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Exited.", false);
                        Console.WriteLine(Tag(AppName + " took " + SW.ElapsedMilliseconds + " ms to exit."));
                    }
                    SW.Stop();

                    if (string.IsNullOrWhiteSpace(Console.Title) == false)
                        Console.Title = "[" +
                            string.Format("{0:D2}d:{1:D2}h:{2:D2}m:{3:D2}s",
                            Uptime.Elapsed.Days,
                            Uptime.Elapsed.Hours,
                            Uptime.Elapsed.Minutes,
                            Uptime.Elapsed.Seconds)
                            + "][" + TotalCycles++ + "][" + AppName + "]";

                    if (SW.ElapsedMilliseconds < MinTime) MinTime = (int)SW.ElapsedMilliseconds;
                    if (SW.ElapsedMilliseconds > MaxTime) MaxTime = (int)SW.ElapsedMilliseconds;
                    AvgTime = (AvgTime + (int)SW.ElapsedMilliseconds);
                    
                    if (TotalCycles % 10 == 0) { try { Console.Clear(); } catch { /*Well...*/ } PrintLine("Total Cycles: " + TotalCycles); PrintLine("Runtime [Min, Avg, Max]: " + MinTime + " ms, " + AvgTime / TotalCycles + " ms, " + MaxTime + " ms"); }
                    if (AutoInterval == true && (SW.ElapsedMilliseconds - IntervalAddition) > (AvgTime / TotalCycles)) Interval = Interval + IntervalAddition;

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Spacebar)
                    {
                        Console.WriteLine(Tag("Sleeping " + 10000 + " ms..."));
                        System.Threading.Thread.Sleep(10000);
                    }
                    else
                    {
                        Console.WriteLine(Tag("Sleeping " + Interval + " ms..."));
                        System.Threading.Thread.Sleep(Interval);
                    }
                }
                catch (Exception ex) { PrintLine("ERR: " + ex.Message); PrintLine("DMP: " + ex.StackTrace); }
            }
        }

        public Dictionary<string, string> GetConfigs(string ConfigFilePath)
        {
            try
            {
                string RawConfigs = File.ReadAllText(ConfigFilePath);
                string[] RawConfigsArray = RawConfigs.Replace("\r", "").Split('\n');
                List<string> MediumRareConfigs = new List<string>();
                Dictionary<string, string> CookedConfigs = new Dictionary<string, string>();
                foreach (var Line in RawConfigsArray)
                {
                    if (Line.Contains('=') == true && CookedConfigs.ContainsKey(Line.Split('=')[0]) == false)
                        CookedConfigs.Add(Line.Split('=')[0], Line.Substring(Line.IndexOf('=') + 1));
                }
                return CookedConfigs;
            }
            catch { }
            return null;
        }

        #region Essentials
        public string LogPath = @"data\Logs.txt";
        public bool NoConsolePrint = false;
        public bool NoFilePrint = false;
        public void Print(string String)
        {
            Check();
            if (NoFilePrint == false) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "")));
            if (NoConsolePrint == false) Console.Write(Tag(String));
        }
        public void Print(string String, bool DoTag)
        {
            Check();
            if (DoTag == true) { if (NoFilePrint == false) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", ""))); if (NoConsolePrint == false) Console.Write(Tag(String)); }
            else { if (NoFilePrint == false) WaitWrite(Rooter(LogPath), String.Replace("\r", "")); if (NoConsolePrint == false) Console.Write(String); }
        }
        public void PrintLine(string String)
        {
            Check();
            if (NoFilePrint == false) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "") + "\n"));
            if (NoConsolePrint == false) Console.WriteLine(Tag(String));
        }
        public void PrintLine(string String, bool DoTag)
        {
            Check();
            if (DoTag == true) { if (NoFilePrint == false) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "") + "\n")); if (NoConsolePrint == false) Console.WriteLine(Tag(String)); }
            else { if (NoFilePrint == false) WaitWrite(Rooter(LogPath), String.Replace("\r", "") + "\n"); if (NoConsolePrint == false) Console.WriteLine(String); }
        }
        public void PrintLine()
        {
            Check();
            if (NoFilePrint == false) WaitWrite(Rooter(LogPath), "\n");
            if (NoConsolePrint == false) Console.WriteLine();
        }
        public void PrintLines(string[] StringArray)
        {
            Check();
            foreach (string String in StringArray)
            {
                if (NoFilePrint == false) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "") + "\n"));
                if (NoConsolePrint == false) Console.WriteLine(Tag(String));
            }
        }
        public void PrintLines(string[] StringArray, bool DoTag)
        {
            Check();
            foreach (string String in StringArray)
            {
                if (DoTag == true) { if (NoFilePrint == false) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "") + "\n")); if (NoConsolePrint == false) Console.WriteLine(Tag(String)); }
                else { if (NoFilePrint == false) WaitWrite(Rooter(LogPath), String.Replace("\r", "") + "\n"); if (NoConsolePrint == false) Console.WriteLine(String); }
            }
        }
        public void Check()
        {
            if (System.IO.File.Exists(LogPath) == false) Touch(LogPath);
        }
        private bool WriteLock = false;
        public void WaitWrite(string Path, string Data)
        {
            while (WriteLock == true) { System.Threading.Thread.Sleep(20); }
            WriteLock = true;
            System.IO.File.AppendAllText(Path, Data);
            WriteLock = false;
        }
        public string[] ReadData(string DataDir)
        {
            if (System.IO.File.Exists(DataDir) == true)
            {
                return System.IO.File.ReadAllLines(DataDir);
            }
            else
                return null;
        }
        public void CleanLine()
        {
            Console.Write("\r");
            for (int i = 0; i < Console.WindowWidth - 1; i++) Console.Write(" ");
            Console.Write("\r");
        }
        public void CleanLastLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            CleanLine();
        }
        public string Rooter(string RelPath)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), RelPath);
        }
        public string Tag(string Text)
        {
            return "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "] " + Text;
        }
        public string Tag()
        {
            return "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "] ";
        }
        public bool Touch(string Path)
        {
            try
            {
                System.Text.StringBuilder PathCheck = new System.Text.StringBuilder();
                string[] Direcories = Path.Split(System.IO.Path.DirectorySeparatorChar);
                foreach (var Directory in Direcories)
                {
                    PathCheck.Append(Directory);
                    string InnerPath = PathCheck.ToString();
                    if (System.IO.Path.HasExtension(InnerPath) == false)
                    {
                        PathCheck.Append("\\");
                        if (System.IO.Directory.Exists(InnerPath) == false) System.IO.Directory.CreateDirectory(InnerPath);
                    }
                    else
                    {
                        System.IO.File.WriteAllText(InnerPath, "");
                    }
                }
                if (IsDirectory(Path) == true && System.IO.Directory.Exists(PathCheck.ToString()) == true) { return true; }
                if (IsDirectory(Path) == false && System.IO.File.Exists(PathCheck.ToString()) == true) { return true; }
            }
            catch (Exception ex) { PrintLine("ERROR: Failed touching \"" + Path + "\". " + ex.Message, true); }
            return false;
        }
        public bool IsDirectory(string Path)
        {
            try
            {
                System.IO.FileAttributes attr = System.IO.File.GetAttributes(Path);
                if ((attr & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
                    return true;
                else
                    return false;
            }
            catch
            {
                if (System.IO.Path.HasExtension(Path) == true) return true;
                else return false;
            }
        }
        #endregion
    }
}
