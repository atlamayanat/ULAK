using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ulak.Core;
using Ulak.Gameplay;

namespace Ulak.EditorTools
{
    /// <summary>
    /// Tek tıkla greybox "Yol" test sahnesi kurar (Faz 1).
    /// Asset gerekmez: beyaz kare sprite üretip renk tint'iyle gri-kutu yapılır.
    /// Menü: Ulak ▸ Greybox Yol Sahnesi Kur
    /// </summary>
    public static class GreyboxRoadBuilder
    {
        private const string SpritePath = "Assets/_Project/Art/Greybox/square.png";
        private const string SkySpritePath = "Assets/_Project/Art/Background/gokyuzu.png";
        private const string PhysMatPath = "Assets/_Project/Art/Greybox/NoFriction.physicsMaterial2D";
        private const int GroundLayer = 6;
        private const int EnemyLayer = 7;

        [MenuItem("Ulak/Greybox Yol Sahnesi Kur")]
        public static void Build()
        {
            EnsureLayers();
            Sprite sq = GetOrCreateSquareSprite();
            PhysicsMaterial2D noFriction = GetOrCreateNoFrictionMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Kamera ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.15f, 0.17f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10);

            // --- Gökyüzü arka planı (gün doğumu kayması) ---
            BuildSky(cam);

            // --- Zemin (uzun yol) ---
            MakeBox("Ground", new Vector2(0, -3f), new Vector2(80, 1f),
                new Color(0.35f, 0.3f, 0.25f), sq, GroundLayer, isStatic: true);

            // --- Zıplanacak engel (alçak duvar / basamak) ---
            MakeBox("Obstacle_Step", new Vector2(8f, -2.25f), new Vector2(1f, 1.5f),
                new Color(0.5f, 0.42f, 0.3f), sq, GroundLayer, isStatic: true);

            // --- Çarpılacak yüksek duvar (auto-run burada durur) ---
            MakeBox("Wall", new Vector2(32f, -1.5f), new Vector2(1f, 4f),
                new Color(0.4f, 0.2f, 0.2f), sq, GroundLayer, isStatic: true);

            // --- Oyuncu ---
            var player = BuildPlayer(sq, noFriction);

            // Kamera takibi
            var follow = camGo.AddComponent<CameraFollow>();
            SerializedSet(follow, "target", player.transform);

            // --- Düşmanlar ---
            // İlki basamaktan önce (açık alanda), diğerleri basamak ile duvar
            // arasındaki geniş bölgede — knockback'te duvara sıkışmasınlar diye aralıklı.
            BuildEnemy(sq, noFriction, new Vector2(5f, -2f));
            BuildEnemy(sq, noFriction, new Vector2(15f, -2f));
            BuildEnemy(sq, noFriction, new Vector2(22f, -2f));

            EditorSceneManager.MarkSceneDirty(scene);
            string scenePath = "Assets/_Project/Scenes/Road_Greybox.unity";
            System.IO.Directory.CreateDirectory(Application.dataPath + "/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("[Ulak] Greybox yol sahnesi kuruldu → " + scenePath +
                      "\nKontroller: Sağ tık = saldırı, Space = zıpla. Oto-koşu sağa.");
            EditorUtility.DisplayDialog("Ulak",
                "Greybox yol sahnesi kuruldu.\n\nKontroller:\n• Sağ tık → kılıç saldırısı\n• Space → zıpla\n• Oto-koşu otomatik (sağa)\n\nPlay'e bas ve test et.",
                "Tamam");
        }

        // ---- Oyuncu kurulumu ----
        private static GameObject BuildPlayer(Sprite sq, PhysicsMaterial2D noFriction)
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            go.transform.position = new Vector2(-3f, -1.8f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sq;
            sr.color = new Color(0.3f, 0.6f, 1f);
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Sıfır sürtünme: duvara dayanırken yapışıp kalmayı önler (zıplayarak aşılabilir).
            var col = go.AddComponent<BoxCollider2D>();
            col.sharedMaterial = noFriction;

            go.AddComponent<Knockback>();
            go.AddComponent<DamageFlash>();

            var health = go.AddComponent<Health>();
            SerializedSet(health, "maxHealth", 5);
            go.AddComponent<HealthBar>();

            // Ayak hizası zemin kontrol noktası
            var feet = new GameObject("GroundCheck");
            feet.transform.SetParent(go.transform);
            feet.transform.localPosition = new Vector3(0, -0.55f, 0);

            var pc = go.AddComponent<PlayerController>();
            SerializedSet(pc, "groundCheck", feet.transform);
            SerializedSet(pc, "groundLayer", (LayerMask)(1 << GroundLayer));

            var atk = go.AddComponent<SwordAttack>();
            SerializedSet(atk, "targetLayers", (LayerMask)(1 << EnemyLayer));

            go.AddComponent<PlayerRespawn>();

            return go;
        }

