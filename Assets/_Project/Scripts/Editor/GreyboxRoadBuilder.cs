using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
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
        private const string PlayerIdle0Path = "Assets/_Project/Art/Characters/Player/player_idle_0.png";
        private const string PlayerIdle1Path = "Assets/_Project/Art/Characters/Player/player_idle_1.png";
        private const string PlayerWalkPath = "Assets/_Project/Art/Characters/Player/player_walk.png";
        private const string SlashArcPath = "Assets/_Project/Art/VFX/slash_arc.png";
        private const string EngelPath = "Assets/_Project/Art/Environment/engel.jpeg";
        private const string PhysMatPath = "Assets/_Project/Art/Greybox/NoFriction.physicsMaterial2D";
        private const int GroundLayer = 6;
        private const int EnemyLayer = 7;

        [MenuItem("Ulak/Greybox Yol Sahnesi Kur")]
        public static void Build()
        {
            // KORUMA: var olan sahnenin üzerine yazmadan önce onay iste
            // (elle yapılan harita düzenlemeleri geri getirilemez şekilde silinir).
            if (System.IO.File.Exists("Assets/_Project/Scenes/Road_Greybox.unity") &&
                !EditorUtility.DisplayDialog("Ulak — DİKKAT",
                    "Road_Greybox.unity zaten var.\n\nÜzerine yazarsan elle yaptığın TÜM harita düzenlemeleri silinir!",
                    "Üzerine Yaz", "İptal"))
                return;

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
            // Kaplama: engel.jpeg (taş dokusu). Yoksa gri kutuya düşer.
            // 52 ppu → ~1x1 dünya birimi; kutu ölçeğiyle 1 x 1.5'e esner.
            Sprite engel = GetOrCreateSprite(EngelPath, 52f);
            MakeBox("Obstacle_Step", new Vector2(8f, -2.25f), new Vector2(1f, 1.5f),
                engel != null ? Color.white : new Color(0.5f, 0.42f, 0.3f),
                engel != null ? engel : sq, GroundLayer, isStatic: true);

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
                      "\nKontroller: A/D-ok = hareket, Space/W/yukarı = zıpla, Sağ tık = saldırı, H = can yenile (3 yük).");
            EditorUtility.DisplayDialog("Ulak",
                "Greybox yol sahnesi kuruldu.\n\nKontroller:\n• A/D veya ok tuşları → hareket\n• Space / W / yukarı ok → zıpla\n• Sağ tık → kılıç saldırısı\n• H → can yenile (3 yük gerekir)\n\nPlay'e bas ve test et.",
                "Tamam");
        }

        // ================= GÜNDÜZ MODU: AT KOŞUSU =================
        private const string DayScenePath = "Assets/_Project/Scenes/Road_Day_Greybox.unity";
        private const string HorseIdlePath = "Assets/_Project/Art/Characters/Horse/horse_idle.png";
        private const string HorseRunPath = "Assets/_Project/Art/Characters/Horse/horse_run.png";

        [MenuItem("Ulak/Gündüz At Sahnesi Kur")]
        public static void BuildDay()
        {
            if (System.IO.File.Exists(DayScenePath) &&
                !EditorUtility.DisplayDialog("Ulak — DİKKAT",
                    "Road_Day_Greybox.unity zaten var.\n\nÜzerine yazarsan elle yaptığın TÜM düzenlemeler silinir!",
                    "Üzerine Yaz", "İptal"))
                return;

            BuildDayCore();

            EditorUtility.DisplayDialog("Ulak",
                "Gündüz at sahnesi kuruldu.\n\n• At otomatik koşar ve giderek hızlanır\n• 2 blokluk hendeklerden zıplayarak aş (Space/W/yukarı)\n• Boşluklara düşersen ÖLÜRSÜN — sahne baştan başlar\n• Sağ tık → kılıçla altın kutuları kır (+10 puan)\n\nPlay'e bas ve test et.",
                "Tamam");
        }

        /// <summary>Sahneyi onaysız kurar (programatik çağrılar için).</summary>
        public static void BuildDayCore()
        {
            EnsureLayers();
            Sprite sq = GetOrCreateSquareSprite();
            PhysicsMaterial2D noFriction = GetOrCreateNoFrictionMaterial();
            Sprite engel = GetOrCreateSprite(EngelPath, 52f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Kamera (at hızlı: önü daha geniş görsün) ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.45f, 0.7f, 0.95f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10);
            var follow = camGo.AddComponent<CameraFollow>();
            SerializedSet(follow, "offset", new Vector3(4f, 0f, -10f));

            // --- Gökyüzü: gün doğumu kayması KAPALI → sabit gündüz ---
            BuildSky(cam);
            var skyGo = GameObject.Find("Sky");
            if (skyGo != null)
                SerializedSet(skyGo.GetComponent<SkyBackground>(), "sunriseDuration", 0f);

            // ====== SEVİYE TASARIMI ======
            // Boşluklar (çukurlar): merkez X + genişlik. Düşen ölür, sahne yeniden başlar.
            float[] pitX = { 48f, 100f, 155f, 215f, 275f, 335f };
            const float pitW = 2.8f;

            // Hendekler (2 blok = 2 birim yüksek): at zıplayarak aşar.
            float[] obstacleX = { 25f, 72f, 128f, 185f, 245f, 305f, 358f };

            var groundColor = new Color(0.45f, 0.4f, 0.3f);

            // --- Zemin: çukurların arasında parçalı segmentler ---
            float levelStart = -20f, levelEnd = 390f;
            float segStart = levelStart;
            int segIndex = 0;
            foreach (float px in pitX)
            {
                float gapL = px - pitW * 0.5f;
                float segW = gapL - segStart;
                MakeBox("Ground_" + segIndex++, new Vector2(segStart + segW * 0.5f, -3f),
                    new Vector2(segW, 1f), groundColor, sq, GroundLayer, isStatic: true);
                segStart = px + pitW * 0.5f;
            }
            MakeBox("Ground_" + segIndex, new Vector2(segStart + (levelEnd - segStart) * 0.5f, -3f),
                new Vector2(levelEnd - segStart, 1f), groundColor, sq, GroundLayer, isStatic: true);

            // --- Hendekler: 2 blok yüksek (1 x 2), zemin üstüne oturur ---
            for (int i = 0; i < obstacleX.Length; i++)
            {
                MakeBox("Obstacle_" + i, new Vector2(obstacleX[i], -1.5f), new Vector2(1f, 2f),
                    engel != null ? Color.white : new Color(0.5f, 0.42f, 0.3f),
                    engel != null ? engel : sq, GroundLayer, isStatic: true);
            }

            // --- Altın kutular: hendek diplerinden ve çukurlardan UZAK ---
            for (int i = 0; i < 24; i++)
            {
                float x = 12f + i * 15.5f;
                if (x > levelEnd - 10f) break;

                // Hendeğe 5 birimden yakınsa koyma.
                bool nearObstacle = false;
                foreach (float ox in obstacleX)
                    if (Mathf.Abs(x - ox) < 5f) { nearObstacle = true; break; }

                // Çukura (kenarları dahil) 4 birimden yakınsa koyma.
                bool nearPit = false;
                foreach (float px in pitX)
                    if (Mathf.Abs(x - px) < pitW * 0.5f + 4f) { nearPit = true; break; }

                if (!nearObstacle && !nearPit)
                    BuildGoldBox(sq, new Vector2(x, -2.1f));
            }

            // --- At: gerçek sprite varsa onu, yoksa küp placeholder kullan ---
            // horse_idle.png 80x64 (duruş), horse_run.png 160x64 (2 koşu karesi).
            Sprite horseIdle = GetOrCreateSprite(HorseIdlePath, 40f); // 64px / 40 = 1.6 birim boy
            Sprite[] horseRun = GetOrCreateSheetSprites(HorseRunPath, 80, 64, 40f, "horse_run");
            bool hasHorseArt = horseIdle != null;

            var horse = new GameObject("Horse");
            horse.tag = "Player";
            horse.transform.position = new Vector2(0f, -1.85f);
            if (!hasHorseArt)
                horse.transform.localScale = new Vector3(1.6f, 1.2f, 1f);

            var hsr = horse.AddComponent<SpriteRenderer>();
            hsr.sprite = hasHorseArt ? horseIdle : sq;
            hsr.color = hasHorseArt ? Color.white : new Color(0.55f, 0.38f, 0.22f);
            hsr.sortingOrder = 10;

            if (hasHorseArt && horseRun != null && horseRun.Length >= 2)
            {
                var hBook = horse.AddComponent<SpriteFlipbook>();
                // Koşu efekti: 2 kare ard arda (her zaman koşuyor → idle seti olarak ata).
                SerializedSet(hBook, "frames", horseRun);
                SerializedSet(hBook, "frameInterval", 0.16f); // dörtnal temposu
            }

            var hrb = horse.AddComponent<Rigidbody2D>();
            hrb.gravityScale = 3.5f;
            hrb.freezeRotation = true;
            hrb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hrb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var hcol = horse.AddComponent<BoxCollider2D>();
            hcol.sharedMaterial = noFriction;
            if (hasHorseArt)
            {
                // Gövdeye göre hitbox (at.png dolu alanı ~65x59 px @40ppu).
                hcol.size = new Vector2(1.5f, 1.45f);
                hcol.offset = new Vector2(-0.13f, -0.05f);
            }

            var hFeet = new GameObject("GroundCheck");
            hFeet.transform.SetParent(horse.transform, false);
            hFeet.transform.localPosition = new Vector3(0, hasHorseArt ? -0.82f : -0.55f, 0);

            var hc = horse.AddComponent<HorseController>();
            SerializedSet(hc, "groundCheck", hFeet.transform);
            SerializedSet(hc, "groundLayer", (LayerMask)(1 << GroundLayer));
            // 2 blokluk hendeği rahat aşsın (13 → sınırda kalıyordu).
            SerializedSet(hc, "jumpForce", 14.5f);

            // Boşluğa düşünce ölüm → sahne baştan.
            horse.AddComponent<FallDeath>();

            var hAtk = horse.AddComponent<SwordAttack>();
            SerializedSet(hAtk, "targetLayers", (LayerMask)(1 << EnemyLayer));
            SerializedSet(hAtk, "hitboxOffset", new Vector2(1.2f, 0f));

            // Savurma efekti at için de
            Sprite slash = GetOrCreateCharacterSprite(SlashArcPath);
            if (slash != null)
            {
                var slashGo = new GameObject("SlashVisual");
                slashGo.transform.SetParent(horse.transform, false);
                slashGo.transform.localPosition = new Vector3(1.2f, 0f, 0f);
                var ssr2 = slashGo.AddComponent<SpriteRenderer>();
                ssr2.sprite = slash;
                ssr2.sortingOrder = 12;
                slashGo.SetActive(false);
                SerializedSet(hAtk, "slashVisual", slashGo);
            }

            horse.AddComponent<ScoreHUD>();

            EditorSceneManager.MarkSceneDirty(scene);
            System.IO.Directory.CreateDirectory(Application.dataPath + "/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, DayScenePath);

            // Sahneyi Build Settings'e ekle (FallDeath'in LoadScene'i için gerekli).
            RegisterSceneInBuildSettings(DayScenePath);

            Debug.Log("[Ulak] Gündüz at sahnesi kuruldu → " + DayScenePath);
        }

        // ---- Sahneyi Build Settings listesine ekle (yoksa) ----
        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---- Gökyüzü dekoru: bulutlar (gece+gündüz) ve kuşlar (sadece gündüz) ----
        // Additive: sahnelerdeki mevcut objelere dokunmaz, yalnızca SkyDecor
        // grubunu (varsa eskisini silip) yeniden ekler. Yeniden çalıştırılabilir.
        private const string NightScenePath = "Assets/_Project/Scenes/Road_Greybox.unity";

        public static string ScatterSkyDecor()
        {
            EditorSceneManager.SaveOpenScenes();
            int night = DecorateScene(NightScenePath, -15f, 45f, withBirds: false);
            int day = DecorateScene(DayScenePath, -15f, 385f, withBirds: true);
            return "gece dekor=" + night + " gunduz dekor=" + day;
        }

        private static int DecorateScene(string scenePath, float xMin, float xMax, bool withBirds)
        {
            if (!System.IO.File.Exists(scenePath)) return -1;
            var scene = EditorSceneManager.OpenScene(scenePath);

            // Eski dekoru kaldır (yeniden çalıştırılabilirlik).
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "SkyDecor")
                    Object.DestroyImmediate(root);

            var parent = new GameObject("SkyDecor");

            var clouds = new Sprite[6];
            for (int i = 0; i < 6; i++)
                clouds[i] = GetOrCreateSprite(
                    $"Assets/_Project/Art/Environment/Sky/cloud{i + 1}.png", 32f);

            int made = 0;
            int count = Mathf.RoundToInt((xMax - xMin) / 13f);
            for (int i = 0; i < count; i++)
            {
                var spr = clouds[i % 6];
                if (spr == null) continue;

                // Deterministik ama düzensiz görünümlü yerleşim.
                float x = xMin + i * 13f + ((i * 37) % 9) - 4f;
                float y = 1.3f + ((i * 53) % 28) / 10f;   // 1.3 .. 4.0
                float s = 0.8f + ((i * 29) % 7) / 10f;    // 0.8 .. 1.4

                var go = new GameObject("Cloud_" + i);
                go.transform.SetParent(parent.transform);
                go.transform.position = new Vector3(x, y, 0f);
                go.transform.localScale = new Vector3(s, s, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sortingOrder = -90; // gökyüzünün önü, oyunun arkası
                made++;
            }

            if (withBirds)
            {
                var birds = GetOrCreateSprite("Assets/_Project/Art/Environment/Sky/birds1.png", 16f);
                if (birds != null)
                {
                    int flocks = Mathf.Max(1, count / 3);
                    for (int i = 0; i < flocks; i++)
                    {
                        float x = xMin + 10f + i * 40f + ((i * 31) % 11);
                        float y = 2.4f + ((i * 47) % 16) / 10f; // 2.4 .. 3.9
                        var go = new GameObject("Birds_" + i);
                        go.transform.SetParent(parent.transform);
                        go.transform.position = new Vector3(x, y, 0f);
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = birds;
                        sr.sortingOrder = -75; // bulutların önünde
                        made++;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return made;
        }

        // ---- Tilki yoldaşı gündüz sahnesine additive ekle ----
        public static string IntegrateFox()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("Road_Day_Greybox"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(DayScenePath);
            }

            // 192x64 sheet = 6 sütun x 2 satır (32x32). Alt satır (rect y=0) = koşu.
            Sprite[] all = GetOrCreateGridSprites(
                "Assets/_Project/Art/Characters/Fox/tilki.png", 32, 32, 32f, "tilki");
            if (all == null) return "tilki sheet yok";

            var run = all.Where(s => s.name.StartsWith("tilki_r0_")).OrderBy(s => s.name).ToArray();
            if (run.Length < 2) return "kosu karesi eksik: " + run.Length;

            // Eski tilkiyi kaldır (yeniden çalıştırılabilir).
            var old = GameObject.Find("FoxCompanion");
            if (old != null) Object.DestroyImmediate(old);

            var fox = new GameObject("FoxCompanion");
            var sr = fox.AddComponent<SpriteRenderer>();
            sr.sprite = run[0];
            sr.sortingOrder = 9; // atın hemen arkasında

            var book = fox.AddComponent<SpriteFlipbook>();
            SerializedSet(book, "frames", run);
            SerializedSet(book, "frameInterval", 0.1f); // tilki çevik

            fox.AddComponent<FoxCompanion>();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "tilki eklendi, kosu karesi=" + run.Length;
        }

        // ================= KÖY SAHNESİ: UMAY KÖY =================
        private const string UmayKoyScenePath = "Assets/_Project/Scenes/UmayKoy.unity";
        private const string TentPath = "Assets/_Project/Art/Environment/Village/large_tent.png";
        private const string UnarmedIdle0Path = "Assets/_Project/Art/Characters/Player/player_unarmed_0.png";
        private const string UnarmedIdle1Path = "Assets/_Project/Art/Characters/Player/player_unarmed_1.png";

        [MenuItem("Ulak/Umay Koy Sahnesi Kur")]
        public static void BuildUmayKoy()
        {
            if (System.IO.File.Exists(UmayKoyScenePath) &&
                !EditorUtility.DisplayDialog("Ulak — DİKKAT",
                    "UmayKoy.unity zaten var.\n\nÜzerine yazarsan elle yaptığın TÜM düzenlemeler silinir!",
                    "Üzerine Yaz", "İptal"))
                return;

            BuildUmayKoyCore();

            EditorUtility.DisplayDialog("Ulak",
                "Umay Köy sahnesi kuruldu.\n\n• Silahsız karakter (A/D hareket, zıplama)\n• Düz çimenli zemin\n• 4 çadır — Hierarchy'den taşıyıp çoğaltarak köyü kur\n\nDüzenlemeler sende!",
                "Tamam");
        }

        /// <summary>Köy sahnesini onaysız kurar (programatik çağrılar için).</summary>
        public static void BuildUmayKoyCore()
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
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.9f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10);
            var follow = camGo.AddComponent<CameraFollow>();
            SerializedSet(follow, "offset", new Vector3(2f, 0f, -10f));

            // --- Gökyüzü: sabit gündüz ---
            BuildSky(cam);
            var skyGo = GameObject.Find("Sky");
            if (skyGo != null)
                SerializedSet(skyGo.GetComponent<SkyBackground>(), "sunriseDuration", 0f);

            // --- Düz zemin: çimenli karo, TiledBoxSync'li (elle uzatılabilir) ---
            Sprite grass = GetOrCreateTileSprite("Assets/_Project/Art/Environment/Tiles/grass_ground.jpeg");
            var ground = new GameObject("Ground");
            ground.layer = GroundLayer;
            ground.isStatic = true;
            ground.transform.position = new Vector2(30f, -3f);
            var gsr = ground.AddComponent<SpriteRenderer>();
            if (grass != null)
            {
                gsr.sprite = grass;
                gsr.drawMode = SpriteDrawMode.Tiled;
                gsr.size = new Vector2(120f, 1f);
            }
            else
            {
                gsr.sprite = sq;
                gsr.color = new Color(0.35f, 0.5f, 0.25f);
                ground.transform.localScale = new Vector3(120f, 1f, 1f);
            }
            var gcol = ground.AddComponent<BoxCollider2D>();
            gcol.size = new Vector2(120f, 1f);
            ground.AddComponent<TiledBoxSync>();

            // --- Çadırlar: dekoratif nesneler (kullanıcı taşıyıp çoğaltacak) ---
            Sprite tent = GetOrCreateSprite(TentPath, 48f); // 96x128 → 2 x 2.67 birim
            if (tent != null)
            {
                float[] tentX = { 6f, 14f, 22f, 30f };
                for (int i = 0; i < tentX.Length; i++)
                {
                    var t = new GameObject("Tent_" + i);
                    t.transform.position = new Vector2(tentX[i], -1.17f); // zemine oturur
                    var tsr = t.AddComponent<SpriteRenderer>();
                    tsr.sprite = tent;
                    tsr.sortingOrder = 1; // oyuncunun arkasında
                }
            }
            else
            {
                Debug.LogWarning("[Ulak] Çadır görseli yüklenemedi: " + TentPath);
            }

            // --- Silahsız oyuncu ---
            Sprite u0 = GetOrCreateCharacterSprite(UnarmedIdle0Path);
            Sprite u1 = GetOrCreateCharacterSprite(UnarmedIdle1Path);
            bool hasArt = u0 != null;

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector2(0f, -1.7f);

            var psr = player.AddComponent<SpriteRenderer>();
            psr.sprite = hasArt ? u0 : sq;
            psr.color = hasArt ? Color.white : new Color(0.3f, 0.6f, 1f);
            psr.sortingOrder = 10;

            if (hasArt && u1 != null)
            {
                var book = player.AddComponent<SpriteFlipbook>();
                SerializedSet(book, "frames", new[] { u0, u1 });
            }

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = player.AddComponent<BoxCollider2D>();
            col.sharedMaterial = noFriction;
            if (hasArt) col.size = new Vector2(0.47f, 1.5f);

            var feet = new GameObject("GroundCheck");
            feet.transform.SetParent(player.transform, false);
            feet.transform.localPosition = new Vector3(0, -0.78f, 0);

            var pc = player.AddComponent<PlayerController>();
            SerializedSet(pc, "groundCheck", feet.transform);
            SerializedSet(pc, "groundLayer", (LayerMask)(1 << GroundLayer));
            // Köyde kılıç yok: SwordAttack / Health / KillCharges bilerek eklenmedi.

            EditorSceneManager.MarkSceneDirty(scene);
            System.IO.Directory.CreateDirectory(Application.dataPath + "/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, UmayKoyScenePath);
            RegisterSceneInBuildSettings(UmayKoyScenePath);

            Debug.Log("[Ulak] Umay Köy sahnesi kuruldu → " + UmayKoyScenePath);
        }

        // ---- 3 otağ çadırını Umay Köy sahnesine additive ekle ----
        public static string PlaceYurts()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("UmayKoy"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(UmayKoyScenePath);
            }

            int made = 0;
            for (int i = 1; i <= 3; i++)
            {
                string name = "Yurt_" + i;
                if (GameObject.Find(name) != null) continue; // varsa dokunma

                Sprite spr = GetOrCreateSprite(
                    $"Assets/_Project/Art/Environment/Village/cadir{i}.png", 48f);
                if (spr == null) continue;

                var go = new GameObject(name);
                // 256x160 @48ppu → 5.3 x 3.3 birim; zemin üstüne oturt.
                go.transform.position = new Vector2(34f + i * 8f, -0.83f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sortingOrder = 1; // oyuncunun arkasında
                made++;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "eklenen otag: " + made;
        }

        // ---- Canavar türlerini gece sahnesine ŞABLON olarak ekle (additive) ----
        // Karakura: çevik yakın dövüşçü (mor) | Tepegöz: ağır tank (yeşil, iri)
        // Merküt: uçan okçu (mavi-gri, havada). Kullanıcı Ctrl+D ile çoğaltır.
        public static string PlaceMonsterTypes()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("Road_Greybox"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(NightScenePath);
            }

            EnsureLayers();
            Sprite sq = GetOrCreateSquareSprite();
            PhysicsMaterial2D noFriction = GetOrCreateNoFrictionMaterial();

            var player = GameObject.Find("Player");
            float px = player != null ? player.transform.position.x : 0f;
            float py = player != null ? player.transform.position.y : -1.8f;

            var sb = new System.Text.StringBuilder();

            // --- KARAKURA: hızlı, saldırgan yakın dövüşçü ---
            if (GameObject.Find("Karakura") == null)
            {
                var go = MakeMonsterBase("Karakura", sq, noFriction,
                    new Vector2(px + 8f, py), new Color(0.6f, 0.2f, 0.75f),
                    new Vector3(0.9f, 0.9f, 1f), maxHealth: 2, gravity: 3.5f);
                var ai = go.AddComponent<SmallEnemyAI>();
                SerializedSet(ai, "moveSpeed", 3.6f);       // çevik
                SerializedSet(ai, "patrolSpeed", 1.6f);
                SerializedSet(ai, "aggroRange", 9f);
                SerializedSet(ai, "loseAggroRange", 14f);   // ısrarcı
                SerializedSet(ai, "contactCooldown", 0.5f); // sık vurur
                go.AddComponent<EnemyDeath>();
                sb.Append("Karakura ");
            }

            // --- TEPEGÖZ: yavaş, dayanıklı, sert vuran tank ---
            if (GameObject.Find("Tepegoz") == null)
            {
                var go = MakeMonsterBase("Tepegoz", sq, noFriction,
                    new Vector2(px + 14f, py + 0.4f), new Color(0.3f, 0.5f, 0.25f),
                    new Vector3(1.6f, 1.6f, 1f), maxHealth: 8, gravity: 3.5f);
                go.GetComponent<Rigidbody2D>().mass = 3f; // knockback direnci
                var ai = go.AddComponent<SmallEnemyAI>();
                SerializedSet(ai, "moveSpeed", 1.2f);       // hantal
                SerializedSet(ai, "patrolSpeed", 0.6f);
                SerializedSet(ai, "aggroRange", 6f);
                SerializedSet(ai, "contactDamage", 2);      // sert vurur
                SerializedSet(ai, "contactKnockback", 12f);
                SerializedSet(ai, "jumpForce", 7f);         // ağır, alçak zıplar
                go.AddComponent<EnemyDeath>();
                sb.Append("Tepegoz ");
            }

            // --- MERKÜT: uçan, uzaktan ok atan ---
            if (GameObject.Find("Merkut") == null)
            {
                var go = MakeMonsterBase("Merkut", sq, noFriction,
                    new Vector2(px + 20f, py + 3.5f), new Color(0.45f, 0.6f, 0.85f),
                    new Vector3(0.8f, 0.8f, 1f), maxHealth: 2, gravity: 0f);
                var ai = go.AddComponent<MerkutAI>();
                SerializedSet(ai, "projectileSprite", sq);
                go.AddComponent<EnemyDeath>();
                sb.Append("Merkut ");
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "eklenen: " + (sb.Length > 0 ? sb.ToString() : "(hepsi zaten vardı)");
        }

        // ---- Eski kırmızı Enemy_Small küplerini 3 yeni türe dönüştür ----
        // Her düşman kendi konumunda kalır; sırayla Karakura → Tepegöz → Merküt
        // dağıtılır (Merküt havaya alınır). Haritadaki başka hiçbir şeye dokunmaz.
        public static string ReplaceOldEnemies()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("Road_Greybox"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(NightScenePath);
            }

            EnsureLayers();
            Sprite sq = GetOrCreateSquareSprite();
            PhysicsMaterial2D noFriction = GetOrCreateNoFrictionMaterial();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var olds = scene.GetRootGameObjects()
                .Where(g => g.name.StartsWith("Enemy_Small"))
                .ToArray();
            if (olds.Length == 0) return "Enemy_Small bulunamadı";

            int k = 0, tp = 0, mk = 0, i = 0;
            foreach (var old in olds)
            {
                Vector2 pos = old.transform.position;
                Object.DestroyImmediate(old);

                switch (i % 3)
                {
                    case 0: SpawnKarakura(pos, sq, noFriction); k++; break;
                    case 1: SpawnTepegoz(pos + Vector2.up * 0.35f, sq, noFriction); tp++; break;
                    default: SpawnMerkut(new Vector2(pos.x, pos.y + 3f), sq, noFriction); mk++; break;
                }
                i++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"donusturulen {olds.Length}: Karakura={k} Tepegoz={tp} Merkut={mk}";
        }

        // ---- Tür üreticileri ----
        private static GameObject SpawnKarakura(Vector2 pos, Sprite sq, PhysicsMaterial2D nf)
        {
            var go = MakeMonsterBase("Karakura", sq, nf, pos,
                new Color(0.6f, 0.2f, 0.75f), new Vector3(0.9f, 0.9f, 1f), 2, 3.5f);
            var ai = go.AddComponent<SmallEnemyAI>();
            SerializedSet(ai, "moveSpeed", 3.6f);
            SerializedSet(ai, "patrolSpeed", 1.6f);
            SerializedSet(ai, "aggroRange", 9f);
            SerializedSet(ai, "loseAggroRange", 14f);
            SerializedSet(ai, "contactCooldown", 0.5f);
            go.AddComponent<EnemyDeath>();
            return go;
        }

        private static GameObject SpawnTepegoz(Vector2 pos, Sprite sq, PhysicsMaterial2D nf)
        {
            var go = MakeMonsterBase("Tepegoz", sq, nf, pos,
                new Color(0.3f, 0.5f, 0.25f), new Vector3(1.6f, 1.6f, 1f), 8, 3.5f);
            go.GetComponent<Rigidbody2D>().mass = 3f;
            var ai = go.AddComponent<SmallEnemyAI>();
            SerializedSet(ai, "moveSpeed", 1.2f);
            SerializedSet(ai, "patrolSpeed", 0.6f);
            SerializedSet(ai, "aggroRange", 6f);
            SerializedSet(ai, "contactDamage", 2);
            SerializedSet(ai, "contactKnockback", 12f);
            SerializedSet(ai, "jumpForce", 7f);
            go.AddComponent<EnemyDeath>();
            return go;
        }

        private static GameObject SpawnMerkut(Vector2 pos, Sprite sq, PhysicsMaterial2D nf)
        {
            var go = MakeMonsterBase("Merkut", sq, nf, pos,
                new Color(0.45f, 0.6f, 0.85f), new Vector3(0.8f, 0.8f, 1f), 2, 0f);
            var ai = go.AddComponent<MerkutAI>();
            SerializedSet(ai, "projectileSprite", sq);
            go.AddComponent<EnemyDeath>();
            return go;
        }

        // Ortak canavar gövdesi: sprite + fizik + can + bar + efektler.
        private static GameObject MakeMonsterBase(string name, Sprite sq,
            PhysicsMaterial2D noFriction, Vector2 pos, Color color,
            Vector3 scale, int maxHealth, float gravity)
        {
            var go = new GameObject(name);
            go.layer = EnemyLayer;
            go.transform.position = pos;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sq;
            sr.color = color;
            sr.sortingOrder = 9;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = gravity;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.sharedMaterial = noFriction;

            go.AddComponent<Knockback>();
            go.AddComponent<DamageFlash>();

            var health = go.AddComponent<Health>();
            SerializedSet(health, "maxHealth", maxHealth);
            go.AddComponent<HealthBar>();

            return go;
        }

        // ---- Gece sahnesi zemin/engellerini karo dokulara çevir (additive) ----
        // Mevcut objelerin DÜNYA BOYUTLARINI aynen korur; sadece görseli
        // Tiled moda çevirir (doku gerilmez, karo karo tekrarlar) ve
        // TiledBoxSync ekler (sonraki elle uzatmalarda yamulma sigortası).
        public static string ApplyNightTiles()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("Road_Greybox"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(NightScenePath);
            }

            Sprite grass = GetOrCreateTileSprite("Assets/_Project/Art/Environment/Tiles/grass_ground.jpeg");
            Sprite dirt = GetOrCreateTileSprite("Assets/_Project/Art/Environment/Tiles/ground.jpeg");
            if (grass == null || dirt == null) return "karo sprite eksik";

            int nGround = 0, nObstacle = 0;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                bool isGround = root.name.StartsWith("Ground");
                bool isObstacle = root.name.StartsWith("Obstacle") || root.name.StartsWith("Wall");
                if (!isGround && !isObstacle) continue;

                var sr = root.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                // Mevcut dünya boyutunu koru (kullanıcının uzattığı ölçüler).
                Vector2 worldSize = sr.bounds.size;

                sr.sprite = isGround ? grass : dirt;
                sr.color = Color.white;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = worldSize;
                root.transform.localScale = Vector3.one;

                var col = root.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = worldSize;
                    col.offset = Vector2.zero;
                }

                if (root.GetComponent<TiledBoxSync>() == null)
                    root.AddComponent<TiledBoxSync>();

                if (isGround) nGround++; else nObstacle++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "zemin=" + nGround + " engel=" + nObstacle + " karoya cevrildi";
        }

        // ---- İki karo küpünü ŞABLON olarak sahneye ekle (elle tasarım için) ----
        // Var olan hiçbir objeye dokunmaz; spawn yakınına 1x1 iki küp koyar.
        // Kullanıcı Ctrl+D ile çoğaltıp uzatarak bölümü kendisi kurar;
        // TiledBoxSync sayesinde uzatınca doku karo karo çoğalır, hitbox uyar.
        public static string PlaceTileTemplates()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("Road_Greybox"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(NightScenePath);
            }

            Sprite grass = GetOrCreateTileSprite("Assets/_Project/Art/Environment/Tiles/grass_ground.jpeg");
            Sprite dirt = GetOrCreateTileSprite("Assets/_Project/Art/Environment/Tiles/ground.jpeg");
            if (grass == null || dirt == null) return "karo sprite eksik";

            MakeTileCube("Tile_GrassGround", grass, new Vector2(-6f, -1f));
            MakeTileCube("Tile_Ground", dirt, new Vector2(-8f, -1f));

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "2 karo kup eklendi (Tile_GrassGround, Tile_Ground)";
        }

        private static void MakeTileCube(string name, Sprite tile, Vector2 pos)
        {
            // Aynı isimde şablon zaten varsa dokunma (yeniden çalıştırılabilir).
            if (GameObject.Find(name) != null) return;

            var go = new GameObject(name);
            go.layer = GroundLayer;
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tile;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = Vector2.one; // 1x1 küp

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            go.AddComponent<TiledBoxSync>();
        }

        // ---- Dağ arka planını Umay Köy sahnesine additive ekle ----
        public static string PlaceMountains()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("UmayKoy"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(UmayKoyScenePath);
            }

            if (GameObject.Find("Mountains") != null) return "Mountains zaten var";

            // 1280x256 @64ppu → her tekrar 20 x 4 birim; Tiled ile köy boyunca yay.
            Sprite daglar = GetOrCreateTileSprite("Assets/_Project/Art/Background/daglar.png", 64f);
            if (daglar == null) return "daglar sprite yok";

            var go = new GameObject("Mountains");
            go.transform.position = new Vector3(30f, -0.5f, 0f); // tabanı zemin üstüne (−2.5) oturur
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = daglar;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(140f, 4f);
            sr.sortingOrder = -85; // gökyüzünün önü, her şeyin arkası

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "daglar eklendi";
        }

        // ---- Umay Köy arka planını (dağ + gökyüzü + kamera) tüm sahnelere uygula ----
        public static string ApplyUmayBackdropToAll()
        {
            EditorSceneManager.SaveOpenScenes();

            // ===== 1) KAYNAK: UmayKoy'dan ayarları oku =====
            EditorSceneManager.OpenScene(UmayKoyScenePath);

            // Dağ objeleri: "daglar*" ya da "Mountains*" adlı tüm kök objeler
            // (sonsuz döngü için 2 kopyalı kurulum dahil) birebir kopyalanır.
            var srcScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var mountainSrcs = srcScene.GetRootGameObjects()
                .Where(g => g.name.StartsWith("daglar") || g.name.StartsWith("Mountains"))
                .Where(g => g.GetComponent<SpriteRenderer>() != null)
                .ToArray();
            if (mountainSrcs.Length == 0) return "UmayKoy'da dag objesi yok";

            // Sahne değişince objeler yok olur — verileri düz değerlere kopyala.
            var mData = mountainSrcs.Select(g =>
            {
                var gsr = g.GetComponent<SpriteRenderer>();
                var gpb = g.GetComponent<ParallaxBackground>();
                return new
                {
                    name = g.name,
                    pos = g.transform.position,
                    scale = g.transform.localScale,
                    sprite = gsr.sprite,
                    mode = gsr.drawMode,
                    size = gsr.drawMode != SpriteDrawMode.Simple ? gsr.size : Vector2.zero,
                    color = gsr.color,
                    order = gsr.sortingOrder,
                    pbFactor = gpb != null ? gpb.parallaxEffect : float.NaN
                };
            }).ToArray();

            var camSrcGo = GameObject.FindGameObjectWithTag("MainCamera");
            var camSrc = camSrcGo.GetComponent<Camera>();
            float orthoSize = camSrc.orthographicSize;
            Color camBg = camSrc.backgroundColor;
            Vector3 camPos = camSrcGo.transform.position;
            var cfSrc = camSrcGo.GetComponent<CameraFollow>();
            object cfOffset = cfSrc != null ? GetFieldValue(cfSrc, "offset") : null;
            object cfSmooth = cfSrc != null ? GetFieldValue(cfSrc, "smoothTime") : null;
            object cfFollowY = cfSrc != null ? GetFieldValue(cfSrc, "followY") : null;

            var skySrc = GameObject.Find("Sky");
            Sprite skySprite = null;
            object skyDur = null;
            if (skySrc != null)
            {
                skySprite = skySrc.GetComponent<SpriteRenderer>().sprite;
                var sbSrc = skySrc.GetComponent<SkyBackground>();
                if (sbSrc != null) skyDur = GetFieldValue(sbSrc, "sunriseDuration");
            }

            // ===== 2) HEDEF SAHNELERE UYGULA =====
            string[] targets =
            {
                NightScenePath,
                DayScenePath,
                "Assets/Scenes/BossFightArea.unity",
                "Assets/Scenes/umay_koy.unity"
            };

            var log = new System.Text.StringBuilder();
            foreach (string path in targets)
            {
                if (!System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path);

                var camGo = GameObject.FindGameObjectWithTag("MainCamera");
                Camera cam = camGo != null ? camGo.GetComponent<Camera>() : null;

                // --- Kamera: Umay ayarları ---
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = orthoSize;
                    cam.backgroundColor = camBg;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    camGo.transform.position = camPos;

                    var cf = camGo.GetComponent<CameraFollow>();
                    if (cf == null) cf = camGo.AddComponent<CameraFollow>();
                    if (cfOffset != null) SerializedSet(cf, "offset", cfOffset);
                    if (cfSmooth != null) SerializedSet(cf, "smoothTime", cfSmooth);
                    if (cfFollowY != null) SerializedSet(cf, "followY", cfFollowY);
                }

                // --- Dağlar: eski dağ objelerini temizle, kaynaktakileri birebir kur ---
                foreach (var oldM in scene.GetRootGameObjects()
                             .Where(g => g.name.StartsWith("daglar") || g.name.StartsWith("Mountains"))
                             .ToArray())
                    Object.DestroyImmediate(oldM);

                foreach (var d in mData)
                {
                    var m = new GameObject(d.name);
                    var sr = m.AddComponent<SpriteRenderer>();
                    sr.sprite = d.sprite;
                    sr.drawMode = d.mode;
                    if (d.mode != SpriteDrawMode.Simple) sr.size = d.size;
                    sr.color = d.color;
                    sr.sortingOrder = d.order;
                    m.transform.position = d.pos;
                    m.transform.localScale = d.scale;
                    if (!float.IsNaN(d.pbFactor))
                    {
                        var pb = m.AddComponent<ParallaxBackground>();
                        pb.parallaxEffect = d.pbFactor;
                        if (camGo != null) pb.cam = camGo;
                    }
                }

                // --- Gökyüzü: yoksa Umay'ınkiyle kur; varsa görseline dokunma ---
                var sky = GameObject.Find("Sky");
                if (sky == null && skySprite != null)
                {
                    sky = new GameObject("Sky");
                    var ssr = sky.AddComponent<SpriteRenderer>();
                    ssr.sprite = skySprite;
                    ssr.sortingOrder = -100;
                    var sb = sky.AddComponent<SkyBackground>();
                    if (cam != null) SerializedSet(sb, "targetCamera", cam);
                    if (skyDur != null) SerializedSet(sb, "sunriseDuration", skyDur);
                }
                // Not: VAR OLAN Sky'ın sunriseDuration'ına bilerek dokunulmuyor —
                // gece sahnesinin gün doğumu akışı tasarım gereği korunur.

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.Append(scene.name).Append(" ");
            }

            // Kaynak sahneye geri dön.
            EditorSceneManager.OpenScene(UmayKoyScenePath);
            return "uygulandi: " + log;
        }

        // ---- Reflection ile private alan oku (SerializedSet'in tersi) ----
        private static object GetFieldValue(Object target, string field)
        {
            for (var t = target.GetType(); t != null; t = t.BaseType)
            {
                var fi = t.GetField(field,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (fi != null) return fi.GetValue(target);
            }
            return null;
        }

        // ---- Karo sprite: ppu ayarlı + FullRect mesh (Tiled için) ----
        private static Sprite GetOrCreateTileSprite(string path, float ppu = 65f)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogWarning("[Ulak] Karo bulunamadı: " + path);
                return null;
            }

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            bool needsSetup = imp.textureType != TextureImporterType.Sprite
                              || imp.spriteImportMode != SpriteImportMode.Single
                              || !Mathf.Approximately(imp.spritePixelsPerUnit, ppu)
                              || settings.spriteMeshType != SpriteMeshType.FullRect;
            if (needsSetup)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = ppu;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;

                imp.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; // Tiled mod şartı
                imp.SetTextureSettings(settings);

                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---- Yoldaşın görselini dost kurtla değiştir (additive) ----
        public static string IntegrateWolf()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("Road_Day_Greybox"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(DayScenePath);
            }

            Sprite[] all = GetOrCreateGridSprites(
                "Assets/_Project/Art/Characters/Wolf/kurt_dost.png", 32, 32, 32f, "kurt");
            if (all == null) return "kurt sheet yok";

            var run = all.Where(s => s.name.StartsWith("kurt_r0_")).OrderBy(s => s.name).ToArray();
            if (run.Length < 2) return "kosu karesi eksik: " + run.Length;

            // Mevcut yoldaşı bul (tilki ya da kurt adıyla).
            var companion = GameObject.Find("FoxCompanion");
            if (companion == null) companion = GameObject.Find("WolfCompanion");
            if (companion == null) return "yoldas objesi yok";

            companion.name = "WolfCompanion";
            var sr = companion.GetComponent<SpriteRenderer>();
            sr.sprite = run[0];

            var book = companion.GetComponent<SpriteFlipbook>();
            SerializedSet(book, "frames", run);

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "kurt entegre, kosu karesi=" + run.Length;
        }

        // ---- Genel ızgara dilimleyici: sütun x satır eşit kareler ----
        // İsimlendirme: prefix_r{satır}_c{sütun} — r0 = dokunun EN ALT satırı.
        public static Sprite[] GetOrCreateGridSprites(
            string path, int frameW, int frameH, float ppu, string namePrefix)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogWarning("[Ulak] Sheet bulunamadı: " + path);
                return null;
            }

            bool needsSetup = imp.textureType != TextureImporterType.Sprite
                              || imp.spriteImportMode != SpriteImportMode.Multiple
                              || !Mathf.Approximately(imp.spritePixelsPerUnit, ppu);
            if (needsSetup)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Multiple;
                imp.spritePixelsPerUnit = ppu;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;

                int texW, texH;
                imp.GetSourceTextureWidthAndHeight(out texW, out texH);
                int cols = Mathf.Max(1, texW / frameW);
                int rows = Mathf.Max(1, texH / frameH);

                var factory = new SpriteDataProviderFactories();
                factory.Init();
                var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
                dp.InitSpriteEditorDataProvider();

                var rects = new List<SpriteRect>();
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        rects.Add(new SpriteRect
                        {
                            name = $"{namePrefix}_r{r}_c{c}",
                            rect = new Rect(c * frameW, r * frameH, frameW, frameH),
                            alignment = SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f),
                            spriteID = GUID.Generate()
                        });
                    }
                dp.SetSpriteRects(rects.ToArray());

                var nameFileId = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
                if (nameFileId != null)
                    nameFileId.SetNameFileIdPairs(
                        rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)).ToList());

                dp.Apply();
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
        }

        // ---- At görsellerini AÇIK gündüz sahnesine additive uygula ----
        // (Sahneyi yeniden kurmaz; mevcut Horse objesini günceller.)
        public static string IntegrateHorseArt()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.path.Contains("Road_Day_Greybox"))
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(DayScenePath);
            }

            Sprite idle = GetOrCreateSprite(HorseIdlePath, 40f);
            Sprite[] run = GetOrCreateSheetSprites(HorseRunPath, 80, 64, 40f, "horse_run");
            if (idle == null || run == null || run.Length < 2)
                return "sprite eksik: idle=" + (idle != null) + " run=" + (run?.Length ?? 0);

            var horse = GameObject.Find("Horse");
            if (horse == null) return "Horse objesi yok";

            horse.transform.localScale = Vector3.one;

            var sr = horse.GetComponent<SpriteRenderer>();
            sr.sprite = idle;
            sr.color = Color.white;

            var col = horse.GetComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 1.45f);
            col.offset = new Vector2(-0.13f, -0.05f);

            var feet = horse.transform.Find("GroundCheck");
            if (feet != null) feet.localPosition = new Vector3(0f, -0.82f, 0f);

            var book = horse.GetComponent<SpriteFlipbook>();
            if (book == null) book = horse.AddComponent<SpriteFlipbook>();
            SerializedSet(book, "frames", run);
            SerializedSet(book, "frameInterval", 0.16f);

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "at entegre edildi, kosu karesi=" + run.Length;
        }

        // ---- Altın kutu (kılıçla kırılır, çarpınca etkisiz) ----
        private static void BuildGoldBox(Sprite sq, Vector2 pos)
        {
            var go = new GameObject("GoldBox");
            go.layer = EnemyLayer; // SwordAttack'ın hedef katmanı
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sq;
            sr.color = new Color(1f, 0.84f, 0.2f); // altın sarısı
            sr.sortingOrder = 5;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; // ata çarpmaz, içinden geçilir

            go.AddComponent<GoldBox>();
        }

        // ---- Oyuncu kurulumu ----
        private static GameObject BuildPlayer(Sprite sq, PhysicsMaterial2D noFriction)
        {
            // Karakter görselleri: 32x48 px, 32 ppu → 1 x 1.5 dünya birimi.
            Sprite idle0 = GetOrCreateCharacterSprite(PlayerIdle0Path);
            Sprite idle1 = GetOrCreateCharacterSprite(PlayerIdle1Path);
            bool hasArt = idle0 != null;

            var go = new GameObject("Player");
            go.tag = "Player";
            go.transform.position = new Vector2(-3f, -1.7f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = hasArt ? idle0 : sq;
            sr.color = hasArt ? Color.white : new Color(0.3f, 0.6f, 1f);
            sr.sortingOrder = 10;

            if (hasArt && idle1 != null)
            {
                var book = go.AddComponent<SpriteFlipbook>();
                SerializedSet(book, "frames", new[] { idle0, idle1 });

                // Yürüme kareleri (2'li sprite sheet'ten dilimlenir)
                Sprite[] walk = GetOrCreateWalkSprites();
                if (walk != null && walk.Length >= 2)
                    SerializedSet(book, "walkFrames", walk);
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Sıfır sürtünme: duvara dayanırken yapışıp kalmayı önler (zıplayarak aşılabilir).
            // Hitbox görselin dolu alanına göre: 15 px genişlik, 48 px boy (32 ppu).
            var col = go.AddComponent<BoxCollider2D>();
            col.sharedMaterial = noFriction;
            if (hasArt)
                col.size = new Vector2(0.47f, 1.5f);

            go.AddComponent<Knockback>();
            go.AddComponent<DamageFlash>();

            var health = go.AddComponent<Health>();
            SerializedSet(health, "maxHealth", 5);
            var bar = go.AddComponent<HealthBar>();
            if (hasArt) // bar, 1.5 birimlik karakterin tepesinin üstünde dursun
                SerializedSet(bar, "offset", new Vector2(0f, 1.05f));

            // Ayak hizası zemin kontrol noktası (sprite altı: -0.75)
            var feet = new GameObject("GroundCheck");
            feet.transform.SetParent(go.transform);
            feet.transform.localPosition = new Vector3(0, hasArt ? -0.78f : -0.55f, 0);

            var pc = go.AddComponent<PlayerController>();
            SerializedSet(pc, "groundCheck", feet.transform);
            SerializedSet(pc, "groundLayer", (LayerMask)(1 << GroundLayer));

            var atk = go.AddComponent<SwordAttack>();
            SerializedSet(atk, "targetLayers", (LayerMask)(1 << EnemyLayer));

            // Kılıç savurma efekti (hilal yay — saldırı anında kısa süre parlar)
            Sprite slashSprite = GetOrCreateCharacterSprite(SlashArcPath);
            if (slashSprite != null)
            {
                var slash = new GameObject("SlashVisual");
                slash.transform.SetParent(go.transform, false);
                slash.transform.localPosition = new Vector3(0.9f, 0f, 0f);
                var ssr = slash.AddComponent<SpriteRenderer>();
                ssr.sprite = slashSprite;
                ssr.sortingOrder = 12; // karakterin önünde
                slash.SetActive(false);
                SerializedSet(atk, "slashVisual", slash);
            }

            go.AddComponent<PlayerRespawn>();
            go.AddComponent<KillCharges>(); // can basma mekaniği (sol üst sayaç + H ile yenile)

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

        // ---- Tek sprite: pixel-art importer ayarlarıyla yükle ----
        private static Sprite GetOrCreateCharacterSprite(string path) => GetOrCreateSprite(path, 32f);

        private static Sprite GetOrCreateSprite(string path, float ppu)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogWarning("[Ulak] Görsel bulunamadı: " + path);
                return null;
            }

            // Pixel-art için doğru ayarlar (idempotent — değişmişse güncelle).
            if (imp.textureType != TextureImporterType.Sprite ||
                imp.spriteImportMode != SpriteImportMode.Single ||
                !Mathf.Approximately(imp.spritePixelsPerUnit, ppu) ||
                imp.filterMode != FilterMode.Point)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single; // şablon varsayılanı Multiple+auto-slice yapabiliyor
                imp.spritePixelsPerUnit = ppu;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---- Oyuncu yürüme kareleri ----
        private static Sprite[] GetOrCreateWalkSprites()
            => GetOrCreateSheetSprites(PlayerWalkPath, 64, 48, 32f, "player_walk");

        // ---- Genel sheet dilimleyici: yatay dizilmiş eşit kareler ----
        public static Sprite[] GetOrCreateSheetSprites(
            string path, int frameW, int frameH, float ppu, string namePrefix)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogWarning("[Ulak] Sheet bulunamadı: " + path);
                return null;
            }

            bool needsSetup = imp.textureType != TextureImporterType.Sprite
                              || imp.spriteImportMode != SpriteImportMode.Multiple
                              || !Mathf.Approximately(imp.spritePixelsPerUnit, ppu);
            if (needsSetup)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Multiple;
                imp.spritePixelsPerUnit = ppu;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;

                // Kare sayısını dokunun genişliğinden türet.
                int texW = frameW, texH = frameH;
                imp.GetSourceTextureWidthAndHeight(out texW, out texH);
                int count = Mathf.Max(1, texW / frameW);

                // Modern dilimleme API'si (ISpriteEditorDataProvider).
                var factory = new SpriteDataProviderFactories();
                factory.Init();
                var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
                dp.InitSpriteEditorDataProvider();

                var rects = new SpriteRect[count];
                for (int i = 0; i < count; i++)
                {
                    rects[i] = new SpriteRect
                    {
                        name = namePrefix + "_" + i,
                        rect = new Rect(i * frameW, 0, frameW, frameH),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = GUID.Generate()
                    };
                }
                dp.SetSpriteRects(rects);

                // Unity 2021.2+ isim ↔ fileId eşlemesi ister.
                var nameFileId = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
                if (nameFileId != null)
                {
                    var pairs = rects
                        .Select(r => new SpriteNameFileIdPair(r.name, r.spriteID))
                        .ToList();
                    nameFileId.SetNameFileIdPairs(pairs);
                }

                dp.Apply();
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
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

        // ---- Reflection ile private [SerializeField] alanı doğrudan ata ----
        // Not: SerializedObject.ApplyModifiedProperties MCP üzerinden tetiklenen
        // sahne kurulumlarında güvenilmez şekilde kayboluyordu; doğrudan alan
        // ataması canlı objeye yazar ve SaveScene her zaman bu değerleri kaydeder.
        private static void SerializedSet(Object target, string field, object value)
        {
            System.Reflection.FieldInfo fi = null;
            for (var t = target.GetType(); t != null && fi == null; t = t.BaseType)
            {
                fi = t.GetField(field,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
            }

            if (fi == null)
            {
                Debug.LogWarning($"[Ulak] Alan bulunamadı: {field} ({target})");
                return;
            }

            fi.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }
    }
}
