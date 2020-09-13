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
                args = new[] { @"C:\Program Files\Unity\Hub\Editor\2020.2.0a19\Editor\Unity.exe" };

            var project    = Path.GetFullPath("DummyProject");
            var command    = Directory.Exists(project) ? "projectPath" : "createProject";
            var processes = Process.GetProcessesByName("unity");
            foreach(var proc in processes)
            {
                var procArgs = GetCommandLine(proc);
                if (procArgs.Contains(project))
                {
                    Console.WriteLine("Killing zombie process");
                    proc.Kill();
                }
                Console.WriteLine(procArgs);
            }
            string channel = null;
            var server     = new IpcInterface();
            RemoteHooking.IpcCreateServer(ref channel, WellKnownObjectMode.Singleton, server);
            RemoteHooking.CreateAndInject(
                args[0],
                $"-nographics -batchmode -{command} \"{project}\" -logFile \"{Directory.GetCurrentDirectory()}\\Log.txt\"",
                0,
                InjectionOptions.DoNotRequireStrongName,
                typeof(EntryPoint).Assembly.Location,
                typeof(EntryPoint).Assembly.Location,
                out int processID,
                channel
            );
            var process = Process.GetProcessById(processID);
            Console.ReadKey();
            if (!process.HasExited)
            {
                Console.WriteLine("Zombie process still alive. Killing process");
                process.Kill();
            }

        }
        private static string GetCommandLine(Process process)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }
        }
    }
}
