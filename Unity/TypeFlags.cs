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
}
