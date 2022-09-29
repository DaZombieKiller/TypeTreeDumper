using System;

namespace Unity
{
    public unsafe class JsonHandler
    {
        public bool IsSupported { get; } = true;
        private static string SerializeObjectSymbolName => $"?SerializeObject@JSONUtility@@SAXP{NameMangling.Ptr64}AVObject@@AEAV?$basic_string@DV?$StringStorageDefault@D@core@@@core@@_NW4TransferInstructionFlags@@@Z";
        private readonly delegate* unmanaged[Cdecl]<void*, void*, byte, TransferInstructionFlags, void> s_SerializeObject;

        public JsonHandler(UnityVersion version, SymbolResolver resolver)
        {
            if(resolver.TryResolve(SerializeObjectSymbolName, out var address))
            {
                s_SerializeObject = (delegate* unmanaged[Cdecl]<void*, void*, byte, TransferInstructionFlags, void>)address;
            }
            else
            {
                IsSupported = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="object"></param>
        /// <param name="flags">Flags for the method call. They don't seem to matter.</param>
        /// <param name="prettyPrint"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public unsafe string SerializeObjectAsJson(NativeObject @object, TransferInstructionFlags flags = TransferInstructionFlags.None, bool prettyPrint = true)
        {
            if (s_SerializeObject != null)
            {
                BasicString basicString = BasicString.CreateExternal();
                s_SerializeObject(@object.Pointer, &basicString, prettyPrint ? (byte)1 : (byte)0, flags);
                return basicString.GetString();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
