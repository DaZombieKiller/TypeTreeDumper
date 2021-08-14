using System;
using System.Collections.Specialized;

namespace Unity
{
    public partial class NativeObject
    {
        internal unsafe class V1 : INativeObjectImpl
        {
            static readonly BitVector32.Section MemLabelIdentifierSection = BitVector32.CreateSection(1 << 12);

            static readonly BitVector32.Section IsRootOwnerSection = BitVector32.CreateSection(1 << 0, MemLabelIdentifierSection);

            static readonly BitVector32.Section TemporaryFlagsSection = BitVector32.CreateSection(1 << 0, IsRootOwnerSection);

            static readonly BitVector32.Section HideFlagsSection = BitVector32.CreateSection(1 << 3, TemporaryFlagsSection);

            static readonly BitVector32.Section IsPersistentSection = BitVector32.CreateSection(1 << 0, HideFlagsSection);

            static readonly BitVector32.Section CachedTypeIndexSection = BitVector32.CreateSection(1 << 11, IsPersistentSection);

            NativeObject* nativeObject;

            public int InstanceID => nativeObject->InstanceID;

            public IntPtr Pointer => new IntPtr(nativeObject);

            public byte TemporaryFlags
            {
                get { return (byte)nativeObject->bits[TemporaryFlagsSection]; }
            }

            public HideFlags HideFlags
            {
                get { return (HideFlags)nativeObject->bits[HideFlagsSection]; }
            }

            public bool IsPersistent
            {
                get { return nativeObject->bits[IsPersistentSection] != 0; }
            }

            public uint CachedTypeIndex
            {
                get { return (uint)nativeObject->bits[CachedTypeIndexSection]; }
            }

            public V1(IntPtr ptr)
            {
                nativeObject = (NativeObject*)ptr;
            }

            internal struct NativeObject
            {
                public IntPtr* VirtualFunctionTable;
                public int InstanceID;
                public BitVector32 bits;
                // There are more fields but they aren't needed.
            }
        }
    }
}
