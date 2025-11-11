using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Network.Packets.Schema {
    public class PacketSchemas {
        #region UpdateWeaverPacket
        [Flags]
        public enum UpdateWeaver : byte {
            Scene = 0,
            PosX = 1,
            PosY = 2,
            ScaleX = 3,
            VelocityX = 4,
            VelocityY = 5
        }

        public static readonly Dictionary<UpdateWeaver, TypeCode> UpdateWeaverSchema = new() {
            { UpdateWeaver.Scene, TypeCode.Object },
            { UpdateWeaver.PosX, TypeCode.Single },
            { UpdateWeaver.PosY, TypeCode.Single },
            { UpdateWeaver.ScaleX, TypeCode.Single },
            { UpdateWeaver.VelocityX, TypeCode.Single },
            { UpdateWeaver.VelocityY, TypeCode.Single },
       };
        #endregion
    }
}
