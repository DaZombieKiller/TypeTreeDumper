using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public abstract class SymbolResolver
    {
        public abstract IntPtr Resolve(string name);

        public T ResolveFunction<T>(string name)
            where T : Delegate
        {
            var address = Resolve(name);

            if (address == IntPtr.Zero)
                throw new InvalidOperationException($"Could not find {name}");

            if (address == IntPtr.Zero)
                return null;

            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }

        public unsafe T* Resolve<T>(string name)
            where T : unmanaged
        {
            var address = Resolve(name);

            if (address == IntPtr.Zero)
                throw new InvalidOperationException($"Could not find {name}");

            return (T*)address;
        }
    }
}
