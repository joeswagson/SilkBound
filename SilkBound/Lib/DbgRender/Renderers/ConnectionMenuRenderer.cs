using Mono.Remoting.Channels.Unix;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Lib.DbgRender.Renderers {
    internal class ConnectionMenuRenderer() : Renderer(DrawAnchor.BottomLeft) {
        #region Status Objects
        public struct Status(string body, Color color)
        {
            private static GUIContent StatusLabel = new GUIContent("Status: ");

            #region Impl
            static readonly Color _stall = new(0.5f, 0.5f, 0.5f);
            static readonly Color _waiting = new(0.7f, 0.7f, 0.7f);
            static readonly Color _orange = new(1f, 0.75f, 0f);

            public static Status NotReady = new("N/A", _stall);
            public static Status Waiting = new("Waiting", _waiting);
            public static Status Connected = new("Connected", Color.green);
            public static Status Connecting = new("Connecting...", _orange);
            public static Status Disconnected = new("Disconnected", Color.red);
            #endregion

            public string Body = body;
            public Color Color = color;

            public void Render(Renderer renderer, Rect pos)
            {
                var slideFactor = GUI.skin.label.CalcSize(StatusLabel).x; // how far the cursor needs to slide to render the body accurately
                GUI.Label(pos, StatusLabel); // "Status:"

                var prevColor = GUI.color;
                GUI.color = Color;

                GUI.Label(renderer.Slide(slideFactor), Body); // status body
                GUI.color = prevColor;

                renderer.Slide(-slideFactor); // reset cursor x offset
            }
        }
        #endregion
        private void SetStatus(Status status)
        {
            _status = status;
        }
        private void RenderStatus()
        {
            CurrentStatus.Render(this, CursorToScreen());
        }
        private readonly GUIContent HostHeader = new GUIContent("Host from:");
        private readonly GUIContent HostText = new GUIContent("Host");
        private readonly GUIContent ConnectHeader = new GUIContent("Connect to:");
        private readonly GUIContent ConnectText = new GUIContent("Connect");
        private readonly Color bgColor = new Color(0.5f, 0.5f, 1f, 0.75f);
        private readonly Color frameBgColor = new Color(0f, 0f, 0.03f, 0.75f);
        protected override void ApplySettings(bool init)
        {
            GUI.backgroundColor = bgColor;
            GUI.skin.textField.fixedHeight = 20;

            if (init)
                SetStatus(Status.Waiting);
        }
        const float WIDTH = 200;
        const float HEIGHT = 145;
        const float MARGIN = 5f;

        bool connecting = false;
        
        Status _status = Status.NotReady;
        public Status CurrentStatus => _status;
        public override void Draw(DrawAnchor origin)
        {
            SetCursorReference(DrawBox(WIDTH, HEIGHT, frameBgColor, 5)); // draw and center cursor around frame
            ElementBuffer(ElementWidth - 2 * MARGIN, 20); // prevent overlap
            Slide(MARGIN); // left side margin

            Scroll(MARGIN); // top margin
            RenderStatus();

            GUI.Label(Scroll(ElementHeight + MARGIN), HostHeader); // host label
            ModMain.Config.HostIP = GUI.TextField(Scroll(ElementHeight), ModMain.Config.HostIP, 15); // host textfield

            GUI.Label(Scroll(ElementHeight + MARGIN), ConnectHeader); // connect label
            ModMain.Config.ConnectIP = GUI.TextField(Scroll(ElementHeight), ModMain.Config.ConnectIP, 15); // connect textfield

            ElementBuffer((WIDTH - 3 * MARGIN) / 2);
            if (GUI.Button(Scroll(ElementHeight + MARGIN), HostText) && !connecting)
            {
                Logger.Msg("host");
                connecting = true;
                SetStatus(Status.Connecting);


            }

            Slide(ElementWidth + MARGIN);
            if (GUI.Button(CursorToScreen(), ConnectText) && !connecting)
            {
                Logger.Msg("join");
                connecting = true;
                SetStatus(Status.Connecting);
            }
        }
    }
}
