# ULAK

> 2D yan-bakış (side-scroller) aksiyon / pixel-art oyunu — Türklük, dayanışma ve toplanma (Toy) teması üzerine kısa, odaklı bir aksiyon-anlatı deneyimi.

**Ulak**, oyuncunun atının üstünde köyden köye giden bir ulağı (haberci/elçi) canlandırdığı bir oyundur. Yol boyunca kılıcıyla canavarları ve engelleri aşar; köylerde NPC'lerle konuşarak insanları ortak bir amaç — **demir dağını eritip işlemek** — etrafında birleştirmeye çalışır.

## Teknik

| | |
|---|---|
| Motor | Unity `6000.4.9f1` |
| Render | URP 2D |
| Girdi | Unity Input System |
| Geliştirme | Claude Code + MCP for Unity |

## Mevcut Durum — Greybox v1 (Faz 1)

Asset'siz, gri kutularla oynanabilir prototip:

- ⚔️ Yöne duyarlı kılıç saldırısı + knockback
- 🏃 A/D veya ok tuşlarıyla hareket, Space ile zıplama (coyote time'lı)
- 👾 Aggro menzilli küçük düşman AI'ı (temas hasarı + savrulma)
- ❤️ Ortak `Health`/`IDamageable` sistemi, ölünce respawn
- 🎬 Tek tıkla test sahnesi: **Ulak ▸ Greybox Yol Sahnesi Kur**

### Kontroller

| Tuş | İşlev |
|---|---|
| A / D veya ← / → | Hareket |
| Space | Zıpla |
| Sağ tık | Kılıç saldırısı |

## Klasör Yapısı

```
Assets/_Project/
├── Art/Greybox/     # Üretilen placeholder asset'ler
├── Scenes/          # Road_Greybox.unity (test sahnesi)
└── Scripts/
    ├── Core/        # Health, Knockback, DamageFlash, IDamageable
    ├── Gameplay/    # PlayerController, SwordAttack, SmallEnemyAI, CameraFollow
    └── Editor/      # GreyboxRoadBuilder (sahne kurucu)
Docs/                # Tasarım & geliştirme planı (ULAK_PLAN.md)
```

Detaylı tasarım ve yol haritası için: [Docs/ULAK_PLAN.md](Docs/ULAK_PLAN.md)
