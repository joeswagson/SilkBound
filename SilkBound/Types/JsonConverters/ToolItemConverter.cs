using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.JsonConverters
{
    public class ToolItemConverter : JsonConverter<ToolItem>
    {
        public override ToolItem? ReadJson(JsonReader reader, Type objectType, ToolItem? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            string toolName = reader.ReadAsString() ?? "idfk";
            return ToolItemManager.GetToolByName(toolName);
        }

        public override void WriteJson(JsonWriter writer, ToolItem? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteValue(value.name);
        }
    }
}
