using Mono.Remoting.Channels.Unix;
using SilkBound.Managers;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkBound.Lib.DbgRender.Renderers {
    #region Status Objects
    public struct ConnectionStatus(string body, Color color) {
        private static GUIContent StatusLabel = new GUIContent("Status: ");
        #region Impl
        static readonly Color _stall = new(0.5f, 0.5f, 0.5f);
        static readonly Color _waiting = new(0.7f, 0.7f, 0.7f);
        static readonly Color _orange = new(1f, 0.75f, 0f);

        public static ConnectionStatus NotReady = new("N/A", _stall);
        public static ConnectionStatus Waiting = new("Waiting", _waiting);
        public static ConnectionStatus Connected = new("Connected", Color.green);
        public static ConnectionStatus Connecting = new("Connecting...", _orange);
        public static ConnectionStatus Disconnected = new("Disconnected", Color.red);
        #endregion

        public readonly string Body => body;
        public Color Color = color;

        public readonly void Render(Renderer renderer, Rect pos)
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
    internal class ConnectionMenuRenderer() : Renderer(DrawAnchor.BottomLeft) {
        private static readonly List<ConnectionMenuRenderer> Active = [];
        public override void Created() => Active.Add(this);
        public override void Dispose() => Active.Remove(this);

        public static bool GlobalReady;
        public static void ConnectReady()
        {
            GlobalReady = true;
            Active.ForEach(r => {
                r.SetStatus(ConnectionStatus.Waiting);
                r.IsReady = true;
            });
        }

        public void SetStatus(ConnectionStatus status)
        {
            _status = status;
        }
        private void RenderStatus()
        {
            CurrentStatus.Render(this, CursorToScreen());
        }
        private readonly GUIContent HostHeader = new GUIContent("Host from:");
        //private readonly GUIContent HostText = new GUIContent("Host");
        private readonly GUIContent ConnectHeader = new GUIContent("Connect to:");
        //private readonly GUIContent ConnectText = new GUIContent("Connect");
        private readonly Color bgColor = new Color(0.5f, 0.5f, 1f, 0.75f);
        private readonly Color frameBgColor = new Color(0f, 0f, 0.03f, 0.75f);
        protected override void ApplySettings(bool init)
        {
            GUI.backgroundColor = bgColor;
            GUI.skin.textField.fixedHeight = 20;

            if(init && GlobalReady)
            {
                SetStatus(ConnectionStatus.Waiting);
                IsReady = true;
            }
        }
        const float WIDTH = 200;
        const float HEIGHT = 170;
        const float MARGIN = 5f;

        bool connecting = false;

        ConnectionStatus _status = ConnectionStatus.NotReady;
        public ConnectionStatus CurrentStatus => _status;
        public bool IsReady = false;
        public override void Draw()
        {
            SetCursorReference(DrawBox(WIDTH, HEIGHT, frameBgColor, 5)); // draw and center cursor around frame
            ElementBuffer(ElementWidth - 2 * MARGIN, 20); // prevent overlap
            X(MARGIN); // left side margin

            Y(MARGIN); // top margin
            RenderStatus();

            GUI.Label(Scroll(ElementHeight + MARGIN), HostHeader); // host label
            Silkbound.Config.HostIP = GUI.TextField(Scroll(ElementHeight), Silkbound.Config.HostIP, 15); // host textfield

            GUI.Label(Scroll(ElementHeight + MARGIN), ConnectHeader); // connect label
            Silkbound.Config.ConnectIP = GUI.TextField(Scroll(ElementHeight), Silkbound.Config.ConnectIP, 20); // connect textfield

            ElementBuffer((WIDTH - 3 * MARGIN) / 2);
            if (GUI.Button(Scroll(ElementHeight + MARGIN), IsReady ? "Host" : "Not Ready") && IsReady && !connecting)
            {
                Logger.Debug("host");
                connecting = true;
                SetStatus(ConnectionStatus.Connecting);

                var host = Silkbound.Config.HostIP;
                int? port = null;
                if (host.Contains(':') && ushort.TryParse(host.Split(":")[1], out ushort ushort_port))
                    port = ushort_port;

                ConnectionManager.Server(ip: host, port: port).ContinueWith(t => t.Result.Dump());
            }

            Slide(ElementWidth + MARGIN);
            if (GUI.Button(CursorToScreen(), IsReady ? "Connect" : "Not Ready") && IsReady && !connecting)
            {
                Logger.Debug("join");
                connecting = true;
                SetStatus(ConnectionStatus.Connecting);

                var connect = Silkbound.Config.ConnectIP;
                int? port = null;
                if (connect.Contains(':') && ushort.TryParse(connect.Split(":")[1], out ushort ushort_port))
                    port = ushort_port;

                Task<ConnectionRequest> t = ConnectionManager.Client(ip: connect, port: port);
                t.ContinueWith(t => t.Result.Dump());
                Task.Run(() => {
                    t.Wait();
                    if (t.IsFaulted)
                        Logger.Error(t.Exception);
                });
            }

            X(MARGIN);
            ElementBuffer(WIDTH - 2 * MARGIN, 20);
            if (GUI.Button(Scroll(ElementHeight + MARGIN), "Disconnect") && NetworkUtils.Connected)
            {
                Logger.Debug("disconnect");
                connecting = false;
                NetworkUtils.Disconnect("Leaving.");
                SetStatus(ConnectionStatus.Disconnected);
            }
        }
    }
}
