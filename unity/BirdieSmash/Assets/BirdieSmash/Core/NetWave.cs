// File dokusu topun hızına göre tepki verir (yasin 2026-10-19: "süper gerçekçi olmasına gerek
// yok ama düşünüldüğü belli olmalı"). Her sütun 1B sönümlü dalga: smaç fileyi derin esnetir,
// dalga direklere doğru yayılır; drop yalnız hafif kıpırdatır. 2026-10-27: sabitler HTML'den ayrıldı (HTML donuk;
// Unity ürünün kendisi), file_temas_parite.js ağ sütunlarında artık birebir tutmaz.
//
// 2026-10-27 (yasin "file de standartlarda olmalı bant kısmı daha sert ama file hareketli olacak"), ölçüm
// tool/unity_test/BotMac.cs FileStandart(): önceki hâlde (1) ağ hit'i bandı hiç oynatmıyordu ama bandın hemen altındaki
// ağ satırı 3,6 cm oynuyordu (ağ bandın altından dışarı taşıyor görünüyordu), (2) dalga 1,1 m öteye ancak 0,65 sn'de ve
// tepenin %6'sıyla varıyordu (yayılma görünmüyordu), (3) bant 2,8 Hz'te salınıyordu (sert değil), (4) Hareketi azalt
// genliği hiç düşürmüyor, yalnız sönümü artırıyordu (dinamik değişiyordu). Şimdi:
//  - ağ: dikey zarf bandın alt kenarında 0 (ağ banda dikili), alt kenara doğru 1; dalga ~4 m/s yayılır, ~2,5 sn'de durulur;
//  - bant: kendi sert yayı (~7 Hz, ζ≈0,4, ~0,3 sn'de söner), birim metre; ağ onu küçük bir yay katsayısıyla çeker (ağ
//    smacında bant birkaç mm oynar, ağ bandı hiç çekmez değil); ağın en üst görünen satırı bandın sapmasını taşır (yırtılma yok);
//  - Hareketi azalt yalnız GÖRÜNEN genliği AZALT katına indirir: a/v/ta/tv aynı (dinamik, zamanlama, top kuralı aynı);
//  - uyku: her şey eşiğin altına inince diziler sıfırlanır ve Advance hiç çalışmaz; lo..hi yalnız hareketli sütunları
//    verir ki görünüm yalnız onların köşelerini yenilesin. Ayırma (allocation) yok.
using System;

namespace BirdieSmash.Core
{
    public class NetWave
    {
        public const int N = 33;
        public readonly float[] a = new float[N], v = new float[N];
        // Bant ayrı bir yay (2026-10-26, yasin "file kısmı ve bant kısmı farklı tepki vermeli"): üstte gergin kordonlu 75 mm
        // beyaz bant (BWF Law 1.5) ağ gibi esnemez; çarpılan yerde küçük, sert ve hızlı titrer, kordon boyunca çabuk yayılır.
        // 2026-10-27: ta birimi METRE (görünen sapma, GORSEL'siz); eskiden görünüm ta×0,05 + tape×0,02 ile ayrıca ölçekliyordu.
        // `tape` bandın ortalama sapması olarak kalır (RalliKayit).
        public readonly float[] ta = new float[N], tv = new float[N];
        public float z = Court.NETH * .5f, tape, glow, gx = Court.MID, gz = Court.NETH * .5f;
        public bool active;
        // Hareketli sütun aralığı (dahil; lo > hi = hareketsiz). Görünüm yalnız bu aralığın köşelerini yeniler.
        public int lo = N, hi = -1;
        // İşaretli sapma ve görünüm kazancı (2026-10-26, yasin "file kritik, bant daha sert kısım, file ise oynak"): yon = topun
        // gittiği yön (+1 = +y). GORSEL: ağ dalgası a'yı görünen metreye çevirir (HTML 1,8 ile aynı); Zorla() topun gördüğü
        // sapmayı bu kazançla yazar ki top ağın şişkinliğinin tam üstünde dursun.
        public const float GORSEL = 1.8f;
        // Hareketi azalt (Ayar.reduce / HTML prefers-reduced-motion): 2026-10-27'den beri yalnız görünen genlik ×AZALT.
        public bool sakin; public const float AZALT = .35f;
        public float Olcek { get { return sakin ? AZALT : 1f; } }

