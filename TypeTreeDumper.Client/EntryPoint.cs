using System;
using System.IO;
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

        static string ProjectPath;

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
        public EntryPoint(RemoteHooking.IContext context, string outputPath, string projectPath)
        {
            try
            {
                AttachToParentConsole();
                OutputPath  = outputPath;
                ProjectPath = projectPath;
                module      = Process.GetCurrentProcess().MainModule;
                resolver    = new DiaSymbolResolver(module);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public void Run(RemoteHooking.IContext context, string outputPath, string projectPath)
        {
            try
            {
                AttachToParentConsole();

                try
                {
                    var patten  = new Regex(Regex.Escape("?AfterEverythingLoaded@Application@") + "*");
                    var address = resolver.ResolveFirstMatching(patten);
                    var hook    = LocalHook.Create(address, new AfterEverythingLoadedDelegate(AfterEverythingLoaded), null);
                    hook.ThreadACL.SetExclusiveACL(Array.Empty<int>());

                    // Work around Unity 4.0 to 4.3 licensing bug
                    if (resolver.TryResolve("?ValidateDates@LicenseManager@@QAEHPAVDOMDocument@xercesc_3_1@@@Z", out var validateDatesAddress))
                    {
                        var validateDatesHook = LocalHook.Create(validateDatesAddress, new LicenseManager_ValidateDatesDelegate(LicenseManager_ValidateDates), null);
                        validateDatesHook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                    }
                }
                catch (NotSupportedException)
                {
                    // EasyHook can't handle Unity 3.4 and 3.5, use script loader method instead
                    Console.WriteLine("Could not hook AfterEverythingLoaded, using Script Loader");
                    GenerateLoaderScript();
                }

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

                var mono             = FindProcessModule(new Regex("^mono", RegexOptions.IgnoreCase)).BaseAddress;
                var MonoStringToUTF8 = Kernel32.GetProcAddress<MonoStringToUTF8Delegate>(mono, "mono_string_to_utf8");
                version              = new UnityVersion(Marshal.PtrToStringAnsi(MonoStringToUTF8(GetUnityVersion())));
            }

            Dumper.Execute(new UnityEngine(version, resolver), OutputPath);
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

            HandleAfterEverythingLoaded();
        }

        void HandleAfterEverythingLoaded()
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

        void GenerateLoaderScript()
        {
            var function  = Marshal.GetFunctionPointerForDelegate<Action>(HandleAfterEverythingLoaded);
            var assetsDir = Path.Combine(ProjectPath, "Assets");

            if (!Directory.Exists(assetsDir))
                Directory.CreateDirectory(assetsDir);

            var loader = string.Format(LoaderTemplate.Trim(), function.ToInt64());
            File.WriteAllText(Path.Combine(assetsDir, "Loader.cs"), loader);
        }

        const string LoaderTemplate = @"
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;

public class Loader
{{
    public static void Load()
    {{
        var ptr = new IntPtr(0x{0:X});
        var del = Marshal.GetDelegateForFunctionPointer(ptr, typeof(Action));
        del.DynamicInvoke();
    }}
}}
";
    }
}
