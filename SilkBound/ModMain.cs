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
            Logger.Debug("SilkBound is in Debug mode.");

            var overrideInstance = new HeroAnimationControllerOverrides();


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
        public class MainThreadDispatcher : MonoBehaviour
        {
            private static MainThreadDispatcher? _instance;
            public static MainThreadDispatcher Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var go = new GameObject("MainThreadDispatcher");
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<MainThreadDispatcher>();
                    }
                    return _instance;
                }
            }

            private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
            private readonly ConcurrentQueue<Action> _lateQueue = new ConcurrentQueue<Action>();
            public void Enqueue(Action action) => _queue.Enqueue(action);
            public void EnqueueLate(Action action) => _lateQueue.Enqueue(action);

            void Update()
            {
                while (_queue.TryDequeue(out var a))
                {
                    try { a(); }
                    catch (Exception e) { Debug.LogError("[MainThreadDispatcher] exception: " + e); }
                }
            }

            void LateUpdate()
            {
                while (_lateQueue.TryDequeue(out var a))
                {
                    try { a(); }
                    catch (Exception e) { Debug.LogError("[MainThreadDispatcher] exception: " + e); }
                }
            }

            void OnDestroy()
            {
                _instance = null;
            }
        }
        private static System.Random random = new System.Random();
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
                Logger.Msg("Sending test transfer");
                TransferManager.Send(new TestTransfer(new Dictionary<string, string>
                {
                    { "Hello", "World" },
                    { "Foo", "Bar" },
                    { "Lorem", "Ipsum" },
                    { "The quick brown fox", "jumps over the lazy dog" },
                    { "5kchars", RandomString(5000) }
                }));
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
                Server.ConnectTCP("127.0.0.1", "host");
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                //NetworkUtils.ConnectPipe("sb_dbg", "client");
                NetworkUtils.ConnectTCP("127.0.0.1", "client");
                while (!NetworkUtils.IsConnected) ;
                NetworkUtils.LocalConnection!.Send(new HandshakePacket(NetworkUtils.LocalClient!.ClientID.ToString(), NetworkUtils.LocalClient!.ClientName));
            }
        }
    }
}