        // Ağ dalgası (sütun aralığı 6,1/32 = 0,19 m): C2 sütun²/s² (c = √C2 × 0,19 ≈ 4 m/s), K geri getirme (ağın dikey gerilimi),
        // SONUM. Bant: daha hızlı kordon dalgası, sert yay (ω0 = √TK ≈ 44 rad/s ≈ 7 Hz), güçlü sönüm (ζ = TSONUM / 2ω0 ≈ 0,4),
        // KAVRAMA: ağın sapması bandı çeker (dengede bant ≈ KAVRAMA/TK × ağ ≈ %2).
        public const float C2 = 450, K = 30, SONUM = 3.8f, TC2 = 1600, TK = 1900, TSONUM = 34, KAVRAMA = 40;
        // Dikey Gauss genişliği (m) ve uyku eşikleri (görünen ~0,5 mm).
        const float W = .3f, EPS_A = 2.5e-4f, EPS_V = 2.5e-3f, EPS_T = 2e-4f, EPS_TV = 2e-3f;

        public void Impulse(float x, float zz, float sp, bool tapeOnly, float yon = 1)
        {
            int i = Kolon(x); gx = x; gz = zz;
            if (tapeOnly)
            {
                // Bant: çarpma hızı → kordona dik hız (m/s); 20 m/s topta ~6 mm, yavaş topta 2 mm.
                float ku = Math.Min(1f, sp / 30f);
                for (int d = -2; d <= 2; d++) { int j = i + d; if (j <= 0 || j >= N - 1) continue; tv[j] += (.12f + .9f * ku) * (float)Math.Exp(-d * d / .8f) * yon; }
                glow = Math.Max(glow, .5f * ku); gz = Court.NetLimit(x); Uyan(); return;
            }
            float k = Math.Min(1f, sp / 55f);   // smaç ~50 m/s → tam, drop ~6 m/s → %10
            z = Math.Max(.15f, Math.Min(Court.NETH - .1f, zz)); glow = Math.Max(glow, k);
            for (int d = -2; d <= 2; d++) { int j = i + d; if (j <= 0 || j >= N - 1) continue; v[j] += (.3f + 2.6f * k) * (float)Math.Exp(-d * d / 1.6f) * yon; }
            Uyan();
        }

        // Top ağa gömülürken ağı ona göre biçimlendirir (2026-10-26): çarpma sütunu ve iki komşusu, görünen sapma (×GORSEL)
        // topun ittiği kadar olacak şekilde yazılır, hızları da değişimden; top bırakınca dalga oradan serbestçe sürer.
        // disp: görünen sapma (m, işaretli +y); zz: topun yüksekliği (şişkinliğin tepesi topla birlikte aşağı kayar).
        // 2026-10-27: zarf bandın altında sıfıra indiği için şişkinliğin tepesi (topun ittiği kadar) topun hizasında değil
        // sütunun en çok esneyen yerindedir: bölen profilin sütundaki tepesi. Bandın dibine çarpan top ağı az içeri iter (ağ
        // orada banda dikili), şişkinlik aşağıda kalır ve topun ittiğinden büyük olmaz (eskiden bölen ≥ 0,15 → 6× şişkinlik).
        public void Zorla(float x, float zz, float disp, float h)
        {
            z = Math.Max(.15f, Math.Min(Court.NETH - .05f, zz)); gx = x; gz = z;
            float prof = ProfTepe(x);
            float A = disp / (GORSEL * prof); int i = Kolon(x);
            for (int d = -2; d <= 2; d++)
            {
                int j = i + d; if (j <= 0 || j >= N - 1) continue;
                float hedef = A * (float)Math.Exp(-d * d / 1.6f); v[j] = h > 0 ? (hedef - a[j]) / h : 0; a[j] = hedef;
            }
            Uyan();
        }

        void Uyan() { active = true; if (lo > hi) { lo = 1; hi = N - 2; } }

