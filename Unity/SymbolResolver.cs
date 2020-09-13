using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public abstract class SymbolResolver
    {
        protected abstract IntPtr GetAddressOrZero(string name);

        public IntPtr Resolve(string name)
        {
            if (TryResolve(name, out IntPtr address))
                return address;

            throw new UnresolvedSymbolException(name);
        }

        public bool TryResolve(string name, out IntPtr address)
        {
            address = GetAddressOrZero(name);
            return address != IntPtr.Zero;
        }

        public unsafe bool TryResolve<T>(string name, out T* address)
            where T : unmanaged
        {
            address = (T*)GetAddressOrZero(name);
            return address != null;
        }

        public unsafe bool TryResolveFunction<T>(string name, out T del)
            where T : Delegate
        {
            var address = GetAddressOrZero(name);
            del = Marshal.GetDelegateForFunctionPointer<T>(address);
            return del != null;
        }

        public T ResolveFunction<T>(string name)
            where T : Delegate
        {
            if (TryResolveFunction(name, out T del))
                return del;

            throw new UnresolvedSymbolException(name);
        }

        public unsafe T* Resolve<T>(string name)
            where T : unmanaged
        {
            if (TryResolve(name, out T* address))
                return address;

            throw new UnresolvedSymbolException(name);
        }
    }
}
