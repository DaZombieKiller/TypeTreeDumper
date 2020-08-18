using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Dia2Lib;
using Unity;

namespace TypeTreeDumper
{
    public class DiaSymbolResolver : SymbolResolver
    {
        readonly ThreadLocal<IDiaSession> session;

        readonly ProcessModule module;

        readonly Dictionary<string, IntPtr> cache;

        public DiaSymbolResolver(ProcessModule module)
        {
            session      = new ThreadLocal<IDiaSession>(CreateSession);
            this.module  = module;
            cache        = new Dictionary<string, IntPtr>();
        }

        public override IntPtr Resolve(string name)
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
            cache.Add(name, address);
            return address;
        }

        IDiaSession CreateSession()
        {
            var dia = new DiaSourceClass();
            dia.loadDataForExe(module.FileName, null, null);
            dia.openSession(out IDiaSession session);
            return session;
        }
    }
}
