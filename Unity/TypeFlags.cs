using System;

namespace Unity
{
    [Flags]
    public enum TypeFlags : byte
    {
        None                       = 0,
        IsArray                    = 1 << 0,
        IsManagedReference         = 1 << 1,
        IsManagedReferenceRegistry = 1 << 2,
        IsArrayOfRefs              = 1 << 3,
    }

    public static class TypeFlagsExtensions
    {
        public static bool IsArray(this TypeFlags _this)
        {
            return (_this & TypeFlags.IsArray) != 0;
        }
        public static bool IsManagedReference(this TypeFlags _this)
        {
            return (_this & TypeFlags.IsManagedReference) != 0;
        }
        public static bool IsManagedReferenceRegistry(this TypeFlags _this)
        {
            return (_this & TypeFlags.IsManagedReferenceRegistry) != 0;
        }
        public static bool IsArrayOfRefs(this TypeFlags _this)
        {
            return (_this & TypeFlags.IsArrayOfRefs) != 0;
        }
    }
}
