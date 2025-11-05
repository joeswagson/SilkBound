global using Logger = SilkBound.Utils.Logger;
global using Object = UnityEngine.Object;
using HarmonyLib;
using HutongGames.PlayMaker;
#if MELON
using SilkBound;
using MelonLoader;
#else
using BepInEx;
#endif
using SilkBound.Managers;
using SilkBound.Patches.Simple.Attacks;
using SilkBound.Types;
using SilkBound.Utils;
using Steamworks;
using System;
using System.Collections;
using System.Linq;
using SilkBound.Types.Language;
using UnityEngine;
using SilkBound.Types.Language;
using SilkBound.Lib.DbgRender;
using SilkBound.Lib.DbgRender.Renderers;
using System.Collections.Generic;
#if DEBUG
using SilkBound.Types.Language;
using System.Reflection;
using System.Threading;
#endif

#if MELON
[assembly: MelonInfo(typeof(ModMain), "SilkBound", "1.0.0", "@joeswanson.")]
#endif
namespace SilkBound {
#if MELON
    public partial class ModMain : MelonMod
#elif BEPIN
    [BepInAutoPlugin("io.github.joeswagson", "SilkBound", "1.0.0")]
    [BepInProcess("Hollow Knight Silksong.exe")]
    public partial class ModMain : BaseUnityPlugin
#endif
    {



        public static ModMain Instance = null!;
        public string HOST_IP => Config.HostIP;
        public string CONNECT_IP => Config.ConnectIP;

        public static int MainThreadId;
        public static Thread? ModThread;

#if BEPIN
        public Harmony HarmonyInstance = new(Id);
#endif

        public Dictionary<string, object?> listRendererData = [];

#if MELON
        public override void OnInitializeMelon()
#elif BEPIN
        public void Awake()
#endif
        {
            ModThread = Thread.CurrentThread;
            Config.SaveToFile(clientNumber > 1 ? $"config{clientNumber}" : "config"); // ensure config exists
            Instance = this;

            MainThreadId = Environment.CurrentManagedThreadId;
            global::SilkBound.Utils.Logger.Debug("SilkBound is in Debug mode. Client Number:", SilkDebug.GetClientNumber(),
                "| Unity Thread ID:", MainThreadId);
            var method = AccessTools.Method(typeof(HealthManager), "TakeDamage", [typeof(HitInstance)]);
            global::SilkBound.Utils.Logger.Msg(method == null ? "Method not found!" : $"Found method: {method.FullDescription()}");

            if (SilkConstants.DEBUG)
            {
                if (SilkDebug.GetClientNumber() > SilkConstants.TEST_CLIENTS)
                {
                    global::SilkBound.Utils.Logger.Error("Client number exceeds TEST_CLIENTS constant. Quitting.");
                    Application.Quit();
                    return;
                }

                DebugDrawColliderRuntime.IsShowing = SilkConstants.DEBUG_COLLIDERS;

                CheatManager.Invincibility = SilkConstants.INVULNERABILITY
                    ? CheatManager.InvincibilityStates.FullInvincible
                    : CheatManager.InvincibilityStates.Off;
                Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            }
            //_loaded = -1;
            //SceneManager.sceneLoaded += (scene, mode) =>
            //{
            //    _loaded = Time.frameCount;
            //    _lastScene = (scene, mode);
            //};

            #region Patches

            //var overrideInstance = new HeroAnimationControllerOverrides();

            var cacheObjectBase = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(a => a.GetType("UnityExplorer.CacheObject.CacheObjectBase", throwOnError: false))
                    .FirstOrDefault(t => t != null);
            //Logger.Msg("base:", cacheObjectBase);

            if (cacheObjectBase != null)
            {
                var methodTarget = AccessTools.Method(cacheObjectBase, "SetValueState");
                var met = AccessTools.Method(GetType(), nameof(lePrefixduCacheObjectBase));
                //Logger.Msg("target:", methodTarget?.Name ?? "null");
                //Logger.Msg("prefix:", met?.Name ?? "null");
                HarmonyInstance.Patch(methodTarget, new HarmonyMethod(met));
            }

            foreach (var t in AccessTools.AllTypes())
            {
                if (typeof(IHitResponder).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                {
                    var m = AccessTools.Method(t, "Hit", [typeof(HitInstance)]);
                    if (m == null) continue;

                    if (m.DeclaringType != t)
                        continue;
                    HarmonyInstance.Patch(m,
                        prefix: new HarmonyMethod(typeof(IHitResponderPatches),
                            nameof(IHitResponderPatches.HitPrefix)));
                }
            }

            #endregion

            //foreach (var skin in SkinManager.Library)
            //{
            //    Logger.Msg("skin:", skin.Key);
            //    //skin.Value.WriteToFile($"{skin.Key}.skin");
            //}
            //TickManager.OnTick += () =>
            //{
            //    Logger.Msg("Tick");
            //};
            //Screen.SetResolution(1200, 600, false);

            #region Debug Renderer
            if (SilkConstants.DEBUG)
            {
                int clientCount = 0;
                var clients = "";
                listRendererData["Connected"] =
                    new UpdatingHostVariable<Server>(
                        Server.CurrentServer,
                        () => Server.CurrentServer,
                        c => c?.Connections?.Count.ToString() ?? "no server");
                listRendererData["Clients"] =
                    new UpdatingHostVariable<Server>(
                        Server.CurrentServer,
                        () => Server.CurrentServer,
                        (c) => {
                            if (c == null)
                                return "no server";

                            if (clientCount != c.Connections.Count)
                            {
                                var conns = c.Connections.Select(w => $"- {w.ClientName}");
                                clientCount = conns.Count();
                                clients = '\n' + conns.Join();
                            }

                            return clients;
                        });
                listRendererData["ObjectManager Cache"] =
                    new UpdatingVariable<Dictionary<string, DisposableGameObject>>(
                        ObjectManager.Cache,
                        c => c.Count.ToString());

                DbgRenderCore.RegisterRenderer(new ListRenderer(listRendererData));

                ConnectionMenu = new ConnectionMenuRenderer();
                DbgRenderCore.RegisterRenderer(ConnectionMenu);
            }
            #endregion
        }
        internal static ConnectionMenuRenderer? ConnectionMenu;
        public
#if MELON
        override
#endif
        void OnGUI()
        {

            DbgRenderCore.OnGUI();
        }

#if MELON
        public override void OnLateInitializeMelon()
#elif BEPIN
        public void Start()
#endif
        {
#if DEBUG
#if MELON
            MelonCoroutines.Start(DelayedWindowPosition());
#elif BEPIN
            StartCoroutine(DelayedWindowPosition());
#endif
#endif
        }
#if DEBUG
        System.Collections.IEnumerator DelayedWindowPosition()
        {
            bool smallWindow = false;
            int width = smallWindow ? 950 : 1200;
            int height = smallWindow ? 500 : 600;

            int cW = 0;// 500;
            int w = SilkConstants.TEST_CLIENTS <= 2
                ? 1
                : 2; // SilkConstants.TEST_CLIENTS - (SilkConstants.TEST_CLIENTS % 1);
            SilkDebug.PositionConsoleWindow(
                new Vector2Int(5, 15),
                new Vector2Int(width + cW, height),
                w
            );

            yield return new WaitForSeconds(0.5f);

            SilkDebug.PositionGameWindow(
                new Vector2Int(((int) Math.Ceiling(SilkConstants.TEST_CLIENTS / 2.0) * (width + cW)) + 5, 15),
                new Vector2Int(width, height),
                w
            );

            //if (SilkDebug.GetClientNumber() == 1)
            //{
            //    //Server.ConnectPipe("pipetest", "host");
            //    //Server.ConnectTCP("127.0.0.1", "host");
            //    //ConnectionManager.Server(NetworkingLayer.TCP, HOST_IP, name: "host").ContinueWith(t => NetworkUtils.LocalClient.ChangeSkin(SkinManager.GetOrDefault("blue")));

            //    //NetworkUtils.LocalClient.ChangeSkin(SkinManager.GetOrDefault("blue"));
            //    //Skin skin = SkinManager.GetOrDefault("blue");
            //    //NetworkUtils.LocalClient!.ChangeSkin(skin);
            //} else
            //{
            //    //NetworkUtils.ConnectPipe("pipetest", $"client{SilkDebug.GetClientNumber() - 1}");
            //    //NetworkUtils.ConnectTCP(CONNECT_IP, $"client{SilkDebug.GetClientNumber() - 1}");
            //    //_ = ConnectionManager.Client(NetworkingLayer.TCP, CONNECT_IP);
            //    //NetworkUtils.ClientPacketHandler!.HandshakeFulfilled += () => {
            //    //    Skin skin = SilkDebug.GetClientNumber() switch {
            //    //        2 => SkinManager.GetOrDefault("purple"),
            //    //        3 => SkinManager.GetOrDefault("green"),
            //    //        4 => SkinManager.GetOrDefault("red"),
            //    //        _ => SkinManager.Default
            //    //    };
            //    //    NetworkUtils.LocalClient!.ChangeSkin(skin);
            //    //    ActionScope.Detach(NetworkUtils.ClientPacketHandler, nameof(NetworkUtils.ClientPacketHandler.HandshakeFulfilled));
            //    //};

            //    //NetworkUtils.LocalClient!.AppliedSkin = SkinManager.GetOrDefault("blue");
            //    //NetworkUtils.ClientPacketHandler!.HandshakeFulfilled += () => NetworkUtils.LocalClient!.ChangeSkin(SkinManager.Library["blue"]);
            //}
        }
#endif
        //public class MainThreadDispatcher : MonoBehaviour
        //{
        //    private static MainThreadDispatcher? _instance;
        //    public static MainThreadDispatcher Instance
        //    {
        //        get
        //        {
        //            if (_instance == null)
        //            {
        //                var go = new GameObject("MainThreadDispatcher");
        //                DontDestroyOnLoad(go);
        //                _instance = go.AddComponent<MainThreadDispatcher>();
        //            }
        //            return _instance;
        //        }
        //    }

        //    private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        //    private readonly ConcurrentQueue<Action> _lateQueue = new ConcurrentQueue<Action>();
        //    public void Enqueue(Action action) => _queue.Enqueue(action);
        //    public void EnqueueLate(Action action) => _lateQueue.Enqueue(action);

        //    void Update()
        //    {
        //        while (_queue.TryDequeue(out var a))
        //        {
        //            try { a(); }
        //            catch (Exception e) { Debug.LogError("[MainThreadDispatcher] exception: " + e); }
        //        }
        //    }

        //    void LateUpdate()
        //    {
        //        while (_lateQueue.TryDequeue(out var a))
        //        {
        //            try { a(); }
        //            catch (Exception e) { Debug.LogError("[MainThreadDispatcher] exception: " + e); }
        //        }
        //    }

        //    void OnDestroy()
        //    {
        //        _instance = null;
        //    }
        //}

        private static readonly int clientNumber = SilkDebug.GetClientNumber();
        public static readonly Config Config = ConfigurationManager.ReadFromFile(clientNumber > 1 ? $"config{clientNumber}" : "config");
        private static readonly System.Random random = new();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string([.. Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)])]);
        }
