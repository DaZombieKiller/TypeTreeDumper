using System;
using System.Collections.Specialized;

namespace Unity
{
    public unsafe class NativeObject : IDisposable
    {
        NativeObjectV1* nativeObject;

        NativeObjectFactory factory;

        PersistentTypeID persistentTypeID;

        public IntPtr* VirtualFunctionTable => nativeObject->VirtualFunctionTable;

        public int InstanceID => nativeObject->InstanceID;

        public IntPtr Pointer => new IntPtr(nativeObject);

        static readonly BitVector32.Section MemLabelIdentifierSection = BitVector32.CreateSection(1 << 11);

        static readonly BitVector32.Section TemporaryFlagsSection = BitVector32.CreateSection(1 << 0, MemLabelIdentifierSection);

        static readonly BitVector32.Section HideFlagsSection = BitVector32.CreateSection(1 << 6, TemporaryFlagsSection);

        static readonly BitVector32.Section IsPersistentSection = BitVector32.CreateSection(1 << 0, HideFlagsSection);

        static readonly BitVector32.Section CachedTypeIndexSection = BitVector32.CreateSection(1 << 10, IsPersistentSection);


        public NativeObject(IntPtr ptr, NativeObjectFactory factory, PersistentTypeID persistentTypeID)
        {
            if (ptr.ToInt64() == 0) throw new ArgumentException("Object Ptr cannot be null");
            nativeObject = (NativeObjectV1*)ptr;
            this.factory = factory;
            this.persistentTypeID = persistentTypeID;
        }

        public void Dispose()
        {
            if ((IntPtr)nativeObject != IntPtr.Zero)
            {
                factory.DestroyIfNotSingletonOrPersistent(this, persistentTypeID);
            }
        }

        public byte TemporaryFlags
        {
            get { return (byte)nativeObject->bits[TemporaryFlagsSection]; }
            set { nativeObject->bits[TemporaryFlagsSection] = value; }
        }

        public HideFlags HideFlags
        {
            get { return (HideFlags)nativeObject->bits[HideFlagsSection]; }
            set { nativeObject->bits[HideFlagsSection] = (int)value; }
        }

        public bool IsPersistent
        {
            get { return nativeObject->bits[IsPersistentSection] != 0; }
            set { nativeObject->bits[IsPersistentSection] = value ? 1 : 0; }
        }

        public uint CachedTypeIndex
        {
            get { return (uint)nativeObject->bits[CachedTypeIndexSection]; }
            set { nativeObject->bits[CachedTypeIndexSection] = (int)value; }
        }

        struct NativeObjectV1
        {
            public IntPtr* VirtualFunctionTable;
            public int InstanceID;
            public BitVector32 bits;
            // There are more fields but they aren't needed.
        }
    }
}