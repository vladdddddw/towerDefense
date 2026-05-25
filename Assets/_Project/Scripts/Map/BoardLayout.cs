using UnityEngine;

namespace Vanguard.TD.Map
{
    /// <summary>
    /// Discrete 12×8 board with a fixed route. Renders tile sprites and
    /// performs world↔grid conversions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardLayout : MonoBehaviour
    {
        public static BoardLayout Live { get; private set; }

        [Header("Dimensions")]
        public int columns = 12;
        public int rows    = 8;
        public Vector2 origin = new(-5.5f, -3.5f);

        [Header("Route (set by builder)")]
        public Transform[] route;

        [Header("Tile colors — sunny meadow")]
        public Color tileTone   = new(0.42f, 0.68f, 0.30f);   // bright grass
        public Color tileTone2  = new(0.30f, 0.55f, 0.22f);   // shaded grass
        public Color routeTone  = new(0.78f, 0.62f, 0.38f);   // warm sand
        public Color routeTone2 = new(0.58f, 0.43f, 0.22f);   // dark earth
        public Color gridLine   = new(0.20f, 0.10f, 0.08f, 0.30f);

        BoardCell[,] _cells;
        const int TileTexSize = 24;

        public Vector3 OriginWorld => new(origin.x, origin.y, 0f);

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
            Build();
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        // ── Building visuals ─────────────────────────────────────────────────
        void Build()
        {
            _cells = new BoardCell[columns, rows];
            var routeSet = MarkRoute();

            var tileSprite  = MakeHexTile(tileTone);
            var routeSprite = MakeRouteTile(routeTone);

            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                bool isRoute = routeSet.Contains(new Vector2Int(x, y));
                var go = new GameObject($"Cell_{x}_{y}");
                go.transform.SetParent(transform, false);
                go.transform.position = ToWorld(new Vector2Int(x, y));

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = isRoute ? routeSprite : tileSprite;
                sr.sortingOrder = 0;

                var cell = go.AddComponent<BoardCell>();
                cell.Coord   = new Vector2Int(x, y);
                cell.OnRoute = isRoute;
                _cells[x, y] = cell;
            }
        }

        System.Collections.Generic.HashSet<Vector2Int> MarkRoute()
        {
            var set = new System.Collections.Generic.HashSet<Vector2Int>();
            if (route == null || route.Length < 2) return set;
            for (int i = 0; i < route.Length - 1; i++)
            {
                var a = ToCoord(route[i].position);
                var b = ToCoord(route[i + 1].position);
                if (a.x == b.x)
                {
                    int lo = Mathf.Min(a.y, b.y), hi = Mathf.Max(a.y, b.y);
                    for (int y = lo; y <= hi; y++) set.Add(new Vector2Int(a.x, y));
                }
                else if (a.y == b.y)
                {
                    int lo = Mathf.Min(a.x, b.x), hi = Mathf.Max(a.x, b.x);
                    for (int x = lo; x <= hi; x++) set.Add(new Vector2Int(x, a.y));
                }
            }
            return set;
        }

        // ── Public API ───────────────────────────────────────────────────────
        public Vector3 ToWorld(Vector2Int c) => new(origin.x + c.x, origin.y + c.y, 0f);

        public Vector2Int ToCoord(Vector3 world) => new(
            Mathf.RoundToInt(world.x - origin.x),
            Mathf.RoundToInt(world.y - origin.y));

        public BoardCell Lookup(Vector2Int coord)
        {
            if (coord.x < 0 || coord.x >= columns || coord.y < 0 || coord.y >= rows) return null;
            return _cells[coord.x, coord.y];
        }

        public bool CanBuildAt(Vector2Int coord) => Lookup(coord)?.Buildable ?? false;

        public void MarkOccupied(Vector2Int coord, bool occupied = true)
        {
            var c = Lookup(coord);
            if (c != null) c.Occupied = occupied;
        }

