using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TextJsonDemo
{
    class Program
    {
        static void Main(string demo)
        {

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.AutoFlush = true;
            switch (demo)
            {
                case "serialize":
                    SerializePrettyPrint();
                    break;
                case "jdocument":
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
        private static void SerializePrettyPrint()
        {
            var payload = new DataPayload()
            {
                Id = 1,
                Type = Type.Root,
                Descendants = new DataPayload[]{
                    new DataPayload(){Id = 2, Type = Type.Standard}
                }
            };
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
            string payloadAsText = JsonSerializer.Serialize(payload, options);
            Trace.TraceInformation(payloadAsText);
            var payload2 = JsonSerializer.Deserialize<DataPayload>(payloadAsText, options);
            Debug.Assert(payload2.Id == payload.Id, "same id");
            Debug.Assert(payload2.Type == Type.Root, "same id");
        }
    }

    class DataPayload
    {
        public int Id { get; set; }
        [JsonPropertyName("nodeType")]
        public Type Type { get; set; }
        public DataPayload[] Descendants { get; set; }
        public string Title { get; set; }
        [JsonIgnore]
        public DataPayload Parent { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

    }

    enum Type
    {
        Root = 0b_0001,
        Standard = 0b_0010,

    }
}
