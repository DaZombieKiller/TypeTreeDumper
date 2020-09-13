using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EasyHook;
using Unity;
using System.IO;

namespace TypeTreeDumper
{
    public class EntryPoint : IEntryPoint
    {
        readonly IpcInterface server;

        readonly ProcessModule module;

        readonly SymbolResolver resolver;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void AfterEverythingLoadedDelegate(IntPtr app);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityVersionDelegate(out UnityVersion version, [MarshalAs(UnmanagedType.LPStr)] string value);

        static UnityVersionDelegate ParseUnityVersion;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        delegate bool IsMainThreadDelegate();

        static IsMainThreadDelegate IsMainThread;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetUnityVersionDelegate();

        static GetUnityVersionDelegate GetUnityVersion;

        bool engineLoaded;

        string OutputDirectory = "Output";

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public EntryPoint(RemoteHooking.IContext context, string channelName)
        {
            module   = Process.GetCurrentProcess().MainModule;
            server   = RemoteHooking.IpcConnectClient<IpcInterface>(channelName);
            resolver = new DiaSymbolResolver(module);
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public void Run(RemoteHooking.IContext context, string channelName)
        {
            try
            {
                // Wait for Unity to initialize before grabbing data
                using (var hook = CreateEngineInitializationHook())
                {
                    server.WriteLine($"Created engine hooks.");
                    RemoteHooking.WakeUpProcess();

                    while (!engineLoaded)
                    {
                        server.Ping();
                        Thread.Sleep(500);
                    }

                    server.WriteLine($"Engine loaded. IsMainThread {IsMainThread()}");
                }
            }
            catch(Exception ex)
            {
                server.WriteLine(ex.ToString());
            }
            finally
            {
                server.WriteLine();
                server.WriteLine("RunLoop Finished");
            }
        }

        LocalHook CreateEngineInitializationHook()
        {
            var address = resolver.Resolve("?AfterEverythingLoaded@Application@@QEAAXXZ");
            var hook    = LocalHook.Create(address, new AfterEverythingLoadedDelegate(AfterEverythingLoaded), null);

            void AfterEverythingLoaded(IntPtr application)
            {
                var bypass = HookRuntimeInfo.Handle.HookBypassAddress;
                var method = Marshal.GetDelegateForFunctionPointer<AfterEverythingLoadedDelegate>(bypass);
                method.Invoke(application);
                engineLoaded = true;
                Dump();
            }

            hook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
            return hook;
        }

        void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            server.WriteLine(args.ExceptionObject.ToString());
        }
        void Dump()
        {
            try
            {
                server.WriteLine("Hello World");
                GetUnityVersion = resolver.ResolveFunction<GetUnityVersionDelegate>("?GameEngineVersion@PlatformWrapper@UnityEngine@@SAPEBDXZ");
                ParseUnityVersion = resolver.ResolveFunction<UnityVersionDelegate>("??0UnityVersion@@QEAA@PEBD@Z");

                if (GetUnityVersion is null || ParseUnityVersion is null)
                {
                    server.WriteLine("Error: Could not resolve UnityEngine::PlatformWrapper::GameEngineVersion and/or UnityVersion::UnityVersion.");
                    return;
                }

                ParseUnityVersion(out UnityVersion version, Marshal.PtrToStringAnsi(GetUnityVersion()));
                server.WriteLine($"UnityVersion {version}");
                if (version >= UnityVersion.Unity2018_1)
                {
                    IsMainThread = resolver.ResolveFunction<IsMainThreadDelegate>("?CurrentThreadIsMainThread@@YA_NXZ");
                }
                else if (version >= UnityVersion.Unity2018_4)
                {
                    IsMainThread = resolver.ResolveFunction<IsMainThreadDelegate>("?IsMainThread@CurrentThread@@YA_NXZ");
                }  else
                {
                    IsMainThread = () => false;
                }
                server.WriteLine($"Starting export. IsMainThread {IsMainThread()}");
                var engine = new UnityEngine(version, resolver);
                ExportStringData(engine);
                ExportClassesJson(engine);
                ExportRTTI(engine);
                ExportStructDump(engine);
                ExportStructData(engine);
            }
            catch(Exception ex)
            {
                server.WriteLine("Error");
                server.WriteLine(ex.ToString());
            }
            finally
            {
                server.WriteLine();
                server.WriteLine("Finished dumping");
                Environment.Exit(0);
            }
        }

