using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity;

namespace TypeTreeDumper
{
    static class Dumper
    {
        static ExportOptions Options = new();

        public static void Execute(UnityEngine engine, ExportOptions options, DumperEngine dumperEngine)
        {
            Options = options;
            Logger.Info($"Starting export. UnityVersion {engine.Version}.");
            Directory.CreateDirectory(Options.OutputDirectory);
            TransferInstructionFlags releaseFlags = Options.TransferFlags | TransferInstructionFlags.SerializeGameRelease;
            TransferInstructionFlags editorFlags = Options.TransferFlags & (~TransferInstructionFlags.SerializeGameRelease);
            var info = UnityInfo.Create(engine, releaseFlags, editorFlags);
            if (options.ExportClassesJson)
            {
                ExportClassesJson(info);
            }
            if (options.ExportTextDump)
            {
                FieldValuesJsonDumper.ExportFieldValuesJson(engine, Path.Combine(Options.OutputDirectory, "fieldValues.json"));
                ExportRTTI(info);
                ExportStructDump(info, "structs.dump", true);
                ExportStructDump(info, "editor_structs.dump", false);
                ExportInfoJson(info);
            }
            if (options.ExportBinaryDump)
            {
                if (engine.Version >= UnityVersion.Unity5_0)
                {
                    ExportStringData(engine.CommonString);
                }
                ExportStructData(engine, "structs.dat", releaseFlags);
                ExportStructData(engine, "editor_structs.dat", editorFlags);
            }
            dumperEngine.InvokeExportCompleted(engine, options);
            Logger.Info("Success");
        }

        static void ExportRTTI(UnityInfo info)
        {
            Logger.Info("Writing RTTI...");
            using var tw = new StreamWriter(Path.Combine(Options.OutputDirectory, "RTTI.dump"));
            foreach (var type in info.Classes.OrderBy(x => x.TypeID))
            {
                tw.WriteLine($"PersistentTypeID {type.TypeID}");
                tw.WriteLine($"    Name {type.Name}");
                tw.WriteLine($"    Namespace {type.Namespace}");
                tw.WriteLine($"    Module {type.Module}");
                tw.WriteLine($"    Base {type.Base ?? ""}");
                tw.WriteLine($"    DescendantCount {type.DescendantCount}");
                tw.WriteLine($"    IsAbstract {type.IsAbstract}");
                tw.WriteLine($"    IsSealed {type.IsSealed}");
                tw.WriteLine($"    IsStripped {type.IsStripped}");
                tw.WriteLine($"    IsEditorOnly {type.IsEditorOnly}");
                tw.WriteLine();
            }
        }

        static void ExportStringData(CommonString strings)
        {
            byte[] data = strings.GetData();
            if (data.Length == 0)
                return;

            Logger.Info("Writing common string buffer...");
            File.WriteAllBytes(Path.Combine(Options.OutputDirectory, "strings.dat"), data);
        }

        static void ExportClassesJson(UnityInfo info)
        {
            Logger.Info("Writing classes.json...");
            using var tw = new StreamWriter(Path.Combine(Options.OutputDirectory, "classes.json"));
            tw.WriteLine("{");

            IEnumerable<string> entries = from type in info.Classes.OrderBy(x => (int)x.TypeID) select $"  \"{(int)type.TypeID}\": \"{type.Name}\"";
            var json = string.Join(',' + tw.NewLine, entries);

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
            var typeCount = 0;
            //Later will be overwritten with actual type count
            bw.Write(typeCount);

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

        static void ExportStructDump(UnityInfo info, string fileName, bool isRelease)
        {
            Logger.Info("Writing structure information dump...");
            using var tw = new StreamWriter(Path.Combine(Options.OutputDirectory, fileName));

            int typeCount = 0;
            foreach (var type in info.Classes.OrderBy(x => (int)x.TypeID))
            {
                var iter = type;
                var inheritance = string.Empty;

                Logger.Verb("[{0}] Child: {1}::{2}, {3}, {4}",
                    typeCount,
                    type.Namespace,
                    type.Name,
                    type.Module,
                    type.TypeID
                );

                Logger.Verb("[{0}] Getting base type...", typeCount);
                while (true)
                {
                    inheritance += iter.Name;

                    if (string.IsNullOrEmpty(iter.Base))
                        break;

                    inheritance += " <- ";
                    iter = info.Classes.Single(c => c.Name == iter.Base);
                }

                tw.WriteLine("\n// classID{{{0}}}: {1}", (int)type.TypeID, inheritance);
                iter = type;

                while (iter.IsAbstract)
                {
                    tw.WriteLine("// {0} is abstract", iter.Name);

                    if (string.IsNullOrEmpty(iter.Base))
                        break;

                    iter = info.Classes.Single(c => c.Name == iter.Base);
                }

                Logger.Verb("[{0}] Base: {1}::{2}, {3}, {4}",
                    typeCount,
                    iter.Namespace,
                    iter.Name,
                    iter.Module,
                    iter.TypeID
                );

                var tree = isRelease ? iter.ReleaseRootNode : iter.EditorRootNode;
                if(tree != null)
                    TypeTreeUtility.CreateTextDump(tree, tw);

                typeCount++;
            }
        }

        static void ExportInfoJson(UnityInfo info)
        {
            Logger.Info("Writing information json...");
            using var sw = new StreamWriter(Path.Combine(Options.OutputDirectory, "info.json"));
            using var jw = new JsonTextWriter(sw) { Indentation = 1, IndentChar = '\t' };
            new JsonSerializer { Formatting = Formatting.Indented }.Serialize(jw, info);
        }
    }
}
