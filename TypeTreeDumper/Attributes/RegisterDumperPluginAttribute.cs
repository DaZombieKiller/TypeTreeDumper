using System;
using TypeTreeDumper.Interfaces;

namespace TypeTreeDumper.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class RegisterDumperPluginAttribute : Attribute
    {
        public IDumperPlugin Plugin { get; }

        public RegisterDumperPluginAttribute(IDumperPlugin plugin) => Plugin = plugin;
    }
}
