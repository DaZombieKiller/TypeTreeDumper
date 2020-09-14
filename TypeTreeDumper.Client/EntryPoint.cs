using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EasyHook;

namespace TypeTreeDumper
{
    public class EntryPoint : IEntryPoint
    {
        static IpcInterface server;

        static ProcessModule module;

        static DiaSymbolResolver resolver;

        static event Action OnEngineInitialized;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void AfterEverythingLoadedDelegate(IntPtr app);

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public EntryPoint(RemoteHooking.IContext context, string channelName)
        {
            module   = Process.GetCurrentProcess().MainModule;
            server   = RemoteHooking.IpcConnectClient<IpcInterface>(channelName);
            resolver = new DiaSymbolResolver(module);
            Console.SetIn(server.In);
            Console.SetOut(server.Out);
            Console.SetError(server.Error);
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public void Run(RemoteHooking.IContext context, string channelName)
        {
            var address    = resolver.Resolve("?AfterEverythingLoaded@Application@@QEAAXXZ");
            using var hook = LocalHook.Create(address, new AfterEverythingLoadedDelegate(AfterEverythingLoaded), null);
            hook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
            OnEngineInitialized += () => Dumper.Execute(resolver, server.OutputDirectory);
            RemoteHooking.WakeUpProcess();

            while (true)
            {
                server.Ping();
                Thread.Sleep(500);
            }
        }

        void AfterEverythingLoaded(IntPtr app)
        {
            using (HookRuntimeInfo.Handle)
            {
                var bypass = HookRuntimeInfo.Handle.HookBypassAddress;
                var method = Marshal.GetDelegateForFunctionPointer<AfterEverythingLoadedDelegate>(bypass);
                method.Invoke(app);
            }

            try
            {
                OnEngineInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
