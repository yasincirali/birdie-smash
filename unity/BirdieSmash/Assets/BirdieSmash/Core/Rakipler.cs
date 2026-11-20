// Her rakip ayrı biri (2026-11-20, yasin: "rakipler de random tiplerden oluşmalı, özellikleri de değişmeli. Karşılaşılan her
// rakip ayrı isme ve özelliklere sahip olmalı ve tabiki finale yaklaştıkça daha zor rakipler olmalı").
// Önceden kariyerin 48 basamağında altı adlı rakip dönüp duruyordu (Blaze sekiz kez). Şimdi final dışı basamaklar (tur 1–7'nin
// 1.–5. basamağı, 35 maç) ve Usta yolunun final dışı basamakları üretilmiş, adı tekil bir rakiple oynanır:
//  - Tarz ve güç basamağın verisinden: zorluk merdiveni (g, tepki, hücum; Denge'yle ölçülmüş) dokunulmadan kalır, yani finale
//    yaklaştıkça rakip yine zorlaşır. Görünen güç sayısı (Hikaye.Guc) da merdivenden, özellikten değil (sayı tur tur artar).
//  - Kimlik oyuncunun tohumundan (tuytop_rakip_tohum, ilk açılışta rastgele; iCloud'la taşınır): her oyuncunun dünyası farklı,
//    ama aynı basamağın rakibi tekrar oynayınca aynı kişi (rövanş anlamlı kalır; maç kartı, portre, saha aynı figürü verir).
//  - Model: çoğu Oyuncak 3B bedeni (tip, saç, saç rengi, ten, bazen şapka), dörtte biri katalogdaki maskot modellerden biri
//    (kedi, tavşan, panda, robot ...; karakter.json modeller'e eklenen yeni model kendiliğinden havuza girer).
//  - Ad: bedene uygun listeden (stil.json adlar), karıştırılmış sırayla; kariyer boyunca aynı ad iki kez gelmez.
//  - Özellik: stil.json ozellikler'den biri; bir artı bir eksi ("Sabırlı: az hata yapar, az smaç atar"). Denge 'ozellik'
//    kipi her birinin galibiyet oranını en çok ±4 puan oynattığını ölçer: kişilik verir, merdiveni bozmaz.
// Finaller ve adli tur (Efsaneler Kupası) altı adlı rakiple kalır: hikâye, imza vuruşu ve rövanş lafı onların.
// Hızlı maç ve Günün maçı altı adlı rakibi kullanır (seçici çiplerde ad, karne). Saf C#: BotMac Rakiplers() kilitler.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class Ozellik { public string id, ad, n; public float hiz = 1, hata = 1, hucum = 1, tepki = 1, yorK = 1, aldatP, uyum; }

    public class RakipKimlik { public string isim, model, kat; public Ozellik oz; public RakipGorunum gor; }

    public static class Rakipler
    {
        public const string ANAHTAR = "tuytop_rakip_tohum";
        public const float MASKOT_P = .25f, SAPKA_P = .33f;
        // Oyuncunun tohumu (Game/Ayar yükler). 0 = testlerin sabit dünyası.
        static int tohum;
        public static int Tohum { get { return tohum; } set { if (value != tohum) { tohum = value; onbellek.Clear(); karisik = null; } } }

        static Ozellik[] oz; static Dictionary<string, string[]> adlar; static Dictionary<string, string[]> karisik;
        static readonly Dictionary<int, RakipKimlik> onbellek = new Dictionary<int, RakipKimlik>();
        public static void Sifirla() { oz = null; adlar = null; karisik = null; onbellek.Clear(); }

        public static Ozellik[] Ozellikler { get { Yukle(); return oz; } }

        // Kariyer basamağı üretilmiş rakiple mi oynanır: final değil ve turu adli değil.
        public static bool Uretilir(int g)
        {
            if (!KariyerVeri.Var(g) || KariyerVeri.Final(g)) return false;
            return !KariyerVeri.Turlar[KariyerVeri.TurOf(g)].adli;
        }
        // Üretilmiş basamaklar sırayla 0, 1, 2 ... (ad sırası bu sırayla dağılır: kariyer boyunca tekil).
        public static int Anahtar(int g)
        {
            int k = 0; for (int i = 0; i < g; i++) if (Uretilir(i)) k++;
            return k;
        }
        public static int UretilenSayi { get { int k = 0; for (int i = 0; i < KariyerVeri.TUR * KariyerVeri.BASAMAK; i++) if (Uretilir(i)) k++; return k; } }
        // Usta yolu: kariyerin ardından sürer (ad listesi döner; aynı ad en erken liste boyu kadar maç sonra gelir).
        public static int UstaAnahtar(int n) { return UretilenSayi + Math.Max(0, n); }

        // Taban rakibin (tarzın merdivenle ölçeklenmiş kopyası) üstüne kimliği giydirir.
        public static void Giydir(Style s, int anahtar)
        {
            var k = Kimlik(anahtar); var o = k.oz;
            s.isim = k.isim; s.uretilen = true; s.model = k.model; s.gor = k.gor; s.ozellik = o.id; s.ozellikAd = o.ad; s.ozellikN = o.n;
            s.hiz *= o.hiz; s.hata *= o.hata; s.hucum *= o.hucum; s.tepki *= o.tepki; s.yorK *= o.yorK; s.aldatP += o.aldatP; s.uyum = Math.Min(.3f, s.uyum + o.uyum);
        }

        public static RakipKimlik Kimlik(int anahtar)
        {
            RakipKimlik k; if (onbellek.TryGetValue(anahtar, out k)) return k;
            Yukle(); if (karisik == null) Karistir();
            // Ad: aynı bedendeki önceki üretilmişlerin sayısı kadar ileri (tekil). Önceki anahtarların bedeni ucuz (yalnız kura).
            var ham = Ham(anahtar); int sira = 0;
            for (int j = 0; j < anahtar; j++) if (Ham(j).kat == ham.kat) sira++;
            var liste = karisik[ham.kat]; ham.isim = liste[sira % liste.Length];
            onbellek[anahtar] = ham; return ham;
        }

        // Bir anahtarın kurası (ad hariç): yalnız tohum ve anahtardan, sıradan bağımsız.
        static RakipKimlik Ham(int anahtar)
        {
            var r = new Kura(Karis(tohum, anahtar * 7919 + 17));
            var k = new RakipKimlik { oz = oz[r.Sayi(oz.Length)] };
            var M = Kozmetik.Liste(Kozmetik.MODEL); var maskot = new List<string>();
            foreach (var m in M) if (m.id != "oyuncak") maskot.Add(m.id);
            bool mk = maskot.Count > 0 && r.Ondalik() < MASKOT_P; string mModel = mk ? maskot[r.Sayi(maskot.Count)] : null;
            var T = Kozmetik.Liste(Kozmetik.TIP); var S = Kozmetik.Liste(Kozmetik.SAC); var P = Kozmetik.Liste(Kozmetik.SAPKA);
            var SR = Karakter.Gorunum("sac"); var TN = Karakter.Gorunum("ten");
            var g = new RakipGorunum();
            g.tip = T.Length > 0 ? T[r.Sayi(T.Length)].id : null;
            g.sac = S.Length > 0 ? S[r.Sayi(S.Length)].id : null;
            g.sacRenk = SR.Length > 1 ? SR[1 + r.Sayi(SR.Length - 1)].id : null;   // 0 = "modelin rengi"
            g.ten = TN.Length > 1 ? TN[1 + r.Sayi(TN.Length - 1)].id : null;
            if (P.Length > 1 && r.Ondalik() < SAPKA_P) g.sapka = P[1 + r.Sayi(P.Length - 1)].id;   // 0 = "yok"
            k.gor = g; k.model = mModel;
            k.kat = mk ? "maskot" : g.tip == "kadin" ? "kadin" : "erkek";
            if (!adlar.ContainsKey(k.kat) || adlar[k.kat].Length == 0) k.kat = "erkek";
            return k;
        }

        static void Karistir()
        {
            karisik = new Dictionary<string, string[]>(); int i = 0;
            foreach (var kv in adlar)
            {
                var a = (string[])kv.Value.Clone(); var r = new Kura(Karis(tohum, 1000003 + i++ * 31));
                for (int n = a.Length - 1; n > 0; n--) { int j = r.Sayi(n + 1); var t = a[n]; a[n] = a[j]; a[j] = t; }
                karisik[kv.Key] = a;
            }
        }

        static void Yukle()
        {
            if (oz != null) return;
            var so = Json.Obj(Json.Parse(KariyerVeri.Metin("stil")));
            var ol = new List<Ozellik>(); object x;
            if (so != null && so.TryGetValue("ozellikler", out x))
                foreach (var e in (List<object>)x)
                {
                    var d = Json.Obj(e);
                    ol.Add(new Ozellik { id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), n = Json.Str(d, "n", ""), hiz = F(d, "hiz", 1), hata = F(d, "hata", 1), hucum = F(d, "hucum", 1),
                        tepki = F(d, "tepki", 1), yorK = F(d, "yorK", 1), aldatP = F(d, "aldatP", 0), uyum = F(d, "uyum", 0) });
                }
            if (ol.Count == 0) ol.Add(new Ozellik { id = "dengeli", ad = "Dengeli", n = "" });
            var ad = new Dictionary<string, string[]>();
            if (so != null && so.TryGetValue("adlar", out x))
                foreach (var kv in Json.Obj(x)) { var l = new List<string>(); foreach (var e in (List<object>)kv.Value) l.Add((string)e); ad[kv.Key] = l.ToArray(); }
            if (!ad.ContainsKey("erkek")) ad["erkek"] = new[] { "Rakip" };
            oz = ol.ToArray(); adlar = ad;
        }
        static float F(Dictionary<string, object> d, string k, float v) { return d.ContainsKey(k) ? (float)Json.Num(d, k, v) : v; }

        // Platformdan bağımsız kura (System.Random'ın algoritması çalışma zamanına göre değişebilir; IL2CPP ile mono aynı adı
        // vermeli). xorshift32.
        static int Karis(int a, int b) { unchecked { uint h = (uint)a * 2654435761u ^ (uint)b * 2246822519u; h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; return (int)(h == 0 ? 1 : h); } }
        class Kura
        {
            uint s; public Kura(int t) { s = (uint)t; if (s == 0) s = 1; }
            uint Sonraki() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
            public int Sayi(int n) { return n <= 1 ? 0 : (int)(Sonraki() % (uint)n); }
            public float Ondalik() { return (Sonraki() & 0xffffff) / 16777216f; }
        }
    }
}
