using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Management;
using System.Diagnostics;
using System.Collections.Generic;
using CommandLine;
using EasyHook;

namespace TypeTreeDumper
{
    internal class Options
    {
        [Value(0, Required = true, HelpText = "Path to the Unity executable.")]
        public string UnityExePath { get; set; }

        [Option('o', "output", HelpText = "Directory to export to.")]
        public string OutputDirectory { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Verbose logging output.")]
        public bool Verbose { get; set; }

        [Option('s', "silent", Default = false, HelpText = "No logging output except errors.")]
        public bool Silent { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Main(options) );
        }

        static void Main(Options options)
        {
            var version          = FileVersionInfo.GetVersionInfo(options.UnityExePath);
            var projectDirectory = ExecutingDirectory.Combine("DummyProject-" + version.FileVersion);
            var commandLineArgs  = new List<string>
            {
                "-nographics",
                "-batchmode",
                "-logFile", ExecutingDirectory.Combine("Log.txt")
            };

            if (version.FileMajorPart == 3)
            {
                commandLineArgs.Add("-executeMethod");
                commandLineArgs.Add(string.Join(".", typeof(FallbackLoader).FullName, nameof(FallbackLoader.Initialize)));
            }

            if (version.FileMajorPart >= 2018)
                commandLineArgs.Add("-noUpm");

            commandLineArgs.Add(Directory.Exists(projectDirectory) ? "-projectPath" : "-createProject");
            commandLineArgs.Add(projectDirectory);

            foreach (var process in Process.GetProcessesByName("Unity"))
            {
                using var mo       = GetManagementObjectForProcess(process);
                var executablePath = mo.GetPropertyValue("ExecutablePath") as string ?? string.Empty;
                var commandLine    = mo.GetPropertyValue("CommandLine")    as string ?? string.Empty;

                if (executablePath.Equals(options.UnityExePath, StringComparison.OrdinalIgnoreCase) &&
                    commandLine.Contains(EscapeArgument(projectDirectory)))
                {
                    Console.WriteLine("Terminating orphaned editor process {0}...", process.Id);
                    process.Kill();
                }
            }

            RemoteHooking.CreateAndInject(options.UnityExePath,
                CreateCommandLine(commandLineArgs),
                InProcessCreationFlags: 0,
                InjectionOptions.DoNotRequireStrongName,
                typeof(EntryPoint).Assembly.Location,
                typeof(EntryPoint).Assembly.Location,
                out int processID,
                new EntryPointArgs
                {
                    OutputPath  = (new DirectoryInfo(options.OutputDirectory ?? ExecutingDirectory.Combine("Output"))).FullName,
                    ProjectPath = projectDirectory,
                    Verbose = options.Verbose,
                    Silent = options.Silent
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

            if (arg.Contains('.') || arg.Contains(' ') || arg.Contains('"'))
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
