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
            var scriptLoader = "";

            foreach (var process in Process.GetProcessesByName("Unity"))
            {
                using var mo       = GetManagementObjectForProcess(process);
                var executablePath = mo.GetPropertyValue("ExecutablePath") as string ?? string.Empty;
                var commandLine    = mo.GetPropertyValue("CommandLine")    as string ?? string.Empty;

                if (executablePath.Equals(args[0], StringComparison.OrdinalIgnoreCase) &&
                    commandLine.Contains($"-{command} \"{project}\""))
                {
                    Console.WriteLine("Terminating orphaned editor process {0}...", process.Id);
                    process.Kill();
                }
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(args[0]);
            string version = versionInfo.FileVersion;
            if (version.StartsWith("3."))
            {
                scriptLoader = " -executeMethod Loader.Load";
            }

            string channel = null;
            var server     = new IpcInterface(Console.In, Console.Out, Console.Error, outPath, project);

            RemoteHooking.IpcCreateServer(ref channel, WellKnownObjectMode.Singleton, server);
            RemoteHooking.CreateAndInject(args[0],
                $"-nographics -batchmode -{command} \"{project}\" -logFile \"{logPath}\"{scriptLoader}",
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
            var query           = $"select * from Win32_Process where ProcessId = {process.Id}";
            using var searcher  = new ManagementObjectSearcher(query);
            using var processes = searcher.Get();
            return processes.OfType<ManagementBaseObject>().FirstOrDefault();
        }
    }
}
