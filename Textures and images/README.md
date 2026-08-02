# Project Return — Asset Pack 01

Procedurally generated, Unity-ready. All maps are power-of-two; materials tile seamlessly.

Drop `ReturnAssets/` anywhere under `Assets/`.

---

## UI/StanceIcons — 512×512 RGBA

Three variants per stance (Stone, Water, Flame, Wind):

| Suffix | Use |
|---|---|
| `_Mask` | Pure white + alpha. Tint at runtime via `Image.color` — best for state changes (active stance flares gold, others grey). |
| `_Gold` | Pre-shaded weathered brass. Use if you're not tinting. |
| `_Locked` | Desaturated, for stances not yet earned from a boss. |

**Import settings:** Texture Type `Sprite (2D and UI)`, Alpha Is Transparency ✅, Generate Mip Maps ❌, Filter `Bilinear`, Compression `High Quality`.

---

## UI/HUD

| File | Notes |
|---|---|
| `HUD_BarFrame.png` (96×40) | **9-slice, border = 10px all sides.** Set in the Sprite Editor. |
| `HUD_BossBarFrame.png` (160×30) | **9-slice, border = 8px all sides.** |
| `HUD_Fill_*.png` (64×32) | Health / Stamina / Focus / Posture / Lag. Set Image `Type = Filled`, `Fill Method = Horizontal`. Wrap Mode `Repeat` so stretching stays clean. |
| `HUD_StanceWheel.png` (512²) | Radial backing for the stance selector; four 90° slots. |
| `HUD_Vignette.png` (1024²) | Black + alpha gradient. Full-screen Image, tint red and lerp alpha for low health. |

`HUD_Fill_Lag` is the chip-damage trail — draw it behind the health fill and lerp its `fillAmount` down over ~0.4s after a hit. Standard Souls readability trick and worth having wired early.

---

## VFX

All greyscale-in-RGB + alpha, so they tint cleanly per stance (Flame → orange, Water → pale blue, etc.).

| File | Use |
|---|---|
| `VFX_SoftParticle` (128²) | General-purpose particle dot. |
| `VFX_ImpactFlash` (512²) | Hit confirm. Pair with your hit-stop frames. |
| `VFX_SlashArc` (512²) | Weapon trail / attack telegraph. Sweep UVs or rotate the billboard. |
| `VFX_DustPuff` (256²) | Footsteps, dodge rolls, landings. |
| `VFX_EmberStreak` (128²) | Stretched billboard sparks. |
| `VFX_GroundCrack` (512²) | Projector/decal for heavy attacks and boss slams. |
| `VFX_DissolveNoise_Tileable` (512²) | Dissolve shader alpha clip — enemy death, stance swap. |
| `VFX_VeinNoise_Tileable` (512²) | Ridged noise for energy/crack emissive patterns. |
| `VFX_DistortionMap_Tileable` (256²) | R/G = UV offset. Heat shimmer, wind distortion. |

**Import settings:** Texture Type `Default`, Alpha Is Transparency ✅, Wrap `Clamp` (or `Repeat` for the three `_Tileable` files), sRGB ✅ except the tileable noise/distortion maps — **untick sRGB on those**, they're data not colour.

---

## Materials — 1024×1024, seamless

`Stone_CastleBlock`, `Wood_Weathered`, `Iron_Rusted`, `Leather_Worn`, `Ground_Ash`

Each set ships `_Albedo`, `_Normal`, `_MaskMap`, `_Roughness`, `_Height`, `_AO`.

**Built-in / URP Lit:**
- Albedo → `_Albedo` (smoothness is packed in its **alpha**; set Smoothness Source = `Albedo Alpha`)
- Normal Map → `_Normal` (Texture Type `Normal map`)
- Occlusion → `_AO` (untick sRGB)
- Height → `_Height` for parallax if you want it (untick sRGB)

