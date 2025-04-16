using UnityEngine;
using System.Collections.Generic;

namespace Makra.Rendering.Tools
{
    /// <summary>
    ///   extracts one or more pixel‑perfect outline polygons from a sprite
    ///   ‑ works entirely in pixel space; the caller can convert to world units
    ///   ‑ outer contours are clockwise; inner holes are counter‑clockwise
    ///   ‑ collinear vertices are culled for the smallest possible path
    /// </summary>
    public static class PixelPerfectSpritePathCalculator
    {
        ///   any texel with α &lt; threshold is treated as empty</param>
        /// <returns>
        ///   an array of polygons; each polygon is an array of <see cref="Vector2Int"/>
        ///   whose coordinates are in the sprite’s own pixel space
        ///   (zero in the bottom‑left of <see cref="spriterect"/>)
        ///   first and last vertex are *not* repeated
        ///   a <c>null</c> / length‑0 array means “no opaque pixels”
        /// </returns>
        public static Vector2Int[][] TraceSprite(Sprite sprite, float alphaThreshold = .5f)
        {
            // --- step 1 -  pull sprite pixels into a bool mask --------------------
            Rect r          = sprite.rect;
            int  w          = (int)r.width;
            int  h          = (int)r.height;
            int  texWidth   = sprite.texture.width;
            int  startX     = (int)r.x;
            int  startY     = (int)r.y;

            Color32[] texels = sprite.texture.GetPixels32();          // full tex
            bool[,] solid    = new bool[w, h];                        // local mask

            for (int y = 0; y < h; ++y)
            {
                int row = texWidth * (startY + y);
                for (int x = 0; x < w; ++x)
                    solid[x, y] = texels[row + startX + x].a / 255f >= alphaThreshold;
            }

            // quick out: fully transparent
            if (System.Linq.Enumerable.All(solid, b => !b))
                return System.Array.Empty<Vector2Int[]>();

            // --- step 2 -  march the grid, collecting border edges -------------------
            var edges = new List<Edge>(w * h);         // rough upper bound
            for (int y = 0; y < h; ++y)
            for (int x = 0; x < w; ++x)
            {
                if (!solid[x, y]) continue;

                //   for every side where the neighbour is empty/out‑of‑bounds
                //   emit a directed edge that runs clockwise around the pixel
                //   Pixel (x,y) spans [x,x+1]×[y,y+1] on the grid

                // top
                if (y == h - 1 || !solid[x, y + 1])
                    edges.Add(new Edge(new Vector2Int(x,     y + 1),
                                       new Vector2Int(x + 1, y + 1)));
                // right
                if (x == w - 1 || !solid[x + 1, y])
                    edges.Add(new Edge(new Vector2Int(x + 1, y + 1),
                                       new Vector2Int(x + 1, y    )));
                // bottom
                if (y == 0 || !solid[x, y - 1])
                    edges.Add(new Edge(new Vector2Int(x + 1, y    ),
                                       new Vector2Int(x,     y    )));
                // left
                if (x == 0 || !solid[x - 1, y])
                    edges.Add(new Edge(new Vector2Int(x,     y    ),
                                       new Vector2Int(x,     y + 1)));
            }

            // --- step 3 -  Stitch edges into closed loops ----------------------------
            // map: start‑vertex → list of outgoing edge indices
            var fan = new Dictionary<Vector2Int, List<int>>(edges.Count);
            for (int i = 0; i < edges.Count; ++i)
            {
                if (!fan.TryGetValue(edges[i].a, out var lst))
                    fan[edges[i].a] = lst = new List<int>(2);
                lst.Add(i);
            }

            bool[] used = new bool[edges.Count];
            var    polys = new List<Vector2Int[]>();

            for (int i = 0; i < edges.Count; ++i)
            {
                if (used[i]) continue;

                var path = new List<Vector2Int>(64);
                int  eIdx     = i;
                var  start    = edges[eIdx].a;
                var  curr     = start;

                while (true)
                {
                    used[eIdx] = true;
                    path.Add(curr);

                    var nextV = edges[eIdx].b;
                    if (nextV == start) break;           // closed the ring

                    // pick next unused edge that starts at nextV
                    if (!fan.TryGetValue(nextV, out var candidates))
                        break;                           // should not happen (open edge)

                    int nxt = -1;
                    foreach (int c in candidates)
                        if (!used[c]) { nxt = c; break; }

                    if (nxt < 0) break;                  // isolated cusp (also rare)
                    eIdx = nxt;
                    curr = nextV;
                }

                if (path.Count >= 3)
                {
                    CullCollinear(path);
                    if (path.Count >= 3)
                        polys.Add(path.ToArray());
                }
            }

            return polys.ToArray();
        }


        // ---------- helpers ----------------------------------------------------

        private struct Edge
        {
            public Vector2Int a, b;
            public Edge(Vector2Int a, Vector2Int b) { this.a = a; this.b = b; }
        }

        /// <summary>Removes vertices that fall on a straight line.</summary>
        private static void CullCollinear(List<Vector2Int> p)
        {
            int i = 0;
            while (p.Count >= 3 && i < p.Count)
            {
                Vector2Int prev = p[(i - 1 + p.Count) % p.Count];
                Vector2Int curr = p[i];
                Vector2Int next = p[(i + 1) % p.Count];

                var  v1 = curr - prev;
                var  v2 = next - curr;
                long cross = (long)v1.x * v2.y - (long)v1.y * v2.x;

                if (cross == 0) p.RemoveAt(i);          // collinear ⇒ drop
                else            ++i;
            }
        }
    }
}
