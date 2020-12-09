using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BetterConsole;

namespace Process_Dumper
{
    class Program
    {
        static Random r = new Random();

        static Stopwatch sw = new Stopwatch();
        static Stopwatch total = new Stopwatch();

        static bool a = false;
        static bool b = false;
        private static string dumper = "";

        static List<string> svchostList = new List<string>();

        static string[] exclusionList = {"svchost"}; // always keep svchost in this list, this prevents duplicated dumps

        static string path = AppDomain.CurrentDomain.BaseDirectory;

        static void Main(string[] args)
        {
            Console.WriteLine(
                "Make a choice on what to use\nStrings2 (slower but more reliable)\nStrings3 (faster but might miss some strings)");
            string choice = Console.ReadLine();
            if (choice == "strings2" || choice == "2")
            {
                dumper = "strings2";
            }
            else if (choice == "strings3" || choice == "3")
            {
                dumper = "strings3";
            }
            else
            {
                Console.WriteLine("idk how the fuck u fucked that up. As punishment u gotta restart the program");
                Console.ReadLine();
                Environment.Exit(0);
            }

            // setup all needed directories and files
            if (!Directory.Exists("dumps") || !Directory.Exists("assets"))
            {
                Directory.CreateDirectory("dumps");
                Directory.CreateDirectory("assets");
                if (dumper == "strings2")
                {
                    File.WriteAllBytes("assets\\dumper.exe", Properties.Resources.s2);
                }
                else
                {
                    File.WriteAllBytes("assets\\dumper.exe", Properties.Resources.s3);
                }
            }

            // gather service list and dump them
            total.Start();
            getSvchost();


            dumpSvchost();


            dumpProcesses();


            // dump all running processes excluding svchost

            while (!Program.a || !Program.b)
            {
            }

            Process[] cmdprocs = Process.GetProcessesByName("cmd");
            int procsLeft = cmdprocs.Length;

            foreach (var proc in cmdprocs)
            {
                while (procsLeft != 0)
                {
                    int currentproc = proc.Id;
                    if (!getParent(currentproc).Equals(Process.GetCurrentProcess().ProcessName))
                    {
                        procsLeft--;
                    }
                }
            }


            Console.Clear();
            Console.WriteLine();
            Bconsole.printASCII("Proc Dumper", Bconsole.fonts.alligator2, ConsoleColor.DarkCyan);

            Console.WriteLine("\n─────────────────────────── Finished Dumps ───────────────────────────");
            Console.WriteLine($"took a total of {total.ElapsedMilliseconds}ms (using {dumper})");
            Console.ReadLine();
        }

        /// <summary>
        /// process dumper
        /// </summary>
        static void dumpProcesses()
        {
            Console.WriteLine("\n─────────────────────────── Dumping Processes ───────────────────────────");

            var allProcesses = Process.GetProcesses();
            foreach (Process p in allProcesses)
            {
                new Thread(() =>
                {
                    try
                    {
                        if (!exclusionList.Contains(p.ProcessName))
                        {
                            sw.Restart();

                            // creat directory for specific process if it doesnt exist
                            if (!Directory.Exists($"dumps\\{p.ProcessName}"))
                                Directory.CreateDirectory($"dumps\\{p.ProcessName}");

                            // dump process
                            if (dumper == "strings2")
                            {
                                runCommand($"\"{path}assets\\\"dumper.exe -pid {p.Id} -l 4 -nh > \"{path}dumps\\{p.ProcessName}\\\"{p.ProcessName}_{r.Next(0, 999999999)}.txt");

                                Console.WriteLine($"Dumped process \"{p.ProcessName}\" (took {sw.ElapsedMilliseconds}ms)");
                            }
                            else
                            {
                                runCommand(
                                    $"\"{path}assets\\\"dumper.exe {p.Id} > path > \"{path}dumps\\{p.ProcessName}\\\"{p.ProcessName}_{r.Next(0, 999999999)}.txt");

                                Bconsole.WriteLine($"Dumped process \"{p.ProcessName}\" (took {sw.ElapsedMilliseconds}ms)");
                            }
                          
                        }
                    }
                    catch
                    {
                        Bconsole.WriteLine($"Failed to dump process \"{p.ProcessName}\"", ConsoleColor.Red);
                    }
                }).Start();
            }

            b = true;
        }

