using HarmonyLib;
using SilkBound.Managers;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;

namespace SilkBound.Patches
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.LoadGameFromUI), [typeof(int), typeof(SaveGameData)])]
        public static bool LoadGameFromUI(GameManager __instance, int saveSlot, SaveGameData saveGameData)
        {
            if (!NetworkUtils.Connected || NetworkUtils.LocalConnection == null || NetworkUtils.IsPacketThread()) return true;

            TransferManager.Send(new SaveDataTransfer(
                NetworkUtils.IsServer ? NetworkUtils.LocalClient.ClientID : Server.CurrentServer.Host!.ClientID,
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

            return Server.CurrentServer.Settings.LoadGamePermission != Network.Packets.AuthorityNode.Server || NetworkUtils.IsServer;
        }
    }
}
