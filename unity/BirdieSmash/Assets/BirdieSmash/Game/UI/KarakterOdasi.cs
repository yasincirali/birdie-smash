// Soyunma odası › Karakter (2026-10-26, yasin: "tip seçebilme, garip saçlar ... Karakterin tasarımına ekstra önem
// istiyoruz, kullanıcı tipini değiştirecek, parayla ya da tüyle saç modeli alacak. Ayakkabı seçebilecek, özelliklerine ve
// fiyatına göre"; "yakın açıda görüp"). Alt segment Tip · Saç · Ayakkabı · Ten; vitrin alt sayfaya göre yakın plana geçer
// (Saç → Yüz, Ayakkabı → Ayak), oyuncu vitrin düğmeleriyle istediği an değiştirir.
// Akış: her öğeye dokunmak onu vitrinde DENER (alınmamış ve kilitli olanlar dahil: önce gör, sonra karar ver). Sahip
// olunan öğe dokununca seçilir (geçiş ücretsiz). Alınmamış öğe denenirken sayfanın içinde bir deneme kartı açılır: ad, durum
// ve birincil düğme "<ad> · <fiyat> Tüy ile al" (kilitliyse pasif, nedeni yazar; Tüy yetmiyorsa kaç Tüy eksik). Ayrı onay
// penceresi yok (tasarım: diyalog yok): deneme + açık fiyatlı düğme iki bilinçli dokunuştur, "Denemeyi bırak" geri alır.
// Alımda Yukselt sesi + hafif titreşim (tick), üst şeritteki ve seviye satırındaki Tüy hemen güncellenir.
// Para: yalnız Tüy (Core/Kozmetik). Ayakkabı modeli Core/Gear'in 7. model yuvasıdır (güç taşır, 5 kendi çubuğu, artı ve
// eksi; IAP'ye hiçbir zaman girmez), ayakkabı rengi ücretsiz görünüm.
// Durum hiçbir yerde yalnız renkle verilmez: seçili ✓ + "Seçili", kilitli kilit + "Sv n", fiyat yazıyla; ekran okuyucu
// etiketi ad + durum + fiyat/koşul taşır. Büyük yazıda ızgaralar alt alta (USS .buyuk-yazi .izgara).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        int karAlt;
        static readonly string[] KAR_AD = { "Tip", "Saç", "Ayakkabı", "Ten" };
        public const int KAR_TIP = 0, KAR_SAC = 1, KAR_AYK = 2, KAR_TEN = 3;

        // EkranTuru için: Karakter segmentinde alt sayfa aç.
        public void KarakterAc(int alt) { SegmentBirak(); karAlt = Mathf.Clamp(alt, 0, 3); SoyunmaSegment(SEG_KARAKTER); KarakterOdakAyarla(); SoyunmaGovdeKur(false); }
        public void VitrinOdak(int o, string klip = null) { root.vitrin.Odak(o, klip); SoyunmaGovdeKur(true); }
        // EkranTuru için: bir öğeyi vitrinde dene (kat: Kozmetik.TIP, Kozmetik.SAC ya da "ayakkabi").
        public void KarakterDene(string kat, string id) { Dene(kat, id); }

        // Segmentten çıkınca deneme silinir, kamera tüm boya döner (deneme kayda yazılmadığı için başka sayfada kafa karıştırmasın).
        void SegmentBirak()
        {
            var vt = root.vitrin; if (vt == null) return; bool d = vt.deneTip != null || vt.deneSac != null || vt.deneAyk != null;
            vt.deneTip = vt.deneSac = vt.deneAyk = null; vt.Odak(Vitrin.TAM); if (d) vt.Yenile();
        }
        void KarakterOdakAyarla() { var vt = root.vitrin; if (vt != null) vt.Odak(karAlt == KAR_SAC ? Vitrin.YUZ : karAlt == KAR_AYK ? Vitrin.AYAK : Vitrin.TAM); }

        // Vitrinin sol altı: Yüz · Ayak · Tutuş. Seçili ✓ ve açık zemin; aynısına dokunmak Tüm boy'a döner.
        void OdakDugmeleriKur()
        {
            if (vitrinOdak == null) return; vitrinOdak.Clear(); var vt = root.vitrin;
            var ad = new[] { "Yüz", "Ayak", "Tutuş" }; var od = new[] { Vitrin.YUZ, Vitrin.AYAK, Vitrin.TUTUS };
            for (int i = 0; i < 3; i++)
            {
                int o = od[i]; bool sec = vt.odak == o && vt.hedefGec < 1;
                var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("odak-dugme"); if (sec) d.AddToClassList("odak-dugme--secili");
                var ic = new VisualElement(); ic.AddToClassList("cip-ic"); ic.pickingMode = PickingMode.Ignore; d.Add(ic);
                if (sec) ic.Add(Ikon(Ikonlar.Tik, 13, Kit.Hex("#07161c")));
                ic.Add(Yazi(L.T(ad[i]), 13, "dugme-yazi"));
                d.AddManipulator(new Clickable(() => { Tik(); vt.Odak(sec ? Vitrin.TAM : o, o == Vitrin.TUTUS ? vt.poz ?? "drive" : null); SoyunmaGovdeKur(true); }));
                Erisim.Etiket(d, L.T("Yakın plan: {0}", L.T(ad[i])) + (sec ? ", " + L.T("tüm boya dönmek için yeniden dokun") : ""), "dugme", sec);
                vitrinOdak.Add(d);
            }
        }

        // Raket bölümü: tutuş görünümü. Kamera ele yaklaşır, figür o vuruşun temas pozunda durur (raket hep sağ elde).
        static readonly string[] TUTUS_AD = { "Forehand", "Backhand", "File", "Servis" }, TUTUS_KLIP = { "drive", "bh_drive", "net", "serve" };
        void TutusSecici(VisualElement c)
        {
            var vt = root.vitrin; c.Add(Grup(L.T("Tutuşa yakından bak"))); var s = Satir(); c.Add(s);
            for (int i = 0; i < 4; i++)
            {
                string k = TUTUS_KLIP[i]; bool sec = vt.odak == Vitrin.TUTUS && vt.poz == k && vt.hedefGec < 1;
                s.Add(Cip(L.T(TUTUS_AD[i]), sec, () => { vt.Odak(Vitrin.TUTUS, k); root.sfx.Tik(); SoyunmaGovdeKur(true); }));
            }
        }

        // ---- Karakter gövdesi ----
        void KarakterGovde(VisualElement c)
        {
            SeviyeSatiri(c);
            var seg = Satir("segment"); seg.AddToClassList("alt-segment"); c.Add(seg);
            for (int i = 0; i < 4; i++)
            {
                int ii = i; var d = Cip(L.T(KAR_AD[i]), karAlt == i, () =>
                {
                    if (karAlt == ii) return; karAlt = ii; var vt = root.vitrin; bool d0 = vt.deneTip != null || vt.deneSac != null || vt.deneAyk != null;
                    vt.deneTip = vt.deneSac = vt.deneAyk = null; if (d0) vt.Yenile(); KarakterOdakAyarla(); SoyunmaGovdeKur(false);
                });
                d.AddToClassList("segment-cip"); var b = d.userData as ErisimBilgi; if (b != null) b.rol = "sekme"; seg.Add(d);
            }
            if (Ayar.model != "oyuncak" && karAlt != KAR_TEN) c.Add(Yazi(L.T("Tip, saç biçimi ve ayakkabı modeli Oyuncak modelde görünür (Ayarlar › Oyuncu görünümü). Seçimin kaydedilir."), 12, "soluk"));
            if (karAlt == KAR_TIP) TipSayfa(c); else if (karAlt == KAR_SAC) SacSayfa(c); else if (karAlt == KAR_AYK) AyakkabiSayfa(c); else TenSayfa(c);
        }

        // Kozmetik öğenin durumu: yazı + simge (✓ seçili, kilit kilitli). Fiyatı 0 olan sahip öğe "Ücretsiz", alınmış "Senin".
        string KozDurum(string kat, KozmetikOge o, ModelDurum du, out Texture2D ik)
        {
            ik = null;
            if (du == ModelDurum.Secili) { ik = Ikonlar.Tik; return L.T("Seçili"); }
            if (du == ModelDurum.Acik) return o.fiyat == 0 ? L.T("Ücretsiz") : L.T("Senin");
            if (du == ModelDurum.Kilitli) { ik = Ikonlar.Kilit; return L.T("Sv {0}", o.sv) + " · " + o.fiyat + " " + L.T("Tüy"); }
            return o.fiyat + " " + L.T("Tüy");
        }

        // Dokunuş: sahipse seç (kaydet), değilse vitrinde dene.
        void KozDokun(string kat, string id)
        {
            var gr = Ayar.gear;
            bool sahip = kat == "ayakkabi" ? gr.Sahip("ayakkabi", id) : Kozmetik.Sahip(gr, kat, id) || Ayar.KOZ[kat] == id;
            if (sahip) { KozSec(kat, id); Tik(); }
            else { Dene(kat, id); root.sfx.Tik(); Yerel.Hap("tick"); }
        }
        void Dene(string kat, string id)
        {
            var vt = root.vitrin;
            if (kat == Kozmetik.TIP) vt.deneTip = id; else if (kat == Kozmetik.SAC) vt.deneSac = id; else vt.deneAyk = id;
            vt.Yenile(); SoyunmaGovdeKur(true);
        }
        void DenemeBirak(string kat)
        {
            var vt = root.vitrin; if (kat == Kozmetik.TIP) vt.deneTip = null; else if (kat == Kozmetik.SAC) vt.deneSac = null; else vt.deneAyk = null;
            vt.Yenile(); Tik(); SoyunmaGovdeKur(true);
        }
        void KozSec(string kat, string id)
        {
            var gr = Ayar.gear; var vt = root.vitrin;
            if (kat == "ayakkabi") { if (!gr.Sec("ayakkabi", id)) return; vt.deneAyk = null; Ayar.GearHesap(); }
            else { Ayar.KOZ[kat] = id; if (kat == Kozmetik.TIP) vt.deneTip = null; else vt.deneSac = null; }
            Ayar.Kaydet(); vt.Yenile(); kirli[(int)Sekme.Profil] = true; SoyunmaGovdeKur(true);
        }
        void KozAl(string kat, string id)
        {
            var gr = Ayar.gear; bool ok = kat == "ayakkabi" ? gr.Al("ayakkabi", id) : Kozmetik.Al(gr, kat, id, Ayar.KOZ[kat]);
            if (!ok) return;
            root.sfx.Yukselt(); Yerel.Hap("tick"); ustKirli = true; UstKur(); KozSec(kat, id); Degisti();
        }

        // Deneme kartı: denenen öğe alınmamışsa (seçili ya da sahipse kart yok; dokunuş onu zaten seçmiştir).
        void DenemeKart(VisualElement c, string kat, string id, string ad, int fiyat, int sv, ModelDurum du, bool alOlur)
        {
            if (id == null || du == ModelDurum.Secili || du == ModelDurum.Acik) return;
            var kart = Kart("deneme-kart"); c.Add(kart);
            var bas = Satir("arali"); bas.pickingMode = PickingMode.Ignore; kart.Add(bas);
            bas.Add(Yazi(L.T("Deneniyor: {0}", L.T(ad)), 15, "kalin"));
            var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; bas.Add(dr);
            if (du == ModelDurum.Kilitli) { dr.Add(Ikon(Ikonlar.Kilit, 14, SOLUK)); }
            var dy = Yazi(du == ModelDurum.Kilitli ? L.T("Sv {0} gerekli", sv) : fiyat + " " + L.T("Tüy"), 12, "durum-kilit"); dy.style.marginLeft = 4; dr.Add(dy);
            Erisim.Etiket(kart, L.T("Deneniyor: {0}", L.T(ad)) + ", " + (du == ModelDurum.Kilitli ? L.T("Sv {0} gerekli", sv) : fiyat + " " + L.T("Tüy")), "metin");
            string cta = L.T("{0} · {1} Tüy ile al", L.T(ad), fiyat);
            if (du == ModelDurum.Kilitli) kart.Add(Pasif(cta, L.T("Sv {0}'da açılır · seviye atladıkça", sv)));
            else if (alOlur) kart.Add(Dugme(cta, () => KozAl(kat, id), "dugme--birincil", L.T("Bir kez alınır, hep senin"), 16));
            else kart.Add(Pasif(cta, L.T("{0} Tüy daha gerekli", fiyat - Ayar.gear.eko.tuy)));
            kart.Add(Dugme(L.T("Denemeyi bırak"), () => DenemeBirak(kat), "deneme-birak"));
        }

        // ---- Tip: 2×2 kart ----
        void TipSayfa(VisualElement c)
        {
            var gr = Ayar.gear; string sec = Ayar.KOZ[Kozmetik.TIP], dn = root.vitrin.deneTip;
            c.Add(Grup(L.T("Beden tipi · {0}", L.T(Kozmetik.Oge(Kozmetik.TIP, sec).ad))));
            c.Add(Yazi(L.T("Yalnız görünüş: maçta boy ve kol uzunluğu fark etmez. Sahip olduğun tipler arasında geçiş ücretsiz."), 12, "soluk"));
            var iz = Satir("izgara"); iz.AddToClassList("koz-izgara"); c.Add(iz);
            foreach (var o in Kozmetik.Liste(Kozmetik.TIP))
            {
                var du = Kozmetik.Durum(gr, Kozmetik.TIP, o.id, sec); string id = o.id; Texture2D ik; string durum = KozDurum(Kozmetik.TIP, o, du, out ik);
                var k = Kart("koz-kart"); k.AddToClassList("dugme"); if (du == ModelDurum.Secili) k.AddToClassList("secili"); if (du == ModelDurum.Kilitli) k.AddToClassList("kilitli"); if (dn == o.id) k.AddToClassList("denenen");
                k.Add(Yazi(L.T(o.ad), 16, "kalin")); if (!string.IsNullOrEmpty(o.n)) k.Add(Yazi(L.T(o.n), 12, "ipucu"));
                var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; k.Add(dr); if (ik != null) { var i = Ikon(ik, 13, du == ModelDurum.Secili ? LIMON : SOLUK); i.style.marginRight = 4; dr.Add(i); }
                dr.Add(Yazi(durum, 12, du == ModelDurum.Secili ? "durum-gecti" : "durum-kilit"));
                if (dn == o.id && du != ModelDurum.Secili) dr.Add(Yazi(" · " + L.T("deneniyor"), 12, "durum-simdi"));
                k.AddManipulator(new Clickable(() => KozDokun(Kozmetik.TIP, id)));
                Erisim.Etiket(k, L.T("Beden tipi") + ", " + L.T(o.ad) + ", " + durum + (dn == o.id ? ", " + L.T("deneniyor") : ""), "dugme", du == ModelDurum.Secili);
                iz.Add(k);
            }
            if (dn != null) { var o = Kozmetik.Oge(Kozmetik.TIP, dn); DenemeKart(c, Kozmetik.TIP, dn, o.ad, o.fiyat, o.sv, Kozmetik.Durum(gr, Kozmetik.TIP, dn, sec), Kozmetik.AlOlur(gr, Kozmetik.TIP, dn, sec)); }
        }

        // ---- Saç: 5 sütunlu biçim ızgarası + renk sırası ----
        void SacSayfa(VisualElement c)
        {
            var gr = Ayar.gear; string sec = Ayar.KOZ[Kozmetik.SAC], dn = root.vitrin.deneSac; int sv = gr.Seviye();
            c.Add(Grup(L.T("Saç modeli · {0}", L.T(Kozmetik.Oge(Kozmetik.SAC, dn ?? sec).ad))));
            var iz = Satir("izgara"); iz.AddToClassList("koz-izgara"); c.Add(iz);
            foreach (var o in Kozmetik.Liste(Kozmetik.SAC))
            {
                var du = Kozmetik.Durum(gr, Kozmetik.SAC, o.id, sec); string id = o.id; Texture2D ik; string durum = KozDurum(Kozmetik.SAC, o, du, out ik);
                var k = Kart("sac-kare"); k.AddToClassList("dugme"); if (du == ModelDurum.Secili) k.AddToClassList("secili"); if (du == ModelDurum.Kilitli) k.AddToClassList("kilitli"); if (dn == o.id) k.AddToClassList("denenen");
                k.Add(Yazi(L.T(o.ad), 12, "kalin"));
                var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; k.Add(dr); if (ik != null) { var i = Ikon(ik, 11, du == ModelDurum.Secili ? LIMON : SOLUK); i.style.marginRight = 2; dr.Add(i); }
                dr.Add(Yazi(du == ModelDurum.Kilitli ? L.T("Sv {0}", o.sv) : durum, 10, du == ModelDurum.Secili ? "durum-gecti" : "durum-kilit"));
                k.AddManipulator(new Clickable(() => KozDokun(Kozmetik.SAC, id)));
                Erisim.Etiket(k, L.T("Saç modeli") + ", " + L.T(o.ad) + ", " + durum + (dn == o.id ? ", " + L.T("deneniyor") : ""), "dugme", du == ModelDurum.Secili);
                iz.Add(k);
            }
            var go = Kozmetik.Oge(Kozmetik.SAC, dn ?? sec);
            if (go != null && !go.bant && Ayar.gear.bant != "yok") c.Add(Yazi(L.T("{0} saçta kafa bandı görünmez (saçın içinde kalır); bandın etkisi sürer.", L.T(go.ad)), 12, "soluk"));
            if (dn != null) { var o = Kozmetik.Oge(Kozmetik.SAC, dn); DenemeKart(c, Kozmetik.SAC, dn, o.ad, o.fiyat, o.sv, Kozmetik.Durum(gr, Kozmetik.SAC, dn, sec), Kozmetik.AlOlur(gr, Kozmetik.SAC, dn, sec)); }
            var l = new List<Ornek>(); foreach (var o in Karakter.Gorunum("sac")) l.Add(new Ornek { id = o.id, ad = o.ad, renk = o.renk, acik = sv >= o.sv, neden = L.T("Sv {0} gerekli", o.sv), kisa = L.T("Sv {0}", o.sv) });
            OrnekSira(c, L.T("Saç rengi"), l, Ayar.GORUNUM["sac"], id => { Ayar.GORUNUM["sac"] = id; Ayar.Kaydet(); }, true);
        }

        // ---- Ayakkabı: seçili/denenen ayakkabı kartı (5 çubuk) + model satırları + renk ----
        void AyakkabiSayfa(VisualElement c)
        {
            var gr = Ayar.gear; string dn = root.vitrin.deneAyk, gos = dn ?? gr.ayakkabi; int sv = gr.Seviye();
            var m = Gear.GEAR["ayakkabi"][gos]; var notr = Gear.AyakkabiCubuklar(Gear.GEAR["ayakkabi"][Gear.Varsayilan("ayakkabi")]);
            var du0 = gr.Durum("ayakkabi", gos);
            var kart = Kart("model-kart"); kart.AddToClassList(dn != null && du0 != ModelDurum.Secili ? "denenen" : "secili"); c.Add(kart);
            var bas = Satir("arali"); bas.pickingMode = PickingMode.Ignore; kart.Add(bas); bas.Add(Yazi(L.T(m.ad), 17, "kalin"));
            var dr0 = Satir("rakip-satir"); dr0.pickingMode = PickingMode.Ignore; bas.Add(dr0);
            if (du0 == ModelDurum.Secili) dr0.Add(Ikon(Ikonlar.Tik, 14, LIMON));
            dr0.Add(Yazi(du0 == ModelDurum.Secili ? L.T("Seçili") : L.T("deneniyor"), 12, du0 == ModelDurum.Secili ? "durum-gecti" : "durum-simdi"));
            kart.Add(Yazi(L.T(m.n), 13, "ipucu"));
            CubukKart(kart, Gear.AyakkabiCubuklar(m), notr, false, Gear.AYK_CUBUK_AD);
            Erisim.Etiket(kart, L.T("Ayakkabı") + ", " + L.T(m.ad) + ", " + L.T(m.n) + ", " + (du0 == ModelDurum.Secili ? L.T("Seçili") : L.T("deneniyor")), "metin", du0 == ModelDurum.Secili);
            if (dn != null) DenemeKart(c, "ayakkabi", dn, m.ad, m.fiyat, m.sv, du0, gr.AlOlur("ayakkabi", dn));
            c.Add(Yazi(L.T("Çubuklar ayakkabıyı anlatır, 50 nötr. En iyi ayakkabı yok: her birinin artısı ve eksisi var. Takımın Çabukluk ve Dayanıklılık çubuğuna da girer."), 12, "soluk"));
            c.Add(Grup(L.T("Modeller")));
            foreach (var a in Gear.GEAR["ayakkabi"].Values)
            {
                var du = gr.Durum("ayakkabi", a.id); string id = a.id;
                var r = Kart("ayk-satir"); r.AddToClassList("dugme"); if (du == ModelDurum.Secili) r.AddToClassList("secili"); if (du == ModelDurum.Kilitli) r.AddToClassList("kilitli"); if (dn == a.id) r.AddToClassList("denenen"); c.Add(r);
                var b = Satir("arali"); b.pickingMode = PickingMode.Ignore; r.Add(b); b.Add(Yazi(L.T(a.ad), 15, "kalin"));
                var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; b.Add(dr); string durum;
                if (du == ModelDurum.Secili) { dr.Add(Ikon(Ikonlar.Tik, 14, LIMON)); durum = L.T("Seçili"); }
                else if (du == ModelDurum.Kilitli) { dr.Add(Ikon(Ikonlar.Kilit, 14, SOLUK)); durum = L.T("Sv {0}", a.sv) + " · " + a.fiyat + " " + L.T("Tüy"); }
                else if (du == ModelDurum.Alinir) durum = a.fiyat + " " + L.T("Tüy"); else durum = a.fiyat == 0 ? L.T("Ücretsiz") : L.T("Senin");
                var dy = Yazi(durum, 12, du == ModelDurum.Secili ? "durum-gecti" : "durum-kilit"); dy.style.marginLeft = 4; dr.Add(dy);
                r.Add(Yazi(L.T(a.n), 12, "ipucu"));
                r.AddManipulator(new Clickable(() => KozDokun("ayakkabi", id)));
                Erisim.Etiket(r, L.T("Ayakkabı") + ", " + L.T(a.ad) + ", " + L.T(a.n) + ", " + durum + (dn == a.id ? ", " + L.T("deneniyor") : ""), "dugme", du == ModelDurum.Secili);
            }
            var l = new List<Ornek>(); foreach (var o in Karakter.Gorunum("ayakkabi")) l.Add(new Ornek { id = o.id, ad = o.ad, renk = o.renk, acik = sv >= o.sv, neden = L.T("Sv {0} gerekli", o.sv), kisa = L.T("Sv {0}", o.sv) });
            OrnekSira(c, L.T("Ayakkabı rengi"), l, Ayar.GORUNUM["ayakkabi"], id => { Ayar.GORUNUM["ayakkabi"] = id; Ayar.Kaydet(); }, true);
            c.Add(Yazi(L.T("Renk yalnız görünüştür, ücretsiz."), 12, "soluk"));
        }

        // ---- Ten: baştan hepsi açık (kimlik satılmaz) ----
        void TenSayfa(VisualElement c)
        {
            int sv = Ayar.gear.Seviye();
            var l = new List<Ornek>(); foreach (var o in Karakter.Gorunum("ten")) l.Add(new Ornek { id = o.id, ad = o.ad, renk = o.renk, acik = sv >= o.sv, neden = L.T("Sv {0} gerekli", o.sv), kisa = L.T("Sv {0}", o.sv) });
            OrnekSira(c, L.T("Ten tonu"), l, Ayar.GORUNUM["ten"], id => { Ayar.GORUNUM["ten"] = id; Ayar.Kaydet(); }, true);
            c.Add(Yazi(L.T("Ten tonlarının hepsi baştan açık ve ücretsiz."), 12, "soluk"));
            if (Ayar.model == "rocketbox") c.Add(Yazi(L.T("Gerçekçi modelde ten dokudan gelir; ten tonu Oyuncak modelde görünür."), 12, "soluk"));
        }
    }
}
