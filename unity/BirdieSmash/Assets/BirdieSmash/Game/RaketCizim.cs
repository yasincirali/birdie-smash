// Koddan çizilen raket (2026-11-26, yasin: "alt bar, Atölye ve diğer her yerde raket görseli iyileşmeli, daha gerçekçi olmalı";
// önce/sonra https://claude.ai/artifact/HBxHjzHmrkHgXzQ8P65tiN). Eski çizimlerde kafa raket boyunun yarısıydı: tenis raketi ya da
// büyüteç gibi okunuyordu. Gerçek badminton raketi ~675 mm: sap (dip kapağıyla) ~200 mm, şaft ~7 mm kalınlıkta ve uzun, kafa
// ~235 × 200 mm, yani boyun ancak %35'i. Bütün oranlar buradan, raket boyu 1 birim:
//   · sap u 0,015–0,285 (yarı genişlik ,0205 ≈ 28 mm sargılı), dip kapağı u 0–0,015, sargı ekleri eğik bant
//   · şaft u 0,30'dan kafanın altına (yarı genişlik ,0058 → ,0047, yukarı incelir), sapın üstünde kısa koni
//   · T bağlantısı kafanın alt kenarında kısa takoz; kafa RaketSekil.Egri ile (oval / izometrik / kalp, oyundaki 3B raketin aynısı)
//   · 22 ana + 21 çapraz tel (insan.raket ile aynı sayı), çerçeve borusu yarı kalınlık ,0085
// Kenar yumuşatma tek örnekle, işaretli uzaklıktan (RaketSekil.Ikon'un yolu): 256² vitrinde 4 örnekli eski çizimden ucuz.
// Bu dosyada Texture/UI yok (yalnız Mathf, Color, Vector2), böylece tool/unity_test/RaketCizimOlc.cs mono ile PNG'ye döker.
using System.Collections.Generic;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class RaketCizim
    {
        // ---- Alt bar sekme simgesi (96 px çizgi simge; Ikonlar.Ciz'in yüklemi, W = sekme çizgi kalınlığı) ----
        // 40° eğik (dik raket büyüteç gibi okunuyordu, 2026-11-23 önizlemesi), kafa sol üstte. Kafa izometrik: köşeli omuzlar
        // badmintonu tenisten ayırır. Kafa çizgisi sekme çizgisinden %25 ince (küçük kafada kalın halka dolu leke gibi duruyordu),
        // 2 ana + 2 çapraz tel, ince şaft, kalın sap ve dip kapağı. Süperelips örtük denklemi: 36k pikselde eğri taraması yerine.
        public static bool SekmeIc(float x, float y, float W)
        {
            const float AN = -.70f; float c = Mathf.Cos(AN), s = Mathf.Sin(AN);
            float u = x * s + y * c, v = x * c - y * s;   // u boyuna (kafa sol üstte), v enine
            float uu = (u - .62f) / .36f, vv = v / (.30f * 1.04f);
            float k = Mathf.SmoothStep(0, 1, (Mathf.Clamp(uu, -1, 1) + 1) / 2), nn = Mathf.Lerp(2, 3.4f, k);
            float r = Mathf.Pow(Mathf.Pow(Mathf.Abs(uu), nn) + Mathf.Pow(Mathf.Abs(vv), nn), 1 / nn);
            float yari = W * .75f, d = (r - 1) * .32f;   // .32 ≈ kafanın yarı ekseni: örtük farkı birime çevirir
            if (Mathf.Abs(d) < yari) return true;
            if (d < -yari) return Mathf.Abs(Mathf.Abs(v) - .11f) < .022f || Mathf.Abs(u - .52f) < .022f || Mathf.Abs(u - .74f) < .022f;
            return Boy(u, v, .20f, .30f) < W * .85f            // T bağlantısı
                || Boy(u, v, -.46f, .22f) < W * .6f            // şaft
                || Boy(u, v, -.98f, -.50f) < W * 1.2f          // sap
                || Hip(u + 1.07f, Mathf.Max(0, Mathf.Abs(v) - .075f)) < W * .55f;   // dip kapağı
        }
        // u ekseni boyunca [a, b] parçasına uzaklık (yuvarlak uçlu çizgi).
        static float Boy(float u, float v, float a, float b) { return Hip(Mathf.Max(0, Mathf.Max(a - u, u - b)), v); }
        static float Hip(float a, float b) { return Mathf.Sqrt(a * a + b * b); }

        // ---- Atölye vitrini: oyuncunun raketi kendi renkleriyle ----
        // Kart üstünde 40° eğik, doku zaten eğik üretilir (USS rotate yok: eğik kare doku köşeleri kırpıyordu). Renkler maçtaki 3B
        // raketle aynı kaynaktan (PlayerView.Benim): çerçeve, ana/çapraz tel, şaft, grip. Şaft "model" (null) = grafit gri.
        // Çerçevenin ortasında açık bir şerit (borunun ışığı): siyah çerçeve mavi kartta da okunur.
        const float L = 2.30f, ROT = .70f, OX = -.10f, OY = .07f;
        const float KB = .355f, KG = .300f, FT = .0085f, SW = .0016f;
        public static readonly Color SAFT_VARSAYILAN = new Color(.62f, .64f, .68f);
        public static Color32[] Vitrin(int n, string kafa, Color cer, Color tel, Color tel2, Color saft, Color grip)
        {
            var px = new Color32[n * n];
            float a = KB / 2 - .006f, b = KG / 2 - .006f, uc = 1 - KB / 2, ub = uc - a, p = 2f / n / L;
            var egri = RaketSekil.Egri(kafa, a, b, 120); int m = egri.Count;
            var ex = new float[m]; var ey = new float[m];
            for (int i = 0; i < m; i++) { ex[i] = egri[i].x + uc; ey[i] = egri[i].y; }
            float sp = 2 * b * .94f / 22, sc = 2 * a * .94f / 21, c = Mathf.Cos(ROT), s = Mathf.Sin(ROT);
            Color acik = Color.Lerp(cer, Color.white, .45f), sarg = grip * .6f; sarg.a = 1;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float X = (x + .5f) / n * 2 - 1 + OX, Y = (y + .5f) / n * 2 - 1 + OY;
                    float u = (Y * c - X * s + L / 2) / L, v = (X * c + Y * s) / L;
                    if (u < -.01f || u > 1.01f || Mathf.Abs(v) > b + .03f) continue;
                    float R = 0, G = 0, B = 0, A = 0;
                    if (u > ub - .03f)
                    {
                        float sd = UzaklikIsaretli(u, v, ex, ey);
                        float ic = Kapsa(sd + FT, p);
                        if (ic > 0)
                        {
                            float dm = Mathf.Abs(Mathf.Repeat(v + sp / 2, sp) - sp / 2) - SW, dc = Mathf.Abs(Mathf.Repeat(u - uc + sc / 2, sc) - sc / 2) - SW;
                            Ust(ref R, ref G, ref B, ref A, tel, Kapsa(dm, p) * ic * .9f); Ust(ref R, ref G, ref B, ref A, tel2, Kapsa(dc, p) * ic * .9f);
                        }
                        Ust(ref R, ref G, ref B, ref A, cer, Kapsa(Mathf.Abs(sd) - FT, p));
                        Ust(ref R, ref G, ref B, ref A, acik, Kapsa(Mathf.Abs(sd) - FT * .45f, p) * .55f);
                        // T bağlantısı: kafanın alt kenarına doğru genişleyen kısa takoz
                        float t0 = ub - .03f, tj = Mathf.Max(Mathf.Abs(v) - (.0075f + (u - t0) * .12f), Mathf.Max(t0 - u, u - (ub + .01f)));
                        Ust(ref R, ref G, ref B, ref A, cer, Kapsa(tj, p));
                    }
                    // şaft (sapın üstünde kısa koniyle) ve T'ye kadar
                    float sy = u < .30f ? .0058f + (.30f - u) / .015f * .013f : .0058f - (u - .30f) * .004f;
                    Ust(ref R, ref G, ref B, ref A, saft, Kapsa(Mathf.Max(Mathf.Abs(v) - sy, Mathf.Max(.285f - u, u - (ub - .025f))), p));
                    float gd = Mathf.Max(Mathf.Abs(v) - .0205f, Mathf.Max(.015f - u, u - .285f)), gk = Kapsa(gd, p);
                    if (gk > 0)
                    {
                        Ust(ref R, ref G, ref B, ref A, grip, gk);
                        Ust(ref R, ref G, ref B, ref A, Color.black, Kapsa(.008f - v, p) * gk * .35f);   // sapın gölgede kalan yanı
                        float t = Mathf.Repeat((u + v * 1.2f) * 1000, 22);
                        Ust(ref R, ref G, ref B, ref A, sarg, Kapsa((Mathf.Abs(t - 1.6f) - 1.6f) / 1000, p) * gk * .7f);   // sargı ekleri
                    }
                    Ust(ref R, ref G, ref B, ref A, cer, Kapsa(Mathf.Max(Mathf.Abs(v) - .0235f, Mathf.Max(-u, u - .015f)), p));   // dip kapağı
                    if (A <= 0) continue;
                    px[y * n + x] = new Color32((byte)(Mathf.Clamp01(R / A) * 255), (byte)(Mathf.Clamp01(G / A) * 255), (byte)(Mathf.Clamp01(B / A) * 255), (byte)(Mathf.Clamp01(A) * 255));
                }
            return px;
        }
        // İşaretli uzaklık: d < 0 sınırın içinde, birim = 1 piksel genişliğinde yumuşak kenar.
        static float Kapsa(float d, float p) { return Mathf.Clamp01(.5f - d / p); }
        // Önceden çarpılmış renk üstüne bindirme (arkadaki katman önce).
        static void Ust(ref float R, ref float G, ref float B, ref float A, Color k, float al)
        {
            if (al <= 0) return; R = R * (1 - al) + k.r * al; G = G * (1 - al) + k.g * al; B = B * (1 - al) + k.b * al; A = A * (1 - al) + al;
        }
        // Kafa eğrisine işaretli uzaklık (içeride eksi); içerisi ışın sayımıyla.
        static float UzaklikIsaretli(float u, float v, float[] ex, float[] ey)
        {
            int m = ex.Length; float en = 9; bool ic = false;
            for (int i = 0, j = m - 1; i < m; j = i++)
            {
                float ax = ex[j], ay = ey[j], bx = ex[i], by = ey[i], vx = bx - ax, vy = by - ay, wx = u - ax, wy = v - ay;
                float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy)), dx = wx - vx * t, dy = wy - vy * t, d = dx * dx + dy * dy;
                if (d < en) en = d;
                if ((ay > v) != (by > v) && u < (bx - ax) * (v - ay) / (by - ay) + ax) ic = !ic;
            }
            en = Mathf.Sqrt(en); return ic ? -en : en;
        }
    }
}
