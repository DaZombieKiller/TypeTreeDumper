using System;

namespace Unity
{
    public static class NameMangling
    {
        public static string Ptr64
        {
            get => Environment.Is64BitProcess ? "E" : "";
        }
    }
}
