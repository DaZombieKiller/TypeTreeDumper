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

        static FallbackLoader.CallbackDelegate FallbackLoaderCallback;

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
                    if (resolver.TryResolve($"?Initialize@Api@PackageManager@@I{NameMangling.Ptr64}AAXXZ", out address))
                    {
                        InitializePackageManagerHook = LocalHook.Create(address, new InitializePackageManagerDelegate(InitializePackageManager), null);
                        InitializePackageManagerHook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
                    }
                }

                if (VersionInfo.FileMajorPart == 3)
                {
                    InitializeFallbackLoader();
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
                    address = resolver.Resolve($"?ValidateDates@LicenseManager@@QAEHP{NameMangling.Ptr64}AVDOMDocument@xercesc_3_1@@@Z");
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

            if (resolver.TryResolveFunction($"?GameEngineVersion@PlatformWrapper@UnityEngine@@SAP{NameMangling.Ptr64}BDXZ", out GetUnityVersion))
            {
                var ParseUnityVersion = resolver.ResolveFunction<UnityVersionDelegate>(
                    $"??0UnityVersion@@Q{NameMangling.Ptr64}AA@P{NameMangling.Ptr64}BD@Z",
                    $"??0UnityVersion@@QAE@P{NameMangling.Ptr64}BD@Z"
                );

                ParseUnityVersion(out version, Marshal.PtrToStringAnsi(GetUnityVersion()));
            }
            else
            {
                GetUnityVersion = resolver.ResolveFunction<GetUnityVersionDelegate>(
                    $"?Application_Get_Custom_PropUnityVersion@@YAP{NameMangling.Ptr64}AUMonoString@@XZ",
                    $"?Application_Get_Custom_PropUnityVersion@@YAP{NameMangling.Ptr64}AVScriptingBackendNativeStringPtrOpaque@@XZ"
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

        void InitializeFallbackLoader()
        {
            Console.WriteLine("Initializing fallback loader...");
            FallbackLoaderCallback = new FallbackLoader.CallbackDelegate(HandleEngineInitialization);
            var source      = typeof(FallbackLoader).Assembly.Location;
            var destination = Path.Combine(ProjectPath, "Assets");
            var address     = Marshal.GetFunctionPointerForDelegate(FallbackLoaderCallback).ToInt64();

            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            File.Copy(source, Path.Combine(destination, Path.GetFileName(source)), overwrite: true);
            Environment.SetEnvironmentVariable(FallbackLoader.CallbackAddressName, address.ToString());
        }
    }
}
