using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using EasyHook;
using Unity;

namespace TypeTreeDumper
{
    public class EntryPoint : IEntryPoint
    {
        static ProcessModule module;

        static DiaSymbolResolver resolver;

        static event Action OnEngineInitialized;

        static string OutputPath;

        static DetourHook AfterEverythingLoadedHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void AfterEverythingLoadedDelegate(IntPtr app);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityVersionDelegate(out UnityVersion version, [MarshalAs(UnmanagedType.LPStr)] string value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetUnityVersionDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr MonoStringToUTF8Delegate(IntPtr monoString);

        static void AttachToParentConsole()
        {
            Kernel32.FreeConsole();
            Kernel32.AttachConsole(Kernel32.ATTACH_PARENT_PROCESS);
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public EntryPoint(RemoteHooking.IContext context, EntryPointArgs args)
        {
            try
            {
                AttachToParentConsole();
                OutputPath = args.OutputPath;
                module     = Process.GetCurrentProcess().MainModule;
                resolver   = new DiaSymbolResolver(module);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public void Run(RemoteHooking.IContext context, EntryPointArgs args)
        {
            try
            {
                AttachToParentConsole();
                var pattern = new Regex(Regex.Escape("?AfterEverythingLoaded@Application@") + "*");
                var address = resolver.ResolveFirstMatching(pattern);

                // Hook
                AfterEverythingLoadedHook = DetourHook.Create<AfterEverythingLoadedDelegate>(address, AfterEverythingLoaded);

                // Work around Unity 4.0 to 4.3 licensing bug
                if (resolver.TryResolve("?ValidateDates@LicenseManager@@QAEHPAVDOMDocument@xercesc_3_1@@@Z", out address))
                    DetourHook.Create<ValidateDatesDelegate>(address, ValidateDates);

                OnEngineInitialized += ExecuteDumper;
                RemoteHooking.WakeUpProcess();
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }

        static ProcessModule FindProcessModule(Regex regex)
        {
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (regex.IsMatch(module.ModuleName))
                    return module;
            }

            throw new MissingModuleException(regex.ToString());
        }

        static void ExecuteDumper()
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

                var mono             = FindProcessModule(new Regex("^mono", RegexOptions.IgnoreCase)).BaseAddress;
                var MonoStringToUTF8 = Kernel32.GetProcAddress<MonoStringToUTF8Delegate>(mono, "mono_string_to_utf8");
                version              = new UnityVersion(Marshal.PtrToStringAnsi(MonoStringToUTF8(GetUnityVersion())));
            }

            Dumper.Execute(new UnityEngine(version, resolver), OutputPath);
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int ValidateDatesDelegate(IntPtr @this, IntPtr param1);

        static int ValidateDates(IntPtr @this, IntPtr param1)
        {
            Console.WriteLine("LicenseManager::ValidateDates");
            return 0;
        }

        static void AfterEverythingLoaded(IntPtr app)
        {
            Marshal.GetDelegateForFunctionPointer<AfterEverythingLoadedDelegate>(AfterEverythingLoadedHook.Pointer).Invoke(app);
            AfterEverythingLoadedHook.Dispose();
            HandleAfterEverythingLoaded();
        }

        static void HandleAfterEverythingLoaded()
        {
            try
            {
                AttachToParentConsole();
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
