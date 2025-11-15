using SilkBound.Managers;
using SilkBound.Network.NetworkLayers;
using SilkBound.Network.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SilkBound.Utils {
    public struct NetworkData {
        public void Reset() => this = default;

        #region Persistent counters
        public uint PacketsSent;
        public uint PacketsRead;
        public uint PacketsSentDropped;
        public uint PacketsReadDropped;

        public uint BytesSent;
        public uint BytesRead;

        public uint BytesSentDropped;
        public uint BytesReadDropped;

        public uint Endpoints;
        #endregion

        #region Derived
        public readonly uint PacketsDroppedTotal => PacketsReadDropped + PacketsSentDropped;
        #endregion

        #region Windowed rates
        public float PacketsSentPerSecond;
        public float PacketsReadPerSecond;
        public float BytesSentPerSecond;
        public float BytesReadPerSecond;
        #endregion

        public static string FormatMetric(float quantity, string unit = "", bool spaced = false)
            => $"{quantity:F1}{(spaced ? " " : string.Empty)}{unit}/s";
    }

    public class NetworkStats {
        private const double WindowSeconds = 1.0;

        public NetworkConnection Connection { get; }
        public NetworkData _data;
        public NetworkData Data => _data;

        private readonly Queue<(double time, uint packets, uint bytes)> _sentHistory = new();
        private readonly Queue<(double time, uint packets, uint bytes)> _readHistory = new();

        private uint _lastPacketsSent;
        private uint _lastPacketsRead;
        private uint _lastBytesSent;
        private uint _lastBytesRead;

        public NetworkStats(NetworkConnection connection)
        {
            Connection = connection;
            TickManager.OnTick += OnTick;
        }

        ~NetworkStats()
        {
            TickManager.OnTick -= OnTick;
        }

        internal void LogPacketSentImmediate(byte[] data)
        {
            LogPacketSent(data);
        }
        internal void LogPacketSentFaulted(byte[] data)
        {
            LogPacketSentDropped(data);
        }

        internal void LogPacketSent(byte[] data)
        {
            _data.PacketsSent++;
            _data.BytesSent += (uint) data.Length;
        }

        internal void LogPacketSentDropped(byte[] data)
        {
            if(_data.PacketsSent > 0)
                _data.PacketsSent--;
            if(_data.BytesSent >= data.Length)
                _data.BytesSent -= (uint) data.Length;

            _data.PacketsSentDropped++;
            _data.BytesSentDropped += (uint) data.Length;
        }

        internal void LogBytesRead(byte[] data)
        {
            _data.BytesRead += (uint) data.Length + 4; // add back stripped frame size
        }

        internal void LogPacketRead(byte[] data, Packet? packet)
        {
            if (packet == null)
                LogPacketReadDropped(data);
            else
                _data.PacketsRead++;
        }

        internal void LogPacketReadDropped(byte[] data)
        {
            _data.PacketsReadDropped++;
            _data.BytesReadDropped += (uint) data.Length + 4; // add back stripped frame size
        }

        public void LogEndpoints(IEnumerable<NetworkConnection> connections)
        {
            _data.Endpoints = (uint) connections.Count();
        }

        private void OnTick(float dt)
        {

            double now = Stopwatch.GetTimestamp() / (double) Stopwatch.Frequency;

            uint dPacketsSent = Math.Max(0, _data.PacketsSent - _lastPacketsSent);
            uint dPacketsRead = Math.Max(0, _data.PacketsRead - _lastPacketsRead);
            uint dBytesSent = Math.Max(0, _data.BytesSent - _lastBytesSent);
            uint dBytesRead = Math.Max(0, _data.BytesRead - _lastBytesRead);

            if (dPacketsSent > 0 || dBytesSent > 0)
                _sentHistory.Enqueue((now, dPacketsSent, dBytesSent));

            if (dPacketsRead > 0 || dBytesRead > 0)
                _readHistory.Enqueue((now, dPacketsRead, dBytesRead));

            // prune anything older than the 1s window
            while (_sentHistory.Count > 0 && now - _sentHistory.Peek().time > WindowSeconds)
                _sentHistory.Dequeue();
            while (_readHistory.Count > 0 && now - _readHistory.Peek().time > WindowSeconds)
                _readHistory.Dequeue();

            // compute sums
            double sentSpan = _sentHistory.Count > 1 ? now - _sentHistory.Peek().time : WindowSeconds;
            double readSpan = _readHistory.Count > 1 ? now - _readHistory.Peek().time : WindowSeconds;

            uint sentPackets = (uint) _sentHistory.Sum(s => s.packets);
            uint readPackets = (uint) _readHistory.Sum(s => s.packets);
            uint sentBytes = (uint) _sentHistory.Sum(s => s.bytes);
            uint readBytes = (uint) _readHistory.Sum(s => s.bytes);

            _data.PacketsSentPerSecond = (float) (sentPackets / sentSpan);
            _data.PacketsReadPerSecond = (float) (readPackets / readSpan);
            _data.BytesSentPerSecond = (float) (sentBytes / sentSpan);
            _data.BytesReadPerSecond = (float) (readBytes / readSpan);

            _lastPacketsSent = _data.PacketsSent;
            _lastPacketsRead = _data.PacketsRead;
            _lastBytesSent = _data.BytesSent;
            _lastBytesRead = _data.BytesRead;
        }
    }
}
