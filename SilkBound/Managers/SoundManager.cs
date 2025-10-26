using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Utils;
using UnityEngine.Audio;

namespace SilkBound.Managers
{
    public class SoundManager
    {
        public static Dictionary<string, Sound> Sounds { get; } = new Dictionary<string, Sound>()
        {
            { "shaw", new Sound(ResourceManager.LoadEmbedded(ResourceManager.SilkResolve("Sounds", "shaw.wav"))) }
        };
        public class Sound
        {
            protected AudioClip? clip = null;

            protected event Action? loaded;
            public bool Playing => Source.isPlaying;
            public Sound(string path)
            {
                Task.Run(async () =>
                {
                    clip = await LoadClip(path);
                    loaded?.Invoke();
                });
            }
            public Sound(byte[] data)
            {
                clip = WavImport.LoadFromBytes(data);
            }

            AudioSource? _source = null;
            static AudioMixerGroup? _audioMixerGroup = null;
            static AudioMixerGroup? audioMixerGroup
            {
                get
                {
                    return _audioMixerGroup ??= HeroController.instance?.AudioCtrl.jump.outputAudioMixerGroup;
                }
            }
            AudioSource Source
            {
                get
                {
                    if (_source == null)
                    {
                        _source = new GameObject("AudioSource").AddComponent<AudioSource>();
                        AudioSourcePriority prio = _source.gameObject.AddComponent<AudioSourcePriority>();
                        prio.sourceType = AudioSourcePriority.SourceType.Hero;
                        prio.UpdatePriority();
                        _source.outputAudioMixerGroup = audioMixerGroup;
                        _source.spatialBlend = 1f;
                        _source.playOnAwake = false;
                        GameObject.DontDestroyOnLoad(_source.gameObject);
                    }
                    return _source;
                }
            }
            public void Play(Vector3 position, float mindist = 30f, float maxdist = 45f, float volume = 1f)
            {
                if (clip == null)
                {
                    Logger.Warn("Sound clip not loaded yet. Enqueing.");
                    loaded = () => Play(position, mindist, maxdist, volume);
                    return;
                }

                Source.minDistance = mindist;
                Source.maxDistance = maxdist;
                Source.rolloffMode = AudioRolloffMode.Linear;
                Source.volume = volume;
                Source.transform.position = position;

                Logger.Msg($"Playing sound {clip.name} at {position}, {mindist}->{maxdist}, {volume:P1}");
                Source.clip = clip;
                Source.Play();
            }


            static async Task<AudioClip?> LoadClip(string path)
            {
                using UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);
                await uwr.SendWebRequest();
                await Task.Delay(100);
                try {

                    if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                        Debug.Log($"{uwr.error}");
                    else
                        return DownloadHandlerAudioClip.GetContent(uwr);
                } catch (Exception err) {
                    Debug.Log($"{err.Message}, {err.StackTrace}");
                }

                return null;
            }
        }

        public class WavImport
        {
            public static AudioClip LoadFromBytes(byte[] data, string clipName = "AudioClip")
            {
                int channels = BitConverter.ToUInt16(data, 22);
                int sampleRate = BitConverter.ToInt32(data, 24);
                ushort bitDepth = BitConverter.ToUInt16(data, 34);

                int subChunk1Size = BitConverter.ToInt32(data, 16);
                int dataOffset = 16 + 4 + subChunk1Size + 4;
                int dataLength = BitConverter.ToInt32(data, dataOffset);

                float[] samples = bitDepth switch
                {
                    8 => Extract8Bit(data, dataOffset, dataLength),
                    16 => Extract16Bit(data, dataOffset, dataLength),
                    24 => Extract24Bit(data, dataOffset, dataLength),
                    32 => Extract32Bit(data, dataOffset, dataLength),
                    _ => throw new NotSupportedException($"{bitDepth}-bit WAV not supported")
                };

                AudioClip clip = AudioClip.Create(clipName, samples.Length / channels, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }

            private static float[] Extract8Bit(byte[] data, int offset, int length)
            {
                float[] samples = new float[length];
                for (int i = 0; i < length; i++)
                    samples[i] = (data[offset + i] - 128) / 128f;
                return samples;
            }

            private static float[] Extract16Bit(byte[] data, int offset, int length)
            {
                int count = length / 2;
                float[] samples = new float[count];
                for (int i = 0; i < count; i++)
                    samples[i] = BitConverter.ToInt16(data, offset + i * 2) / 32768f;
                return samples;
            }

            private static float[] Extract24Bit(byte[] data, int offset, int length)
            {
                int count = length / 3;
                float[] samples = new float[count];
                byte[] buffer = new byte[4];

                for (int i = 0; i < count; i++)
                {
                    Array.Clear(buffer, 0, 4);
                    Array.Copy(data, offset + i * 3, buffer, 1, 3);
                    samples[i] = BitConverter.ToInt32(buffer, 0) / (float)Int32.MaxValue;
                }

                return samples;
            }

            private static float[] Extract32Bit(byte[] data, int offset, int length)
            {
                int count = length / 4;
                float[] samples = new float[count];
                for (int i = 0; i < count; i++)
                    samples[i] = BitConverter.ToInt32(data, offset + i * 4) / (float)Int32.MaxValue;
                return samples;
            }
        }

        public static bool Play(PlaySoundPacket packet, Vector3? position = null, float? mindist = null, float? maxdist = null, float? volume = null)
        {
            return Play(packet.SoundName, position ?? packet.Position, mindist ?? packet.MinDistance, maxdist ?? packet.MaxDistance, volume ?? packet.Volume);
        }
        public static bool PlayNetworked(PlaySoundPacket packet, Vector3? position = null, float? mindist = null, float? maxdist = null, float? volume = null)
        {
            NetworkUtils.LocalConnection?.Send(packet);

            return Play(packet.SoundName, position ?? packet.Position, mindist ?? packet.MinDistance, maxdist ?? packet.MaxDistance, volume ?? packet.Volume);
        }

        public static bool Play(string name, Vector3? position = null, float? mindist = null, float? maxdist = null, float? volume = null)
        {
            if (Sounds.TryGetValue(name, out Sound sound))
            {
                Play(sound, position, mindist, maxdist, volume);

                return true;
            }

            return false;
        }

        public static void Play(Sound sound, Vector3? position = null, float? mindist = null, float? maxdist = null, float? volume = null)
        {
            sound.Play(position ?? Vector3.zero, mindist ?? 30f, maxdist ?? 45f, volume ?? 1f);
        }

        public static Sound? GetSound(string name)
        {
            if (Sounds.ContainsKey(name))
            {
                return Sounds[name];
            }
            return null;
        }

        public static void LoadSound(string name, Sound sound)
        {
            if (!Sounds.ContainsKey(name))
            {
                Sounds.Add(name, sound);
            }
        }

        public static void UnloadSound(string name)
        {
            if (Sounds.ContainsKey(name))
            {
                Sounds.Remove(name);
            }
        }

        public static void UnloadAllSounds()
        {
            Sounds.Clear();
        }
    }
}