#if MELON
        public override void OnUpdate()
#elif BEPIN
        public void Update()
#endif
        {
            TickManager.Update();



            //Logger.Msg("Clients:", (Server.CurrentServer?.Connection as TCPServer)?.GetPlayerList().Count);

            //if (_loaded > -1 && Time.frameCount > _loaded)
            //{
            //    _loaded = -1;

            //    Scene scene = _lastScene.Item1;
            //    LoadSceneMode mode = _lastScene.Item2;

            //    if (scene.name == "Menu_Title" || scene.name == "Pre_Menu_Intro" || !scene.IsValid())
            //        return;

            //    SceneStateManager.Fetch(scene.name).Value.Sync(scene);
            //}

            if (Input.GetKeyDown(KeyCode.F8))
            {
                DbgRenderCore.Toggle();
            }

            if (Cursor.visible && HeroController.instance != null)
                return;


            if (Input.GetKeyDown(KeyCode.Minus))
            {
                NetworkUtils.LocalClient.Shaw();
                global::SilkBound.Utils.Logger.Msg("SHAW!");

                //Logger.Msg("Sending test transfer");
                //TransferManager.Send(new TestTransfer(new Dictionary<string, string>
                //{
                //    { "Hello", "World" },
                //    { "Foo", "Bar" },
                //    { "Lorem", "Ipsum" },
                //    { "The quick brown fox", "jumps over the lazy dog" },
                //    { "5kchars", RandomString(5000) }
                //}));
                //Logger.Debug("sending handshake", Guid.NewGuid().ToString("N"));
                //NetworkUtils.LocalConnection?.Send(new HandshakePacket(NetworkUtils.LocalClient!.ClientID.ToString(), NetworkUtils.LocalClient!.ClientName));
            }

            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                if (!SteamAPI.Init())
                    global::SilkBound.Utils.Logger.Error("SteamAPI.Init() failed!");
                else
                    global::SilkBound.Utils.Logger.Msg("SteamAPI initialized.");
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                //    //Server.ConnectPipe("sb_dbg", "host");
                //    switch (Config.NetworkLayer)
                //    {
                //        case NetworkingLayer.TCP:
                //            Server.ConnectTCP(HOST_IP, Config.Username);
                //            break;
                //        case NetworkingLayer.Steam:
                //            Server.ConnectP2P(Config.Username);
                //            break;
                //        case NetworkingLayer.NamedPipe:
                //            Server.ConnectPipe(HOST_IP, Config.Username);
                //            break;
                //    }
                //    NetworkUtils.LocalClient.ChangeSkin(SkinManager.GetOrDefault("blue"));
                //    //Server.ConnectP2P("joe");
                //}

                //if (Input.GetKeyDown(KeyCode.J))
                //{
                //    //NetworkUtils.ConnectPipe("sb_dbg", "client");

                //    switch (Config.NetworkLayer)
                //    {
                //        case NetworkingLayer.TCP:
                //            NetworkUtils.ConnectTCP(CONNECT_IP, Config.Username);
                //            break;
                //        case NetworkingLayer.Steam:
                //            NetworkUtils.ConnectP2P(ulong.Parse(CONNECT_IP), Config.Username);
                //            break;
                //        case NetworkingLayer.NamedPipe:
                //            NetworkUtils.ConnectPipe(CONNECT_IP, Config.Username);
                //            break;
                //    }

                //    NetworkUtils.ClientPacketHandler!.HandshakeFulfilled += () => {
                //        NetworkUtils.LocalClient.ChangeSkin(SkinManager.GetOrDefault("purple"));
                //    };

                //}

                if (Input.GetKeyDown(KeyCode.K))
                {
                    NetworkUtils.Disconnect("Leaving.");
                }
            }
        }

        static bool lePrefixduCacheObjectBase(object __instance, [HarmonyArgument("cell")] object cell, [HarmonyArgument("args")] object args)
        {
            var valueProp = AccessTools.Property(__instance.GetType(), "Value");
            var value = valueProp?.GetValue(__instance);

            var fallbackTypeProp = AccessTools.Property(__instance.GetType(), "FallbackType");
            var fallbackType = fallbackTypeProp?.GetValue(__instance) as Type;

            var labelProp = AccessTools.Property(__instance.GetType(), "ValueLabelText");
            if (labelProp == null)
                return true;

            void Convert(string name)
            {
                var formatted = AccessTools.Method("UniverseLib.Utility.ToStringUtility:ToStringWithType").Invoke(null, [value, fallbackType, true]) + $" - <i><color=#b0edff>{name}</color></i>";
                labelProp.SetValue(__instance, formatted);
            }

            switch (value)
            {
                case FsmState fsm: Convert(fsm.Name); break;
                case FsmEvent fsm: Convert(fsm.Name); break;
                case FsmStateAction fsm: Convert(fsm.Name); break;
                case FsmVar fsm: Convert(fsm.NamedVar.Name); break;
                case tk2dSprite tk2d: Convert(tk2d.Collection.spriteCollectionName); break;
                case tk2dSpriteCollection tk2d: Convert(tk2d.spriteCollection.name); break;
                case tk2dSpriteCollectionData tk2d: Convert(tk2d.name); break;
                case tk2dSpriteAnimation tk2d: Convert(tk2d.name); break;
                case tk2dSpriteAnimationClip tk2d: Convert(tk2d.name); break;
            }

            return true;
        }

        public
#if MELON
        override
#endif
            void OnApplicationQuit()
        {
            NetworkUtils.Disconnect("Game closing.");
            DbgRenderCore.Dispose();
            Config.SaveToFile(clientNumber > 1 ? $"config{clientNumber}" : "config");
        }

#if DEBUG
        /// <summary>
        /// https://github.com/Bigfootmech/Silksong_Skipintro/blob/master/Mod.cs - dont wanna install a seperate dll just for testing this so im including this in debug builds - should be removed in public releases that are built under Release
        /// </summary>
        [HarmonyPatch(typeof(StartManager), "Start")]
        public class AtStart {
            [HarmonyPostfix]
            static void Postfix(StartManager __instance,
                IEnumerator __result,
                ref AsyncOperation ___loadop) // , ref float ___FADE_SPEED
            {
                __instance.gameObject.GetComponent<Animator>().speed = 99999;
            }
        }

        //[HarmonyPatch]
        //public static class CacheObjectBasePatches {

        //}
#endif
    }
}