**HDRP Lit:**
- Base Map → `_Albedo`, Mask Map → `_MaskMap` (R metallic, G AO, B detail, A smoothness), Normal → `_Normal`. Untick sRGB on the mask map.

Normals are OpenGL convention (+Y up), which is what Unity expects — no green-channel flip needed.

---

## Not included

Character, boss, and weapon art. These are geometric/procedural assets — they'll carry a greybox and a real HUD, but they can't stand in for hand-authored models or concept art.

---
---

# Pack 02 — Terrain, Characters, Combat Decals

## Terrain/Heightmaps

Three landscapes: `Terrain_CastleApproach`, `Terrain_Ashlands`, `Terrain_ArenaBasin`.

Each ships a `_1025.raw`, a `_Preview.png` (shaded relief, for picking — not for import) and a `_Splatmap.png`.

**Import the RAW:** Terrain → Settings → Import Raw.
- Depth: **Bit 16**
- Byte Order: **Windows** (little-endian)
- Resolution: **1025**
- Terrain Size: start at **1000 × 180 × 1000** for the two open regions; **220 × 45 × 220** for `ArenaBasin`, which is built as a bowl-shaped boss arena rather than an explorable region.

Set Heightmap Resolution to 1025 in Terrain Settings *before* importing or the dialog will reject the file.

**Splatmaps** are RGBA control maps — R = grass/turf, G = cliff rock, B = mud, A = gravel. Channel order matches the Terrain Layer order below. Applying them needs a one-off editor script (`TerrainData.SetAlphamaps`) since Unity has no import button for control maps.

## Terrain/Layers

`Moss_Turf`, `Cliff_Rock`, `Mud_Wet` — add as Terrain Layers in that order so they line up with the splatmap's R/G/B. Use `Ground_Ash` from Pack 01 as the A/gravel layer.

Suggested tiling size: **4 × 4** for turf and mud, **8 × 8** for cliff rock.

## Terrain/Detail

`Detail_GrassTuft`, `Detail_DeadGrass`, `Detail_AshWeed`, `Detail_DeadShrub` — cutout billboards with alpha. Add via Paint Details → Add Grass Texture. Render Mode `Grass`, Billboard ✅, Healthy/Dry colour tinted per region.

## Characters/Blockouts

Six OBJ proxies. **Metric scale, origin at the feet, +Y up, +Z forward** — they drop into Unity at scale 1 with no rotation fix.

| Mesh | Height | Reach | Role |
|---|---|---|---|
| `Player_Wanderer` | 1.78 m | — | Player capsule reference |
| `Enemy_HollowSoldier` | 1.72 m | 1.4 m | Basic trash mob |
| `Enemy_SpearGuard` | 1.80 m | 2.6 m | Range-check enemy — punishes greedy approach |
| `Enemy_HeavyKnight` | 2.05 m | 2.1 m | Poise-heavy, slow |
| `Enemy_Houndbeast` | 0.95 m | 1.2 m | Quadruped, lunges |
| `Boss_GreatKnight` | 3.20 m | 4.3 m | Arena boss scale test |

`_ColliderSpec.csv` lists capsule radius/height and attack reach per unit. `_ScaleReference.png` shows all six side by side against a metre grid.

These exist so you can tune **hitboxes, camera framing, lock-on distance and animation timing against correct silhouettes and mass** before any real art lands. They are proxies, not shippable models — swap them out and the collider values carry over.

## Characters/Materials

`Plate_Steel`, `Chainmail_Rusted`, `Cloth_Tattered`, `Fur_Matted` — seamless 1024², same channel packing as Pack 01 (smoothness in albedo alpha; mask map for HDRP). Assign to the blockouts to get readable material contrast between enemy types.

## Combat/Telegraphs

`Telegraph_Cone45 / Cone90 / Cone180`, `Telegraph_CircleAoE`, `Telegraph_LungeLine` — attack wind-up decals. Project onto the ground during the telegraph window and animate alpha or a radial fill up to the active frame.

