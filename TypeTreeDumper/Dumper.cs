using System;
using System.IO;
using System.Text;
using System.Linq;
using Unity;

namespace TypeTreeDumper
{
    static class Dumper
    {
        static string OutputDirectory;

        public static void Execute(UnityEngine engine, string outputDirectory)
        {
            OutputDirectory = outputDirectory;
            Console.WriteLine($"Starting export. UnityVersion {engine.Version}.");
            Directory.CreateDirectory(OutputDirectory);
            if (engine.Version >= UnityVersion.Unity5_0)
            {
                ExportStringData(engine.CommonString);
            }
            ExportClassesJson(engine.RuntimeTypes);
            ExportRTTI(engine.RuntimeTypes);
            ExportStructDump(engine);
            ExportStructData(engine);
            Console.WriteLine("Success");
        }

        static void ExportRTTI(RuntimeTypeArray runtimeTypes)
        {
            Console.WriteLine("Writing RTTI...");
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "RTTI.dump"));
            for (int i = 0; i < runtimeTypes.Count; i++)
            {
                var type = runtimeTypes[i];
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

        unsafe static void ExportStringData(CommonString strings)
        {
            if (strings.BufferBegin == IntPtr.Zero || strings.BufferEnd == IntPtr.Zero)
                return;

            Console.WriteLine("Writing common string buffer...");
            var source = (byte*)strings.BufferBegin;
            var length = (byte*)strings.BufferEnd - source - 1;
            var buffer = new byte[length];

            fixed (byte* destination = buffer)
                Buffer.MemoryCopy(source, destination, length, length);

            File.WriteAllBytes(Path.Combine(OutputDirectory, "strings.dat"), buffer);
        }

        unsafe static void ExportClassesJson(RuntimeTypeArray runtimeTypes)
        {
            Console.WriteLine("Writing classes.json...");
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "classes.json"));
            tw.WriteLine("{");

            var entries = from type in runtimeTypes select $"  \"{(int)type.PersistentTypeID}\": \"{type.Name}\"";
            var json    = string.Join(',' + tw.NewLine, entries);

            tw.WriteLine(json);
            tw.WriteLine("}");
        }

        unsafe static void ExportStructData(UnityEngine engine)
        {
            Console.WriteLine("Writing structure information...");
            var flags    = TransferInstructionFlags.SerializeGameRelease;
            using var bw = new BinaryWriter(File.OpenWrite(Path.Combine(OutputDirectory, "structs.dat")));

            bw.Write(Encoding.UTF8.GetBytes(engine.Version.ToString()));
            bw.Write((byte)0);

            bw.Write((int)RuntimePlatform.WindowsEditor);
            bw.Write((byte)1); // hasTypeTrees

            var countPosition = (int)bw.BaseStream.Position;
            var typeCount     = 0;

            Console.WriteLine("Writing runtime types...");
            for (int i = 0; i < engine.RuntimeTypes.Count; i++)
            {
                var type = engine.RuntimeTypes[i];
                var iter = type;

                Console.WriteLine("[{0}] Child: {1}::{2}, {3}, {4}",
                    i,
                    type.Namespace,
                    type.Name,
                    type.Module,
                    type.PersistentTypeID
                );

                Console.WriteLine("[{0}] Getting base type...", i);
                while (iter.IsAbstract)
                {
                    if (iter.Base == null)
                        break;

                    iter = iter.Base;
                }

                Console.WriteLine("[{0}] Base: {1}::{2}, {3}, {4}",
                    i,
                    iter.Namespace,
                    iter.Name,
                    iter.Module,
                    iter.PersistentTypeID
                );

                Console.WriteLine("[{0}] Producing native object...", i);
                using var obj = engine.ObjectFactory.GetOrProduce(iter);

                if (obj == null)
                    continue;

                Console.WriteLine("[{0}] Produced object {1}. Persistent = {2}.", i, obj.InstanceID, obj.IsPersistent);
                Console.WriteLine("[{0}] Generating type tree...", i);
                var tree = engine.TypeTreeFactory.GetTypeTree(obj, flags);

                Console.WriteLine("[{0}] Getting GUID...", i);
                bw.Write((int)iter.PersistentTypeID);
                for (int j = 0, n = iter.PersistentTypeID < 0 ? 0x20 : 0x10; j < n; ++j)
                    bw.Write((byte)0);

                TypeTreeUtility.CreateBinaryDump(tree, bw);
                typeCount++;
            }

            bw.Seek(countPosition, SeekOrigin.Begin);
            bw.Write(typeCount);
        }

        unsafe static void ExportStructDump(UnityEngine engine)
        {
            Console.WriteLine("Writing structure information dump...");
            var flags    = TransferInstructionFlags.SerializeGameRelease;
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "structs.dump"));

            for (int i = 0; i < engine.RuntimeTypes.Count; i++)
            {
                var type        = engine.RuntimeTypes[i];
                var iter        = type;
                var inheritance = string.Empty;

                Console.WriteLine("[{0}] Child: {1}::{2}, {3}, {4}",
                    i,
                    type.Namespace,
                    type.Name,
                    type.Module,
                    type.PersistentTypeID
                );

                Console.WriteLine("[{0}] Getting base type...", i);
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

                Console.WriteLine("[{0}] Base: {1}::{2}, {3}, {4}",
                    i,
                    iter.Namespace,
                    iter.Name,
                    iter.Module,
                    iter.PersistentTypeID
                );

                Console.WriteLine("[{0}] Producing native object...", i);
                using var obj = engine.ObjectFactory.GetOrProduce(iter);

                if (obj == null)
                    continue;

                Console.WriteLine("[{0}] Produced object {1}. Persistent = {2}.", i, obj.InstanceID, obj.IsPersistent);
                Console.WriteLine("[{0}] Generating type tree...", i);
                var tree = engine.TypeTreeFactory.GetTypeTree(obj, flags);
                TypeTreeUtility.CreateTextDump(tree, tw);
            }
        }
    }
}
