using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Impl;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Behaviours
{
    // mostly from https://github.com/nek5s/SilklessCoop/blob/master/Components/GameSync.cs (with permission)
    public class HornetMirror : MonoBehaviour
    {
        private string _id => NetworkUtils.LocalClient!.ClientID.ToString();

        // sprite sync - self
        private GameObject? _hornetObject = null;
        private tk2dSprite? _hornetSprite = null;
        private Rigidbody2D? _hornetRigidbody = null;
        private Dictionary<string, tk2dSpriteCollectionData> _collectionCache = new Dictionary<string, tk2dSpriteCollectionData>();

        // sprite sync - others
        private Dictionary<string, GameObject> _playerObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, tk2dSprite> _playerSprites = new Dictionary<string, tk2dSprite>();
        private Dictionary<string, SimpleInterpolator> _playerInterpolators = new Dictionary<string, SimpleInterpolator>();

        // map sync - self
        private GameObject? _map = null;
        private GameMap? _gameMap = null;
        private GameObject? _mainQuests = null;
        private GameObject? _compass = null;

        // map sync - others
        private Dictionary<string, GameObject> _playerCompasses = new Dictionary<string, GameObject>();
        private Dictionary<string, tk2dSprite> _playerCompassSprites = new Dictionary<string, tk2dSprite>();

        // player count
        private Dictionary<string, float> _lastSeen = new Dictionary<string, float>();
        private Dictionary<string, GameObject> _playerCountPins = new Dictionary<string, GameObject>();


        private void Start()
        {
            NetworkUtils.LocalConnection!.PacketHandler.Subscribe(new UpdateWeaverPacket().PacketName, (packet, connection) => OnHornetPositionPacket((UpdateWeaverPacket)packet));
            NetworkUtils.LocalConnection!.PacketHandler.Subscribe(new HornetAnimationPacket().PacketName, (packet, connection) => OnHornetAnimationPacket((HornetAnimationPacket)packet));
            //_network.AddHandler<PacketTypes.HornetAnimationPacket>(OnHornetAnimationPacket);
            NetworkUtils.LocalConnection!.PacketHandler.Subscribe(new CompassPositionPacket().PacketName, (packet, connection) => OnCompassPositionPacket((CompassPositionPacket)packet));
        }

        private void Update()
        {
            try
            {
                // setup references
                if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet");
                if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet(Clone)");
                if (_hornetObject && !_hornetRigidbody) _hornetRigidbody = _hornetObject.GetComponent<Rigidbody2D>();
                if (_hornetObject && !_hornetSprite) _hornetSprite = _hornetObject.GetComponent<tk2dSprite>();

                if (_hornetSprite && _collectionCache.Count == 0)
                    foreach (tk2dSpriteCollectionData c in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>())
                        _collectionCache[c.spriteCollectionGUID] = c;

                if (!_map) _map = GameObject.Find("Game_Map_Hornet");
                if (!_map) _map = GameObject.Find("Game_Map_Hornet(Clone)");
                if (_map && !_mainQuests) _mainQuests = _map.transform.Find("Main Quest Pins")?.gameObject;
                if (_map && !_compass) _compass = _map.transform.Find("Compass Icon")?.gameObject;
                if (_map && !_gameMap) _gameMap = _map.GetComponent<GameMap>();
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        private void UpdateUI()
        {
            try
            {
                if (_compass)
                {
                    int i = 0;

                    foreach (string id in _lastSeen.Keys)
                    {
                        if (!_playerCountPins.TryGetValue(id, out GameObject pin) || !pin)
                        {
                            GameObject newObject = Instantiate(_compass, _map!.transform);
                            newObject.SetActive(_mainQuests!.activeSelf);
                            newObject.SetName("SilklessPlayerCount");
                            newObject.transform.position = new Vector3(-14.8f + 0.6f * (i++), -8.2f, 0);
                            newObject.transform.localScale = new Vector3(0.6f, 0.6f, 1);
                            _playerCountPins[id] = newObject;
                            continue;
                        }

                        pin.SetActive(_mainQuests!.activeSelf);
                        pin.transform.position = new Vector3(-14.8f + 0.6f * (i++), -8.2f, 0);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }
        public void RemovePlayer(string id)
        {
            try
            {
                Logger.Msg($"Removing player {id}...");

                if (_playerObjects.TryGetValue(id, out GameObject g1)) Destroy(g1);
                if (_playerCompasses.TryGetValue(id, out GameObject g2)) Destroy(g2);
                if (_playerCountPins.TryGetValue(id, out GameObject g3)) Destroy(g3);

                _lastSeen.Remove(id);

                Logger.Msg($"Player {id} removed.", true);
            }
            catch (Exception e)
            {
                Logger.Msg(e.ToString());
            }
        }
        public void HeroSyncTick(HeroController hero)
        {
            if (hero == null)
            {
                return;
            }


            try
            {
                Logger.Msg(!NetworkUtils.IsConnected || NetworkUtils.LocalClient == null || NetworkUtils.LocalConnection == null);
                if (!NetworkUtils.IsConnected || NetworkUtils.LocalClient == null || NetworkUtils.LocalConnection == null) return;

                // timeouts
                _lastSeen["self"] = Time.unscaledTime;
                foreach (string id in _lastSeen.ToDictionary(e => e.Key, e => e.Value).Keys)
                    if (_lastSeen[id] < Time.unscaledTime - SilkConstants.CONNECTION_TIMEOUT)
                        RemovePlayer(id);

                SendHornetPositionPacket();
                SendHornetAnimationPacket();
                if (SilkConstants.SYNC_COMPASS) SendCompassPositionPacket();

                UpdateUI();
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }
        private void SendHornetPositionPacket()
        {
            try
            {
                Logger.Msg(!_hornetObject || !_hornetRigidbody);
                if (!_hornetObject || !_hornetRigidbody) return;

                NetworkUtils.LocalConnection!.Send(new UpdateWeaverPacket
                {
                    id = _id,
                    scene = SceneManager.GetActiveScene().name,
                    posX = _hornetObject.transform.position.x,
                    posY = _hornetObject.transform.position.y,
                    scaleX = _hornetObject.transform.localScale.x,
                    vX = _hornetRigidbody.linearVelocity.x * Time.timeScale,
                    vY = _hornetRigidbody.linearVelocity.y * Time.timeScale,
                });

                Logger.Debug("Sent position");
            }
            catch (Exception e)
            {
                Logger.Msg(e.ToString());
            }
        }
        private void OnHornetPositionPacket(UpdateWeaverPacket packet)
        {
            Logger.Debug("Got hornet position packet");
            try
            {
                _lastSeen[packet.id] = Time.unscaledTime;

                if (!_hornetObject) return;

                if (!_playerObjects.TryGetValue(packet.id, out GameObject playerObject) || !playerObject)
                {
                    // create player
                    Logger.Debug($"Creating new player object for player {packet.id}...");

                    GameObject newObject = new GameObject();
                    newObject.SetName($"SilklessCooperator - {packet.id}");
                    newObject.transform.SetParent(transform);
                    newObject.transform.position = new Vector3(packet.posX, packet.posY, _hornetObject.transform.position.z + 0.001f);
                    newObject.transform.localScale = new Vector3(packet.scaleX, 1, 1);

                    tk2dSprite newSprite = tk2dSprite.AddComponent(newObject, _hornetSprite.Collection, _hornetSprite.spriteId);
                    newSprite.color = new Color(1, 1, 1, 1);

                    SimpleInterpolator newInterpolator = newObject.AddComponent<SimpleInterpolator>();
                    newInterpolator.velocity = new Vector3(packet.vX, packet.vY, 0);

                    _playerObjects[packet.id] = newObject;
                    _playerSprites[packet.id] = newSprite;
                    _playerInterpolators[packet.id] = newInterpolator;

                    Logger.Debug($"Created new player object for player {packet.id}.");
                }
                else
                {
                    if (!_playerInterpolators.TryGetValue(packet.id, out SimpleInterpolator playerInterpolator)) return;

                    // update player
                    playerObject.transform.position = new Vector3(packet.posX, packet.posY, _hornetObject.transform.position.z + 0.001f);
                    playerObject.transform.localScale = new Vector3(packet.scaleX, 1, 1);
                    playerObject.SetActive(packet.scene == SceneManager.GetActiveScene().name);
                    playerInterpolator.velocity = new Vector3(packet.vX, packet.vY, 0);

                    Logger.Debug($"Updated position of player {packet.id} to {packet.scene}/({packet.posX} {packet.posY})");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        private void SendHornetAnimationPacket()
        {
            try
            {
                if (!_hornetSprite) return;

                NetworkUtils.LocalConnection!.Send(new HornetAnimationPacket
                {
                    id = _id,
                    collectionGuid = _hornetSprite.Collection.spriteCollectionGUID,
                    spriteId = _hornetSprite.spriteId,
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }
        private void OnHornetAnimationPacket(HornetAnimationPacket packet)
        {
            try
            {
                _lastSeen[packet.id] = Time.unscaledTime;

                if (!_hornetObject) return;
                if (!_playerSprites.TryGetValue(packet.id, out tk2dSprite playerSprite) || !playerSprite) return;
                if (!_collectionCache.TryGetValue(packet.collectionGuid, out tk2dSpriteCollectionData collectionData) || !collectionData) return;

                playerSprite.Collection = collectionData;
                playerSprite.spriteId = packet.spriteId;

                Logger.Debug($"Set sprite for player {packet.id} to {packet.collectionGuid}/{packet.spriteId}");
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        private void SendCompassPositionPacket()
        {
            try
            {
                if (!_map || !_gameMap || !_compass) return;

                _gameMap.PositionCompassAndCorpse();

                NetworkUtils.LocalConnection!.Send(new CompassPositionPacket
                {
                    id = _id,
                    active = _compass.activeSelf,
                    posX = _compass.transform.localPosition.x,
                    posY = _compass.transform.localPosition.y,
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }
        private void OnCompassPositionPacket(CompassPositionPacket packet)
        {
            try
            {
                _lastSeen[packet.id] = Time.unscaledTime;

                if (!_map || !_compass || !_mainQuests) return;

                if (!_playerCompasses.TryGetValue(packet.id, out GameObject playerCompass) || !playerCompass)
                {
                    // create compass
                    Logger.Debug($"Creating new compass object for player {packet.id}...");

                    GameObject newObject = Instantiate(_compass, _map.transform);
                    newObject.SetActive(packet.active);
                    newObject.SetName($"SilklessCompass - {packet.id}");
                    newObject.transform.localPosition = new Vector2(packet.posX, packet.posY);

                    tk2dSprite newSprite = newObject.GetComponent<tk2dSprite>();
                    newSprite.color = new Color(1, 1, 1, 0.5f);

                    _playerCompasses[packet.id] = newObject;
                    _playerCompassSprites[packet.id] = newSprite;

                    Logger.Debug($"Created new player object for player {packet.id}.");
                }
                else
                {
                    if (!_playerCompassSprites.TryGetValue(packet.id, out tk2dSprite compassSprite) || !compassSprite) return;

                    // update compass
                    playerCompass.transform.localPosition = new Vector2(packet.posX, packet.posY);
                    playerCompass.SetActive(packet.active);

                    Logger.Debug($"Updated position of compass {packet.id} to ({packet.posX} {packet.posY}) active={packet.active}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }
    }
}
