namespace Unity
{
    struct MemLabelId
    {
        public AllocationRootWithSalt RootReference;
        public MemoryLabelIdentifier Identifier;
    }

    struct AllocationRootWithSalt
    {
        public uint Salt;
        public uint RootReferenceIndex;
    }

    enum MemoryLabelIdentifier
    {
    }
}
