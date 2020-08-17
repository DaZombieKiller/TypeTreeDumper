using System;
using System.IO;
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
            string channel = null;
            var server     = new IpcInterface();
            RemoteHooking.IpcCreateServer(ref channel, WellKnownObjectMode.Singleton, server);
            RemoteHooking.CreateAndInject(
                args[0],
                $"-nographics -batchmode -{command} \"{project}\"",
                0,
                InjectionOptions.DoNotRequireStrongName,
                typeof(EntryPoint).Assembly.Location,
                typeof(EntryPoint).Assembly.Location,
                out _,
                channel
            );

            Console.ReadKey();
        }
    }
}
