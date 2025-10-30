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
#if DEBUG
using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using SilkBound.Types.Language;
#endif

#if MELON
[assembly: MelonInfo(typeof(ModMain), "SilkBound", "1.0.0", "@joeswanson.")]
#endif
namespace SilkBound
{
#if MELON
    public partial class ModMain : MelonMod
#elif BEPIN
    [BepInAutoPlugin("io.github.joeswagson", "SilkBound", "1.0.0")]
    [BepInProcess("Hollow Knight Silksong.exe")]
    public partial class ModMain : BaseUnityPlugin
#endif
    {
        public const string CONNECT_IP = "127.0.0.1";

        public static int MainThreadId;

#if BEPIN
        public Harmony HarmonyInstance = new(Id);
#endif

#if MELON
        public override void OnInitializeMelon()
#elif BEPIN
        public void Awake()
#endif
        {
            MainThreadId = Environment.CurrentManagedThreadId;
            Logger.Debug("SilkBound is in Debug mode. Client Number:", SilkDebug.GetClientNumber(),
                "| Unity Thread ID:", MainThreadId);
            var method = AccessTools.Method(typeof(HealthManager), "TakeDamage", [typeof(HitInstance)]);
            Logger.Msg(method == null ? "Method not found!" : $"Found method: {method.FullDescription()}");

            if (SilkConstants.DEBUG)
            {
                if (SilkDebug.GetClientNumber() > SilkConstants.TEST_CLIENTS)
                {
                    Logger.Error("Client number exceeds TEST_CLIENTS constant. Quitting.");
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

            foreach (var skin in SkinManager.Library)
            {
                Logger.Msg("skin:", skin.Key);
                //skin.Value.WriteToFile($"{skin.Key}.skin");
            }
            //TickManager.OnTick += () =>
            //{
            //    Logger.Msg("Tick");
            //};
            //Screen.SetResolution(1200, 600, false);

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
            int cW = 0;// 500;
            int w = SilkConstants.TEST_CLIENTS <= 2
                ? 1
                : 2; // SilkConstants.TEST_CLIENTS - (SilkConstants.TEST_CLIENTS % 1);
            SilkDebug.PositionConsoleWindow(
                new Vector2Int(5, 15),
                new Vector2Int(1200 + cW, 600),
                w
            );

            yield return new WaitForSeconds(0.5f);

            SilkDebug.PositionGameWindow(
                new Vector2Int(((int)Math.Ceiling(SilkConstants.TEST_CLIENTS / 2.0) * (1200 + cW)) + 5, 15),
                new Vector2Int(1200, 600),
                w
            );

            if (SilkDebug.GetClientNumber() == 1)
            {
                //Server.ConnectPipe("pipetest", "host");
                Server.ConnectTCP("127.0.0.1", "host");
                Skin skin = SkinManager.GetOrDefault("purple");
                NetworkUtils.LocalClient!.ChangeSkin(skin);
            }
            else
            {
                //NetworkUtils.ConnectPipe("pipetest", $"client{SilkDebug.GetClientNumber() - 1}");
                NetworkUtils.ConnectTCP(CONNECT_IP, $"client{SilkDebug.GetClientNumber() - 1}");
                NetworkUtils.ClientPacketHandler!.HandshakeFulfilled += () =>
                {
                    Skin skin = SilkDebug.GetClientNumber() switch
                    {
                        2 => SkinManager.GetOrDefault("blue"),
                        3 => SkinManager.GetOrDefault("green"),
                        4 => SkinManager.GetOrDefault("red"),
                        _ => SkinManager.Default
                    };
                    NetworkUtils.LocalClient!.ChangeSkin(skin);
                    ActionScope.Detach(NetworkUtils.ClientPacketHandler, nameof(NetworkUtils.ClientPacketHandler.HandshakeFulfilled));
                };

                //NetworkUtils.LocalClient!.AppliedSkin = SkinManager.GetOrDefault("blue");
                //NetworkUtils.ClientPacketHandler!.HandshakeFulfilled += () => NetworkUtils.LocalClient!.ChangeSkin(SkinManager.Library["blue"]);
            }
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

        public static readonly Config Config = ConfigurationManager.ReadFromFile("config");
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

            if (Cursor.visible && HeroController.instance != null)
                return;

            if (Input.GetKeyDown(KeyCode.Minus))
            {
                NetworkUtils.LocalClient?.Shaw();
                Logger.Msg("SHAW!");

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
                    Logger.Error("SteamAPI.Init() failed!");
                else
                    Logger.Msg("SteamAPI initialized.");
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                //Server.ConnectPipe("sb_dbg", "host");
                Server.ConnectTCP("0.0.0.0", "host");
                //Server.ConnectP2P("joe");
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                //NetworkUtils.ConnectPipe("sb_dbg", "client");

                NetworkUtils.ConnectTCP(CONNECT_IP, "client");
                NetworkUtils.ClientPacketHandler!.HandshakeFulfilled += () =>
                {
                    NetworkUtils.LocalClient!.ChangeSkin(SkinManager.GetOrDefault("blue"));
                    ActionScope.Detach(NetworkUtils.ClientPacketHandler, nameof(NetworkUtils.ClientPacketHandler.HandshakeFulfilled));
                };

            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                NetworkUtils.Disconnect("Leaving.");
            }
        }

        public
#if MELON
        override
#endif
            void OnApplicationQuit()
        {
            NetworkUtils.Disconnect();
        }

#if DEBUG
        /// <summary>
        /// https://github.com/Bigfootmech/Silksong_Skipintro/blob/master/Mod.cs - dont wanna install a seperate dll just for testing this so im including this in debug builds - should be removed in public releases that are built under Release
        /// </summary>
        [HarmonyPatch(typeof(StartManager), "Start")]
        public class AtStart
        {
            [HarmonyPostfix]
            static void Postfix(StartManager __instance,
                IEnumerator __result,
                ref AsyncOperation ___loadop) // , ref float ___FADE_SPEED
            {
                __instance.gameObject.GetComponent<Animator>().speed = 99999;
            }
        }

        [HarmonyPatch(typeof(CacheObjectBase))]
        public class CacheObjectBasePatches
        {
            [HarmonyPrefix]
            [HarmonyPatch("SetValueState")]
            public static bool blehhh(CacheObjectBase __instance, CacheObjectCell cell,
                CacheObjectBase.ValueStateArgs args)
            {
                if (cell is not CacheListEntryCell listEntry)
                    return true;


                void Convert(string name)
                {
                    AccessTools.Property(typeof(CacheObjectBase), "ValueLabelText")
                        .SetValue(
                            __instance,
                            UniverseLib.Utility.ToStringUtility.ToStringWithType(
                                __instance.Value,
                                __instance.FallbackType,
                                true
                            )
                            + $" - <i><color=#b0edff>{name}</color></i>"
                        );
                }

                switch (__instance.Value)
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
        }
#endif
    }
} 
