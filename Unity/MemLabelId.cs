namespace Unity
{
    struct MemLabelId
    {
        public AllocationRootWithSalt RootReference;
        public MemoryLabelIdentifier Identifier;

        public override string ToString()
        {
            return $"MemLabelId Salt: 0x{RootReference.Salt:X} RootReferenceIndex: 0x{RootReference.RootReferenceIndex:X} Identifier: {Identifier}";
        }

        /// <summary>
        /// 2020.2.6 and higher
        /// </summary>
        public static MemLabelId DefaultMemBaseObject_2020_2_6 { get; } = new()
        {
            RootReference = AllocationRootWithSalt.Default,
            Identifier = (MemoryLabelIdentifier)56,
        };

        /// <summary>
        /// 2020.2.6 and higher
        /// </summary>
        public static MemLabelId DefaultMemTypeTree_2020_2_6 { get; } = new()
        {
            RootReference = AllocationRootWithSalt.Default,
            Identifier = (MemoryLabelIdentifier)83,
        };
    }

    struct AllocationRootWithSalt
    {
        public uint Salt;
        public uint RootReferenceIndex;

        public static AllocationRootWithSalt Default { get; } = new()
        {
            Salt = 0,
            RootReferenceIndex = uint.MaxValue
        };
    }

    /// <summary>
    /// Content changes often between Unity versions
    /// </summary>
    enum MemoryLabelIdentifier
    {
    }
}
