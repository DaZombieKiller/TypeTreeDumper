using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TerraFX.Interop.Windows;
using Unity;

namespace TypeTreeDumper
{
    public unsafe class DiaSymbolResolver : SymbolResolver
    {
        readonly ThreadLocal<ComPtr<IDiaSession>> session;

        readonly ProcessModule module;

        readonly ConcurrentDictionary<string, IntPtr> cache;

        public DiaSymbolResolver(ProcessModule module)
        {
            session      = new ThreadLocal<ComPtr<IDiaSession>>(CreateSession);
            this.module  = module;
            cache        = new ConcurrentDictionary<string, IntPtr>();
        }

        public override IEnumerable<string> FindSymbolsMatching(Regex expression)
        {
            var options = NameSearchOptions.RegularExpression;

            if (expression.Options.HasFlag(RegexOptions.IgnoreCase))
                options |= NameSearchOptions.CaseInsensitive;

            using ComPtr<IDiaSymbol> globalScope = default;
            using ComPtr<IDiaEnumSymbols> enumSymbols = default;
            int count = InitializeAndGetCount();

            for (int i = 0; i < count; i++)
            {
                yield return GetSymbolName(i);
            }

            int InitializeAndGetCount()
            {
                session.Value.Get()->get_globalScope(globalScope.GetAddressOf());

                fixed (char* pExpression = expression.ToString())
                    globalScope.Get()->findChildren(SymTagEnum.SymTagPublicSymbol, (ushort*)pExpression, (uint)options, enumSymbols.GetAddressOf());

                int count;
                enumSymbols.Get()->get_Count(&count);
                return count;
            }

            string GetSymbolName(int index)
            {
                using ComPtr<IDiaSymbol> symbol = default;
                enumSymbols.Get()->Item((uint)index, symbol.GetAddressOf());
                ushort* name;
                symbol.Get()->get_name(&name);
                return new string((char*)name);
            }
        }

        protected override void* GetAddressOrZero(string name)
        {
            if (cache.TryGetValue(name, out IntPtr address))
                return (void*)address;

            using ComPtr<IDiaSymbol> globalScope      = default;
            using ComPtr<IDiaEnumSymbols> enumSymbols = default;
            session.Value.Get()->get_globalScope(globalScope.GetAddressOf());

            fixed (char* pName = name)
                globalScope.Get()->findChildren(SymTagEnum.SymTagPublicSymbol, (ushort*)pName, 0, enumSymbols.GetAddressOf());

            int count;
            enumSymbols.Get()->get_Count(&count);

            if (count == 0)
                return null;

            uint rva;
            using ComPtr<IDiaSymbol> symbol = default;
            enumSymbols.Get()->Item(0, symbol.GetAddressOf());
            symbol.Get()->get_relativeVirtualAddress(&rva);
            
            address = new IntPtr((nint)module.BaseAddress + rva);
            cache.TryAdd(name, address);
            return (void*)address;
        }

        ComPtr<IDiaSession> CreateSession()
        {
            IDiaDataSource* source;
            ComPtr<IDiaSession> session = default;
            DiaSourceFactory.CreateDiaSource(&source);

            fixed (char* pFileName = module.FileName)
                source->loadDataForExe((ushort*)pFileName, null, null);

            source->openSession(session.GetAddressOf());
            return session;
        }
    }
}
