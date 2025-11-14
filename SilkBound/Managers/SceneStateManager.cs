using HutongGames.PlayMaker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilkBound.Extensions;
using SilkBound.Network.Packets.Impl.Sync.Entity;
using SilkBound.Types.JsonConverters;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilkBound.Managers {
    // note: unless YOU wanna fuck around and make a whole ass JsonConverter for a single parameter type thats YOUR job to implement. otherwise just stick with serializable types and strictly gameobjects for unityobjects.
    public class SceneState(string sceneName) {
        public string SceneName = sceneName;

        #region StateChange cache
        [JsonIgnore]
        public List<StateChange> CachedChanges = [];
        #endregion

        #region Breakables
        public List<Guid> BrokenObjects = [];
        public bool RegisterBrokenObject(Guid id)
        {
            Logger.Msg("registering broken object:", id);
            if (BrokenObjects.Contains(id))
                return false;

            BrokenObjects.Add(id);
            return true;
        }
        #endregion

        #region FSM Event Queue
        public struct FSMEventData {
            //public string goPath;
            //public string fsmName;
            public Guid id;
            public string eventName;
            public bool dispatched;
        }
        public struct FSMStatusData {
            //public string goPath;
            //public string fsmName;
            public Guid id;
            public bool started;
        }
        public List<FSMEventData> QueuedEvents = [];
        public Dictionary<Guid, FSMStatusData> StatusUpdates = [];
        public void RegisterFSMEvent(Guid id, string eventName)
        {
            FSMEventData data = new() {
                //goPath = goPath,
                //fsmName = fsmName,
                id=id,
                eventName = eventName,
                dispatched = false
            };

            QueuedEvents.Add(data);
        }
        public void RegisterFSMStatus(Guid id, bool started)
        {
            FSMStatusData data = new() {
                //goPath = goPath,
                //fsmName = fsmName,
                id=id,
                started = started
            };

            if (StatusUpdates.ContainsKey(id))
                StatusUpdates[id] = data;
            else
                StatusUpdates.Add(id, data);
        }
        #endregion

        public void Sync(Scene scene)
        {
            #region StateChange flusher

            JsonSerializer methodSerializer = ChunkedTransfer.CreateSerializer([new GameObjectConverter(false)]);
            foreach (var change in CachedChanges)
            {
                switch (change.ChangeAction)
                {
                    case StateChange.Action.FieldSet:
                    {
                        var field = GetType().GetField(change.TargetName) ?? throw new Exception($"Field {change.TargetName} not found on SceneState");
                        if (change.Args.Length != 1)
                            throw new Exception($"FieldSet requires exactly 1 argument, got {change.Args.Length}");

                        if (change.Args[0] is JArray deserialized)
                            change.Args[0] = deserialized.ToObject(field.FieldType, methodSerializer);
                        field.SetValue(this, change.Args[0]);
                        break;
                    }
                    case StateChange.Action.PropertySet:
                    {
                        var prop = GetType().GetProperty(change.TargetName) ?? throw new Exception($"Property {change.TargetName} not found on SceneState");
                        if (change.Args.Length != 1)
                            throw new Exception($"PropertySet requires exactly 1 argument, got {change.Args.Length}");

                        if (change.Args[0] is JArray deserialized)
                            change.Args[0] = deserialized.ToObject(prop.PropertyType, methodSerializer);
                        prop.SetValue(this, change.Args[0]);
                        break;
                    }
                    case StateChange.Action.MethodCall:
                    {
                        var method = GetType().GetMethod(change.TargetName) ?? throw new Exception($"Method {change.TargetName} not found on SceneState");
                        for (int i = 0; i < change.Args.Length; i++)
                            if (change.Args[i] is JArray deserialized)
                                change.Args[i] = deserialized.ToObject(method.GetParameters()[i].ParameterType, methodSerializer);
                        method.Invoke(this, change.Args);
                        break;
                    }
                    case StateChange.Action.Reset:
                    {
                        BrokenObjects.Clear();

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
            }
            CachedChanges.Clear();
            #endregion

            #region Breakables
            foreach (Guid path in BrokenObjects)
            {
                GameObject? go = ObjectManager.Get(path)?.GameObject;
                if (go == null)
                    continue;

                Breakable component = go.GetComponent<Breakable>();
                if (component == null)
                    continue;

                component.SetAlreadyBroken();
            }
            #endregion

            #region FSM Event Queue
            for (int i = 0; i < QueuedEvents.Count; i++)
            {
                var eventData = QueuedEvents[i];
                if (!FSMPacket.FindFSM(eventData.id, out Fsm? fsm))
                    continue;

                fsm.Event(eventData.eventName);
                eventData.dispatched = true;

                QueuedEvents[i] = eventData;
            }

            foreach (var statusUpdate in StatusUpdates)
            {
                if (FSMPacket.FindFSM(statusUpdate.Value.id, out Fsm? fsm))
                {
                    if (statusUpdate.Value.started && !fsm.Started)
                        fsm.Start();
                    else if (!statusUpdate.Value.started && !fsm.Started)
                        fsm.Stop();
                }

            }
            QueuedEvents.RemoveAll(@event => @event.dispatched);
            #endregion
        }
    }

    public struct GuaranteedFetchResult<T>(T value, bool created) {
        public readonly T Result => value;
        public readonly bool Created => created;
    }

    public struct StateChange {
        public enum Action {
            FieldSet,
            PropertySet,
            MethodCall,
            Reset
            //[Obsolete] ListAppend
        }

        public Action ChangeAction;
        public string TargetName;
        public object?[] Args;

        public static StateChange Method(string name, params object?[] args)
        {
            return new StateChange() {
                ChangeAction = Action.MethodCall,
                TargetName = name,
                Args = args
            };
        }
        public static StateChange Field(string name, object? value)
        {
            return new StateChange() {
                ChangeAction = Action.FieldSet,
                TargetName = name,
                Args = [value]
            };
        }
        public static StateChange Property(string name, object? value)
        {
            return new StateChange() {
                ChangeAction = Action.PropertySet,
                TargetName = name,
                Args = [value]
            };
        }
        public static StateChange Reset()
        {
            return new StateChange() {
                ChangeAction = Action.Reset,
            };
        }
    }

    public class SceneStateManager {
        public static Dictionary<string, SceneState> States { get; internal set; } = [];
        public static GuaranteedFetchResult<SceneState> Fetch(string sceneName)
        {
            if (States.ContainsKey(sceneName))
                return new(States[sceneName], false);
            else
            {
                SceneState state = new(sceneName);
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
            return Fetch(SceneManager.GetActiveScene().name).Result;
        }

        public static void ApplyChange(string sceneName, StateChange change)
        {
            ApplyChanges(Fetch(sceneName).Result, [change]);
        }
        public static void ApplyChanges(SceneState state, StateChange[] changes)
        {
            state.CachedChanges.AddRange(changes);
        }
        public static bool ProposeChanges(string sceneName, params StateChange[] change)
        {
            return ProposeChanges(Fetch(sceneName).Result, change);
        }
        public static bool ProposeChanges(SceneState state, params StateChange[] changes)
        {
            ApplyChanges(state, changes);

            if (NetworkUtils.Connected)
            {
                TransferManager.Send(new SceneStateTransfer(state.SceneName, changes));
                return true;
            }

            return false;
        }
    }
}
