// Dereceli lig (2026-11-07, online raporu https://claude.ai/artifact/V2uZN5PBYvFSP3GC3RAynf "Derecelendirme ve lig").
// İki katman: görünmeyen beceri puanı (Glicko-2; eşleştirme bununla yapılır) ve görünen kupa/lig (oyuncunun gördüğü,
// gösterdiği). Neden ikisi: Glicko-2 sayısı oyuncuya soğuk ve kayıpta sert düşer; kupa ise kazanınca hep artar, lig tabanı
// düşmeyi sınırlar (ilerlemeyi suçlamayan ilke). Kupa farkı rakibin gizli puanına göre: güçlü rakibi yenmek daha çok verir.
// Sunucu (Bulut Kod, 2. aşama) AYNI kuralı uygular; istemci yalnız gösterir ve tahmin eder. Bu dosya Unity'siz (BotMac Ligler).
using System;
using System.Globalization;

namespace BirdieSmash.Core
{
    public class LigBilgi { public string id, ad; public int taban; public int kademe; }   // kademe: 3 (III, II, I) ya da 1

    public class LigDurum
    {
        public double r = 1500, rd = 350, sigma = .06;   // Glicko-2 (ölçek 1500/173,7178)
        public int kupa, taban, mac, enYuksekLig = -1;   // taban: geçilen ligin alt sınırı (altına inilmez); enYuksekLig: sezon rozeti
        public bool Yerlesti { get { return mac >= Lig.YERLESTIRME; } }
    }

    public static class Lig
    {
        public const int YERLESTIRME = 5;   // ilk 5 maç lig vermez, puanı oturtur (kupa yine yazılır, gösterilmez)
        public const double TAU = .5;        // oynaklık değişim hızı (Glickman'ın önerdiği .3–1.2 aralığının ortası)
        public static readonly LigBilgi[] LIGLER = {
            new LigBilgi { id = "bronz", ad = "Bronz", taban = 0, kademe = 3 },
            new LigBilgi { id = "gumus", ad = "Gümüş", taban = 300, kademe = 3 },
            new LigBilgi { id = "altin", ad = "Altın", taban = 700, kademe = 3 },
            new LigBilgi { id = "platin", ad = "Platin", taban = 1200, kademe = 3 },
            new LigBilgi { id = "elmas", ad = "Elmas", taban = 1800, kademe = 3 },
            new LigBilgi { id = "sampiyon", ad = "Şampiyon", taban = 2500, kademe = 1 } };

        public static int LigNo(int kupa) { int i = 0; for (int k = 0; k < LIGLER.Length; k++) if (kupa >= LIGLER[k].taban) i = k; return i; }
        // Kademe: 3 = III (ligin ilk üçte biri) … 1 = I. Şampiyon tek kademe.
        public static int Kademe(int kupa)
        {
            int i = LigNo(kupa); var L = LIGLER[i]; if (L.kademe == 1 || i + 1 >= LIGLER.Length) return 1;
            int boy = LIGLER[i + 1].taban - L.taban, ic = kupa - L.taban; return 3 - Math.Min(2, ic * 3 / boy);
        }
        public static string Ad(int kupa) { var L = LIGLER[LigNo(kupa)]; int k = Kademe(kupa); return L.kademe == 1 ? L.ad : L.ad + " " + (k == 3 ? "III" : k == 2 ? "II" : "I"); }

        // Beklenen kazanma olasılığı (Glicko-2 E), rakibin belirsizliği hesaba katılarak.
        public static double Beklenen(LigDurum a, LigDurum b)
        {
            double mu = (a.r - 1500) / 173.7178, muJ = (b.r - 1500) / 173.7178, phiJ = b.rd / 173.7178;
            return 1 / (1 + Math.Exp(-G(phiJ) * (mu - muJ)));
        }
        static double G(double phi) { return 1 / Math.Sqrt(1 + 3 * phi * phi / (Math.PI * Math.PI)); }

        // Kupa farkı: kazanınca +18…+32, kaybedince −12…−26 (beklenene göre). Yerleştirmede kupa gizli puandan türetilir.
        public static int KupaFarki(LigDurum a, LigDurum b, bool kazandi)
        {
            double E = Beklenen(a, b);
            return kazandi ? (int)Math.Round(18 + 14 * (1 - E)) : -(int)Math.Round(12 + 14 * E);
        }

