using SilkBound.Extensions;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Managers
{
    public class SerializedGameObjectManager
    {
        public SerializedGameObjectManager(Guid id, GameObject obj, string? path=null)
        {
            Id = id;
            Path = path ?? obj.transform.GetPath();
            Instance = obj;
        }
        public SerializedGameObjectManager(GameObject obj)
        {
            Id = Guid.NewGuid();
            Path = obj.transform.GetPath(); //we js gon hope ts works
            Instance = obj;
        }

        public Guid Id;
        public string Path;
        public GameObject? Instance;
        public bool HasInstance => Instance != null;

        public byte[] Serialize()
        {
            byte[] guid = Id.ToByteArray();
            byte[] path = Encoding.UTF8.GetBytes(Path);
            byte[] buffer = new byte[guid.Length + path.Length];

            if(!Assertions.EnsureLength(guid, 16)) { throw new Exception("Guid length is not 16 bytes???????? wtf????"); }

            Buffer.BlockCopy(guid, 0, buffer, 0, guid.Length);
            Buffer.BlockCopy(path, 0, buffer, guid.Length, path.Length);
            return buffer;
        }
        public SerializedGameObjectManager Deserialize(byte[] data)
        {
            byte[] guid = new byte[16];
            byte[] path = new byte[data.Length - 16];
            Buffer.BlockCopy(data, 0, guid, 0, 16);
            Buffer.BlockCopy(data, 16, path, 0, path.Length);
            Id = new Guid(guid);
            Path = Encoding.UTF8.GetString(path);
            Instance = ObjectManager.Get(Path)?.GameObject;
            return new SerializedGameObjectManager(Id, Instance!);
        }



        private static readonly Dictionary<Guid, GameObject> Registered = [];

        public static void Register(Guid id, GameObject obj)
        {
            if (!Registered.ContainsKey(id))
                Registered.Add(id, obj);
        }

        public static Guid? FetchId(GameObject obj)
        {
            foreach (var pair in Registered)
                if (pair.Value == obj)
                    return pair.Key;

            return null;
        }

        public static bool TryGet(Guid id, out GameObject obj)
        {
            return Registered.TryGetValue(id, out obj);
        }

        public static void Unregister(Guid id)
        {
            if (Registered.ContainsKey(id))
                Registered.Remove(id);
        }
    }
}
