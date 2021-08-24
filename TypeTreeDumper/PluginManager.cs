using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
                if(TryLoadAssembly(file, out Assembly assembly) && TryGetCustomAttributes(assembly, out IEnumerable<RegisterDumperPluginAttribute> attributes))
                {
                    foreach (var attribute in attributes)
                    {
                        Type pluginType = attribute.PluginType;
                        if (IsValidPluginType(pluginType))
                        {
                            try
                            {
                                Plugins.Add((IDumperPlugin)Activator.CreateInstance(pluginType));
                                Console.WriteLine($"'{pluginType.Name}' loaded");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error while instantiating {pluginType?.Name ?? "a plugin"}");
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }
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

        private static bool TryLoadAssembly(string path, out Assembly assembly)
        {
            try
            {
                assembly = Assembly.LoadFile(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while loading the assembly at {path}");
                Console.WriteLine(ex.ToString());
                assembly = null;
                return false;
            }
        }

        private static bool TryGetCustomAttributes<T>(Assembly assembly, out IEnumerable<T> attributes) where T : Attribute
        {
            try
            {
                attributes = assembly.GetCustomAttributes<T>();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error while getting the attributes for {assembly?.FullName ?? "an assembly"}");
                Console.WriteLine(ex.ToString());
                attributes = null;
                return false;
            }
        }

        private static bool IsValidPluginType(Type type)
        {
            return type != null
                && typeof(IDumperPlugin).IsAssignableFrom(type)
                && type.GetConstructors().Where(constructorInfo => constructorInfo.GetParameters().Length == 0).Any();
        }
    }
}
