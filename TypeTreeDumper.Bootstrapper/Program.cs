using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Management;
using System.Diagnostics;
using System.Collections.Generic;
using DetourSharp.Hosting;
using CommandLine;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.CREATE;

namespace TypeTreeDumper
{
    class Options
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
        const string DefaultRuntimeConfig = @"{
  ""runtimeOptions"": {
    ""tfm"": ""net6.0"",
    ""rollForward"": ""LatestMinor"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""6.0.0""
    },
    ""configProperties"": {
      ""System.Reflection.Metadata.MetadataUpdater.IsSupported"": false
    }
  }
}";

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options) );
        }

        static unsafe void Run(Options options)
        {
            var version          = FileVersionInfo.GetVersionInfo(options.UnityExePath);
            var projectDirectory = Path.Combine(System.AppContext.BaseDirectory, "DummyProjects", "DummyProject-" + version.FileVersion);
            var commandLineArgs  = new List<string>
            {
                "-nographics",
                "-batchmode",
                "-logFile", Path.Combine(System.AppContext.BaseDirectory, "Log.txt")
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

            STARTUPINFOW si;
            PROCESS_INFORMATION pi;
            
            fixed (char* pAppName = options.UnityExePath)
            fixed (char* pCmdLine = CreateCommandLine(commandLineArgs))
            {
                if (!CreateProcessW((ushort*)pAppName, (ushort*)pCmdLine, null, null, true, CREATE_SUSPENDED, null, null, &si, &pi))
                {
                    Console.WriteLine("Failed to start Unity process.");
                    return;
                }
            }

            // The handle we get from CreateProcessW is only valid for this process,
            // so we need to duplicate the handle to pass it to the Unity editor process.
            HANDLE hThread;
            DuplicateHandle(GetCurrentProcess(), pi.hThread, pi.hProcess, &hThread, 0, true, DUPLICATE_SAME_ACCESS);
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);

            var config = Path.Combine(AppContext.BaseDirectory, "TypeTreeDumper.Bootstrapper.runtimeconfig.json");

            if (!File.Exists(config))
            {
                config = Path.GetTempFileName();
                File.WriteAllText(config, DefaultRuntimeConfig);
            }

            using var runtime = new RemoteRuntime((int)pi.dwProcessId);
            runtime.Initialize(config);
            runtime.Invoke(((Delegate)EntryPoint.Main).Method, new EntryPointArgs
            {
                OutputPath = Path.GetFullPath(options.OutputDirectory ?? Path.Combine(System.AppContext.BaseDirectory, "Output")),
                ProjectPath = projectDirectory,
                Verbose = options.Verbose,
                Silent = options.Silent,
                ThreadHandle = (ulong)hThread,
            });

            Process.GetProcessById((int)pi.dwProcessId).WaitForExit();
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
