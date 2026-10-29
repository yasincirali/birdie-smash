// Ekran dışı top işareti (2026-10-27, yasin build 41 "Top yükseldiğinde kadrajdan çıkıyor"; karar Core/Kadraj.cs başında).
// Yüksek top takibinin tavanına rağmen skor kutusunun üstünde kalan top ve hareketi azalt (takip yok) için: skor kutusunun
// hemen altında, topun x'inde yukarı bakan ok. Topun gölgesi kortta zaten düşeceği yeri gösterir (ShuttleView); ok yalnız
// "top yukarıda, şurada" der. Ayrı bileşen (Hud'a değil): Hud'un yerleşimi başka dalda değişiyor, birleştirme kolay kalsın.
// Dokunulmaz (girdi almaz), yanıp sönmez (hareketi azaltta da aynı), beyaz ok koyu kenarlı: kortun her renginde okunur,
// anlam renkle değil biçimle (ok). Alfa sınıra yaklaşırken artar (Kadraj.Isaret), kesik görünmez.
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class KadrajIsaret : MonoBehaviour
    {
        public GameRoot root; Texture2D ok;
        public float SonAlfa, SonX;   // tanı (EkranTuru): son karede işaret ne kadar görünür ve nerede

        static Texture2D OkTex()
        {   // 64 px üçgen ok: beyaz iç, koyu 5 px kenar (yumuşak kenar: alfa kenar uzaklığıyla)
            const int N = 64; var t = new Texture2D(N, N, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[N * N]; Vector2 a = new Vector2(N / 2f, N - 4), b = new Vector2(4, 8), c = new Vector2(N - 4, 8);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                var p = new Vector2(x + .5f, y + .5f);
                float d = Mathf.Min(Kenar(p, a, b), Mathf.Min(Kenar(p, b, c), Kenar(p, c, a)));   // içeride +
                float al = Mathf.Clamp01(d + .5f); Color k = d > 5 ? Color.white : new Color(.03f, .06f, .07f);
                px[y * N + x] = new Color(k.r, k.g, k.b, al * (d > 5 ? 1 : .85f));
            }
            t.SetPixels(px); t.Apply(); return t;
        }
        static float Kenar(Vector2 p, Vector2 a, Vector2 b) { var v = b - a; var n = new Vector2(-v.y, v.x).normalized; return Vector2.Dot(p - a, n); }

        // GUILayout kullanılmıyor: Layout olayı (OnGUI'nin her karedeki ikinci çağrısı) atlanır (Hud ile aynı; tur 2 inceleme).
        void Awake() { useGUILayout = false; }
        void OnGUI()
        {
            GUI.depth = 1;   // Hud'un (perde, duraklat) arkasında
            if (Event.current.type != EventType.Repaint) return;
            SonAlfa = 0; if (root == null || root.match == null || root.ekran != Ekran.Oyun || root.paused) return;
            float sx, al; if (!Kadraj.Isaret(root.kam, root.kadrajEgim, root.match.s, out sx, out al)) return;
            if (ok == null) ok = OkTex();
            float u = Mathf.Min(Screen.height / 640f, Screen.width / 340f), w = 18 * u, h = 15 * u, y = root.kam.ustPx + 4 * u;
            var o = GUI.color; GUI.color = new Color(1, 1, 1, .9f * al); GUI.DrawTexture(new Rect(sx - w / 2, y, w, h), ok); GUI.color = o;
            SonAlfa = al; SonX = sx;
        }
    }
}
