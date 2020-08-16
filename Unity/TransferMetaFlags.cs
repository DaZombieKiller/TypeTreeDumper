using System;

namespace Unity
{
    [Flags]
    public enum TransferMetaFlags
    {
        None                                   = 0,
        HideInEditor                           = 1 << 0,
        NotEditable                            = 1 << 4,
        StrongPPtr                             = 1 << 6,
        TreatIntegerValueAsBoolean             = 1 << 8,
        SimpleEditor                           = 1 << 11,
        DebugProperty                          = 1 << 12,
        AlignBytes                             = 1 << 14,
        AnyChildUsesAlignBytesFlag             = 1 << 15,
        IgnoreWithInspectorUndo                = 1 << 16,
        EditorDisplaysCharacterMap             = 1 << 18,
        IgnoreInMetaFiles                      = 1 << 19,
        TransferAsArrayEntryNameInMetaFiles    = 1 << 20,
        TransferUsingFlowMappingStyle          = 1 << 21,
        GenerateBitwiseDifferences             = 1 << 22,
        DontAnimate                            = 1 << 23,
        TransferHex64                          = 1 << 24,
        CharPropertyMask                       = 1 << 25,
        DontValidateUTF8                       = 1 << 26,
        FixedBuffer                            = 1 << 27,
        DisallowSerializedPropertyModification = 1 << 28,
    }
}
