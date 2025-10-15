using Newtonsoft.Json;
using SilkBound.Behaviours;
using SilkBound.Extensions;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilkBound.Types.JsonConverters
{
    public class GameObjectConverter(bool replaceController = true, bool createNew = false, string? newName = null, Dictionary<string, JsonConverter>? extras = null)
        : JsonConverter<GameObject>
    {
        public override GameObject? ReadJson(JsonReader reader, Type objectType, GameObject? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            reader.FloatParseHandling = FloatParseHandling.Decimal;

            bool createNew = (reader.ReadAsBoolean() ?? false);
            string name = reader.ReadAsString() ?? "SBUnnamedGO";

            float posX = (float)(reader.ReadAsDecimal() ?? 0M);
            float posY = (float)(reader.ReadAsDecimal() ?? 0M);
            float posZ = (float)(reader.ReadAsDecimal() ?? 0M);
            Vector3 position = new Vector3(posX, posY, posZ);

            float rotX = (float)(reader.ReadAsDecimal() ?? 0M);
            float rotY = (float)(reader.ReadAsDecimal() ?? 0M);
            float rotZ = (float)(reader.ReadAsDecimal() ?? 0M);
            float rotW = (float)(reader.ReadAsDecimal() ?? 1M);
            Quaternion rotation = new Quaternion(rotX, rotY, rotZ, rotW);

            var obj = createNew ? new GameObject(name) : UnityObjectExtensions.FindObjectFromFullName(name);
            if (obj)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }

            if (extras != null)
            {
                if (obj && extras.TryGetValue(obj.name, out JsonConverter extra))
                    extra.ReadJson(reader, objectType, obj, serializer);
                else if (extras.TryGetValue(name, out JsonConverter extraOther))
                    extraOther.ReadJson(reader, objectType, obj, serializer);
            }

            // consume EndArray if present
            if (reader.TokenType != JsonToken.EndArray)
                reader.Read();

            return obj;
        }

        public override void WriteJson(JsonWriter writer, GameObject? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();

            writer.WriteValue(createNew);

            string name = createNew ? (newName ?? value.name) : value.transform.GetPath();

            if (replaceController)
                name = name.Replace("Hero_Hornet(Clone)", HornetMirror.GetObjectName(NetworkUtils.ClientID));

            writer.WriteValue(name);

            writer.WriteValue((decimal)value.transform.position.x); // if ur sending hornet to fucking 8e28 you deserve to have casting issues
            writer.WriteValue((decimal)value.transform.position.y);
            writer.WriteValue((decimal)value.transform.position.z);

            writer.WriteValue((decimal)value.transform.rotation.x);
            writer.WriteValue((decimal)value.transform.rotation.y);
            writer.WriteValue((decimal)value.transform.rotation.z);
            writer.WriteValue((decimal)value.transform.rotation.w);

            if (extras?.TryGetValue(value.name, out JsonConverter extra) ?? false)
                extra.WriteJson(writer, value, serializer);

            writer.WriteEndArray();
        }
    }
}
