using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Unity
{
    unsafe struct BasicString
    {
        private readonly static int HeapStructSize = sizeof(HeapAllocatedRepresentation);

        public StringRepresentationUnion m_data_union;
        public StringRepresentation m_data_repr;
        public MemLabelId m_label;

        public static explicit operator string(BasicString basicString) => basicString.GetString();

        /// <summary>
        /// Get an equivalent managed string
        /// </summary>
        /// <param name="basicString"></param>
        /// <returns>A new string made from the underlying Utf8 data</returns>
        public string GetString() => Encoding.UTF8.GetString(GetData());

        /// <summary>
        /// Get the Utf8 bytes for this string
        /// </summary>
        /// <returns>A span containing the data</returns>
        public ReadOnlySpan<byte> GetData() => GetDataInternal(this);
        private static ReadOnlySpan<byte> GetDataInternal(BasicString basicString)
        {
            return basicString.m_data_repr == StringRepresentation.Embedded
                ? new ReadOnlySpan<byte>(&basicString, HeapStructSize - basicString.m_data_union.m_embedded.m_extra)
                : new ReadOnlySpan<byte>(basicString.m_data_union.m_heap.m_data, (int)(uint)basicString.m_data_union.m_heap.m_size);
        }

        /// <summary>
        /// Create a simple basic string
        /// </summary>
        /// <returns></returns>
        public static BasicString CreateExternal()
        {
            return new BasicString
            {
                m_data_repr = StringRepresentation.External
            };
        }

        /// <summary>
        /// Create an external basic string
        /// <code>
        /// var data   = Marshal.StringToCoTaskMemUTF8(str);
        /// var length = (uint)new Span&lt;byte&gt;((void*)data, int.MaxValue).IndexOf((byte)0);
        /// 
        /// try
        /// {
        ///     var basicString = BasicString.CreateExternal((byte*)data, length);
        ///     //Do interop
        /// }
        /// finally
        /// {
        ///     Marshal.FreeCoTaskMem(data);
        ///	}
        /// </code>
        /// </summary>
        /// <param name="address">A pointer to some utf8 data. Can be null if the length is zero.</param>
        /// <param name="length">The length of the utf8 data.</param>
        /// <returns>A new basic string.</returns>
        public static BasicString CreateExternal(byte* address, nuint length)
        {
            return new BasicString
            {
                m_data_repr = StringRepresentation.External,
                m_data_union = new StringRepresentationUnion
                {
                    m_heap = new HeapAllocatedRepresentation
                    {
                        m_data = address,
                        m_capacity = length,
                        m_size = length
                    }
                }
            };
        }

        /// <summary>
        /// Create an embedded basic string from a C# managed string.
        /// </summary>
        /// <remarks>
        /// If the string provided is larger than the space available, it will be truncated.
        /// </remarks>
        /// <param name="str">The string to be copied.</param>
        /// <returns>A new basic string containing the string.</returns>
        public static BasicString CreateEmbedded(string str) => CreateEmbedded(Encoding.UTF8.GetBytes(str));

        /// <summary>
        /// Create an embedded basic string from some utf8 data.
        /// </summary>
        /// <remarks>
        /// If the data provided is larger than the space available, it will be truncated.
        /// </remarks>
        /// <param name="utf8String">The data to be copied.</param>
        /// <returns>A new basic string containing the data.</returns>
        public static BasicString CreateEmbedded(ReadOnlySpan<byte> utf8String)
        {
            var basicString = new BasicString
            {
                m_data_repr = StringRepresentation.Embedded,
            };

            int size = Math.Min(HeapStructSize, utf8String.Length);
            Span<byte> embeddedData = new Span<byte>(&basicString, HeapStructSize);
            for(int i = 0; i < size; i++)
            {
                embeddedData[i] = utf8String[i];
            }
            basicString.m_data_union.m_embedded.m_extra = (byte)(HeapStructSize - size);

            return basicString;
        }
    }

    unsafe struct HeapAllocatedRepresentation
    {
        public byte* m_data;
        public nuint m_capacity;
        public nuint m_size;
    }

    unsafe struct StackAllocatedRepresentation
    {
        // StackAllocatedRepresentation is sizeof(HeapAllocatedRepresentation) + sizeof(TChar)
        public HeapAllocatedRepresentation m_heap;
        public byte m_extra;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct StringRepresentationUnion
    {
        [FieldOffset(0)]
        public StackAllocatedRepresentation m_embedded;

        [FieldOffset(0)]
        public HeapAllocatedRepresentation m_heap;
    }

    enum StringRepresentation : byte
    {
        /// <summary>
        /// The data is stored by Unity.
        /// </summary>
        Heap,
        /// <summary>
        /// The data is stored within the structure.
        /// </summary>
        Embedded,
        /// <summary>
        /// The data is stored outside of Unity.
        /// </summary>
        External
    };
}