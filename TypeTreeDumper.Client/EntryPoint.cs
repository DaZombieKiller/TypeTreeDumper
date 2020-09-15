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

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public EntryPoint(RemoteHooking.IContext context, string channelName)
        {
            server = RemoteHooking.IpcConnectClient<IpcInterface>(channelName);
            Console.SetIn(server.In);
            Console.SetOut(new TextWriterWrapper(server.Out));
            Console.SetError(new TextWriterWrapper(server.Error));

            try
            {
                module   = Process.GetCurrentProcess().MainModule;
                resolver = new DiaSymbolResolver(module);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public void Run(RemoteHooking.IContext context, string channelName)
        {
            try
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
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }

        ProcessModule GetMonoModule()
        {
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (module.ModuleName.StartsWith("mono"))
                    return module;
            }

            throw new MissingModuleException("mono");
        }

        void ExecuteDumper()
        {
            UnityVersion version;
            GetUnityVersionDelegate GetUnityVersion;

            if (resolver.TryResolveFunction("?GameEngineVersion@PlatformWrapper@UnityEngine@@SAPEBDXZ", out GetUnityVersion))
            {
                var ParseUnityVersion = resolver.ResolveFunction<UnityVersionDelegate>("??0UnityVersion@@QEAA@PEBD@Z");
                ParseUnityVersion(out version, Marshal.PtrToStringAnsi(GetUnityVersion()));
            }
            else
            {
                GetUnityVersion = resolver.ResolveFunction<GetUnityVersionDelegate>(
                    "?Application_Get_Custom_PropUnityVersion@@YAPAUMonoString@@XZ",
                    "?Application_Get_Custom_PropUnityVersion@@YAPEAUMonoString@@XZ",
                    "?Application_Get_Custom_PropUnityVersion@@YAPEAVScriptingBackendNativeStringPtrOpaque@@XZ"
                );

                var mono             = GetMonoModule().BaseAddress;
                var MonoStringToUTF8 = Kernel32.GetProcAddress<MonoStringToUTF8Delegate>(mono, "mono_string_to_utf8");
                version              = new UnityVersion(Marshal.PtrToStringAnsi(MonoStringToUTF8(GetUnityVersion())));
            }

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
