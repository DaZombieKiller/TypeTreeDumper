using AsmResolver.PE.File;
using AsmResolver.Symbols.Pdb;
using AsmResolver.Symbols.Pdb.Records;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity;

namespace TypeTreeDumper;
internal class AsmSymbolResolver : SymbolResolver
{
    readonly ProcessModule module;

    readonly Dictionary<string, uint> cache = new();

    IntPtr BaseAddress => module.BaseAddress;

    public AsmSymbolResolver(ProcessModule module)
    {
        this.module = module;

        if (!TryGetPaths(module, out string exePath, out string pdbPath))
        {
            throw new ArgumentException("Module paths could not be determined.", nameof(module));
        }

        //This operates on an unsafe assumption that pdb segments match one-to-one with pe sections.
        //While this has been historically true, it is not guaranteed.
        uint[] sectionOffsets = PEFile.FromModuleBaseAddress(BaseAddress).Sections.Select(s => s.Rva).ToArray();

        foreach (ICodeViewSymbol symbol in PdbImage.FromFile(pdbPath).Symbols)
        {
            if (symbol is PublicSymbol publicSymbol)
            {
                if (publicSymbol.SegmentIndex == sectionOffsets.Length + 1)
                {
                    //Ignore these rare symbols. They are just for cfguard.
                }
                else
                {
                    cache.TryAdd(publicSymbol.Name, publicSymbol.Offset + sectionOffsets[publicSymbol.SegmentIndex - 1]);
                }
            }
        }
    }

    public override IEnumerable<string> FindSymbolsMatching(Regex expression)
    {
        foreach (string name in cache.Keys)
        {
            if (expression.IsMatch(name))
            {
                yield return name;
            }
        }
    }

    protected override unsafe void* GetAddressOrZero(string name)
    {
        return cache.TryGetValue(name, out uint offset) ? (void*)(BaseAddress + (nint)offset) : default;
    }

    private static bool TryGetPaths(ProcessModule module, [MaybeNullWhen(false)] out string exePath, [MaybeNullWhen(false)] out string pdbPath)
    {
        exePath = module.FileName;
        if (string.IsNullOrEmpty(exePath))
        {
            pdbPath = default;
            return false;
        }

        string pathWithoutExtension = Path.ChangeExtension(exePath, null);
        foreach (string suffix in new string[] { ".pdb", "_x64.pdb" })
        {
            string path = pathWithoutExtension + suffix;
            if (File.Exists(path))
            {
                pdbPath = path;
                return true;
            }
        }

        pdbPath = default;
        return false;
    }
}
