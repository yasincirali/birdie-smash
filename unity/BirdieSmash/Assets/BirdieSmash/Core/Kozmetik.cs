// Kozmetik: beden tipi ve saç biçimi (2026-10-26, yasin: "tip seçebilme, garip saçlar falan seçilebilme olmalı ...
// parayla ya da tüyle saç modeli alacak"; "yakın açıda görüp upgrade lenecekleri için"). Veri veri/karakter.json (tipler,
// sac_model), rakibin görünüşü veri/stil.json (stiller.<id>.gorunum).
// İlke (tasarım §5.1): güç ekipmandan, tarz karakterden, görünüm ikisinden. Tip ve saç YALNIZ görünüş: hiçbir GearStats
// alanına dokunmaz (veride etki alanı olamaz; BotMac kilitler). Vuruş menzili ve fizik Core'da beden boyundan bağımsız,
// bu yüzden iri ya da ince beden maçta avantaj değildir.
// Para: bugün yalnız Tüy (kazanılan para). Gerçek para kodu YOK; IAP ancak iCloud kaydından sonra (tasarım §6.2). Her
// öğedeki "paket" alanı ileride kozmetik IAP paketine ayrılmış kimliktir, okunur ama hiçbir yerde kullanılmaz.
// Sahiplik: Gear ile aynı kayıt (tuytop_ekonomi.acik), önek "tip:" / "sac_model:". Neden ayrı anahtar değil: Tüy de orada;
// alım tek EkoYaz ile atomik (Tüy düştü ama öğe yazılmadı durumu olmaz) ve eski build acik listesini aynen geri yazar.
// Fiyatı 0 olan öğe seviye gelince kendiliğinden senin (listeye girmez). Alınan bir daha geri alınmaz, sahip olunanlar
// arasında geçiş ücretsiz (yasin varsayılanı: "tip bir kez alınınca serbest").
// Seçim: Ayar (tuytop_gorunum.tip / sac_model). O an seçili öğe her durumda sahip sayılır (Koru): fiyat ya da seviye sonradan
// değişirse oyuncunun görünüşü elinden alınmaz (Gear'deki "seçili model açık kalır" kuralının aynısı).
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    // bant false: bu saçta kafa bandı gizlenir (saç hacminin içinde kalır). garip: tuhaf/iddialı biçim (fiyatı yüksek tutulur).
    // nadir (2026-11-01 Dolap yenilemesi): siradan / nadir / destansi / mistik; null = sınıfsız (varsayılan "yok" öğeleri).
    // renk: hazır ağın (varlik kaykit) ana rengi yerine (KayKit iskelet paketinde hepsi aynı gül kurusu; mistikler gece rengine).
    // aks (2026-11-14, saç/yüz/ayakkabı rafı): saç biçimi FBX parçası yerine baş çerçevesine takılan hazır ağla gelir
    // (Resources/karakter/aks/<aks>.bytes; varlik o saçın altındaki taban saçı, null = kel). Bkz. Game/Aksesuar.Kur.
    public class KozmetikOge { public string id, ad, n, varlik, paket, nadir, renk, koleksiyon, aks; public int sv = 1, fiyat; public bool bant = true, garip; }

    // Rakibin görünüşü (stil.json gorunum). Boş alan = varsayılan (ilk öğe / modelin rengi).
    public class RakipGorunum { public string tip, sac, sacRenk, ten, ayakkabi, aykRenk, sapka; }

    public static class Kozmetik
    {
        // 2026-10-30 Soyunma › Görünüm: şapka ve yüz aksesuarı (yalnız görünüş, aynı sahiplik ve fiyat kuralı; ilk öğe "yok").
        public const string TIP = "tip", SAC = "sac_model", SAPKA = "sapka", YUZ = "yuz";
        // 2026-11-01 Sevinç (Ar-Ge K3): sayı sevinci ve maç sonu hareketi; aynı sahiplik/fiyat kuralı, yalnız görünüş.
        public const string SEVINC = "sevinc", MAC_SONU = "mac_sonu";
        // Smaç hareketi (2026-11-10, yasin: "xp ve levele özgü animasyonlar, cool smash tricks yapılabilsin"): sıçrayarak smaçta
        // figürün havadaki duruşu. Yalnız görünüş (Yukle güç alanını reddeder): vuruşun hızı, zamanı, isabeti aynı. Tüy ile
        // satılmaz, yalnız seviyeyle açılır (fiyat 0 + sv): Sahip kuralı zaten "ücretsiz ve seviyesi gelmiş" der.
        public const string SMAC = "smac";
        // 2026-11-01 Sırt (pelerin, omuzluk; KayKit CC0 hazır ağları): aynı sahiplik/fiyat kuralı, yalnız görünüş.
        public const string SIRT = "sirt";
        // 2026-11-12 Kol (yasin: "ayakkabı pelerin şapka, kalkan gibi şeyler satacağız ... oyuna etkisi olmayacak"): raketsiz (sol)
        // ön kola takılan kalkan ve benzerleri; aynı sahiplik/fiyat kuralı, yalnız görünüş. Sona eklendi (kayıt sırası değişmez).
        public const string KOL = "kol";
        // 2026-11-12 Bel (yasin: konsept setleri, "app boyutunu artırmadan"): leğene bağlı etek, kemer, kuyruk, simit. Set, yeni model
        // dosyası yerine aynı gövdeye takılan parçalarla kurulur; bel bunun eksik takma noktasıydı. Sona eklendi (kayıt sırası değişmez).
        public const string BEL = "bel";
        // 2026-11-14 Ayakkabı görünümü (yasin: "ayakkabıda da eksiğimiz var"): ayakkabı bir ekipman (güç Gear'de, 5 model) ve
        // kozmetik tarafı yalnız renkti. Görünüm ekipman ayakkabısının üstüne giyilir (Aksesuar.Ayak gizler ve yerine takar), güç
        // değişmez. Ayrı kategori, çünkü Gear'in ayakkabı modeli satın alınan bir ekipman, bu Tüy ile alınan bir görünüş.
        // Sona eklendi (kayıt sırası değişmez).
        public const string AYAK = "ayak";
        // 2026-11-20 Oyuncu modeli (yasin: "toy 3d dışı ücretli olmalı"): karakter.json modeller. Aynı sahiplik/fiyat kuralı, yalnız
        // görünüş. KAT'a girmez: Dolap sayımı ve deneme akışı kategori sayfalarına göre kurulu; model Beden sayfasının başındaki
        // çiplerden alınır ve seçilir (seçim Ayar.model, kayıt tuytop_model).
        public const string MODEL = "model";
        // Modele taşınan beden tipleri: kedi, tavşan, panda ve robot tam gövdeli model oldu; Yapay Zekâ Böceği tipi kalktı,
        // sahibi Hamamböceği modelini alır. "tip:<id>" sahipliği "model:<id>" olur (ödenen Tüy boşa gitmez); eski kayıt satırı
        // silinmez (eski build aynen geri yazar). Dönüş: seçili tip taşındıysa geçilecek model, yoksa null.
        static readonly string[][] TIP_MODEL = { new[] { "kedi", "kedi" }, new[] { "tavsan", "tavsan" }, new[] { "panda", "panda" }, new[] { "robot", "robot" }, new[] { "bocek", "hamambocegi" } };
        public static string TipModel(Gear g, string seciliTip)
        {
            string hedef = null; bool degisti = false;
            foreach (var t in TIP_MODEL)
            {
                bool secili = seciliTip == t[0]; if (secili) hedef = t[1];
                if ((secili || g.eko.acik.Contains(TIP + ":" + t[0])) && g.eko.acik.Add(MODEL + ":" + t[1])) degisti = true;
            }
            if (degisti) g.EkoYaz();
            return hedef;
        }
        public static readonly string[] KAT = { TIP, SAC, SAPKA, YUZ, SEVINC, MAC_SONU, SIRT, SMAC, KOL, BEL, AYAK };
        static Dictionary<string, KozmetikOge[]> veri; static Dictionary<string, RakipGorunum> rakip;
        static Dictionary<string, KozmetikOge[]> gosterim;
        public static void Sifirla() { veri = null; rakip = null; gosterim = null; }

        public static KozmetikOge[] Liste(string kat) { Yukle(); KozmetikOge[] a; return veri.TryGetValue(kat, out a) ? a : new KozmetikOge[0]; }
        // 2026-11-15 (inceleme turu: paketler dizinin sonuna eklendiği için Dolap rafında Sv 3 saçı Sv 15 kilidinden sonra
        // çıkıyordu): YALNIZ gösterim sırası. Varsayılan (dizinin ilki) başta, sonra seviye, eşitse fiyat, eşitse veri sırası
        // (kararlı). Veri dizisi, kayıt ve "ilk öğe varsayılan" kuralı değişmez; Liste() hâlâ veri sırasını döndürür.
        public static KozmetikOge[] Gosterim(string kat)
        {
            Yukle(); if (gosterim == null) gosterim = new Dictionary<string, KozmetikOge[]>();
            KozmetikOge[] g; if (gosterim.TryGetValue(kat, out g)) return g;
            var l = Liste(kat); var sira = new int[l.Length]; for (int i = 0; i < sira.Length; i++) sira[i] = i;
            System.Array.Sort(sira, (a, b) =>
            {
                if (a == b) return 0; if (a == 0) return -1; if (b == 0) return 1;
                int c = l[a].sv.CompareTo(l[b].sv); if (c != 0) return c;
                c = l[a].fiyat.CompareTo(l[b].fiyat); return c != 0 ? c : a.CompareTo(b);
            });
            g = new KozmetikOge[l.Length]; for (int i = 0; i < g.Length; i++) g[i] = l[sira[i]];
            gosterim[kat] = g; return g;
        }
        public static KozmetikOge Bul(string kat, string id) { foreach (var o in Liste(kat)) if (o.id == id) return o; return null; }
        // Listenin ilki varsayılandır: var olan oyuncu ve hakemler bu görünüşle kalır.
        // Rakibin sevinci (2026-11-07): tarzın seçimi veride varsa o, yoksa kategorinin varsayılanı (yumruk / zafer). Online'da
        // aynı yol karttan gelen seçimle beslenecek (OyuncuKarti.gorunus[SEVINC]); bilinmeyen kimlik hiçbir şey açmaz, varsayılan çizilir.
        public static string RakipSecim(string kat, string secim) { return secim != null && Bul(kat, secim) != null ? secim : Varsayilan(kat); }
        public static string Varsayilan(string kat) { var l = Liste(kat); return l.Length > 0 ? l[0].id : null; }
        public static KozmetikOge Oge(string kat, string id) { return Bul(kat, id) ?? Bul(kat, Varsayilan(kat)); }

        public static bool Sahip(Gear g, string kat, string id)
        {
            var o = Bul(kat, id); if (o == null) return false;
            if (g.eko.acik.Contains(kat + ":" + id) || (o.fiyat == 0 && g.Seviye() >= o.sv)) return true;
            // 2026-11-07 (inceleme turu: "Zıpla ve dön" hem Sevinç hem Maç sonu listesinde satılıyordu, aynı poz iki kez ücret):
            // aynı kimlikli hareket iki listede de varsa birini almak ikisini de açar. Kayıt biçimi aynı (tek "kat:id" satırı).
            string es = kat == SEVINC ? MAC_SONU : kat == MAC_SONU ? SEVINC : null;
            return es != null && Bul(es, id) != null && g.eko.acik.Contains(es + ":" + id);
        }
        public static ModelDurum Durum(Gear g, string kat, string id, string secili)
        {
            if (secili == id) return ModelDurum.Secili;
            if (Sahip(g, kat, id)) return ModelDurum.Acik;
            var o = Bul(kat, id); return o != null && g.Seviye() >= o.sv ? ModelDurum.Alinir : ModelDurum.Kilitli;
        }
        public static bool AlOlur(Gear g, string kat, string id, string secili)
        {
            var o = Bul(kat, id); return o != null && Durum(g, kat, id, secili) == ModelDurum.Alinir && g.eko.tuy >= o.fiyat;
        }
        // Tüy ile alma: seviye kapısı Tüy ile aşılmaz (Gear.Al ile aynı kural). Dönüş: alındı mı.
        public static bool Al(Gear g, string kat, string id, string secili)
        {
            if (!AlOlur(g, kat, id, secili)) return false; var o = Bul(kat, id);
            g.eko.tuy -= o.fiyat; if (o.fiyat > 0) g.eko.acik.Add(kat + ":" + id); g.EkoYaz(); return true;
        }
        // Kayıttaki seçim: bilinmeyen kimlik varsayılana düşer; bilinen kimlik sahip sayılır (fiyat/seviye sonradan değişse de).
        public static string Koru(Gear g, string kat, string id)
        {
            var o = Bul(kat, id); if (o == null) return Varsayilan(kat);
            if (o.fiyat > 0 && !g.eko.acik.Contains(kat + ":" + id)) { g.eko.acik.Add(kat + ":" + id); g.EkoYaz(); }
            return id;
        }
        // Koleksiyon sayımı (2026-11-10, yasin: "dolap kısmı göz alıcı ve orada zaman geçirtici değil"): kategori çipinde "7/19",
        // Dolap'ın başında toplam. Sevinç ile Maç sonu aynı kimliği paylaşabildiğinden (biri alınınca ikisi açılır, bkz. Sahip)
        // Maç sonu'nda yalnız Sevinç'te olmayanlar sayılır; aynı hareket iki kez sayılmaz. Kayıt okunur, yeni anahtar yok.
        public static void Sayim(Gear g, string kat, out int sahip, out int toplam)
        {
            sahip = 0; toplam = 0;
            foreach (var o in Liste(kat))
            {
                if (kat == MAC_SONU && Bul(SEVINC, o.id) != null) continue;
                toplam++; if (Sahip(g, kat, o.id)) sahip++;
            }
        }
        // Sıradaki eşya: oyuncuya Dolap'ta tek bir "buna yakınsın" göstermek için. Önce seviyesi yeten ve en ucuz olan (Tüy
        // yetmese de: ne kadar kaldığını çağıran yazar), hiçbiri yoksa en yakın seviyede açılacak olan. Rastgelelik yok:
        // eşit durumda veri sırası kazanır, aynı kayıtta hep aynı eşya çıkar. Hepsi alınmışsa null.
        public static KozmetikOge Siradaki(Gear g, string[] katlar, out string kat)
        {
            kat = null; KozmetikOge en = null; bool enAlinir = false; int sv = g.Seviye();
            foreach (var k in katlar) foreach (var o in Liste(k))
            {
                if (Sahip(g, k, o.id) || (k == MAC_SONU && Bul(SEVINC, o.id) != null)) continue;
                bool al = sv >= o.sv;
                bool iyi = en == null || (al && !enAlinir) || (al == enAlinir && (al ? o.fiyat < en.fiyat : o.sv < en.sv || (o.sv == en.sv && o.fiyat < en.fiyat)));
                if (iyi) { en = o; kat = k; enAlinir = al; }
            }
            return en;
        }
        // Seviye gelince açılanlar (Profil "Sıradaki açılış").
        public static List<KozmetikOge> Acilan(string kat, int sv) { var l = new List<KozmetikOge>(); foreach (var o in Liste(kat)) if (o.sv == sv) l.Add(o); return l; }

        // Rakip çeşitliliği (2026-11-15, yasin: "Rakip çeşitliliği de otomasyonda olmalı ve oyun karakterleri ve tipleri değişmeli
        // sürekli"): tur = oynanan maç sayısı. Rakibin tipi her maçta katalogdaki sıradaki tipe geçer (insan bedenleri, hayvanlar,
        // robot; yeni tip karakter.json'a girince kendiliğinden havuza girer, kod değişmez). Üç maçta bir de şapka takar. Saç, ten
        // ve ayakkabı rakibin kimliğinden kalır (insan bedeninde tanınır). Rastgele değil belirlenimli: maç kartındaki portre,
        // sahadaki oyuncu ve Hud aynı turu okur, aynı görünüşü verir; ardışık iki maçta tip hiç aynı olmaz (N > 1).
        public static RakipGorunum RakipTur(string stilId, int tur)
        {
            var r = Rakip(stilId); if (r == null) return null; var T = Liste(TIP); if (T.Length < 2) return r;
            int h = 0; foreach (char c in stilId) h = (h * 31 + c) & 0x7fffff; if (tur < 0) tur = 0;
            int b = 0; for (int i = 0; i < T.Length; i++) if (T[i].id == r.tip) b = i;
            var y = new RakipGorunum { tip = T[(b + tur + h % T.Length) % T.Length].id, sac = r.sac, sacRenk = r.sacRenk, ten = r.ten, ayakkabi = r.ayakkabi, aykRenk = r.aykRenk };
            var S = Liste(SAPKA); if (S.Length > 0 && (h + tur) % 3 == 0) y.sapka = S[(h / 7 + tur * 5) % S.Length].id;
            return y;
        }
        public static RakipGorunum Rakip(string stilId) { Yukle(); RakipGorunum r; return stilId != null && rakip.TryGetValue(stilId, out r) ? r : null; }

        static void Yukle()
        {
            if (veri != null) return;
            var o = Json.Obj(Json.Parse(KariyerVeri.Metin("karakter"))); if (o == null) throw new InvalidOperationException("Birdie Smash: veri/karakter.json okunamadı");
            var v = new Dictionary<string, KozmetikOge[]>();
            foreach (var k in new[] { "tipler", SAC, SAPKA, YUZ, SEVINC, MAC_SONU, SIRT, SMAC, KOL, BEL, AYAK, "modeller" })
            {
                var l = new List<KozmetikOge>(); var a = o.ContainsKey(k) ? o[k] as List<object> : null;
                if (a != null) foreach (var x in a)
                    {
                        var d = Json.Obj(x);
                        // Yalnız görünüş: bir etki/güç alanı yazılırsa sessiz kalmasın (güç satılmaz, tasarım §4.3).
                        foreach (var yasak in new[] { "etki", "guc", "kontrol", "hiz", "tatli", "kosu", "enerji", "toparla", "yor", "uzanma", "jumpTatli" })
                            if (d.ContainsKey(yasak)) throw new InvalidOperationException("Birdie Smash: karakter.json " + k + " öğesinde güç alanı: " + yasak);
                        l.Add(new KozmetikOge { id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), n = Json.Str(d, "n", ""), varlik = Json.Str(d, "varlik", null), paket = Json.Str(d, "paket", null), nadir = Json.Str(d, "nadir", null), renk = Json.Str(d, "renk", null), koleksiyon = Json.Str(d, "set", null), aks = Json.Str(d, "aks", null),
                            sv = (int)Json.Num(d, "sv", 1), fiyat = (int)Json.Num(d, "fiyat", 0), bant = Json.Bool(d, "bant", true), garip = Json.Bool(d, "garip", false) });
                    }
                if (l.Count == 0 || l[0].fiyat != 0 || l[0].sv != 1) throw new InvalidOperationException("Birdie Smash: karakter.json " + k + " ilk öğesi ücretsiz ve Sv 1 olmalı (varsayılan)");
                v[k == "tipler" ? TIP : k == "modeller" ? MODEL : k] = l.ToArray();
            }
            var r = new Dictionary<string, RakipGorunum>();
            var so = Json.Obj(Json.Parse(KariyerVeri.Metin("stil"))); var ss = so != null && so.ContainsKey("stiller") ? Json.Obj(so["stiller"]) : null;
            if (ss != null) foreach (var kv in ss)
                {
                    var d = Json.Obj(kv.Value); var gv = d != null && d.ContainsKey("gorunum") ? Json.Obj(d["gorunum"]) : null; if (gv == null) continue;
                    r[kv.Key] = new RakipGorunum { tip = Json.Str(gv, "tip", null), sac = Json.Str(gv, "sac", null), sacRenk = Json.Str(gv, "sacRenk", null), ten = Json.Str(gv, "ten", null), ayakkabi = Json.Str(gv, "ayakkabi", null), aykRenk = Json.Str(gv, "aykRenk", null) };
                }
            rakip = r; veri = v;
        }
    }
}
