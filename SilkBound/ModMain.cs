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


[assembly: MelonInfo(typeof(ModMain), "SilkBound", "1.0.0", "@joeswanson.")]
namespace SilkBound
{
    public class ModMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Logger.Debug("SilkBound is in Debug mode.");
            ModFolder.RegisterFolders();
        }
        public override void OnUpdate()
        {
            TickManager.Update();

            if (Input.GetKeyDown(KeyCode.Minus))
            {
                Logger.Debug("sending handshake", Guid.NewGuid().ToString("N"));
                NetworkUtils.LocalConnection?.Send(new HandshakePacket(NetworkUtils.LocalClient!.ClientID.ToString(), NetworkUtils.LocalClient!.ClientName));
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
