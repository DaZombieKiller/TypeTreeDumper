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

        static FileVersionInfo VersionInfo;

        static LocalHook AfterEverythingLoadedHook;

        static LocalHook PlayerInitEngineNoGraphicsHook;

        static LocalHook ValidateDatesHook;

        static LocalHook InitializePackageManagerHook;

        Action LegacyHandleEngineLoadCallback;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void AfterEverythingLoadedDelegate(IntPtr app);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        delegate bool PlayerInitEngineNoGraphicsDelegate(IntPtr a, IntPtr b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityVersionDelegate(out UnityVersion version, [MarshalAs(UnmanagedType.LPStr)] string value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetUnityVersionDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr MonoStringToUTF8Delegate(IntPtr monoString);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void InitializePackageManagerDelegate();

        static void AttachToParentConsole()
        {
            Kernel32.FreeConsole();
            Kernel32.AttachConsole(Kernel32.ATTACH_PARENT_PROCESS);
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public EntryPoint(RemoteHooking.IContext context, EntryPointArgs args)
        {
            try
            {
                module = Process.GetCurrentProcess().MainModule;
                VersionInfo = FileVersionInfo.GetVersionInfo(module.FileName);

                // Can cause 2017.1 & 2017.2 to hang, cause is currently unknown but may be
                // related to the engine trying to attach to the package manager.
                if (!(VersionInfo.FileMajorPart == 2017 && VersionInfo.FileMinorPart < 3))
                    AttachToParentConsole();

                OutputPath = args.OutputPath;
                ProjectPath = args.ProjectPath;
                resolver = new DiaSymbolResolver(module);
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
            IntPtr address;

            try
            {
                if (!(VersionInfo.FileMajorPart == 2017 && VersionInfo.FileMinorPart < 3))
                    AttachToParentConsole();

                if (VersionInfo.FileMajorPart == 2017)
                {
                    if (resolver.TryResolve("?Initialize@Api@PackageManager@@IEAAXXZ", out address))
                    {
                        InitializePackageManagerHook = LocalHook.Create(address, new InitializePackageManagerDelegate(InitializePackageManager), null);
                        InitializePackageManagerHook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                    }
                }

                if(VersionInfo.FileMajorPart == 3)
                {
                    GenerateLoaderScript();
                }
                else if (resolver.TryResolveFirstMatching(new Regex(Regex.Escape("?AfterEverythingLoaded@Application@") + "*"), out address))
                {
                    AfterEverythingLoadedHook = LocalHook.Create(address, new AfterEverythingLoadedDelegate(AfterEverythingLoaded), null);
                    AfterEverythingLoadedHook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                }
                else
                {
                    address = resolver.ResolveFirstMatching(new Regex(Regex.Escape("?PlayerInitEngineNoGraphics@") + "*"));
                    PlayerInitEngineNoGraphicsHook = LocalHook.Create(address, new PlayerInitEngineNoGraphicsDelegate(PlayerInitEngineNoGraphics), null);
                    PlayerInitEngineNoGraphicsHook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                }

                // Work around Unity 4.0 to 4.3 licensing bug
                if (VersionInfo.FileMajorPart == 4 && VersionInfo.FileMinorPart <= 3)
                {
                    address = resolver.Resolve("?ValidateDates@LicenseManager@@QAEHPAVDOMDocument@xercesc_3_1@@@Z");
                    ValidateDatesHook = LocalHook.Create(address, new ValidateDatesDelegate(ValidateDates), null);
                    ValidateDatesHook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
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

        static void InitializePackageManager()
        {
            // Stubbed
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

                var mono = FindProcessModule(new Regex("^mono", RegexOptions.IgnoreCase)).BaseAddress;
                var MonoStringToUTF8 = Kernel32.GetProcAddress<MonoStringToUTF8Delegate>(mono, "mono_string_to_utf8");
                version = new UnityVersion(Marshal.PtrToStringAnsi(MonoStringToUTF8(GetUnityVersion())));
            }

            Dumper.Execute(new UnityEngine(version, resolver), OutputPath);
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int ValidateDatesDelegate(IntPtr @this, IntPtr param1);

        static int ValidateDates(IntPtr @this, IntPtr param1)
        {
            Console.WriteLine("Validating dates");
            return 0;
        }

        static bool PlayerInitEngineNoGraphics(IntPtr a, IntPtr b)
        {
            using (HookRuntimeInfo.Handle)
            {
                var bypass = HookRuntimeInfo.Handle.HookBypassAddress;
                var method = Marshal.GetDelegateForFunctionPointer<PlayerInitEngineNoGraphicsDelegate>(bypass);
                method.Invoke(a, b);
            }

            HandleEngineInitialization();
            return true;
        }

        static void AfterEverythingLoaded(IntPtr app)
        {
            using (HookRuntimeInfo.Handle)
            {
                var bypass = HookRuntimeInfo.Handle.HookBypassAddress;
                var method = Marshal.GetDelegateForFunctionPointer<AfterEverythingLoadedDelegate>(bypass);
                method.Invoke(app);
            }
            HandleEngineInitialization();
        }

        static void HandleEngineInitialization()
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
            Console.WriteLine("Generating loader script");
            //Save delegate to prevent pointer pointer from being garbage collected
            LegacyHandleEngineLoadCallback = new Action(HandleEngineInitialization);
            var function = Marshal.GetFunctionPointerForDelegate<Action>(LegacyHandleEngineLoadCallback);
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
