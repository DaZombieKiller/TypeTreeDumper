using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Dia2Lib;
using Unity;

namespace TypeTreeDumper
{
    public class DiaSymbolResolver : SymbolResolver
    {
        readonly ThreadLocal<IDiaSession> session;

        readonly ProcessModule module;

        readonly ConcurrentDictionary<string, IntPtr> cache;

        public DiaSymbolResolver(ProcessModule module)
        {
            session      = new ThreadLocal<IDiaSession>(CreateSession);
            this.module  = module;
            cache        = new ConcurrentDictionary<string, IntPtr>();
        }

        public override IEnumerable<string> FindSymbolsMatching(Regex expression)
        {
            var options = NameSearchOptions.RegularExpression;

            if (expression.Options.HasFlag(RegexOptions.IgnoreCase))
                options |= NameSearchOptions.CaseInsensitive;

            session.Value.globalScope.findChildren(
                SymTagEnum.SymTagPublicSymbol,
                expression.ToString(),
                (uint)options,
                out IDiaEnumSymbols symbols
            );

            foreach (IDiaSymbol symbol in symbols)
                yield return symbol.name;
        }

        protected override IntPtr GetAddressOrZero(string name)
        {
            if (cache.TryGetValue(name, out IntPtr address))
                return address;

            session.Value.globalScope.findChildren(
                SymTagEnum.SymTagPublicSymbol,
                name,
                0,
                out IDiaEnumSymbols symbols
            );

            if (symbols.count == 0)
                return IntPtr.Zero;

            var symbol = symbols.Item(0);
            address    = IntPtr.Add(module.BaseAddress, (int)symbol.relativeVirtualAddress);
            cache.TryAdd(name, address);
            return address;
        }

        IDiaSession CreateSession()
        {
            var dia = DiaSourceFactory.CreateInstance();
            dia.loadDataForExe(module.FileName, null, null);
            dia.openSession(out IDiaSession session);
            return session;
        }
    }
}
