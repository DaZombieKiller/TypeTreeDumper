using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Management;
using System.Diagnostics;
using System.Collections.Generic;
using EasyHook;

namespace TypeTreeDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                args = new[] { @"C:\Program Files\Unity\Hub\Editor\2020.2.0b4\Editor\Unity.exe" };

            var projectDirectory = Path.GetFullPath("DummyProject");
            var commandLineArgs  = new List<string>
            {
                "-nographics",
                "-batchmode",
                "-logFile", Path.Combine(Environment.CurrentDirectory, "Log.txt")
            };

            commandLineArgs.Add(Directory.Exists(projectDirectory) ? "-projectPath" : "-createProject");
            commandLineArgs.Add(projectDirectory);

            foreach (var process in Process.GetProcessesByName("Unity"))
            {
                using var mo       = GetManagementObjectForProcess(process);
                var executablePath = mo.GetPropertyValue("ExecutablePath") as string ?? string.Empty;
                var commandLine    = mo.GetPropertyValue("CommandLine")    as string ?? string.Empty;

                if (executablePath.Equals(args[0], StringComparison.OrdinalIgnoreCase) &&
                    commandLine.Contains(EscapeArgument(projectDirectory)))
                {
                    Console.WriteLine("Terminating orphaned editor process {0}...", process.Id);
                    process.Kill();
                }
            }

            if (FileVersionInfo.GetVersionInfo(args[0]).FileMajorPart == 3)
            {
                commandLineArgs.Add("-executeMethod");
                commandLineArgs.Add("Loader.Load");
            }

            RemoteHooking.CreateAndInject(args[0],
                CreateCommandLine(commandLineArgs),
                InProcessCreationFlags: 0,
                InjectionOptions.DoNotRequireStrongName,
                typeof(EntryPoint).Assembly.Location,
                typeof(EntryPoint).Assembly.Location,
                out int processID,
                new EntryPointArgs
                {
                    OutputPath  = Path.Combine(Environment.CurrentDirectory, "Output"),
                    ProjectPath = projectDirectory
                }
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

        static string EscapeArgument(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return '"' + (arg ?? string.Empty) + '"';

            if (arg.Contains(' ') || arg.Contains('"'))
                return '"' + arg.Replace("\"", "\\\"") + '"';

            return arg;
        }

        static string CreateCommandLine(List<string> args)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < args.Count; i++)
            {
                if (i > 0)
                    sb.Append(' ');

                sb.Append(EscapeArgument(args[i]));
            }

            return sb.ToString();
        }
    }
}
