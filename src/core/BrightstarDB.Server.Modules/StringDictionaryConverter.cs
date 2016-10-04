using System;
using System.Collections.Generic;
using Nancy.Json;

namespace BrightstarDB.Server.Modules
{
    public class StringDictionaryConverter : JavaScriptConverter
    {
        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            if (type == typeof(Dictionary<string, string>))
            {
                var ret = new Dictionary<string, string>();
                foreach (var entry in dictionary)
                {
                    if (entry.Value is string)
                    {
                        ret[entry.Key] = entry.Value as string;
                    }
                }
                return ret;
            }
            return null;
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            var dict = obj as Dictionary<string, string>;
            if (dict != null)
            {
                var json = new Dictionary<string, object>();
                foreach (var entry in dict)
                {
                    json[entry.Key] = entry.Value;
                }
                return json;
            }
            return null;
        }

        public override IEnumerable<Type> SupportedTypes { get { yield return typeof(Dictionary<string, string>); } }
    }

}
