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
        // 2026-10-30 Görünüm segmenti (eski Karakter): Beden · Yüz · Saç · Şapka · Ayakkabı · Renk. Yüz = ten + yüz aksesuarı
        // (gözlük, bıyık, korsan bandı, yüz boyası); Şapka her modelde; Renk = forma, şort ve raket görünüşü (eski Kıyafet ve
        // eski Görünüm segmenti). Ayakkabı sayfası yalnız görünüşü seçer, taban Oyun'da. Sabitlerin değeri korunur (EkranTuru).
        // 2026-10-30 Dolap (öneri artifact seçenek A): kategori şeridi vitrinin hemen altında, sabit (gövde kayar); Raket kendi
        // kategorisi (KAR_RAKET): seçilince vitrin raket tezgâhına geçer, parça şeridi (Kafa şekli · Çerçeve · Şaft · Grip ·
        // Kordaj) kamerayı o bölgeye götürür. Renk sayfası artık yalnız forma, şort ve takılı kıyafet.
        static readonly string[] KAR_AD = { "Beden", "Saç", "Ayakkabı", "Yüz", "Forma", "Şapka", "Raket" };
        public const int KAR_TIP = 0, KAR_SAC = 1, KAR_AYK = 2, KAR_TEN = 3, KAR_KIYAFET = 4, KAR_SAPKA = 5, KAR_RAKET = 6;
        // Alt sayfalar modele göre (2026-10-27, yasin: "Gerçekçi de vücut özellikleri değil de sadece aksesuarlar değişmeli
        // kafa bandı forma bileklik ayakkabı şort tişört raket"): Gerçekçi'de beden/yüz/saç tek, Tip/Saç/Ten sayfası yok;
        // Ayakkabı (beş model görünür) ve Kıyafet (forma rengi + kumaşı, şort rengi, kafa bandı, bileklik, raket). Sade'de beden
        // ve saç biçimi tek; Ayakkabı (renk görünür, model görünmez: kart söyler), Kıyafet ve Ten (ten + saç rengi düz renk
        // parçalarda görünür). Oyuncak 3B'nin dört sayfası aynen. Tüycük (2026-10-29): beden, yüz ve perçem tek; ten ve saç yok
        // (gövdenin rengi forma rengi) → Ayakkabı (beş model görünür) ve Kıyafet (forma = tüy rengi, bant, bileklik, raket; şort yok).
        public static int[] KarSayfalar() { return Ayar.VaryantGorunur ? new[] { KAR_TIP, KAR_TEN, KAR_SAC, KAR_SAPKA, KAR_AYK, KAR_KIYAFET, KAR_RAKET } : new[] { KAR_TEN, KAR_SAPKA, KAR_AYK, KAR_KIYAFET, KAR_RAKET }; }
        static int KarGecerli(int a) { var l = KarSayfalar(); return System.Array.IndexOf(l, a) >= 0 ? a : l[0]; }

        // EkranTuru için: Karakter segmentinde alt sayfa aç (bu modelde olmayan sayfa istenirse modelin ilk sayfası).
        public void KarakterAc(int alt) { SegmentBirak(); karAlt = KarGecerli(Mathf.Clamp(alt, 0, KAR_RAKET)); SoyunmaSegment(SEG_KARAKTER); KarakterOdakAyarla(); KategoriKur(); SoyunmaGovdeKur(false); }
        public void VitrinOdak(int o, string klip = null) { root.vitrin.Odak(o, klip); SoyunmaGovdeKur(true); }
        // EkranTuru için: bir öğeyi vitrinde dene (kat: Kozmetik.TIP, Kozmetik.SAC ya da "ayakkabi").
        public void KarakterDene(string kat, string id) { Dene(kat, id); }

        // Segmentten çıkınca deneme silinir, kamera tüm boya döner (deneme kayda yazılmadığı için başka sayfada kafa karıştırmasın).
        void SegmentBirak()
        {
            var vt = root.vitrin; if (vt == null) return; bool d = DenemeVar();
            DenemeSil(); vt.Odak(Vitrin.TAM); if (d) vt.Yenile();
        }
        bool DenemeVar() { var vt = root.vitrin; return vt.deneTip != null || vt.deneSac != null || vt.deneAyk != null || vt.deneSapka != null || vt.deneYuz != null; }
        void DenemeSil() { var vt = root.vitrin; vt.deneTip = vt.deneSac = vt.deneAyk = vt.deneSapka = vt.deneYuz = null; }
        // Kategori kamerayı çağırır (araştırma: her kategorinin bir kadrajı); Raket vitrini tezgâha geçirir, diğerleri figüre döndürür.
        void KarakterOdakAyarla()
        {
            var vt = root.vitrin; if (vt == null) return;
            if (karAlt == KAR_RAKET) { vt.Odak(Vitrin.TAM); vt.hedefGec = 1; vt.RaketParca(raketParca); return; }
            vt.hedefGec = 0; vt.Odak(karAlt == KAR_SAC || karAlt == KAR_SAPKA || karAlt == KAR_TEN ? Vitrin.YUZ : karAlt == KAR_AYK ? Vitrin.AYAK : Vitrin.TAM);
        }

        // Kategori şeridi (vitrinin altında, sabit): seçili ✓ + zemin, ekran okuyucuda "sekme, seçili". 7 kategori 4 + 3 satır.
        void KategoriKur()
        {
            if (soyunmaSegment == null) return; soyunmaSegment.Clear(); karAlt = KarGecerli(karAlt);
            foreach (int i in KarSayfalar())
            {
                int ii = i; var d = Cip(L.T(KAR_AD[i]), karAlt == i, () => KategoriSec(ii));
                d.AddToClassList("segment-cip"); d.AddToClassList("dar-cip"); Sigdir(d); var b = d.userData as ErisimBilgi; if (b != null) b.rol = "sekme"; soyunmaSegment.Add(d);
            }
        }
        void KategoriSec(int i)
        {
            if (karAlt == i) { if (i == KAR_RAKET && raketParca != null) { raketParca = null; root.vitrin.RaketParca(null); SoyunmaGovdeKur(true); } return; }
            karAlt = i; var vt = root.vitrin; bool d0 = DenemeVar(); DenemeSil(); if (d0) vt.Yenile(); Tik();
            KarakterOdakAyarla(); KategoriKur(); SoyunmaGovdeKur(false);
        }

        // ---- Raket tezgâhı (2026-10-30) ----
        // Parça şeridi: Tüm raket · Kafa şekli · Çerçeve · Şaft · Grip · Kordaj. Seçilen parça vitrinde kadraja gelir ve bir kez
        // parlar (Vitrin.RaketParca); rakette bir parçaya dokunmak da onu seçer (kısayol, tek yol değil). Seçenekler yalnız
        // görünüş (Dolap kuralı): kafa şekli, çerçeve kaplaması, şaft rengi, grip rengi, kordaj rengi ve deseni.
        string raketParca;
        static readonly string[] PARCA = { null, "kafa", "cerceve", "saft", "grip", "kordaj" };
        static readonly string[] PARCA_AD = { "Tüm raket", "Kafa şekli", "Çerçeve", "Şaft", "Grip", "Kordaj" };
        public void RaketParcaSec(string p) { raketParca = p; root.vitrin.RaketParca(p); Tik(); SoyunmaGovdeKur(true); }   // EkranTuru + vitrin dokunuşu
        void RaketSayfa(VisualElement c)
        {
            var s = Satir("segment"); s.AddToClassList("alt-segment"); s.AddToClassList("parca-ray"); c.Add(s);
            for (int i = 0; i < PARCA.Length; i++)
            {
                string p = PARCA[i]; bool sec = raketParca == p;
                var d = Cip(L.T(PARCA_AD[i]), sec, () => { if (raketParca != p) RaketParcaSec(p); });
                d.AddToClassList("segment-cip"); d.AddToClassList("dar-cip"); Sigdir(d); var b = d.userData as ErisimBilgi; if (b != null) b.rol = "sekme"; s.Add(d);
            }
            var gr = Ayar.gear; int sv = gr.Seviye(); string ps = raketParca;
            if (ps == null || ps == "kafa") KafaSekliSira(c);
            if (ps == null || ps == "cerceve")
            {
                var kl = new List<Ornek>(); foreach (var o in Karakter.Gorunum("kaplama")) kl.Add(new Ornek { id = o.id, ad = o.ad, renk = o.renk, acik = sv >= o.sv, neden = L.T("Sv {0} gerekli", o.sv), kisa = L.T("Sv {0}", o.sv) });
                OrnekSira(c, L.T("Çerçeve kaplaması"), kl, Ayar.GORUNUM["kaplama"], id => { Ayar.GORUNUM["kaplama"] = id; Ayar.Kaydet(); });
            }
            if (ps == null || ps == "saft") SaftSira(c);
            if (ps == null || ps == "grip") RenkSira(c, "Grip", "grip");
            if (ps == null || ps == "kordaj")
            {
                RenkSira(c, "Kordaj", "tel");
                c.Add(Grup(L.T("Desen"))); var ds = Satir(); c.Add(ds);
                ds.Add(Cip(L.T("Tek renk"), Ayar.RGOR["desen"] == "tek", () => { Ayar.RGOR["desen"] = "tek"; Ayar.Kaydet(); root.vitrin.Yenile(); root.sfx.Tik(); SoyunmaGovdeKur(true); }));
                ds.Add(Cip(L.T("Ana ve çapraz ayrı"), Ayar.RGOR["desen"] == "iki", () => { Ayar.RGOR["desen"] = "iki"; Ayar.Kaydet(); root.vitrin.Yenile(); root.sfx.Tik(); SoyunmaGovdeKur(true); }));
                if (Ayar.RGOR["desen"] == "iki") RenkSira(c, "Çapraz teller", "tel2");
            }
            c.Add(Yazi(L.T("Raketin görünüşü oyunu etkilemez. Raketin gücü ve kordaj gerginliği Atölye'de."), 12, "soluk"));
        }
        // Kafa şekli: üç biçim, örnek kare içinde biçimin kendisi çizili (yalnız adla değil); seçili ✓ + kalın çerçeve.
        void KafaSekliSira(VisualElement c)
        {
            string cur = Ayar.RGOR["kafa"]; c.Add(Grup(L.T("Kafa şekli") + " · " + L.T(RaketSekil.Ad(cur)))); var s = Satir("renkler"); c.Add(s);
            foreach (var id in RaketSekil.SIRA)
            {
                bool on = cur == id; string k = id;
                var d = new VisualElement(); d.AddToClassList("renk"); d.AddToClassList("sekil-ornek"); if (on) d.AddToClassList("renk--secili"); d.style.backgroundColor = Kit.Hex("#33233F");
                d.Add(Ikon(RaketSekil.Ikon(id), 30, on ? TURUNCU : Color.white));
                var y = Yazi(L.T(RaketSekil.Ad(id)), 10, "renk-kilit-yazi"); y.style.color = Color.white; d.Add(y);
                if (!on) d.AddManipulator(new Clickable(() => { Ayar.RGOR["kafa"] = k; Ayar.Kaydet(); Tik(); root.vitrin.Yenile(); root.vitrin.RaketParca("kafa", true); SoyunmaGovdeKur(true); }));
                Erisim.Etiket(d, L.T("Kafa şekli") + ", " + L.T(RaketSekil.Ad(id)), "dugme", on); s.Add(d);
            }
        }
        // Şaft rengi: "Modelin rengi" (FBX'teki koyu gri) + raket renkleri.
        void SaftSira(VisualElement c)
        {
            var l = new List<Ornek> { new Ornek { id = "model", ad = "Modelin rengi", renk = null, acik = true } };
            foreach (var id in Vitrin.RAK_SIRA) l.Add(new Ornek { id = id, ad = Vitrin.RAK_AD[id], renk = Vitrin.RAK_RENK[id], acik = true });
            OrnekSira(c, L.T("Şaft"), l, Ayar.RGOR["saft"], id => { Ayar.RGOR["saft"] = id; Ayar.Kaydet(); });
        }

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
                if (sec) ic.Add(Ikon(Ikonlar.Tik, 13, MUREKKEP));
                ic.Add(Yazi(L.T(ad[i]), VitrinPx(13), "dugme-yazi"));
                d.AddManipulator(new Clickable(() => { Tik(); vt.Odak(sec ? Vitrin.TAM : o, o == Vitrin.TUTUS ? vt.poz ?? "drive" : null); SoyunmaGovdeKur(true); }));
                Erisim.Etiket(d, L.T("Yakın plan: {0}", L.T(ad[i])) + (sec ? ", " + L.T("tüm boya dönmek için yeniden dokun") : ""), "dugme", sec);
                vitrinOdak.Add(d);
            }
        }

        // ---- Karakter gövdesi ----
        void KarakterGovde(VisualElement c)
        {
            // raket görünümünde (vitrin rakette) yalnız raketin görünüşü: forma ve şapka figür görünmezken seçilmez
            karAlt = KarGecerli(karAlt); GorunumRozet(c);
            if (karAlt == KAR_RAKET) { RaketSayfa(c); return; }
            if (!Ayar.VaryantGorunur) TekBedenKart(c);
            if (!Ayar.AykModelGorunur && karAlt == KAR_AYK) ModelUyari(c);
            if (karAlt == KAR_TIP) TipSayfa(c); else if (karAlt == KAR_SAC) SacSayfa(c); else if (karAlt == KAR_AYK) AyakkabiSayfa(c); else if (karAlt == KAR_KIYAFET) KiyafetSayfa(c);
            else if (karAlt == KAR_SAPKA) SapkaSayfa(c); else TenSayfa(c);
        }
        // "Oyunu etkilemez" rozeti: Görünüm'deki hiçbir seçim maçta bir sayıyı değiştirmez (yazıyla; renk yalnız süs).
        void GorunumRozet(VisualElement c)
        {
            var r = Satir("rakip-satir"); r.pickingMode = PickingMode.Ignore; r.style.marginBottom = 6; c.Add(r);
            var i = Ikon(Ikonlar.Tik, 13, CAMGOBEGI); i.style.marginRight = 6; r.Add(i);
            var y = Yazi(L.T("Oyunu etkilemez · yalnız görünüş"), 13, "etiket"); r.Add(y); Erisim.Etiket(r, y.text, "metin");
        }

        // Gerçekçi/Sade: ne sabit, ne değişir (2026-10-27). Ürün yalan söylemez: beden tipi ve saç biçimi bu modelde yok, o yüzden
        // sayfaları da yok; kart nedenini ve nerede olduğunu söyler, Oyuncak 3B'ye tek dokunuşla geçilir (ikincil düğme: bu
        // sayfanın işi kıyafet seçmek, model değiştirmek değil).
        void TekBedenKart(VisualElement c)
        {
            bool rb = Ayar.model == "rocketbox", tc = Ayar.model == "tuycuk"; string bas = rb ? L.T("Gerçekçi: tek beden") : tc ? L.T("Tüycük: tek beden") : L.T("Sade: tek beden");
            string metin = rb ? L.T("Beden, yüz ve saç bu modelde sabit. Ayakkabı, forma, şort, kafa bandı, bileklik ve raket değişir. Beden tipi ve saç biçimi Oyuncak 3B'de.")
                : tc ? L.T("Beden, yüz ve tüy perçemi bu modelde sabit. Tüy rengi forma renginden gelir; ayakkabı, kafa bandı, bileklik ve raket değişir. Beden tipi ve saç biçimi Oyuncak 3B'de.")
                : L.T("Beden ve saç biçimi bu modelde sabit. Forma, şort, ayakkabı rengi, ten, saç rengi, kafa bandı, bileklik ve raket değişir. Beden tipi ve saç biçimi Oyuncak 3B'de.");
            var kart = Kart("bilgi-kart"); c.Add(kart);
            kart.Add(Yazi(bas, 15, "kalin")); kart.Add(Yazi(metin, 13, "ipucu"));
            kart.Add(Dugme(L.T("Oyuncak 3B'ye geç"), () => { Ayar.model = "oyuncak"; Ayar.Kaydet(); root.ModelDegisti(); karAlt = KAR_TIP; Tik(); SoyunmaGovdeKur(true); }, null, L.T("Ayarlar › Oyuncu görünümü"), 15));
            Erisim.Etiket(kart, bas + ". " + metin, "metin");
        }

        // 2026-10-27 (yasin build 41: "3B model kaybolmuş, seçmeme rağmen görmedim"): Gerçekçi ya da Sade seçiliyken tip, saç
        // biçimi ve ayakkabı modeli için ne vitrinde ne maçta parça yoktu; dokunuş "deneniyor" yazıp figürü değiştirmiyordu. Kart:
        // neden görünmediği, hangi modelde göründüğü ve tek dokunuşla geçiş. Aynı gün ikinci tur: Gerçekçi'ye beş ayakkabı modeli
        // geldi (rocketbox_aksesuar.py), tip ve saç sayfaları o modelde hiç gösterilmiyor (TekBedenKart). Kart artık yalnız
        // Sade'nin Ayakkabı sayfasında: model orada görünmez, renk görünür; seçim kaydedilir ve alınabilir.
        void ModelUyari(VisualElement c)
        {
            int i = System.Array.IndexOf(Ayar.MODELLER, Ayar.model); string ad = L.T(i >= 0 ? Ayar.MODEL_AD[i] : Ayar.model);
            string metin = L.T("Şu an {0} modeli seçili. Ayakkabı modeli Oyuncak 3B ve Gerçekçi'de görünür, rengi burada da; seçimin kaydedilir.", ad);
            var kart = Kart("deneme-kart"); c.Add(kart);
            kart.Add(Yazi(L.T("Ayakkabı modeli bu modelde görünmez"), 15, "kalin"));
            kart.Add(Yazi(metin, 13, "ipucu"));
            kart.Add(Dugme(L.T("Gerçekçi'ye geç"), () => { Ayar.model = "rocketbox"; Ayar.Kaydet(); root.ModelDegisti(); Tik(); SoyunmaGovdeKur(true); }, "dugme--birincil", L.T("Ayarlar › Oyuncu görünümü"), 16));
            Erisim.Etiket(kart, L.T("Ayakkabı modeli bu modelde görünmez") + ". " + metin, "metin");
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
            if (kat == Kozmetik.TIP) vt.deneTip = id; else if (kat == Kozmetik.SAC) vt.deneSac = id; else if (kat == Kozmetik.SAPKA) vt.deneSapka = id; else if (kat == Kozmetik.YUZ) vt.deneYuz = id; else vt.deneAyk = id;
            vt.Yenile(); SoyunmaGovdeKur(true);
        }
        void DenemeBirak(string kat)
        {
            var vt = root.vitrin; if (kat == Kozmetik.TIP) vt.deneTip = null; else if (kat == Kozmetik.SAC) vt.deneSac = null; else if (kat == Kozmetik.SAPKA) vt.deneSapka = null; else if (kat == Kozmetik.YUZ) vt.deneYuz = null; else vt.deneAyk = null;
            vt.Yenile(); Tik(); SoyunmaGovdeKur(true);
        }
        void KozSec(string kat, string id)
        {
            var gr = Ayar.gear; var vt = root.vitrin;
            if (kat == "ayakkabi") { if (!gr.Sec("ayakkabi", id)) return; vt.deneAyk = null; Ayar.GearHesap(); }
            else { Ayar.KOZ[kat] = id; if (kat == Kozmetik.TIP) vt.deneTip = null; else if (kat == Kozmetik.SAPKA) vt.deneSapka = null; else if (kat == Kozmetik.YUZ) vt.deneYuz = null; else vt.deneSac = null; }
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
                var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; dr.style.flexWrap = Wrap.Wrap; k.Add(dr); if (ik != null) { var i = Ikon(ik, 13, du == ModelDurum.Secili ? TURUNCU : SOLUK); i.style.marginRight = 4; dr.Add(i); }   // r55: " · deneniyor" dar kartta alta sarar
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
                var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; dr.style.flexWrap = Wrap.Wrap; k.Add(dr); if (ik != null) { var i = Ikon(ik, 11, du == ModelDurum.Secili ? TURUNCU : SOLUK); i.style.marginRight = 2; dr.Add(i); }   // r55: " · deneniyor" dar kartta alta sarar
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

        // ---- Ayakkabı tabanı (Oyun segmenti, 2026-10-30): seçili/denenen ayakkabı kartı (5 çubuk) + model satırları ----
        // Güç taşıyan yuva burada; figürde görünen model ve rengi Görünüm › Ayakkabı'da (yalnız görünüş).
        void TabanBolum(VisualElement c)
        {
            var gr = Ayar.gear; string dn = root.vitrin.deneAyk, gos = dn ?? gr.ayakkabi; int sv = gr.Seviye();
            var m = Gear.GEAR["ayakkabi"][gos]; var notr = Gear.AyakkabiCubuklar(Gear.GEAR["ayakkabi"][Gear.Varsayilan("ayakkabi")]);
            var du0 = gr.Durum("ayakkabi", gos);
            var kart = Kart("model-kart"); kart.AddToClassList(dn != null && du0 != ModelDurum.Secili ? "denenen" : "secili"); c.Add(kart);
            var bas = Satir("arali"); bas.pickingMode = PickingMode.Ignore; kart.Add(bas); bas.Add(Yazi(L.T(m.ad), 17, "kalin"));
            var dr0 = Satir("rakip-satir"); dr0.pickingMode = PickingMode.Ignore; bas.Add(dr0);
            if (du0 == ModelDurum.Secili) dr0.Add(Ikon(Ikonlar.Tik, 14, TURUNCU));
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
                if (du == ModelDurum.Secili) { dr.Add(Ikon(Ikonlar.Tik, 14, TURUNCU)); durum = L.T("Seçili"); }
                else if (du == ModelDurum.Kilitli) { dr.Add(Ikon(Ikonlar.Kilit, 14, SOLUK)); durum = L.T("Sv {0}", a.sv) + " · " + a.fiyat + " " + L.T("Tüy"); }
                else if (du == ModelDurum.Alinir) durum = a.fiyat + " " + L.T("Tüy"); else durum = a.fiyat == 0 ? L.T("Ücretsiz") : L.T("Senin");
                var dy = Yazi(durum, 12, du == ModelDurum.Secili ? "durum-gecti" : "durum-kilit"); dy.style.marginLeft = 4; dr.Add(dy);
                r.Add(Yazi(L.T(a.n), 12, "ipucu"));
                r.AddManipulator(new Clickable(() => KozDokun("ayakkabi", id)));
                Erisim.Etiket(r, L.T("Ayakkabı") + ", " + L.T(a.ad) + ", " + L.T(a.n) + ", " + durum + (dn == a.id ? ", " + L.T("deneniyor") : ""), "dugme", du == ModelDurum.Secili);
            }
        }

        // ---- Ayakkabı görünüşü (Görünüm): figürde hangi model görünsün (sahip olunanlar), rengi ----
        // "Tabanla aynı" varsayılan: eski kayıtta görünüş değişmez. Alınmamış model burada alınmaz (fiyatı ve gücü tabanda,
        // Oyun'da); satırı nedenini yazar ve Oyun'a götürür.
        void AyakkabiSayfa(VisualElement c)
        {
            var gr = Ayar.gear; int sv = gr.Seviye(); string gor = Ayar.AykGorunum;
            c.Add(Grup(L.T("Ayakkabı modeli · {0}", L.T(Gear.GEAR["ayakkabi"][gor].ad))));
            var cp = Satir("secim"); cp.style.flexWrap = Wrap.Wrap; c.Add(cp);
            cp.Add(Cip(L.T("Tabanla aynı"), Ayar.AYK_GOR == "taban", () => { Ayar.AYK_GOR = "taban"; Ayar.Kaydet(); root.vitrin.Yenile(); SoyunmaGovdeKur(true); }));
            foreach (var a in Gear.GEAR["ayakkabi"].Values)
            {
                string id = a.id; bool sahip = gr.Sahip("ayakkabi", id), sec = Ayar.AYK_GOR != "taban" && gor == id;
                if (sahip) cp.Add(Cip(L.T(a.ad), sec, () => { Ayar.AYK_GOR = id; Ayar.Kaydet(); root.vitrin.Yenile(); SoyunmaGovdeKur(true); }));
            }
            c.Add(Yazi(L.T("Sahip olduğun ayakkabıların görünüşünü taşırsın; tabanın gücü değişmez. Yeni model Oyun › Ayakkabı tabanında alınır."), 12, "soluk"));
            c.Add(Dugme(L.T("Ayakkabı tabanı · Oyun"), () => { Tik(); SoyunmaSegment(0); }, null, L.T("Güç, fiyat ve çubuklar orada"), 14));
            var l = new List<Ornek>(); foreach (var o in Karakter.Gorunum("ayakkabi")) l.Add(new Ornek { id = o.id, ad = o.ad, renk = o.renk, acik = sv >= o.sv, neden = L.T("Sv {0} gerekli", o.sv), kisa = L.T("Sv {0}", o.sv) });
            OrnekSira(c, L.T("Ayakkabı rengi"), l, Ayar.GORUNUM["ayakkabi"], id => { Ayar.GORUNUM["ayakkabi"] = id; Ayar.Kaydet(); }, true);
        }

        // ---- Şapka ve yüz aksesuarı: 2 sütunlu kart ızgarası (Tip sayfasıyla aynı akış: dokun = dene, sahipse seç) ----
        void KozIzgara(VisualElement c, string kat, string baslik, string dn)
        {
            var gr = Ayar.gear; string sec = Ayar.KOZ[kat];
            c.Add(Grup(L.T(baslik) + " · " + L.T(Kozmetik.Oge(kat, dn ?? sec).ad)));
            var iz = Satir("izgara"); iz.AddToClassList("koz-izgara"); c.Add(iz);
            foreach (var o in Kozmetik.Liste(kat))
            {
                var du = Kozmetik.Durum(gr, kat, o.id, sec); string id = o.id; Texture2D ik; string durum = KozDurum(kat, o, du, out ik);
                var k = Kart("koz-kart"); k.AddToClassList("dugme"); k.AddToClassList("koz-kart--kisa"); if (du == ModelDurum.Secili) k.AddToClassList("secili"); if (du == ModelDurum.Kilitli) k.AddToClassList("kilitli"); if (dn == o.id) k.AddToClassList("denenen");
                k.Add(Yazi(L.T(o.ad), 15, "kalin"));
                var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; dr.style.flexWrap = Wrap.Wrap; k.Add(dr); if (ik != null) { var i = Ikon(ik, 13, du == ModelDurum.Secili ? TURUNCU : SOLUK); i.style.marginRight = 4; dr.Add(i); }
                dr.Add(Yazi(durum, 12, du == ModelDurum.Secili ? "durum-gecti" : "durum-kilit"));
                if (dn == o.id && du != ModelDurum.Secili) dr.Add(Yazi(" · " + L.T("deneniyor"), 12, "durum-simdi"));
                k.AddManipulator(new Clickable(() => KozDokun(kat, id)));
                Erisim.Etiket(k, L.T(baslik) + ", " + L.T(o.ad) + ", " + durum + (dn == o.id ? ", " + L.T("deneniyor") : ""), "dugme", du == ModelDurum.Secili);
                iz.Add(k);
            }
            if (dn != null) { var o = Kozmetik.Oge(kat, dn); DenemeKart(c, kat, dn, o.ad, o.fiyat, o.sv, Kozmetik.Durum(gr, kat, dn, sec), Kozmetik.AlOlur(gr, kat, dn, sec)); }
        }
        void SapkaSayfa(VisualElement c)
        {
            KozIzgara(c, Kozmetik.SAPKA, "Şapka", root.vitrin.deneSapka);
            string sm = Ayar.KOZ[Kozmetik.SAC];
            if (Ayar.VaryantGorunur && Aksesuar.HACIMLI.Contains(sm)) c.Add(Yazi(L.T("{0} saç şapkanın altında kısa kesime döner; şapkayı çıkarınca geri gelir.", L.T(Kozmetik.Oge(Kozmetik.SAC, sm).ad)), 12, "soluk"));
            c.Add(Yazi(L.T("Şapkalar her oyuncu görünümünde takılır, Tüy ile bir kez alınır."), 12, "soluk"));
        }

        // ---- Ten: baştan hepsi açık (kimlik satılmaz) ----
        void TenSayfa(VisualElement c)
        {
            int sv = Ayar.gear.Seviye();
            KozIzgara(c, Kozmetik.YUZ, "Yüz aksesuarı", root.vitrin.deneYuz);
            if (!Ayar.VaryantGorunur && Ayar.model != "sade") return;   // Gerçekçi ve Tüycük'te ten sabit
            var l = new List<Ornek>(); foreach (var o in Karakter.Gorunum("ten")) l.Add(new Ornek { id = o.id, ad = o.ad, renk = o.renk, acik = sv >= o.sv, neden = L.T("Sv {0} gerekli", o.sv), kisa = L.T("Sv {0}", o.sv) });
            OrnekSira(c, L.T("Ten tonu"), l, Ayar.GORUNUM["ten"], id => { Ayar.GORUNUM["ten"] = id; Ayar.Kaydet(); }, true);
            c.Add(Yazi(L.T("Ten tonlarının hepsi baştan açık ve ücretsiz."), 12, "soluk"));
            // Sade (2026-10-27): saç biçimi tek, saç kabuğu düz renkli parça: saç rengi burada (Saç sayfası Sade'de yok).
            if (Ayar.model == "sade")
            {
                var sl = new List<Ornek>(); foreach (var o in Karakter.Gorunum("sac")) sl.Add(new Ornek { id = o.id, ad = o.ad, renk = o.renk, acik = sv >= o.sv, neden = L.T("Sv {0} gerekli", o.sv), kisa = L.T("Sv {0}", o.sv) });
                OrnekSira(c, L.T("Saç rengi"), sl, Ayar.GORUNUM["sac"], id => { Ayar.GORUNUM["sac"] = id; Ayar.Kaydet(); }, true);
            }
        }

        // ---- Kıyafet (Gerçekçi, Sade; 2026-10-27): görünüş kişiden değil kıyafetten gelir ----
        // Forma rengi (Görünüm'deki seçimin aynısı, tek kayıt), şort rengi (ücretsiz görünüş), forma kumaşı / kafa bandı /
        // bileklik (Ekipman yuvaları: güç taşırlar, burada yalnız ne takılı olduğu ve görünüşü yazar; seçimi yuva sayfası yapar,
        // fiyat ve çubuklar orada) ve raket (Görünüm). Hepsi vitrinde ve maçta figürde görünür.
        void KiyafetSayfa(VisualElement c)
        {
            var gr = Ayar.gear; int sv = gr.Seviye();
            var fr = new List<Ornek>(); foreach (var m in Gear.RENK.Values) fr.Add(new Ornek { id = m.id, ad = m.ad, renk = m.renk, acik = gr.RenkAcik(m.id, Ayar.sampiyon), neden = m.kilit ? L.T("Kariyeri bitirince açılır") : L.T("Sv {0} gerekli", m.sv), kisa = m.kilit ? "" : L.T("Sv {0}", m.sv) });
            // Tüycük (2026-10-29): forma rengi gövdenin tüy rengidir (karın onun açığı); şort ağı yok, şort satırı gösterilmez (yalnız
            // görünen seçim sunulur). Gerçekçi ve Sade'de satırlar aynen.
            bool tc = Ayar.model == "tuycuk";
            OrnekSira(c, L.T(tc ? "Tüy rengi (forma)" : "Forma rengi"), fr, gr.renk, id => { gr.renk = id; Ayar.GearHesap(); Ayar.Kaydet(); });
            if (Ayar.SortGorunur)
            {
                var sl = new List<Ornek>(); foreach (var o in Karakter.Gorunum("sort")) sl.Add(new Ornek { id = o.id, ad = o.ad, renk = o.renk, acik = sv >= o.sv, neden = L.T("Sv {0} gerekli", o.sv), kisa = L.T("Sv {0}", o.sv) });
                OrnekSira(c, L.T("Şort rengi"), sl, Ayar.GORUNUM["sort"], id => { Ayar.GORUNUM["sort"] = id; Ayar.Kaydet(); });
            }
            c.Add(Yazi(L.T("Renkler yalnız görünüştür, ücretsiz."), 12, "soluk"));
            c.Add(Grup(L.T("Takılı olanlar")));
            KiyafetSatir(c, "forma", L.T(Ayar.model == "rocketbox" ? "Formanın deseni ve parlaklığı kumaşa göre değişir." : tc ? "Kumaşın işlevi maçta; Tüycük'ün tüyü forma renginde, karnı onun açığı." : "Kumaşın işlevi maçta; bu modelde forma düz renk."));
            KiyafetSatir(c, "bant", L.T(tc ? "Perçemin altında görünür." : "Alında görünür."));
            KiyafetSatir(c, "bileklik", L.T("Raket kolunun bileğinde görünür."));
            c.Add(Yazi(L.T("Raketin görünüşü Raket kategorisinde."), 12, "soluk"));
        }
        // Ekipman yuvasının takılı modeli: ad + görünüş notu + yuva sayfasına geçiş (44 pt düğme; durum yazıyla, renkle değil).
        void KiyafetSatir(VisualElement c, string yk, string not)
        {
            var Y = Gear.YuvaOf(yk); if (Y == null) return; string id = Ayar.gear.Secili(Y.m); GearModel m; Gear.GEAR[Y.m].TryGetValue(id, out m);
            string ad = m != null ? L.T(m.ad) : id; bool yok = id == "yok";
            var k = Kart("ayk-satir"); c.Add(k);
            var b = Satir("arali"); b.pickingMode = PickingMode.Ignore; k.Add(b); b.Add(Yazi(L.T(Y.ad), 15, "kalin")); b.Add(Yazi(yok ? L.T("Takılı değil") : ad, 12, yok ? "durum-kilit" : "durum-gecti"));
            k.Add(Yazi(yok ? L.T("Atölye'den takınca figürde görünür.") : not, 12, "ipucu"));
            k.Add(Dugme(L.T("{0} · Atölye", L.T(Y.ad)), () => { Tik(); YuvaAc(Y.k); }, null, null, 14));
            Erisim.Etiket(k, L.T(Y.ad) + ", " + (yok ? L.T("Takılı değil") : ad) + ", " + (yok ? L.T("Atölye'den takınca figürde görünür.") : not), "metin");
        }
    }
}
