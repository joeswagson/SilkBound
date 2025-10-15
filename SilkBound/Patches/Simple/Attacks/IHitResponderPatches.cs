using HarmonyLib;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Types.JsonConverters;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamCherry.Localization;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Patches.Simple.Attacks
{
    public class IHitResponderPatches
    {
        public static bool HitPrefix(IHitResponder __instance, [HarmonyArgument(0)] HitInstance hitInstance, ref IHitResponder.HitResponse __result)
        {
            //Logger.Msg(NetworkUtils.LocalAuthority.ToString());
            if (!NetworkUtils.IsConnected || NetworkUtils.IsPacketThread()) return true;
            //var instance = (MonoBehaviour)__instance;
            //if (hitInstance.RepresentingTool != null)
            //{
            //    hitInstance.RepresentingTool = new SerializedTool(hitInstance.RepresentingTool.name, hitInstance.RepresentingTool.Type);
            //}

            //hitInstance.NailImbuement = null;
            //hitInstance.SlashEffectOverrides = null;
            //Logger.Msg("hitinstance:", instance.GetType().FullName, ChunkedTransfer.Serialize(hitInstance, new GameObjectConverter()).Length);
            Logger.Msg("hi");
            //Logger.Msg("source:", ChunkedTransfer.Deserialize<HitInstance>(ChunkedTransfer.Serialize(hitInstance, new GameObjectConverter(), new ToolItemConverter()), new GameObjectConverter(false), new ToolItemConverter()).Source);
            NetworkUtils.SendPacket(new SyncHitPacket(hitInstance, __instance));
            return true;
        }
    }
}
