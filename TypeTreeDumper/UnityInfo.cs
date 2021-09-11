using System.Collections.Generic;
using Unity;

namespace TypeTreeDumper
{
    internal class UnityInfo
    {
        public string Version { get; set; }
        public List<UnityString> Strings { get; set; }
        public List<UnityClass> Classes { get; set; }

        public static UnityInfo Create(UnityEngine engine)
        {
            var result = new UnityInfo();
            result.Version = engine.Version.ToString();
            result.Strings = UnityString.MakeList(engine.CommonString);
            result.Classes = UnityClass.MakeList(engine);
            return result;
        }
    }
}
