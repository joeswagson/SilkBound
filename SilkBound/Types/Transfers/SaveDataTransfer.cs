using SilkBound.Managers;
using SilkBound.Network.Packets.Impl;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.UI.SaveSlotButton;

namespace SilkBound.Types.Transfers
{
    public class SaveDataTransfer : Transfer
    {
        public SaveDataTransfer()
        {

        }

        public class OnlineSave
        {
            public int HostHash;
            public string? SceneName;
            public string? SceneMarker;
            public SaveGameData? SaveGame;
        }

        public SaveDataTransfer(Guid host, SaveGameData saveGame, string sceneName, string sceneMarker)
        {
            Data = new OnlineSave()
            {
                HostHash = GetHostHash(host),
                SceneName = sceneName,
                SceneMarker = sceneMarker,
                SaveGame = saveGame
            };
        }
        public OnlineSave? Data;

        public static int GetHostHash(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();

            return BitConverter.ToInt32(bytes, 0)
                 ^ BitConverter.ToInt32(bytes, 4)
                 ^ BitConverter.ToInt32(bytes, 8)
                 ^ BitConverter.ToInt32(bytes, 12);
        }


        public override object Fetch(params object[] args)
        {
            return Data!;
        }

        public override void Completed(List<byte[]> unpacked, NetworkConnection connection)
        {
            Data = ChunkedTransfer.Unpack<OnlineSave>(unpacked);
            if (Data == null)
                return;
            if(ModMain.Config.UseMultiplayerSaving)
                NetworkUtils.LocalClient!.SaveGame = LocalSaveManager.SaveExists(Data.HostHash) ? LocalSaveManager.ReadFromFile(LocalSaveManager.GetSavePath(Data.HostHash))! : new MultiplayerSaveGameData(Data.SaveGame!);
            else
                NetworkUtils.LocalClient!.SaveGame = new MultiplayerSaveGameData(Data.SaveGame!);
            //TransactionManager.Promise<bool>(Data.HostHash, true);

            GameManager.instance.LoadGameFromUI(Data.HostHash, NetworkUtils.LocalClient.SaveGame.Data);
        }
    }
}
