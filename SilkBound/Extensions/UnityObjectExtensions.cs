using AsmResolver.DotNet.Code.Cil;
using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Extensions
{
    public static class UnityObjectExtensions
    {
        public static Component CopyComponent(this Component original, GameObject destination, params string[] excludeProps)
        {
            var list = excludeProps.ToList();
            list.AddRange(new List<string>() {
                {
                    "name"
                }
            });
            excludeProps = list.ToArray();
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default;
            PropertyInfo[] properties = type.GetProperties(flags);
            foreach (var prop in properties.Where(prop => !excludeProps.Contains(prop.Name)))
            {
                if (prop.CanWrite && prop.GetCustomAttribute(typeof(System.ObsoleteAttribute)) == null)
                {
                    try
                    {
                        Logger.Msg("set prop:", prop.Name, "to", prop.GetValue(original, null));
                        prop.SetValue(copy, prop.GetValue(original, null), null);
                    }
                    catch { }
                }
            }

            FieldInfo[] fields = type.GetFields(flags);
            foreach (var field in fields.Where(prop => !excludeProps.Contains(prop.Name)))
            {
                Logger.Msg("set field:", field.Name, "to", field.GetValue(original));
                field.SetValue(copy, field.GetValue(original));
            }
            Logger.Msg("copied:", copy);

            return copy;
        }

        public static string GetPath(this Transform transform)
        {
            return GetFullName(transform.gameObject);

            //if (transform.parent == null)
            //    return "/" + transform.name;
            //return transform.parent.GetPath() + "/" + transform.name;
        }
        public static string GetFullName(GameObject current)
        {
            if (current == null)
                return "";
            if (current.transform.parent == null)
                return "/" + current.name;
            return GetFullName(current.transform.parent.gameObject) + "/" + current.name;
        }
        public static GameObject? Resolve(GameObject root, string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return null;

            string[] parts = fullName.Split('/');
            if (parts.Length == 0)
                return null;

            int startIndex = parts[0] == "" ? 1 : 0;

            GameObject current = root;
            for (int i = startIndex + 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                Transform child = current.transform.Find(parts[i]);
                if (child == null)
                    return null;

                current = child.gameObject;
            }

            return current;
        }
        static GameObject? lamb = null;
        private static List<GameObject>? GetDontDestroyOnLoadRoots()
        {
            var temp = lamb ??= new GameObject("Sacrificial Lamb");
            UnityEngine.Object.DontDestroyOnLoad(temp);

            var scene = temp.scene;
            var roots = scene.GetRootGameObjects().ToList();

            return roots;
        }

        public static GameObject? FindObjectFromFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return null;

            string[] parts = fullName.Split('/');
            int startIndex = parts[0] == "" ? 1 : 0;

            List<GameObject> roots = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                roots.AddRange(SceneManager.GetSceneAt(i).GetRootGameObjects());
            }

            var persistentRootObjects = GetDontDestroyOnLoadRoots();
            if (persistentRootObjects != null)
                roots.AddRange(persistentRootObjects);

            GameObject? current = roots.FirstOrDefault(r => r.name == parts[startIndex]);
            if (current == null)
                return null;

            for (int i = startIndex + 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                var child = current.transform.Find(parts[i]);
                if (child == null)
                    return null;

                current = child.gameObject;
            }

            return current;
        }
    }
}
