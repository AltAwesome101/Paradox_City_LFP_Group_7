using Forestlevel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


internal static class HubQuad
{
    private static Mesh s_mesh;

    
    public static Mesh Shared
    {
        get
        {
            if (s_mesh == null)
            {
                s_mesh = new Mesh { name = "HubQuad" };
                s_mesh.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f,  0.5f, 0f), new Vector3(0.5f,  0.5f, 0f)
                };
                s_mesh.uv = new[]
                {
                    new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0f, 1f), new Vector2(1f, 1f)
                };
                s_mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                s_mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
                s_mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                s_mesh.RecalculateBounds();
            }
            return s_mesh;
        }
    }

    public static GameObject Create(string name, Transform parent, Material mat)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);

        go.AddComponent<MeshFilter>().sharedMesh = Shared;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return go;
    }

    
    public static Material MakeMaterial(Texture tex, Color tint)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Transparent");
        if (sh == null) sh = Shader.Find("Unlit/Texture");
        if (sh == null)
        {
            Debug.LogError("[HubMissionExtras] No unlit shader found for generated quads.");
            return null;
        }

        var m = new Material(sh);
        m.mainTexture = tex;
        if (m.HasProperty("_Color")) m.color = tint;
        return m;
    }
}




internal sealed class PixelCanvas
{
    public readonly int W, H;
    public readonly Color32[] Px;

    
    private static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        { 'A', ".###." + "#...#" + "#...#" + "#####" + "#...#" + "#...#" + "#...#" },
        { 'B', "####." + "#...#" + "#...#" + "####." + "#...#" + "#...#" + "####." },
        { 'C', ".###." + "#...#" + "#...." + "#...." + "#...." + "#...#" + ".###." },
        { 'D', "####." + "#...#" + "#...#" + "#...#" + "#...#" + "#...#" + "####." },
        { 'E', "#####" + "#...." + "#...." + "####." + "#...." + "#...." + "#####" },
        { 'F', "#####" + "#...." + "#...." + "####." + "#...." + "#...." + "#...." },
        { 'G', ".###." + "#...#" + "#...." + "#.###" + "#...#" + "#...#" + ".###." },
        { 'H', "#...#" + "#...#" + "#...#" + "#####" + "#...#" + "#...#" + "#...#" },
        { 'I', ".###." + "..#.." + "..#.." + "..#.." + "..#.." + "..#.." + ".###." },
        { 'J', "..###" + "...#." + "...#." + "...#." + "...#." + "#..#." + ".##.." },
        { 'K', "#...#" + "#..#." + "#.#.." + "##..." + "#.#.." + "#..#." + "#...#" },
        { 'L', "#...." + "#...." + "#...." + "#...." + "#...." + "#...." + "#####" },
        { 'M', "#...#" + "##.##" + "#.#.#" + "#.#.#" + "#...#" + "#...#" + "#...#" },
        { 'N', "#...#" + "##..#" + "##..#" + "#.#.#" + "#..##" + "#..##" + "#...#" },
        { 'O', ".###." + "#...#" + "#...#" + "#...#" + "#...#" + "#...#" + ".###." },
        { 'P', "####." + "#...#" + "#...#" + "####." + "#...." + "#...." + "#...." },
        { 'R', "####." + "#...#" + "#...#" + "####." + "#.#.." + "#..#." + "#...#" },
        { 'S', ".####" + "#...." + "#...." + ".###." + "....#" + "....#" + "####." },
        { 'T', "#####" + "..#.." + "..#.." + "..#.." + "..#.." + "..#.." + "..#.." },
        { 'U', "#...#" + "#...#" + "#...#" + "#...#" + "#...#" + "#...#" + ".###." },
        { 'V', "#...#" + "#...#" + "#...#" + "#...#" + "#...#" + ".#.#." + "..#.." },
        { 'W', "#...#" + "#...#" + "#...#" + "#.#.#" + "#.#.#" + "##.##" + "#...#" },
        { 'Y', "#...#" + "#...#" + ".#.#." + "..#.." + "..#.." + "..#.." + "..#.." },
        { '.', "....." + "....." + "....." + "....." + "....." + ".##.." + ".##.." },
        { '!', "..#.." + "..#.." + "..#.." + "..#.." + "..#.." + "....." + "..#.." },
    };

    public PixelCanvas(int w, int h, Color32 fill)
    {
        W = w;
        H = h;
        Px = new Color32[w * h];
        for (int i = 0; i < Px.Length; i++) Px[i] = fill;
    }

    public static Color32 C(int r, int g, int b)
    {
        return new Color32((byte)r, (byte)g, (byte)b, 255);
    }

    public static Color32 Mix(Color32 a, Color32 b, float t)
    {
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
            255);
    }

    

    public void Box(int x, int y, int w, int h, Color32 col)
    {
        int x0 = Mathf.Max(0, x), y0 = Mathf.Max(0, y);
        int x1 = Mathf.Min(W, x + w), y1 = Mathf.Min(H, y + h);
        for (int yy = y0; yy < y1; yy++)
        {
            int row = yy * W;
            for (int xx = x0; xx < x1; xx++) Px[row + xx] = col;
        }
    }

    
    public void VGrad(int x0, int x1, int y0, int y1, Color32 bottom, Color32 top)
    {
        x0 = Mathf.Max(0, x0);
        x1 = Mathf.Min(W, x1);
        for (int y = Mathf.Max(0, y0); y < Mathf.Min(H, y1); y++)
        {
            float t = (y - y0) / (float)Mathf.Max(1, y1 - y0 - 1);
            Color32 col = Mix(bottom, top, t);
            int row = y * W;
            for (int x = x0; x < x1; x++) Px[row + x] = col;
        }
    }

    public void Circle(int cx, int cy, int r, Color32 col)
    {
        int rr = r * r;
        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(H - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(W - 1, cx + r); x++)
            {
                int dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= rr) Px[y * W + x] = col;
            }
    }

    public void Ring(int cx, int cy, int r, int thick, Color32 col)
    {
        int ro = r * r;
        int ri = (r - thick) * (r - thick);
        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(H - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(W - 1, cx + r); x++)
            {
                int dx = x - cx, dy = y - cy;
                int d2 = dx * dx + dy * dy;
                if (d2 <= ro && d2 >= ri) Px[y * W + x] = col;
            }
    }

    public void Ellipse(int cx, int cy, int rx, int ry, Color32 col)
    {
        for (int y = Mathf.Max(0, cy - ry); y <= Mathf.Min(H - 1, cy + ry); y++)
            for (int x = Mathf.Max(0, cx - rx); x <= Mathf.Min(W - 1, cx + rx); x++)
            {
                float dx = (x - cx) / (float)rx, dy = (y - cy) / (float)ry;
                if (dx * dx + dy * dy <= 1f) Px[y * W + x] = col;
            }
    }

    public void Tri(float x0, float y0, float x1, float y1, float x2, float y2, Color32 col)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, Mathf.Min(x1, x2))));
        int maxX = Mathf.Min(W - 1, Mathf.CeilToInt(Mathf.Max(x0, Mathf.Max(x1, x2))));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, Mathf.Min(y1, y2))));
        int maxY = Mathf.Min(H - 1, Mathf.CeilToInt(Mathf.Max(y0, Mathf.Max(y1, y2))));

        float d = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
        if (Mathf.Abs(d) < 0.0001f) return;

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float a = ((y1 - y2) * (x - x2) + (x2 - x1) * (y - y2)) / d;
                float b = ((y2 - y0) * (x - x2) + (x0 - x2) * (y - y2)) / d;
                float g = 1f - a - b;
                if (a >= 0f && b >= 0f && g >= 0f) Px[y * W + x] = col;
            }
    }

   
    public void Line(float x0, float y0, float x1, float y1, float thick, Color32 col)
    {
        float r = thick * 0.5f;
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - r));
        int maxX = Mathf.Min(W - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + r));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - r));
        int maxY = Mathf.Min(H - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + r));

        float dx = x1 - x0, dy = y1 - y0;
        float len2 = dx * dx + dy * dy;
        float rr = r * r;

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(((x - x0) * dx + (y - y0) * dy) / len2);
                float px = x0 + dx * t - x;
                float py = y0 + dy * t - y;
                if (px * px + py * py <= rr) Px[y * W + x] = col;
            }
    }

    public void Border(int t, Color32 col)
    {
        Box(0, 0, W, t, col);
        Box(0, H - t, W, t, col);
        Box(0, 0, t, H, col);
        Box(W - t, 0, t, H, col);
    }

   

    
    public void Grain(float amount, int seed)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                uint h = unchecked((uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791));
                h ^= h >> 13;
                h = unchecked(h * 1274126177u);
                h ^= h >> 16;

                float n = ((h & 0xFFFF) / 65535f - 0.5f) * 2f * amount;
                Color32 p = Px[y * W + x];
                Px[y * W + x] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.r + n), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.g + n), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.b + n), 0, 255),
                    255);
            }
    }

    
    public void Vignette(float strength)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float nx = (x - W * 0.5f) / (W * 0.5f);
                float ny = (y - H * 0.5f) / (H * 0.5f);
                float d = Mathf.Clamp01((nx * nx + ny * ny) * 0.5f);
                float f = 1f - strength * d * d * 1.6f;
                Color32 p = Px[y * W + x];
                Px[y * W + x] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.r * f), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.g * f), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.b * f), 0, 255),
                    255);
            }
    }

    public Texture2D ToTexture()
    {
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
        tex.SetPixels32(Px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.anisoLevel = 4;
        tex.Apply(true);
        return tex;
    }

    

    private static List<string> Wrap(string text, int maxChars)
    {
        var lines = new List<string>();
        foreach (string para in text.Split('|')) 
        {
            string cur = "";
            foreach (string word in para.Split(' '))
            {
                if (cur.Length == 0) cur = word;
                else if (cur.Length + 1 + word.Length <= maxChars) cur += " " + word;
                else
                {
                    lines.Add(cur);
                    cur = word;
                }
            }
            if (cur.Length > 0) lines.Add(cur);
        }
        return lines;
    }

    private void DrawString(string s, int x, int y, int scale, Color32 col)
    {
        foreach (char ch in s)
        {
            if (Glyphs.TryGetValue(ch, out string g))
            {
                for (int row = 0; row < 7; row++)
                    for (int cc = 0; cc < 5; cc++)
                        if (g[row * 5 + cc] == '#')
                            Box(x + cc * scale, y + (6 - row) * scale, scale, scale, col);
            }
            x += 6 * scale;
        }
    }

    
    public void FitText(string text, int x0, int x1, int y0, int y1, int maxScale, Color32 col, Color32 shadow)
    {
        int availW = x1 - x0, availH = y1 - y0;

        for (int s = maxScale; s >= 1; s--)
        {
            int maxChars = Mathf.Max(1, (availW + s) / (6 * s));
            List<string> lines = Wrap(text, maxChars);

            int lineH = 7 * s, gap = Mathf.Max(2, s * 2);
            int total = lines.Count * lineH + (lines.Count - 1) * gap;

            bool tooWide = false;
            foreach (string l in lines) if (l.Length > maxChars) tooWide = true;

            if ((total <= availH && !tooWide) || s == 1)
            {
                int top = y1 - (availH - total) / 2;
                for (int i = 0; i < lines.Count; i++)
                {
                    string l = lines[i];
                    int lw = l.Length * 6 * s - s;
                    int x = x0 + (availW - lw) / 2;
                    int yBase = top - i * (lineH + gap) - lineH;
                    DrawString(l, x + s, yBase - s, s, shadow);
                    DrawString(l, x, yBase, s, col);
                }
                return;
            }
        }
    }
}



