using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EasyHook;
using Unity;

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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityVersionDelegate(out UnityVersion version, [MarshalAs(UnmanagedType.LPStr)] string value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetUnityVersionDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr MonoStringToUTF8Delegate(IntPtr monoString);

        [DllImport("kernel32")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32")]
        public static extern IntPtr GetModuleHandle(string moduleName);

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public EntryPoint(RemoteHooking.IContext context, string channelName)
        {
            module   = Process.GetCurrentProcess().MainModule;
            server   = RemoteHooking.IpcConnectClient<IpcInterface>(channelName);
            resolver = new DiaSymbolResolver(module);
            Console.SetIn(server.In);
            Console.SetOut(new TextWriterWrapper(server.Out));
            Console.SetError(new TextWriterWrapper(server.Error));
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public void Run(RemoteHooking.IContext context, string channelName)
        {
            var address = resolver.Resolve("?AfterEverythingLoaded@Application@@QEAAXXZ");
            var hook    = LocalHook.Create(address, new AfterEverythingLoadedDelegate(AfterEverythingLoaded), null);
            hook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
            OnEngineInitialized += ExecuteDumper;
            RemoteHooking.WakeUpProcess();

            while (true)
            {
                server.Ping();
                Thread.Sleep(500);
            }
        }

        IntPtr GetMonoHandle()
        {
            foreach(ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (module.ModuleName.StartsWith("mono"))
                {
                    return GetModuleHandle(module.ModuleName);
                }
            }
            throw new Exception("Could not find mono library");
        }

        void ExecuteDumper()
        {
            GetUnityVersionDelegate GetUnityVersion;
            if (!(resolver.TryResolveFunction("?Application_Get_Custom_PropUnityVersion@@YAPAUMonoString@@XZ", out GetUnityVersion) ||
                    resolver.TryResolveFunction("?Application_Get_Custom_PropUnityVersion@@YAPEAUMonoString@@XZ", out GetUnityVersion) ||
                    resolver.TryResolveFunction("?Application_Get_Custom_PropUnityVersion@@YAPEAVScriptingBackendNativeStringPtrOpaque@@XZ", out GetUnityVersion)))
            {
                throw new UnresolvedSymbolException(nameof(GetUnityVersion));
            }
            var mono = GetMonoHandle();
            var address = GetProcAddress(mono, "mono_string_to_utf8");
            var MonoStringToUTF8 = Marshal.GetDelegateForFunctionPointer<MonoStringToUTF8Delegate>(address);
            var ParseUnityVersion = resolver.ResolveFunction<UnityVersionDelegate>("??0UnityVersion@@QEAA@PEBD@Z");
            ParseUnityVersion(out UnityVersion version, Marshal.PtrToStringAnsi(MonoStringToUTF8(GetUnityVersion())));
            Dumper.Execute(new UnityEngine(version, resolver), server.OutputDirectory);
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
