// Karakter: oyun tarzı, unvanlar ve görünüm öğeleri (komple-oyun tasarımı §5; adım 6, 2026-10-26). Veri veri/karakter.json.
// İlke (§5.1): güç ekipmandan, tarz karakterden, görünüm ikisinden. Karakter ikinci bir güç kaynağı olmaz; yasin
// varsayılanı kabul etti: puan dağıtılan yetenek ağacı yok, karakter = tarz. Gerekçe: ekonomi tavanı (%15–18) ekipmana göre
// ayarlandı; ikinci bir katman ya tavanı deler ya da iki katmanı da önemsizleştirir.
// 2026-11-07 (yasin "tarz farkı oyunda hissedilsin"): ±%4–10'luk etkiler kortta fark edilmiyordu (Denge tarz kipi: tarzlar
// arası galibiyet farkı gürültü düzeyinde, inceleme 08:30 #3). Tavan tek durumda %35'e çıktı, her tarzın bir imza etkisi var
// (Smaşçı smaç hızı, Duvar erişim, File ustası file önü, Aldatıcı slice) ve eksisi aynı ölçüde gerçek. Hissedilme ölçüsü
// Match.tarzFark (tarzsız başka olacak an); ilk 3'ü kortta tarz adıyla yazılır. Denge kuralı aynı: tarzlar arası fark ≤ 5 puan.
// Tarz: artısı ve eksisi olan bir yan seçim (≤ %35 tek bir durumda, genel güç etkisi ≈ 0), ücretsiz, maç dışında her
// an değişir, Eşit şartlarda da geçerlidir (güç değil kimlik). GearStats'ın üstüne çarpan olarak biner (Uygula); ekipmanın
// 5 çubuğuna girmez. Seviyesi gelmemiş tarz kayıtta kalsa da uygulanmaz (Dengeli sayılır), seviye gelince kendiliğinden döner.
// Unvan: kariyerden, antrenmandan, seviyeden; kazanılan unvan bir daha geri alınmaz (koşulu kalıcı ilerlemeye bağlı).
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class TarzBilgi { public string id, ad, oz, arti, eksi; public int sv = 1; public Dictionary<string, float> etki = new Dictionary<string, float>(); }
    // sayac/esik (2026-11-07 inceleme, fikir 3 "beceri unvanları"): oynayıştan kazanılan unvan; Ilerleme.Sayac(sayac) ≥ esik.
    // İlerleme unvanları kariyerde ne kadar ilerlediğini söyler, bunlar nasıl oynadığını (online kartında rakip okur). Ödülsüz.
    // kademe (2026-11-07 inceleme): beceri unvanının üç eşiği (I · II · III); ödülsüz, yalnız kimlik. Yoksa tek eşik {esik}.
    public class UnvanBilgi { public string id, ad, nasil, sayac; public int tur = -1, sv, antrenman, esik; public int[] kademe; }
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
            if (u.sayac != null) return Kademe(u, p) >= 1;
            if (u.tur >= 0) return u.tur < p.tur.Length && p.TurBitti(u.tur);
            if (u.sv > 0) return seviye >= u.sv;
            if (u.antrenman > 0) return antrenmanYildiz >= u.antrenman;
            return true;
        }
        // Bu maçla açılan ilk beceri unvanı (maç sonu kartında tek satır). once = maçtan önce kazanılmış beceri unvanı kimlikleri.
        // Kademe 2 ve 3 listeye "id:2" / "id:3" olarak ayrıca girer; kademe 1 eski kimlikle kalır (kayıt ve kart değişmez).
        public static List<string> BeceriKazanilan(Ilerleme p)
        {
            var l = new List<string>();
            foreach (var u in Unvanlar) { int k = Kademe(u, p); if (k >= 1) l.Add(u.id); for (int i = 2; i <= k; i++) l.Add(u.id + ":" + i); }
            return l;
        }
        // Yeni unvan ya da yeni kademe: maç sonu alt notu "Yeni unvan: Aldatıcı II." (ad + KademeEki aynı p ile).
        public static UnvanBilgi YeniBeceri(Ilerleme p, List<string> once)
        {
            foreach (var u in Unvanlar) { int k = Kademe(u, p); if (k >= 1 && !once.Contains(k == 1 ? u.id : u.id + ":" + k)) return u; }
            return null;
        }
        // Beceri unvanının kademesi (0: kazanılmadı). Sayaçsız unvanın kademesi yok (0); kazanılması Kazanildi'de.
        public static int Kademe(UnvanBilgi u, Ilerleme p)
        {
            if (u.sayac == null || u.esik <= 0) return 0;
            int n = p.Sayac(u.sayac), k = 0; var e = u.kademe ?? new[] { u.esik };
            foreach (var x in e) if (n >= x) k++;
            return k;
        }
        // Görünen ek: kademe 1 eksiz ("Aldatıcı"), 2 " II", 3 " III". Roma rakamı renkten bağımsız okunur (HIG renk körlüğü).
        public static string KademeEki(UnvanBilgi u, Ilerleme p) { int k = Kademe(u, p); return k == 2 ? " II" : k == 3 ? " III" : ""; }
        // Sıradaki kademe eşiği (yoksa 0): Profil'de tek satır "sonraki kademe 212/400".
        public static int SonrakiEsik(UnvanBilgi u, Ilerleme p)
        {
            int k = Kademe(u, p); if (k < 1 || u.kademe == null || k >= u.kademe.Length) return 0; return u.kademe[k];
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
        // Seviye anı grupları (2026-11-07 stüdyo incelemesi UX1/GD4): ham ad listesi "Head-heavy, Bun, Multifilament" raket parçasını
        // ve kozmetiği karıştırıyordu, tek düğme Dolap'ı açıyordu (raket parçası Atölye'de). Her öğe {grup, ad, yuva}: grup
        // "Dolap" (görünüm, ayakkabı, renkler, raket kaplaması: Dolap'ın raket görünümünde), "Atolye" (raket parçası, tarz),
        // "Diger" (unvan, aldatma vuruşu). yuva yalnız raket parçasında (Atölye'de o yuva açılır). Sıra AcilanAralik ile aynı.
        // Satır: { grup, ad, Atölye yuvası, denenebilir "kat|id" }. 4. alan (inceleme 10 Kas 19:49 #5): seviye anındaki "Dolap'ta dene"
        // eşyayı gerçekten vitrinde denesin diye; yalnız vitrinin DeneYaz ile giydirebildiği türler (ayakkabı, tip, saç modeli), gerisi null.
        public static List<string[]> AcilanAralikGruplu(int once, int sonra)
        {
            var l = new List<string[]>();
            for (int sv = Math.Max(2, once + 1); sv <= sonra; sv++)
            {
                foreach (var y in Gear.MODEL_YUVA) foreach (var m in Gear.GEAR[y].Values) if (m.sv == sv) l.Add(new[] { y == "ayakkabi" ? "Dolap" : "Atolye", m.ad, y == "ayakkabi" ? null : y, y == "ayakkabi" ? "ayakkabi|" + m.id : null });
                foreach (var t in Tarzlar) if (t.sv == sv) l.Add(new[] { "Atolye", t.ad, null });
                foreach (var o in Kozmetik.Acilan(Kozmetik.TIP, sv)) l.Add(new[] { "Dolap", o.ad, null, Kozmetik.TIP + "|" + o.id });
                foreach (var o in Kozmetik.Acilan(Kozmetik.SAC, sv)) l.Add(new[] { "Dolap", o.ad, null, Kozmetik.SAC + "|" + o.id });
                foreach (var m in Gear.RENK.Values) if (m.sv == sv && !m.kilit) l.Add(new[] { "Dolap", m.ad, null });
                foreach (var k in GORUNUM_KAT) foreach (var o in Gorunum(k)) if (o.sv == sv) l.Add(new[] { "Dolap", o.ad, null });
                foreach (var u in Unvanlar) if (u.sv == sv) l.Add(new[] { "Diger", u.ad, null });
                foreach (var t in Aldatma.Liste) if (t.sv == sv) l.Add(new[] { "Diger", t.ad, null });
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
                var d = Json.Obj(x); var t = new TarzBilgi { id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), oz = Json.Str(d, "oz", ""), arti = Json.Str(d, "arti", ""), eksi = Json.Str(d, "eksi", ""), sv = (int)Json.Num(d, "sv", 1) };
                var e = Json.Obj(d.ContainsKey("etki") ? d["etki"] : null); if (e != null) foreach (var kv in e) if (kv.Value is double) t.etki[kv.Key] = (float)(double)kv.Value;
                tl.Add(t);
            }
            var ul = new List<UnvanBilgi>();
            foreach (var x in (List<object>)o["unvanlar"])
            {
                var d = Json.Obj(x); ul.Add(new UnvanBilgi { id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), nasil = Json.Str(d, "nasil", ""), tur = (int)Json.Num(d, "tur", -1), sv = (int)Json.Num(d, "sv", 0), antrenman = (int)Json.Num(d, "antrenman", 0), sayac = Json.Str(d, "sayac", null), esik = (int)Json.Num(d, "esik", 0) });
                var ke = d.ContainsKey("kademe") ? d["kademe"] as List<object> : null; var ub = ul[ul.Count - 1];
                if (ke != null) { ub.kademe = new int[ke.Count]; for (int i = 0; i < ke.Count; i++) ub.kademe[i] = (int)(double)ke[i]; if (ub.kademe[0] != ub.esik) throw new InvalidOperationException("Birdie Smash: karakter.json " + ub.id + " kademe[0] esik olmalı"); }
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
