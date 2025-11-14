using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SilkBound.Types.Data {
    public class OptionalValueSet<TEnum> where TEnum : Enum {
        private readonly object?[] _values; // backing storage
        private readonly TEnum[] _allFlags;
        private readonly Dictionary<TEnum, TypeCode>? _schema;
        public OptionalValueSet(Dictionary<TEnum, TypeCode>? schema=null)
        {
            _allFlags = (TEnum[]) Enum.GetValues(typeof(TEnum));
            _values = new object?[_allFlags.Length];
            _schema = schema;
        }

        /// <summary>
        /// Set a value for a specific flag.
        /// </summary>
        public void Set<T>(TEnum flag, T? value)
        {
            if (value == null || (value is IOptionalValue opt && !opt.HasValue))
                return;

            _values[Convert.ToByte(flag)] = value;
        }

        /// <summary>
        /// Get a value for a specific flag, or null if unset.
        /// </summary>
        public T? Get<T>(TEnum flag) where T : struct
        {
            var obj = _values[Convert.ToByte(flag)];
            if (obj is T val) return val;
            if (TryCast(obj, out T nullable)) return nullable;
            //Logger.Msg("was not of T:", flag.ToString(), obj?.GetType().Name ?? "no obj", typeof(T).Name);
            if (obj is null) return null;
            throw new InvalidCastException($"Flag {flag} is stored as {obj.GetType()}, not {typeof(T)}");
        }

        public static bool TryCast<T>(object? obj, out T value)
        {
            value = default!;

            if (obj == null)
            {
                if (!typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null)
                    return true;

                return false;
            }

            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (targetType.IsInstanceOfType(obj))
            {
                value = (T) obj;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Serialize all set values. Primitive types are written automatically, others use customFunc.
        /// </summary>
        public void Write(BinaryWriter writer, Func<TEnum, object, BinaryWriter, bool>? customFunc = null)
        {
            byte flags = 0;
            for (int i = 0; i < _allFlags.Length; i++)
                if (_values[i] != null) flags |= (byte) (1 << i);

            writer.Write(flags);
            //Logger.Msg(" te", Convert.ToString(flags, 2).PadLeft(8, '0'));

            for (int i = 0; i < _allFlags.Length; i++)
            {
                var value = _values[i];
                if (value == null) continue;
                if (value is IOptionalValue opt && !opt.HasValue) continue;

                bool written = true;
                switch (value)
                {
                    case byte b: writer.Write(b); break;
                    case sbyte sb: writer.Write(sb); break;
                    case short s: writer.Write(s); break;
                    case ushort us: writer.Write(us); break;
                    case int i32: writer.Write(i32); break;
                    case uint u32: writer.Write(u32); break;
                    case long l: writer.Write(l); break;
                    case ulong ul: writer.Write(ul); break;
                    case float f: writer.Write(f); break;
                    case double d: writer.Write(d); break;
                    case bool bo: writer.Write(bo); break;
                    case char c: writer.Write(c); break;
                    case string str: writer.Write(str); break;
                    default: written = false; break;
                }

                if (!written)
                {
                    if (customFunc != null && customFunc(_allFlags[i], value, writer))
                        continue;

                    throw new InvalidOperationException($"No serializer for value type {value.GetType()} on flag {_allFlags[i]}");
                }
            }
        }

        /// <summary>
        /// Deserialize all set values. Primitive types are read automatically, others use customFunc.
        /// </summary>
        public void Read(BinaryReader reader, Func<TEnum, BinaryReader, object>? customFunc = null)
        {
            byte flags = reader.ReadByte();
            if (_schema == null)
                throw new NullReferenceException("Deserialization schema required.");

            for (int i = 0; i < _allFlags.Length; i++)
            {
                if ((flags & (1 << i)) == 0)
                {
                    continue;
                }

                TEnum flag = _allFlags[i];
                object value;

                if (!_schema.ContainsKey(flag))
                    throw new KeyNotFoundException($"Schema did not define a TypeCode for flag {flag}");

                switch (_schema[flag])
                {
                    case TypeCode.Byte: value = reader.ReadByte(); break;
                    case TypeCode.SByte: value = reader.ReadSByte(); break;
                    case TypeCode.Int16: value = reader.ReadInt16(); break;
                    case TypeCode.UInt16: value = reader.ReadUInt16(); break;
                    case TypeCode.Int32: value = reader.ReadInt32(); break;
                    case TypeCode.UInt32: value = reader.ReadUInt32(); break;
                    case TypeCode.Int64: value = reader.ReadInt64(); break;
                    case TypeCode.UInt64: value = reader.ReadUInt64(); break;
                    case TypeCode.Single: value = reader.ReadSingle(); break;
                    case TypeCode.Double: value = reader.ReadDouble(); break;
                    case TypeCode.Boolean: value = reader.ReadBoolean(); break;
                    case TypeCode.Char: value = reader.ReadChar(); break;
                    case TypeCode.String: value = reader.ReadString(); break;
                    default:
                        if (customFunc != null)
                            value = customFunc(flag, reader);
                        else
                            throw new InvalidOperationException($"No deserializer for {_schema[flag]} ({flag})");
                        break;
                }

                _values[i] = value;
            }
        }
    }
}
