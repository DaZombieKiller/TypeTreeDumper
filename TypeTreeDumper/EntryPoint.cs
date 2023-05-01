using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EasyHook;
using Unity;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace TypeTreeDumper
{
    public static unsafe class EntryPoint
    {
        static ProcessModule module;

        static DiaSymbolResolver resolver;

        static event Action OnEngineInitialized;

        static string OutputPath;

        static string ProjectPath;

        static FileVersionInfo VersionInfo;

        static LocalHook AfterEverythingLoadedDetour;

        static LocalHook PlayerInitEngineNoGraphicsDetour;

        static LocalHook ValidateDatesDetour;

        static LocalHook InitializePackageManagerDetour;

        static void AttachToParentConsole()
        {
            FreeConsole();
            AttachConsole(ATTACH_PARENT_PROCESS);
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }

        static ProcessModule GetUnityModule(Process process)
        {
            if (TryGetModule(process, "Unity.dll", out var module))
                return module;

            return process.MainModule;
        }

        static bool TryGetModule(Process process, string name, out ProcessModule module)
        {
            foreach (ProcessModule entry in process.Modules)
            {
                if (entry.ModuleName == name)
                {
                    module = entry;
                    return true;
                }
            }

            module = null;
            return false;
        }

        public static void Main(EntryPointArgs args)
        {
            try
            {
                VersionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);

                // If we're on a version of Unity after the editor code was split into a
                // separate dynamic library, it won't be loaded at this point. We need to
                // access stuff inside it, so we try to load it here ahead of time.
                NativeLibrary.TryLoad("Unity.dll", out _);

                // Can cause 2017.1 & 2017.2 to hang, cause is currently unknown but may be
                // related to the engine trying to attach to the package manager.
                if (!(VersionInfo.FileMajorPart == 2017 && VersionInfo.FileMinorPart < 3))
                    AttachToParentConsole();

                ConsoleLogger.Initialize(args.Silent, args.Verbose);

                if (!(VersionInfo.FileMajorPart == 2017 && VersionInfo.FileMinorPart < 3))
                    AttachToParentConsole();

                module      = GetUnityModule(Process.GetCurrentProcess());
                OutputPath  = args.OutputPath;
                ProjectPath = args.ProjectPath;
                resolver    = new DiaSymbolResolver(module);
                void* address;

                if (VersionInfo.FileMajorPart == 2017)
                {
                    if (resolver.TryResolve($"?Initialize@Api@PackageManager@@I{NameMangling.Ptr64}AAXXZ", out address))
                    {
                        InitializePackageManagerDetour = LocalHook.CreateUnmanaged((IntPtr)address, (IntPtr)(delegate* unmanaged[Cdecl]<void>)&InitializePackageManager, IntPtr.Zero);
                        InitializePackageManagerDetour.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                    }
                }

                if (VersionInfo.FileMajorPart == 3)
                {
                    InitializeFallbackLoader();
                }
                else if (resolver.TryResolveFirstMatch(new Regex(Regex.Escape("?AfterEverythingLoaded@Application@") + "*"), out address))
                {
                    AfterEverythingLoadedDetour = LocalHook.CreateUnmanaged((IntPtr)address, (IntPtr)(delegate* unmanaged[Cdecl]<void*, void>)&AfterEverythingLoaded, IntPtr.Zero);
                    AfterEverythingLoadedDetour.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                }
                else
                {
                    address = resolver.ResolveFirstMatch(
                        new Regex(Regex.Escape("?PlayerInitEngineNoGraphics@") + "*"),
                        new Regex(Regex.Escape("?InitializeEngineNoGraphics@") + "*")
                    );
                    PlayerInitEngineNoGraphicsDetour = LocalHook.CreateUnmanaged((IntPtr)address, (IntPtr)(delegate* unmanaged[Cdecl]<void*, void*, byte>)&PlayerInitEngineNoGraphics, IntPtr.Zero);
                    PlayerInitEngineNoGraphicsDetour.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                }

                // Work around Unity 4.0 to 4.3 licensing bug
                if (VersionInfo.FileMajorPart == 4 && VersionInfo.FileMinorPart <= 3)
                {
                    address = resolver.Resolve($"?ValidateDates@LicenseManager@@QAEHP{NameMangling.Ptr64}AVDOMDocument@xercesc_3_1@@@Z");
                    ValidateDatesDetour = LocalHook.CreateUnmanaged((IntPtr)address, (IntPtr)(delegate* unmanaged[Thiscall]<void*, void*, int>)&ValidateDates, IntPtr.Zero);
                    ValidateDatesDetour.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                }

                OnEngineInitialized += PluginManager.LoadPlugins;
                OnEngineInitialized += ExecuteDumper;
                ResumeThread((HANDLE)args.ThreadHandle);
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
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
            Logger.Info("Executing Dumper");
            UnityVersion version;
            delegate* unmanaged[Cdecl]<sbyte*> GetUnityVersion;

            var dumperEngine = new DumperEngine();
            PluginManager.InitializePlugins(dumperEngine);

            if (resolver.TryResolve($"?GameEngineVersion@PlatformWrapper@UnityEngine@@SAP{NameMangling.Ptr64}BDXZ", out *(void**)&GetUnityVersion))
            {
                var ParseUnityVersion = (delegate* unmanaged[Cdecl]<UnityVersion*, sbyte*, void>)resolver.Resolve(
                    $"??0UnityVersion@@Q{NameMangling.Ptr64}AA@P{NameMangling.Ptr64}BD@Z",
                    $"??0UnityVersion@@QAE@P{NameMangling.Ptr64}BD@Z"
                );

                ParseUnityVersion(&version, GetUnityVersion());
            }
            else
            {
                *(void**)&GetUnityVersion = resolver.Resolve(
                    $"?Application_Get_Custom_PropUnityVersion@@YAP{NameMangling.Ptr64}AUMonoString@@XZ",
                    $"?Application_Get_Custom_PropUnityVersion@@YAP{NameMangling.Ptr64}AVScriptingBackendNativeStringPtrOpaque@@XZ"
                );

                var mono = FindProcessModule(new Regex("^mono", RegexOptions.IgnoreCase)).BaseAddress;
                var MonoStringToUTF8 = (delegate* unmanaged[Cdecl]<sbyte*, void*>)NativeLibrary.GetExport(mono, "mono_string_to_utf8");
                version = new UnityVersion(Marshal.PtrToStringAnsi((IntPtr)MonoStringToUTF8(GetUnityVersion())));
            }

            Dumper.Execute(new UnityEngine(version, resolver), new ExportOptions(OutputPath), dumperEngine);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void InitializePackageManager()
        {
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvThiscall) })]
        static int ValidateDates(void* @this, void* param1)
        {
            Logger.Info("Validating dates");
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl)})]
        static byte PlayerInitEngineNoGraphics(void* a, void* b)
        {
            ((delegate* unmanaged[Cdecl]<void*, void*, byte>)PlayerInitEngineNoGraphicsDetour.HookBypassAddress)(a, b);
            HandleEngineInitialization();
            return 1;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void AfterEverythingLoaded(void* app)
        {
            ((delegate* unmanaged[Cdecl]<void*, void>)AfterEverythingLoadedDetour.HookBypassAddress)(app);
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
                Logger.Error(ex);
                throw;
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void FallbackLoaderCallback()
        {
            HandleEngineInitialization();
        }

        static void InitializeFallbackLoader()
        {
            Logger.Info("Initializing fallback loader...");
            var source      = typeof(FallbackLoader).Assembly.Location;
            var destination = Path.Combine(ProjectPath, "Assets");
            var address     = (delegate* unmanaged[Cdecl]<void>)&FallbackLoaderCallback;

            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            File.Copy(source, Path.Combine(destination, Path.GetFileName(source)), overwrite: true);
            Environment.SetEnvironmentVariable(FallbackLoader.CallbackAddressName, new IntPtr(address).ToString());
        }
    }
}
