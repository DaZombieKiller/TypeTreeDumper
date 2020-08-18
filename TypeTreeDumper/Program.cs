using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.Remoting;
using EasyHook;
using System.Runtime.InteropServices;

namespace TypeTreeDumper
{
    class Program
    {
        static Process UnityProcess;

        [DllImport("kernel32")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate bool ConsoleCtrlHandler(CtrlType sig);

        static void Main(string[] args)
        {
            if (args.Length == 0)
                args = new[] { @"C:\Program Files\Unity\Hub\Editor\2020.2.0a19\Editor\Unity.exe" };

            var project    = Path.GetFullPath("DummyProject");
            var command    = Directory.Exists(project) ? "projectPath" : "createProject";
            string channel = null;
            var server     = new IpcInterface();
            RemoteHooking.IpcCreateServer(ref channel, WellKnownObjectMode.Singleton, server);
            RemoteHooking.CreateAndInject(
                args[0],
                $"-logfile - -nographics -batchmode -{command} \"{project}\"",
                0,
                InjectionOptions.DoNotRequireStrongName,
                typeof(EntryPoint).Assembly.Location,
                typeof(EntryPoint).Assembly.Location,
                out int processId,
                channel
            );
            
            UnityProcess = Process.GetProcessById(processId);
            SetConsoleCtrlHandler(OnConsoleCtrl, add: true);
            Console.ReadKey();
            OnConsoleCtrl(CtrlType.CloseEvent);
        }

        static bool OnConsoleCtrl(CtrlType sig)
        {
            if (!UnityProcess.HasExited)
                UnityProcess.Kill();

            Environment.Exit(0);
            return true;
        }

        enum CtrlType
        {
            CEvent,
            BreakEvent,
            CloseEvent,
            LogoffEvent,
            ShutdownEvent
        }
    }
}