internal static class PosterArt
{
    public const int W = 256;
    public const int H = 384;
    private const int HeadY0 = 320; 
    private const int FootY1 = 96;  

    private static Color32 C(int r, int g, int b) { return PixelCanvas.C(r, g, b); }

    // ---- "before": dark red / black / dirty cream / dark grey ----
    private static readonly Color32 Black = C(14, 14, 16);
    private static readonly Color32 DarkRed = C(110, 14, 20);
    private static readonly Color32 Red = C(175, 25, 30);
    private static readonly Color32 Cream = C(214, 200, 160);
    private static readonly Color32 CreamDim = C(170, 158, 125);
    private static readonly Color32 Grey = C(62, 64, 68);
    private static readonly Color32 GreyL = C(95, 97, 100);
    private static readonly Color32 GreyD = C(36, 37, 41);

    // ---- "after": bright red / yellow / cream / light blue ----
    private static readonly Color32 URed = C(222, 38, 34);
    private static readonly Color32 UYellow = C(252, 212, 50);
    private static readonly Color32 UCream = C(250, 240, 208);
    private static readonly Color32 ULBlue = C(135, 200, 238);
    private static readonly Color32 ULBlue2 = C(196, 228, 246);
    private static readonly Color32 Navy = C(28, 40, 86);
    private static readonly Color32 White = C(255, 255, 250);

    public static Texture2D Make(bool utopia, int idx)
    {
        var c = new PixelCanvas(W, H, Black);
        string title, slogan;

        if (!utopia)
        {
            if (idx == 0) { D1(c); title = "THE UNEMPLOYED GENERATION"; slogan = "YOUR FUTURE HAS BEEN TAKEN."; }
            else if (idx == 1) { D2(c); title = "WORK IS A PRIVILEGE"; slogan = "NOT EVERYONE DESERVES A JOB."; }
            else { D3(c); title = "THE LOST FUTURE"; slogan = "NO JOB.|NO FUTURE.|NO HOPE."; }

            c.Box(0, HeadY0, W, H - HeadY0, DarkRed);
            c.Box(0, 0, W, FootY1, Black);
            c.Box(0, HeadY0 - 3, W, 3, Cream);
            c.Box(0, FootY1, W, 3, Cream);

            c.Grain(24f, idx + 1);
            c.Vignette(0.55f);
            c.Border(3, CreamDim);

            c.FitText(title, 8, W - 8, HeadY0 + 4, H - 4, 3, Cream, Black);
            c.FitText(slogan, 8, W - 8, 4, FootY1 - 4, 3, Cream, Red);
        }
        else
        {
            if (idx == 0) { U1(c); title = "A FUTURE FOR EVERYONE"; slogan = "EVERY HAND BUILDS THE FUTURE."; }
            else if (idx == 1) { U2(c); title = "EMPLOYMENT FOR ALL"; slogan = "WORK IS DIGNITY. OPPORTUNITY IS FREEDOM."; }
            else { U3(c); title = "HISTORY CHANGED. THE FUTURE RESTORED."; slogan = "ONE MOMENT CHANGED EVERYTHING."; }

            c.Box(0, HeadY0, W, H - HeadY0, UCream);
            c.Box(0, 0, W, FootY1, URed);
            c.Box(0, HeadY0 - 4, W, 4, UYellow);
            c.Box(0, FootY1, W, 4, UYellow);

            c.Grain(7f, idx + 11);
            c.Vignette(0.18f);
            c.Border(4, UYellow);

            c.FitText(title, 8, W - 8, HeadY0 + 4, H - 4, 3, URed, Navy);
            c.FitText(slogan, 8, W - 8, 4, FootY1 - 4, 3, UYellow, DarkRed);
        }

        return c.ToTexture();
    }

    // =========================================================================== figures

    private struct Pose
    {
        public int shL, shR, shY, hx, hy, hr, hipY, tw, th;
    }

    private static Pose HumanBody(PixelCanvas c, int cx, int by, int fh, Color32 skin, Color32 shirt, Color32 pants)
    {
        int headR = Mathf.Max(2, fh * 10 / 100);
        int legH = fh * 40 / 100;
        int torsoH = fh * 34 / 100;
        int torsoW = Mathf.Max(6, fh * 30 / 100);
        int legW = Mathf.Max(2, torsoW * 2 / 5);

        c.Box(cx - torsoW / 2 + 1, by, legW, legH, pants);
        c.Box(cx + torsoW / 2 - legW - 1, by, legW, legH, pants);
        c.Box(cx - torsoW / 2, by + legH, torsoW, torsoH, shirt);

        int headY = by + legH + torsoH + headR + 1;
        c.Circle(cx, headY, headR, skin);

        Pose p = new Pose();
        p.shL = cx - torsoW / 2;
        p.shR = cx + torsoW / 2;
        p.shY = by + legH + torsoH - 3;
        p.hx = cx;
        p.hy = headY;
        p.hr = headR;
        p.hipY = by + legH;
        p.tw = torsoW;
        p.th = torsoH;
        return p;
    }

    private static Pose Human(PixelCanvas c, int cx, int by, int fh, Color32 skin, Color32 shirt, Color32 pants, bool outline)
    {
        if (outline)
        {
            HumanBody(c, cx - 2, by, fh, Navy, Navy, Navy);
            HumanBody(c, cx + 2, by, fh, Navy, Navy, Navy);
            HumanBody(c, cx, by - 2, fh, Navy, Navy, Navy);
            HumanBody(c, cx, by + 2, fh, Navy, Navy, Navy);
        }
        return HumanBody(c, cx, by, fh, skin, shirt, pants);
    }

    private static void Arm(PixelCanvas c, float x0, float y0, float x1, float y1, int thick, Color32 col, Color32 hand, bool outline)
    {
        int hr = Mathf.Max(2, thick * 3 / 5);
        int ex = Mathf.RoundToInt(x1), ey = Mathf.RoundToInt(y1);
        if (outline)
        {
            c.Line(x0, y0, x1, y1, thick + 4, Navy);
            c.Circle(ex, ey, hr + 2, Navy);
        }
        c.Line(x0, y0, x1, y1, thick, col);
        c.Circle(ex, ey, hr, hand);
    }

    private static void ArmsDown(PixelCanvas c, Pose p, Color32 col, Color32 hand, bool outline)
    {
        int armW = Mathf.Max(2, p.tw / 4);
        Arm(c, p.shL - 1, p.shY, p.shL - 2, p.shY - p.th * 0.85f, armW, col, hand, outline);
        Arm(c, p.shR + 1, p.shY, p.shR + 2, p.shY - p.th * 0.85f, armW, col, hand, outline);
    }

    private static void ArmsToward(PixelCanvas c, Pose p, float tx, float ty, float len, Color32 col, Color32 hand, bool outline)
    {
        int armW = Mathf.Max(2, p.tw / 4);
        for (int side = 0; side < 2; side++)
        {
            float sx = side == 0 ? p.shL : p.shR;
            float dx = tx - sx, dy = ty - p.shY;
            float m = Mathf.Sqrt(dx * dx + dy * dy);
            if (m < 0.001f) m = 1f;
            Arm(c, sx, p.shY, sx + dx / m * len, p.shY + dy / m * len, armW, col, hand, outline);
        }
    }

    
    private static void CrowdRow(PixelCanvas c, int by, int fh, int startX, int step, int endX,
                                 Color32 shirt, Color32 pants, Color32 skin, float tx, float ty)
    {
        for (int x = startX; x <= endX; x += step)
        {
            Pose p = Human(c, x, by, fh, skin, shirt, pants, false);
            if (ty > 0f) ArmsToward(c, p, tx, ty, fh * 0.42f, shirt, skin, false);
            else ArmsDown(c, p, shirt, skin, false);
        }
    }

    private static void GearShape(PixelCanvas c, int cx, int cy, int r, int teeth, Color32 col, bool outlined)
    {
        int tr = Mathf.Max(3, r / 6);
        if (outlined)
        {
            c.Circle(cx, cy, r + 3, Navy);
            for (int k = 0; k < teeth; k++)
            {
                float a = k * Mathf.PI * 2f / teeth;
                c.Circle(cx + Mathf.RoundToInt(Mathf.Cos(a) * (r + tr * 0.4f)),
                         cy + Mathf.RoundToInt(Mathf.Sin(a) * (r + tr * 0.4f)), tr + 3, Navy);
            }
        }
        c.Circle(cx, cy, r, col);
        for (int k = 0; k < teeth; k++)
        {
            float a = k * Mathf.PI * 2f / teeth;
            c.Circle(cx + Mathf.RoundToInt(Mathf.Cos(a) * (r + tr * 0.4f)),
                     cy + Mathf.RoundToInt(Mathf.Sin(a) * (r + tr * 0.4f)), tr, col);
        }
    }

