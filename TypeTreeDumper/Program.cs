using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Remoting;
using EasyHook;

namespace TypeTreeDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                args = new[] { @"C:\Program Files\Unity\Hub\Editor\2020.2.0b2\Editor\Unity.exe" };

            var outPath = Path.Combine(Environment.CurrentDirectory, "Output");
            var logPath = Path.Combine(Environment.CurrentDirectory, "Log.txt");
            var project = Path.GetFullPath("DummyProject");
            var command = Directory.Exists(project) ? "projectPath" : "createProject";
            
            foreach (var process in Process.GetProcessesByName("Unity"))
            {
                if (GetProcessPath(process).Equals(args[0], StringComparison.OrdinalIgnoreCase) &&
                    GetProcessCommandLine(process).Contains($"-{command} \"{project}\""))
                {
                    Console.WriteLine("Terminating orphaned editor process {0}...", process.Id);
                    process.Kill();
                }
            }

            string channel = null;
            var server     = new IpcInterface(Console.In, Console.Out, Console.Error, outPath);

            RemoteHooking.IpcCreateServer(ref channel, WellKnownObjectMode.Singleton, server);
            RemoteHooking.CreateAndInject(args[0],
                $"-nographics -batchmode -{command} \"{project}\" -logFile \"{logPath}\"",
                InProcessCreationFlags: 0,
                InjectionOptions.DoNotRequireStrongName,
                typeof(EntryPoint).Assembly.Location,
                typeof(EntryPoint).Assembly.Location,
                out int processID,
                channel
            );

            Process.GetProcessById(processID).WaitForExit();
        }

        static ManagementBaseObject GetManagementObjectForProcess(Process process)
        {
            var query          = $"select * from Win32_Process where ProcessId = {process.Id}";
            using var searcher = new ManagementObjectSearcher(query);
            return searcher.Get().OfType<ManagementBaseObject>().FirstOrDefault();
        }

        static string GetProcessCommandLine(Process process)
        {
            using var handle = GetManagementObjectForProcess(process);
            return handle?.Properties["CommandLine"]?.Value?.ToString() ?? string.Empty;
        }

        static string GetProcessPath(Process process)
        {
            using var handle = GetManagementObjectForProcess(process);
            return handle?.Properties["ExecutablePath"]?.Value?.ToString() ?? string.Empty;
        }
    }
}
