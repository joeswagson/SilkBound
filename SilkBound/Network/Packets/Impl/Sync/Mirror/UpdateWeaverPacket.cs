using GlobalEnums;
using SilkBound.Types.Data;
using SilkBound.Utils;
using System;
using System.IO;
using static SilkBound.Network.Packets.Schema.PacketSchemas;

namespace SilkBound.Network.Packets.Impl.Mirror {

    public class UpdateWeaverPacket(
        string? scene,
        float? posX,
        float? posY,
        float? scaleX,
        float? vX,
        float? vY
    ) : Packet {
        public string? Scene => scene;
        public float? PosX => posX;
        public float? PosY => posY;
        public float? ScaleX => scaleX;
        public float? VelocityX => vX;
        public float? VelocityY => vY;

        public override void Serialize(BinaryWriter writer)
        {
            if (Assertions.None(
                Scene,
                PosX,
                PosY,
                ScaleX,
                VelocityX,
                VelocityY))
            {
                Abrupt();
                return; // dont send empty updates
            }

            var optional = new OptionalValueSet<UpdateWeaver>();

            optional.Set(UpdateWeaver.Scene, new ComparableString(scene));
            optional.Set(UpdateWeaver.PosX, posX);
            optional.Set(UpdateWeaver.PosY, posY);
            optional.Set(UpdateWeaver.ScaleX, scaleX);
            optional.Set(UpdateWeaver.VelocityX, vX);
            optional.Set(UpdateWeaver.VelocityY, vY);

            optional.Write(writer, (flag, obj, w) => {
                if (obj is ComparableString cStr)
                {
                    if (cStr != null)
                        writer.Write(cStr);
                    return true;
                }
                return false;
            });
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            var optional = new OptionalValueSet<UpdateWeaver>(UpdateWeaverSchema);
            //optional.Read(reader, (flag, r) => {
            //    return flag switch {
            //        UpdateFlags.Scene => new ComparableString(r.ReadString()),
            //        _ => null!,
            //    };
            //});
            optional.Read(reader, (flag, r) => {
                switch (flag)
                {
                    case UpdateWeaver.Scene: return new ComparableString(r.ReadString());
                    default:
                        Logger.Warn("Unhandled flag!", flag.ToString());
                        return null!;
                }
            });

            string? scene = optional.Get<ComparableString>(UpdateWeaver.Scene);
            float? posX = optional.Get<float>(UpdateWeaver.PosX);
            float? posY = optional.Get<float>(UpdateWeaver.PosY);
            float? scaleX = optional.Get<float>(UpdateWeaver.ScaleX);
            float? vX = optional.Get<float>(UpdateWeaver.VelocityX);
            float? vY = optional.Get<float>(UpdateWeaver.VelocityY);

            Logger.Msg("read scene:", scene);
            Logger.Msg("read posX:", posX);
            Logger.Msg("read posY:", posY);
            Logger.Msg("read scaleX:", scaleX);
            Logger.Msg("read vX:", vX);
            Logger.Msg("read vY:", vY);

            return new UpdateWeaverPacket(scene, posX, posY, scaleX, vX, vY);
        }
    }
}
