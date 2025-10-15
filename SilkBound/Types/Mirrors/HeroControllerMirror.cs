using GlobalEnums;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Types.Mirrors
{
    public class HeroControllerMirror : HeroController
    {
        #region Message Overrides
        new void Awake()
        {
            cState = new HeroControllerStates();
        }
        new void FixedUpdate() { }
        new void LateUpdate() { }
        new void OnDestroy() { }
        new void OnDisable() { }
        new void OnValidate() { }
        new void Start() { }
        new void Update() { }
        #endregion

        //downspike fx
        public void HandleCollisionTouching(Collision2D collision, Vector3? downspikeEffectPrefabSpawnPoint)
        {
            if (FindCollisionDirection(collision) == CollisionSide.bottom)
            {
                Vector3 vector = downspikeEffectPrefabSpawnPoint.HasValue ? downspikeEffectPrefabSpawnPoint.Value : transform.position;
                HeroController.instance.nailTerrainImpactEffectPrefabDownSpike.Spawn(vector, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
                NailSlashTerrainThunk.ReportDownspikeHitGround(vector);
            }
        }

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
            return (T?)cState.GetType().GetMethod(key)?.Invoke(cState, args);
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
