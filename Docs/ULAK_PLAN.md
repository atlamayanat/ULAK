# ULAK — Oyun Tasarım & Geliştirme Planı

> **Tür:** Aksiyon / Pixel-Art / Lineer ilerleyen 2D yan-bakış (side-scroller)
> **Tema:** Türklük · Dayanışma · Toplanma (Toy)
> **Motor:** Unity `6000.4.9f1` · URP 2D · Input System · Unity MCP (Claude Code ile geliştirme)
> **Durum:** Planlama (v0.1) — asset'ler ve karakterler çizildikçe eklenecek
> **Son güncelleme:** 2026-06-05

---

## 0. İçindekiler
1. [Vizyon ve Konsept](#1-vizyon-ve-konsept)
2. [Oyun Akışı (İki Mod)](#2-oyun-akışı-iki-mod)
3. [Çekirdek Mekanikler](#3-çekirdek-mekanikler)
4. [Hikâye Yapısı](#4-hikâye-yapısı)
5. [Teknik Temel](#5-teknik-temel)
6. [Yazılım Mimarisi](#6-yazılım-mimarisi)
7. [Klasör & Sahne Yapısı](#7-klasör--sahne-yapısı)
8. [Asset Pipeline (Pixel Art)](#8-asset-pipeline-pixel-art)
9. [Geliştirme Pipeline'ı (Fazlar)](#9-geliştirme-pipelineı-fazlar)
10. [MCP + Claude Code İş Akışı](#10-mcp--claude-code-iş-akışı)
11. [Karar Bekleyen Noktalar & Varsayımlar](#11-karar-bekleyen-noktalar--varsayımlar)
12. [Görev Listesi (Backlog)](#12-görev-listesi-backlog)

---

## 1. Vizyon ve Konsept

**Ulak**, oyuncunun atının üstünde köyden köye giden bir ulağı (haberci/elçi) canlandırdığı, kısa ve odaklı bir aksiyon-anlatı oyunudur. Oyuncu yol boyunca kılıcıyla canavarları ve engelleri aşar; köylere vardığında NPC'lerle konuşarak insanları ortak bir amaç — **demir dağını eritip işlemek** — etrafında birleştirmeye, yani bir **toy** (toplanma/dayanışma) kurmaya çalışır.

**Duygu hedefi:** Yol = gerilim/aksiyon ritmi. Köy = nefes alma, hikâye, anlam. İkisi dönüşümlü ilerler.

**Tasarım ilkeleri:**
- **Az ama öz mekanik:** Tek silah (kılıç), net bir zamanlama-temelli dövüş.
- **Lineer ve okunabilir:** Dallanma yok; ilerleme = köy zinciri.
- **Placeholder-first:** Önce gri kutu (greybox) ile oynanış, sonra sanat. Asset gelmeden mekanik test edilebilir olmalı.

---

## 2. Oyun Akışı (İki Mod)

Oyun iki sahne türünün zincirlenmesinden oluşur:

```
[Açılış/Menü]
      │
      ▼
[KÖY 1] ──(yola çık)──► [YOL 1→2] ──(vardın)──► [KÖY 2] ──► [YOL 2→3] ──► [KÖY 3] ──► [Final/Toy]
  diyalog               aksiyon                diyalog        aksiyon       diyalog      kapanış
```

### Mod A — Yolculuk (Yol sahnesi)
- At üstünde, yandan bakışlı, **soldan sağa** lineer ilerleme.
- Kılıçla yakın dövüş, knockback, düşman dalgaları, engeller.
- Hedef: yolun sonundaki köy kapısına ulaşmak (hayatta kalarak).

### Mod B — Köy (Sosyal/anlatı sahnesi)
- Küçük, gezilebilir bir harita.
- Birkaç konuşulabilir NPC; diyalog kutularıyla hikâye anlatımı.
- Hedef: köyün anahtar NPC'sini ikna etmek → "yola çık" tetikleyicisi açılır.

---

## 3. Çekirdek Mekanikler

### 3.1 Yolculuk — Hareket  ⟶ **Otomatik koşu** (karar verildi)
- At **sürekli, sabit hızda sağa** ilerler (auto-run). Oyuncu yatay yürümeyi kontrol etmez.
- Oyuncunun işi: **zamanlama** — `Attack` (saldırı) ve `Jump` (engel aşma) doğru anda.
- `Sprint`/hız değişimi opsiyonel cila olarak kalır (örn. dramatik anlarda). Çekirdek girdi: **Saldırı + Zıpla.**
- Zemin: düz bir yol (tek seviye). Dikey platform karmaşası **yok** (v1) — odak dövüş ritmi.
- Tasarım sonucu: Düşman/engel **oyuncuya doğru akar**; oyuncu sabit hızda onlara yetişir → "geliyor, zamanla, vur" ritmi otomatik koşuyla daha keskin.

### 3.2 Yolculuk — Kılıç Dövüşü (çekirdek his)
- **Saldırı girdisi:** `Attack` aksiyonu = **Sağ tık** (karar verildi). Input map'e sağ-tık binding'i eklenecek; sol tık ileride başka işe (örn. UI/ikincil) ayrılabilir.
- Karakterin **önünde (sağında) kısa bir hasar alanı (hitbox)** belirir; sadece menzildeki düşmanlar etkilenir.
- **Knockback:** Ufak düşmanlar vurulunca geri savrulur. Oyuncu ilerledikçe tekrar yaklaşırlar → "yaklaş–zamanla–vur" ritmi doğar.
- **Zamanlama:** Düşman menzile girince doğru anda saldırı = hasar + güvenli geri itme. Yanlış zamanlama = oyuncu hasar alır.
- Saldırı **cooldown**'u / kısa animasyon kilidi spam'i engeller (ritim hissi için kritik).

**Dövüş döngüsü (state):**
```
Idle/İlerle → Düşman menzilde? → Saldır (hitbox aç) → İsabet? → Knockback + Hasar
                  │ hayır                                    │ ıskaladın / geç kaldın
                  └────────► İlerlemeye devam                └────────► Oyuncu hasar alır
```

### 3.3 Düşmanlar & Engeller
- **Ufak canavar:** Düşük can, vurulunca knockback, oyuncuya yürür/yaklaşır, temasta hasar verir.
- **(Sonra) Ağır/engelleyici tip:** Knockback'e dirençli, birkaç vuruş ister.
- **Engel:** Statik (kaya/çukur vb.) — `Jump` ile aşılır veya kılıçla kırılır. (v1'de basit tutulur.)
- Düşmanlar **spawn point / dalga** sistemiyle yol boyunca yerleştirilir (tasarımcı kontrolü).

### 3.4 Can / Hasar / Ölüm
- Oyuncu canı (kalp/bar). Hasar → geri besleme (titreşim/flash). Can biterse → checkpoint/sahne başı.
- Düşman canı + ölüm efekti.
- Ortak bir `Health` bileşeni + `IDamageable` arayüzü (oyuncu, düşman, kırılabilir engel paylaşır).

### 3.5 Köy — Diyalog & NPC
- `Interact` (mevcut, **Hold**) ile NPC'ye yaklaşıp konuşma.
- Diyaloglar **veri-temelli** (ScriptableObject veya JSON) → metinler koddan ayrı, kolay düzenlenir.
- Basit ikna/ilerleme bayrağı: NPC ikna edilince köyün "yola çık" kapısı/işareti aktifleşir.

### 3.6 Okçu Çocuk (OPSİYONEL — kapsam belirsiz)
- Atın arkasında oturan, düşmanlara **otomatik ok atan** yardımcı karakter.
- Tamamen modüler tasarlanacak: `Companion` bileşeni olarak eklenebilir/çıkarılabilir, çekirdek dövüşü bozmaz.
- Karar verilene kadar **iskelet + arayüz** hazırlanır, ama oynanışa bağlanması ertelenebilir. Bkz. [Karar #2](#11-karar-bekleyen-noktalar--varsayımlar).

---

## 4. Hikâye Yapısı

**Ana hat:** Ulak, atıyla **3 köyü** dolaşır. Her köyde insanları **demir dağını eritip dağı işlemeye** (ortak üretim/dayanışma) ikna etmesi gerekir. Köyler ikna edildikçe büyük bir **toy** (toplanma) doğru ilerler; final bu birleşmenin sahnesidir.

| Köy | Tema/Çatışma (taslak) | İkna koşulu (taslak) |
|-----|------------------------|----------------------|
| Köy 1 | Tanışma, güvensizlik | İlk yardımı yap / ulağı dinlet |
| Köy 2 | Bölünmüşlük / korku | Yoldaki tehdidi göster, kanıt getir |
| Köy 3 | Son direnç | Diğer köylerin desteğini göster |
| Final | Toy — demir dağı | Hepsi birleşir |

> Metinler ve diyaloglar **zamanla detaylandırılacak**. Yapı şimdi sabit, içerik sonra dolacak — bu yüzden diyalog sistemi veri-temelli kurulur.

---

## 5. Teknik Temel

| Konu | Karar |
|------|-------|
| Unity sürümü | `6000.4.9f1` (Unity 6) |
| Render | URP 2D (`Renderer2D`), `UniversalRP` asset mevcut |
| Girdi | Input System 1.19.0 — `Assets/InputSystem_Actions.inputactions` (Player + UI) |
| 2D araçlar | 2D Animation, **Aseprite Importer**, PSD Importer, SpriteShape, **Tilemap (+Extras)** |
| Köprü | **Unity MCP** (`com.coplaydev.unity-mcp`) → Claude Code Editor'ı sürebilir |
| Hedef platform | PC (Windows) öncelik; girdi map'i gamepad/touch'ı da kapsıyor |
| Pixel art | Sabit PPU (örn. 16 veya 32), Pixel Perfect Camera, point/no-filter sampling |

**Mevcut Input aksiyonları (Player map):** `Move, Look, Attack, Interact(Hold), Crouch, Jump, Previous, Next, Sprint`.
→ Yol modu (**oto-koşu**): `Attack(sağ tık) + Jump` (yatay hareket otomatik). Köy modu: `Move/Interact`. (Action Map'leri mod bazında etkinleştir/kapat.)
→ **Yapılacak:** Attack aksiyonuna `<Mouse>/rightButton` binding'i ekle.

---

## 6. Yazılım Mimarisi

**Yaklaşım:** Bileşen-temelli + hafif state machine + veri için ScriptableObject. Aşırı mühendislikten kaçın; küçük oyun.

### Üst seviye sistemler
- **GameManager** — mod/sahne geçişleri, oyun durumu, ilerleme bayrakları (hangi köy ikna edildi).
- **SceneFlow / Loader** — Köy↔Yol geçişleri, additive yükleme opsiyonel.
- **InputRouter** — aktif moda göre doğru Action Map'i açar/kapar.
- **SaveSystem (v2)** — ilerleme kaydı (başta basit PlayerPrefs/JSON).

### Yol modu bileşenleri
- `PlayerMover` (yatay hareket, sprint, jump)
- `SwordAttack` (hitbox, cooldown, animasyon tetik)
- `Health` + `IDamageable` (ortak)
- `Knockback` (hasar alınca/verince itme)
- `EnemyBase` → `SmallEnemyAI` (yaklaş, temas hasarı), ileride `HeavyEnemyAI`
- `EnemySpawner` / `WaveTrigger` (yol boyunca yerleşim)
- `Obstacle` (statik/kırılabilir)
- `CompanionArcher` (opsiyonel, modüler)

### Köy modu bileşenleri
- `NPCInteractable` (yaklaşınca prompt, `Interact` ile tetik)
- `DialogueRunner` + `DialogueData` (ScriptableObject/JSON)
- `VillageProgress` (ikna bayrağı → kapı açma)

### Ortak
- `IDamageable`, `IInteractable` arayüzleri
- `GameEvents` (basit event/observer; çoklu sistemleri gevşek bağlamak)
- `ScriptableObject` veri: EnemyConfig, DialogueData, VillageConfig

### Assembly Definition (asmdef)
Derleme süresini ve sınırları korumak için:
```
Ulak.Core      (GameManager, arayüzler, eventler, ortak)
Ulak.Gameplay  (Yol + Köy bileşenleri)  → Core'a referans
Ulak.UI        (HUD, diyalog UI, menü)  → Core'a referans
Ulak.Tests     (EditMode/PlayMode)      → yukarıdakilere referans
```

---

## 7. Klasör & Sahne Yapısı

### Önerilen `Assets/` düzeni
```
Assets/
├─ _Project/                 # tüm özgün içerik tek kök altında
│  ├─ Art/
│  │  ├─ Characters/         # ulak, at, çocuk, NPC'ler (.aseprite)
│  │  ├─ Enemies/
│  │  ├─ Tilesets/           # yol, köy zeminleri
│  │  └─ UI/
│  ├─ Audio/                 # sfx, müzik
│  ├─ Prefabs/
│  │  ├─ Player/  Enemies/  Props/  UI/
│  ├─ ScriptableObjects/     # EnemyConfig, DialogueData, VillageConfig
│  ├─ Scenes/
│  │  ├─ Boot.unity          # başlangıç/menü
│  │  ├─ Village_01.unity
│  │  ├─ Road_01_02.unity
│  │  ├─ Village_02.unity
│  │  ├─ Road_02_03.unity
│  │  └─ Village_03.unity
│  ├─ Scripts/
│  │  ├─ Core/  Gameplay/  UI/  (+ asmdef'ler)
│  └─ Settings/              # input map buraya taşınabilir
└─ (Settings/, URP asset'leri — Unity'nin oluşturduğu, yerinde kalır)
```
> Mevcut `Assets/Scenes/SampleScene.unity` greybox/test sahnesi olarak kullanılıp sonra arşivlenebilir.

### Sahne sözleşmesi (her sahnede ortak)
- Bir `SystemsBootstrap` (GameManager vb. yoksa yükler — additive pattern).
- Mod'a uygun kamera (Yol: takip kamerası + Pixel Perfect; Köy: sınırlı kamera).
- Net giriş/çıkış tetikleyicileri (köy kapısı → yol; yol sonu → köy).

---

## 8. Asset Pipeline (Pixel Art)

**Araç zinciri:** Aseprite (çizim/animasyon) → Unity **Aseprite Importer** (`.aseprite` doğrudan import) → Animator/Sprite Library.

**Kurallar (proje genel):**
- Sabit **PPU** (öneri: 16 veya 32 — tüm asset'ler aynı). [Karar #4]
- **Pixel Perfect Camera** bileşeni; sprite sampling = Point, compression = None.
- İsimlendirme: `chr_ulak_idle`, `chr_ulak_attack`, `enm_small_walk`, `tile_road_*`.
- Animasyon klipleri Aseprite tag'lerinden otomatik üretilir (idle/walk/attack/hit/death).

**Placeholder stratejisi (asset gelene kadar):**
- Tek renk kareler / Unity built-in sprite'lar ile **greybox**.
- Tüm prefab'ler ve mekanikler placeholder ile çalışır; gerçek sprite gelince sadece SpriteRenderer + Animator swap edilir. **Mekanik koda dokunmadan sanat değişir.**

---

## 9. Geliştirme Pipeline'ı (Fazlar)

Her faz **oynanabilir/test edilebilir** bir çıktı verir. Sanat beklemeden ilerler.

### Faz 0 — İskelet & Altyapı  ✅ çıktı: boş ama derlenen proje
- [ ] `_Project/` klasör yapısı + asmdef'ler
- [ ] `Boot` sahnesi + `GameManager` + sahne geçiş iskeleti
- [ ] Input map'i mod bazında aç/kapa (`InputRouter`)
- [ ] CLAUDE.md (proje kuralları, MCP notları) — `/unity-init`

### Faz 1 — Yol Çekirdek Dövüşü (greybox)  ✅ çıktı: bir düşmanı kılıçla deviren oynanış
- [x] `PlayerController` (oto-koşu: sabit hızda sağa) + `Jump` (zemin/coyote kontrollü) + duvara çarpma (fizik)
- [x] Input map'e **sağ tık** Attack binding'i ekle (scriptler ayrıca cihazdan doğrudan okuyor)
- [x] `SwordAttack` (önde OverlapBox hitbox + cooldown, sağ tık tetikli)
- [x] `Health` + `IDamageable` + `Knockback` (+ `DamageFlash`)
- [x] `SmallEnemyAI` (yaklaş + temas hasarı + knockback'e tepki) + `EnemyDeath`
- [x] Greybox takip kamerası (`CameraFollow`) — *Pixel Perfect cila olarak sonra*
- [x] `GreyboxRoadBuilder` editor menüsü: tek tıkla test sahnesi (`Ulak ▸ Greybox Yol Sahnesi Kur`)
- [ ] **Hedef his:** "yaklaş–zamanla–vur–geri savur" döngüsü iyi hissettirmeli (Play-test + denge)

### Faz 2 — Yol İçeriği & Akış  ✅ çıktı: baştan sona bitirilebilir bir yol
- [ ] `EnemySpawner` / `WaveTrigger` (yol boyu yerleşim)
- [ ] `Obstacle` (zıpla/kır)
- [ ] Yol sonu → köy geçiş tetikleyicisi
- [ ] Can UI (HUD), ölüm/yeniden başlama

### Faz 3 — Köy & Diyalog  ✅ çıktı: konuşulabilir NPC + ikna → yola çık
- [ ] `NPCInteractable` + `DialogueRunner` + `DialogueData`
- [ ] Diyalog UI (kutu, isim, ilerlet)
- [ ] `VillageProgress` (ikna bayrağı → kapı açılır)
- [ ] Köy 1 sahnesini greybox kur

### Faz 4 — Dikey Dilim (Vertical Slice)  ✅ çıktı: Köy1 → Yol1→2 → Köy2 tam akış
- [ ] GameManager ilerleme bayrakları + sahne zinciri
- [ ] Basit kayıt (PlayerPrefs/JSON)
- [ ] Ses iskeleti (vuruş, hasar, ambient)
- [ ] İlk gerçek metinler (taslak hikâye)

### Faz 5 — İçerik Doldurma & Cila
- [ ] 3 köy + 2 yol tam içerik
- [ ] (Opsiyonel) Okçu çocuk mekaniği
- [ ] Düşman çeşitliliği (ağır tip)
- [ ] Gerçek pixel-art asset entegrasyonu (swap)
- [ ] Final/toy sahnesi
- [ ] Cila: feel (screen shake, hit-stop, partiküller), denge, ses miks

### Faz 6 — Build & Test
- [ ] EditMode/PlayMode testleri (kritik: hasar, knockback, geçişler)
- [ ] `/unity-build` ile Windows build
- [ ] Playtest geri bildirim turu

---

## 10. MCP + Claude Code İş Akışı

Unity MCP kurulu olduğundan Claude Code, Editor'ı doğrudan sürebilir. Tipik döngü:

1. **Plan** — bu doküman + `/unity-feature` veya `/unity-workflow` ile özellik kır.
2. **Kod** — script'leri Claude Code yazar (`Assets/_Project/Scripts/...`).
3. **Sahne kurulumu** — `/unity-scene` ile GameObject/prefab/hierarchy MCP üzerinden.
4. **Çalıştır & doğrula** — MCP ile Play mode, console hatalarını oku (`/unity-fix`).
5. **Gözden geçir** — `/unity-review` (serialization, performans, Unity tuzakları).
6. **Test** — `/unity-test` (EditMode/PlayMode).

**Kullanışlı skill'ler:** `unity-init` (CLAUDE.md), `unity-prototype` (tek komutla oynanabilir prototip), `unity-feature`, `unity-scene`, `unity-fix`, `unity-review`, `unity-build`, `unity-doctor` (MCP bağlantı kontrolü).

**İlk komut önerisi:** `/unity-doctor` → MCP bağlantısını doğrula, sonra `/unity-init` → CLAUDE.md üret.

**Çalışma kuralı:** Asset gelmeden mekanik geliştir (greybox). Her PR/iterasyon tek bir oynanabilir kazanım hedeflesin (yukarıdaki faz çıktıları).

---

## 11. Karar Bekleyen Noktalar & Varsayımlar

| # | Konu | Durum / Karar |
|---|------|---------------|
| 1 | **Saldırı tuşu** | ✅ **Sağ tık** (karar verildi). Input map'e `<Mouse>/rightButton` binding'i eklenecek. |
| 2 | **Okçu çocuk** | ✅ **Ertelendi** (karar verildi). Modüler `Companion` arayüzü hazırlanır, oynanışa bağlama Faz 5'e bırakıldı. |
| 3 | **İlerleme** | ✅ **Otomatik koşu** (karar verildi). At sabit hızda ilerler; oyuncu yalnızca saldırı/zıplama zamanlar. |
| 4 | **PPU / pixel ölçeği** | ⏳ Öneri: 32 PPU. Asset üretimi başlamadan netleşmeli (tutarlılık için). *Onay gerek.* |
| 5 | **Atın rolü** | ⏳ Varsayım: görsel + binici tek birim, `Jump` ile engel aşma (oto-koşu ile uyumlu). |
| 6 | **Dikey platform** | ⏳ v1'de **yok** (tek seviye yol) varsayımı. İstenirse sonra eklenir. |
| 7 | **Hedef en/boy & çözünürlük** | ⏳ Pixel-perfect için referans çözünürlük belirlenmeli (örn. 320×180 ölçekli). *Onay gerek.* |

> Bu kararlar netleşince ilgili bölümler güncellenecek. Şimdilik makul varsayımlarla ilerlenebilir.

---

## 12. Görev Listesi (Backlog)

**Hemen yapılabilir (kod beklemeden, MCP ile):**
- [ ] `/unity-doctor` — MCP bağlantısını doğrula
- [ ] `/unity-init` — CLAUDE.md üret
- [ ] `_Project/` klasör iskeletini + asmdef'leri oluştur
- [ ] `Boot` + `GameManager` + sahne geçiş iskeleti
- [ ] Greybox `Road_01` test sahnesi (SampleScene üzerinden)

**Sonra (Faz 1):**
- [ ] PlayerMover, SwordAttack, Health/IDamageable, Knockback, SmallEnemyAI
- [ ] Pixel Perfect kamera + takip
- [ ] İlk "his" testi: düşmanı vur, geri savur, ritmi doğrula

---

### Ek: Tek bakışta özet
- **Ne:** At üstünde köyden köye giden ulağın kılıçlı yolculuğu + köylerde ikna/dayanışma.
- **Nasıl:** İki mod (Yol aksiyon / Köy diyalog), lineer zincir, greybox-first.
- **Çekirdek his:** Yaklaş → zamanla → vur → geri savur.
- **Önce:** Mekanik (kod), sonra: sanat (Aseprite swap).
- **İlk adım:** `/unity-doctor` → `/unity-init` → Faz 0 iskeleti.