    private static void Smoke(PixelCanvas c, int x, int y, int puffs, Color32 a, Color32 b)
    {
        for (int k = 0; k < puffs; k++)
            c.Circle(x + k * 5, y + k * 9, 4 + k * 2, k % 2 == 0 ? a : b);
    }

    // =========================================================================== BEFORE

    // ---- Poster 1: THE UNEMPLOYED GENERATION ---------------------------------------
    private static void D1(PixelCanvas c)
    {
        const int hor = 190;
        c.VGrad(0, W, FootY1, HeadY0, C(150, 138, 108), C(52, 52, 57));
        c.Box(0, FootY1, W, hor - FootY1, GreyD);

        // giant factory, doors shut
        c.Box(16, hor, 224, 70, Grey);
        for (int i = 0; i < 4; i++)
            c.Tri(16 + i * 56, hor + 70, 72 + i * 56, hor + 70, 72 + i * 56, hor + 94, GreyL);
        c.Box(36, hor + 70, 16, 50, GreyD);
        c.Box(33, hor + 114, 22, 8, Black);
        c.Box(196, hor + 70, 16, 40, GreyD);
        c.Box(193, hor + 104, 22, 8, Black);

        for (int i = 0; i < 10; i++)
        {
            int wx = 26 + i * 22;
            c.Box(wx, hor + 54, 12, 10, Black);
            if (wx < 88 || wx > 164) c.Box(wx, hor + 30, 12, 10, Black);
        }

        c.Box(94, hor, 68, 46, Black);
        c.Box(98, hor, 28, 42, GreyD);
        c.Box(130, hor, 28, 42, GreyD);
        c.Line(98, hor + 4, 158, hor + 40, 5, CreamDim);
        c.Line(98, hor + 40, 158, hor + 4, 5, CreamDim);
        c.Circle(128, hor + 22, 6, Cream);

        // the crowd - every row is made of the same figure
        CrowdRow(c, 146, 40, 10, 17, 250, C(34, 34, 38), C(28, 28, 32), C(60, 60, 64), 0f, 0f);
        CrowdRow(c, 120, 52, 20, 22, 250, C(24, 24, 27), C(18, 18, 21), C(48, 48, 52), 0f, 0f);
        CrowdRow(c, 96, 66, 6, 28, 250, C(14, 14, 16), C(10, 10, 12), C(36, 36, 40), 0f, 0f);

        // government figure on a podium, pointing at the crowd
        Color32 suit = C(26, 26, 32);
        c.Box(22, hor, 76, 30, Black);
        c.Box(22, hor + 30, 76, 4, Red);
        Pose g = Human(c, 60, hor + 34, 84, Cream, suit, suit, false);
        c.Line(g.shL, g.shY, g.shR, g.hipY + 6, 5, Red);                      // sash
        c.Box(g.hx - g.hr - 1, g.hy + g.hr / 3, g.hr * 2 + 2, g.hr * 2 / 3 + 2, Black); // officer cap
        c.Box(g.hx - g.hr - 1, g.hy + g.hr / 3, g.hr * 2 + 2, 2, Red);
        c.Box(g.hx, g.hy + g.hr / 3 - 2, g.hr + 5, 3, Black);                 // visor
        Arm(c, g.shR, g.shY, g.shR + 44, g.shY - 40, 6, suit, Cream, false);  // pointing arm
        Arm(c, g.shL - 1, g.shY, g.shL - 3, g.shY - 24, 6, suit, Cream, false);

        // the big red "no jobs" symbol
        int sx = 198, sy = 272;
        c.Circle(sx, sy, 44, Black);
        c.Ring(sx, sy, 42, 8, Red);
        c.Box(sx - 20, sy - 14, 40, 26, Cream);
        c.Box(sx - 20, sy - 2, 40, 3, GreyL);
        c.Box(sx - 8, sy + 12, 3, 9, Cream);
        c.Box(sx + 5, sy + 12, 3, 9, Cream);
        c.Box(sx - 8, sy + 19, 16, 3, Cream);
        c.Line(sx - 30, sy + 30, sx + 30, sy - 30, 9, Red);
    }

    // ---- Poster 2: WORK IS A PRIVILEGE ---------------------------------------------
    private static void D2(PixelCanvas c)
    {
        c.VGrad(0, W, FootY1, HeadY0, C(140, 128, 100), C(48, 48, 53));
        c.Box(0, FootY1, W, 80, GreyD);

        // dead factory with a smokestack that produces no smoke
        c.Box(8, 176, 100, 40, Grey);
        for (int i = 0; i < 5; i++) c.Box(16 + i * 19, 190, 10, 10, Black);
        c.Box(40, 216, 18, 96, GreyD);
        c.Box(40, 260, 18, 6, DarkRed);
        c.Box(37, 308, 24, 8, Black);

        // the businessman's tower
        c.Box(170, 176, 66, 64, Black);
        c.Box(166, 240, 74, 6, DarkRed);
        for (int k = 0; k < 6; k++) c.Circle(180 + k * 10, 184, 4, Cream);
        for (int k = 0; k < 4; k++) c.Circle(186 + k * 10, 194, 4, Cream);

        Color32 suit = C(46, 46, 54);
        c.Box(176, 246, 50, 62, DarkRed);                 // throne back
        c.Box(166, 252, 38, 13, suit);                    // thighs
        c.Box(162, 228, 13, 26, suit);                    // shins
        c.Box(154, 224, 22, 6, Black);                    // shoes
        c.Box(190, 252, 26, 36, suit);                    // torso
        c.Circle(211, 264, 13, suit);                     // belly
        c.Box(199, 285, 8, 4, Red);                       // bow tie
        c.Circle(203, 298, 9, Cream);                     // head
        c.Line(209, 296, 221, 294, 2, CreamDim);          // cigar
        c.Circle(222, 294, 2, Red);
        c.Box(192, 304, 22, 3, Black);                    // top hat
        c.Box(196, 306, 14, 10, Black);

        // the job application, held high and out of reach
        c.Line(192, 284, 146, 300, 7, suit);
        c.Box(126, 296, 24, 20, Cream);
        c.Box(129, 309, 18, 2, DarkRed);
        c.Box(129, 304, 18, 2, GreyL);
        c.Box(129, 299, 12, 2, GreyL);
        c.Circle(146, 300, 5, Cream);

        // workers straining to reach it
        CrowdRow(c, 140, 46, 8, 19, 150, C(40, 40, 44), C(26, 26, 30), C(64, 64, 68), 138f, 300f);
        CrowdRow(c, 96, 62, 14, 26, 160, C(86, 22, 26), C(18, 18, 22), C(60, 60, 64), 138f, 300f);
    }

    // ---- Poster 3: THE LOST FUTURE ---------------------------------------------------
    private static void D3(PixelCanvas c)
    {
        c.VGrad(0, W, FootY1, HeadY0, C(104, 44, 40), C(14, 14, 18));

        // city of abandoned factories
        int[] bx = { 0, 30, 62, 92, 128, 160, 190, 222 };
        int[] bw = { 34, 36, 34, 40, 36, 34, 36, 36 };
        int[] bh = { 62, 96, 70, 112, 78, 100, 66, 90 };
        Color32 far = C(48, 42, 44);
        for (int i = 0; i < bx.Length; i++)
        {
            c.Box(bx[i], 150, bw[i], bh[i], far);
            if (i % 2 == 0) c.Box(bx[i] + 6, 150 + bh[i], 8, 30, far);          // dead smokestacks
            else c.Tri(bx[i], 150 + bh[i], bx[i] + bw[i], 150 + bh[i], bx[i] + bw[i], 150 + bh[i] + 16, far);
            for (int wy = 162; wy < 150 + bh[i] - 6; wy += 14)
                for (int wx = bx[i] + 5; wx < bx[i] + bw[i] - 8; wx += 10)
                    if (((wx * 7 + wy * 3) / 4) % 3 != 0) c.Box(wx, wy, 5, 7, Black);
        }
        // collapsed sheds in front
        c.Box(8, 150, 54, 30, GreyD);
        c.Tri(8, 180, 62, 180, 30, 198, GreyD);
        c.Box(176, 150, 60, 26, GreyD);
        c.Tri(176, 176, 236, 176, 214, 196, GreyD);

        // giant clock - almost midnight
        int cx = 128, cy = 262, r = 48;
        c.Circle(cx, cy, r + 4, Black);
        c.Circle(cx, cy, r, Cream);
        c.Ring(cx, cy, r, 5, DarkRed);
        for (int k = 0; k < 12; k++)
        {
            float a = k * Mathf.PI / 6f;
            c.Line(cx + Mathf.Sin(a) * (r - 8), cy + Mathf.Cos(a) * (r - 8),
                   cx + Mathf.Sin(a) * (r - 16), cy + Mathf.Cos(a) * (r - 16), 3, Black);
        }
        float ma = 342f * Mathf.PI / 180f, ha = 358.5f * Mathf.PI / 180f;
        c.Line(cx, cy, cx + Mathf.Sin(ha) * 26, cy + Mathf.Cos(ha) * 26, 5, Black);
        c.Line(cx, cy, cx + Mathf.Sin(ma) * 38, cy + Mathf.Cos(ma) * 38, 4, Red);
        c.Circle(cx, cy, 5, Black);

        // street, lamp post, bench
        c.Box(0, FootY1, W, 54, Black);
        c.Box(0, 128, W, 22, C(30, 30, 34));
        c.Box(212, 108, 4, 110, GreyD);
        c.Box(204, 214, 20, 6, GreyD);

        Color32 wood = C(140, 126, 92);
        c.Box(66, 108, 6, 18, GreyD);
        c.Box(166, 108, 6, 18, GreyD);
        c.Box(60, 126, 118, 8, wood);
        c.Box(60, 148, 118, 7, wood);
        c.Box(64, 126, 6, 29, GreyD);
        c.Box(168, 126, 6, 29, GreyD);

        // the young man
        Color32 jacket = C(66, 68, 76), pants = C(30, 30, 36), skin = C(196, 160, 124);
        c.Box(112, 134, 46, 12, pants);
        c.Box(148, 110, 12, 26, pants);
        c.Box(148, 106, 20, 6, Black);
        c.Box(104, 146, 24, 34, jacket);
        c.Circle(112, 190, 9, Black);
        c.Circle(115, 188, 7, skin);
        c.Line(126, 176, 146, 160, 6, jacket);
        c.Box(140, 150, 20, 28, Cream);          // the rejected CV
        c.Box(143, 172, 12, 2, GreyL);
        c.Box(143, 167, 12, 2, GreyL);
        c.Line(142, 152, 158, 176, 3, Red);
        c.Line(158, 152, 142, 176, 3, Red);
        c.Circle(144, 162, 4, skin);

        // rain
        for (int i = 0; i < 70; i++)
        {
            int x = (i * 37) % W, y = 100 + (i * 53) % 210;
            c.Line(x, y, x - 3, y - 10, 1f, C(120, 116, 110));
        }
    }

