﻿using System;
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
        private void Update() {
            if (activated)
                return;

            var scene = SceneManager.GetActiveScene();
            if (SilkConstants.DEBUG && scene.name == "Tut_03")
                GameObject.Find("RestBench").transform.position.Set(76.8323f, 17.5686f, 0.004f);
            if (scene.name != "Menu_Title" && scene.name != "Pre_Menu_Title")
                firstLoad = false;
            activated = true;

            SceneLoaded?.Invoke(scene);
            Logger.Msg("syncing scenestate:", scene.name);
            Logger.Msg("- clients in scene:", Server.CurrentServer.GetPlayersInScene());

            if (Server.CurrentServer.GetPlayersInScene() == 0)
                SceneStateManager.ProposeChanges(scene.name, StateChange.Reset());

            SceneStateManager.Fetch(scene.name).Result.Sync(scene);

        }

        private void OnDestroy() {
            activated = false;
        }
    }
}
