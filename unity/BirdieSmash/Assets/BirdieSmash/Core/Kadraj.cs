// Kadraj (2026-10-27 öğleden sonra, yasin build 41: "Top yükseldiğinde kadrajdan çıkıyor"). Kamera çözümü GameRoot.FitCamera'dan
// buraya, saf C#'a taşındı: aynı çözüm hem oyunda hem BotMac'te (mono, Unity'siz) koşsun, top ekrandan kaç kare çıkıyor
// sayıyla ölçülsün. GameRoot yalnız sonucu kameraya yazar. Çözüm birebir eskisi (FitCamera'nın 26–27 Eki karar kaydı orada).
// Dünya: Kit.W(x, y, z) = (x − MID, z, NET − y); kamera (0, h, −L/2 − D), yalnız eğim (x ekseni), Unity sol el kuralı.
//
// Yüksek top (ölçüm BotMac Kadrajlar(): 8 bot maçı, 60 Hz, 7 ekran × 3 açı). Kök neden: 27 Eki kadrajı üst sınırı (skor
// kutusunun altı) "seyirci başları / rakibin sıçrama tepesi"ne (2,6 m) oturtur, topun uçuşu hiç hesaba katılmaz. Yayın'da üst
// sınır rakip dip çizgisinin üstünde ~1–2 m yükseklikten geçer: clear (tepe 6–9 m), yüksek servis ve lift tepede ekran dışı.
// Ölçülen (önce, takipsiz): iPhone 390×844 Yayın'da uçuş karelerinin %16'sı skor kutusunun arkasında, %10,6'sı ekran dışı,
// tepesi 5 m'yi aşan 688 uçuştan 306'sının tepesi hiç görünmüyor; SE'de 523/688, Alçak'ta daha kötü.
// Üst sınırı 9 m'ye sabit açmak kortu ~%40 küçültür ve onaylanan "üstte boş bant yok" hedefini bozar, o yüzden:
//  1) Yumuşak takip (Takip + Genis): top üst sınırı aşacaksa kamera yukarı bakar (eğim azalır) ve görüş açısı yakın dip
//     çizginin 0,8 m gerisini (geri kaçan oyuncunun ayakları) smaç çubuğunda tutacak kadar açılır; sonra geri döner. Konum
//     sabit: kort yerinde kalır, yalnız "kamera hafifçe geri çekilir" gibi okunur. Ticari örnekler (Badminton League, Tennis
//     Clash) kortu sabit tutup topun gölgesi + hafif kamera takibiyle çözer. Yalnız eğim yakın yarıyı keserdi (9°'de Yayın
//     19,5:9'da yakın dip ~170 px aşağı, ekran dışına); yalnız uzaklaşma tepede kortu %40 küçültürdü.
//     Durumsuz: eğim kare deltalarından değil, uçuşun tahmin edilen yolundan (Shuttle.landPred.path, canlı uçuşla aynı adım)
//     hesaplanır: c(t) = max_τ min(tavan, r(τ))·K(t − τ); r(τ) = topu τ anında sınırın altına almak için gereken eğim, K bir
//     tümsek (öne ONCE sn, geriye SONRA sn, smoothstep). Kamera topun yükseleceğini önceden bilir, yavaşça kalkar, tepede
//     durur, inişte yavaşça döner; aynı vuruş her seferinde aynı hareketi verir. Vuruş anındaki eğim (devir) yeni uçuşun
//     zarfına GECIS sn'de geçer, uçuş bitince SONRA sn'de söner: kamera hiç sıçramaz. Mide bulandırmasın diye: görüş
//     açısı en çok ×GENIS_AZAMI (eğim tavanı buradan, ~6–8°), açısal hız ölçülen en çok ~25°/s (BotMac ≤ HIZ_AZAMI).
//  2) Ekran dışı işareti (Isaret, Game/KadrajIsaret): tavana rağmen skor kutusunun üstünde kalan top ve hareketi azalt
//     (takip yok) için skor kutusunun altında, topun x'inde yukarı ok; topun gölgesi zaten kortta (ShuttleView).
// Sonra (takipli, iPhone 390×844 Yayın): skor kutusu arkası %1,9, ekran dışı %0,7, tepesi görünmeyen 26/688 (hepsinde işaret);
// oyuncunun ayakları hiçbir karede smaç çubuğunun altına inmez. Kalan: SE Alçak 139/688, iPad (geniş ekran, kamera alçak
// ve yatık: 7,5 m, 32°) Yayın 238/688; orada işaret taşır.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public static class Kadraj
    {
        // KAMERA_SIRA (GameRoot) sırasıyla: Yayın, Alçak, Üstten. Değerlerin gerekçesi GameRoot.FitCamera üstünde.
        public static readonly float[] D = { 7, 5, 3 };              // dipten geri (m)
        public static readonly float[] GENIS = { 1, 1.1f, .85f };    // tekler(+15 cm) genişliğinin ekrana oranı
        public static readonly float[] EGIM_AZAMI = { 47, 48, 64 };  // eğim tavanı (derece)

        public struct Kamera
        {
            public float h, p, tv, k, asp, W, H, nu, na;   // yükseklik, eğim (°), dikey yarım açı tanjantı, eksen dışı kayma (NDC), en/boy, ekran px, sınırlar (NDC)
            public float ustPx, altPx;                     // ekran pikseli, yukarıdan: skor kutusunun altı, smaç çubuğunun üstü
            public float aTan, bTan, egimAzami;            // takip: yakın noktanın ve smaç çubuğunun dikey tanjantı (eğim 0), eğim tavanı (°)
            public int ki;
            public float Z { get { return -Court.L / 2 - D[ki]; } }   // kameranın dünya z'si
        }

        // Ekran (px) ve güvenli alan (Unity Rect: x, y aşağıdan, genişlik, yükseklik) için çözüm.
        public static Kamera Coz(float W, float H, float saX, float saY, float saW, float saH, int ki)
        {
            var c = new Kamera(); c.ki = ki; W = Math.Max(1, W); H = Math.Max(1, H); c.W = W; c.H = H;
            float kD = D[ki], kF = GENIS[ki], pAz = EGIM_AZAMI[ki];
            float u = Math.Min(H / 640f, W / 340f); c.asp = W / H;
            float ust = (H - (saY + saH)) + 46 * u, alt = H - saY - 36 * u;
            c.ustPx = ust; c.altPx = alt; c.nu = 1 - 2 * ust / H; c.na = 1 - 2 * alt / H;
            float h, p = pAz, s = Pay(c, kD, kF, pAz, out h);
            if (float.IsNaN(s) || s < 0)
            {
                float lo = 10, hi = pAz;
                for (int i = 0; i < 30; i++) { p = (lo + hi) / 2; s = Pay(c, kD, kF, p, out h); if (float.IsNaN(s) || s > 0) lo = p; else hi = p; }
                p = hi; s = Pay(c, kD, kF, p, out h);
            }
            float tvW, tU, tY;
            if (float.IsNaN(h)) { float en = -1e9f; for (float hh = 2; hh <= 45; hh += .5f) { float g = UstFark(c, kD, kF, hh, p); if (g > en) { en = g; h = hh; } } }
            Olc(c, kD, kF, h, p, out tvW, out tU, out tY);
            c.h = h; c.p = p; c.tv = tvW; c.k = float.IsNaN(s) ? c.nu - tU / tvW : 0;   // son çare dışında kaydırma yok
            c.aTan = tY; c.bTan = (c.na - c.k) * tvW;
            { float lo = 0, hi = 40; for (int i = 0; i < 30; i++) { float m = (lo + hi) / 2; if (Genis(c, m) > GENIS_AZAMI) hi = m; else lo = m; } c.egimAzami = Math.Min(AZAMI, lo); }
            return c;
        }

        // Dünyadan kamera uzayına (Unity: Quaternion.Inverse(Euler(p,0,0)) * (w − pos)).
        static void Yerel(float h, float zc, float p, float wx, float wy, float wz, out float lx, out float ly, out float lz)
        {
            double r = p * Math.PI / 180; float cs = (float)Math.Cos(r), sn = (float)Math.Sin(r);
            float dy = wy - h, dz = wz - zc; lx = wx; ly = dy * cs + dz * sn; lz = -dy * sn + dz * cs;
        }
        static float Tan(float h, float zc, float p, float x, float y, float z)
        {
            float wx = x - Court.MID, wy = z, wz = Court.NET - y, lx, ly, lz; Yerel(h, zc, p, wx, wy, wz, out lx, out ly, out lz); return ly / Math.Max(.01f, lz);
        }
        // Konum (0, h, dipten kD geri), eğim p için: tekler genişliğinin gerektirdiği yarım açı tanjantı (tvW) ve üst/alt noktaların
        // dikey tanjantları. Üst nokta: seyirci başları ya da rakibin sıçrama tepesi; alt: yakın dip çizginin 0,8 m gerisi.
        static void Olc(Kamera c, float kD, float kF, float h, float p, out float tvW, out float tU, out float tY)
        {
            float zc = -Court.L / 2 - kD, th = 0;
            foreach (float x in new[] { Court.SL - .15f, Court.SR + .15f }) foreach (float y in new[] { 0f, Court.L })
                { float lx, ly, lz; Yerel(h, zc, p, x - Court.MID, 0, Court.NET - y, out lx, out ly, out lz); th = Math.Max(th, Math.Abs(lx / Math.Max(.01f, lz))); }
            tvW = th / c.asp / kF;
            tU = Math.Max(Tan(h, zc, p, Court.MID, -2.3f, 1.8f), Tan(h, zc, p, Court.MID, .4f, 2.6f));
            tY = Tan(h, zc, p, Court.MID, Court.L + .8f, 0);
        }
        // + = üst nokta sınırın üstünde
        static float UstFark(Kamera c, float kD, float kF, float h, float p) { float tvW, tU, tY; Olc(c, kD, kF, h, p, out tvW, out tU, out tY); return tU / tvW - c.nu; }
        // Eğim p'de üst noktayı tam sınıra oturtan en yüksek konum (yükseldikçe kort küçülür, fark azalır; tepeden sonra tekdüze),
        // sonra yakın noktanın alt sınıra payı (NDC). Üst nokta hiçbir yükseklikte sınıra ulaşmıyorsa NaN: eğim artmalı.
        static float Pay(Kamera c, float kD, float kF, float p, out float h)
        {
            h = float.NaN; float a = 45;
            while (a > 2 && UstFark(c, kD, kF, a, p) < 0) a -= .5f;
            if (a <= 2) return float.NaN;
            float b = a + .5f; for (int i = 0; i < 24; i++) { float m = (a + b) / 2; if (UstFark(c, kD, kF, m, p) >= 0) a = m; else b = m; }
            h = a; float tvW, tU, tY; Olc(c, kD, kF, h, p, out tvW, out tU, out tY); return tY / tvW - c.na;
        }

        // Kort noktasının (x, y, z = yükseklik) ekran konumu: px, yukarıdan (sy). egim = takip eğimi (°; + = yukarı bakış, p − egim).
        // Kameranın arkasındaysa false.
        public static bool Izdus(Kamera c, float egim, float x, float y, float z, out float sx, out float sy)
        {
            float lx, ly, lz; Yerel(c.h, c.Z, c.p - egim, x - Court.MID, z, Court.NET - y, out lx, out ly, out lz);
            sx = sy = 0; if (lz < .05f) return false;
            float tv = c.tv * Genis(c, egim), nx = lx / lz / (tv * c.asp), ny = ly / lz / tv + c.k;
            sx = (nx + 1) / 2 * c.W; sy = (1 - ny) / 2 * c.H; return true;
        }
        // Takip eğimi e (°) için görüş açısı çarpanı (tanjant; ≥ 1). Kamera yukarı bakınca yakın nokta (dip çizginin 0,8 m gerisi,
        // geri kaçan oyuncunun ayakları) aşağı kayar: önce smaç çubuğuna kadar olan boş payı kullanır (yalnız eğim), sonra
        // görüş açısı o noktayı çubukta tutacak kadar açılır (hafif uzaklaşma: kort yakın dip çizgiye göre küçülür, oyuncu
        // ekrandan çıkmaz). Yalnız eğim yakın yarıyı keserdi (9°'de Yayın 19,5:9'da yakın dip ~170 px, ekranın dışına).
        public static float Genis(Kamera c, float egim)
        {
            double ba = Math.Atan(c.aTan), bb = Math.Atan(c.bTan), pay = Math.Max(0, ba - bb), e = egim * Math.PI / 180;
            if (e <= pay) return 1;
            return (float)(Math.Tan(ba - e) / Math.Tan(bb));
        }
        // Noktanın üst sınırı (skor kutusunun altı, pay ile) aşmaması için gereken eğim (°, ≥ 0, tavansız). Eğim yalnız yz düzleminde
        // döndürür (açı atan2(ly, lz) tam eğim kadar kayar), görüş açısı Genis ile açılır; ikisi birlikte tekdüze: ikiye bölme.
        public static float Gereken(Kamera c, float x, float y, float z, float payPx)
        {
            float lx, ly, lz; Yerel(c.h, c.Z, c.p, x - Court.MID, z, Court.NET - y, out lx, out ly, out lz);
            if (lz < .05f) return 0;
            double a = Math.Atan2(ly, lz), hedef = c.nu - 2 * payPx / c.H - c.k;
            if (Math.Tan(a) / c.tv <= hedef) return 0;
            float lo = 0, hi = 60;
            for (int i = 0; i < 26; i++) { float m = (lo + hi) / 2; if (Math.Tan(a - m * Math.PI / 180) / (c.tv * Genis(c, m)) > hedef) lo = m; else hi = m; }
            return hi;
        }

        // ---- yumuşak takip eğimi ----
        public const float AZAMI = 20;         // derece: eğim tavanı (asıl sınır GENIS_AZAMI)
        public const float GENIS_AZAMI = 1.35f; // görüş açısı tanjantı en çok %35 açılır: kort en çok ~%26 küçülür (yakın dip çizgiye göre)
        public const float ONCE = .6f, SONRA = .8f;   // tümsek: kamera ihtiyaçtan ÖNCE sn önce kalkmaya başlar, SONRA sn'de döner
        public const float GECIS = .6f;               // yeni uçuşa geçiş: vuruş anındaki eğimden uçuşun eğimine (sn)
        public const float PAY_PX = 10;        // referans 640 px'te üst sınırın altında bırakılan pay (top gövdesi ~6 px)
        public const float HIZ_AZAMI = 30;     // °/s, ölçüm sınırı (BotMac): bundan hızlı eğim baş döndürür

        static float Yumusak(float u) { u = Math.Max(0, Math.Min(1, u)); return u * u * (3 - 2 * u); }
        // Tümsek: d = t − τ (tepe anına göre). d < 0: öncesi, d > 0: sonrası.
        public static float Tumsek(float d) { return d < 0 ? Yumusak(1 + d / ONCE) : Yumusak(1 - d / SONRA); }

        public class Takip
        {
            object ucus; bool bitti; float[] gt, gr; int gn;   // uçuş başına: yol örneklerinin zamanı ve gereken eğim
            float devirC, devirT0 = -99;   // devir: uçuş değişince ya da bitince o anki eğim, o andan SONRA sn'de söner
            public float Son;              // son hesaplanan eğim (yalnız devir değeri; eğimin kendisi uçuştan hesaplanır)

            void Devir(float saat) { devirC = Son; devirT0 = saat; }
            // s: uçan top (null = top yok), saat: Match.clock. Yol yere kadar kullanılır (karşılayanın temas anı oyuncuya bağlı;
            // temastan sonraki tahmini yol zaten alçalıyor, eğimi büyütmez).
            public float Egim(Kamera c, Shuttle s, float saat, bool azalt)
            {
                if (azalt) { Son = 0; devirC = 0; ucus = null; return 0; }
                object u = s != null ? (object)s.landPred : null;   // uçuş kimliği: tahmin edilen yol (her vuruşta bir kez kurulur)
                if (!ReferenceEquals(u, ucus))
                {
                    Devir(saat); ucus = u; bitti = false; gn = 0;
                    if (s != null && s.landPred != null && s.landPred.path != null)
                    {
                        float payPx = PAY_PX * c.H / 640f; var P = s.landPred.path; int n = P.Count + 1;
                        if (gt == null || gt.Length < n) { gt = new float[n]; gr = new float[n]; }
                        gt[0] = s.t; gr[0] = Math.Min(c.egimAzami, Gereken(c, s.x, s.y, s.z, payPx)); gn = 1;
                        foreach (var q in P) { if (q.t <= s.t) continue; gt[gn] = q.t; gr[gn] = Math.Min(c.egimAzami, Gereken(c, q.x, q.y, q.z, payPx)); gn++; }
                    }
                }
                // Fileye değen, ağa gömülen ya da yere düşen topta yol tahmini geçersiz: o andan itibaren yalnız devir sönümü.
                bool gecerli = s != null && gn > 0 && !s.dead && !s.agda && !s.netHit && !s.netCord;
                if (!gecerli && !bitti && s != null) { bitti = true; Devir(saat); }
                // Uçuş sürerken: vuruş anındaki eğimden (devir) uçuşun zarfına GECIS sn'de yumuşak geçiş (vuruş yüksekte, ör. rakibin
                // dip çizgide 3 m'de smacı: zarf ilk karede büyük olabilir, kamera yine sıçramaz). Uçuş yoksa ya da bittiyse devir söner.
                float e;
                if (gecerli)
                {
                    float t = s.t, z = 0;
                    for (int i = 0; i < gn; i++) { float d = t - gt[i]; if (d < -ONCE || d > SONRA || gr[i] <= z) continue; z = Math.Max(z, gr[i] * Tumsek(d)); }
                    float g = Yumusak((saat - devirT0) / GECIS); e = devirC + (z - devirC) * g;
                }
                else e = devirC * Tumsek(saat - devirT0);
                e = Math.Min(c.egimAzami, e); Son = e; return e;
            }
        }

        // Ekran dışı işareti: top üst sınırın (skor kutusunun altı) üstündeyse ya da ekranın üstünden çıktıysa, işaretin x'i (kenara kıstırılır).
        // alfa = 0..1 (sınıra yaklaşırken belirir, kesik değil).
        public static bool Isaret(Kamera c, float egim, Shuttle s, out float sx, out float alfa)
        {
            sx = 0; alfa = 0; if (s == null || s.dead) return false;
            float sy; if (!Izdus(c, egim, s.x, s.y, s.z, out sx, out sy)) return false;
            float sinir = c.ustPx;   // skor kutusunun altı
            alfa = Math.Max(0, Math.Min(1, (sinir + 12 * c.H / 640f - sy) / (24 * c.H / 640f)));
            sx = Math.Max(16 * c.H / 640f, Math.Min(c.W - 16 * c.H / 640f, sx));
            return alfa > 0;
        }
    }
}