    // =========================================================================== AFTER

    // ---- Poster 1: A FUTURE FOR EVERYONE -------------------------------------------
    private static void U1(PixelCanvas c)
    {
        const int hor = 176;
        c.VGrad(0, W, FootY1, HeadY0, UCream, ULBlue);

        // rising sun
        int sx = 128, sy = hor + 6;
        for (int k = 0; k < 14; k += 2)
        {
            float a = (k + 0.5f) * Mathf.PI / 14f, da = 0.065f;
            c.Tri(sx + Mathf.Cos(a - da) * 46, sy + Mathf.Sin(a - da) * 46,
                  sx + Mathf.Cos(a + da) * 46, sy + Mathf.Sin(a + da) * 46,
                  sx + Mathf.Cos(a) * 230, sy + Mathf.Sin(a) * 230, C(255, 236, 150));
        }
        c.Circle(sx, sy, 46, UYellow);
        c.Circle(sx, sy, 36, C(255, 232, 120));

        c.Box(0, FootY1, W, hor - FootY1, C(236, 208, 140));
        c.Box(0, hor - 4, W, 4, C(214, 184, 110));

        // working factories with smoking chimneys
        int[] fhs = { 58, 76, 64, 84, 60 };
        for (int i = 0; i < 5; i++)
        {
            int x0 = 6 + i * 50, w = 44, h = fhs[i];
            bool red = i % 2 == 0;
            Color32 body = red ? URed : UCream;
            Color32 roof = PixelCanvas.Mix(body, Navy, 0.35f);

            c.Box(x0 - 2, hor - 2, w + 4, h + 4, Navy);
            c.Box(x0, hor, w, h, body);
            c.Tri(x0, hor + h, x0 + 22, hor + h, x0 + 22, hor + h + 14, roof);
            c.Tri(x0 + 22, hor + h, x0 + 44, hor + h, x0 + 44, hor + h + 14, roof);
            for (int r = 0; r < 2; r++)
                for (int j = 0; j < 3; j++)
                    c.Box(x0 + 6 + j * 13, hor + 8 + r * (h / 2 - 4), 8, 10, UYellow);

            c.Box(x0 + w - 14, hor + h, 8, 26, red ? Navy : URed);
            for (int k = 0; k < 3; k++)
                c.Circle(x0 + w - 10 + k * 5, hor + h + 32 + k * 9, 4 + k * 2, k % 2 == 0 ? White : ULBlue2);
        }

        // group lifting a giant gear together
        GearShape(c, 206, 214, 30, 10, UYellow, true);
        c.Circle(206, 214, 11, UCream);
        Color32[] shirts = { URed, UCream, ULBlue };
        Color32[] skins = { C(232, 190, 150), C(170, 115, 80), C(205, 150, 110) };
        int[] gx = { 172, 206, 240 };
        for (int i = 0; i < 3; i++)
        {
            Pose p = Human(c, gx[i], 100, 84, skins[i], shirts[i], Navy, true);
            ArmsToward(c, p, 206f, 190f, 34f, shirts[i], skins[i], true);
            c.Box(p.hx - p.hr - 1, p.hy + p.hr / 3, p.hr * 2 + 2, p.hr * 2 / 3 + 2, UYellow);   // hard hat
        }

        // the strong worker, standing proud
        Color32 skin = C(210, 160, 120);
        Pose w1 = Human(c, 84, 100, 150, skin, UYellow, Navy, true);
        c.Box(w1.hx - w1.hr - 1, w1.hy + w1.hr / 3, w1.hr * 2 + 2, w1.hr * 2 / 3 + 2, URed);   // cap
        c.Box(w1.hx, w1.hy + w1.hr / 3, w1.hr + 5, 3, URed);
        c.Circle(w1.hx, w1.hipY + w1.th * 6 / 10, 6, URed);                                      // emblem
        Arm(c, w1.shL, w1.shY, w1.shL - 16, w1.shY + 44, 9, UYellow, skin, true);               // raised arm...
        c.Line(w1.shL - 16, w1.shY + 44, w1.shL - 16, w1.shY + 80, 5, Navy);                     // ...holding a hammer
        c.Box(w1.shL - 30, w1.shY + 76, 28, 14, Navy);
        c.Box(w1.shL - 28, w1.shY + 78, 24, 10, C(170, 175, 185));
        Arm(c, w1.shR, w1.shY, w1.shR + 16, w1.shY - 20, 9, UYellow, skin, true);               // fist on hip
        Arm(c, w1.shR + 16, w1.shY - 20, w1.shR + 2, w1.shY - 42, 9, UYellow, skin, true);
    }

    // ---- Poster 2: EMPLOYMENT FOR ALL ----------------------------------------------
    private static void U2(PixelCanvas c)
    {
        c.VGrad(0, W, FootY1, HeadY0, UCream, ULBlue);
        c.Circle(36, 288, 20, UYellow);
        c.Ring(36, 288, 28, 3, UYellow);

        // fields and hills
        c.Ellipse(70, 166, 120, 36, C(190, 220, 140));
        c.Ellipse(206, 160, 90, 30, C(170, 210, 130));
        c.Box(0, FootY1, W, 70, C(240, 214, 120));
        for (int i = 0; i < 5; i++) c.Box(0, FootY1 + 4 + i * 14, W, 7, C(250, 224, 100));

        // a big red gear behind everyone
        GearShape(c, 128, 214, 82, 16, URed, true);
        c.Circle(128, 214, 66, PixelCanvas.C(255, 248, 225));

        // woman operating machinery
        c.Box(56, 98, 52, 58, Navy);
        c.Box(59, 101, 46, 52, URed);
        c.Box(64, 118, 22, 18, UCream);
        c.Circle(75, 127, 6, UYellow);
        c.Line(75, 127, 79, 131, 2, Navy);
        c.Box(66, 152, 32, 10, Navy);
        c.Line(98, 128, 104, 150, 4, Navy);
        c.Circle(104, 150, 5, UYellow);

        Color32 skin1 = C(205, 150, 110);
        Pose wm = Human(c, 40, 100, 108, skin1, UCream, Navy, true);
        c.Box(wm.hx - wm.tw / 2 + 2, wm.hipY + 4, wm.tw - 4, wm.th - 4, URed);                    // apron
        c.Circle(wm.hx, wm.hy + wm.hr / 2, wm.hr + 1, C(90, 50, 30));                              // hair
        c.Circle(wm.hx + 1, wm.hy - 1, wm.hr - 1, skin1);
        c.Circle(wm.hx - 3, wm.hy + wm.hr + 2, 5, C(90, 50, 30));                                  // bun
        Arm(c, wm.shR, wm.shY, 98, 146, 7, UCream, skin1, true);
        Arm(c, wm.shL - 1, wm.shY, wm.shL - 3, wm.shY - 28, 7, UCream, skin1, true);

        // engineer with blueprints
        Color32 skin2 = C(170, 115, 80);
        Pose en = Human(c, 124, 100, 116, skin2, C(205, 225, 240), Navy, true);
        c.Box(en.shL + 2, en.hipY, 6, en.th, UYellow);
        c.Box(en.shR - 8, en.hipY, 6, en.th, UYellow);
        c.Box(en.hx - en.hr - 1, en.hy + en.hr / 3, en.hr * 2 + 2, en.hr * 2 / 3 + 2, UYellow);   // hard hat
        c.Box(en.hx - en.hr - 4, en.hy + en.hr / 3 - 2, en.hr * 2 + 8, 3, UYellow);
        Arm(c, en.shL, en.shY, en.hx - 2, en.hipY + 12, 7, C(205, 225, 240), skin2, true);
        Arm(c, en.shR, en.shY, en.hx + 34, en.hipY + 12, 7, C(205, 225, 240), skin2, true);
        c.Box(en.hx - 8, en.hipY + 4, 46, 30, Navy);
        c.Box(en.hx - 6, en.hipY + 6, 42, 26, C(60, 120, 205));
        for (int i = 0; i < 3; i++) c.Box(en.hx - 2, en.hipY + 11 + i * 7, 34, 2, UCream);
        c.Circle(en.hx + 26, en.hipY + 26, 3, UCream);

        // farmer with crops
        Color32 skin3 = C(232, 190, 150);
        Pose fa = Human(c, 180, 100, 112, skin3, UCream, Navy, true);
        c.Ellipse(fa.hx, fa.hy + fa.hr / 2, fa.hr + 11, 4, C(240, 200, 70));                     // straw hat
        c.Box(fa.hx - fa.hr + 1, fa.hy + fa.hr / 2, fa.hr * 2 - 1, fa.hr * 3 / 4, C(240, 200, 70));
        c.Box(fa.hx - fa.hr + 1, fa.hy + fa.hr / 2, fa.hr * 2 - 1, 3, URed);
        int hx = fa.hx + 34, hy = fa.hipY + 22;
        for (int k = 0; k < 5; k++)
        {
            float tx = hx + (k - 2) * 7, ty = hy + 44;
            c.Line(hx, hy, tx, ty, 2, C(220, 170, 40));
            c.Ellipse(Mathf.RoundToInt(tx), Mathf.RoundToInt(ty) + 4, 3, 7, UYellow);
        }
        Arm(c, fa.shR, fa.shY, hx, hy, 7, UCream, skin3, true);
        Arm(c, fa.shL - 1, fa.shY, fa.shL - 3, fa.shY - 28, 7, UCream, skin3, true);

        // factory worker with a wrench
        Color32 skin4 = C(120, 80, 55);
        Pose fw = Human(c, 230, 100, 114, skin4, URed, URed, true);
        c.Box(fw.hx - fw.hr - 1, fw.hy + fw.hr / 3, fw.hr * 2 + 2, fw.hr * 2 / 3 + 2, UYellow);
        c.Box(fw.hx - fw.hr - 4, fw.hy + fw.hr / 3 - 2, fw.hr * 2 + 8, 3, UYellow);
        int wx = fw.shR + 14, wy = fw.shY + 34;
        Arm(c, fw.shR, fw.shY, wx, wy, 7, URed, skin4, true);
        c.Line(wx, wy, wx, wy + 36, 5, Navy);
        c.Circle(wx, wy + 40, 9, Navy);
        c.Circle(wx, wy + 40, 7, C(170, 175, 185));
        c.Box(wx - 2, wy + 40, 4, 8, ULBlue2);
        Arm(c, fw.shL - 1, fw.shY, fw.shL - 3, fw.shY - 28, 7, URed, skin4, true);
    }

