using GlobalEnums;
using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Sync;
using SilkBound.Types;
using SilkBound.Types.Language;
using SilkBound.Types.Mirrors;
using SilkBound.Utils;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SilkBound.Behaviours {
    public class HornetMirror : GenericSync {
        public static tk2dSprite Sprite => HeroController.instance?.GetComponent<tk2dSprite>()!;

        public bool IsLocal = true;
        public GameObject Root = null!;
        public bool IsInScene => Root.activeInHierarchy;
        public SimpleInterpolator Interpolator = null!;
        public tk2dSprite MirrorSprite = null!;
        public tk2dSpriteAnimator MirrorAnimator = null!;
        public HeroControllerMirror MirrorController = null!;
        public tk2dSpriteCollectionData MirrorSpriteCollection = null!;
        public tk2dSpriteAnimation MirrorLibrary = null!;
        public Weaver Client = null!;
        public float Layer = 0.004f;
        public string Scene = string.Empty;

        private static tk2dSpriteAnimator? _cachedLocal;
        public static tk2dSpriteAnimator ReferenceAnimator {
            get
            {
                if (NetworkUtils.IsNullPtr(_cachedLocal))
                    _cachedLocal = HeroController.instance.GetComponent<tk2dSpriteAnimator>();

                return _cachedLocal;
            }
        }

        public static HornetMirror AddComponent(GameObject go, bool local, Weaver? client = null, HeroControllerMirror? mirrorController = null, tk2dSprite? mirrorSprite = null, tk2dSpriteCollectionData? mirrorSpriteCollection = null, tk2dSpriteAnimation? mirrorLibrary = null, tk2dSpriteAnimator? mirrorAnimator = null, SimpleInterpolator? interpolator = null, float? layer = null) {
            HornetMirror mirror = go.AddComponent<HornetMirror>();

            mirror.IsLocal = local;
            mirror.Root = go;
            mirror.Client = client ?? NetworkUtils.LocalClient!;
            mirror.MirrorSprite = mirrorSprite!;
            mirror.MirrorSpriteCollection = mirrorSpriteCollection!;
            mirror.MirrorLibrary = mirrorLibrary!;
            mirror.MirrorAnimator = mirrorAnimator!;
            mirror.MirrorController = mirrorController!;
            mirror.Interpolator = interpolator!;

            if (layer.HasValue)
                mirror.Layer = layer.Value;

            mirror.Init();

            TransactionManager.Revoke(go);

            return mirror;
        }
        // is a MIRROR and not a SYNC object
        public static bool IsMirror(GameObject obj, out HornetMirror mirror) {
            if (obj.TryGetComponent<HornetMirror>(out mirror)) {
                return !mirror.IsLocal;
            }

            return false;
        }
        public static bool IsMirror(GameObject obj) {
            return TransactionManager.Fetch<bool>(obj);
        }
        public static string GetObjectName(Guid id) {
            return $"SilkBound Mirror {id}";
        }
        public static HornetMirror CreateLocal() {
            GameObject mirrorObj = new();
            //mirrorObj.SetName($"SilkBound Hornet Sync");
            mirrorObj.SetName(GetObjectName(NetworkUtils.ClientID));
            mirrorObj.transform.SetParent(HeroController.instance.transform);

            return AddComponent(mirrorObj, true, NetworkUtils.LocalClient); // not gunna do object comparisons js as a precaution
        }
        public GameObject Attacks = null!;
        public GameObject Effects = null!;
        public GameObject Dash = null!;
        public GameObject AirDash = null!;
        public GameObject WallDashKickoff = null!;
        public void Init() {
            if (IsLocal) return;

            Root.AddComponent<HeroNailImbuement>();

            var heroEffects = HeroController.instance.transform.Find("Effects").gameObject;
            heroEffects.SetActive(false);
            Effects = Instantiate(heroEffects, Root.transform);
            foreach (var tween in Effects.GetComponentsInChildren<iTween>(true))
                tween.enabled = false;
            heroEffects.SetActive(true);
            Effects.name = "Effects";
            Dash = Effects.transform.Find("Dash Burst").gameObject;
            AirDash = Effects.transform.Find("Dash Burst").gameObject;
            WallDashKickoff = Effects.transform.Find("Walldash Kickoff").gameObject;

            Attacks = Instantiate(HeroController.instance.transform.Find("Attacks").gameObject, Root.transform);
            Attacks.name = "Attacks";

            //Attacks.GetComponentsInChildren<DamageEnemies>(true).ToList().ForEach(c => c.enabled = false); // position misalignments could cause damage inbalances. we will sync this from direct calls instead
            Attacks.GetComponentsInChildren<AudioSource>(true).ToList().ForEach((source) =>                // audio falloffs
            {
                source.rolloffMode = AudioRolloffMode.Linear;
                source.maxDistance = 45f;
                source.minDistance = 30f;
                source.spatialBlend = 1f;
            });

            MirrorController.SetupGameRefs();

            regionListener = Root.AddComponentIfNotPresent<EnviroRegionListener>();
            regionListener.enabled = false;
        }

        public T? GetNailAttack<T>(string path) where T : NailAttackBase {
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

        public static tk2dSpriteAnimation CloneAnimationWithCollection(GameObject owner, tk2dSpriteAnimation source, tk2dSpriteCollectionData targetCollection) {
            tk2dSpriteAnimation clone = owner.AddComponent<tk2dSpriteAnimation>();
            clone.name = source.name + "_Clone";

            clone.clips = new tk2dSpriteAnimationClip[source.clips.Length];
            for (int i = 0; i < source.clips.Length; i++) {
                var srcClip = source.clips[i];
                if (srcClip == null)
                    continue;

                var dstClip = new tk2dSpriteAnimationClip {
                    name = srcClip.name,
                    fps = srcClip.fps,
                    loopStart = srcClip.loopStart,
                    wrapMode = srcClip.wrapMode
                };

                if (srcClip.frames != null) {
                    dstClip.frames = new tk2dSpriteAnimationFrame[srcClip.frames.Length];
                    for (int j = 0; j < srcClip.frames.Length; j++) {
                        var srcFrame = srcClip.frames[j];
                        var dstFrame = new tk2dSpriteAnimationFrame {
                            spriteCollection = targetCollection,
                            spriteId = srcFrame.spriteId
                        };
                        dstClip.frames[j] = dstFrame;
                    }
                }

                clone.clips[i] = dstClip;
            }

            return clone;
        }
        public static tk2dSpriteCollectionData CopyCollection(GameObject mirror, tk2dSpriteCollectionData source, Skin skin) {
            tk2dSpriteCollectionData collection = (tk2dSpriteCollectionData) source.CopyComponent(mirror, "inst", "platformSpecificData", "name", "spriteDefinitions", "materialInsts", "textureInsts", "materials", "textures");

            if (source.material != null)
                collection.material = new Material(source.material);

            collection.materials = new Material[source.materials.Length];
            for (int i = 0; i < source.materials.Length; i++) {
                var material = source.materials[i];
                collection.materials[i] = new Material(material);
            }

            collection.textures = new Texture[source.textures.Length];
            collection.textureInsts = new Texture2D[source.textureInsts.Length];
            for (int i = 0; i < source.textures.Length; i++) {
                var texture = (Texture2D) source.textures[i];

                Texture2D copy = new(texture.width, texture.height, texture.format, false);
                copy.LoadRawTextureData(texture.GetRawTextureData());
                copy.Apply();
                collection.textures[i] = copy;
            }


            collection.materialInsts = new Material[collection.materials.Length];
            for (int i = 0; i < collection.materials.Length; i++) {
                collection.materialInsts[i] = new Material(collection.materials[i]);
            }
            collection.textureInsts = null;

            collection.Init();

            Logger.Msg("Mirror Skin:", skin.SkinName);
            SkinManager.ApplySkin(collection, skin);

            collection.spriteDefinitions = new tk2dSpriteDefinition[source.spriteDefinitions.Length];
            for (int i = 0; i < collection.spriteDefinitions.Length; i++) {
                var sourceDefinition = source.spriteDefinitions[i];
                collection.spriteDefinitions[i] = new tk2dSpriteDefinition() // im sorry
                {
                    name = sourceDefinition.name,
                    texelSize = sourceDefinition.texelSize,
                    material = sourceDefinition.material,
                    materialInst = sourceDefinition.materialInst,
                    materialId = sourceDefinition.materialId,
                    sourceTextureGUID = sourceDefinition.sourceTextureGUID,
                    extractRegion = sourceDefinition.extractRegion,
                    regionX = sourceDefinition.regionX,
                    regionY = sourceDefinition.regionY,
                    regionW = sourceDefinition.regionW,
                    regionH = sourceDefinition.regionH,
                    complexGeometry = sourceDefinition.complexGeometry,
                    colliderConvex = sourceDefinition.colliderConvex,
                    colliderSmoothSphereCollisions = sourceDefinition.colliderSmoothSphereCollisions,
                    flipped = sourceDefinition.flipped,
                    physicsEngine = sourceDefinition.physicsEngine,
                    colliderType = sourceDefinition.colliderType,
                    colliderVertices = sourceDefinition.colliderVertices,
                    colliderIndicesFwd = sourceDefinition.colliderIndicesFwd,
                    colliderIndicesBack = sourceDefinition.colliderIndicesBack,
                    boundsData = sourceDefinition.boundsData,
                    untrimmedBoundsData = sourceDefinition.untrimmedBoundsData,
                    positions = sourceDefinition.positions,
                    normals = sourceDefinition.normals,
                    tangents = sourceDefinition.tangents,
                    uvs = sourceDefinition.uvs,
                    polygonCollider2D = sourceDefinition.polygonCollider2D ?? [],
                    edgeCollider2D = sourceDefinition.edgeCollider2D ?? [],
                    customColliders = sourceDefinition.customColliders ?? [],
                    normalizedUvs = sourceDefinition.normalizedUvs ?? [],
                    indices = sourceDefinition.indices ??
                    [
                        0,
                        3,
                        1,
                        2,
                        3,
                        0
                    ],
                    attachPoints = sourceDefinition.attachPoints ?? []
                };
            }

            foreach (var sprite in collection.spriteDefinitions) {
                sprite.material = collection.materials[sprite.materialId];
                sprite.materialInst = collection.materialInsts![sprite.materialId];
            }

            return collection;
        }
        public static HornetMirror? CreateMirror(UpdateWeaverPacket packet) {
            using (new StackFlag<HornetMirror>()) {
                if (HeroController.instance == null)
                    return null;

                GameObject mirrorObj = new();
                //mirrorObj.SetName($"SilkBound Mirror " + packet.id);
                mirrorObj.SetName(GetObjectName(packet.Sender.ClientID));
                //mirrorObj.transform.SetParent(HeroController.instance.transform);
                mirrorObj.transform.position = new Vector3(packet.PosX, packet.PosY, 0.004f + Server.CurrentServer!.Connections.Count * 0.001f);
                mirrorObj.transform.localScale = new Vector3(packet.ScaleX, 1, 1);

                mirrorObj.layer = LayerMask.NameToLayer("Ignore Raycast");

                DontDestroyOnLoad(mirrorObj);

                tk2dSprite source = Sprite!;
                tk2dSpriteCollectionData collection = CopyCollection(mirrorObj, source.Collection, packet.Sender.AppliedSkin);
                tk2dSprite mirrorSprite = tk2dSprite.AddComponent(mirrorObj, source!.Collection, UnityEngine.Random.Range(int.MinValue, int.MaxValue));

                mirrorSprite.color = new Color(1, 1, 1, 1);

                tk2dSpriteAnimator reference = HeroController.instance.GetComponent<tk2dSpriteAnimator>();
                HeroControllerMirror mirrorController = mirrorObj.AddComponent<HeroControllerMirror>();

                TransactionManager.Promise<bool>(mirrorObj, true);
                tk2dSpriteAnimation library = CloneAnimationWithCollection(mirrorObj, reference.Library, collection);

                tk2dSpriteAnimator mirrorAnimator = tk2dSpriteAnimator.AddComponent(mirrorObj, reference.Library, 0);
                mirrorAnimator.Library = library;

                //mirrorAnimator.SetSprite(mirrorSprite.Collection, mirrorSprite.GetSpriteIdByName("Hornet_sit_breath_look0010"));

                SimpleInterpolator interpolator = mirrorObj.AddComponent<SimpleInterpolator>();
                interpolator.velocity = new Vector3(0, 0, 0);

                HeroController.instance.GetComponent<Collider2D>().CopyComponent(mirrorObj);

                var rb2d = ((Rigidbody2D) HeroController.instance.GetComponent<Rigidbody2D>().CopyComponent(mirrorObj));
                rb2d.bodyType = RigidbodyType2D.Dynamic;
                rb2d.constraints = RigidbodyConstraints2D.FreezeAll;

                return HornetMirror.AddComponent(mirrorObj, false, packet.Sender, mirrorController, mirrorSprite, collection, library, mirrorAnimator, interpolator, mirrorObj.transform.position.z);
            }
        }

        private EnviroRegionListener regionListener = null!;
        public EnvironmentTypes CurrentEnvironment { get; private set; } = EnvironmentTypes.Dust;
        public void UpdateMirror(UpdateWeaverPacket packet) {
            if (IsLocal) return;
            //Logger.Stacktrace();

            Scene = packet.Scene;

            CurrentEnvironment = packet.Environment;
            regionListener.overrideEnvironment = true;
            regionListener.overrideEnvironmentType = packet.Environment;
            Root.SetActive(packet.Scene == SceneManager.GetActiveScene().name);

            Root.transform.position = new Vector3(packet.PosX, packet.PosY, Layer);
            Root.transform.localScale = new Vector3(packet.ScaleX, 1, 1);

            Interpolator.velocity = new Vector3(packet.VelocityX, packet.VelocityY, 0);
            //Logger.Msg("updating mirror:", packet.posX, packet.posY, packet.scaleX, packet.vX, packet.vY);
        }
        public void PlayClip(PlayClipPacket packet) {
            if (IsLocal) return;

            MirrorAnimator.Play(MirrorLibrary.GetClipByName(packet.clipName), packet.clipStartTime, packet.overrideFps);
            //Mirror
        }
        public void DoAirDashVFX(bool doGroundDash, bool doAirDash, bool wallSliding, bool dashDown, float scale) {
            if (wallSliding) {
                WallDashKickoff.SetActive(false);
                WallDashKickoff.SetActive(true);
            }

            float num = scale;
            if (doGroundDash) {
                Dash.transform.localScale = new Vector3(-num, num, num);
                Dash.SetActive(false);
                Dash.SetActive(true);
                return;
            }
            if (dashDown) {
                AirDash.transform.SetLocalRotation2D(90f);
            } else {
                AirDash.transform.SetLocalRotation2D(0f);
            }
            AirDash.transform.localScale = new Vector3(num * -Math.Abs(Root.transform.localScale.x), num, num); // this works idk why
            AirDash.SetActive(false);
            AirDash.SetActive(true);
        }
        public void Ghost() {
            if (IsLocal) {
                Sprite.color = new Color(1, 1, 1, 0.5f);
                return;
            }

            MirrorSprite.color = new Color(1, 1, 1, 0.5f); // so happy tk2d supports alpha
        }
        public void EndGhost() {
            if (IsLocal) {
                Sprite.color = new Color(1, 1, 1, 1f);
                return;
            }

            MirrorSprite.color = new Color(1, 1, 1, 1f);
        }

        public void GhostRespawn() {
            if (IsLocal) {

                return;
            }
            //Transform targetBench;
            //MirrorController.Respawn();
        }
        protected override void Start() {

        }

        protected override void Reset() {

        }

        EnviroRegionListener? _cached = null;
        EnviroRegionListener CachedEnviro {
            get
            {
                return _cached ??= HeroController.instance.GetComponent<EnviroRegionListener>();
            }
        }
        public UpdateWeaverPacket CraftPacket() {
            return new UpdateWeaverPacket(
                SceneManager.GetActiveScene().name,
                HeroController.instance.transform.position.x,
                HeroController.instance.transform.position.y,
                HeroController.instance.transform.localScale.x,
                HeroController.instance.GetComponent<Rigidbody2D>().linearVelocity.x * Time.timeScale,
                HeroController.instance.GetComponent<Rigidbody2D>().linearVelocity.y * Time.timeScale,
                CachedEnviro?.CurrentEnvironmentType ?? EnvironmentTypes.Dust
            );
        }
        protected override void Tick(float dt) {
            if (Root)
                Root.name = GetObjectName(Client.ClientID);

            if (MirrorSprite?.Collection != MirrorSpriteCollection && !IsLocal) {
                MirrorAnimator.SetSprite(MirrorSpriteCollection, MirrorSprite!.spriteId);
                MirrorAnimator.Sprite.collectionInst = MirrorSpriteCollection.inst;
                Root!.GetComponent<Renderer>().material = MirrorSpriteCollection.spriteDefinitions[MirrorSprite.spriteId].material;
                MirrorAnimator.Sprite.UpdateMaterial();
                //MirrorSprite!.Collection = MirrorSpriteCollection;
            }

            if (MirrorAnimator?.Library != MirrorLibrary && !IsLocal)
                MirrorAnimator!.Library = MirrorLibrary;

            if (IsLocal)
                NetworkUtils.SendPacket(CraftPacket());
        }
    }
}