        // Bir maç sonucu: a güncellenir (b yalnız okunur; sunucu iki tarafı ayrı çağırır). Dönen değer görünen kupa farkı.
        public static int Sonuc(LigDurum a, LigDurum b, bool kazandi)
        {
            int fark = KupaFarki(a, b, kazandi);
            GlickoGuncelle(a, b, kazandi ? 1 : 0);
            a.mac++;
            if (!a.Yerlesti) return 0;
            if (a.mac == YERLESTIRME) { a.kupa = Math.Max(0, (int)Math.Round((a.r - 1200) * 1.2)); a.taban = LIGLER[LigNo(a.kupa)].taban; fark = 0; }   // yerleştirme biter: gizli puandan başlangıç kupası
            else a.kupa = Math.Max(a.taban, a.kupa + fark);
            int lig = LigNo(a.kupa); a.taban = Math.Max(a.taban, LIGLER[lig].taban); a.enYuksekLig = Math.Max(a.enYuksekLig, lig);
            return fark;
        }

        // Sezon sonu (4 hafta): kupa yumuşak sıfırlanır (%60), taban yeni kupanın ligine iner; rozet (enYuksekLig) çağıran
        // tarafından kalıcı rozet listesine yazılır, burada sıfırlanır. Gizli puanın belirsizliği biraz açılır (geri dönüşte hızlı oturur).
        public static int SezonBitir(LigDurum a)
        {
            int rozet = a.enYuksekLig;
            if (a.Yerlesti) { a.kupa = (int)Math.Round(a.kupa * .6); a.taban = LIGLER[LigNo(a.kupa)].taban; a.enYuksekLig = LigNo(a.kupa); }
            a.rd = Math.Min(350, Math.Sqrt(a.rd * a.rd + 50 * 50));
            return rozet;
        }

        // Glicko-2, tek maçlık dönem (Glickman 2012, "Example of the Glicko-2 system", adım 2–8).
        static void GlickoGuncelle(LigDurum a, LigDurum b, double s)
        {
            double mu = (a.r - 1500) / 173.7178, phi = a.rd / 173.7178, muJ = (b.r - 1500) / 173.7178, phiJ = b.rd / 173.7178;
            double g = G(phiJ), E = 1 / (1 + Math.Exp(-g * (mu - muJ))), v = 1 / (g * g * E * (1 - E)), delta = v * g * (s - E);
            double aa = Math.Log(a.sigma * a.sigma), A = aa, B, eps = 1e-6;
            Func<double, double> f = x => { double ex = Math.Exp(x); return ex * (delta * delta - phi * phi - v - ex) / (2 * Math.Pow(phi * phi + v + ex, 2)) - (x - aa) / (TAU * TAU); };
            if (delta * delta > phi * phi + v) B = Math.Log(delta * delta - phi * phi - v);
            else { int k = 1; while (f(aa - k * TAU) < 0 && k < 100) k++; B = aa - k * TAU; }
            double fA = f(A), fB = f(B);
            for (int i = 0; i < 100 && Math.Abs(B - A) > eps; i++)
            {
                double C = A + (A - B) * fA / (fB - fA), fC = f(C);
                if (fC * fB <= 0) { A = B; fA = fB; } else fA /= 2;
                B = C; fB = fC;
            }
            double sig = Math.Exp(A / 2), phiS = Math.Sqrt(phi * phi + sig * sig), phiY = 1 / Math.Sqrt(1 / (phiS * phiS) + 1 / v), muY = mu + phiY * phiY * g * (s - E);
            a.r = 173.7178 * muY + 1500; a.rd = Math.Max(30, Math.Min(350, 173.7178 * phiY)); a.sigma = sig;   // RD tabanı 30: çok oynayan da az da olsa oynar
        }

        // Kayıt biçimi (yerel önbellek; asıl kayıt sunucuda): "r;rd;sigma;kupa;taban;mac;enYuksekLig", değişmez kültür.
        public static string Yaz(LigDurum a)
        {
            var c = CultureInfo.InvariantCulture;
            return string.Join(";", a.r.ToString("0.###", c), a.rd.ToString("0.###", c), a.sigma.ToString("0.######", c), a.kupa.ToString(c), a.taban.ToString(c), a.mac.ToString(c), a.enYuksekLig.ToString(c));
        }
        public static LigDurum Oku(string s)
        {
            var a = new LigDurum(); if (string.IsNullOrEmpty(s)) return a;
            var p = s.Split(';'); var c = CultureInfo.InvariantCulture; if (p.Length < 7) return a;
            double d; int n;
            if (double.TryParse(p[0], NumberStyles.Float, c, out d)) a.r = d;
            if (double.TryParse(p[1], NumberStyles.Float, c, out d)) a.rd = d;
            if (double.TryParse(p[2], NumberStyles.Float, c, out d)) a.sigma = d;
            if (int.TryParse(p[3], NumberStyles.Integer, c, out n)) a.kupa = Math.Max(0, n);
            if (int.TryParse(p[4], NumberStyles.Integer, c, out n)) a.taban = Math.Max(0, n);
            if (int.TryParse(p[5], NumberStyles.Integer, c, out n)) a.mac = Math.Max(0, n);
            if (int.TryParse(p[6], NumberStyles.Integer, c, out n)) a.enYuksekLig = n;
            return a;
        }
    }
}