    // ---- Poster 3: HISTORY CHANGED. THE FUTURE RESTORED. ------------------------------
    private static void U3(PixelCanvas c)
    {
        // left = the timeline we escaped, right = the one we built
        c.VGrad(0, 128, FootY1, HeadY0, C(92, 46, 44), C(14, 14, 18));
        c.VGrad(128, W, FootY1, HeadY0, UCream, ULBlue);
        c.Box(0, FootY1, 128, 56, Black);
        c.Box(128, FootY1, 128, 56, C(236, 208, 140));
        c.Box(124, FootY1, 8, HeadY0 - FootY1, Navy);

        // dark, abandoned industrial city
        int[] lx = { 2, 30, 60, 90 };
        int[] lw = { 26, 28, 28, 34 };
        int[] lh = { 50, 84, 58, 76 };
        Color32 dark = C(46, 42, 46);
        for (int i = 0; i < lx.Length; i++)
        {
            c.Box(lx[i], 152, lw[i], lh[i], dark);
            if (i % 2 == 0) c.Box(lx[i] + 5, 152 + lh[i], 7, 26, dark);
            else c.Tri(lx[i], 152 + lh[i], lx[i] + lw[i], 152 + lh[i], lx[i], 152 + lh[i] + 14, dark);
            for (int wy = 164; wy < 152 + lh[i] - 6; wy += 13)
                for (int wx = lx[i] + 4; wx < lx[i] + lw[i] - 6; wx += 9)
                    if (((wx * 5 + wy * 3) / 3) % 3 != 0) c.Box(wx, wy, 4, 6, Black);
        }

        // thriving modern city
        int[] rx = { 134, 160, 190, 220 };
        int[] rw = { 24, 28, 26, 30 };
        int[] rh = { 96, 126, 84, 108 };
        Color32[] rc = { UCream, ULBlue2, URed, UCream };
        for (int i = 0; i < rx.Length; i++)
        {
            c.Box(rx[i] - 2, 150, rw[i] + 4, rh[i] + 4, Navy);
            c.Box(rx[i], 152, rw[i], rh[i], rc[i]);
            for (int wy = 160; wy < 152 + rh[i] - 6; wy += 12)
                for (int wx = rx[i] + 4; wx < rx[i] + rw[i] - 6; wx += 9)
                    c.Box(wx, wy, 5, 7, UYellow);
        }
        c.Box(196, 152 + 84, 6, 16, Navy);
        Smoke(c, 199, 152 + 84 + 20, 3, White, ULBlue2);
        c.Line(172, 152 + 126, 172, 152 + 126 + 18, 2, Navy);

        // the timeline splitting in two
        c.Line(128, FootY1 + 4, 128, 176, 13, Navy);
        c.Line(128, FootY1 + 4, 128, 176, 9, UCream);
        for (int k = 0; k < 4; k++) c.Box(120, FootY1 + 12 + k * 17, 16, 3, Navy);
        c.Line(128, 176, 70, 214, 11, Navy);
        c.Line(128, 176, 70, 214, 7, C(130, 128, 128));
        c.Tri(58, 220, 80, 222, 66, 202, Navy);
        c.Line(128, 176, 188, 214, 11, Navy);
        c.Line(128, 176, 188, 214, 7, UYellow);
        c.Tri(200, 220, 178, 222, 192, 202, Navy);
        c.Circle(128, 176, 10, Navy);
        c.Circle(128, 176, 6, URed);

        // giant clock - hands pointing to the bright side
        int cx = 128, cy = 266, r = 46;
        c.Circle(cx, cy, r + 4, Navy);
        c.Circle(cx, cy, r, UCream);
        c.Ring(cx, cy, r, 6, UYellow);
        for (int k = 0; k < 12; k++)
        {
            float a = k * Mathf.PI / 6f;
            c.Line(cx + Mathf.Sin(a) * (r - 9), cy + Mathf.Cos(a) * (r - 9),
                   cx + Mathf.Sin(a) * (r - 16), cy + Mathf.Cos(a) * (r - 16), 3, Navy);
        }
        float ha = 40f * Mathf.PI / 180f, ma = 78f * Mathf.PI / 180f;
        c.Line(cx, cy, cx + Mathf.Sin(ha) * 24, cy + Mathf.Cos(ha) * 24, 5, Navy);
        c.Line(cx, cy, cx + Mathf.Sin(ma) * 36, cy + Mathf.Cos(ma) * 36, 4, URed);
        c.Circle(cx, cy, 5, Navy);
    }
}


public class PartMinimapMarkers
{
   
    private static readonly Color MarkerBrown = new Color(0.55f, 0.30f, 0.10f, 1f);
    private static readonly Color OutlineColor = new Color(1f, 0.93f, 0.75f, 1f);

    private const float DotRadius = 0.08f;       // fraction of the map's half-height
    private const float DotBreathe = 0.35f;
    private const float OutlineWidth = 0.35f;    // fraction of the dot radius
    private const int PingRings = 3;
    private const float PingDuration = 2.4f;     // seconds for one ring to grow from the dot to full size
    private const float PingPause = 0.2f;        // rest between ring cycles (0 = non-stop)
    private const float PingMaxRadius = 1.5f;    // fraction of the map's half-height (1 = touches the edge, 1.5 = sweeps the whole map)
    private const float PingThickness = 0.06f;   // fraction of the map's half-height
    private const float PingPeakAlpha = 1f;
    private const bool PinToCircle = false;     // true if your minimap is round: pins off-map markers to a circle instead of a rectangle

    private const int Segments = 64;

    private class Shape
    {
        public Mesh mesh;
        public Vector3[] verts;
        public Material mat;
    }

    private class Marker
    {
        public GameObject part;
        public Transform root;
        public MeshRenderer[] renderers;   // [0..PingRings-1] = rings, then outline, then dot
        public bool wanted;
    }

    private readonly List<Marker> _markers = new List<Marker>();
    private readonly GameObject _rootGo;
    private readonly Texture2D _whiteTex;
    private readonly Shape[] _shapes = new Shape[PingRings + 2]; // shared by every marker
    private readonly bool[] _on = new bool[PingRings + 2];
    private const int OutlineIdx = PingRings;
    private const int DotIdx = PingRings + 1;

    private Camera _mapCam;
    private float _nextCamSearch;
    private float _noCamTimer;
    private bool _warned;
    private bool _disposed;

    public PartMinimapMarkers(IList<GameObject> parts, Color color)
    {
        // 'color' is ignored: the markers are always brown (see MarkerBrown above).
        _rootGo = new GameObject("PartMinimapMarkers");

        _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _whiteTex.SetPixel(0, 0, Color.white);
        _whiteTex.Apply();

        for (int k = 0; k < PingRings; k++) _shapes[k] = NewShape("PingRing" + k, MarkerBrown);
        _shapes[OutlineIdx] = NewShape("MarkerOutline", OutlineColor);
        _shapes[DotIdx] = NewShape("MarkerDot", MarkerBrown);

        for (int i = 0; i < parts.Count; i++)
        {
            var m = new Marker { part = parts[i] };

            var rootGo = new GameObject("Marker_" + i);
            rootGo.transform.SetParent(_rootGo.transform, false);
            m.root = rootGo.transform;

            m.renderers = new MeshRenderer[PingRings + 2];
            for (int s = 0; s < m.renderers.Length; s++)
            {
                int order = s < PingRings ? 10 : (s == OutlineIdx ? 20 : 21);   // rings < outline < dot
                m.renderers[s] = MakePiece("Piece" + s, m.root, _shapes[s], order);
                m.renderers[s].enabled = false;
            }

            _markers.Add(m);
        }

        // Works in the built-in pipeline AND URP/HDRP: whichever event fires for the active pipeline.
        Camera.onPreCull += OnCameraPreCull;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    // ------------------------------------------------------------------ geometry

    private Shape NewShape(string name, Color col)
    {
        var s = new Shape();
        s.verts = new Vector3[Segments * 2];
        s.mesh = new Mesh { name = name };
        s.mesh.MarkDynamic();

        SetShape(s, 0f, 1f);   // vertices first, then triangles

        int[] tris = new int[Segments * 6];
        for (int i = 0; i < Segments; i++)
        {
            int a = i * 2;                      // inner i
            int b = i * 2 + 1;                  // outer i
            int c = ((i + 1) % Segments) * 2;   // inner i+1
            int d = c + 1;                      // outer i+1
            int t = i * 6;
            tris[t] = a; tris[t + 1] = b; tris[t + 2] = d;
            tris[t + 3] = a; tris[t + 4] = d; tris[t + 5] = c;
        }
        s.mesh.triangles = tris;

        s.mat = HubQuad.MakeMaterial(_whiteTex, col);
        return s;
    }

    private MeshRenderer MakePiece(string name, Transform parent, Shape shape, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        go.AddComponent<MeshFilter>().sharedMesh = shape.mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = shape.mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        mr.sortingOrder = sortingOrder;
        return mr;
    }

    
    private static void SetShape(Shape s, float inner, float outer)
    {
        for (int i = 0; i < Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            s.verts[i * 2] = new Vector3(cs * inner, sn * inner, 0f);
            s.verts[i * 2 + 1] = new Vector3(cs * outer, sn * outer, 0f);
        }
        s.mesh.vertices = s.verts;
        s.mesh.RecalculateBounds();
    }

    // ------------------------------------------------------------------ per-frame

    
    public void Tick(bool missionActive)
    {
        if (_disposed) return;

        bool any = false;
        for (int i = 0; i < _markers.Count; i++)
        {
            var m = _markers[i];
            m.wanted = missionActive && m.part != null && m.part.activeInHierarchy;
            any |= m.wanted;
        }

        if (!any) { _noCamTimer = 0f; return; }

        if (_mapCam == null && Time.unscaledTime >= _nextCamSearch)
        {
            _nextCamSearch = Time.unscaledTime + 1.5f;
            _mapCam = FindMapCamera();
            if (_mapCam != null) AssignLayer(_mapCam);
        }

        if (_mapCam == null)
        {
            _noCamTimer += Time.unscaledDeltaTime;
            if (!_warned && _noCamTimer > 3f)
            {
                _warned = true;
                Debug.LogWarning("[PartMinimapMarkers] No orthographic, top-down minimap camera was found, " +
                                 "so the part markers cannot be shown on the minimap.");
            }
        }
        
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Camera.onPreCull -= OnCameraPreCull;
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        if (_rootGo != null) Object.Destroy(_rootGo);
        foreach (var s in _shapes)
        {
            if (s == null) continue;
            if (s.mat != null) Object.Destroy(s.mat);
            if (s.mesh != null) Object.Destroy(s.mesh);
        }
        if (_whiteTex != null) Object.Destroy(_whiteTex);
    }

    
    private void OnCameraPreCull(Camera cam) { Apply(cam); }
    private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) { Apply(cam); }

