using SilkBound;
using MelonLoader;
using System;
using UnityEngine;
using SilkBound.Types;
using SilkBound.Utils;
using Logger = SilkBound.Utils.Logger;
using SilkBound.Network.Packets.Impl;
using SilkBound.Types.NetLayers;
using Steamworks;
using SilkBound.Managers;
using System.Collections.Concurrent;
using SilkBound.Types.Transfers;
using System.Collections.Generic;
using System.Linq;
using SilkBound.Patches.Overrides.Impl;
using SilkBound.Patches.Overrides;
using SilkBound.Network.Packets.Impl.Communication;
using HarmonyLib;
using SilkBound.Patches.Simple.Attacks;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Network.Packets.Handlers;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections;


[assembly: MelonInfo(typeof(ModMain), "SilkBound", "1.0.0", "@joeswanson.")]
namespace SilkBound
{
    public class ModMain : MelonMod
    {
        public override void OnEarlyInitializeMelon()
        {
        }


        public override void OnInitializeMelon()
        {
            Logger.Debug("SilkBound is in Debug mode. Client Number:", SilkDebug.GetClientNumber());
            if (SilkConstants.DEBUG)
            {
                
                Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            }

            #region Patches
            //var overrideInstance = new HeroAnimationControllerOverrides();

            foreach (var t in AccessTools.AllTypes())
            {
                if (typeof(IHitResponder).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                {
                    var m = AccessTools.Method(t, "Hit", new Type[] { typeof(HitInstance) });
                    if (m == null) continue;

                    if (m.DeclaringType != t)
                        continue;

                    HarmonyInstance.Patch(m, prefix: new HarmonyMethod(typeof(IHitResponderPatches), nameof(IHitResponderPatches.HitPrefix)));
                }
            }
            #endregion
            //foreach (var skin in SkinManager.Library)
            //{
            //    skin.Value.WriteToFile($"{skin.Key}.skin");
            //}
            //TickManager.OnTick += () =>
            //{
            //    Logger.Msg("Tick");
            //};
            //Screen.SetResolution(1200, 600, false);

        }

        public override void OnLateInitializeMelon()
        {
#if DEBUG
            MelonCoroutines.Start(DelayedWindowPosition());
#endif
        }
#if DEBUG
        System.Collections.IEnumerator DelayedWindowPosition()
        {

            SilkDebug.PositionConsoleWindow(
                new Vector2Int(50, 50),
                new Vector2Int(1200, 600),
                1
            );
            yield return new WaitForSeconds(1);

            SilkDebug.PositionGameWindow(
                new Vector2Int(1250, 50),
                new Vector2Int(1200, 600),
                1
            );

            if (SilkDebug.GetClientNumber() == 1)
            {
                Server.ConnectTCP("127.0.0.1", "host");
            }
            else
            {
                NetworkUtils.ConnectTCP("127.0.0.1", "client");
                NetworkUtils.ClientPacketHandler!.HandshakeFulfilled += () => NetworkUtils.LocalClient!.ChangeSkin(SkinManager.Library["blue"]);

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
        private static readonly System.Random random = new System.Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public override void OnUpdate()
        {
            TickManager.Update();

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

                NetworkUtils.ConnectTCP("127.0.0.1", "client");
                //NetworkUtils.ConnectP2P(76561198383107093, "dyluxe");
                Logger.Msg("sednign handshakl");
                //NetworkUtils.LocalConnection!.Send(new HandshakePacket(NetworkUtils.LocalClient!.ClientID, NetworkUtils.LocalClient!.ClientName));
                NetworkUtils.ClientPacketHandler!.HandshakeFulfilled += () => NetworkUtils.LocalClient!.ChangeSkin(SkinManager.Library["blue"]);

            }
        }

        public override void OnApplicationQuit()
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
#endif
    }
}
