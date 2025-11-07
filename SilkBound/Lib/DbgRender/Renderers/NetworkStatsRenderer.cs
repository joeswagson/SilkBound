using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Lib.DbgRender.Renderers {
    public class NetworkStatsRenderer(DrawAnchor? origin = null) : Renderer(origin) {
        private static readonly List<NetworkStatsRenderer> Active = [];
        public static void ObjectSafeAssignAll(NetworkConnection connection) => Active.ForEach(r => r.SetConnection(connection));

        private readonly Color bgColor = new Color(0.03f, 0f, 0f, 0.75f);

        const float WIDTH = 200;
        const float HEIGHT = 355;
        const float MARGIN = 5f;

        private NetworkStats? target;
        public void SetConnection(NetworkConnection connection) => target = connection.Stats;
        public override void Created() => Active.Add(this);
        public override void Dispose() => Active.Remove(this);
        public override void Draw()
        {
            SetCursorReference(DrawBox(WIDTH, target == null ? 25 : HEIGHT, bgColor, 5));
            ElementBuffer(ElementWidth - 2 * MARGIN, 20);
            Slide(MARGIN);

            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.Label(Scroll(MARGIN), "Network Stats");
            GUI.skin.label.fontStyle = FontStyle.Normal;

            if (target == null)
                return;

            var data = target.Data;

            GUI.Label(Scroll(ElementHeight + MARGIN), $"Packets Sent: {data.PacketsSent}");
            GUI.Label(Scroll(ElementHeight + MARGIN), $"Packets Recieved: {data.PacketsRead}");

            GUI.Label(Scroll(ElementHeight + MARGIN), $"Bytes Sent: {data.BytesSent}");
            GUI.Label(Scroll(ElementHeight + MARGIN), $"Bytes Recieved: {data.BytesRead}");

            DrawVoid(margin: MARGIN);

            GUI.Label(Scroll(ElementHeight + MARGIN), $"Sent Packets Dropped: {data.PacketsSentDropped}");
            GUI.Label(Scroll(ElementHeight + MARGIN), $"Recieved Packets Dropped: {data.PacketsReadDropped}");

            DrawVoid(margin: MARGIN);

            GUI.Label(Scroll(ElementHeight + MARGIN), $"Packet Send Rate: {NetworkData.FormatMetric(data.PacketsSentPerSecond)}");
            GUI.Label(Scroll(ElementHeight + MARGIN), $"Packet Recieve Rate: {NetworkData.FormatMetric(data.PacketsReadPerSecond)}");

            DrawVoid(margin: MARGIN);

            GUI.Label(Scroll(ElementHeight + MARGIN), $"Byte Send Rate: {NetworkData.FormatMetric(data.BytesSentPerSecond / 1024f, "KB")}");
            GUI.Label(Scroll(ElementHeight + MARGIN), $"Byte Recieve Rate: {NetworkData.FormatMetric(data.BytesReadPerSecond / 1024f, "KB")}");
        }
    }
}