`Reticle_LockOn` for the lock-on target, `Decal_ShadowBlob` for cheap grounded contact shadows on enemies.

All are white + alpha, so tint per stance or per threat level (white = normal, red = unblockable) in the material.

## Still not included

Rigged, animated, hand-authored character models and boss art. The blockouts will carry combat prototyping a long way, but the actual creature design needs a modeller or marketplace assets.

---
---

# Pack 03 — Skins, UI Kit, Screens

## Characters/Skins

Six 1024² skin sets, same channel packing as before (smoothness in albedo alpha, mask map for HDRP).

| Skin | For |
|---|---|
| `Skin_Player_AshenWanderer` | Player — worn cloth over leather |
| `Skin_Enemy_HollowDesiccated` | Hollow soldier — pallid, sinewy |
| `Skin_Enemy_KnightTarnished` | Heavy knight — dulled plate with gold trim |
| `Skin_Enemy_KnightElite` | Elite variant — same plate, ember-lit cracks |
| `Skin_Enemy_HoundMangy` | Houndbeast — patchy matted fur |
| `Skin_Boss_Blackened` | Boss — soot-blackened plate bleeding forge light |

`KnightElite` and `Boss_Blackened` ship an `_Emissive` map. Enable Emission on the material and set the HDR colour intensity around 2–3; that glow is your at-a-glance "this one hits harder" tell, which matters more than silhouette at distance.

## UI/Kit

**9-slice sprites** — set these borders in the Sprite Editor:

| Sprite | Border |
|---|---|
| `Panel_Dark`, `Panel_Parchment` | 24 |
| `Panel_Tooltip` | 18 |
| `Button_Normal / Hover / Pressed / Disabled` | 14 |
| `Slider_Track`, `Slider_Fill` | 6 |
| `Scrollbar_Track`, `Scrollbar_Handle` | 6 |

Wire the four button states into a `Button` component's Sprite Swap transition. `Hover` has corner flourishes that read as a highlight without needing a colour tint.

**Non-sliced:** `ItemSlot_Empty` / `ItemSlot_Selected` (128²), `Slider_Handle`, `Toggle_On` / `Toggle_Off`, `Arrow_Left / Right / Down`, `Caret_MenuSelect`, `Divider_Ornament` (512×32), `Cursor`.

All Sprite (2D and UI), Alpha Is Transparency ✅, no mip maps.

## UI/Screens — 1920×1080

| Screen | Notes |
|---|---|
| `Screen_Title` | Fortress on a ridge, dim gold horizon, drifting motes |
| `Screen_Death` | Deep red bloom on near-black |
| `Screen_SettingsBackdrop` | Blurred stone, heavily vignetted so text reads |
| `Screen_PauseOverlay` | **RGBA, black with graded alpha** — layer over live gameplay |
| `Screen_Loading` | Near-black with a faint ember drift; leave the lower right free for a spinner |
| `Screen_Rest` | Warm ember glow — checkpoint / bonfire equivalent |
| `Screen_StanceGained` | Vertical light shaft — for stance unlocks after a boss |
| `Screen_BossDefeated` | Radiating gold shafts |

Import as Sprite, no mip maps, and set the Canvas Image to `Preserve Aspect` — or use a `Raw Image` with a Fit component if you need to support ultrawide without letterboxing.

**`Mockup_*` files are compositions, not assets.** They show `Screen_Title`, `Screen_Death` and `Screen_SettingsBackdrop` with placeholder text so you can judge layout — the type is baked in and set in Lora, which is not a licensed choice for you. Use the clean `Screen_*` backgrounds and do real text in TextMeshPro with a font you've picked.

On the death screen wording: I used a neutral placeholder rather than the phrase Dark Souls uses, since that's theirs. Worth landing on your own line early — it's one of the most-seen four words in the game.

## Fonts

None supplied. For this direction look at Cinzel, Marcellus, EB Garamond or Cormorant (all SIL Open Font License, free for commercial use) — pair a display serif for headings with something quieter for body copy.
