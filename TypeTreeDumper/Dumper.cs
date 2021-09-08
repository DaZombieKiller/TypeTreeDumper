using System;
using System.IO;
using System.Text;
using System.Linq;
using Unity;

namespace TypeTreeDumper
{
    static class Dumper
    {
        static ExportOptions Options;

        public static void Execute(UnityEngine engine, ExportOptions options, DumperEngine dumperEngine)
        {
            Options = options;
            Logger.Info($"Starting export. UnityVersion {engine.Version}.");
            Directory.CreateDirectory(Options.OutputDirectory);
            TransferInstructionFlags releaseFlags = Options.TransferFlags | TransferInstructionFlags.SerializeGameRelease;
            TransferInstructionFlags editorFlags = Options.TransferFlags & (~TransferInstructionFlags.SerializeGameRelease);
            if (engine.Version >= UnityVersion.Unity5_0 && options.ExportBinaryDump)
            {
                ExportStringData(engine.CommonString);
            }
            if (options.ExportClassesJson)
            {
                ExportClassesJson(engine.RuntimeTypes);
            }
            if (options.ExportTextDump)
            {
                ExportRTTI(engine.RuntimeTypes);
                ExportStructDump(engine, "structs.dump", releaseFlags);
                ExportStructDump(engine, "editor_structs.dump", editorFlags);
            }
            if (options.ExportBinaryDump)
            {
                ExportStructData(engine, "structs.dat", releaseFlags);
                ExportStructData(engine, "editor_structs.dat", editorFlags);
            }
            dumperEngine.InvokeExportCompleted(engine, options);
            Logger.Info("Success");
        }

        static void ExportRTTI(RuntimeTypeArray runtimeTypes)
        {
            Logger.Info("Writing RTTI...");
            using var tw = new StreamWriter(Path.Combine(Options.OutputDirectory, "RTTI.dump"));
            foreach (var type in runtimeTypes.ToArray().OrderBy(x => (int)x.PersistentTypeID))
            {
                tw.WriteLine($"PersistentTypeID {(int)type.PersistentTypeID}");
                tw.WriteLine($"    Name {type.Name}");
                tw.WriteLine($"    Namespace {type.Namespace}");
                tw.WriteLine($"    Module {type.Module}");
                tw.WriteLine($"    Base {type.Base?.Name ?? ""}");
                tw.WriteLine($"    DescendantCount {type.DescendantCount}");
                tw.WriteLine($"    IsAbstract {type.IsAbstract}");
                tw.WriteLine($"    IsSealed {type.IsSealed}");
                tw.WriteLine($"    IsStripped {type.IsStripped}");
                tw.WriteLine($"    IsEditorOnly {type.IsEditorOnly}");
                tw.WriteLine();
            }
        }

        unsafe static void ExportStringData(CommonString strings)
        {
            if (strings.BufferBegin == IntPtr.Zero || strings.BufferEnd == IntPtr.Zero)
                return;

            Logger.Info("Writing common string buffer...");
            var source = (byte*)strings.BufferBegin;
            var length = (byte*)strings.BufferEnd - source - 1;
            var buffer = new byte[length];

            fixed (byte* destination = buffer)
                Buffer.MemoryCopy(source, destination, length, length);

            File.WriteAllBytes(Path.Combine(Options.OutputDirectory, "strings.dat"), buffer);
        }

        unsafe static void ExportClassesJson(RuntimeTypeArray runtimeTypes)
        {
            Logger.Info("Writing classes.json...");
            using var tw = new StreamWriter(Path.Combine(Options.OutputDirectory, "classes.json"));
            tw.WriteLine("{");

            var entries = from type in runtimeTypes.OrderBy(x => (int)x.PersistentTypeID) select $"  \"{(int)type.PersistentTypeID}\": \"{type.Name}\"";
            var json    = string.Join(',' + tw.NewLine, entries);

            tw.WriteLine(json);
            tw.WriteLine("}");
        }

