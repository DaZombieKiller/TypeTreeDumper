using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Unity
{
    public abstract unsafe class SymbolResolver
    {
        protected abstract void* GetAddressOrZero(string name);

        public abstract IEnumerable<string> FindSymbolsMatching(Regex expression);

        public void* Resolve(string name)
        {
            if (TryResolve(name, out void* address))
                return address;

            throw new UnresolvedSymbolException(name);
        }

        public void* Resolve(params string[] names)
        {
            foreach (string name in names)
            {
                if (TryResolve(name, out void* address))
                    return address;
            }

            throw new UnresolvedSymbolException(string.Join(", ", names));
        }

        public bool TryResolve(string name, out void* address)
        {
            address = GetAddressOrZero(name);
            return address != null;
        }

        public unsafe bool TryResolve<T>(string name, out T* address)
            where T : unmanaged
        {
            address = (T*)GetAddressOrZero(name);
            return address != null;
        }

        public unsafe T* Resolve<T>(string name)
            where T : unmanaged
        {
            if (TryResolve(name, out T* address))
                return address;

            throw new UnresolvedSymbolException(name);
        }

        public unsafe T* Resolve<T>(params string[] names)
            where T : unmanaged
        {
            foreach (string name in names)
            {
                if (TryResolve(name, out T* address))
                    return address;
            }

            throw new UnresolvedSymbolException(string.Join(", ", names));
        }

        public bool TryResolveFirstMatch(Regex regex, out void* address)
        {
            var name = FindSymbolsMatching(regex).FirstOrDefault();

            if (string.IsNullOrEmpty(name))
            {
                address = null;
                return false;
            }

            address = GetAddressOrZero(name);
            return address != null;
        }

        public unsafe bool TryResolveFirstMatch<T>(Regex regex, out T* address)
            where T : unmanaged
        {
            bool success = TryResolveFirstMatch(regex, out void* ptr);
            address      = (T*)ptr;
            return success;
        }

        public void* ResolveFirstMatch(Regex regex)
        {
            var name = FindSymbolsMatching(regex).FirstOrDefault();

            if (string.IsNullOrEmpty(name))
                throw new UnresolvedSymbolException(regex.ToString());

            return GetAddressOrZero(name);
        }

        public void* ResolveFirstMatch(params Regex[] regexes)
        {
            string name = null;

            foreach (var regex in regexes)
            {
                name = FindSymbolsMatching(regex).FirstOrDefault();

                if (name != null)
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(name))
                throw new UnresolvedSymbolException(string.Join(", ", (object[])regexes));

            return GetAddressOrZero(name);
        }

        public unsafe T* ResolveFirstMatch<T>(Regex regex)
            where T : unmanaged
        {
            return (T*)ResolveFirstMatch(regex);
        }
    }
}
