using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Process_Dumper {
    class Program {
        static Random r = new Random();

        static Stopwatch sw = new Stopwatch();

        static List<string> svchostList = new List<string>();

        static string[] exclusionList = { "svchost" }; // always keep svchost in this list, this prevents duplicated dumps

        static string path = AppDomain.CurrentDomain.BaseDirectory;

        static void Main(string[] args) {
            // setup all needed directories and files
            if (!Directory.Exists("dumps") || !Directory.Exists("assets")) {
                Directory.CreateDirectory("dumps");
                Directory.CreateDirectory("assets");
                File.WriteAllBytes("assets\\dumper.exe", Properties.Resources.s2);
            }

            // gather service list and dump them
            getSvchost();
            dumpSvchost();

            // dump all running processes excluding svchost
            dumpProcesses();

            Console.WriteLine("\n─────────────────────────── Finished Dumps ───────────────────────────");
            Console.ReadLine();
        }

        /// <summary>
        /// process dumper
        /// </summary>
        static void dumpProcesses() {
            Console.WriteLine("\n─────────────────────────── Dumping Processes ───────────────────────────");

            var allProcesses = Process.GetProcesses();
            foreach (Process p in allProcesses) {
                try {
                    if (!exclusionList.Contains(p.ProcessName)) {
                        sw.Restart();

                        // creat directory for specific process if it doesnt exist
                        if (!Directory.Exists($"dumps\\{p.ProcessName}"))
                            Directory.CreateDirectory($"dumps\\{p.ProcessName}");

                        // dump process
                        runCommand($"\"{path}assets\\\"dumper.exe -pid {p.Id} -l 4 -nh > \"{path}dumps\\{p.ProcessName}\\\"{p.ProcessName}_{r.Next(0, 999999999)}.txt");

                        Console.WriteLine($"Dumped process \"{p.ProcessName}\" (took {sw.ElapsedMilliseconds}ms)");
                    }
                } catch { Console.WriteLine($"Failed to dump process \"{p.ProcessName}\""); }
            }
        }

        /// <summary>
        /// svchost/service dumper
        /// </summary>
        static void dumpSvchost() {
            Console.WriteLine("─────────────────────────── Dumping Services ───────────────────────────");

            // creat special directory for all services to go
            if (!Directory.Exists("dumps\\svchost"))
                Directory.CreateDirectory("dumps\\svchost");

            foreach (string service in svchostList) {
                try {
                    sw.Restart();

                    // creat directory for specific service if it doesnt exist
                    if (!Directory.Exists($"dumps\\svchost\\{service}"))
                        Directory.CreateDirectory($"dumps\\svchost\\{service}");

                    // dump service 
                    runCommand($"\"{path}assets\\\"dumper.exe -pid {getService(service)} -l 4 -nh > \"{path}dumps\\svchost\\{service}\\\"{service}_{r.Next(0, 999999999)}.txt");

                    Console.WriteLine($"Dumped service \"{service}\" (took {sw.ElapsedMilliseconds}ms)");
                } catch { Console.WriteLine($"Failed to dump service \"{service}\""); }
            }
        }

        /// <summary>
        /// parse through svchost list and grab only the service name
        /// </summary>
        static void getSvchost() {
            runCommand($"cd {path}assets & tasklist /svc | find \"svchost.exe\" > svchost.log");

            string reader = File.ReadAllText("assets\\svchost.log");
            foreach (string line in reader.Split('\n')) {
                if (line.Length > 5) {
                    string serviceName = line.Substring(35).Replace(" ", "").Replace(",", ".");
                    svchostList.Add(serviceName.Substring(0, serviceName.Length - 1));
                }
            }
        }

        /// <summary>
        /// run any command input through cmd as admin
        /// </summary>
        static void runCommand(string command) {
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
            CMD.WaitForExit();
        }

        /// <summary>
        /// Gets the PID of specifc services
        /// </summary>
        static uint getService(string serviceName) {
            uint processId = 0;
            string qry = "SELECT PROCESSID FROM WIN32_SERVICE WHERE NAME = '" + serviceName + "'";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(qry);

            foreach (System.Management.ManagementObject mngntObj in searcher.Get())
                processId = (uint) mngntObj["PROCESSID"];

            return processId;
        }
    }
}