        // ---- Düşman kurulumu ----
        private static void BuildEnemy(Sprite sq, PhysicsMaterial2D noFriction, Vector2 pos)
        {
            var go = new GameObject("Enemy_Small");
            go.layer = EnemyLayer;
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sq;
            sr.color = new Color(0.85f, 0.3f, 0.3f);
            sr.sortingOrder = 9;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.sharedMaterial = noFriction;
            go.AddComponent<Knockback>();
            go.AddComponent<DamageFlash>();
            go.AddComponent<Health>();
            go.AddComponent<HealthBar>();
            go.AddComponent<SmallEnemyAI>();
            go.AddComponent<EnemyDeath>();
        }

        // ---- Kutu (zemin/duvar/engel) ----
        private static GameObject MakeBox(string name, Vector2 pos, Vector2 size,
            Color color, Sprite sq, int layer, bool isStatic)
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.isStatic = isStatic;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sq;
            sr.color = color;
            sr.sortingOrder = 0;

            go.AddComponent<BoxCollider2D>();
            return go;
        }

        // ---- 1x1 beyaz kare sprite (üret ya da yükle) ----
        private static Sprite GetOrCreateSquareSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null) return existing;

            System.IO.Directory.CreateDirectory(Application.dataPath + "/_Project/Art/Greybox");

            var tex = new Texture2D(32, 32);
            var px = new Color32[32 * 32];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(Application.dataPath + "/_Project/Art/Greybox/square.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 32;     // 32x32 png = 1 dünya birimi
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        // ---- Gökyüzü arka planı ----
        private static void BuildSky(Camera cam)
        {
            var sky = AssetDatabase.LoadAssetAtPath<Sprite>(SkySpritePath);
            if (sky == null)
            {
                // Henüz Sprite olarak import edilmemiş olabilir — importer'ı ayarla ve tekrar dene.
                var imp = AssetImporter.GetAtPath(SkySpritePath) as TextureImporter;
                if (imp == null)
                {
                    Debug.LogWarning("[Ulak] Gökyüzü görseli bulunamadı: " + SkySpritePath);
                    return;
                }
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 64; // 640px / 64 = 10 dünya birimi (ekran yüksekliği)
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
                sky = AssetDatabase.LoadAssetAtPath<Sprite>(SkySpritePath);
                if (sky == null)
                {
                    Debug.LogWarning("[Ulak] Gökyüzü sprite'ı yüklenemedi: " + SkySpritePath);
                    return;
                }
            }

            var go = new GameObject("Sky");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sky;
            sr.sortingOrder = -100;

            var bg = go.AddComponent<SkyBackground>();
            SerializedSet(bg, "targetCamera", cam);
        }

        // ---- Sıfır sürtünmeli fizik materyali (üret ya da yükle) ----
        private static PhysicsMaterial2D GetOrCreateNoFrictionMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysMatPath);
            if (existing != null) return existing;

            System.IO.Directory.CreateDirectory(Application.dataPath + "/_Project/Art/Greybox");

            var mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, PhysMatPath);
            return mat;
        }

        // ---- Layer'ları garantiye al (Ground=6, Enemy=7) ----
        private static void EnsureLayers()
        {
            SetLayerName(GroundLayer, "Ground");
            SetLayerName(EnemyLayer, "Enemy");
        }

        private static void SetLayerName(int index, string name)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            if (layers == null || index >= layers.arraySize) return;

            var sp = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(sp.stringValue) || sp.stringValue == name)
            {
                sp.stringValue = name;
                tagManager.ApplyModifiedProperties();
            }
        }

        // ---- SerializedObject ile private [SerializeField] alanı ata ----
        private static void SerializedSet(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Ulak] Alan bulunamadı: {field} ({target})"); return; }

            switch (value)
            {
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case LayerMask lm: p.intValue = lm.value; break;
                case Object o: p.objectReferenceValue = o; break;
                default: Debug.LogWarning($"[Ulak] Desteklenmeyen tip: {field}"); break;
            }
            so.ApplyModifiedProperties();
        }
    }
}
