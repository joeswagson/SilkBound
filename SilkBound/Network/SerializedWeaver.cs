using SilkBound.Managers;
using SilkBound.Types;
using System;

namespace SilkBound.Network
{
    public struct SerializedWeaver
    {
        public Guid ClientID;
        public string ClientName;
        public string AppliedSkin;

        public static SerializedWeaver FromWeaver(Weaver weaver)
        {
            return new SerializedWeaver
            {
                ClientID = weaver.ClientID,
                ClientName = weaver.ClientName,
                AppliedSkin = weaver.AppliedSkin.SkinName
            };
        }

        public Weaver ToWeaver()
        {
            var skin = SkinManager.GetOrDefault(AppliedSkin);
            return Server.CurrentServer.GetWeaver(ClientID)!;
        }
    }
}
