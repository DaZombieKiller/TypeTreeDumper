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

    public static class TransferMetaFlagsExtensions
    {
        public static bool IsHideInEditor(this TransferMetaFlags _this) => (_this & TransferMetaFlags.HideInEditor) != 0;
        public static bool IsNotEditable(this TransferMetaFlags _this) => (_this & TransferMetaFlags.NotEditable) != 0;
        public static bool IsStrongPPtr(this TransferMetaFlags _this) => (_this & TransferMetaFlags.StrongPPtr) != 0;
        public static bool IsTreatIntegerValueAsBoolean(this TransferMetaFlags _this) => (_this & TransferMetaFlags.TreatIntegerValueAsBoolean) != 0;
        public static bool IsSimpleEditor(this TransferMetaFlags _this) => (_this & TransferMetaFlags.SimpleEditor) != 0;
        public static bool IsDebugProperty(this TransferMetaFlags _this) => (_this & TransferMetaFlags.DebugProperty) != 0;
        public static bool IsAlignBytes(this TransferMetaFlags _this) => (_this & TransferMetaFlags.AlignBytes) != 0;
        public static bool IsAnyChildUsesAlignBytesFlag(this TransferMetaFlags _this) => (_this & TransferMetaFlags.AnyChildUsesAlignBytesFlag) != 0;
        public static bool IsIgnoreWithInspectorUndo(this TransferMetaFlags _this) => (_this & TransferMetaFlags.IgnoreWithInspectorUndo) != 0;
        public static bool IsEditorDisplaysCharacterMap(this TransferMetaFlags _this) => (_this & TransferMetaFlags.EditorDisplaysCharacterMap) != 0;
        public static bool IsIgnoreInMetaFiles(this TransferMetaFlags _this) => (_this & TransferMetaFlags.IgnoreInMetaFiles) != 0;
        public static bool IsTransferAsArrayEntryNameInMetaFiles(this TransferMetaFlags _this) => (_this & TransferMetaFlags.TransferAsArrayEntryNameInMetaFiles) != 0;
        public static bool IsTransferUsingFlowMappingStyle(this TransferMetaFlags _this) => (_this & TransferMetaFlags.TransferUsingFlowMappingStyle) != 0;
        public static bool IsGenerateBitwiseDifferences(this TransferMetaFlags _this) => (_this & TransferMetaFlags.GenerateBitwiseDifferences) != 0;
        public static bool IsDontAnimate(this TransferMetaFlags _this) => (_this & TransferMetaFlags.DontAnimate) != 0;
        public static bool IsTransferHex64(this TransferMetaFlags _this) => (_this & TransferMetaFlags.TransferHex64) != 0;
        public static bool IsCharPropertyMask(this TransferMetaFlags _this) => (_this & TransferMetaFlags.CharPropertyMask) != 0;
        public static bool IsDontValidateUTF8(this TransferMetaFlags _this) => (_this & TransferMetaFlags.DontValidateUTF8) != 0;
        public static bool IsFixedBuffer(this TransferMetaFlags _this) => (_this & TransferMetaFlags.FixedBuffer) != 0;
        public static bool IsDisallowSerializedPropertyModification(this TransferMetaFlags _this) => (_this & TransferMetaFlags.DisallowSerializedPropertyModification) != 0;
    }
}
