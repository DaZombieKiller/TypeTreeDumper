using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EasyHook;
using Unity;
using System.Text.RegularExpressions;
using System.IO;

namespace TypeTreeDumper
{
    public class EntryPoint : IEntryPoint
    {
        static IpcInterface server;

        static ProcessModule module;

        static DiaSymbolResolver resolver;

        static event Action OnEngineInitialized;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
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
                try
                {
                    var patten = new Regex(Regex.Escape("?AfterEverythingLoaded@Application@") + "*");
                    var address = resolver.ResolveFirstMatching(patten);
                    var hook = LocalHook.Create(address, new AfterEverythingLoadedDelegate(AfterEverythingLoaded), null);
                    hook.ThreadACL.SetExclusiveACL(Array.Empty<int>());

                    //Work around Unity 4.0 to 4.3 licensing bug
                    if (resolver.TryResolve("?ValidateDates@LicenseManager@@QAEHPAVDOMDocument@xercesc_3_1@@@Z", out var validateDatesAddress))
                    {
                        var validateDatesHook = LocalHook.Create(validateDatesAddress, new LicenseManager_ValidateDatesDelegate(LicenseManager_ValidateDates), null);
                        validateDatesHook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                    }
                } catch(NotSupportedException)
                {
                    //EasyHook can't handle Unity 3.4 and 3.5, use script loader method instead
                    Console.WriteLine("Could not hook AfterEverythingLoaded, using Script Loader");
                    InitScriptLoader();
                }
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
            Console.WriteLine("Executing Dumper");
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

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        unsafe delegate int LicenseManager_ValidateDatesDelegate(IntPtr @this, IntPtr param1);

        unsafe int LicenseManager_ValidateDates(IntPtr @this, IntPtr param1)
        {
            Console.WriteLine("Validating dates");
            return 0;
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

        delegate void ExecuteDumperDelegate();

        void InitScriptLoader()
        {
            var ptr = Marshal.GetFunctionPointerForDelegate(new ExecuteDumperDelegate(ExecuteDumper));
            var del = Marshal.GetDelegateForFunctionPointer<ExecuteDumperDelegate>(ptr);
            var assetsDir = Path.Combine(server.ProjectDirectory, "Assets");
            if(!Directory.Exists(assetsDir)) Directory.CreateDirectory(assetsDir);
            File.WriteAllText($@"{assetsDir}\Loader.cs",
                @"
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;


public class Loader
{
    delegate void ExecuteDumperDelegate();
    public static void Load()
    {
        File.WriteAllText(""Loader.txt"", ""Hello world!"");
        var ptr = new IntPtr(ADDRESS);
        var del = Marshal.GetDelegateForFunctionPointer(ptr, typeof(ExecuteDumperDelegate));
        del.DynamicInvoke();
        EditorApplication.Exit(0);
    }
}".Replace("ADDRESS", ptr.ToInt64().ToString()));
        }
    }
}
