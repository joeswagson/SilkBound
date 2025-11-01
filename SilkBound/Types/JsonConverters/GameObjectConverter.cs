using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilkBound.Behaviours;
using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilkBound.Types.JsonConverters
{
    public class GameObjectConverter(bool replaceController = true, bool createNew = false, string? newName = null, Dictionary<string, JsonConverter>? extras = null) : JsonConverter<GameObject>
    {

        public override GameObject? ReadJson(JsonReader reader, Type objectType, GameObject? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var array = JArray.Load(reader);

            bool createNew = array[0].ToObject<bool>(serializer);
            string name = array[1].ToObject<string>(serializer) ?? "SBUnnamedGO";

            Vector3 position = new(
                array[2].ToObject<float>(serializer),
                array[3].ToObject<float>(serializer),
                array[4].ToObject<float>(serializer)
            );

            Quaternion rotation = new(
                array[5].ToObject<float>(serializer),
                array[6].ToObject<float>(serializer),
                array[7].ToObject<float>(serializer),
                array[8].ToObject<float>(serializer)
            );

            GameObject obj = createNew
                ? new GameObject(name)
                : ObjectManager.Get(name)?.GameObject ?? new GameObject(name);

            obj.transform.position = position;
            obj.transform.rotation = rotation;

            if (extras != null && obj != null)
            {
                foreach (var kvp in extras)
                {
                    kvp.Value.ReadJson(array.CreateReader(), objectType, obj, serializer);
                }
            }

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

            // write createNew
            writer.WriteValue(createNew);

            // determine name
            string name = createNew ? (newName ?? value.name) : value.transform.GetPath();
            if (replaceController)
                name = name.Replace("Hero_Hornet(Clone)", HornetMirror.GetObjectName(NetworkUtils.ClientID));

            writer.WriteValue(name);

            // write position
            writer.WriteValue((decimal)value.transform.position.x); // if ur sending hornet to fucking 8e28 you deserve to have casting issues
            writer.WriteValue((decimal)value.transform.position.y);
            writer.WriteValue((decimal)value.transform.position.z);

            // write rotation
            writer.WriteValue((decimal)value.transform.rotation.x);
            writer.WriteValue((decimal)value.transform.rotation.y);
            writer.WriteValue((decimal)value.transform.rotation.z);
            writer.WriteValue((decimal)value.transform.rotation.w);

            // write extras if any
            if (extras != null && extras.TryGetValue(value.name, out JsonConverter extra))
            {
                extra.WriteJson(writer, value, serializer);
            }

            writer.WriteEndArray();
        }
    }
}
