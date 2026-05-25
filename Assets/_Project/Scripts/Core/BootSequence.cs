using UnityEngine;
using UnityEngine.UI;
using Vanguard.TD.Data;
using Vanguard.TD.Gameplay;

namespace Vanguard.TD.Core
{
    /// <summary>
    /// Builds detailed fantasy sprites and runtime prefabs procedurally.
    /// Style: mystic-fantasy — purple/teal palette, painterly shading, expressive
    /// creature design. No external assets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BootSequence : MonoBehaviour
    {
        [Header("Turret blueprints")]
        public TurretBlueprint pulseBP;
        public TurretBlueprint plasmaBP;
        public TurretBlueprint cryoBP;
        public TurretBlueprint railBP;

        [Header("Hostile blueprints")]
        public HostileBlueprint scoutBP;
        public HostileBlueprint tankBP;
        public HostileBlueprint phaseBP;

        void Awake()
        {
            // Towers
            BuildTurretPrefab(pulseBP,  ArcherTowerSprite(),  ArrowSprite(),    "Archer");
            BuildTurretPrefab(plasmaBP, MageTowerSprite(),    MagicOrbSprite(), "Mage");
            BuildTurretPrefab(cryoBP,   IceTowerSprite(),     IceShardSprite(), "Ice");
            BuildTurretPrefab(railBP,   CannonTowerSprite(),  CannonBallSprite(),"Cannon");

            // Hostiles
            BuildHostilePrefab(scoutBP, GoblinSprite(), 0.55f);
            BuildHostilePrefab(tankBP,  OrcSprite(),    0.70f);
            BuildHostilePrefab(phaseBP, GhostSprite(),  0.60f);

            SpawnCastleDecoration();
        }

        // ════════════════════════════════════════════════════════════════════
        // Prefab plumbing (unchanged behavior, just clean wrappers)
        // ════════════════════════════════════════════════════════════════════
        void BuildTurretPrefab(TurretBlueprint bp, Sprite turretSpr, Sprite boltSpr, string label)
        {
            if (bp == null) return;

            if (bp.turretPrefab == null)
            {
                var go = new GameObject($"TurretPrefab_{label}");
                go.SetActive(false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = turretSpr;
                sr.sortingOrder = 2;
                go.AddComponent<Turret>();
                go.AddComponent<TurretFireControl>();
                DontDestroyOnLoad(go);
                bp.turretPrefab = go;
            }

            if (bp.boltPrefab == null && boltSpr != null)
            {
                var go = new GameObject($"BoltPrefab_{label}");
                go.SetActive(false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = boltSpr;
                sr.sortingOrder = 4;
                go.transform.localScale = Vector3.one * 0.35f;
                go.AddComponent<Bolt>();
                DontDestroyOnLoad(go);
                bp.boltPrefab = go;
            }
        }

        void BuildHostilePrefab(HostileBlueprint bp, Sprite spr, float scale)
        {
            if (bp == null || bp.prefab != null) return;
            var go = new GameObject($"HostilePrefab_{bp.codename}");
            go.SetActive(false);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = 3;

            go.AddComponent<Hostile>();
            BuildIntegrityBar(go.transform);

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;
            col.isTrigger = true;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) go.layer = enemyLayer;

            DontDestroyOnLoad(go);
            bp.prefab = go;
        }

        Image BuildIntegrityBar(Transform parent)
        {
            var canvasGo = new GameObject("IntegrityCanvas");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.15f, -0.1f);
            canvasGo.transform.localScale    = Vector3.one * 0.014f;

            var cv = canvasGo.AddComponent<Canvas>();
            cv.renderMode      = RenderMode.WorldSpace;
            cv.sortingOrder    = 10;
            cv.overrideSorting = true;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 10);

            var frame = new GameObject("Frame");
            frame.transform.SetParent(canvasGo.transform, false);
            var frameImg = frame.AddComponent<Image>();
            frameImg.color = new Color(0.20f, 0.10f, 0.05f, 0.95f);
            var frameRt = frameImg.rectTransform;
            frameRt.anchorMin = Vector2.zero; frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = new Vector2(-1, -1);
            frameRt.offsetMax = new Vector2( 1,  1);

            var bg = new GameObject("Backdrop");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.05f, 0.10f, 0.95f);
            var bgRt = bgImg.rectTransform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var fill = new GameObject("Bar");
            fill.transform.SetParent(canvasGo.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.30f, 0.90f, 0.45f);
            fillImg.type  = Image.Type.Simple;
            var fillRt = fillImg.rectTransform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            return fillImg;
        }

        // ════════════════════════════════════════════════════════════════════
        // CASTLE DECORATION (Base visual)
        // ════════════════════════════════════════════════════════════════════
        void SpawnCastleDecoration()
        {
            var reactor = FindAnyObjectByType<Vanguard.TD.Map.Reactor>();
            if (reactor == null) return;
            var pos = reactor.transform.position;

            var oldSr = reactor.GetComponent<SpriteRenderer>();
            if (oldSr != null) Destroy(oldSr);

            var dec = new GameObject("CastleDecoration");
            dec.transform.position = new Vector3(pos.x + 0.3f, pos.y - 0.15f, 0.5f);
            var sr = dec.AddComponent<SpriteRenderer>();
            sr.sprite = CastleSprite();
            sr.sortingOrder = 1;
        }

        // ════════════════════════════════════════════════════════════════════
        // PALETTE — Mystic Fantasy
        // ════════════════════════════════════════════════════════════════════
        // Earth + wood
        static readonly Color WoodDark  = new(0.30f, 0.18f, 0.10f, 1f);
        static readonly Color WoodMid   = new(0.55f, 0.36f, 0.20f, 1f);
        static readonly Color WoodLite  = new(0.78f, 0.55f, 0.32f, 1f);
        static readonly Color StoneDark = new(0.35f, 0.30f, 0.35f, 1f);
        static readonly Color StoneMid  = new(0.55f, 0.50f, 0.55f, 1f);
        static readonly Color StoneLite = new(0.75f, 0.72f, 0.78f, 1f);

        // Magical accents
        static readonly Color VioletDeep = new(0.30f, 0.10f, 0.55f, 1f);
        static readonly Color Violet     = new(0.55f, 0.25f, 0.85f, 1f);
        static readonly Color Pink       = new(0.95f, 0.50f, 0.90f, 1f);
        static readonly Color GoldDeep   = new(0.70f, 0.50f, 0.10f, 1f);
        static readonly Color Gold       = new(1.00f, 0.80f, 0.20f, 1f);
        static readonly Color GoldLite   = new(1.00f, 0.95f, 0.55f, 1f);

        // Ice
        static readonly Color IceDeep = new(0.20f, 0.40f, 0.70f, 1f);
        static readonly Color IceMid  = new(0.55f, 0.80f, 1.00f, 1f);
        static readonly Color IceLite = new(0.85f, 0.95f, 1.00f, 1f);

        // Foliage / creatures
        static readonly Color SkinGreen  = new(0.45f, 0.65f, 0.30f, 1f);
        static readonly Color SkinGDark  = new(0.30f, 0.45f, 0.20f, 1f);
        static readonly Color SkinGLite  = new(0.60f, 0.80f, 0.40f, 1f);
        static readonly Color SkinBrown  = new(0.55f, 0.40f, 0.25f, 1f);
        static readonly Color SkinBDark  = new(0.35f, 0.25f, 0.15f, 1f);
        static readonly Color GhostMain  = new(0.85f, 0.90f, 1.00f, 0.80f);
        static readonly Color GhostDeep  = new(0.60f, 0.70f, 0.85f, 0.85f);
        static readonly Color GhostGlow  = new(0.95f, 0.95f, 1.00f, 0.95f);

        static readonly Color RedDeep = new(0.45f, 0.10f, 0.10f, 1f);
        static readonly Color RedMid  = new(0.85f, 0.20f, 0.20f, 1f);

        // Eye / detail
        static readonly Color Black = new(0.05f, 0.05f, 0.08f, 1f);
        static readonly Color White = new(0.98f, 0.98f, 0.95f, 1f);

        // ════════════════════════════════════════════════════════════════════
        // TOWER SPRITES (32×40 detailed)
        // ════════════════════════════════════════════════════════════════════
        Sprite ArcherTowerSprite()
        {
            // Wooden archer post on stone base with a wooden roof.
            int W = 32, H = 40;
            var t = NewTex(W, H);

            // Stone base
            for (int y = 0; y < 8; y++)
            for (int x = 4; x < W - 4; x++)
            {
                bool brick = ((x / 4 + y / 2) % 2 == 0);
                Px(t, x, y, brick ? StoneMid : StoneDark);
            }
            // mortar lines
            for (int x = 4; x < W - 4; x++) Px(t, x, 2, StoneDark);
            for (int x = 4; x < W - 4; x++) Px(t, x, 5, StoneDark);
            // outline
            for (int y = 0; y < 8; y++) { Px(t, 4, y, Black); Px(t, W - 5, y, Black); }

            // Wood log walls
            for (int y = 8; y < 26; y++)
            for (int x = 7; x < W - 7; x++)
            {
                bool grain = (y - 8) % 3 == 0;
                Px(t, x, y, grain ? WoodDark : WoodMid);
            }
            // wood highlight
            for (int y = 9; y < 26; y += 3)
                for (int x = 8; x < W - 8; x++)
                    Px(t, x, y, WoodLite);
            // outline
            for (int y = 8; y < 26; y++) { Px(t, 7, y, Black); Px(t, W - 8, y, Black); }

            // Window slit (where archer peeks)
            for (int y = 15; y < 22; y++)
            for (int x = 13; x < W - 13; x++)
                Px(t, x, y, Black);
            // Archer silhouette in window
            for (int y = 17; y < 21; y++) Px(t, W / 2, y, WoodLite);   // body
            Px(t, W / 2, 21, GoldLite);                                 // head
            // bow
            Px(t, W / 2 - 1, 19, GoldDeep);
            Px(t, W / 2 + 1, 19, GoldDeep);
            Px(t, W / 2 - 2, 18, GoldDeep);
            Px(t, W / 2 + 2, 18, GoldDeep);

            // Roof — triangular wooden shingles
            for (int y = 26; y < 36; y++)
            {
                int half = (36 - y);
                int left  = W / 2 - half;
                int right = W / 2 + half;
                for (int x = left; x <= right; x++)
                {
                    bool stripe = ((y - 26) % 2 == 0);
                    Px(t, x, y, stripe ? WoodDark : WoodMid);
                }
                Px(t, left,  y, Black);
                Px(t, right, y, Black);
            }
            // Flag pole + flag (red)
            for (int y = 36; y < 40; y++) Px(t, W / 2, y, WoodDark);
            Px(t, W / 2 + 1, 38, RedMid);
            Px(t, W / 2 + 2, 38, RedMid);
            Px(t, W / 2 + 1, 37, RedDeep);
            Px(t, W / 2 + 2, 37, RedDeep);

            t.Apply();
            return Build(t);
        }

        Sprite MageTowerSprite()
        {
            int W = 32, H = 42;
            var t = NewTex(W, H);

            // Stone circular base
            for (int y = 0; y < 8; y++)
            for (int x = 4; x < W - 4; x++)
            {
                bool brick = ((x / 3 + y / 2) % 2 == 0);
                Px(t, x, y, brick ? StoneMid : StoneDark);
            }
            for (int y = 0; y < 8; y++) { Px(t, 4, y, Black); Px(t, W - 5, y, Black); }

            // Cylindrical violet body
            for (int y = 8; y < 26; y++)
            for (int x = 8; x < W - 8; x++)
            {
                int dx = x - W / 2;
                float gradient = 1f - Mathf.Abs(dx) / 9f;
                var col = Color.Lerp(VioletDeep, Violet, gradient);
                Px(t, x, y, col);
            }
            for (int y = 8; y < 26; y++) { Px(t, 8, y, Black); Px(t, W - 9, y, Black); }

            // Rune window — diamond shape
            int cx = W / 2, cy = 17;
            for (int dy = -3; dy <= 3; dy++)
            for (int dx = -3; dx <= 3; dx++)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 3)
                    Px(t, cx + dx, cy + dy, Black);
            // glowing center
            Px(t, cx, cy, Pink);
            Px(t, cx, cy - 1, Pink); Px(t, cx, cy + 1, Pink);
            Px(t, cx - 1, cy, Pink); Px(t, cx + 1, cy, Pink);

            // Conical pointed roof
            for (int y = 26; y < 38; y++)
            {
                int half = (38 - y) / 1;
                int left  = W / 2 - half;
                int right = W / 2 + half;
                for (int x = left; x <= right; x++)
                {
                    int xd = Mathf.Abs(x - W / 2);
                    bool light = xd < half / 2;
                    Px(t, x, y, light ? Violet : VioletDeep);
                }
                Px(t, left,  y, Black);
                Px(t, right, y, Black);
            }

            // Glowing orb on top
            Px(t, W / 2, 38, GoldDeep);
            Px(t, W / 2, 39, Gold);
            Px(t, W / 2 - 1, 39, Gold);
            Px(t, W / 2 + 1, 39, Gold);
            Px(t, W / 2, 40, GoldLite);
            // sparkles
            Px(t, W / 2 - 2, 40, Pink);
            Px(t, W / 2 + 2, 40, Pink);

            t.Apply();
            return Build(t);
        }

        Sprite IceTowerSprite()
        {
            int W = 32, H = 40;
            var t = NewTex(W, H);

            // Frozen rocky base
            for (int y = 0; y < 7; y++)
            for (int x = 4; x < W - 4; x++)
            {
                bool crack = ((x + y) % 4 == 0);
                Px(t, x, y, crack ? IceDeep : StoneDark);
            }
            for (int y = 0; y < 7; y++) { Px(t, 4, y, Black); Px(t, W - 5, y, Black); }

            // Ice crystal body — jagged/triangular sides
            for (int y = 7; y < 32; y++)
            {
                int taper = Mathf.Max(0, (y - 7) / 4);
                int left  = 7 + taper;
                int right = W - 8 - taper;
                for (int x = left; x <= right; x++)
                {
                    int dx = x - W / 2;
                    float t01 = Mathf.Abs(dx) / (float)(right - W / 2 + 1);
                    var col = Color.Lerp(IceMid, IceDeep, t01);
                    Px(t, x, y, col);
                }
                Px(t, left,  y, Black);
                Px(t, right, y, Black);
            }
            // bright highlight stripe down center
            for (int y = 8; y < 30; y++) Px(t, W / 2, y, IceLite);

            // Snowflake centerpiece
            int cy = 18;
            for (int i = -3; i <= 3; i++)
            {
                Px(t, W / 2 + i, cy, White);
                Px(t, W / 2, cy + i, White);
            }
            Px(t, W / 2 - 2, cy - 2, IceLite);
            Px(t, W / 2 + 2, cy + 2, IceLite);
            Px(t, W / 2 - 2, cy + 2, IceLite);
            Px(t, W / 2 + 2, cy - 2, IceLite);

            // Sharp crystal peaks at top
            for (int y = 32; y < 38; y++)
            {
                int spread = (38 - y);
                Px(t, W / 2, y, IceLite);
                for (int i = 1; i <= spread; i++)
                {
                    Px(t, W / 2 - i * 2, y, IceMid);
                    Px(t, W / 2 + i * 2, y, IceMid);
                }
            }
            Px(t, W / 2, 38, White);
            Px(t, W / 2, 39, White);

            t.Apply();
            return Build(t);
        }

        Sprite CannonTowerSprite()
        {
            int W = 32, H = 38;
            var t = NewTex(W, H);

            // Solid stone base, broad
            for (int y = 0; y < 10; y++)
            for (int x = 2; x < W - 2; x++)
            {
                bool brick = ((x / 4 + y / 3) % 2 == 0);
                Px(t, x, y, brick ? StoneMid : StoneDark);
            }
            for (int y = 0; y < 10; y++) { Px(t, 2, y, Black); Px(t, W - 3, y, Black); }
            // mortar
            for (int x = 2; x < W - 2; x++) Px(t, x, 3, StoneDark);
            for (int x = 2; x < W - 2; x++) Px(t, x, 6, StoneDark);

            // Mid section — heavier stone
            for (int y = 10; y < 24; y++)
            for (int x = 5; x < W - 5; x++)
            {
                bool brick = ((x / 5 + y / 3) % 2 == 0);
                Px(t, x, y, brick ? StoneLite : StoneMid);
            }
            for (int y = 10; y < 24; y++) { Px(t, 5, y, Black); Px(t, W - 6, y, Black); }

            // Iron banding
            for (int x = 5; x < W - 5; x++)
            {
                Px(t, x, 11, Black);
                Px(t, x, 22, Black);
            }

            // Battlement crenellations on top
            for (int x = 5; x < W - 5; x++)
            {
                bool gap = ((x - 5) % 4) < 2;
                Px(t, x, 24, gap ? StoneLite : StoneDark);
                if (!gap) Px(t, x, 25, StoneDark);
            }

            // Cannon barrel pointing up-right
            for (int i = 0; i < 8; i++)
            {
                int bx = W / 2 + i;
                int by = 26 + i;
                if (bx >= W || by >= H) continue;
                Px(t, bx - 1, by, Black);
                Px(t, bx,     by, StoneDark);
                Px(t, bx + 1, by, Black);
            }
            // muzzle
            Px(t, W / 2 + 8, 34, RedMid);
            Px(t, W / 2 + 7, 35, RedMid);
            Px(t, W / 2 + 9, 35, RedMid);
            Px(t, W / 2 + 8, 36, Gold);

            // Cannon body / breach
            for (int y = 24; y < 30; y++)
            for (int x = W / 2 - 3; x <= W / 2 + 1; x++)
                Px(t, x, y, Black);
            for (int y = 25; y < 29; y++)
            for (int x = W / 2 - 2; x <= W / 2; x++)
                Px(t, x, y, StoneDark);

            t.Apply();
            return Build(t);
        }

        // ════════════════════════════════════════════════════════════════════
        // PROJECTILE SPRITES
        // ════════════════════════════════════════════════════════════════════
        Sprite ArrowSprite()
        {
            // 6×14 — arrowhead, shaft, feathers
            int W = 6, H = 14;
            var t = NewTex(W, H);

            // Feathers (bottom)
            Px(t, 1, 0, RedMid);  Px(t, 4, 0, RedMid);
            Px(t, 1, 1, RedDeep); Px(t, 4, 1, RedDeep);
            Px(t, 2, 1, WoodLite); Px(t, 3, 1, WoodLite);

            // Shaft
            for (int y = 2; y < 11; y++)
            {
                Px(t, 2, y, WoodMid);
                Px(t, 3, y, WoodLite);
            }

            // Arrowhead (top)
            Px(t, 1, 10, StoneLite); Px(t, 4, 10, StoneLite);
            Px(t, 1, 11, StoneMid);  Px(t, 4, 11, StoneMid);
            Px(t, 2, 11, StoneLite); Px(t, 3, 11, StoneLite);
            Px(t, 2, 12, StoneMid);  Px(t, 3, 12, StoneMid);
            Px(t, 2, 13, StoneDark); Px(t, 3, 13, StoneDark);

            t.Apply();
            return Build(t);
        }

        Sprite MagicOrbSprite()
        {
            // 12×12 — glowing magenta orb with sparkle
            int s = 12;
            var t = NewTex(s, s);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f));
                if (d < 1.5f)       Px(t, x, y, White);
                else if (d < 3.0f)  Px(t, x, y, Pink);
                else if (d < 4.5f)  Px(t, x, y, Violet);
                else if (d < 5.5f)  Px(t, x, y, VioletDeep);
            }
            // sparkle
            Px(t, 2, 9, GoldLite);
            Px(t, 9, 2, GoldLite);
            t.Apply();
            return Build(t);
        }

        Sprite IceShardSprite()
        {
            // 10×12 — sharp ice crystal shard
            int W = 10, H = 12;
            var t = NewTex(W, H);

            // Diamond shape
            for (int y = 0; y < H; y++)
            {
                int half = y < H / 2 ? y + 1 : (H - y);
                for (int x = W / 2 - half; x <= W / 2 + half - 1; x++)
                {
                    int dx = x - W / 2;
                    var c = dx < 0 ? IceMid : IceLite;
                    if (Mathf.Abs(dx) == half - 1) c = IceDeep;
                    Px(t, x, y, c);
                }
            }
            // bright tip
            Px(t, W / 2, H - 1, White);
            Px(t, W / 2 - 1, H - 1, White);

            t.Apply();
            return Build(t);
        }

        Sprite CannonBallSprite()
        {
            // 10×10 — iron ball with shading
            int s = 10;
            var t = NewTex(s, s);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f));
                if (d < 4.5f)
                {
                    // shading: top-left lighter, bottom-right darker
                    float light = 1f - ((x + (s - y)) / (float)(s * 2));
                    Color c = Color.Lerp(StoneLite, Black, light * 0.85f);
                    Px(t, x, y, c);
                }
            }
            // highlight
            Px(t, 3, 7, StoneLite);
            Px(t, 2, 6, StoneLite);
            t.Apply();
            return Build(t);
        }

        // ════════════════════════════════════════════════════════════════════
        // ENEMY SPRITES (detailed)
        // ════════════════════════════════════════════════════════════════════
        Sprite GoblinSprite()
        {
            // 20×24 — green goblin with big eyes, ears, dagger
            int W = 20, H = 24;
            var t = NewTex(W, H);

            // Body (small tunic)
            for (int y = 4; y < 11; y++)
            for (int x = 6; x < W - 6; x++)
                Px(t, x, y, SkinGDark);
            // brown belt
            for (int x = 6; x < W - 6; x++)
                Px(t, x, 5, SkinBDark);
            // legs
            for (int y = 0; y < 4; y++)
            {
                Px(t, 7, y, SkinGreen); Px(t, 8, y, SkinGreen);
                Px(t, 11, y, SkinGreen); Px(t, 12, y, SkinGreen);
                Px(t, 7, y, SkinGDark); Px(t, 12, y, SkinGDark);
            }
            // feet
            for (int x = 6; x < 10; x++) Px(t, x, 0, SkinBDark);
            for (int x = 10; x < 14; x++) Px(t, x, 0, SkinBDark);

            // Arms
            for (int y = 7; y < 11; y++)
            {
                Px(t, 5, y, SkinGreen);
                Px(t, W - 6, y, SkinGreen);
            }
            // hand holding dagger
            Px(t, W - 6, 11, SkinGLite);
            Px(t, W - 5, 11, StoneLite);
            Px(t, W - 5, 12, StoneLite);
            Px(t, W - 5, 13, StoneMid);

            // Head (big oval)
            for (int y = 11; y < 19; y++)
            for (int x = 5; x < W - 5; x++)
            {
                int dx = x - W / 2;
                int dy = y - 15;
                if (dx * dx + dy * dy < 18) Px(t, x, y, SkinGreen);
            }
            // head shade
            for (int x = 5; x < 9; x++) Px(t, x, 14, SkinGDark);

            // Pointy ears
            Px(t, 4, 16, SkinGreen); Px(t, 4, 17, SkinGreen); Px(t, 3, 16, SkinGDark);
            Px(t, W - 5, 16, SkinGreen); Px(t, W - 5, 17, SkinGreen); Px(t, W - 4, 16, SkinGDark);

            // Big yellow eyes
            Px(t, 8, 16, White); Px(t, 11, 16, White);
            Px(t, 8, 15, Gold); Px(t, 11, 15, Gold);
            Px(t, 8, 16, Black); Px(t, 11, 16, Black);

            // Mouth (sharp teeth)
            for (int x = 8; x < 12; x++) Px(t, x, 12, Black);
            Px(t, 9, 13, White); Px(t, 11, 13, White);

            // Wild tuft on head
            Px(t, 9, 19, SkinGDark);
            Px(t, 10, 19, SkinGDark);
            Px(t, 10, 20, SkinGDark);

            t.Apply();
            return Build(t);
        }

        Sprite OrcSprite()
        {
            // 24×26 — beefy orc with armor and tusks
            int W = 24, H = 26;
            var t = NewTex(W, H);

            // Legs
            for (int y = 0; y < 5; y++)
            {
                for (int x = 7; x < 11; x++) Px(t, x, y, SkinGDark);
                for (int x = 13; x < 17; x++) Px(t, x, y, SkinGDark);
            }
            // Boots
            for (int x = 7; x < 11; x++) Px(t, x, 0, SkinBDark);
            for (int x = 13; x < 17; x++) Px(t, x, 0, SkinBDark);

            // Wide torso with armor
            for (int y = 5; y < 14; y++)
            for (int x = 5; x < W - 5; x++)
                Px(t, x, y, StoneMid);
            // armor edges
            for (int y = 5; y < 14; y++) { Px(t, 5, y, Black); Px(t, W - 6, y, Black); }
            // armor highlight
            for (int x = 6; x < W - 6; x++) Px(t, x, 13, StoneLite);
            // chest emblem
            for (int y = 8; y < 11; y++)
                for (int x = W / 2 - 2; x <= W / 2 + 1; x++)
                    Px(t, x, y, RedDeep);
            Px(t, W / 2, 9, Gold);

            // Arms with skin
            for (int y = 7; y < 13; y++)
            {
                Px(t, 4, y, SkinGreen);
                Px(t, W - 5, y, SkinGreen);
            }
            // weapon (club)
            for (int y = 4; y < 9; y++) Px(t, 3, y, WoodMid);
            Px(t, 2, 8, WoodDark); Px(t, 4, 8, WoodDark);
            Px(t, 2, 9, WoodMid); Px(t, 3, 9, WoodLite); Px(t, 4, 9, WoodMid);

            // Massive head
            for (int y = 14; y < 22; y++)
            for (int x = 7; x < W - 7; x++)
            {
                int dx = x - W / 2;
                int dy = y - 18;
                if (dx * dx + dy * dy < 22) Px(t, x, y, SkinGreen);
            }
            // head shade
            for (int y = 14; y < 22; y++) Px(t, 7, y, SkinGDark);

            // Tusks (white triangles below mouth)
            Px(t, 10, 14, White);
            Px(t, 13, 14, White);
            Px(t, 10, 15, White);
            Px(t, 13, 15, White);

            // Eyes (red, angry)
            Px(t, 9, 18, RedMid); Px(t, 14, 18, RedMid);
            Px(t, 9, 19, Black); Px(t, 14, 19, Black);
            // brow ridge
            for (int x = 8; x < 16; x++) Px(t, x, 20, SkinGDark);

            // Horns
            Px(t, 7, 21, StoneDark); Px(t, 6, 22, StoneDark); Px(t, 6, 23, StoneLite);
            Px(t, W - 8, 21, StoneDark); Px(t, W - 7, 22, StoneDark); Px(t, W - 7, 23, StoneLite);

            t.Apply();
            return Build(t);
        }

        Sprite GhostSprite()
        {
            // 20×22 — wispy ethereal ghost with hollow eyes
            int W = 20, H = 22;
            var t = NewTex(W, H);

            // Wispy bottom — irregular tail
            int[] tailX = { 5, 7, 9, 11, 13, 15 };
            for (int i = 0; i < tailX.Length; i++)
            {
                int height = (i % 2 == 0) ? 3 : 5;
                for (int y = 0; y < height; y++)
                {
                    Px(t, tailX[i], y, GhostDeep);
                    Px(t, tailX[i] + 1, y, GhostMain);
                }
            }

            // Body (rounded blob)
            for (int y = 5; y < 19; y++)
            for (int x = 3; x < W - 3; x++)
            {
                int dx = x - W / 2;
                int dy = y - 12;
                int r2 = dx * dx + dy * dy;
                if (r2 < 56)
                {
                    var c = (r2 < 20) ? GhostGlow : GhostMain;
                    Px(t, x, y, c);
                }
                else if (r2 < 70)
                    Px(t, x, y, GhostDeep);
            }

            // Hollow eyes
            for (int y = 13; y < 16; y++)
            {
                Px(t, 7, y, Black); Px(t, 8, y, Black);
                Px(t, 12, y, Black); Px(t, 13, y, Black);
            }
            // glowing pupils
            Px(t, 7, 14, Pink); Px(t, 13, 14, Pink);

            // Mouth (small O)
            Px(t, 9, 10, Black); Px(t, 10, 10, Black); Px(t, 11, 10, Black);
            Px(t, 9, 9, Black); Px(t, 11, 9, Black);
            Px(t, 10, 9, GhostDeep);

            // Subtle sparkle around
            Px(t, 2, 16, GhostGlow);
            Px(t, W - 3, 16, GhostGlow);

            t.Apply();
            return Build(t);
        }

        // ════════════════════════════════════════════════════════════════════
        // CASTLE (defender's Base decoration)
        // ════════════════════════════════════════════════════════════════════
        Sprite CastleSprite()
        {
            // 48×40 — central keep + two side towers + flags
            int W = 48, H = 40;
            var t = NewTex(W, H);

            // Foundation
            for (int y = 0; y < 6; y++)
            for (int x = 2; x < W - 2; x++)
            {
                bool brick = ((x / 4 + y / 2) % 2 == 0);
                Px(t, x, y, brick ? StoneMid : StoneDark);
            }
            for (int x = 2; x < W - 2; x++) Px(t, x, 3, StoneDark);

            // Left tower (column)
            for (int y = 6; y < 30; y++)
            for (int x = 4; x < 12; x++)
            {
                bool brick = ((x / 3 + y / 3) % 2 == 0);
                Px(t, x, y, brick ? StoneLite : StoneMid);
            }
            for (int y = 6; y < 30; y++) { Px(t, 4, y, Black); Px(t, 11, y, Black); }

            // Right tower
            for (int y = 6; y < 30; y++)
            for (int x = W - 12; x < W - 4; x++)
            {
                bool brick = ((x / 3 + y / 3) % 2 == 0);
                Px(t, x, y, brick ? StoneLite : StoneMid);
            }
            for (int y = 6; y < 30; y++) { Px(t, W - 12, y, Black); Px(t, W - 5, y, Black); }

            // Central keep (wider, taller)
            for (int y = 6; y < 26; y++)
            for (int x = 14; x < W - 14; x++)
            {
                bool brick = ((x / 4 + y / 3) % 2 == 0);
                Px(t, x, y, brick ? StoneMid : StoneDark);
            }
            for (int y = 6; y < 26; y++) { Px(t, 14, y, Black); Px(t, W - 15, y, Black); }

            // Gate (arch, dark)
            for (int y = 6; y < 18; y++)
            for (int x = 21; x < W - 21; x++)
                Px(t, x, y, WoodDark);
            // arch top
            for (int x = 22; x < W - 22; x++) Px(t, x, 18, WoodDark);
            Px(t, 21, 17, WoodDark); Px(t, W - 22, 17, WoodDark);
            // gate planks
            for (int y = 6; y < 17; y += 2)
                for (int x = 22; x < W - 22; x++)
                    Px(t, x, y, WoodMid);
            // gate ironbands
            for (int x = 21; x < W - 21; x++) { Px(t, x, 9, StoneDark); Px(t, x, 13, StoneDark); }

            // Crenellations on towers
            for (int x = 4; x < 12; x++)
            {
                bool gap = ((x - 4) % 3) < 1;
                Px(t, x, 30, gap ? StoneLite : Black);
                if (!gap) Px(t, x, 31, StoneDark);
            }
            for (int x = W - 12; x < W - 4; x++)
            {
                bool gap = ((x - (W - 12)) % 3) < 1;
                Px(t, x, 30, gap ? StoneLite : Black);
                if (!gap) Px(t, x, 31, StoneDark);
            }

            // Central keep crenellations
            for (int x = 14; x < W - 14; x++)
            {
                bool gap = ((x - 14) % 4) < 2;
                Px(t, x, 26, gap ? StoneLite : Black);
                if (!gap) Px(t, x, 27, StoneDark);
            }

            // Conical roofs on towers
            for (int y = 31; y < 38; y++)
            {
                int half = 38 - y;
                for (int x = 7 - half; x <= 7 + half; x++)
                    Px(t, x, y, RedDeep);
                Px(t, 7 - half, y, Black); Px(t, 7 + half, y, Black);

                for (int x = (W - 9) - half; x <= (W - 9) + half; x++)
                    Px(t, x, y, RedDeep);
                Px(t, (W - 9) - half, y, Black); Px(t, (W - 9) + half, y, Black);
            }

            // Central keep roof (flat with banner)
            for (int x = 14; x < W - 14; x++) Px(t, x, 28, RedDeep);

            // Flags
            for (int y = 38; y < 40; y++) Px(t, 7, y, WoodDark);
            Px(t, 8, 39, GoldLite); Px(t, 9, 39, GoldLite);
            Px(t, 8, 38, Gold); Px(t, 9, 38, Gold);

            for (int y = 38; y < 40; y++) Px(t, W - 9, y, WoodDark);
            Px(t, W - 8, 39, GoldLite); Px(t, W - 7, 39, GoldLite);

            for (int y = 28; y < 32; y++) Px(t, W / 2, y, WoodDark);
            Px(t, W / 2 + 1, 31, RedMid); Px(t, W / 2 + 2, 31, RedMid);
            Px(t, W / 2 + 1, 30, Gold); Px(t, W / 2 + 2, 30, Gold);

            // Windows on towers (with glow)
            Px(t, 7, 18, Gold); Px(t, 8, 18, Gold);
            Px(t, W - 9, 18, Gold); Px(t, W - 8, 18, Gold);
            Px(t, 7, 22, Gold); Px(t, 8, 22, Gold);
            Px(t, W - 9, 22, Gold); Px(t, W - 8, 22, Gold);

            t.Apply();
            return Build(t);
        }

        // ════════════════════════════════════════════════════════════════════
        // PIXEL HELPERS
        // ════════════════════════════════════════════════════════════════════
        static readonly Color Transparent = new(0f, 0f, 0f, 0f);

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Transparent;
            t.SetPixels(px);
            return t;
        }

        static void Px(Texture2D t, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
            t.SetPixel(x, y, c);
        }

        static Sprite Build(Texture2D t)
            => Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                             new Vector2(0.5f, 0.5f), 32);
    }
}
