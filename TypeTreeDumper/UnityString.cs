using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity;

namespace TypeTreeDumper
{
    internal class UnityString
    {
        public uint Index { get; set; }
        public string String { get; set; }

		internal static List<UnityString> MakeList(CommonString strings)
		{
			byte[] data = strings.GetData();
			var result = new List<UnityString>();

			using (var stream = new MemoryStream(data))
			{
				using (var reader = new BinaryReader(stream))
				{
					while (stream.Position < stream.Length)
					{
						uint position = (uint)stream.Position;
						string str = ReadStringToNull(reader);
						result.Add(new UnityString() { Index = position, String = str });
					}
				}
			}

			return result;
		}

		private static string ReadStringToNull(BinaryReader reader, int maxLength = 32767)
		{
			var bytes = new List<byte>();
			int count = 0;
			while (reader.BaseStream.Position != reader.BaseStream.Length && count < maxLength)
			{
				var b = reader.ReadByte();
				if (b == 0)
				{
					break;
				}
				bytes.Add(b);
				count++;
			}
			return Encoding.UTF8.GetString(bytes.ToArray());
		}
	}
}
