using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Sync;
using SilkBound.Types;
using SilkBound.Types.Mirrors;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Behaviours
{
    public class HornetMirror() : GenericSync
    {
        public static tk2dSprite Sprite => HeroController.instance.GetComponent<tk2dSprite>()!;

        public bool IsLocal = true;
        public GameObject Root = null!;
        public SimpleInterpolator Interpolator = null!;
        public tk2dSprite MirrorSprite = null!;
        public tk2dSpriteAnimator MirrorAnimator = null!;
        public HeroControllerMirror MirrorController = null!;
        public float Layer = 0.004f;

        private static tk2dSpriteAnimator? _cachedLocal;
        public static tk2dSpriteAnimator ReferenceAnimator
        {
            get
            {
                if (_cachedLocal == null)
                    _cachedLocal = HeroController.instance.GetComponent<tk2dSpriteAnimator>();

                return _cachedLocal;
            }
        }

        public static HornetMirror AddComponent(GameObject go, HeroControllerMirror mirrorController, tk2dSprite mirrorSprite, tk2dSpriteAnimator mirrorAnimator, SimpleInterpolator interpolator, float layer, bool local)
        {
            HornetMirror mirror = go.AddComponent<HornetMirror>();
            mirror.IsLocal = local;
            mirror.Root = go;
            mirror.MirrorSprite = mirrorSprite;
            mirror.MirrorAnimator = mirrorAnimator;
            mirror.MirrorController = mirrorController;
            mirror.Interpolator = interpolator;
            mirror.Layer = layer;
            mirror.Init();
            TransactionManager.Revoke(go);
            return mirror;
        }
        // is a MIRROR and not a SYNC
        public static bool IsMirror(GameObject obj, out HornetMirror mirror)
        {
            if (obj.TryGetComponent<HornetMirror>(out mirror))
            {
                Logger.Msg("found mirror (islocal):", mirror.IsLocal);
                return !mirror.IsLocal;
            }

            return false;
        }
        public static bool IsMirror(GameObject obj)
        {
            return TransactionManager.Fetch<bool>(obj);
        }
        public static HornetMirror CreateLocal()
        {
            GameObject mirrorObj = new GameObject();
            mirrorObj.SetName($"SilkBound Hornet Sync");
            mirrorObj.transform.SetParent(HeroController.instance.transform);

            return mirrorObj.AddComponent<HornetMirror>();
        }
        public GameObject Attacks = null!;
        public void Init()
        {
            if (IsLocal) return;

            MirrorController.NailImbuement = Root.AddComponent<HeroNailImbuement>();

            Attacks = Instantiate(HeroController.instance.transform.Find("Attacks").gameObject, Root.transform);
            Attacks.GetComponentsInChildren<DamageEnemies>(true).ToList().ForEach(c => c.enabled = false); // position misalignments could cause damage inbalances. we will sync this from direct calls instead
        }

        public T? GetNailAttack<T>(string path) where T : NailAttackBase
        {
            if (IsLocal) return null;
            return Attacks.transform.Find(path).GetComponent<T>();
        }
        //public NailSlash? GetNailSlash(string path)
        //{
        //    if (IsLocal) return null;
        //    return Attacks.transform.Find(path).GetComponent<NailSlash>();
        //}
        //public Downspike? GetDownspike(string path)
        //{
        //    if (IsLocal) return null;
        //    return Attacks.transform.Find(path).GetComponent<Downspike>();
        //}

        public static HornetMirror CreateMirror(UpdateWeaverPacket packet)
        {
            GameObject mirrorObj = new GameObject();
            mirrorObj.SetName($"SilkBound Mirror " + packet.id);
            //mirrorObj.transform.SetParent(HeroController.instance.transform);
            mirrorObj.transform.position = new Vector3(packet.posX, packet.posY, 0.004f + Server.CurrentServer!.Connections.Count * 0.001f);
            mirrorObj.transform.localScale = new Vector3(packet.scaleX, 1, 1);

            DontDestroyOnLoad(mirrorObj);

            tk2dSprite source = Sprite;
            tk2dSprite mirrorSprite = tk2dSprite.AddComponent(mirrorObj, tk2dSpriteCollection.Instantiate(source.Collection), UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            SkinManager.ApplySkin(mirrorSprite, Server.CurrentServer!.Connections.First(c => c.ClientID.ToString("N") == packet.id.ToString("N")).AppliedSkin);

            mirrorSprite.color = new Color(1, 1, 1, 1);

            tk2dSpriteAnimator reference = HeroController.instance.GetComponent<tk2dSpriteAnimator>();
            //tk2dSpriteAnimator.AddComponent(mirrorObj, reference.Library, reference.Library.GetClipIdByName(reference.CurrentClip.name));

            HeroControllerMirror mirrorController = mirrorObj.AddComponent<HeroControllerMirror>();

            //HeroAnimationController reference = ReferenceAnimator;
            TransactionManager.Promise<bool>(mirrorObj, true);
            tk2dSpriteAnimator mirrorAnimator = tk2dSpriteAnimator.AddComponent(mirrorObj, reference.Library, reference.Library.GetClipIdByName(reference.CurrentClip.name));// mirrorObj.AddComponent<HeroAnimationController>(); //tk2dSpriteAnimator.AddComponent(mirrorObj, reference.Library, reference.Library.GetClipIdByName(reference.CurrentClip.name));
            //mirrorAnimator.SetSprite(mirrorSprite.Collection, mirrorSprite.GetSpriteIdByName("Hornet_sit_breath_look0010"));

            SimpleInterpolator interpolator = mirrorObj.AddComponent<SimpleInterpolator>();
            interpolator.velocity = new Vector3(0, 0, 0);
            return HornetMirror.AddComponent(mirrorObj, mirrorController, mirrorSprite, mirrorAnimator, interpolator, mirrorObj.transform.position.z, false);
        }

        public void UpdateMirror(UpdateWeaverPacket packet)
        {
            if (IsLocal) return;
            Root.SetActive(packet.scene == SceneManager.GetActiveScene().name);

            Root.transform.position = new Vector3(packet.posX, packet.posY, Layer);
            Root.transform.localScale = new Vector3(packet.scaleX, 1, 1);

            Interpolator.velocity = new Vector3(packet.vX, packet.vY, 0);
        }

        public void PlayClip(PlayClipPacket packet)
        {
            if (IsLocal) return;

            MirrorAnimator.Play(MirrorAnimator.Library.GetClipByName(packet.clipName), packet.clipStartTime, packet.overrideFps);
            //Mirror
        }

        protected override void Start()
        {

        }

        protected override void Reset()
        {

        }

        public UpdateWeaverPacket CraftPacket()
        {
            return new UpdateWeaverPacket(
                NetworkUtils.LocalClient!.ClientID,
                SceneManager.GetActiveScene().name,
                HeroController.instance.transform.position.x,
                HeroController.instance.transform.position.y,
                HeroController.instance.transform.localScale.x,
                HeroController.instance.GetComponent<Rigidbody2D>().linearVelocity.x * Time.timeScale,
                HeroController.instance.GetComponent<Rigidbody2D>().linearVelocity.y * Time.timeScale
            );
        }
        protected override void Tick(float dt)
        {
            if(IsLocal)
                NetworkUtils.SendPacket(CraftPacket());
        }
    }
}