    private void Apply(Camera cam)
    {
        if (_disposed) return;

        bool isMap = cam != null && _mapCam != null && cam == _mapCam;
        if (isMap) Reposition();

        for (int i = 0; i < _markers.Count; i++)
        {
            var m = _markers[i];
            for (int r = 0; r < m.renderers.Length; r++)
                if (m.renderers[r] != null) m.renderers[r].enabled = isMap && m.wanted && _on[r];
        }
    }

    
    private void Reposition()
    {
        Transform ct = _mapCam.transform;
        float hh = _mapCam.orthographicSize;
        float hw = hh * _mapCam.aspect;
        float depth = Mathf.Max(_mapCam.nearClipPlane, 0.01f) + 0.5f;
        float now = Time.unscaledTime;

        // --- animation (identical for every marker, so the meshes are shared) ---
        float dotR = DotRadius * hh * (1f + DotBreathe * 0.5f * Mathf.Sin(now * 5f));

        SetShape(_shapes[OutlineIdx], 0f, dotR * (1f + OutlineWidth));
        _shapes[OutlineIdx].mat.color = OutlineColor;
        _on[OutlineIdx] = true;

        SetShape(_shapes[DotIdx], 0f, dotR);
        _shapes[DotIdx].mat.color = MarkerBrown;
        _on[DotIdx] = true;

        float period = Mathf.Max(0.1f, PingDuration + PingPause);
        float duration = Mathf.Max(0.1f, PingDuration);

        for (int k = 0; k < PingRings; k++)
        {
            float phase = Mathf.Repeat(now - k * period / PingRings, period);
            float p = phase / duration;

            if (p > 1f) { _on[k] = false; continue; }
            _on[k] = true;

            float eased = 1f - (1f - p) * (1f - p);                              // fast start, slow end
            float radius = Mathf.Lerp(dotR, PingMaxRadius * hh, eased);
            float alpha = Mathf.Clamp01(p * 8f) * Mathf.Pow(1f - p, 0.8f) * PingPeakAlpha;
            float half = PingThickness * hh * 0.5f;

            SetShape(_shapes[k], Mathf.Max(0f, radius - half), radius + half);
            _shapes[k].mat.color = new Color(MarkerBrown.r, MarkerBrown.g, MarkerBrown.b, alpha);
        }

        // --- placement ---
        float inset = DotRadius * hh * 1.5f;   // keep the dot fully on the map when pinned
        float maxX = Mathf.Max(0f, hw - inset);
        float maxY = Mathf.Max(0f, hh - inset);
        float circleR = Mathf.Max(0f, Mathf.Min(hw, hh) - inset);
        Quaternion rot = Quaternion.LookRotation(ct.forward, ct.up);

        for (int i = 0; i < _markers.Count; i++)
        {
            var m = _markers[i];
            if (!m.wanted || m.part == null) continue;

            Vector3 off = m.part.transform.position - ct.position;
            float x = Vector3.Dot(off, ct.right);
            float y = Vector3.Dot(off, ct.up);

            if (PinToCircle)
            {
                float len = Mathf.Sqrt(x * x + y * y);
                if (len > circleR && len > 0.0001f)
                {
                    float s = circleR / len;
                    x *= s;
                    y *= s;
                }
            }
            else
            {
                x = Mathf.Clamp(x, -maxX, maxX);
                y = Mathf.Clamp(y, -maxY, maxY);
            }

            m.root.position = ct.position + ct.right * x + ct.up * y + ct.forward * depth;
            m.root.rotation = rot;
        }
    }

