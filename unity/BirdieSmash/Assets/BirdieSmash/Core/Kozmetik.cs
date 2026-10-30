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
    public class KozmetikOge { public string id, ad, n, varlik, paket; public int sv = 1, fiyat; public bool bant = true, garip; }

    // Rakibin görünüşü (stil.json gorunum). Boş alan = varsayılan (ilk öğe / modelin rengi).
    public class RakipGorunum { public string tip, sac, sacRenk, ten, ayakkabi, aykRenk; }

    public static class Kozmetik
    {
        // 2026-10-30 Soyunma › Görünüm: şapka ve yüz aksesuarı (yalnız görünüş, aynı sahiplik ve fiyat kuralı; ilk öğe "yok").
        public const string TIP = "tip", SAC = "sac_model", SAPKA = "sapka", YUZ = "yuz";
        public static readonly string[] KAT = { TIP, SAC, SAPKA, YUZ };
        static Dictionary<string, KozmetikOge[]> veri; static Dictionary<string, RakipGorunum> rakip;
        public static void Sifirla() { veri = null; rakip = null; }

        public static KozmetikOge[] Liste(string kat) { Yukle(); KozmetikOge[] a; return veri.TryGetValue(kat, out a) ? a : new KozmetikOge[0]; }
        public static KozmetikOge Bul(string kat, string id) { foreach (var o in Liste(kat)) if (o.id == id) return o; return null; }
        // Listenin ilki varsayılandır: var olan oyuncu ve hakemler bu görünüşle kalır.
        public static string Varsayilan(string kat) { var l = Liste(kat); return l.Length > 0 ? l[0].id : null; }
        public static KozmetikOge Oge(string kat, string id) { return Bul(kat, id) ?? Bul(kat, Varsayilan(kat)); }

        public static bool Sahip(Gear g, string kat, string id)
        {
            var o = Bul(kat, id); if (o == null) return false;
            return g.eko.acik.Contains(kat + ":" + id) || (o.fiyat == 0 && g.Seviye() >= o.sv);
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
        // Seviye gelince açılanlar (Profil "Sıradaki açılış").
        public static List<KozmetikOge> Acilan(string kat, int sv) { var l = new List<KozmetikOge>(); foreach (var o in Liste(kat)) if (o.sv == sv) l.Add(o); return l; }

        public static RakipGorunum Rakip(string stilId) { Yukle(); RakipGorunum r; return stilId != null && rakip.TryGetValue(stilId, out r) ? r : null; }

        static void Yukle()
        {
            if (veri != null) return;
            var o = Json.Obj(Json.Parse(KariyerVeri.Metin("karakter"))); if (o == null) throw new InvalidOperationException("Birdie Smash: veri/karakter.json okunamadı");
            var v = new Dictionary<string, KozmetikOge[]>();
            foreach (var k in new[] { "tipler", SAC, SAPKA, YUZ })
            {
                var l = new List<KozmetikOge>(); var a = o.ContainsKey(k) ? o[k] as List<object> : null;
                if (a != null) foreach (var x in a)
                    {
                        var d = Json.Obj(x);
                        // Yalnız görünüş: bir etki/güç alanı yazılırsa sessiz kalmasın (güç satılmaz, tasarım §4.3).
                        foreach (var yasak in new[] { "etki", "guc", "kontrol", "hiz", "tatli", "kosu", "enerji", "toparla", "yor", "uzanma", "jumpTatli" })
                            if (d.ContainsKey(yasak)) throw new InvalidOperationException("Birdie Smash: karakter.json " + k + " öğesinde güç alanı: " + yasak);
                        l.Add(new KozmetikOge { id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), n = Json.Str(d, "n", ""), varlik = Json.Str(d, "varlik", null), paket = Json.Str(d, "paket", null),
                            sv = (int)Json.Num(d, "sv", 1), fiyat = (int)Json.Num(d, "fiyat", 0), bant = Json.Bool(d, "bant", true), garip = Json.Bool(d, "garip", false) });
                    }
                if (l.Count == 0 || l[0].fiyat != 0 || l[0].sv != 1) throw new InvalidOperationException("Birdie Smash: karakter.json " + k + " ilk öğesi ücretsiz ve Sv 1 olmalı (varsayılan)");
                v[k == "tipler" ? TIP : SAC] = l.ToArray();
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
