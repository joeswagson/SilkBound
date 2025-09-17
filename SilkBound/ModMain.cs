using SilkBound;
using MelonLoader;
using System;
using UnityEngine;
using SilkBound.Types;
using SilkBound.Utils;
using Logger = SilkBound.Utils.Logger;
using SilkBound.Packets.Impl;


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
                Logger.Debug("test.", Guid.NewGuid().ToString("N"));
            }

            if (Input.GetKeyDown(KeyCode.H)) {
                Server.ConnectPiped("sb_dbg", "server");
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                NamedPipeConnection clientConnection = new NamedPipeConnection("sb_dbg");
                NetworkUtils.Connect(clientConnection, "client");
                while (clientConnection.Stream == null || (clientConnection.Stream != null && !clientConnection.Stream.IsConnected)) ;
                clientConnection.Send(new HandshakePacket(NetworkUtils.LocalClient!.ClientID.ToString()));
                Logger.Debug("sent handshake from client.");
            }
        }
    }
}
