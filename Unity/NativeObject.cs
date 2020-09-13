using System;
using System.Collections.Specialized;

namespace Unity
{
    public partial class NativeObject : IDisposable
    {
        INativeObjectImpl nativeObject;

        NativeObjectFactory factory;

        PersistentTypeID persistentTypeID;

        public int InstanceID => nativeObject.InstanceID;

        public IntPtr Pointer => nativeObject.Pointer;

        static readonly BitVector32.Section MemLabelIdentifierSection = BitVector32.CreateSection(1 << 11);

        static readonly BitVector32.Section TemporaryFlagsSection = BitVector32.CreateSection(1 << 0, MemLabelIdentifierSection);

        static readonly BitVector32.Section HideFlagsSection = BitVector32.CreateSection(1 << 6, TemporaryFlagsSection);

        static readonly BitVector32.Section IsPersistentSection = BitVector32.CreateSection(1 << 0, HideFlagsSection);

        static readonly BitVector32.Section CachedTypeIndexSection = BitVector32.CreateSection(1 << 10, IsPersistentSection);


        public NativeObject(IntPtr ptr, NativeObjectFactory factory, PersistentTypeID persistentTypeID)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));
            nativeObject = new V1(ptr);
            this.factory = factory;
            this.persistentTypeID = persistentTypeID;
        }

        public void Dispose()
        {
            if (nativeObject != null)
            {
                factory.DestroyIfNotSingletonOrPersistent(this, persistentTypeID);
            }
        }

        public byte TemporaryFlags
        {
            get { return (byte)nativeObject.Bits[TemporaryFlagsSection]; }
        }

        public HideFlags HideFlags
        {
            get { return (HideFlags)nativeObject.Bits[HideFlagsSection]; }
        }

        public bool IsPersistent
        {
            get { return nativeObject.Bits[IsPersistentSection] != 0; }
        }

        public uint CachedTypeIndex
        {
            get { return (uint)nativeObject.Bits[CachedTypeIndexSection]; }
        }

        interface INativeObjectImpl
        {
            int InstanceID { get; }
            BitVector32 Bits { get; }
            IntPtr Pointer { get; }
        }
    }
}