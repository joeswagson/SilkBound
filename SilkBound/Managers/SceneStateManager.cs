using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilkBound.Extensions;
using SilkBound.Types.JsonConverters;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Managers
{
    // note: unless YOU wanna fuck around and make a whole ass JsonConverter for a single parameter type thats YOUR job to implement. otherwise just stick with serializable types and strictly gameobjects for unityobjects.
    public class SceneState
    {
        [JsonIgnore]
        public List<StateChange> CachedChanges = new List<StateChange>();

        public static SceneState Create(string sceneName)
        {
            return new SceneState()
            {
                SceneName = sceneName,
                BrokenObjects = new List<string>()
            };
        }

        public string SceneName = string.Empty;

        #region Breakables
        public List<string> BrokenObjects = new List<string>();
        public bool RegisterBrokenObject(GameObject breakable)
        {
            Logger.Msg("registering broken object:", breakable.name);
            string serialized = breakable.transform.GetPath();
            if (BrokenObjects.Contains(serialized))
                return false;

            BrokenObjects.Add(serialized);
            return true;
        }
        #endregion

        public void Sync(Scene scene)
        {
            #region StateChange flusher

            JsonSerializer methodSerializer = ChunkedTransfer.CreateSerializer(new JsonConverter[] { new GameObjectConverter(false) });
            foreach (var change in CachedChanges)
                switch (change.ChangeAction)
                {
                    case StateChange.Action.FieldSet:
                        {
                            var field = GetType().GetField(change.TargetName);
                            if (field == null)
                                throw new Exception($"Field {change.TargetName} not found on SceneState");
                            if (change.Args.Length != 1)
                                throw new Exception($"FieldSet requires exactly 1 argument, got {change.Args.Length}");
                            field.SetValue(this, change.Args[0]);
                            break;
                        }
                    case StateChange.Action.PropertySet:
                        {
                            var prop = GetType().GetProperty(change.TargetName);
                            if (prop == null)
                                throw new Exception($"Property {change.TargetName} not found on SceneState");
                            if (change.Args.Length != 1)
                                throw new Exception($"PropertySet requires exactly 1 argument, got {change.Args.Length}");
                            prop.SetValue(this, change.Args[0]);
                            break;
                        }
                    case StateChange.Action.MethodCall:
                        {
                            var method = GetType().GetMethod(change.TargetName);
                            if (method == null)
                                throw new Exception($"Method {change.TargetName} not found on SceneState");
                            for (int i = 0; i < change.Args.Length; i++)
                                if (change.Args[i] is JArray deserialized)
                                    change.Args[i] = deserialized.ToObject(method.GetParameters()[i].ParameterType, methodSerializer);
                            method.Invoke(this, change.Args);
                            break;
                        }
                        #region wip stuff
                        //case StateChange.Action.ListAppend:
                        //    {
                        //        var field = state.GetType().GetField(change.TargetName);
                        //        if (field == null)
                        //            throw new Exception($"Field {change.TargetName} not found on SceneState");
                        //        if (change.Args.Length != 1)
                        //            throw new Exception($"ListAppend requires exactly 1 argument, got {change.Args.Length}");
                        //        var list = field.GetValue(state) as System.Collections.IList;
                        //        if (list == null)
                        //            throw new Exception($"Field {change.TargetName} is not a list");
                        //        list.Add(change.Args[0]);
                        //        break;
                        //    }
                        #endregion
                }
            CachedChanges.Clear();
            #endregion

            #region Breakables
            foreach (string path in BrokenObjects)
            {
                GameObject? go = UnityObjectExtensions.FindObjectFromFullName(path);
                if (go == null)
                    continue;

                Breakable component = go.GetComponent<Breakable>();
                if (component == null)
                    continue;

                component.SetAlreadyBroken();
            }
            #endregion
        }
    }

    public struct GuaranteedFetchResult<T>(T value, bool created)
    {
        public T Value => value;
        public bool Created => created;
    }

    public struct StateChange
    {
        public enum Action
        {
            FieldSet,
            PropertySet,
            MethodCall,
            //[Obsolete] ListAppend
        }

        public Action ChangeAction;
        public string TargetName;
        public object?[] Args;

        public static StateChange Method(string name, params object?[] args)
        {
            return new StateChange()
            {
                ChangeAction = Action.MethodCall,
                TargetName = name,
                Args = args
            };
        }
        public static StateChange Field(string name, object? value)
        {
            return new StateChange()
            {
                ChangeAction = Action.FieldSet,
                TargetName = name,
                Args = new object?[] { value }
            };
        }
        public static StateChange Property(string name, object? value)
        {
            return new StateChange()
            {
                ChangeAction = Action.PropertySet,
                TargetName = name,
                Args = new object?[] { value }
            };
        }
    }

    public class SceneStateManager
    {
        public static Dictionary<string, SceneState> States { get; internal set; } = new Dictionary<string, SceneState>();
        public static GuaranteedFetchResult<SceneState> Fetch(string sceneName)
        {
            if (States.ContainsKey(sceneName))
                return new(States[sceneName], false);
            else
            {
                SceneState state = SceneState.Create(sceneName);
                States[sceneName] = state;
                return new(state, true);
            }
        }

        public bool TryAdd(SceneState state)
        {
            if (States.ContainsKey(state.SceneName))
                return false;

            States.Add(state.SceneName, state);

            return true;
        }

        public static void Register(SceneState state)
        {
            States[state.SceneName] = state;
        }

        public static SceneState GetCurrent()
        {
            return Fetch(SceneManager.GetActiveScene().name).Value;
        }

        public static void ApplyChange(string sceneName, StateChange change)
        {
            ApplyChanges(Fetch(sceneName).Value, new[] { change });
        }
        public static void ApplyChanges(SceneState state, StateChange[] changes)
        {
            state.CachedChanges.AddRange(changes);
        }
        public static bool ProposeChanges(string sceneName, params StateChange[] change)
        {
            return ProposeChanges(Fetch(sceneName).Value, change);
        }
        public static bool ProposeChanges(SceneState state, params StateChange[] changes)
        {
            ApplyChanges(state, changes);

            if (NetworkUtils.IsConnected)
            {
                TransferManager.Send(new SceneStateTransfer(state.SceneName, changes));
                return true;
            }

            return false;
        }
    }
}