        unsafe static void ExportStructData(UnityEngine engine, string fileName, TransferInstructionFlags flags)
        {
            Logger.Info("Writing structure information...");
            using var bw = new BinaryWriter(File.OpenWrite(Path.Combine(Options.OutputDirectory, fileName)));

            bw.Write(Encoding.UTF8.GetBytes(engine.Version.ToString()));
            bw.Write((byte)0);

            bw.Write((int)RuntimePlatform.WindowsEditor);
            bw.Write((byte)1); // hasTypeTrees

            var countPosition = (int)bw.BaseStream.Position;
            var typeCount     = 0;

            Logger.Verb("Writing runtime types...");
            foreach(var type in engine.RuntimeTypes.ToArray().OrderBy(x => (int)x.PersistentTypeID))
            {
                var iter = type;

                Logger.Verb("[{0}] Child: {1}::{2}, {3}, {4}",
                    typeCount,
                    type.Namespace,
                    type.Name,
                    type.Module,
                    type.PersistentTypeID
                );

                Logger.Verb("[{0}] Getting base type...", typeCount);
                while (iter.IsAbstract)
                {
                    if (iter.Base == null)
                        break;

                    iter = iter.Base;
                }

                Logger.Verb("[{0}] Base: {1}::{2}, {3}, {4}",
                    typeCount,
                    iter.Namespace,
                    iter.Name,
                    iter.Module,
                    iter.PersistentTypeID
                );

                Logger.Verb("[{0}] Producing native object...", typeCount);
                using var obj = engine.ObjectFactory.GetOrProduce(iter);

                if (obj == null)
                    continue;

                Logger.Verb("[{0}] Produced object {1}. Persistent = {2}.", typeCount, obj.InstanceID, obj.IsPersistent);
                Logger.Verb("[{0}] Generating type tree...", typeCount);
                var tree = engine.TypeTreeFactory.GetTypeTree(obj, flags);

                Logger.Verb("[{0}] Getting GUID...", typeCount);
                bw.Write((int)iter.PersistentTypeID);
                for (int j = 0, n = iter.PersistentTypeID < 0 ? 0x20 : 0x10; j < n; ++j)
                    bw.Write((byte)0);

                TypeTreeUtility.CreateBinaryDump(tree, bw);
                typeCount++;
            }

            bw.Seek(countPosition, SeekOrigin.Begin);
            bw.Write(typeCount);
        }

        unsafe static void ExportStructDump(UnityEngine engine, string fileName, TransferInstructionFlags flags)
        {
            Logger.Info("Writing structure information dump...");
            using var tw = new StreamWriter(Path.Combine(Options.OutputDirectory, fileName));

            int typeCount = 0;
            foreach(var type in engine.RuntimeTypes.ToArray().OrderBy(x => (int)x.PersistentTypeID))
            {
                var iter        = type;
                var inheritance = string.Empty;

                Logger.Verb("[{0}] Child: {1}::{2}, {3}, {4}",
                    typeCount,
                    type.Namespace,
                    type.Name,
                    type.Module,
                    type.PersistentTypeID
                );

                Logger.Verb("[{0}] Getting base type...", typeCount);
                while (true)
                {
                    inheritance += iter.Name;

                    if (iter.Base == null)
                        break;

                    inheritance += " <- ";
                    iter         = iter.Base;
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

                Logger.Verb("[{0}] Base: {1}::{2}, {3}, {4}",
                    typeCount,
                    iter.Namespace,
                    iter.Name,
                    iter.Module,
                    iter.PersistentTypeID
                );

                Logger.Verb("[{0}] Producing native object...", typeCount);
                using var obj = engine.ObjectFactory.GetOrProduce(iter);

                if (obj == null)
                    continue;

                Logger.Verb("[{0}] Produced object {1}. Persistent = {2}.", typeCount, obj.InstanceID, obj.IsPersistent);
                Logger.Verb("[{0}] Generating type tree...", typeCount);
                var tree = engine.TypeTreeFactory.GetTypeTree(obj, flags);
                TypeTreeUtility.CreateTextDump(tree, tw);

                typeCount++;
            }
        }
    }
}