        public void MarkVacated(Vector2Int coord) => MarkOccupied(coord, false);

        // ── Procedural tile sprites ──────────────────────────────────────────
        // GRASS — painted meadow with rounded corners, soft gradient, flowers
        Sprite MakeHexTile(Color baseCol)
        {
            int s = TileTexSize;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var rng = new System.Random(baseCol.GetHashCode());

            // Soft radial gradient — brighter in the middle, darker at edges
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x - s * 0.5f) / (s * 0.5f);
                float dy = (y - s * 0.5f) / (s * 0.5f);
                float r2 = dx * dx + dy * dy;
                float v = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                var c = Color.Lerp(tileTone2, baseCol, v);
                c = Color.Lerp(c, tileTone2, Mathf.Clamp01(r2 * 0.4f));
                tex.SetPixel(x, y, c);
            }

            // Sparse flower/bush specks — 5 random colored dots per tile
            var flowerHues = new[]
            {
                new Color(1.00f, 0.95f, 0.45f),   // yellow daisy
                new Color(0.98f, 0.55f, 0.85f),   // pink flower
                new Color(1.00f, 1.00f, 1.00f),   // white
                new Color(0.95f, 0.30f, 0.30f),   // red poppy
            };
            for (int i = 0; i < 5; i++)
            {
                int fx = rng.Next(2, s - 2);
                int fy = rng.Next(2, s - 2);
                var hue = flowerHues[rng.Next(flowerHues.Length)];
                tex.SetPixel(fx,     fy,     hue);
                if (rng.Next(2) == 0)
                {
                    tex.SetPixel(fx + 1, fy, hue * 0.85f);
                    tex.SetPixel(fx,     fy + 1, hue * 0.85f);
                }
            }

            // Rounded corner pixels (transparent-ish)
            int corner = 1;
            for (int i = 0; i < corner; i++)
            {
                tex.SetPixel(i, i, gridLine);
                tex.SetPixel(s - 1 - i, i, gridLine);
                tex.SetPixel(i, s - 1 - i, gridLine);
                tex.SetPixel(s - 1 - i, s - 1 - i, gridLine);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        // PATH — sandy dirt with scattered pebbles, no rigid bricks
        Sprite MakeRouteTile(Color baseCol)
        {
            int s = TileTexSize;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var rng = new System.Random(baseCol.GetHashCode() ^ 0x5A);

            // Soft sandy gradient
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float v = Mathf.PerlinNoise(x * 0.22f, y * 0.22f);
                float v2 = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                var c = Color.Lerp(routeTone2, baseCol, v * 0.7f + v2 * 0.3f);
                tex.SetPixel(x, y, c);
            }

            // Footprint / wheel-track ribbons (two thin parallel curves)
            for (int x = 0; x < s; x++)
            {
                int y1 = (int)(s * 0.35f + Mathf.Sin(x * 0.6f) * 1.2f);
                int y2 = (int)(s * 0.65f + Mathf.Sin(x * 0.6f + 1f) * 1.2f);
                tex.SetPixel(x, y1, routeTone2);
                tex.SetPixel(x, y2, routeTone2);
            }

            // Scattered pebbles
            for (int i = 0; i < 6; i++)
            {
                int px = rng.Next(2, s - 2);
                int py = rng.Next(2, s - 2);
                var grey = new Color(0.55f, 0.50f, 0.45f);
                tex.SetPixel(px, py, grey);
                if (rng.Next(2) == 0)
                {
                    tex.SetPixel(px + 1, py, grey * 0.8f);
                    tex.SetPixel(px, py + 1, grey * 0.8f);
                }
            }

            // Soft tile outline
            for (int i = 0; i < s; i++)
            {
                tex.SetPixel(i, 0, gridLine);
                tex.SetPixel(i, s - 1, gridLine);
                tex.SetPixel(0, i, gridLine);
                tex.SetPixel(s - 1, i, gridLine);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }
    }
}
