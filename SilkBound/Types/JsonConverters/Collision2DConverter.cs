using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilkBound.Extensions;
using SilkBound.Network;
using System;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Types.JsonConverters
{

    public class Collision2DConverter(bool local, Weaver? sender=null) : JsonConverter<Collision2D>
    {
        public override void WriteJson(JsonWriter writer, Collision2D? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            JObject obj = new JObject();

            obj["ColliderPath"] = ReplacePath(value.collider ? value.collider.gameObject.transform.GetPath() : null!);
            obj["OtherColliderPath"] = ReplacePath(value.otherCollider ? value.otherCollider.gameObject.transform.GetPath() : null!);
            obj["RigidbodyPath"] = ReplacePath(value.rigidbody ? value.rigidbody.gameObject.transform.GetPath() : null!);
            obj["OtherRigidbodyPath"] = ReplacePath(value.otherRigidbody ? value.otherRigidbody.gameObject.transform.GetPath() : null!);

            obj["RelativeVelocity"] = JToken.FromObject(value.relativeVelocity, serializer);
            obj["Enabled"] = value.enabled ? 1 : 0;
            obj["ContactCount"] = value.contactCount;

            if (value.contacts != null && value.contacts.Length > 0)
            {
                JArray contacts = new JArray();
                foreach (var contact in value.contacts)
                {
                    JObject c = new JObject
                    {
                        ["Point"] = JToken.FromObject(contact.point, serializer),
                        ["Normal"] = JToken.FromObject(contact.normal, serializer),
                        ["Separation"] = contact.separation,
                        ["NormalImpulse"] = contact.normalImpulse,
                        ["TangentImpulse"] = contact.tangentImpulse
                    };
                    contacts.Add(c);
                }
                obj["Contacts"] = contacts;
            }

            obj.WriteTo(writer);
        }

        string? ReplacePath(string? path)
        {
            if (local)
            {
                Logger.Msg(sender!);
                Logger.Msg(sender?.Mirror!);
                Logger.Msg(sender?.Mirror?.name!);
                return path?.Replace("Hero_Hornet(Clone)", sender!.Mirror!.name);
            }
            return path;
        }

        public override Collision2D? ReadJson(JsonReader reader, Type objectType, Collision2D? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);
            Collision2D instance = new Collision2D();

            string colliderPath = obj["ColliderPath"]?.ToString()!;
            string otherColliderPath = obj["OtherColliderPath"]?.ToString()!;
            string rigidbodyPath = obj["RigidbodyPath"]?.ToString()!;
            string otherRigidbodyPath = obj["OtherRigidbodyPath"]?.ToString()!;

            Logger.Msg("Local?: ", local, "| ColliderPath:", colliderPath);
            Logger.Msg("Local?: ", local, "| OtherColliderPath:", otherColliderPath);
            Logger.Msg("Local?: ", local, "| RigidbodyPath:", rigidbodyPath);
            Logger.Msg("Local?: ", local, "| OtherRigidbodyPath:", otherRigidbodyPath);

            Collider2D? collider = !string.IsNullOrEmpty(colliderPath)
                ? GameObject.Find(colliderPath)?.GetComponent<Collider2D>()
                : null;

            Collider2D? otherCollider = !string.IsNullOrEmpty(otherColliderPath)
                ? GameObject.Find(otherColliderPath)?.GetComponent<Collider2D>()
                : null;

            Rigidbody2D? rigidbody = !string.IsNullOrEmpty(rigidbodyPath)
                ? GameObject.Find(rigidbodyPath)?.GetComponent<Rigidbody2D>()
                : null;

            Rigidbody2D? otherRigidbody = !string.IsNullOrEmpty(otherRigidbodyPath)
                ? GameObject.Find(otherRigidbodyPath)?.GetComponent<Rigidbody2D>()
                : null;

            // Assign backing fields (you said you can set them)
            instance.m_Collider = collider ? collider.GetInstanceID() : 0;
            instance.m_OtherCollider = otherCollider ? otherCollider.GetInstanceID() : 0;
            instance.m_Rigidbody = rigidbody ? rigidbody.GetInstanceID() : 0;
            instance.m_OtherRigidbody = otherRigidbody ? otherRigidbody.GetInstanceID() : 0;

            instance.m_RelativeVelocity = obj["RelativeVelocity"]?.ToObject<Vector2>(serializer) ?? Vector2.zero;
            instance.m_Enabled = obj["Enabled"]?.ToObject<int>() ?? 0;
            instance.m_ContactCount = obj["ContactCount"]?.ToObject<int>() ?? 0;

            if (obj["Contacts"] is JArray contacts)
            {
                ContactPoint2D[] contactArray = new ContactPoint2D[contacts.Count];
                for (int i = 0; i < contacts.Count; i++)
                {
                    var c = contacts[i];
                    ContactPoint2D cp = new ContactPoint2D // cyberpunk!?!?!?
                    {
                        m_Point = c["Point"]?.ToObject<Vector2>(serializer) ?? Vector2.zero,
                        m_Normal = c["Normal"]?.ToObject<Vector2>(serializer) ?? Vector2.zero,
                        m_Separation = c["Separation"]?.ToObject<float>() ?? 0f,
                        m_NormalImpulse = c["NormalImpulse"]?.ToObject<float>() ?? 0f,
                        m_TangentImpulse = c["TangentImpulse"]?.ToObject<float>() ?? 0f
                    };
                    contactArray[i] = cp;
                }

                instance.m_ReusedContacts = contactArray;
                instance.m_LegacyContacts = contactArray;
            }

            return instance;
        }
    }

}