    private static Camera FindMapCamera()
    {
        Camera main = Camera.main;
        Camera best = null;
        int bestScore = 0;

        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var cam in cams)
        {
            if (cam == null || cam == main) continue;
            if (cam.cameraType != UnityEngine.CameraType.Game) continue;
            if (!cam.orthographic) continue;
            if (cam.transform.forward.y > -0.5f) continue; // must look downwards

            int score = 1 + 2;
            if (cam.targetTexture != null) score += 2;
            if (cam.name.ToLowerInvariant().Contains("map")) score += 3;

            if (score > bestScore)
            {
                bestScore = score;
                best = cam;
            }
        }
        return best;
    }

    // Put the markers on a layer the minimap camera is able to see.
    private void AssignLayer(Camera mapCam)
    {
        int mapMask = mapCam.cullingMask;
        int mainMask = Camera.main != null ? Camera.main.cullingMask : 0;
        int best = -1;

        for (int i = 0; i < 32; i++)
        {
            if ((mapMask & (1 << i)) == 0) continue;
            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName)) continue;

            bool inMain = (mainMask & (1 << i)) != 0;
            if (!inMain && layerName.ToLowerInvariant().Contains("map"))
            {
                best = i; break;

            }
        }
    }
}

    
    public class PartProximityBeeper
    {
        private const float MaxRange = 80f;        // beeping starts inside this distance
        private const float NearDistance = 4f;     // at this distance (or closer) it beeps fastest
        private const float MaxInterval = 1.6f;    // seconds between beeps when far
        private const float MinInterval = 0.09f;   // seconds between beeps when very close
        private const float MinPitch = 0.8f;
        private const float MaxPitch = 1.6f;
        private const float BeepVolume = 0.5f;
        private const float BeepFrequency = 1400f; // Hz
        private const float BeepLength = 0.07f;    // seconds

        private readonly GameObject _go;
        private readonly AudioSource _src;
        private readonly AudioClip _clip;
        private float _nextBeep;

        public PartProximityBeeper()
        {
            _go = new GameObject("PartProximityBeeper");
            _src = _go.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.loop = false;
            _src.spatialBlend = 0f; // 2D - always audible
            _clip = MakeBeepClip();
        }

        
        public void Tick(float distance)
        {
            if (_src == null) return;

            if (float.IsInfinity(distance) || float.IsNaN(distance) || distance > MaxRange)
            {
                _nextBeep = 0f; // beep immediately when we come back into range
                return;
            }

            float closeness = 1f - Mathf.Clamp01((distance - NearDistance) / (MaxRange - NearDistance));
            float interval = Mathf.Lerp(MaxInterval, MinInterval, closeness * closeness);
            float now = Time.time;

            // when approaching a part, speed up straight away instead of waiting out a long gap
            if (_nextBeep > now + interval) _nextBeep = now + interval;
            if (now < _nextBeep) return;

            _nextBeep = now + interval;
            _src.pitch = Mathf.Lerp(MinPitch, MaxPitch, closeness);
            _src.PlayOneShot(_clip, BeepVolume);
        }

        public void Dispose()
        {
            if (_go != null) Object.Destroy(_go);
            if (_clip != null) Object.Destroy(_clip);
        }

        private static AudioClip MakeBeepClip()
        {
            const int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * BeepLength));
            var data = new float[n];

            float attack = rate * 0.004f;
            float release = rate * 0.02f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Min(1f, i / attack) * Mathf.Min(1f, (n - i) / release);
                data[i] = Mathf.Sin(2f * Mathf.PI * BeepFrequency * t) * env * 0.8f;
            }

            var clip = AudioClip.Create("PartBeep", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }


    
    public class AutoBeerAdSystem
    {
        // ---- poster placement ----
        private const int TargetSmallPosters = 26;
        private const int TargetLargePosters = 8;
        private const float SmallW = 2.8f, SmallH = 4.2f;      // metres (poster art is 2:3)
        private const float LargeW = 7.2f, LargeH = 10.8f;
        private const float LargeChance = 0.4f;                 // share of attempts spent on large posters
        private const float LargeMinBottom = 6f;                // large posters start at least this high above street level...
        private const float LargeMaxTop = 52f;                  // ...and end no higher than this
        private const float MinBottomAboveGround = 1.2f;        // small posters
        private const float MaxTopAboveGround = 24f;
        private const int MaxAttempts = 8000;
        private const int AttemptsPerFrame = 60;                // keeps the scan from causing a hitch
        private const int Seed = 1337;                          // change for a different layout
        private const float AreaScale = 1.5f;                   // search area = float volume footprint x this
        private const float MaxRayDistance = 45f;
        private const float SkyCastHeight = 300f;               // ground is found by casting down from this far above the area centre
        private const float SurfaceOffset = 0.06f;              // gap between wall and poster (avoids z-fighting)

        // ---- flyers ----
        private const int FlyerCount = 28;
        private const float FlyerWidth = 0.32f;
        private const float FlyerHeight = 0.48f;
        private const float FlyerSpawnMin = 15f;                // spawn distance range around the player/camera
        private const float FlyerSpawnMax = 60f;
        private const float FlyerDespawnDistance = 110f;
        private const float WindMinSpeed = 1.5f;                // metres per second
        private const float WindMaxSpeed = 6.5f;
        private const float Turbulence = 1.4f;
        private const float BaseSink = 0.9f;                    // how fast paper sinks while flying
        private const float SinkGrowth = 0.04f;                 // sinks faster the longer it has been airborne
        private const float GustLift = 2.0f;
        private const float GustLiftThreshold = 0.7f;           // a strong gust can pick landed flyers up again
        private const float LandedMinTime = 10f;
        private const float LandedMaxTime = 25f;
        private const float MaxFlightTime = 45f;

        private class PosterRecord
        {
            public Renderer r;
            public int index;
        }

        private class Flyer
        {
            public Transform t;
            public MeshRenderer r;
            public int index;
            public Vector3 vel;
            public Vector3 spinAxis;
            public float spinSpeed;
            public float seed;
            public float flightTime;
            public bool placed;
            public bool landed;
            public float landedAt;
            public float leaveAt;
        }

        private MonoBehaviour _host;
        private Vector3 _areaCentre;
        private Vector3 _areaSize;
        private List<Transform> _ignore = new List<Transform>();

        private GameObject _root, _postersRoot, _flyerRoot;
        private readonly Material[] _beforeMats = new Material[3];
        private readonly Material[] _afterMats = new Material[3];
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly List<PosterRecord> _posters = new List<PosterRecord>();
        private readonly List<Vector3> _posterCentres = new List<Vector3>();
        private readonly List<float> _posterRadii = new List<float>();
        private readonly List<Flyer> _flyers = new List<Flyer>();
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private readonly System.Random _rng = new System.Random(Seed);

        
        public bool AutoPlace = true;

        private readonly List<PosterRecord> _manual = new List<PosterRecord>();

        private int _smallCount, _largeCount;
        private bool _utopia;
        private bool _texturesReady;
        private bool _disposed;

        private Camera _cam;
        private float _nextCamSearch;
        private Vector3 _wind = Vector3.forward;
        private float _gust;
        private float _baseWindAngle;

        // ------------------------------------------------------------------ public API
        // Same shape as before: Initialise / SetCompleted / Tick / Dispose.

        public void Initialise(MonoBehaviour host, bool completed, Vector3 areaCentre, Vector3 areaSize,
                               List<Transform> ignoreRoots)
        {
            _host = host;
            _areaCentre = areaCentre;
            _areaSize = new Vector3(Mathf.Max(10f, areaSize.x * AreaScale), areaSize.y, Mathf.Max(10f, areaSize.z * AreaScale));
            _ignore = ignoreRoots ?? new List<Transform>();
            _baseWindAngle = Random.value * 360f;
            _utopia = completed;

            EnsureRoots();
            if (_host != null) _host.StartCoroutine(SetupRoutine());
        }

    
        public void AddManualPoster(Renderer quad, int posterIndex)
        {
            if (quad == null) return;
            int idx = posterIndex >= 0 ? Mathf.Clamp(posterIndex, 0, 2) : _manual.Count % 3;
            _manual.Add(new PosterRecord { r = quad, index = idx });

            Bounds b = quad.bounds;
            _posterCentres.Add(b.center);
            _posterRadii.Add(Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)));

            ApplySet();
        }

        
        public void SetCompleted(bool completed)
        {
            if (_disposed) return;
            _utopia = completed;
            ApplySet();
        }

        
        public void Tick()
        {
            if (_disposed || !_texturesReady || _root == null) return;
            UpdateFlyers();
        }

        public void Dispose()
        {
            _disposed = true;
            if (_root != null) Object.Destroy(_root);
            foreach (var m in _beforeMats) if (m != null) Object.Destroy(m);
            foreach (var m in _afterMats) if (m != null) Object.Destroy(m);
            foreach (var t in _textures) if (t != null) Object.Destroy(t);
            _textures.Clear();
            _posters.Clear();
            _manual.Clear();
            _flyers.Clear();
        }

        // ------------------------------------------------------------------ setup

        private void EnsureRoots()
        {
            if (_root != null) return;
            _root = new GameObject("PropagandaPosters_Root");
            _postersRoot = new GameObject("Posters");
            _flyerRoot = new GameObject("Flyers");
            _postersRoot.transform.SetParent(_root.transform, false);
            _flyerRoot.transform.SetParent(_root.transform, false);
        }

        private IEnumerator SetupRoutine()
        {
            // 1. paint the six posters, one per frame so there is no big hitch
            for (int set = 0; set < 2; set++)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (_disposed) yield break;

                    Texture2D tex = PosterArt.Make(set == 1, i);
                    _textures.Add(tex);
                    Material mat = HubQuad.MakeMaterial(tex, Color.white);
                    if (set == 0) _beforeMats[i] = mat; else _afterMats[i] = mat;
                    yield return null;
                }
            }

            _texturesReady = true;
            EnsureFlyers();
            ApplySet();

            // 2. find walls and stick posters on them, a few attempts per frame
            int attempts = 0;
            while (AutoPlace && !_disposed && attempts < MaxAttempts &&
                   (_smallCount < TargetSmallPosters || _largeCount < TargetLargePosters))
            {
                for (int i = 0; i < AttemptsPerFrame && attempts < MaxAttempts; i++)
                {
                    bool wantLarge = _largeCount < TargetLargePosters;
                    bool wantSmall = _smallCount < TargetSmallPosters;
                    if (!wantLarge && !wantSmall) break;

                    attempts++;
                    bool large = wantLarge && (!wantSmall || Rand(0f, 1f) < LargeChance);
                    TryPlaceOnePoster(large);
                }
                yield return null;
            }

            if (AutoPlace && !_disposed && _posters.Count == 0)
            {
                Debug.LogWarning("[AutoBeerAdSystem] Could not find any wall to place posters on. " +
                                 "Make sure the buildings have colliders and sit inside the hub float volume " +
                                 "(the cyan box, scaled up by AreaScale).");
            }
            else if (AutoPlace && !_disposed && _largeCount == 0)
            {
                Debug.Log("[AutoBeerAdSystem] Placed " + _smallCount + " small posters but no large ones - " +
                          "no building wall was flat and tall enough (needs ~7 m x 11 m, starting 6 m above the street).");
            }
        }

        private void ApplySet()
        {
            if (!_texturesReady) return;
            Material[] mats = _utopia ? _afterMats : _beforeMats;

            foreach (var p in _posters)
                if (p.r != null) p.r.sharedMaterial = mats[p.index];

            foreach (var p in _manual)
                if (p.r != null) p.r.sharedMaterial = mats[p.index];

            foreach (var f in _flyers)
                if (f.r != null) f.r.sharedMaterial = mats[f.index];
        }

        // ------------------------------------------------------------------ poster placement

        private float Rand(float min, float max)
        {
            return min + (float)_rng.NextDouble() * (max - min);
        }

        private bool TryPlaceOnePoster(bool large)
        {
            float w = large ? LargeW : SmallW;
            float h = large ? LargeH : SmallH;

            // 1. random street-level point inside the area
            Vector3 half = _areaSize * 0.5f;
            float x = _areaCentre.x + Rand(-half.x, half.x);
            float z = _areaCentre.z + Rand(-half.z, half.z);

            if (!TryFindGround(new Vector3(x, _areaCentre.y + SkyCastHeight, z), out float groundY)) return false;

            Vector3 origin = new Vector3(x, groundY + Rand(1.8f, 14f), z);
            if (Physics.CheckSphere(origin, 0.6f, ~0, QueryTriggerInteraction.Ignore)) return false;

            // 2. shoot a ray sideways and see what wall we hit
            float yaw = Rand(0f, 360f);
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            if (!Physics.Raycast(origin, dir, out RaycastHit hit, MaxRayDistance, ~0, QueryTriggerInteraction.Ignore))
                return false;
            if (!IsSolidStatic(hit.collider)) return false;

            Vector3 n = hit.normal;
            if (Mathf.Abs(n.y) > 0.2f) return false;              // not a vertical wall
            if (Vector3.Dot(n, dir) > -0.3f) return false;         // not facing us
            n.y = 0f;
            if (n.sqrMagnitude < 0.001f) return false;
            n.Normalize();

            // 3. street level in front of this wall
            if (!TryFindGround(new Vector3(hit.point.x + n.x * 0.6f, _areaCentre.y + SkyCastHeight, hit.point.z + n.z * 0.6f),
                               out float wallGround))
                return false;

            // 4. choose the height: large posters go HIGH, small ones stay near street level
            float minY, maxY;
            if (large)
            {
                minY = wallGround + LargeMinBottom + h * 0.5f;
                maxY = wallGround + LargeMaxTop - h * 0.5f;
            }
            else
            {
                minY = wallGround + MinBottomAboveGround + h * 0.5f;
                maxY = wallGround + MaxTopAboveGround - h * 0.5f;
            }
            if (maxY < minY) return false;

            float cy = large ? Rand(minY, maxY) : Mathf.Clamp(hit.point.y, minY, maxY);
            Vector3 centre = new Vector3(hit.point.x, cy, hit.point.z);

            // 5. the wall must be flat and unobstructed across the WHOLE poster
            //    (this is what limits large posters to genuinely big, tall buildings)
            Quaternion rot = Quaternion.LookRotation(-n, Vector3.up);
            Vector3 halfRight = (rot * Vector3.right) * (w * 0.5f);
            Vector3 halfUp = Vector3.up * (h * 0.5f);
            if (!WallIsFlat(centre, n, hit.collider, halfRight, halfUp, large ? 5 : 3, large ? 0.25f : 0.2f)) return false;

            // 6. don't crowd other posters
            float radius = Mathf.Max(w, h) * 0.5f;
            for (int i = 0; i < _posterCentres.Count; i++)
            {
                float need = radius + _posterRadii[i] + 1f;
                if ((_posterCentres[i] - centre).sqrMagnitude < need * need) return false;
            }

            // 7. place it
            int index = large ? _largeCount % 3 : _rng.Next(3);
            Material[] mats = _utopia ? _afterMats : _beforeMats;

            var go = HubQuad.Create(large ? "PosterLarge" : "PosterSmall", _postersRoot.transform, mats[index]);
            go.transform.SetPositionAndRotation(centre + n * SurfaceOffset, rot);
            go.transform.localScale = new Vector3(w, h, 1f);

            _posters.Add(new PosterRecord { r = go.GetComponent<MeshRenderer>(), index = index });
            _posterCentres.Add(centre);
            _posterRadii.Add(radius);
            if (large) _largeCount++; else _smallCount++;
            return true;
        }

        
        private bool WallIsFlat(Vector3 centre, Vector3 n, Collider col, Vector3 halfRight, Vector3 halfUp,
                                int grid, float tolerance)
        {
            for (int i = 0; i < grid; i++)
            {
                float u = grid > 1 ? (i / (float)(grid - 1)) * 2f - 1f : 0f;
                for (int j = 0; j < grid; j++)
                {
                    float v = grid > 1 ? (j / (float)(grid - 1)) * 2f - 1f : 0f;
                    Vector3 pt = centre + halfRight * u + halfUp * v;
                    Vector3 o = pt + n * 0.6f;

                    if (!Physics.Raycast(o, -n, out RaycastHit h, 1.2f, ~0, QueryTriggerInteraction.Ignore)) return false;
                    if (h.collider != col) return false;
                    if (Mathf.Abs(h.distance - 0.6f) > tolerance) return false;
                }
            }
            return true;
        }

        
        private bool TryFindGround(Vector3 from, out float groundY)
        {
            int n = Physics.RaycastNonAlloc(from, Vector3.down, _hits, SkyCastHeight * 2f + 200f, ~0,
                                            QueryTriggerInteraction.Ignore);
            bool found = false;
            groundY = float.MaxValue;

            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _hits[i];
                if (h.normal.y < 0.7f) continue;
                if (!IsSolidStatic(h.collider)) continue;
                if (h.point.y < groundY)
                {
                    groundY = h.point.y;
                    found = true;
                }
            }
            return found;
        }

        
        private bool IsSolidStatic(Collider c)
        {
            if (c == null || c.isTrigger) return false;
            if (c is CharacterController) return false;
            if (c.CompareTag("Player")) return false;

            Rigidbody rb = c.attachedRigidbody;
            if (rb != null && !rb.isKinematic) return false;

            Transform ct = c.transform;
            for (int i = 0; i < _ignore.Count; i++)
            {
                Transform root = _ignore[i];
                if (root != null && ct.IsChildOf(root)) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ flyers

        private void EnsureFlyers()
        {
            if (_flyers.Count > 0) return;

            Material[] mats = _utopia ? _afterMats : _beforeMats;
            for (int i = 0; i < FlyerCount; i++)
            {
                int index = i % 3;
                var go = HubQuad.Create("Flyer", _flyerRoot.transform, mats[index]);
                go.transform.localScale = new Vector3(FlyerWidth, FlyerHeight, 1f);
                go.SetActive(false);

                _flyers.Add(new Flyer
                {
                    t = go.transform,
                    r = go.GetComponent<MeshRenderer>(),
                    index = index,
                    seed = Random.value * 100f
                });
            }
        }

        private bool TryGetViewer(out Vector3 pos)
        {
            if (_cam == null && Time.unscaledTime >= _nextCamSearch)
            {
                _nextCamSearch = Time.unscaledTime + 1f;
                _cam = Camera.main;
            }

            pos = _cam != null ? _cam.transform.position : Vector3.zero;
            return _cam != null;
        }

        private bool IsOnScreen(Vector3 p)
        {
            if (_cam == null) return false;
            Vector3 vp = _cam.WorldToViewportPoint(p);
            return vp.z > 0f && vp.x > -0.05f && vp.x < 1.05f && vp.y > -0.05f && vp.y < 1.05f;
        }

        private void UpdateWind()
        {
            float t = Time.time;
            float angle = _baseWindAngle + (Mathf.PerlinNoise(t * 0.03f, 4.2f) - 0.5f) * 140f;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            _gust = Mathf.PerlinNoise(t * 0.12f, 9.7f);
            float speed = Mathf.Lerp(WindMinSpeed, WindMaxSpeed, Mathf.SmoothStep(0f, 1f, _gust));
            _wind = dir * speed;
        }

        private void UpdateFlyers()
        {
            if (_flyers.Count == 0) return;
            if (!TryGetViewer(out Vector3 viewer)) return;

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;

            UpdateWind();

            float time = Time.time;
            float despawnSqr = FlyerDespawnDistance * FlyerDespawnDistance;
            int spawnBudget = 4; // don't spawn everything on one frame

            for (int i = 0; i < _flyers.Count; i++)
            {
                Flyer f = _flyers[i];

                if (!f.placed)
                {
                    if (spawnBudget-- > 0) Respawn(f, viewer);
                    continue;
                }

                Vector3 pos = f.t.position;
                bool far = (pos - viewer).sqrMagnitude > despawnSqr;

                if (f.landed)
                {
                    if (far) { Respawn(f, viewer); continue; }

                    // recycle after a while, but only when the player isn't looking at it
                    if (time >= f.leaveAt && (!IsOnScreen(pos) || time - f.landedAt > 90f))
                    {
                        Respawn(f, viewer);
                        continue;
                    }

                    // a strong gust can blow a landed flyer away again
                    if (_gust > GustLiftThreshold && Random.value < dt * 0.25f) LiftOff(f);
                    continue;
                }

                if (far || f.flightTime > MaxFlightTime)
                {
                    Respawn(f, viewer);
                    continue;
                }

                StepFlying(f, dt, time);
            }
        }

        private void StepFlying(Flyer f, float dt, float time)
        {
            float s = f.seed;
            Vector3 turb = new Vector3(
                Mathf.PerlinNoise(s, time * 0.7f) - 0.5f,
                Mathf.PerlinNoise(s + 17f, time * 0.9f) - 0.5f,
                Mathf.PerlinNoise(s + 31f, time * 0.7f) - 0.5f) * (2f * Turbulence);

            float sink = BaseSink + f.flightTime * SinkGrowth;
            Vector3 targetVel = _wind + turb + Vector3.down * sink;
            targetVel.y += _gust * GustLift;

            f.vel = Vector3.Lerp(f.vel, targetVel, 1f - Mathf.Exp(-2f * dt));

            Vector3 from = f.t.position;
            Vector3 to = from + f.vel * dt;

            if (SegmentHit(from, to, out RaycastHit hit))
            {
                if (hit.normal.y > 0.5f)
                {
                    Land(f, hit);
                    return;
                }

                // wall / ceiling: bounce off it and keep sliding along it
                f.t.position = hit.point + hit.normal * 0.15f;
                f.vel = Vector3.Reflect(f.vel, hit.normal) * 0.3f + hit.normal * 0.5f;
            }
            else
            {
                f.t.position = to;
            }

            f.t.rotation = Quaternion.AngleAxis(f.spinSpeed * dt, f.spinAxis) * f.t.rotation;
            f.flightTime += dt;
        }

        private bool SegmentHit(Vector3 from, Vector3 to, out RaycastHit best)
        {
            best = default(RaycastHit);
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.0001f) return false;

            int n = Physics.RaycastNonAlloc(from, d / len, _hits, len + 0.08f, ~0, QueryTriggerInteraction.Ignore);

            bool found = false;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _hits[i];
                if (h.distance <= 0.0001f || h.distance >= bestDist) continue;
                if (!IsSolidStatic(h.collider)) continue;

                bestDist = h.distance;
                best = h;
                found = true;
            }
            return found;
        }

        private void Land(Flyer f, RaycastHit hit)
        {
            f.landed = true;
            f.landedAt = Time.time;
            f.leaveAt = Time.time + Random.Range(LandedMinTime, LandedMaxTime);
            f.vel = Vector3.zero;

            // lie flat on the surface with a random twist
            f.t.position = hit.point + hit.normal * 0.03f;
            f.t.rotation = Quaternion.AngleAxis(Random.value * 360f, hit.normal) *
                           Quaternion.FromToRotation(Vector3.back, hit.normal);
        }

        private void LiftOff(Flyer f)
        {
            f.landed = false;
            f.flightTime = 0f;
            f.t.position += Vector3.up * 0.15f;
            f.vel = _wind * 0.5f + Vector3.up * 2f;
            f.spinAxis = Random.onUnitSphere;
            f.spinSpeed = Random.Range(120f, 420f);
        }

        private void Respawn(Flyer f, Vector3 viewer)
        {
            Vector3 windDir = _wind.sqrMagnitude > 0.001f ? _wind.normalized : Vector3.forward;
            Vector3 best = viewer + Vector3.up * 10f;

            // try to pick a spot the player can't see, so flyers drift in rather than pop in
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 rnd = Random.insideUnitCircle.normalized;
                Vector3 offs = (-windDir * 0.7f + new Vector3(rnd.x, 0f, rnd.y)).normalized *
                               Random.Range(FlyerSpawnMin, FlyerSpawnMax);
                Vector3 xz = viewer + offs;

                if (!TryFindGround(new Vector3(xz.x, viewer.y + 150f, xz.z), out float groundY))
                    groundY = viewer.y - 1.5f;

                Vector3 p = new Vector3(xz.x, groundY + Random.Range(4f, 16f), xz.z);
                if (Physics.CheckSphere(p, 0.4f, ~0, QueryTriggerInteraction.Ignore)) p.y += 30f; // inside a building -> start above it

                best = p;
                if (!IsOnScreen(p)) break;
            }

            f.placed = true;
            f.landed = false;
            f.flightTime = 0f;
            f.t.gameObject.SetActive(true);
            f.t.position = best;
            f.t.rotation = Random.rotation;
            f.vel = _wind;
            f.spinAxis = Random.onUnitSphere;
            f.spinSpeed = Random.Range(120f, 420f);
        }
    }
