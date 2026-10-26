// Günün maçı (komple-oyun tasarımı §3.5, adım 8; 2026-10-26). Veri veri/gunluk.json.
// Tarih → maç (saf fonksiyon): aynı yerel gün herkes aynı rakip, saha, güç (g), değiştirici ve maç tohumuyla oynar.
// Sunucu yok; saati ileri almak yalnız kendi oyununu etkiler (ekonomi §3.0). Sıralama tablosu yok (başarım kıyas değil).
// Kurallar (değişmez kararlar): seri yok, kaçırma cezası yok, süreli/kaçırılabilir özel ödül yok, geri sayım yok. Ödül
// günde bir kez, kazanınca (+40 Tüy, +30 XP); kaybetmek normal maç ödülünü verir, bugünün ödülü açık kalır. Dün
// oynanmadıysa hiçbir şey kaybolmaz; ekranda "yarın yenisi" yazar, saat yazmaz.
// Neden kendi karma fonksiyonu: System.Random'ın tohumlu algoritması .NET sürümleri arasında aynı kalacağının sözü yok;
// "herkes aynı maç" ancak bu dosyanın sabit aritmetiğiyle garanti. Değiştirici ve rakip 6 günlük karışık bloklarla döner:
// her blokta her biri bir kez, iki blok arasında da arka arkaya aynısı gelmez (BotMac 60 günü doğrular).
// Değiştiriciler (hepsi var olan sistemlerden, yeni kural yok; Match kancaları aşağıda):
//   altin   her 3. rallinin sayısı 2 (servisten önce yazılır)
//   file    file önünden (2,3 m) vurulup kazanılan sayı 2
//   usta    Usta zorluk (oyuncu koşar), 5 sayı
//   patron  rakip imza vuruşunu sık dener (aralık 2 sayı, olasılık .8; maçta en çok IMZA_AZAMI kuralı aynı)
//   uzun    8+ vuruşluk rallinin sayısı 2
//   nefes   iki oyuncunun nefesi 0,7 kat (yorulma 1/0,7 hızlı)
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class GunlukDeg { public string id, ad, aciklama; public int uzunluk = 5; }
    public class GunlukMac { public string tarih, stil, saha; public GunlukDeg deg; public float g; public int uzunluk, tohum, gun; }

    public static class Gunluk
    {
        static string[] rakipler, sahalar; static GunlukDeg[] degler; static float g0 = .95f, g1 = 1.15f;
        public static int OdulTuy = 40, OdulXp = 30;
        public static readonly DateTime BAS = new DateTime(2026, 1, 1);
        public static void Sifirla() { rakipler = null; }
        public static GunlukDeg[] Degistiriciler { get { Yukle(); return degler; } }
        public static GunlukDeg Deg(string id) { foreach (var d in Degistiriciler) if (d.id == id) return d; return null; }

        static void Yukle()
        {
            if (rakipler != null) return;
            var o = Json.Obj(Json.Parse(KariyerVeri.Metin("gunluk"))); if (o == null) throw new InvalidOperationException("Birdie Smash: veri/gunluk.json okunamadı");
            var rl = (List<object>)o["rakipler"]; var r = new string[rl.Count]; for (int i = 0; i < r.Length; i++) r[i] = (string)rl[i];
            var sl = (List<object>)o["sahalar"]; var sa = new string[sl.Count]; for (int i = 0; i < sa.Length; i++) sa[i] = (string)sl[i];
            var gl = (List<object>)o["g"]; g0 = (float)(double)gl[0]; g1 = (float)(double)gl[gl.Count - 1];
            var od = Json.Obj(o["odul"]); OdulTuy = (int)Json.Num(od, "tuy", 40); OdulXp = (int)Json.Num(od, "xp", 30);
            var dl = new List<GunlukDeg>();
            foreach (var x in (List<object>)o["degistiriciler"]) { var d = Json.Obj(x); dl.Add(new GunlukDeg { id = Json.Str(d, "id", ""), ad = Json.Str(d, "ad", ""), aciklama = Json.Str(d, "aciklama", ""), uzunluk = (int)Json.Num(d, "uzunluk", 5) }); }
            foreach (var id in r) if (!KariyerVeri.Stil.ContainsKey(id)) throw new InvalidOperationException("Birdie Smash: gunluk.json bilinmeyen tarz " + id);
            degler = dl.ToArray(); sahalar = sa; rakipler = r;
        }

        // Yerel gün "YYYY-AA-GG" (kayıt ve karşılaştırma; kültürden bağımsız).
        public static string Tarih(DateTime d) { return d.Year.ToString("0000") + "-" + d.Month.ToString("00") + "-" + d.Day.ToString("00"); }
        public static int GunNo(DateTime d) { return (int)Math.Floor((d.Date - BAS).TotalDays); }

        // lowbias32 (Chris Wellons): az işlemle iyi dağılan tamsayı karması. uint taşması bilinçli (unchecked).
        public static uint Karma(uint x) { unchecked { x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16; return x; } }

        // n öğenin blok k'deki karışık sırası (Fisher–Yates, karma akışıyla); ilk öğe önceki bloğun son öğesine eşitse ilk iki
        // yer değişir (n > 2'de bloğun son öğesi bu değişimden etkilenmez, özyineleme gerekmez).
        static int[] Blok(int n, int k, uint tuz)
        {
            var a = Sira(n, k, tuz); if (k > 0 || k < 0) { var o = Sira(n, k - 1, tuz); if (a[0] == o[n - 1]) { int t = a[0]; a[0] = a[1]; a[1] = t; } }
            return a;
        }
        static int[] Sira(int n, int k, uint tuz)
        {
            var a = new int[n]; for (int i = 0; i < n; i++) a[i] = i; uint h = Karma((uint)k * 2654435761U ^ tuz);
            for (int i = n - 1; i > 0; i--) { h = Karma(h + (uint)i); int j = (int)(h % (uint)(i + 1)); int t = a[i]; a[i] = a[j]; a[j] = t; }
            return a;
        }
        static int Sec(int n, int gun, uint tuz) { int k = (int)Math.Floor(gun / (double)n), i = gun - k * n; return Blok(n, k, tuz)[i]; }

        public static GunlukMac Bugun(DateTime d)
        {
            Yukle(); int gun = GunNo(d); uint u = (uint)gun;
            var m = new GunlukMac { tarih = Tarih(d), gun = gun };
            m.deg = degler[Sec(degler.Length, gun, 0x9e3779b9U)];
            m.stil = rakipler[Sec(rakipler.Length, gun + 3, 0x85ebca6bU)];   // +3: değiştiriciyle aynı fazda dönmesin
            m.saha = sahalar[(int)(Karma(u * 5 + 2) % (uint)sahalar.Length)];
            m.g = (float)Math.Round(g0 + (g1 - g0) * (Karma(u * 3 + 1) % 1001) / 1000.0, 2);
            m.uzunluk = m.deg.uzunluk; m.tohum = (int)(Karma(u * 7 + 11) & 0x7fffffff);
            return m;
        }

        // Maçın rakibi: tarzın kopyası, hız ×g, hata ÷g; imza yalnız "Patron provası"nda (Match.DegistiriciKur).
        public static Style Rakip(GunlukMac m)
        {
            var b = KariyerVeri.Stil[m.stil].Clone(); b.hiz *= m.g; b.hata /= m.g; b.kariyer = -1; b.patron = false; if (m.deg.id != "patron") b.imza = null; return b;
        }
    }

    public partial class Match
    {
        public string degistirici; int sayiNo; float vurusY;
        public const string ALTIN_YAZI = "Altın ralli: bu sayı 2";

        // Maç kurulurken (serbest/kariyer maçında null). GS ve stil kopyalanır: kaynak nesneler (Ayar.GS, veri) değişmez.
        void DegistiriciKur(string id)
        {
            degistirici = id; if (id == null) return;
            if (id == "usta") easy = false;
            if (id == "patron") { stil = stil.Clone(); stil.patron = stil.imza != null; stil.imzaAra = 2; stil.imzaP = .8f; }
            if (id == "nefes") { GS = GS.Kopya(); GS.yorK /= .7f; stil = stil.Clone(); stil.yorK /= .7f; }
        }

        // Kazanılan sayının değeri (Point): 1 ya da 2.
        int SayiDegeri(string winner)
        {
            switch (degistirici)
            {
                case "altin": return sayiNo % 3 == 2 ? 2 : 1;
                case "uzun": return rally >= 8 ? 2 : 1;
                case "file": return s != null && s.from == winner && Math.Abs(vurusY - Court.NET) < 2.3f ? 2 : 1;
            }
            return 1;
        }
    }
}
