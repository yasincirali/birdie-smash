// Ekran üstü: skor, servis/ipucu, uçan yazılar (dünya noktasına bağlı), mod ve yeniden başlat.
// IMGUI (OnGUI): paket gerektirmez; demo için yeterli, son ürün UI Toolkit'e taşınır.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Hud : MonoBehaviour
    {
        public GameRoot root; GUIStyle big, mid, small, btn; float u;
        class Float { public string t; public Vector3 w; public float a; public Color c; public float size; }
        readonly List<Float> floats = new List<Float>();

        public void Say(string t, Vector3 world, Color c, float size = 1) { floats.Add(new Float { t = t, w = world, c = c, size = size }); if (floats.Count > 8) floats.RemoveAt(0); }

        void Update() { for (int i = floats.Count - 1; i >= 0; i--) { floats[i].a += Time.unscaledDeltaTime; if (floats[i].a > 1.2f) floats.RemoveAt(i); } }

        void Styles()
        {
            u = Screen.height / 640f; if (big != null && Mathf.Abs(big.fontSize - Mathf.RoundToInt(34 * u)) < 1) return;
            big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(34 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            mid = new GUIStyle(big) { fontSize = Mathf.RoundToInt(20 * u) }; small = new GUIStyle(big) { fontSize = Mathf.RoundToInt(13 * u), fontStyle = FontStyle.Normal, wordWrap = true };
            btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(14 * u) };
        }

        static void Shadowed(Rect r, string t, GUIStyle s, Color c)
        {
            var o = GUI.color; GUI.color = new Color(0, 0, 0, c.a * .6f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, s);
            GUI.color = c; GUI.Label(r, t, s); GUI.color = o;
        }

        void OnGUI()
        {
            var M = root.match; if (M == null) return; Styles(); float W = Screen.width, H = Screen.height;
            Rect safe = Screen.safeArea; float top = H - safe.yMax + 8 * u;
            Shadowed(new Rect(0, top, W, 44 * u), M.sc[0] + "  –  " + M.sc[1], big, Color.white);
            Shadowed(new Rect(0, top + 40 * u, W, 20 * u), "SEN" + new string(' ', 14) + "RAKİP", small, new Color(1, 1, 1, .7f));
            if (!string.IsNullOrEmpty(M.serveMsg) && M.s == null) Shadowed(new Rect(W * .08f, top + 62 * u, W * .84f, 40 * u), M.serveMsg, small, Kit.Hex("#d8ff5a"));
            if (M.s == null && M.server == "me" && !M.between && !M.over) Shadowed(new Rect(0, H * .80f, W, 30 * u), "Servis: yukarı kaydır (hızlı = uzun)", mid, Color.white);
            if (M.HitWindow()) Shadowed(new Rect(0, H * .80f, W, 30 * u), "Şimdi kaydır!", mid, Kit.Hex("#d8ff5a"));
            foreach (var f in floats)
            {
                var sp = root.cam.WorldToScreenPoint(f.w + Vector3.up * (.6f + f.a * .5f)); if (sp.z < 0) continue;
                var c = f.c; c.a = Mathf.Clamp01(1.3f - f.a); var st = new GUIStyle(mid) { fontSize = Mathf.RoundToInt(20 * u * f.size) };
                Shadowed(new Rect(sp.x - W / 2, H - sp.y - 20 * u, W, 40 * u), f.t, st, c);
            }
            float bw = 96 * u, bh = 44 * u,   // 44/640 ekran yüksekliği: iPhone SE (667 pt) dahil ≥44 pt dokunma alanı
            by = safe.yMin > 0 ? H - safe.yMin - bh - 8 * u : H - bh - 10 * u;
            if (GUI.Button(new Rect(10 * u, by, bw, bh), M.easy ? "Kolay" : "Usta", btn)) root.NewMatch(!M.easy);
            if (GUI.Button(new Rect(W - bw - 10 * u, by, bw, bh), "Yeniden", btn)) root.NewMatch(M.easy);
            if (M.over)
            {
                GUI.color = new Color(0, 0, 0, .55f); GUI.DrawTexture(new Rect(0, H * .35f, W, H * .22f), Texture2D.whiteTexture); GUI.color = Color.white;
                Shadowed(new Rect(0, H * .37f, W, 50 * u), M.sc[0] > M.sc[1] ? "Kazandın" : "Bu sefer rakip aldı", big, Color.white);
                Shadowed(new Rect(0, H * .45f, W, 30 * u), "En uzun ralli " + M.maxRally + " · Rakip " + M.stil.ad, small, Color.white);
                if (GUI.Button(new Rect(W / 2 - bw, H * .50f, bw * 2, bh), "Yeni maç", btn)) root.NewMatch(M.easy);
            }
        }
    }
}