        /// <summary>
        /// svchost/service dumper
        /// </summary>
        static void dumpSvchost()
        {
            Console.WriteLine("─────────────────────────── Dumping Services ───────────────────────────");

            // creat special directory for all services to go
            if (!Directory.Exists("dumps\\svchost"))
                Directory.CreateDirectory("dumps\\svchost");

            foreach (string service in svchostList)
            {
                new Thread(() =>
                {
                    try
                    {
                        sw.Restart();

                        // creat directory for specific service if it doesnt exist
                        if (!Directory.Exists($"dumps\\svchost\\{service}"))
                            Directory.CreateDirectory($"dumps\\svchost\\{service}");

                        // dump service 

                        if (dumper == "strings2")
                        {
                            runCommand($"\"{path}assets\\\"dumper.exe -pid {getService(service)} -l 4 -nh > \"{path}dumps\\svchost\\{service}\\\"{service}_{r.Next(0, 999999999)}.txt");

                            Console.WriteLine($"Dumped service \"{service}\" (took {sw.ElapsedMilliseconds}ms)");
                        }
                        else
                        {
                            runCommand(
                                $"\"{path}assets\\\"dumper.exe {getService(service)} > \"{path}dumps\\svchost\\{service}\\\"{service}_{r.Next(0, 999999999)}.txt");

                            Bconsole.WriteLine($"Dumped service \"{service}\" (took {sw.ElapsedMilliseconds}ms)");
                        }


                       
                    }
                    catch
                    {
                        Bconsole.WriteLine($"Failed to dump service \"{service}\"", ConsoleColor.Red);
                    }
                }).Start();
            }

            a = true;
        }

        /// <summary>
        /// parse through svchost list and grab only the service name
        /// </summary>
        static void getSvchost()
        {
            runCommand($"cd {path}assets & tasklist /svc | find \"svchost.exe\" > svchost.log");

            string reader = File.ReadAllText("assets\\svchost.log");
            foreach (string line in reader.Split('\n'))
            {
                if (line.Length > 5)
                {
                    string serviceName = line.Substring(35).Replace(" ", "").Replace(",", ".");
                    svchostList.Add(serviceName.Substring(0, serviceName.Length - 1));
                }
            }
        }

        /// <summary>
        /// run any command input through cmd as admin
        /// </summary>
        static void runCommand(string command)
        {
            Process CMD = new Process();
            CMD.StartInfo.FileName = "cmd.exe";
            CMD.StartInfo.RedirectStandardInput = true;
            CMD.StartInfo.RedirectStandardOutput = true;
            CMD.StartInfo.CreateNoWindow = true;
            CMD.StartInfo.UseShellExecute = false;
            CMD.Start();

            CMD.StandardInput.WriteLine(command);
            CMD.StandardInput.Flush();
            CMD.StandardInput.Close();
            //CMD.WaitForExit();
        }

        /// <summary>
        /// used this to check if there are still cmd's active dumping strings
        /// </summary>
        /// <param name="pid"></param>
        /// <returns>parent of pid<returns>
        static string getParent(int pid)
        {
            try
            {
                var myId = pid;
                var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", myId);
                var search = new ManagementObjectSearcher("root\\CIMV2", query);
                var results = search.Get().GetEnumerator();
                results.MoveNext();
                var queryObj = results.Current;
                var parentId = (uint) queryObj["ParentProcessId"];
                var parent = Process.GetProcessById((int) parentId);

                return parent.ProcessName;
            }
            catch (Exception e)
            {
                return "prolly died so is fine";
            }
        }

        /// <summary>
        /// Gets the PID of specifc services
        /// </summary>
        static uint getService(string serviceName)
        {
            uint processId = 0;
            string qry = "SELECT PROCESSID FROM WIN32_SERVICE WHERE NAME = '" + serviceName + "'";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(qry);

            foreach (System.Management.ManagementObject mngntObj in searcher.Get())
                processId = (uint) mngntObj["PROCESSID"];

            return processId;
        }
    }
}