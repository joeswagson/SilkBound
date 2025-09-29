using HarmonyLib;
using Newtonsoft.Json;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Patches
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.LoadGameFromUI), new Type[] { typeof(int), typeof(SaveGameData) })]
        public static bool LoadGameFromUI(GameManager __instance, int saveSlot, SaveGameData saveGameData)
        {
            if (NetworkUtils.IsConnected && NetworkUtils.LocalConnection != null && TransactionManager.Fetch<bool>(saveSlot) == false)
            {
                TransferManager.Send(new SaveDataTransfer(
                    NetworkUtils.IsServer ? NetworkUtils.LocalClient!.ClientID : Server.CurrentServer!.Host!.ClientID,
                    saveGameData,
                    !string.IsNullOrEmpty(saveGameData.playerData.tempRespawnScene) ? saveGameData.playerData.tempRespawnScene : saveGameData.playerData.respawnScene,
                    !string.IsNullOrEmpty(saveGameData.playerData.tempRespawnMarker) ? saveGameData.playerData.tempRespawnMarker : saveGameData.playerData.respawnMarkerName
                ));

                //Guid transferId = Guid.NewGuid();
                //List<byte[]> chunks = ChunkedTransfer.Pack<SaveDataTransfer>(transfer);
                //for (int i = 0; i < chunks.Count; i++)
                //{
                //    byte[] chunk = chunks[i];
                //    Logger.Msg("sending chunk", i + 1, "of", chunks.Count);
                //    NetworkUtils.LocalConnection!.Send(new TransferDataPacket(chunk, i, chunks.Count, transferId));
                //}

                return NetworkUtils.IsServer;
            }

            return true;
        }
    }
}
