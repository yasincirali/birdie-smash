// Kort ve top fiziği: HTML sürümünün (assets/game/index.html) birebir C# karşılığı.
// Neden saf C# (UnityEngine yok): kurallar ve fizik Unity'siz derlenip bot maçıyla
// test edilebilsin (tool/unity_test). Koordinatlar oyunla aynı: x kort eni (0..CW),
// y kort boyu (0 = rakibin arka çizgisi, L = benim arka çizgim), z yükseklik.
// Unity görünümü (GameRoot) bunları (x-MID, z, NET-y) olarak dünyaya çevirir.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public static class Court
    {
        // BWF tekler: 13,40 × 6,10 m; yan tekler çizgisi 0,46 m içeride; kısa servis çizgisi
        // fileden 1,98 m; file ortada 1,524 m, direkte 1,55 m.
        public const float L = 13.4f, CW = 6.1f, NET = L / 2f, SL = .46f, SR = CW - .46f, SSL = 1.98f;
        public const float NETH = 1.524f, NETP = 1.55f, MID = CW / 2f;

        // Bandın üst kenarı (2026-10-27, BWF Law 1.4–1.5, yasin "file de standartlarda olmalı"): gergin kordon iki direk arasında
        // sarkar; sarkan gergin kordon parabol çizer (eğimi ortada sıfır). Eskiden ortada kırılan V idi (iki doğru, ortada
        // 0,85 mm/10 cm kırık; çeyrekte 1,537 m). Uçlar ve orta aynı: ortada 1,524, çift yan çizgideki direkte 1,55 m;
        // çeyrekte 1,5305 m (fark ≤ 6,5 mm, file payı kuralları aynı fonksiyonu okur).
        public static float NetLimit(float x) { float u = (x - MID) / MID; return NETH + (NETP - NETH) * u * u; }
        // File parçaları (BWF Law 1.5): üstte 75 mm gergin kordonlu bant, altında 760 mm derin ağ (alt kenarı bandın 0,76 m
        // altında); altı boş. 2026-10-26: top bantta sert sekip hızını kaybeder, ağda gömülür (Match ağ teması).
        public const float BANT = .075f, AG_DERIN = .76f;
        // Ağ gözü (BWF Law 1.3: koyu, ince kordon, göz 15–20 mm): görünüm dokusu bu adımla tekrar eder (eskiden 40 mm).
        // File eni CW (6,1 m, direkler çift yan çizgide x 0 ve CW): ağ ve bant direk merkezine kadar uzanır, uçta boşluk yok.
        public const float AG_GOZ = .019f;
        // Fileden ayrılan top merkezinin file düzlemine en yakın mesafesi: gerçek mantar yarıçapı 13,5 mm, ama top 3,2×
        // okunurluk ölçeğinde çizildiği için (ShuttleView.G) mantar görüntüde 4,3 cm; 2 cm ile top ağın içinde görünüyordu.
        public const float TOP_PAY = .05f;
        // Katı engeller (2026-10-26, yasin "objelerde iç içe geçmeleri de çözmen lazım"): direkler çift yan çizgide (x 0 ve
        // CW, file hizası), hakem sandalyeleri CourtView ile aynı yerde (baş hakem x −0,75 file hizası, servis hakemi x CW+0,6,
        // fileden 0,1 m rakip tarafında). Oyuncu gövdesi (1,75× okunurluk ölçeğinde) yarıçap OYUNCU_R; merkezler bu
        // engellerden r + OYUNCU_R'den yakına giremez.
        public const float OYUNCU_R = .32f, DIREK_R = .04f, BH_X = -.75f, SH_X = CW + .6f, SH_DY = -.1f, BH_R = .36f, SH_R = .3f;
        public static bool OutSingles(float x, float y) { return x < SL || x > SR || y < 0 || y > L; }
    }

    public class Shuttle
    {
        // Tüy top: yerçekimi + hızın karesiyle sürüklenme, son hız ~6,7 m/s. Top hızlı çıkar,
        // çabuk yavaşlar, neredeyse dik düşer. DT 1/240: tahmin ile canlı uçuş aynı adımı kullanır.
        // Kaynak (2026-10-27 fizik denetimi, tool/unity_test/FizikOlc.cs): a = −g ẑ − (g/U∞²)|v|v; tüy topta U∞ 6,5–6,9 m/s
        // (Cohen, Darbois Texier, Quéré, Clanet 2015 "The physics of badminton", New J. Phys. 17 063001; JSSM 2009 8:657).
        // Aerodinamik boy U∞²/g = 4,6 m: hız mesafeyle e^(−x/4,6) düşer, ~3,2 m'de yarılanır; yörünge "Tartaglia" biçimi:
        // çıkış dik ve hızlı, tepeden sonra dikey asimptota yakın iniş (ölçülen clear: tepe 0,67 s, iniş 1,56 s, düşüş 83°,
        // inişte 6,5 m/s). Top vuruştan sonra ~20 ms'de mantar-önde döner, ~80 ms salınır (Darbois Texier, Cohen, Quéré,
        // Clanet 2012 "Shuttlecock dynamics", Procedia Eng. 34; görünüm ShuttleView). Dönüş ve Magnus modelde yok: slice
        // yalnız sürüklenmeyi büyütür (SliceKd).
        public const float GR = 9.81f, VT = 6.7f, KD = GR / (VT * VT), DT = 1f / 240f;

        public float x, y, z, vx, vy, vz, kd = KD, t;
        // Ağ teması (2026-10-26): agda = top ağa gömülü (agT süre, agSure itme, agAsili asılı kalma, agMax en derin görünen
        // sapma, agYan vuranın tarafı ±1); altindan = file altından geçti; yerT: yere düştükten sonra geçen süre (sekme + devrilme).
        public bool agda, altindan; public float agT, agSure, agAsili, agMax, agYan, yerT, inVx, inVy, inVz;
        public string from, type;
        public bool serve, super, netDone, dead, netHit, slice, netCord;
        public float[] ogrHedef;   // antrenman: bu vuruşun düşmesi gereken bant ("Hedefte! ★")
        public string imza;        // rakibin imza vuruşu (Core/Imza): cevap kuralı bu uçuşa bakar
        public float gecPay = float.NaN;   // 2026-10-26 (Core/Dril, kısa servis görevi): file düzlemini geçerken bant payı (m)
        public float netK;
        public NetPass netPred;   // tahmin edilen file geçişi (canlı uçuş aynı karara varsın)
        public SimResult landPred;

        public Shuttle Clone() { return (Shuttle)MemberwiseClone(); }
        public float Speed { get { return (float)Math.Sqrt(vx * vx + vy * vy + vz * vz); } }

        public static void Step(ref float x, ref float y, ref float z, ref float vx, ref float vy, ref float vz, float kd, float dt)
        {
            float sp = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz);
            vx -= kd * sp * vx * dt; vy -= kd * sp * vy * dt; vz -= (GR + kd * sp * vz) * dt;
            x += vx * dt; y += vy * dt; z += vz * dt;
        }

        public void Step(float dt) { Step(ref x, ref y, ref z, ref vx, ref vy, ref vz, kd, dt); }

        // Yerdeki top (2026-10-26, yasin "topun yere düşüşü gerçekçi olmalı"): mantar önce yere değer, mantar yumuşak olduğu için
        // küçük bir sekme yapar (iniş hızı ~6,5 m/s'de ~4 cm, 0,18 s), sekerken etek devrilir ve top yan yatar: mantar ve etek
        // ağzı birlikte yere değer, eksen yataydan asin((Rt−Rc)/L) = asin((32−13,5)/66) ≈ 16° kalkık (etek yukarıda).
        // Devrilme yerçekimiyle hızlanır (u²), 0,3 s; sonunda küçük bir sönümlü tıkırtı. Saf zaman fonksiyonu: görünüm (Unity
        // ShuttleView, HTML shuttleModel) aynı sayıları okur. hop: m; devril: 0 = mantar aşağı dik, 1 = yan yatmış.
        public const float YER_EGIM = .284f;   // rad, ≈16,3°
        public static void YerPoz(float t, float vInis, out float hop, out float devril)
        {
            float h0 = Math.Min(.05f, .0062f * Math.Abs(vInis)), T1 = 2 * (float)Math.Sqrt(2 * Math.Max(1e-4f, h0) / GR), T2 = T1 * .5f;
            if (t < T1) { float u = t / T1; hop = 4 * h0 * u * (1 - u); }
            else if (t < T1 + T2) { float u = (t - T1) / T2; hop = h0 * .25f * 4 * u * (1 - u); }
            else hop = 0;
            float d0 = T1 * .45f, dT = .3f, uu = Math.Max(0, Math.Min(1, (t - d0) / dT));
            devril = uu * uu;
            float te = t - d0 - dT; if (te > 0) devril -= .07f * (float)(Math.Exp(-14 * te) * Math.Sin(32 * te));
        }

        public static SimResult Simulate(float x, float y, float z, float vx, float vy, float vz, bool keep, float kd)
        {
            var r = new SimResult(); if (keep) r.path = new List<PathPt>();
            float t = 0;
            for (int i = 0; i < 4000; i++)
            {
                float py = y; Step(ref x, ref y, ref z, ref vx, ref vy, ref vz, kd, DT); t += DT;
                if (keep && i % 4 == 0) r.path.Add(new PathPt { t = t, x = x, y = y, z = z });
                if (r.net == null && (py > Court.NET) != (y > Court.NET))
                {
                    float lim = Court.NetLimit(x); r.net = new NetPass { z = z, ok = z > lim, t = t, lim = lim };
                    if (!r.net.ok) break;
                }
                if (z <= 0) break;
            }
            r.x = x; r.y = y; r.t = t; return r;
        }

        // Belirli hedefe vuruş: açı sabitken hızı ya da hız sabitken açıyı ikiye bölmeyle bulur. 18 adım (HTML 2026-10-24,
        // eskiden 26): düşüş yerindeki fark 1 mm'nin altında, vuruş karesinde %30 daha az fizik.
        // File payı (2026-10-26, yasin "topun fiziği badmintonun doğasına uygun olmalı"; ölçüm tool/unity_test/FizikOlc.cs):
        // ikiye bölme menzili sürekli sanıyor ama fileye takılan uçuş "kısa" sayıldığı için çözüm hedefe ulaşamayan
        // vuruşlarda file sınırına yapışıyordu. Ölçülen: baş üstü drop (7°) bandı −1 cm'de geçiyordu (rakip drop'larının
        // %35'i bandı sıyırdı, %10'u fileye), file önünden alçak lift/clear 140 m/s'ye (arama tavanı) çıkıp fileye gidiyordu,
        // sıçrayarak smaç %29 fileye. Gerçek oyuncu fileye yakın ve alçaktan topu dik kaldırır, uzaktan smaçta da fileyi
        // geçecek açıyı seçer: hedef ancak fileyi geçerek ulaşılabiliyorsa ulaşılır. Bulunan çözüm bandı FILE_PAY'den az
        // geçiyorsa (ya da açı modunda menzil 0,3 m'den çok şaşıyorsa): açı modunda açı fileyi geçen ilk değere kadar
        // 0,06 rad adımlarla dikleşir (hız yeniden bulunur); hız modunda hız korunur, bandı FILE_PAY ile geçen en düşük açı
        // seçilir (top hedeften biraz derine düşer). İsabet hatası (poor hızı ±%8, kenar) bunun DIŞINDA kalır: fileye
        // takmak kötü vuruşun sonucudur, nişan almanın değil. Düzeltme yalnız sınırda çalışır; diğer vuruşlar birebir aynı.
        public const float FILE_PAY = .06f;
        // kd: uçuşun gerçek sürüklenmesi (2026-10-26: slice ×1,35 sürüklenmeyle uçar; nişan KD ile alınırsa top kısa kalıp fileye
        // gidiyordu, bot ölçümü 150/150). Varsayılan KD, diğer vuruşlar birebir aynı.
        public static void Aim(float fx, float fy, float fz, float tx, float ty, bool byAngle, float val, out float ovx, out float ovy, out float ovz, float kd = KD)
        {
            float dx = tx - fx, dy = ty - fy, R = (float)Math.Sqrt(dx * dx + dy * dy); if (R < 1e-3f) R = 1e-3f;
            float ux = dx / R, uy = dy / R;
            Func<float, float, SimResult> sim = (v, a) => { float c = (float)Math.Cos(a); return Simulate(fx, fy, fz, ux * v * c, uy * v * c, v * (float)Math.Sin(a), false, kd); };
            Func<SimResult, float> range = r => (float)Math.Sqrt((r.x - fx) * (r.x - fx) + (r.y - fy) * (r.y - fy));
            Func<SimResult, float> pay = r => r.net == null ? -9 : r.net.z - r.net.lim;
            Func<float, float> hizBul = a => { float l = 1, h = 140; for (int k = 0; k < 18; k++) { float m = (l + h) / 2; if (range(sim(m, a)) < R) l = m; else h = m; } return (l + h) / 2; };
            float lo, hi, vv, aa;
            if (byAngle) { aa = val; vv = hizBul(aa); }
            else { vv = val; lo = -1.2f; hi = .9f; for (int k = 0; k < 18; k++) { float m = (lo + hi) / 2; if (range(sim(vv, m)) < R) lo = m; else hi = m; } aa = (lo + hi) / 2; }
            var r0 = sim(vv, aa);
            if (pay(r0) < FILE_PAY || (byAngle && Math.Abs(range(r0) - R) > .3f))
            {
                if (byAngle)
                {   // fileye olan yol boyu mesafe ve bandın FILE_PAY üstüne çekilen kirişin açısı: aramanın başlangıcı
                    float dn = Math.Abs(fy - Court.NET) / Math.Max(.05f, Math.Abs(uy)), a0 = Math.Max(aa + .05f, (float)Math.Atan2(Court.NETH + FILE_PAY - fz, dn) + .05f);
                    for (float a2 = a0; a2 < 1.45f; a2 += .06f) { float v2 = hizBul(a2); var r2 = sim(v2, a2); if (pay(r2) >= FILE_PAY && Math.Abs(range(r2) - R) < .3f) { vv = v2; aa = a2; break; } }
                }
                else if (pay(sim(vv, .9f)) >= FILE_PAY)
                {   // pay açıyla artar: payı FILE_PAY olan en düşük açı
                    lo = aa; hi = .9f; for (int k = 0; k < 14; k++) { float m = (lo + hi) / 2; if (pay(sim(vv, m)) < FILE_PAY) lo = m; else hi = m; } aa = hi;
                }
            }
            float cc = (float)Math.Cos(aa);
            ovx = ux * vv * cc; ovy = uy * vv * cc; ovz = vv * (float)Math.Sin(aa);
        }
    }

    public class PathPt { public float t, x, y, z; }
    public class NetPass { public float z, t, lim; public bool ok; }   // lim: geçilen yerdeki file yüksekliği (bant payı = z − lim)
    public class SimResult { public float x, y, t; public NetPass net; public List<PathPt> path; }
}
