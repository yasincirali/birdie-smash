// Karakter: oyun tarzı, unvanlar ve görünüm öğeleri (komple-oyun tasarımı §5; adım 6, 2026-10-26). Veri veri/karakter.json.
// İlke (§5.1): güç ekipmandan, tarz karakterden, görünüm ikisinden. Karakter ikinci bir güç kaynağı olmaz; yasin
// varsayılanı kabul etti: puan dağıtılan yetenek ağacı yok, karakter = tarz. Gerekçe: ekonomi tavanı (%15–18) ekipmana göre
// ayarlandı; ikinci bir katman ya tavanı deler ya da iki katmanı da önemsizleştirir.
// Tarz: artısı ve eksisi olan küçük bir yan seçim (≤ %20 tek bir durumda, genel güç etkisi ≈ 0), ücretsiz, maç dışında her
// an değişir, Eşit şartlarda da geçerlidir (güç değil kimlik). GearStats'ın üstüne çarpan olarak biner (Uygula); ekipmanın
// 5 çubuğuna girmez. Seviyesi gelmemiş tarz kayıtta kalsa da uygulanmaz (Dengeli sayılır), seviye gelince kendiliğinden döner.
// Unvan: kariyerden, antrenmandan, seviyeden; kazanılan unvan bir daha geri alınmaz (koşulu kalıcı ilerlemeye bağlı).
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class TarzBilgi { public string id, ad, arti, eksi; public int sv = 1; public Dictionary<string, float> etki = new Dictionary<string, float>(); }
    public class UnvanBilgi { public string id, ad, nasil; public int tur = -1, sv, antrenman; }
    // Görünüm öğesi: renk null = modelin kendi rengi.
    // parla (2026-11-01 mistik saç boyaları): renk hafifçe kendinden ışır (Alev, Buz, Yıldız tozu); yalnız görünüş.
    public class GorunumOge { public string id, ad, renk; public int sv = 1; public bool parla; }

    public static class Karakter
    {
        static TarzBilgi[] tarzlar; static UnvanBilgi[] unvanlar; static Dictionary<string, GorunumOge[]> gorunum;
        public static readonly string[] GORUNUM_KAT = { "ten", "sac", "ayakkabi", "kaplama", "sort" };   // sort: 2026-10-27 şort rengi
        public static TarzBilgi[] Tarzlar { get { Yukle(); return tarzlar; } }
        public static UnvanBilgi[] Unvanlar { get { Yukle(); return unvanlar; } }
        public static GorunumOge[] Gorunum(string kat) { Yukle(); GorunumOge[] a; return gorunum.TryGetValue(kat, out a) ? a : new GorunumOge[0]; }
        public static void Sifirla() { tarzlar = null; unvanlar = null; gorunum = null; Kozmetik.Sifirla(); }

        public static TarzBilgi Tarz(string id) { foreach (var t in Tarzlar) if (t.id == id) return t; return Tarzlar[0]; }
        public static GorunumOge Oge(string kat, string id) { foreach (var o in Gorunum(kat)) if (o.id == id) return o; var a = Gorunum(kat); return a.Length > 0 ? a[0] : null; }

        // Oyuncunun seviyesinde geçerli tarz (kayıtta olan ama seviyesi gelmemiş tarz Dengeli sayılır).
        public static string Gecerli(string id, int seviye) { var t = Tarz(id); return t.sv <= seviye ? t.id : Tarzlar[0].id; }

        // Tarzın değiştiricisi ekipmanın üstüne: kopya döner (ekipman sonucu ve çubuklar değişmez).
        public static GearStats Uygula(GearStats g, string id)
        {
            var t = Tarz(id); var o = g.Kopya(); o.tarz = t.id;
            foreach (var kv in t.etki)
            {
                float v = kv.Value;
                switch (kv.Key)
                {
                    case "enerji": o.enerji *= v; break;
                    case "toparla": o.toparla *= v; break;
                    case "tatli": o.tatli *= v; break;
                    case "slice": o.slice *= v; break;
                    case "jumpTatli": o.jumpTatli *= v; break;
                    case "fileTatli": o.fileTatli *= v; break;
                    case "smacYor": o.smacYor *= v; break;
                    case "smacHiz": o.smacHiz *= v; break;
                    case "uzanma": o.uzanma *= v; break;
                    case "arkaKosu": o.arkaKosu *= v; break;
                    default: throw new InvalidOperationException("Birdie Smash: karakter.json bilinmeyen tarz etkisi " + kv.Key);   // yazım hatası sessiz kalmasın
                }
            }
            return o;
        }

        // Kazanılan unvanlar (sıra: dosyadaki sıra; başlangıç unvanı her zaman var).
        public static List<UnvanBilgi> Kazanilan(Ilerleme p, int seviye, int antrenmanYildiz)
        {
            var l = new List<UnvanBilgi>();
            foreach (var u in Unvanlar) if (Kazanildi(u, p, seviye, antrenmanYildiz)) l.Add(u);
            return l;
        }
        public static bool Kazanildi(UnvanBilgi u, Ilerleme p, int seviye, int antrenmanYildiz)
        {
            if (u.tur >= 0) return u.tur < p.tur.Length && p.TurBitti(u.tur);
            if (u.sv > 0) return seviye >= u.sv;
            if (u.antrenman > 0) return antrenmanYildiz >= u.antrenman;
            return true;
        }
        // Gösterilen unvan: oyuncunun seçtiği (hâlâ kazanılmışsa), seçmediyse kazanılanların en sonuncusu (dosyada en değerli
        // en altta): oyuncu hiç dokunmadan da ilerlemesini görür.
        public static UnvanBilgi Gosterilen(Ilerleme p, int seviye, int antrenmanYildiz)
        {
            var l = Kazanilan(p, seviye, antrenmanYildiz); string s = p.Unvan;
            if (s != null) foreach (var u in l) if (u.id == s) return u;
            return l[l.Count - 1];
        }

        // Seviye açılış tablosu (tasarım §5.2): bu seviyede açılan model, tarz ve görünüm adları (Profil "sıradaki açılış",
        // tempo simülasyonu). Türkçe ad; ekranda L.T ile çevrilir.
        // Profil "Sıradaki açılış" satırı için (2026-10-26 r11): renk adları tek başına belirsizdi ("Pamuk, Kırmızı, Kırmızı" =
        // forma rengi ve raket kaplaması); renk ve görünüm öğeleri türüyle birlikte döner: {tür etiketi ya da null, ad}.
        public static List<string[]> AcilanlarTurlu(int sv)
        {
            var l = new List<string[]>(); if (sv <= 1) return l;
            // 2026-10-26 karakter tasarımı: ayakkabı modeli "Ayakkabı", rengi "Ayakkabı rengi" (iki ayrı şey artık), tip ve saç biçimi.
            foreach (var y in Gear.MODEL_YUVA) foreach (var m in Gear.GEAR[y].Values) if (m.sv == sv) l.Add(new[] { y == "ayakkabi" ? "Ayakkabı" : null, m.ad });
            foreach (var t in Tarzlar) if (t.sv == sv) l.Add(new[] { "Tarz", t.ad });
            foreach (var o in Kozmetik.Acilan(Kozmetik.TIP, sv)) l.Add(new[] { "Tip", o.ad });
            foreach (var o in Kozmetik.Acilan(Kozmetik.SAC, sv)) l.Add(new[] { "Saç modeli", o.ad });
            foreach (var m in Gear.RENK.Values) if (m.sv == sv && !m.kilit) l.Add(new[] { "Forma rengi", m.ad });
            foreach (var k in GORUNUM_KAT) foreach (var o in Gorunum(k)) if (o.sv == sv) l.Add(new[] { k == "ten" ? "Ten tonu" : k == "sac" ? "Saç rengi" : k == "ayakkabi" ? "Ayakkabı rengi" : k == "sort" ? "Şort rengi" : "Raket kaplaması", o.ad });
            foreach (var u in Unvanlar) if (u.sv == sv) l.Add(new[] { "Unvan", u.ad });
            return l;
        }
        // Maç sonu "Seviye atladın" satırı (2026-10-29, yasin "leveller daha anlaşılır olacak"): seviye atlamak önce yalnız
        // "Sv n" yazıyordu, ne açıldığı Profil'de saklıydı. (once, sonra] aralığında açılan her şey, aldatma vuruşları dahil
        // (Aldatma.Liste, seviye kapısı; ekipman şartı ayrıca Aldatma ekranında). Türkçe ad, ekranda L.T.
        public static List<string> AcilanAralik(int once, int sonra)
        {
            var l = new List<string>();
            for (int sv = once + 1; sv <= sonra; sv++)
            {
                foreach (var a in AcilanlarTurlu(sv)) l.Add(a[1]);
                foreach (var t in Aldatma.Liste) if (t.sv == sv) l.Add(t.ad);
            }
            return l;
        }
        public static List<string> Acilanlar(int sv)
        {
            var l = new List<string>();
            foreach (var y in Gear.MODEL_YUVA) foreach (var m in Gear.GEAR[y].Values) if (m.sv == sv && sv > 1) l.Add(m.ad);
            foreach (var t in Tarzlar) if (t.sv == sv && sv > 1) l.Add(t.ad);
            foreach (var k in Kozmetik.KAT) foreach (var o in Kozmetik.Acilan(k, sv)) if (sv > 1) l.Add(o.ad);
            foreach (var m in Gear.RENK.Values) if (m.sv == sv && sv > 1 && !m.kilit) l.Add(m.ad);
            foreach (var k in GORUNUM_KAT) foreach (var o in Gorunum(k)) if (o.sv == sv && sv > 1) l.Add(o.ad);
            foreach (var u in Unvanlar) if (u.sv == sv && sv > 1) l.Add(u.ad);
            return l;
        }

        static void Yukle()
        {
            if (tarzlar != null) return;
            var o = Json.Obj(Json.Parse(KariyerVeri.Metin("karakter"))); if (o == null) throw new InvalidOperationException("Birdie Smash: veri/karakter.json okunamadı");
            var tl = new List<TarzBilgi>();
            foreach (var x in (List<object>)o["tarzlar"])
            {
                var d = Json.Obj(x); var t = new TarzBilgi { id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), arti = Json.Str(d, "arti", ""), eksi = Json.Str(d, "eksi", ""), sv = (int)Json.Num(d, "sv", 1) };
                var e = Json.Obj(d.ContainsKey("etki") ? d["etki"] : null); if (e != null) foreach (var kv in e) if (kv.Value is double) t.etki[kv.Key] = (float)(double)kv.Value;
                tl.Add(t);
            }
            var ul = new List<UnvanBilgi>();
            foreach (var x in (List<object>)o["unvanlar"])
            {
                var d = Json.Obj(x); ul.Add(new UnvanBilgi { id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), nasil = Json.Str(d, "nasil", ""), tur = (int)Json.Num(d, "tur", -1), sv = (int)Json.Num(d, "sv", 0), antrenman = (int)Json.Num(d, "antrenman", 0) });
            }
            var g = new Dictionary<string, GorunumOge[]>();
            foreach (var k in GORUNUM_KAT)
            {
                var l = new List<GorunumOge>(); var a = o.ContainsKey(k) ? o[k] as List<object> : null;
                if (a != null) foreach (var x in a) { var d = Json.Obj(x); l.Add(new GorunumOge { id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), renk = Json.Str(d, "renk", null), sv = (int)Json.Num(d, "sv", 1), parla = Json.Bool(d, "parla", false) }); }
                g[k] = l.ToArray();
            }
            if (tl.Count == 0 || tl[0].etki.Count != 0) throw new InvalidOperationException("Birdie Smash: karakter.json ilk tarz etkisiz (Dengeli) olmalı");
            unvanlar = ul.ToArray(); gorunum = g; tarzlar = tl.ToArray();
        }
    }
}
