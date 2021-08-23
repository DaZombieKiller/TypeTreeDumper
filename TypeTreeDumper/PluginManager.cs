using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TypeTreeDumper.Interfaces;

namespace TypeTreeDumper
{
    public static class PluginManager
    {
        public static string PluginDirectory { get; }
        internal static List<IDumperPlugin> Plugins { get; } = new List<IDumperPlugin>();

        static PluginManager()
        {
            PluginDirectory = Path.Combine(System.AppContext.BaseDirectory, "Plugins");
            Directory.CreateDirectory(PluginDirectory);
        }

        internal static void LoadPlugins()
        {
            Console.WriteLine("Loading plugins");
            foreach(string file in Directory.GetFiles(PluginDirectory, "*.dll"))
            {
                Assembly assembly = Assembly.LoadFile(file);
                CustomAttributeData data = assembly.CustomAttributes.Where(attr => attr.AttributeType == typeof(RegisterDumperPluginAttribute)).FirstOrDefault();
                if(data != null)
                {
                    Type pluginType = (Type)data.ConstructorArguments[0].Value;
                    Plugins.Add((IDumperPlugin)Activator.CreateInstance(pluginType));
                    Console.WriteLine($"'{pluginType.Name}' loaded");
                }
            }
            Console.WriteLine($"{Plugins.Count} plugins loaded");
        }

        internal static void InitializePlugins(IDumperEngine dumperEngine)
        {
            foreach(var plugin in Plugins)
            {
                plugin?.Initialize(dumperEngine);
            }
        }
    }
}
