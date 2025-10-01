using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilkBound.Types.Mirrors
{
    public class HeroControllerMirror : HeroController
    {
        private new void Awake()
        {
            cState = new HeroControllerStates();
        }
        private new void Start() { }
        private new void Update() { }

        public bool IsMethod(string key)
        {
            return cState.GetType().GetMethod(key) != null;
        }
        public object? CallStateMember(string key, params object[] args)
        {
            return cState.GetType().GetMethod(key)?.Invoke(cState, args);
        }
        public T? CallStateMember<T>(string key, params object[] args)
        {
            return (T?) cState.GetType().GetMethod(key)?.Invoke(cState, args);
        }
        public void SetStateProperty(string key, object value)
        {
            var member = cState.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(info => (info.MemberType == MemberTypes.Property || info.MemberType == MemberTypes.Field)
                                     && info.Name == key);

            switch (member)
            {
                case PropertyInfo prop:
                    prop.SetValue(cState, value);
                    break;
                case FieldInfo field:
                    field.SetValue(cState, value);
                    break;
                default:
                    Logger.Error($"Property or field '{key}' not found");
                    break;
            }
        }
    }
}
