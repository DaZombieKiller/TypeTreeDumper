using System;

namespace TypeTreeDumper
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class RegisterDumperPluginAttribute : Attribute
    {
        public Type PluginType { get; }

        public RegisterDumperPluginAttribute(Type type) => PluginType = type;
    }
}
