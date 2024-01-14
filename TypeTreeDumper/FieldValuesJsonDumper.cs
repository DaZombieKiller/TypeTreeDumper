using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity;

namespace TypeTreeDumper
{
    internal static class FieldValuesJsonDumper
    {
        public static void ExportFieldValuesJson(UnityEngine engine, string outputPath)
        {
            if (!engine.JsonHandler.IsSupported)
            {
                return;
            }

            Logger.Info("Writing field values...");
            JObject document = new();
            document.Add("Version", new JValue(engine.Version.ToString()));
            JArray array = new();
            document.Add("Classes", array);

            Dictionary<int, JObject> dictionary1 = GetJsonForAllObjects(engine).ToDictionary(pair => pair.Key, pair => ExtractClassData(pair.Key, pair.Value));
            
            //Some json content is random and must be handled.
            //It really does take this many times to get a consistent result.
            for (int i = 1; i <= 1000; i++)
            {
                if (i % 25 == 0)
                {
                    Logger.Info($"Revision {i}");
                }
                SetRandomValuesToDefault(engine, dictionary1);
            }

            foreach ((_, JObject object1) in dictionary1)
            {
                array.Add(object1);
            }

            File.WriteAllText(outputPath, document.PrettyPrint());
        }

        private static void SetRandomValuesToDefault(UnityEngine engine, Dictionary<int, JObject> dictionary1)
        {
            Dictionary<int, string> dictionary2 = GetJsonForAllObjects(engine);

            foreach ((int id, JObject object1) in dictionary1)
            {
                JObject object2 = ExtractClassData(id, dictionary2[id]);

                Combine(object1, object2);
            }
        }

        private static void Combine(JContainer container1, JContainer container2)
        {
            if (container1.GetType() != container2.GetType())
            {
                throw new ArgumentException("Inequal types.");
            }
            else if (container1.Count != container2.Count)
            {
                ((IList<JToken>)container1).Clear();
            }
            else
            {
                foreach ((JToken child1, JToken child2) in new ChildEnumerable(container1, container2))
                {
                    if (child1 is JValue value1)
                    {
                        value1.SetToDefaultIfNotEqual((JValue)child2);
                    }
                    else if (child1 is JProperty property1)
                    {
                        JProperty property2 = (JProperty)child2;
                        if (property1.Name != property2.Name)
                        {
                            throw new Exception("Property names did not match.");
                        }
                        else if (property1.Value is JContainer propertyValue1)
                        {
                            Combine(propertyValue1, (JContainer)property2.Value);
                        }
                        else
                        {
                            ((JValue)property1.Value).SetToDefaultIfNotEqual((JValue)property2.Value);
                        }
                    }
                    else
                    {
                        Combine((JContainer)child1, (JContainer)child2);
                    }
                }
            }
        }

        private static void SetToDefaultIfNotEqual(this JValue value1, JValue value2)
        {
            if (value1.Type == JTokenType.String && (string)value1.Value == "NaN")
            {
                value1.Value = 0f;
                return;
            }
            if (value1.Type == JTokenType.Float)
            {
                double decimalValue = value1.Value switch
                {
                    float f => f,
                    double d => d,
                    decimal d2 => (double)d2,
                    _ => default
                };
                if (!double.IsFinite(decimalValue) || Math.Abs(decimalValue) < 1E-5)
                {
                    value1.Value = 0f;
                    return;
                }
            }
            if (!value1.Equals(value2))
            {
                value1.SetToDefault();
            }
        }

