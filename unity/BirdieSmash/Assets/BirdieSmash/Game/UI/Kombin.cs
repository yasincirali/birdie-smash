// Dolap › Kombinler, Rastgele ve alt satın alma çubuğu (2026-10-31, Ar-Ge "Usta modu ve Dolap" K2 + K4; yasin "ilerle ve
// devam et"). Araştırma (Fortnite dolap ön ayarları, Brawl Stars/Tennis Clash görünüm ekranları): oyuncu görünüşünü bir kez
// kurar, sonra parça parça değil bütün olarak değiştirir; "hepsini al" ve rastgele, deneme-alma döngüsünü kısaltır.
//   · Kombin: üç yuva. Boş yuvaya dokunmak şu anki görünüşü oraya yazar; dolu yuvaya dokunmak onu giyer. Giyilen yuvadan sonra
//     bir şey değiştirilirse "Kaydet" çıkar (yuvayı günceller). Yalnız görünüş: tip, saç, şapka, yüz, renkler, raketin
//     görünüşü, ayakkabının görünüşü. Ayakkabı modeli, kumaş, bant ve bileklik güç taşır (Atölye), kombine girmez.
//     Giyerken sahip olunmayan ya da seviyesi gelmemiş değer atlanır (kayıt bozulsa da kimse almadığı şeyi giymez).
//   · Rastgele: yalnız sahip olunan/açık değerlerden. Beden tipi ve ten bilerek dışarıda (kimlik; araştırma: oyuncu ten ve
//     bedenini rastgeleye bırakmak istemiyor).
//   · Denemeler kategoriler arasında kalır (eskiden kategori değişince siliniyordu); denenen her şey alttaki sabit çubukta
//     toplanır: tekse "<ad> · n Tüy ile al", birden çoksa "Hepsini al · toplam". Kilitli olan toplamın dışında, nedeni yazar.
//     Sayfanın içindeki deneme kartı bilgi ve "Denemeyi bırak" için kalır; satın alma düğmesi tek yerde (alt çubuk).
// Kayıt: tuytop_kombin (JSON dizi, 3 eleman; boş yuva null) + tuytop_kombin_aktif. Yeni anahtar; eski kayda dokunulmaz.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        const string KOMBIN_ANAHTAR = "tuytop_kombin", KOMBIN_AKTIF = "tuytop_kombin_aktif"; const int KOMBIN_SAY = 3;
        static readonly string[] KOMBIN_GOR = { "ten", "sac", "ayakkabi", "kaplama", "sort" };
        static readonly string[] KOMBIN_RGOR = { "grip", "tel", "tel2", "desen", "kafa", "saft" };
        List<Dictionary<string, object>> kombinler; int kombinAktif = -1;
        VisualElement denemeCubuk;

        void KombinYukle()
        {
            if (kombinler != null) return; kombinler = new List<Dictionary<string, object>>();
            var l = Json.Parse(PlayerPrefs.GetString(KOMBIN_ANAHTAR, "")) as List<object>;
            for (int i = 0; i < KOMBIN_SAY; i++) kombinler.Add(l != null && i < l.Count ? Json.Obj(l[i]) : null);
            int a; kombinAktif = int.TryParse(PlayerPrefs.GetString(KOMBIN_AKTIF, "-1"), out a) && a >= 0 && a < KOMBIN_SAY && kombinler[a] != null ? a : -1;
        }
        void KombinYaz() { PlayerPrefs.SetString(KOMBIN_ANAHTAR, Json.Write(kombinler)); PlayerPrefs.SetString(KOMBIN_AKTIF, kombinAktif.ToString()); PlayerPrefs.Save(); }

        // Şu anki görünüşün anlık görüntüsü (yalnız görünüş alanları).
        static Dictionary<string, object> KombinSimdi()
        {
            var d = new Dictionary<string, object>();
            foreach (var k in Kozmetik.KAT) d["koz_" + k] = Ayar.KOZ[k];
            foreach (var k in KOMBIN_GOR) d["gor_" + k] = Ayar.GORUNUM[k];
            foreach (var k in KOMBIN_RGOR) d["rg_" + k] = Ayar.RGOR[k];
            d["ayk_gor"] = Ayar.AYK_GOR; d["renk"] = Ayar.gear.renk; return d;
        }
        static bool KombinAyni(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            if (a == null || b == null) return false;
            foreach (var kv in b) { object v; if (!a.TryGetValue(kv.Key, out v) || !(v is string) || (string)v != (string)kv.Value) return false; }
            return true;
        }
        // Giy: her alan yalnız sahipse / açıksa uygulanır (Kozmetik.Sahip, seviye, Gear.RenkAcik, ayakkabı sahipliği).
        void KombinGiy(Dictionary<string, object> d)
        {
            var gr = Ayar.gear; int sv = gr.Seviye(); string v;
            foreach (var k in Kozmetik.KAT) { v = Json.Str(d, "koz_" + k, null); if (v != null && (Kozmetik.Sahip(gr, k, v) || Ayar.KOZ[k] == v)) Ayar.KOZ[k] = v; }
            foreach (var k in KOMBIN_GOR) { v = Json.Str(d, "gor_" + k, null); var o = v != null ? Karakter.Oge(k, v) : null; if (o != null && o.id == v && o.sv <= sv) Ayar.GORUNUM[k] = v; }
            foreach (var k in KOMBIN_RGOR)
            {
                v = Json.Str(d, "rg_" + k, null); if (v == null) continue;
                bool gecerli = k == "desen" ? v == "tek" || v == "iki" : k == "kafa" ? System.Array.IndexOf(RaketSekil.SIRA, v) >= 0 : k == "saft" ? v == "model" || Vitrin.RAK_RENK.ContainsKey(v) : Vitrin.RAK_RENK.ContainsKey(v);
                if (gecerli) Ayar.RGOR[k] = v;
            }
            v = Json.Str(d, "ayk_gor", null); if (v != null && (v == "taban" || gr.Sahip("ayakkabi", v))) Ayar.AYK_GOR = v;
            v = Json.Str(d, "renk", null); if (v != null && gr.RenkAcik(v, Ayar.sampiyon)) gr.renk = v;
            KombinUygula();
        }
        void KombinUygula()
        {
            DenemeSil(); Ayar.GearHesap(); Ayar.Kaydet(); root.vitrin.Yenile(); kirli[(int)Sekme.Profil] = true; Degisti();
            SoyunmaGovdeKur(true);
        }

        // Rastgele: sahip olunan/açık değerlerden; beden tipi ve ten dışarıda.
        void KombinRastgele()
        {
            var gr = Ayar.gear; int sv = gr.Seviye();
            foreach (var k in Kozmetik.KAT)
            {
                if (k == Kozmetik.TIP || (k == Kozmetik.SAC && !Ayar.VaryantGorunur)) continue;
                var l = new List<string>(); foreach (var o in Kozmetik.Liste(k)) if (Kozmetik.Sahip(gr, k, o.id) || Ayar.KOZ[k] == o.id) l.Add(o.id);
                if (l.Count > 0) Ayar.KOZ[k] = l[Random.Range(0, l.Count)];
            }
            foreach (var k in new[] { "sac", "sort", "kaplama" })
            {
                var l = new List<string>(); foreach (var o in Karakter.Gorunum(k)) if (o.sv <= sv) l.Add(o.id);
                if (l.Count > 0) Ayar.GORUNUM[k] = l[Random.Range(0, l.Count)];
            }
            var rl = new List<string>(); foreach (var m in Gear.RENK.Values) if (gr.RenkAcik(m.id, Ayar.sampiyon)) rl.Add(m.id);
            if (rl.Count > 0) gr.renk = rl[Random.Range(0, rl.Count)];
            foreach (var k in new[] { "grip", "tel" }) Ayar.RGOR[k] = Vitrin.RAK_SIRA[Random.Range(0, Vitrin.RAK_SIRA.Length)];
            kombinAktif = -1; KombinYaz(); root.sfx.Tik(); Yerel.Hap("tick"); KombinUygula();
        }

        // Kombin satırı: gövdenin başında (Raket kategorisi hariç; orada parça rayı başta). Yuvalar 44 pt çip, seçili ✓ + "Giyili".
        void KombinSatir(VisualElement c)
        {
            KombinYukle(); var simdi = KombinSimdi();
            c.Add(Grup(L.T("Kombinler")));
            var s = Satir("secim"); s.AddToClassList("kombin-satir"); c.Add(s);
            int giyili = -1; for (int i = 0; i < KOMBIN_SAY; i++) if (KombinAyni(kombinler[i], simdi)) { giyili = i; break; }
            for (int i = 0; i < KOMBIN_SAY; i++)
            {
                int ii = i; bool dolu = kombinler[i] != null, on = giyili == i;
                string ad = L.T("Kombin {0}", i + 1), alt = !dolu ? L.T("Boş · kaydet") : on ? L.T("Giyili") : L.T("Giy");
                var d = Cip(ad, on, () =>
                {
                    if (kombinler[ii] == null) { kombinler[ii] = KombinSimdi(); kombinAktif = ii; KombinYaz(); root.sfx.Yukselt(); SoyunmaGovdeKur(true); }
                    else { kombinAktif = ii; KombinYaz(); Tik(); KombinGiy(kombinler[ii]); }
                }, alt);
                d.AddToClassList("dar-cip"); d.AddToClassList("kombin-cip"); Sigdir(d); s.Add(d);
            }
            var r = Cip(L.T("Rastgele"), false, KombinRastgele, L.T("sahip olduklarından")); r.AddToClassList("dar-cip"); r.AddToClassList("kombin-cip"); Sigdir(r); s.Add(r);
            // Giyilen yuvadan sonra bir şey değişti: o yuvayı güncellemek tek dokunuş (yazıyla söylenir, sessizce yazılmaz).
            if (giyili < 0 && kombinAktif >= 0 && kombinler[kombinAktif] != null)
            {
                int a = kombinAktif;
                c.Add(Dugme(L.T("Kombin {0}'i bu görünüşle güncelle", a + 1), () => { kombinler[a] = KombinSimdi(); KombinYaz(); root.sfx.Yukselt(); Yerel.Hap("tick"); SoyunmaGovdeKur(true); }, "kombin-kaydet", null, 14));
            }
        }

        // ---- Denemeler ve alt çubuk ----
        struct Denenen { public string kat, id, ad; public int fiyat, sv; public ModelDurum du; }
        List<Denenen> Denenenler()
        {
            var vt = root.vitrin; var gr = Ayar.gear; var l = new List<Denenen>();
            void Koz(string kat, string id)
            {
                if (id == null) return; var o = Kozmetik.Oge(kat, id); var du = Kozmetik.Durum(gr, kat, id, Ayar.KOZ[kat]);
                if (du == ModelDurum.Secili || du == ModelDurum.Acik) return; l.Add(new Denenen { kat = kat, id = id, ad = o.ad, fiyat = o.fiyat, sv = o.sv, du = du });
            }
            Koz(Kozmetik.TIP, vt.deneTip); Koz(Kozmetik.SAC, vt.deneSac); Koz(Kozmetik.SAPKA, vt.deneSapka); Koz(Kozmetik.YUZ, vt.deneYuz);
            GearModel m; if (vt.deneAyk != null && Gear.GEAR["ayakkabi"].TryGetValue(vt.deneAyk, out m))
            {
                var du = gr.Durum("ayakkabi", vt.deneAyk);
                if (du != ModelDurum.Secili && du != ModelDurum.Acik) l.Add(new Denenen { kat = "ayakkabi", id = vt.deneAyk, ad = m.ad, fiyat = m.fiyat, sv = m.sv, du = du });
            }
            return l;
        }
        // Sabit alt çubuk (K4): kaydırma alanının altında, gövde kayarken yerinde durur. Deneme yoksa gizli.
        void DenemeCubukKur()
        {
            if (denemeCubuk == null) return; denemeCubuk.Clear();
            var l = Denenenler(); denemeCubuk.style.display = l.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex; if (l.Count == 0) return;
            int top = 0, alinir = 0, enAzSv = 99; var adlar = new List<string>(); var kilitli = new List<string>();
            foreach (var d in l) { adlar.Add(L.T(d.ad)); if (d.du == ModelDurum.Kilitli) { kilitli.Add(L.T("{0}: Sv {1} gerekli", L.T(d.ad), d.sv)); enAzSv = Mathf.Min(enAzSv, d.sv); } else { top += d.fiyat; alinir++; } }
            string kilitNot = kilitli.Count > 0 ? string.Join(" · ", kilitli) : null;
            // r101: çubuk 3 satırdı (başlık, kilit notu, düğmeler), gövdeye ~60 pt kalıyordu; tek satır: ne denendiği ve durumu
            // birincil düğmenin alt satırında (ekran okuyucu etiketi ikisini birlikte okur).
            var s = Satir("arali"); s.AddToClassList("deneme-cubuk-dugmeler"); denemeCubuk.Add(s);
            string ne = L.T("Deneniyor: {0}", string.Join(", ", adlar));
            string cta = alinir == 1 && l.Count == 1 ? L.T("{0} · {1} Tüy ile al", L.T(l[0].ad), top) : L.T("Hepsini al · {0} Tüy", top);
            int eksik = top - Ayar.gear.eko.tuy;
            // r100: hepsi kilitliyken "Hepsini al · 0 Tüy" yazıyordu; artık ne zaman açılacağını söyler.
            VisualElement al = alinir == 0 ? Pasif(L.T("Henüz alınamaz"), kilitNot ?? L.T("Sv {0}'da açılır · seviye atladıkça", enAzSv))
                : eksik > 0 ? Pasif(cta, ne + " · " + L.T("{0} Tüy daha gerekli", eksik))
                : Dugme(cta, () => DenemeHepsiniAl(), "dugme--birincil", kilitNot != null ? ne + " · " + kilitNot : ne, 15);
            al.AddToClassList("deneme-cubuk-al"); s.Add(al);
            var b = Dugme(L.T("Bırak"), () => { DenemeSil(); root.vitrin.Yenile(); Tik(); SoyunmaGovdeKur(true); }, "deneme-birak", null, 14); s.Add(b);
        }
        void DenemeHepsiniAl()
        {
            var gr = Ayar.gear; bool biri = false;
            foreach (var d in Denenenler())
            {
                if (d.du == ModelDurum.Kilitli) continue;
                bool ok = d.kat == "ayakkabi" ? gr.Al("ayakkabi", d.id) : Kozmetik.Al(gr, d.kat, d.id, Ayar.KOZ[d.kat]);
                if (!ok) continue; biri = true;
                if (d.kat == "ayakkabi") { gr.Sec("ayakkabi", d.id); root.vitrin.deneAyk = null; Ayar.GearHesap(); }
                else { Ayar.KOZ[d.kat] = d.id; if (d.kat == Kozmetik.TIP) root.vitrin.deneTip = null; else if (d.kat == Kozmetik.SAC) root.vitrin.deneSac = null; else if (d.kat == Kozmetik.SAPKA) root.vitrin.deneSapka = null; else root.vitrin.deneYuz = null; }
            }
            if (!biri) return;
            root.sfx.Yukselt(); Yerel.Hap("tick"); Ayar.Kaydet(); root.vitrin.Yenile(); kirli[(int)Sekme.Profil] = true; ustKirli = true; UstKur(); Degisti(); SoyunmaGovdeKur(true);
        }
    }
}
