using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TripleDetection.Services
{
    public static class SimpleJsonHelper
    {
        public static void Save(object obj, string filePath)
        {
            var json = Serialize(obj);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, json);
        }

        public static T Load<T>(string filePath) where T : new()
        {
            if (!File.Exists(filePath))
            {
                return new T();
            }

            try
            {
                var json = File.ReadAllText(filePath);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                    $"[SimpleJsonHelper.Load] file={filePath}\n json={json.Substring(0, Math.Min(200, json.Length))}\n");
                var result = Deserialize<T>(json);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                    $"[SimpleJsonHelper.Load] deserialized successfully\n");
                return result;
            }
            catch (System.Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                    $"[SimpleJsonHelper.Load] ERROR: {ex.Message}\n{ex.StackTrace}\n");
                return new T();
            }
        }

        public static string Serialize(object obj)
        {
            if (obj == null) return "{}";

            var type = obj.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var parts = new List<string>();
            foreach (var prop in props)
            {
                var value = prop.GetValue(obj);
                var serializedValue = SerializeValue(value, prop.PropertyType);
                parts.Add($"\"{ToCamelCase(prop.Name)}\": {serializedValue}");
            }

            return "{" + string.Join(", ", parts) + "}";
        }

        private static string SerializeValue(object value, Type type)
        {
            if (value == null) return "null";

            if (type == typeof(string))
            {
                return $"\"{EscapeString(value.ToString())}\"";
            }

            if (type == typeof(int) || type == typeof(int?))
            {
                return value.ToString();
            }

            if (type == typeof(bool) || type == typeof(bool?))
            {
                return value.ToString().ToLower();
            }

            if (type == typeof(double) || type == typeof(double?))
            {
                return value.ToString();
            }

            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                return $"\"{((DateTime)value):yyyy-MM-ddTHH:mm:ss}\"";
            }

            if (type.IsArray)
            {
                var array = value as object[];
                if (array != null)
                {
                    var items = new List<string>();
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i] is Dictionary<string, object> dict)
                        {
                            items.Add(Serialize(DeserializeFromDict(type.GetElementType(), dict)));
                        }
                        else
                        {
                            items.Add(SerializeValue(array[i], array[i]?.GetType() ?? typeof(object)));
                        }
                    }
                    return "[" + string.Join(", ", items) + "]";
                }
            }

            if (type.IsClass)
            {
                return Serialize(value);
            }

            return $"\"{value}\"";
        }

        public static T Deserialize<T>(string json) where T : new()
        {
            if (string.IsNullOrWhiteSpace(json)) return new T();

            var obj = new Dictionary<string, object>();
            ParseJson(json, obj);

            var result = new T();
            var type = typeof(T);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var key = ToCamelCase(prop.Name);
                if (obj.ContainsKey(key))
                {
                    var value = ParseValue(obj[key], prop.PropertyType);
                    if (value != null)
                    {
                        prop.SetValue(result, value);
                    }
                }
            }

            return result;
        }

        private static object ParseValue(object value, Type targetType)
        {
            if (value == null) return null;

            if (targetType == typeof(string))
            {
                return value.ToString();
            }

            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (value is int) return value;
                if (value is double) return (int)(double)value;
                if (int.TryParse(value.ToString(), out int intResult)) return intResult;
                return null;
            }

            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                if (value is bool) return value;
                if (bool.TryParse(value.ToString(), out bool boolResult)) return boolResult;
                return null;
            }

            if (targetType == typeof(double) || targetType == typeof(double?))
            {
                if (value is double) return value;
                if (double.TryParse(value.ToString(), out double doubleResult)) return doubleResult;
                return null;
            }

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                if (DateTime.TryParse(value.ToString(), out DateTime dateResult)) return dateResult;
                return null;
            }

            if (targetType.IsArray)
            {
                if (value is object[] arr)
                {
                    var elementType = targetType.GetElementType();
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                        $"[ParseValue] array case: elementType={elementType}, arr.Length={arr.Length}\n");
                    var result = Array.CreateInstance(elementType, arr.Length);
                    for (int i = 0; i < arr.Length; i++)
                    {
                        System.IO.File.AppendAllText(
                            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                            $"[ParseValue] arr[{i}] = {arr[i]?.GetType().Name ?? "null"}\n");
                        if (arr[i] is Dictionary<string, object> itemDict)
                        {
                            result.SetValue(DeserializeFromDict(elementType, itemDict), i);
                        }
                        else if (arr[i] != null)
                        {
                            result.SetValue(Convert.ChangeType(arr[i], elementType), i);
                        }
                    }
                    return result;
                }
                return null;
            }

            if (targetType.IsClass && value is Dictionary<string, object> dict)
            {
                return DeserializeFromDict(targetType, dict);
            }

            return null;
        }

        private static object DeserializeFromDict(Type type, Dictionary<string, object> dict)
        {
            var result = Activator.CreateInstance(type);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var key = ToCamelCase(prop.Name);
                if (dict.ContainsKey(key))
                {
                    var value = ParseValue(dict[key], prop.PropertyType);
                    if (value != null)
                    {
                        prop.SetValue(result, value);
                    }
                }
            }

            return result;
        }

        private static void ParseJson(string json, Dictionary<string, object> result)
        {
            json = json.Trim();
            if (json.StartsWith("{") && json.EndsWith("}"))
            {
                json = json.Substring(1, json.Length - 2);
                ParseObject(json, result);
            }
            else if (json.StartsWith("[") && json.EndsWith("]"))
            {
                json = json.Substring(1, json.Length - 2);
            }
        }

        private static void ParseObject(string json, Dictionary<string, object> result)
        {
            int depth = 0;
            int start = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (c == '{' || c == '[') depth++;
                    if (c == '}' || c == ']') depth--;

                    if (c == ',' && depth == 0)
                    {
                        ParsePair(json.Substring(start, i - start), result);
                        start = i + 1;
                    }
                }
            }

            if (start < json.Length)
            {
                ParsePair(json.Substring(start), result);
            }
        }

        private static void ParsePair(string pair, Dictionary<string, object> result)
        {
            int colonIndex = -1;
            bool inString = false;

            for (int i = 0; i < pair.Length; i++)
            {
                if (pair[i] == '"' && (i == 0 || pair[i - 1] != '\\'))
                {
                    inString = !inString;
                }

                if (pair[i] == ':' && !inString)
                {
                    colonIndex = i;
                    break;
                }
            }

            if (colonIndex < 0) return;

            var key = pair.Substring(0, colonIndex).Trim();
            key = key.Trim('"');
            var valueStr = pair.Substring(colonIndex + 1).Trim();

            result[key] = ParseToken(valueStr);
        }

        private static object ParseToken(string token)
        {
            token = token.Trim();

            if (token.StartsWith("\"") && token.EndsWith("\""))
            {
                return token.Substring(1, token.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");
            }

            if (token == "null") return null;
            if (token == "true") return true;
            if (token == "false") return false;

            if (token.StartsWith("{") && token.EndsWith("}"))
            {
                var dict = new Dictionary<string, object>();
                ParseObject(token.Substring(1, token.Length - 2), dict);
                return dict;
            }

            if (token.StartsWith("[") && token.EndsWith("]"))
            {
                var list = new List<object>();
                var inner = token.Substring(1, token.Length - 2).Trim();
                if (!string.IsNullOrEmpty(inner))
                {
                    int depth = 0;
                    int start = 0;
                    bool inString = false;

                    for (int i = 0; i < inner.Length; i++)
                    {
                        char c = inner[i];
                        if (c == '"' && (i == 0 || inner[i - 1] != '\\'))
                        {
                            inString = !inString;
                        }

                        if (!inString)
                        {
                            if (c == '{' || c == '[') depth++;
                            if (c == '}' || c == ']') depth--;

                            if (c == ',' && depth == 0)
                            {
                                list.Add(ParseToken(inner.Substring(start, i - start).Trim()));
                                start = i + 1;
                            }
                        }
                    }

                    if (start < inner.Length)
                    {
                        list.Add(ParseToken(inner.Substring(start).Trim()));
                    }
                }
                return list.ToArray();
            }

            if (int.TryParse(token, out int intVal)) return intVal;
            if (double.TryParse(token, out double doubleVal)) return doubleVal;

            return token;
        }

        private static string EscapeString(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToLower(s[0]) + s.Substring(1);
        }
    }
}