        private static void SetToDefault(this JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.None:
                    throw new NotSupportedException();
                case JTokenType.Object:
                    throw new NotSupportedException();
                case JTokenType.Array:
                    throw new NotSupportedException();
                case JTokenType.Constructor:
                    throw new NotSupportedException();
                case JTokenType.Property:
                    throw new NotSupportedException();
                case JTokenType.Comment:
                    throw new NotSupportedException();
                case JTokenType.Integer:
                    value.Value = 0;
                    break;
                case JTokenType.Float:
                    value.Value = 0f;
                    break;
                case JTokenType.String:
                    value.Value = "";
                    break;
                case JTokenType.Boolean:
                    value.Value = false;
                    break;
                case JTokenType.Null:
                    break;
                case JTokenType.Undefined:
                    throw new NotSupportedException();
                case JTokenType.Date:
                    value.Value = DateTime.FromBinary(0);
                    break;
                case JTokenType.Raw:
                    throw new NotSupportedException();
                case JTokenType.Bytes:
                    throw new NotSupportedException();
                case JTokenType.Guid:
                    throw new NotSupportedException();
                case JTokenType.Uri:
                    throw new NotSupportedException();
                case JTokenType.TimeSpan:
                    throw new NotSupportedException();
            }
        }

        private readonly struct ChildEnumerable : IEnumerable<(JToken, JToken)>
        {
            private readonly JContainer object1;
            private readonly JContainer object2;

            public ChildEnumerable(JContainer object1, JContainer object2)
            {
                this.object1 = object1 ?? throw new ArgumentNullException(nameof(object1));
                this.object2 = object2 ?? throw new ArgumentNullException(nameof(object2));
            }

            public IEnumerator<(JToken, JToken)> GetEnumerator()
            {
                if (object1.Count != object2.Count)
                {
                    throw new Exception("Inequal counts");
                }

                IEnumerator<JToken> children1 = object1.Children().GetEnumerator();
                IEnumerator<JToken> children2 = object2.Children().GetEnumerator();
                
                while(children1.MoveNext() && children2.MoveNext())
                {
                    yield return (children1.Current, children2.Current);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static Dictionary<int, string> GetJsonForAllObjects(UnityEngine engine)
        {
            Dictionary<int, string> result = new();

            foreach (var type in engine.RuntimeTypes.ToArray().OrderBy(x => (int)x.PersistentTypeID))
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                NativeObject obj = engine.ObjectFactory.GetOrProduce(type);

                if (obj == null)
                {
                    //Logger.Info($"Skipping {type.Name} because {nameof(obj)} was null.");
                }
                else
                {
                    string editorJson = engine.JsonHandler.SerializeObjectAsJson(obj, TransferInstructionFlags.None);

                    if (string.IsNullOrEmpty(editorJson))
                    {
                        Logger.Info($"Skipping {type.Name} because json string was empty.");
                    }
                    else
                    {
                        result.Add((int)type.PersistentTypeID, editorJson);
                    }
                }
            }

            return result;
        }

        private static JObject ExtractClassData(int id, string jsonText)
        {
            try
            {
                ExtractFields(jsonText, out string jsonName, out JObject fields);

                JObject element = new()
                {
                    { "TypeID", new JValue(id) },
                    { "Name", new JValue(jsonName) },
                    { "Fields", fields }
                };

                return element;
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not dump field values for id: {id}", ex);
            }
        }

        private static void ExtractFields(string jsonText, out string jsonName, out JObject fields)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                throw new Exception("Json cannot be empty");
            }

            JObject original = JObject.Parse(jsonText);
            JProperty lastProperty = (JProperty)original.Last ?? throw new Exception("Json has no properties");
            jsonName = lastProperty.Name;
            fields = (JObject)lastProperty.Value.DeepClone();

            //Some types like TextureImporter and ModelImporter don't follow the normal json format and must be converted.
            //Their fields come before the type name instead of being nested underneath.
            if (original.Count > 1)
            {
                foreach (JToken token in original.Children().SkipLast(1))
                {
                    JProperty property = (JProperty)token;
                    fields.Add(property.Name, property.Value.DeepClone());
                }
            }
        }

        private static string PrettyPrint(this JToken token)
        {
            using StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            JsonTextWriter jw = new JsonTextWriter(sw);
            jw.Formatting = Formatting.Indented;
            jw.IndentChar = '\t';
            jw.Indentation = 1;
            token.WriteTo(jw);
            return sw.ToString();
        }
    }
}