        unsafe void ExportRTTI(UnityEngine engine)
        {
            server.WriteLine($"Writing RTTI");
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "RTTI.dump"));
            for (int i = 0; i < engine.RuntimeTypes.Count; i++)
            {
                var type = engine.RuntimeTypes[i];
                tw.WriteLine(i);
                tw.WriteLine($"    Name {type.Name}");
                tw.WriteLine($"    Namespace {type.Namespace}");
                tw.WriteLine($"    Module {type.Module}");
                tw.WriteLine($"    PersistentTypeID {type.PersistentTypeID}");
                tw.WriteLine($"    Size {type.Size}");
                tw.WriteLine($"    TypeIndex {type.TypeIndex}");
                tw.WriteLine($"    DescendantCount {type.DescendantCount}");
                tw.WriteLine($"    IsAbstract {type.IsAbstract}");
                tw.WriteLine($"    IsSealed {type.IsSealed}");
                tw.WriteLine($"    IsStripped {type.IsStripped}");
                tw.WriteLine($"    IsEditorOnly {type.IsEditorOnly}");
                tw.WriteLine($"    Attributes 0x{type.Attributes.ToInt64():X}");
                tw.WriteLine($"    AttributeCount {type.AttributeCount}");
            }
        }


        unsafe void ExportStringData(UnityEngine engine)
        {
            server.WriteLine($"Writing Common Strings");
            var source = (byte*)engine.CommonString.BufferBegin;
            var length = (byte*)engine.CommonString.BufferEnd - source - 1;
            var buffer = new byte[length];
            fixed (byte* destination = buffer)
                Buffer.MemoryCopy(source, destination, length, length);

            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllBytes(Path.Combine(OutputDirectory, "strings.dat"), buffer);
        }

        unsafe void ExportClassesJson(UnityEngine engine)
        {
            server.WriteLine($"Writing Classes JSON");
            Directory.CreateDirectory(OutputDirectory);
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "classes.json"));
            tw.WriteLine("{");
            for (int i = 0; i < engine.RuntimeTypes.Count; i++)
            {
                var type = engine.RuntimeTypes[i];
                var name = type.Name;
                tw.Write("  \"{0}\": \"{1}\"",
                     (int)type.PersistentTypeID, name);
                if (i < engine.RuntimeTypes.Count - 1)
                {
                    tw.Write(",");
                }
                tw.WriteLine();
            }
            tw.WriteLine("}");
        }
        unsafe void ExportStructData(UnityEngine engine)
        {
            server.WriteLine($"Writing Struct Data");
            Directory.CreateDirectory(OutputDirectory);
            var flags = TransferInstructionFlags.SerializeGameRelease;
            using var bw = new BinaryWriter(File.OpenWrite(Path.Combine(OutputDirectory, "structs.dat")));

            foreach (char c in engine.Version.ToString())
                bw.Write((byte)c);
            bw.Write((byte)0);
            bw.Write((int)7); // WindowsEditor
            bw.Write((byte)1); // hasTypeTrees

            var countPosition = (int)bw.BaseStream.Position;
            var typeCount = 0;
            server.WriteLine("Writing RunTimeTypes");
            for (int i = 0; i < engine.RuntimeTypes.Count; i++)
            {
                var type = engine.RuntimeTypes[i];
                var iter = type;
                server.WriteLine("Type {0} Child Class {1}, {2}, {3}, {4}",
                    i,
                    type.Namespace,
                    type.Name,
                    type.Module,
                    type.PersistentTypeID);
                server.WriteLine("Type {0} getting base type", i);
                while (iter.IsAbstract)
                {
                    if (iter.Base == null)
                        break;

                    iter = iter.Base;
                }
                server.WriteLine("Type {0} BaseType is {1}, {2}, {3}, {4}",
                    i,
                    iter.Namespace,
                    iter.Name,
                    iter.Module,
                    iter.PersistentTypeID);
                server.WriteLine("Type {0} Getting native object", i);
                using var obj = engine.ObjectFactory.GetOrProduce(iter);
                if (obj == null)
                    continue;

                server.WriteLine("Type {0} Produced object {1}. Persistant {2}",
                    i, obj.InstanceID, obj.IsPersistent);

                server.WriteLine("Type {0} Getting Type Tree", i);
                var tree = engine.TypeTreeFactory.GetTypeTree(obj, flags);
                bw.Write((int)iter.PersistentTypeID);
                server.WriteLine("Type {0} Getting GUID", i);
                // GUID
                for (int j = 0, n = (int)iter.PersistentTypeID < 0 ? 0x20 : 0x10; j < n; ++j)
                    bw.Write((byte)0);

                TypeTreeUtility.CreateBinaryDump(tree, bw);
                typeCount++;
                server.WriteLine("Type {0} Destroy if Not Singleton or Persistent", i);
            }
            bw.Seek(countPosition, SeekOrigin.Begin);
            bw.Write(typeCount);
        }
        unsafe void ExportStructDump(UnityEngine engine)
        {
            server.WriteLine($"Writing Struct Dump");
            Directory.CreateDirectory(OutputDirectory);
            var flags = TransferInstructionFlags.SerializeGameRelease;
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "structs.dump"));
            for (int i = 0; i < engine.RuntimeTypes.Count; i++)
            {
                var type = engine.RuntimeTypes[i];
                var iter = type;
                var inheritance = string.Empty;
                server.WriteLine("Type {0} Child Class {1}, {2}, {3}, {4}",
                    i,
                    type.Namespace,
                    type.Name,
                    type.Module,
                    type.PersistentTypeID);
                server.WriteLine("Type {0} getting base type", i);
                while (true)
                {
                    inheritance += iter.Name;

                    if (iter.Base == null)
                        break;

                    inheritance += " <- ";
                    iter = iter.Base;
                }


                tw.WriteLine("\n// classID{{{0}}}: {1}", (int)type.PersistentTypeID, inheritance);
                iter = type;

                while (iter.IsAbstract)
                {
                    tw.WriteLine("// {0} is abstract", iter.Name);

                    if (iter.Base == null)
                        break;

                    iter = iter.Base;
                }

                server.WriteLine("Type {0} BaseType is {1}, {2}, {3}, {4}",
                    i,
                    iter.Namespace,
                    iter.Name,
                    iter.Module,
                    iter.PersistentTypeID);

                server.WriteLine("Type {0} Getting native object", i);

                using var obj = engine.ObjectFactory.GetOrProduce(iter);
                if (obj == null)
                    continue;

                server.WriteLine("Type {0} Produced object {1}. Persistant {2}",
                    i, obj.InstanceID, obj.IsPersistent);

                server.WriteLine("Type {0} Getting Type Tree", i);
                var tree = engine.TypeTreeFactory.GetTypeTree(obj, flags);

                TypeTreeUtility.CreateTextDump(tree, tw);

                server.WriteLine("Type {0} Destroy if Not Singleton or Persistent", i);
            }
        }
    }
}
