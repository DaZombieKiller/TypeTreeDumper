using System;

namespace TypeTreeDumper
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class RegisterDumperPluginAttribute : Attribute
    {
        public Type PluginType { get; }

        public RegisterDumperPluginAttribute(Type type) => PluginType = type;
    }
}
