using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public abstract class SymbolResolver
    {
        public abstract IntPtr Resolve(string name);

        public unsafe bool TryResolve<T>(string name, out T* address)
            where T : unmanaged
        {
            address = (T*)Resolve(name);
            return address != null;
        }

        public unsafe bool TryResolveFunction<T>(string name, out T del)
            where T : Delegate
        {
            var address = Resolve(name);
            del = Marshal.GetDelegateForFunctionPointer<T>(address);
            return del != null;
        }

        public T ResolveFunction<T>(string name)
            where T : Delegate
        {
            if (TryResolveFunction<T>(name, out T del))
                return del;

            throw new UnresolvedSymbolException(name);
        }

        public unsafe T* Resolve<T>(string name)
            where T : unmanaged
        {
            if (TryResolve<T>(name, out T* address))
                return address;

            throw new UnresolvedSymbolException(name);
        }
    }
}
