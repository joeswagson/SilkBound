using System;
using UnityEngine;
using SilkBound.Utils;
using UnityEngine.SceneManagement;
using SilkBound.Managers;
using SilkBound.Types;

namespace SilkBound.Behaviours {
    public class SceneStateSyncController : MonoBehaviour {
        static bool activated = false;
        static bool firstLoad = true;

        public static event Action<Scene>? SceneLoaded;

        private void Update()
        {
            if (activated || !NetworkUtils.Connected)
                return;

            activated = true;

            var scene = SceneManager.GetActiveScene();
            if (SilkConstants.DEBUG && scene.name == "Tut_03")
            {
                var t = GameObject.Find("RestBench").transform;
                t.position = new Vector3(76.8323f, 17.1686f, t.position.z);
            }
            if (scene.name != "Menu_Title" && scene.name != "Pre_Menu_Title")
                firstLoad = false;

            SceneLoaded?.Invoke(scene);
            Logger.Msg("syncing scenestate:", scene.name);
            Logger.Msg("- clients in scene:", Server.CurrentServer.GetPlayersInScene());

            if (Server.CurrentServer.GetPlayerCountInScene() == 0)
                SceneStateManager.ProposeChanges(scene.name, StateChange.Reset());

            SceneStateManager.Fetch(scene.name).Result.Sync(scene);

        }

        private void OnDestroy()
        {
            activated = false;
        }
    }
}
