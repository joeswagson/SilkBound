using SilkBound;
using MelonLoader;
using System;
using UnityEngine;
using SilkBound.Types;
using SilkBound.Utils;
using Logger = SilkBound.Utils.Logger;
using SilkBound.Packets.Impl;
using SilkBound.Types.NetLayers;
using Steamworks;


[assembly: MelonInfo(typeof(ModMain), "SilkBound", "1.0.0", "@joeswanson.")]
namespace SilkBound
{
    public class ModMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Logger.Debug("SilkBound is in Debug mode.");
        }
        public override void OnUpdate()
        {
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
                Server.ConnectPipe("sb_dbg", "host");
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                NetworkUtils.ConnectPipe("sb_dbg", "client");
                while (!NetworkUtils.IsConnected) ;
                NetworkUtils.LocalConnection!.Send(new HandshakePacket(NetworkUtils.LocalClient!.ClientID.ToString(), NetworkUtils.LocalClient!.ClientName));
            }
        }
    }
}
