// Usta yolu (komple-oyun tasarımı §3.2 "Usta zorluğu" ve §6 adım 9; 2026-10-26). Veri kariyer.json "usta".
// Dünya Finalleri'nin finali kazanılınca açılır; Usta zorlukta (oyuncu koşar, easy = false) sonu olmayan bir merdivendir.
// Neden ayrı bir "Tur 5" değil: tasarım dört tur sabit (harita, kayıt dizisi, Game Center bs_tur_2..4); oyunu bitiren
// oyuncunun "yeni şey" akışı kesilmesin diye (tempo ölçüsü: Tur 2 sonrası 19 maçlık boşluk) her galibiyet yeni bir
// basamaktır. Basamaklar 24 kariyer basamağının yerlerini ve rakiplerini 24'lük çevrimlerde karışık sırayla dolaşır
// (Gunluk.Karma, sabit tohum: herkes aynı sırayı görür; kayıtta yalnız sayaç).
// Kurallar: kayıpta geri düşme yok (sayaç azalmaz), süre yok, seri yok. Güç her 6 basamakta "adim" artar, "tavan"da durur:
// sonsuz zorlaşan merdiven en sonunda kazanılamaz olurdu, tavan yorgunluğu keser. Her 6. basamak final: 11 sayı ve imza
// vuruşu (Patron). Ödül: ilk galibiyet ödülü (kariyerIlk: +60 Tüy, +40 XP), yeni yıldız +20 Tüy, 6. basamakta bs_usta_6.
// Güç (g) Kolay kariyerinkiyle kıyaslanmaz: Usta zorluğun kendisi (rakip hızı, hata, erişim, ağır çekim) asıl zorluktur;
// Usta botuyla g 1,0'da galibiyet %3–7 idi, 0,6 ~%50 (kariyer.json "usta" notu).
// Yıldız: basamak başına iki (kazan, basamak görevi). Görev kayıpta da sayılır (kariyerle aynı "kaybetmek de ilerletir"
// kuralı); farkla kazanma yıldızı yok (Usta'da fark beklemek cezalandırır). Neden yıldız eklendi (ilk taslakta yoktu):
// tempo ölçümü (Denge tempo, Usta galibiyeti %50) kariyer sonrası "yeni şey" boşluğunu 10 etkinliğe kadar uzatıyordu;
// art arda kayıplarda görev yıldızı akışı sürdürür. Geçilen basamağa dönülmez: kaçırılan görev yıldızı geri gelmez, ama
// kaybolan bir şey de yoktur (sayaç ileri gider, ödül yalnız eklenir).
using System;

namespace BirdieSmash.Core
{
    public class UstaBasamak { public int n, kaynak; public string stil, yer, saha; public float g; public int uzunluk; public bool final; }

    public static class UstaYolu
    {
        public const int CEVRIM = KariyerVeri.TUR * KariyerVeri.BASAMAK;   // 24
        const uint TUZ = 0x27d4eb2dU;

        public static bool Acik(Ilerleme p) { return p != null && p.UstaAcik; }

        // n. Usta basamağının kaynak kariyer basamağı (0–23): her 24'lük çevrimde her biri bir kez.
        public static int Kaynak(int n)
        {
            int k = n / CEVRIM, i = n % CEVRIM; var a = new int[CEVRIM]; for (int j = 0; j < CEVRIM; j++) a[j] = j;
            uint h = Gunluk.Karma((uint)k * 2246822519U ^ TUZ);
            for (int j = CEVRIM - 1; j > 0; j--) { h = Gunluk.Karma(h + (uint)j); int r = (int)(h % (uint)(j + 1)); int t = a[j]; a[j] = a[r]; a[r] = t; }
            return a[i];
        }

        public static UstaBasamak Basamak(int n)
        {
            if (n < 0) n = 0; var u = KariyerVeri.Usta; int src = Kaynak(n); var k = KariyerVeri.Al(src);
            var b = new UstaBasamak { n = n, kaynak = src, stil = k.stil, yer = Yer(k.yer), saha = k.saha, final = n % 6 == 5 };
            b.g = (float)Math.Round(Math.Min(u.tavan, u.g + u.adim * (n / 6)), 3);
            b.uzunluk = b.final ? u.finalUzunluk : u.uzunluk; return b;
        }

        // Yer adı: kaynak basamağın turnuva aşaması atılır ("Londra finali" → "Londra", "Singapur grup 2" → "Singapur"); Usta'da
        // final her 6. basamaktır, kaynağın finali değil (5 sayılık bir maça "final" yazmak yanıltırdı).
        public static string Yer(string yer)
        {
            foreach (var ek in new[] { " çeyrek finali", " yarı finali", " finali" }) if (yer.EndsWith(ek)) return yer.Substring(0, yer.Length - ek.Length);
            int i = yer.IndexOf(" grup "); return i > 0 ? yer.Substring(0, i) : yer;
        }

        // Rakip: tarzın kopyası (kariyer basamağının değil: Usta gücü kendi g'si), veriden ek tepki/hücum (şimdi 1), final patron.
        public static Style Rakip(int n)
        {
            var b = Basamak(n); var u = KariyerVeri.Usta; var s = KariyerVeri.Stil[b.stil].Clone();
            s.hiz *= b.g; s.hata /= b.g; s.tepki *= u.tepki; s.hucum *= u.hucum; s.kariyer = -1; s.usta = n;
            s.patron = b.final && s.imza != null; if (!s.patron) s.imza = null; return s;
        }

        // Basamağın görevi: kaynak kariyer basamağınınki (rakibe göre seçilmişti); imza yoksa "imzayı oku" yerine "5 tam orta".
        public static string GorevId(int n)
        {
            var b = Basamak(n); var k = KariyerVeri.Al(b.kaynak); string id = k != null ? k.gorev : null;
            if (id == "okudun" && !(b.final && KariyerVeri.Stil[b.stil].imza != null)) id = "perf";
            return id ?? "perf";
        }

        // "Kopenhag · Usta 7" gibi; çeviri Lang (yer adı + " · Usta {0}").
        public static string Ad(int n) { return Basamak(n).yer + " · Usta " + (n + 1); }
    }
}
