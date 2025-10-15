using SilkBound.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilkBound.Managers
{
    public class SceneState(string sceneName)
    {
        public string SceneName => sceneName;

        #region Breakables
        public List<string> BrokenObjects = new List<string>();
        public bool RegisterBrokenObject(Breakable breakable)
        {
            string serialized = breakable.transform.GetPath();
            if (BrokenObjects.Contains(serialized))
                return false;

            BrokenObjects.Add(serialized);
            return true;
        }
        #endregion

        public void Sync()
        {
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

    public class SceneStateManager
    {
        public static readonly Dictionary<string, SceneState> States = new Dictionary<string, SceneState>();

        public static GuaranteedFetchResult<SceneState> Fetch(string sceneName)
        {
            if (States.ContainsKey(sceneName))
                return new(States[sceneName], false);
            else
            {
                SceneState state = new SceneState(sceneName);
                States[sceneName] = state;
                return new(state, true);
            }
        }

        public bool TryAdd(SceneState? state)
        {
            if (state == null)
                return false;

            if (States.ContainsKey(state.SceneName))
                return false;

            States.Add(state.SceneName, state);

            return true;
        }

        public static void Register(SceneState? state)
        {
            if (state == null)
                return;

            States[state.SceneName] = state;
        }

        public SceneState GetCurrent()
        {
            return Fetch(SceneManager.GetActiveScene().name).Value;
        }
    }
}
