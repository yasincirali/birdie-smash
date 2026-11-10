// Oyuncu gelişimi (2026-11-10, yasin: "Kariyer yolu ve progression karakter gelişimi istediğim gibi değil, 3A oyunların
// levelling'lerine de, benzer oyunlardaki best practice'lere bakarak ilerle"; benchmark
// arge/fikirler/2026-11-10_kariyer_gelisim_benchmark.md §4.3). Seviye o güne kadar yalnız kapı açıyordu: oyuncunun KENDİSİ
// hiç gelişmiyor, bütün güç ekipmandaydı. Artık her seviye bir gelişim puanı verir ve oyuncu onu beş daldan birine koyar.
// Kurallar:
//   · Puan yalnız seviyeden gelir: Sv 30'da 29 puan. Tüy'le, reklamla, parayla alınmaz (yasin 1 Kas: seviye ve Tüy ayrı).
//   · Dal başına tavan 10, beş dal = 50 yuva: her şeyi doldurmak imkânsız, oyuncu yaklaşık üç dal seçer (yapı kararı).
//   · Dağıtım ücretsiz geri alınır (yanlış seçim cezası yok, denemek serbest); rastgelelik, süre, seri yok.
//   · Etki küçük ve ekipmanın altında: dal tavanında güç/kontrol/tatlı nokta +%10, koşu +%8, yorulma ÷1,15. Ekipman yuva
//     seviyesi tek başına %18'e kadar çıkar; gelişim onun yerine geçmez, üstüne biner.
//   · Eşit şartlarda (kariyer dışı "eşit" maç) etkisizdir: ekipman seviyesiyle aynı kural (Gear.KSv).
//   · Puan seviyeden hesaplanır, ayrıca saklanmaz: eski kayıtla açan oyuncu seviyesi kadar puanı hazır bulur. Kayıt yalnız
//     dağılımdır ("tuytop_gelisim" = "g,k,t,c,d"); bozuk ya da fazla dağılım okurken kırpılır, puan asla eksiye düşmez.
using System;

namespace BirdieSmash.Core
{
    public static class Gelisim
    {
        public const string ANAHTAR = "tuytop_gelisim";
        public const int DAL = 5, TAVAN = 10;
        public const int GUC = 0, KONTROL = 1, TATLI = 2, CABUK = 3, DAYAN = 4;
        // Adlar takım çubuklarıyla aynı (Gear.CUBUK_AD): oyuncu aynı beş kavramı iki yerde iki adla görmesin.
        public static readonly string[] AD = { "Güç", "Kontrol", "Tatlı nokta", "Çabukluk", "Dayanıklılık" };
        public static readonly string[] NE = { "Smaç ve derin vuruş daha hızlı", "Vuruş nişan aldığın yere daha yakın düşer", "Temiz vuruş penceresi genişler", "Topa daha çabuk koşarsın", "Uzun rallide daha geç yorulursun" };
        // Puan başına etki (çarpan artışı). Dayanıklılıkta bölen: yorK /= 1 + .015n.
        static readonly float[] ETKI = { .01f, .01f, .01f, .008f, .015f };

        public static int Toplam(int sv) { return Math.Max(0, Math.Min(30, sv) - 1); }
        public static int Harcanan(int[] p) { int t = 0; for (int i = 0; i < DAL; i++) t += p[i]; return t; }
        public static int Bos(int[] p, int sv) { return Math.Max(0, Toplam(sv) - Harcanan(p)); }

        public static int[] Oku(string kayit, int sv)
        {
            var p = new int[DAL];
            if (!string.IsNullOrEmpty(kayit))
            {
                var s = kayit.Split(',');
                for (int i = 0; i < DAL && i < s.Length; i++) { int v; if (int.TryParse(s[i], out v)) p[i] = Math.Max(0, Math.Min(TAVAN, v)); }
            }
            // Seviyenin verdiğinden fazlası (elle bozulmuş kayıt, XP'si düşmüş kayıt): sondan başlayarak kırpılır.
            int fazla = Harcanan(p) - Toplam(sv);
            for (int i = DAL - 1; i >= 0 && fazla > 0; i--) { int d = Math.Min(p[i], fazla); p[i] -= d; fazla -= d; }
            return p;
        }
        public static string Yaz(int[] p) { return p[0] + "," + p[1] + "," + p[2] + "," + p[3] + "," + p[4]; }

        public static bool VerOlur(int[] p, int dal, int sv) { return dal >= 0 && dal < DAL && p[dal] < TAVAN && Bos(p, sv) > 0; }
        public static bool Ver(int[] p, int dal, int sv) { if (!VerOlur(p, dal, sv)) return false; p[dal]++; return true; }
        public static bool GeriAl(int[] p, int dal) { if (dal < 0 || dal >= DAL || p[dal] <= 0) return false; p[dal]--; return true; }

        // Ekranda yazılan yüzde: dalın şu anki etkisi (tam sayıya yuvarlanmış).
        public static int Yuzde(int dal, int n) { return (int)Math.Round(ETKI[dal] * n * 100); }

        // 2026-11-11 (inceleme turu, fikir 02:05 #1): Atölye "Takımın şimdi" çubukları ekipmanı anlatır ama oyuncu aynı beş adı
        // burada puanla büyütüyor; çubuğun ucunda gelişimin payı ayrı tonla ve "+n" yazısıyla görünmezse puan vermenin görünür
        // ödülü kalmıyor. Pay = Cubuklar(ekipman + gelişim) − Cubuklar(ekipman); tarz yine girmez. Eşit şartlarda sıfır.
        public static int[] CubukPayi(GearStats gse, int[] p, bool esit)
        {
            var a = Gear.Cubuklar(gse); var b = Gear.Cubuklar(Uygula(gse, p, esit));
            for (int i = 0; i < a.Length; i++) a[i] = Math.Max(0, b[i] - a[i]);
            return a;
        }

        public static GearStats Uygula(GearStats g, int[] p, bool esit)
        {
            if (esit || p == null || Harcanan(p) == 0) return g;
            var o = g.Kopya();
            o.guc *= 1 + ETKI[GUC] * p[GUC]; o.kontrol *= 1 + ETKI[KONTROL] * p[KONTROL]; o.tatli *= 1 + ETKI[TATLI] * p[TATLI];
            o.kosu *= 1 + ETKI[CABUK] * p[CABUK]; o.yorK /= 1 + ETKI[DAYAN] * p[DAYAN];
            return o;
        }
    }
}