        public void Step(float dt)
        {
            int l = N, r = -1;
            for (int i = 1; i < N - 1; i++) { float lap = a[i - 1] - 2 * a[i] + a[i + 1]; v[i] += (C2 * lap - K * a[i] - SONUM * v[i]) * dt; }
            for (int i = 1; i < N - 1; i++)
            {
                float lap = ta[i - 1] - 2 * ta[i] + ta[i + 1];
                tv[i] += (TC2 * lap - TK * ta[i] + KAVRAMA * a[i] * GORSEL - TSONUM * tv[i]) * dt;
            }
            float sum = 0;
            for (int i = 1; i < N - 1; i++)
            {
                a[i] += v[i] * dt; ta[i] += tv[i] * dt; sum += ta[i];
                if (Math.Abs(a[i]) > EPS_A || Math.Abs(v[i]) > EPS_V || Math.Abs(ta[i]) > EPS_T || Math.Abs(tv[i]) > EPS_TV) { if (i < l) l = i; r = i; }
            }
            tape = sum / (N - 2); glow = Math.Max(0, glow - dt * 1.6f);
            if (r < 0)
            {   // uyku: kalıntıyı sıfırla ki dinlenen file tam düz (ve belirlenimci) olsun
                Array.Clear(a, 0, N); Array.Clear(v, 0, N); Array.Clear(ta, 0, N); Array.Clear(tv, 0, N); tape = 0; active = false; lo = N; hi = -1; return;
            }
            lo = l; hi = r; active = true;
        }

        public void Advance(float dt) { if (!active && glow <= 0) return; float r = dt; while (r > 0) { float h = Math.Min(1f / 240f, r); if (active) Step(h); else glow = Math.Max(0, glow - h * 1.6f); r -= h; } }

        public static int Kolon(float x) { return Math.Max(0, Math.Min(N - 1, (int)Math.Round(x / Court.CW * (N - 1)))); }

        // Bandın x'teki GÖRÜNEN sapması (m, Hareketi azalt dahil): direklerde sıfır.
        public float BantAt(float x) { return TapeAt(x) * Olcek; }
        // Bandın dinamik sapması (m, ölçeksiz).
        public float TapeAt(float x)
        {
            float f = x / Court.CW * (N - 1); int i = (int)Math.Floor(f); float t = f - i;
            return ta[Math.Max(0, Math.Min(N - 1, i))] * (1 - t) + ta[Math.Max(0, Math.Min(N - 1, i + 1))] * t;
        }

        // Ağın dikey profili: Gauss (son çarpma yüksekliği z çevresinde) × zarf. Zarf bandın alt kenarında 0 (ağ banda dikili),
        // alt kenarda 1 (serbest kenar, eğimi sıfır): u(2 − u), u = bant altından aşağı 0..1.
        public float Prof(float x, float zz)
        {
            float alt = Court.NetLimit(x) - Court.BANT, u = (alt - zz) / (Court.AG_DERIN - Court.BANT);
            if (u <= 0) return 0; if (u > 1) u = 1;
            return (float)Math.Exp(-((zz - z) * (zz - z)) / (2 * W * W)) * u * (2 - u);
        }

        // Sütunun en çok esneyen yerindeki profil (z şimdiki çarpma yüksekliği); 12 örnek, yalnız ağa gömülü topta çağrılır.
        public float ProfTepe(float x)
        {
            float alt = Court.NetLimit(x) - Court.BANT, m = .05f;
            for (int k = 0; k < 12; k++) { float p = Prof(x, alt - (Court.AG_DERIN - Court.BANT) * (k + .5f) / 12); if (p > m) m = p; }
            return m;
        }

        // x,z → ağ dalgasının sapması (a birimi; ×GORSEL = metre, Hareketi azalt hariç, bant hariç). Bantta ve üstünde sıfır.
        public float At(float x, float zz)
        {
            float f = x / Court.CW * (N - 1); int i = (int)Math.Floor(f); float t = f - i;
            float a0 = a[Math.Max(0, Math.Min(N - 1, i))], a1 = a[Math.Max(0, Math.Min(N - 1, i + 1))];
            return (a0 * (1 - t) + a1 * t) * Prof(x, zz);
        }

        // Ağın x,z'deki GÖRÜNEN sapması (m): ağ dalgası + bandın sapması (ağ banda dikili, bandın altında bandla aynı yerde),
        // Hareketi azalt dahil. Görünüm ve ağa gömülü topun konumu bunu kullanır.
        public float Gorunen(float x, float zz) { return (At(x, zz) * GORSEL + TapeAt(x)) * Olcek; }
    }
}
