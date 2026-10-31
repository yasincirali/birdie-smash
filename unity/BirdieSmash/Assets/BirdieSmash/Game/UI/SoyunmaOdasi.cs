// Soyunma odası (eski Ekipman paneli, 2026-10-26). Adım 1'de bugünkü IMGUI paneliyle birebir taşındı; adım 4 (ekipman v2) ve
// adım 6 (karakter) ile tasarım §2.3 yerleşimine geçti:
//   Üst: 360° vitrin (Vitrin.rt): sürükle = döndür (bırakınca yavaşlar), ‹ › 15° (raket görünümünde 20°), "Raketi incele" /
//   "Oyuncuya dön"; rakete dokunmak (sürüklemeden: < 8 pt, < 400 ms, kutunun %14'ü yarıçap) raket görünümünü açar.
//   Altında segment [Ekipman | Tarz | Görünüm] (vitrinle birlikte sabit, gövde kayar):
//   · Ekipman: seviye + Tüy + XP; takımın 5 çubuğu (Güç, Kontrol, Tatlı nokta, Çabukluk, Dayanıklılık; Core/Gear.Cubuklar);
//     altı yuva kartı (3×2, büyük yazıda alt alta): yuva, model, Sv n/10, yükseltme ya da alınacak model varsa ▲ "Hazır".
//     Yuvaya dokunmak yuva sayfasını alt sayfa olarak açar; Eşit şartlar.
//   · Yuva sayfası (alt sayfa): seviye bölmesi + Yükselt (kapı varsa nedeni yazar, düğme pasif ama okunur) + önizleme
//     (gri = şimdi, yeşil uç = yükseltince); Kordaj'da tel gerginliği kaydırıcısı (+ − düğmeleri; kaydırıcı takımın çubuklarını
//     canlı oynatır); modeller: ad, "özel" satırı, o model takılınca 5 çubuk ve nötr modele göre ▲/▼ (artısı ve eksisi her
//     zaman görünür), durum (Seçili ✓ / Seç / Al · fiyat / Sv n gerekli). Toplam "güç puanı" bilerek yok (§4.1).
//     Neden yatay kaydırmalı kart değil (tasarım "yatay kartlar" diyordu): Dynamic Type ×2'de kart yüksekliği ikiye katlanır
//     ve ekran dışındaki kart VoiceOver sırasında da görünmez kalır; dikey liste her ölçekte okunur.
//   · Tarz: Dengeli + 4 tarz (Core/Karakter); artı ve eksi satırı ▲/▼ ile, seçili ✓ + "Seçili", kilitli kilit + "Sv n
//     gerekli". Ücretsiz, maç dışında her an değişir, güç değil kimlik.
//   · Görünüm: forma rengi, ten tonu (baştan hepsi açık), saç, ayakkabı, raket kaplaması; raketin grip/kordaj/desen renkleri
//     (yalnız süs). Raket görünümüne geçince Görünüm açılır (raket renkleri orada). Seçili örnek ✓ ve kalın çerçeve, grup
//     başlığında seçilenin adı (yalnız renkle değil); kilitli örnek kilit + "Sv n".
// Seçimde titreşim; raket/telde deneme smacı (sesi ve titreşimi) aynı.
// 2026-10-26 karakter tasarımı (yasin: tip, garip saçlar, Tüy ile saç, özelliğine ve fiyatına göre ayakkabı; "yakın açıda
// görüp"): segment dörde çıktı [Ekipman | Karakter | Tarz | Görünüm] (sıra: oyuncu önce kendini, sonra oyun biçimini
// seçer). Karakter gövdesi KarakterOdasi.cs'de. Vitrinin sol altında yakın plan düğmeleri Yüz · Ayak · Tutuş (44 pt;
// seçili ✓ + "seçili" etiketi; aynısına ikinci dokunuş Tüm boy). Ten ve saç rengi Görünüm'den Karakter'e taşındı (kişiye
// ait olanlar bir yerde); Görünüm'de forma rengi ve raket kalır, raket bölümüne tutuş görünümü (Forehand · Backhand · File
// · Servis: kamera ele yaklaşır, figür o vuruşun temas pozunda durur) eklendi. Dizin sabit (EkranTuru ve RaketGorunum 2 =
// Görünüm kullanır), görünen sıra SEG_SIRA.
// 2026-10-30 (yasin onayı, öneri artifact 2dg4HEiYCrqaPdwzHKP12S: "oyunu etkileyen ve etkilemeyen ayrı dursun"): iki segment
// [Oyun | Görünüm]. Oyun: tarz en üstte (kimlik, ücretsiz), sonra takımın çubukları, yuvalar ve ayakkabı tabanı (Gear'in
// ayakkabı yuvası: güç taşır). Görünüm: Karakter gövdesi (KarakterOdasi.cs) alt sayfalarla; başta "Oyunu etkilemez" rozeti,
// hiçbir çubuk yok. Ayakkabının modeli (figürde görünen) Görünüm'de, tabanı Oyun'da (Ayar.AYK_GOR; "taban" = aynı model).
// Dizinler korunur (EkranTuru ve eski çağrılar): 0 Oyun, 1 Tarz → Oyun, 2 eski Görünüm (raket) → Görünüm › Renk ve raket,
// 3 Görünüm.
// 2026-10-30 (yasin "Önerilerini yap"; öneri artifact PN6wKhLj3ymqgEqLGzgWvZ, seçenek A + bölge kamerası): Oyun ve Görünüm iki ayrı
// sekme oldu. Araştırma: NBA 2K, CSR Racing 2, Golf Clash, Forza görünüşü ve gücü ayrı yerde tutar; HIG: segment yakın
// seçenekler içindir, farklı işler ayrı hedeftir. Sekme.Soyunma = Dolap: üstte vitrin, altında sabit kategori şeridi (eski
// segmentin yeri), gövde kayar. Sekme.Atolye: vitrinsiz; seviye, tarz, takımın çubukları, yuvalar, taban. Yuva sayfası
// (alt sayfa) Atölye'nin üstünde açılır. Eski dizinli çağrılar (SoyunmaSegment 0–3) doğru sekmeye yönlenir.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        Image vitrinIm; Label vitrinYazi, telYazi, telNot; VisualElement soyunmaGovde, soyunmaSegment; ScrollView soyunmaKaydir;
        Dictionary<string, float> yukOnce; string yukK; bool telDegisti;
        Vector2 vBas; float vBasT, vSonX, vSonT; bool vBasili;
        int soyunmaSeg; string yuvaAcik = "raket";
        // Canlı çubuklar (tel kaydırıcısı sürüklenirken yeniden kurmadan güncellenir): dolgu + değer yazısı.
        readonly List<VisualElement> canliDolgu = new List<VisualElement>(); readonly List<Label> canliDeger = new List<Label>();
        public const int SEG_KARAKTER = 3;
        VisualElement vitrinOdak;

        // EkranTuru ve maç sonu "Ekipmanı yükselt" için açık yöntemler.
        public void SoyunmaSegment(int i)
        {
            int y = Mathf.Clamp(i, 0, 3);
            if (y <= 1) { if (gez.sekme != Sekme.Atolye) SekmeAc(Sekme.Atolye); return; }
            if (y == 2) karAlt = KAR_RAKET;
            if (gez.sekme != Sekme.Soyunma) SekmeAc(Sekme.Soyunma);
            KarakterOdakAyarla(); KategoriKur(); SoyunmaGovdeKur(false);
        }
        // Yuva sayfası Atölye'nin işi: Dolap'tan (takılı kıyafet satırı) çağrılınca önce Atölye'ye geçilir.
        public void YuvaAc(string k)
        {
            if (Gear.YuvaOf(k) == null) return; yuvaAcik = Gear.YuvaOf(k).k;
            if (gez.sekme != Sekme.Atolye) SekmeAc(Sekme.Atolye);
            var once = gez.sekme; gez.Ac(Gezinti.YUVA); Uygula(once, true);
        }

        void SoyunmaKur(VisualElement k)
        {
            k.Clear(); var vt = root.vitrin;
            var ust = new VisualElement(); ust.AddToClassList("vitrin-alan"); k.Add(ust);
            vitrinIm = new Image { image = vt.rt, scaleMode = ScaleMode.ScaleAndCrop }; vitrinIm.AddToClassList("vitrin"); ust.Add(vitrinIm);
            Erisim.Etiket(vitrinIm, L.T("Oyuncun, 360 derece görünüm"), "resim");
            vitrinIm.RegisterCallback<PointerDownEvent>(VitrinBas); vitrinIm.RegisterCallback<PointerMoveEvent>(VitrinSurukle); vitrinIm.RegisterCallback<PointerUpEvent>(VitrinBirak);
            // 2026-10-31 (Unity incelemesi O4): sürükleme iptal edilirse (sistem hareketi, ikinci parmak, yakalama kaybı) PointerUp
            // gelmiyordu; suruk açık kalıp vitrin kendiliğinden dönmeyi bırakıyordu. İptal ve yakalama kaybı da bırakma sayılır.
            vitrinIm.RegisterCallback<PointerCancelEvent>(e => VitrinIptal()); vitrinIm.RegisterCallback<PointerCaptureOutEvent>(e => VitrinIptal());
            vitrinYazi = Yazi("", VitrinPx(12), "vitrin-yazi"); ust.Add(vitrinYazi);
            // 2026-10-26 (r13 incelemesi): yakın plan düğmeleri ve oklar tek bir alt sütunda (Yüz·Ayak·Tutuş üstte, oklar altta).
            // Eskiden ikisi ayrı ayrı mutlak konumdaydı (odak satırı alttan 60 pt); büyük yazıda "Raketi incele" iki satıra
            // çıkınca oklar satırı uzuyor, düğmeler üst üste biniyordu. Sütunda yükseklikler ne olursa olsun çakışmazlar.
            // Kap ve satırlar dokunuşu geçirir (vitrini sürükleyerek döndürmek boşluklarda da çalışsın).
            var alt = new VisualElement(); alt.AddToClassList("vitrin-alt"); alt.pickingMode = PickingMode.Ignore; ust.Add(alt);
            vitrinOdak = Satir("vitrin-odak"); vitrinOdak.pickingMode = PickingMode.Ignore; alt.Add(vitrinOdak);
            var ok = Satir("vitrin-oklar"); ok.pickingMode = PickingMode.Ignore; alt.Add(ok);
            var sol = new VisualElement(); sol.AddToClassList("dugme"); sol.AddToClassList("ikon-dugme"); sol.Add(Ikon(Ikonlar.Geri, 20, Color.white)); sol.AddManipulator(new Clickable(() => VitrinCevir(-1))); Erisim.Etiket(sol, L.T("Sola çevir"), "dugme"); ok.Add(sol);
            var orta = Dugme("", () => RaketGorunum(vt.hedefGec < 1), "vitrin-gecis", null, VitrinPx(16)); Sigdir(orta, .75f); ok.Add(orta);   // r14: ×2'de "Inspect racket" iki satıra çıkıp önizlemeyi kapatıyordu
            var sag = new VisualElement(); sag.AddToClassList("dugme"); sag.AddToClassList("ikon-dugme"); sag.Add(Ikon(Ikonlar.Ileri, 20, Color.white)); sag.AddManipulator(new Clickable(() => VitrinCevir(1))); Erisim.Etiket(sag, L.T("Sağa çevir"), "dugme"); ok.Add(sag);
            OdakDugmeleriKur();
            // Yakın plan kadrajı kutunun en-boyuna ve alttaki düğmelerin payına göre (Vitrin.Uzaklik).
            ust.RegisterCallback<GeometryChangedEvent>(e => VitrinKutuYaz(ust, alt)); alt.RegisterCallback<GeometryChangedEvent>(e => VitrinKutuYaz(ust, alt));
            soyunmaSeg = SEG_KARAKTER; soyunmaSegment = Satir("segment"); soyunmaSegment.AddToClassList("kategori-ray"); k.Add(soyunmaSegment); KategoriKur();
            soyunmaKaydir = Kaydirma(k); soyunmaKaydir.AddToClassList("govde-kaydir"); soyunmaKaydir.AddToClassList("soyunma-kaydir");
            soyunmaGovde = soyunmaKaydir.contentContainer; soyunmaGovde.AddToClassList("sekme-icerik");
            if (root.vitrin.odak == Vitrin.TAM) KarakterOdakAyarla();   // sekmeye dönüşte (Kapat odağı ve raket görünümünü sıfırladı)
            SoyunmaGovdeKur(false);
        }

        // Atölye: oyunu etkileyen her şey (eski Oyun segmenti). Vitrin yok: Golf Clash'teki gibi kart + çubuk + "yükseltince".
        VisualElement atolyeGovde; ScrollView atolyeKaydir;
        void AtolyeKur(VisualElement k)
        {
            k.Clear(); atolyeKaydir = Kaydirma(k); atolyeKaydir.AddToClassList("govde-kaydir");
            atolyeGovde = atolyeKaydir.contentContainer; atolyeGovde.AddToClassList("sekme-icerik"); AtolyeGovdeKur(false);
        }
        void AtolyeGovdeKur(bool yerKoru)
        {
            if (atolyeGovde == null) return; Vector2 of = yerKoru && atolyeKaydir != null ? atolyeKaydir.scrollOffset : Vector2.zero;
            var c = atolyeGovde; c.Clear(); canliDolgu.Clear(); canliDeger.Clear(); EkipmanGovde(c); c.Add(Bosluk(20));
            if (of.y > 0) { atolyeKaydir.scrollOffset = of; var sk = atolyeKaydir; sk.schedule.Execute(() => sk.scrollOffset = of).StartingIn(33); }
        }

        // Vitrinin üstündeki yazılar (köşe yazısı, Raketi incele, Yüz·Ayak·Tutuş) yazı çarpanını en çok 1,35'e kadar izler.
        // Neden (2026-10-26, r13–r14): ×2'de bu yazılar 3D önizlemenin yarısını kapatıyordu; kutu büyük yazıda zaten %30 boy.
        // r14'te 1,6 ile de düğmeler yüzün üstünü örttü. 1,35 büyük yazı eşiği (UiKok .buyuk-yazi); asıl içerik (gövde) tam
        // çarpanla büyür, düğmelerin dokunma alanı 44 pt. Bilinçli HIG sapması (sistemdeki görüntü üstü denetimler gibi).
        float VitrinPx(float px) { return K > 1.35f ? px * 1.35f / K : px; }
        void VitrinKutuYaz(VisualElement ust, VisualElement alt)
        {
            var vt = root.vitrin; float w = ust.layout.width, h = ust.layout.height; if (vt == null || float.IsNaN(w) || float.IsNaN(h) || h < 1) return;
            vt.kutuOran = w / h; vt.kutuAlt = float.IsNaN(alt.layout.height) ? .35f : Mathf.Clamp01((alt.layout.height + 8) / h);
            // RT kutunun ekran pikseli boyunda (panel birimi × ekran/panel oranı): büyütme bulanıklığı ve kırpma yok
            var kok = ust.panel != null ? ust.panel.visualTree : null; float pp = kok != null && kok.layout.width > 1 ? Screen.width / kok.layout.width : 1;
            vt.Cozunurluk(w * pp, h * pp); if (vitrinIm != null && vitrinIm.image != vt.rt) vitrinIm.image = vt.rt;
        }

        public void RaketIncele(bool ac) { RaketGorunum(ac); }   // EkranTuru
        // "Raketi incele" / "Oyuncuya dön": Raket kategorisine geçer (tezgâh) ya da az önceki kategoriye döner.
        int raketOnce = KAR_KIYAFET;
        void RaketGorunum(bool ac)
        {
            if (ac == (karAlt == KAR_RAKET)) return;
            if (ac) { raketOnce = karAlt; KategoriSec(KAR_RAKET); } else KategoriSec(raketOnce == KAR_RAKET ? KAR_KIYAFET : KarGecerli(raketOnce));
        }

        void VitrinCevir(int yon)
        {
            var vt = root.vitrin;
            if (vt.hedefGec >= 1) { vt.rv = 0; vt.ra = Mathf.Round(vt.ra / 10) * 10 + 20 * yon; } else { vt.v = 0; vt.a = Mathf.Round(vt.a / 15) * 15 + 15 * yon; }
            Tik();
        }
        void VitrinBas(PointerDownEvent e)
        {
            var vt = root.vitrin; vt.suruk = true; vBasili = true; vSonX = e.position.x; vSonT = Time.unscaledTime; vBas = e.position; vBasT = vSonT;
            vitrinIm.CapturePointer(e.pointerId); e.StopPropagation();
        }
        void VitrinSurukle(PointerMoveEvent e)
        {
            var vt = root.vitrin; if (!vBasili) return; float w = Mathf.Max(1, vitrinIm.layout.width);
            float dx = e.position.x - vSonX, dt = Mathf.Max(.008f, Time.unscaledTime - vSonT); float deg = dx / w * 360; vSonX = e.position.x; vSonT = Time.unscaledTime;
            if (vt.hedefGec >= 1) { vt.ra += deg; vt.rv = deg / dt; } else { vt.a += deg; vt.v = deg / dt; }
        }
        void VitrinIptal() { if (!vBasili) return; vBasili = false; root.vitrin.suruk = false; }
        void VitrinBirak(PointerUpEvent e)
        {
            var vt = root.vitrin; if (!vBasili) return; vBasili = false; vt.suruk = false; vitrinIm.ReleasePointer(e.pointerId);
            Vector2 vp; var lp = vitrinIm.WorldToLocal(e.position); var lb = vitrinIm.WorldToLocal(vBas);
            bool dokunus = (lp - lb).magnitude < 8 && Time.unscaledTime - vBasT < .4f;
            float W = vitrinIm.layout.width, H = vitrinIm.layout.height; float s = Mathf.Max(W / vt.rt.width, H / vt.rt.height), dw = vt.rt.width * s, dh = vt.rt.height * s;
            // ScaleAndCrop: dokunuşun RT içindeki yeri (viewport 0..1)
            var dv = new Vector2((lp.x - (W - dw) / 2) / dw, 1 - (lp.y - (H - dh) / 2) / dh);
            if (dokunus && vt.hedefGec < 1 && vt.RaketYeri(out vp))
            {
                var gp = new Vector2((W - dw) / 2 + vp.x * dw, (H - dh) / 2 + (1 - vp.y) * dh);
                if ((lp - gp).magnitude < W * .14f) RaketGorunum(true);
            }
            else if (dokunus && vt.hedefGec >= 1) { string p = vt.ParcaYeri(dv, W * .12f / dw); if (p != null && p != raketParca) RaketParcaSec(p); }   // tezgâhta parçaya dokunmak onu seçer
        }

        void SoyunmaGovdeKur(bool yerKoru)
        {
            // Atölye ve Dolap aynı seçim akışlarını paylaşır (ModelSec, TelBitti, yükselt): görünen yenilenir, öteki kirli kalır.
            if (gez.sekme == Sekme.Atolye) { AtolyeGovdeKur(yerKoru); kirli[(int)Sekme.Soyunma] = true; return; }
            kirli[(int)Sekme.Atolye] = true;
            if (soyunmaGovde == null) return; var vt = root.vitrin; bool raketG = vt.hedefGec >= 1;
            Vector2 of = yerKoru && soyunmaKaydir != null ? soyunmaKaydir.scrollOffset : Vector2.zero;
            int od = vt.odak;
            string rp = vt.Parca; string rpAd = rp == "kafa" ? "Kafa şekli" : rp == "cerceve" ? "Çerçeve" : rp == "saft" ? "Şaft" : rp == "grip" ? "Grip" : rp == "kordaj" ? "Kordaj" : null;
            vitrinYazi.text = raketG && rpAd != null ? L.T("Yakın plan: {0}", L.T(rpAd)) : L.T(raketG ? "Raketin, 360 derece görünüm" : od == Vitrin.YUZ ? "Yakın plan: yüz ve saç" : od == Vitrin.AYAK ? "Yakın plan: ayakkabılar" : od == Vitrin.TUTUS ? "Yakın plan: tutuş" : "Oyuncun, 360 derece görünüm");
            OdakDugmeleriKur();
            var gecis = vitrinIm.parent.Q<VisualElement>(null, "vitrin-gecis");
            if (gecis != null) { var gy = gecis.Q<Label>(); string gt = L.T(raketG ? "Oyuncuya dön" : "Raketi incele"); if (gy != null) { gy.text = gt; var gc_ = gecis; gecis.schedule.Execute(() => SigdirOlc(gc_, gy, Mathf.Round(VitrinPx(16) * K), .75f)); } Erisim.Etiket(gecis, gt, "dugme"); }
            var c = soyunmaGovde; c.Clear();
            KarakterGovde(c); c.Add(Bosluk(20));
            if (of.y > 0) { soyunmaKaydir.scrollOffset = of; var sk = soyunmaKaydir; sk.schedule.Execute(() => sk.scrollOffset = of).StartingIn(33); }
        }

        void SeviyeSatiri(VisualElement c)
        {
            var gr = Ayar.gear; int L_, kalan, gerek; gr.OyuncuSv(out L_, out kalan, out gerek);
            var sv = Satir("arali"); c.Add(sv); sv.Add(Yazi(L.T("Seviye {0}", L_), 16, "kalin")); sv.Add(Yazi(L.T("{0} Tüy", gr.eko.tuy), 16, "kalin"));
            c.Add(Cubuk(gerek > 0 ? kalan / (float)gerek : 1, CAMGOBEGI));
            c.Add(Yazi(gerek > 0 ? L.T("Sonraki seviyeye {0} XP · maç kazandıkça Tüy ve XP gelir", gerek - kalan) : L.T("En yüksek seviye"), 12, "soluk"));
        }

        // ---- Oyun segmenti: tarz, takım, yuvalar, ayakkabı tabanı ----
        void EkipmanGovde(VisualElement c)
        {
            var gr = Ayar.gear; SeviyeSatiri(c);
            c.Add(Grup(L.T("Oyun tarzı"))); TarzGovde(c);
            c.Add(Grup(L.T("Takımın")));
            var kc = Kart("cubuk-kart"); c.Add(kc); CubukKart(kc, Gear.Cubuklar(Ayar.GSE), null, true);
            c.Add(Grup(L.T("Yuvalar"))); var iz = Satir("izgara"); iz.AddToClassList("yuva-izgara"); c.Add(iz);
            foreach (var Y in Gear.YUVA) iz.Add(YuvaKare(Y));
            c.Add(Cip(L.T("Eşit şartlar"), gr.eko.esit, () => { gr.eko.esit = !gr.eko.esit; Ayar.GearHesap(); Ayar.Kaydet(); SoyunmaGovdeKur(true); }, L.T("Serbest maçta yükseltmeler sayılmaz")));
            c.Add(Grup(L.T("Ayakkabı tabanı"))); TabanBolum(c);
            c.Add(Yazi(L.T("Çubuklar ekipmanı anlatır, 50 nötr. Oyun tarzı çubuklara girmez. Görünüş Dolap'ta, oyunu etkilemez."), 12, "soluk"));
        }

        // Yuva kartı: dokununca yuva sayfası. "Hazır" = yükseltme alınabilir ya da seviyesi gelmiş, Tüy'ü yeten model var.
        VisualElement YuvaKare(Yuva Y)
        {
            var gr = Ayar.gear; int sv = gr.eko.sv[Y.k], f, kp; bool hazir = gr.YukseltOlur(Y.k, out f, out kp) || gr.AlinabilirVar(Y.m);
            var m = Gear.GEAR[Y.m][gr.Secili(Y.m)];
            var kart = Kart("yuva-kare"); kart.AddToClassList("dugme");
            kart.Add(Yazi(L.T(Y.ad), 12, "soluk")); kart.Add(Yazi(L.T(m.ad), 15, "kalin"));
            var bol = Satir("seviye-bolme"); bol.pickingMode = PickingMode.Ignore; kart.Add(bol);
            for (int i = 0; i < 10; i++) { var b = new VisualElement(); b.AddToClassList("bolme"); b.AddToClassList("bolme--kucuk"); if (i < sv) b.AddToClassList("bolme--dolu"); b.pickingMode = PickingMode.Ignore; bol.Add(b); }
            var alt = Satir("rakip-satir"); alt.pickingMode = PickingMode.Ignore; kart.Add(alt); alt.Add(Yazi(L.T("Kademe {0}/10", sv), 12, "soluk"));
            if (hazir) { var h = Ikon(Ikonlar.Yukari, 12, TURUNCU); h.style.marginLeft = 6; alt.Add(h); var hy = Yazi(L.T("Hazır"), 12, "durum-gecti"); hy.style.marginLeft = 2; alt.Add(hy); }
            kart.AddManipulator(new Clickable(() => { Tik(); YuvaAc(Y.k); }));
            Erisim.Etiket(kart, L.T(Y.ad) + ", " + L.T(m.ad) + ", " + L.T("Kademe {0}/10", sv) + (hazir ? ", " + L.T("yükseltme ya da yeni model hazır") : ""), "dugme");
            return kart;
        }

        // 5 çubuk: ad, iz (50'de nötr çizgisi), değer; karsi verildiyse ▲/▼ (nötr modele göre). canli: tel kaydırıcısı günceller.
        void CubukKart(VisualElement c, int[] v, int[] karsi, bool canli, string[] adlar = null)
        {
            var AD = adlar ?? Gear.CUBUK_AD;
            for (int i = 0; i < 5; i++)
            {
                var r = Satir("stat-satir"); r.pickingMode = PickingMode.Ignore; c.Add(r);
                r.Add(Yazi(L.T(AD[i]), 12, "stat-ad"));
                var iz = new VisualElement(); iz.AddToClassList("stat-iz"); iz.pickingMode = PickingMode.Ignore; r.Add(iz);
                var dl = new VisualElement(); dl.AddToClassList("stat-dolu"); dl.style.width = Length.Percent(v[i]); dl.pickingMode = PickingMode.Ignore; iz.Add(dl);
                var orta = new VisualElement(); orta.AddToClassList("stat-orta"); orta.pickingMode = PickingMode.Ignore; iz.Add(orta);
                var dg = Yazi(v[i].ToString(), 12, "stat-deger"); r.Add(dg);
                int fark = karsi != null ? v[i] - karsi[i] : 0; string yon = "";
                var isk = new VisualElement(); isk.AddToClassList("stat-isaret"); isk.pickingMode = PickingMode.Ignore; r.Add(isk);
                if (fark != 0) { isk.Add(Ikon(fark > 0 ? Ikonlar.Yukari : Ikonlar.Asagi, 12, fark > 0 ? TURUNCU : MERCAN)); yon = ", " + L.T(fark > 0 ? "artı {0}" : "eksi {0}", Mathf.Abs(fark)); }
                Erisim.Etiket(r, L.T(AD[i]) + " " + v[i] + yon, "metin");
                if (canli) { canliDolgu.Add(dl); canliDeger.Add(dg); }
            }
        }
        void CanliCubukYaz()
        {
            if (canliDolgu.Count != 5) return; var v = Gear.Cubuklar(Ayar.GSE);
            for (int i = 0; i < 5; i++) { canliDolgu[i].style.width = Length.Percent(v[i]); canliDeger[i].text = v[i].ToString(); var b = canliDeger[i].parent.userData as ErisimBilgi; if (b != null) b.etiket = L.T(Gear.CUBUK_AD[i]) + " " + v[i]; }
        }

        // ---- yuva sayfası (alt sayfa) ----
        void YuvaSayfaKur()
        {
            var Y = Gear.YuvaOf(yuvaAcik) ?? Gear.YUVA[0]; var gr = Ayar.gear; int L_ = gr.Seviye();
            var c = AltSayfaKur(L.T(Y.ad)); canliDolgu.Clear(); canliDeger.Clear();
            YuvaKur(c, Y, L_);
            if (Y.k == "tel") TelKur(c);
            c.Add(Grup(L.T("Takımın şimdi"))); var kc = Kart("cubuk-kart"); c.Add(kc); CubukKart(kc, Gear.Cubuklar(Ayar.GSE), null, true);
            c.Add(Grup(L.T("Modeller")));
            var notr = gr.CubuklarIle(Y.m, Gear.Varsayilan(Y.m));
            foreach (var m in Gear.GEAR[Y.m].Values) ModelKart(c, Y, m, notr);
            if (gr.eko.esit) c.Add(Yazi(L.T("Eşit şartlar açık: serbest maçta sayılmaz, kariyerde sayılır"), 12, "soluk"));
        }

        void ModelKart(VisualElement c, Yuva Y, GearModel m, int[] notr)
        {
            var gr = Ayar.gear; var du = gr.Durum(Y.m, m.id); string id = m.id;
            var kart = Kart("model-kart"); if (du == ModelDurum.Secili) kart.AddToClassList("secili"); if (du == ModelDurum.Kilitli) kart.AddToClassList("kilitli"); c.Add(kart);
            var bas = Satir("arali"); kart.Add(bas); bas.Add(Yazi(L.T(m.ad), 16, "kalin"));
            var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; bas.Add(dr); string durum;
            if (du == ModelDurum.Secili) { dr.Add(Ikon(Ikonlar.Tik, 14, TURUNCU)); durum = L.T("Seçili"); }
            else if (du == ModelDurum.Kilitli) { dr.Add(Ikon(Ikonlar.Kilit, 14, SOLUK)); durum = L.T("Sv {0} gerekli", m.sv); }
            else if (du == ModelDurum.Alinir) durum = m.fiyat + " " + L.T("Tüy"); else durum = L.T("Senin");
            var dy = Yazi(durum, 12, du == ModelDurum.Secili ? "durum-gecti" : "durum-kilit"); dy.style.marginLeft = 4; dr.Add(dy);
            var ns = Yazi(L.T(m.n), 13, "ipucu"); kart.Add(ns);
            if (m.renk != null) { var rr = Satir("rakip-satir"); rr.pickingMode = PickingMode.Ignore; kart.Add(rr); var n = new VisualElement(); n.AddToClassList("renk-nokta"); n.style.backgroundColor = Kit.Hex(m.renk); rr.Add(n); rr.Add(Yazi(L.T("Görünüm rengi"), 11, "soluk")); }
            CubukKart(kart, gr.CubuklarIle(Y.m, m.id), notr, false);
            Erisim.Etiket(kart, L.T(m.ad) + ", " + L.T(m.n) + ", " + durum, "metin", du == ModelDurum.Secili);
            if (du == ModelDurum.Acik) kart.Add(Dugme(L.T("Seç"), () => ModelSec(Y, id), "model-dugme"));
            else if (du == ModelDurum.Alinir)
            {
                if (gr.AlOlur(Y.m, id)) kart.Add(Dugme(L.T("Al · {0}", m.fiyat + " " + L.T("Tüy")), () => { if (gr.Al(Y.m, id)) { root.sfx.Yukselt(); Yerel.Hap("flow"); ModelSec(Y, id); } }, "dugme--birincil", L.T("Bir kez alınır, hep senin"), 16));
                else kart.Add(Pasif(L.T("Al · {0}", m.fiyat + " " + L.T("Tüy")), L.T("{0} Tüy daha gerekli", m.fiyat - gr.eko.tuy)));
            }
            else if (du == ModelDurum.Kilitli) kart.Add(Pasif(L.T("Sv {0} gerekli", m.sv), L.T("Seviye atladıkça açılır")));
        }

        void ModelSec(Yuva Y, string id)
        {
            var gr = Ayar.gear; if (!gr.Sec(Y.m, id)) return;
            Ayar.GearHesap(); Ayar.Kaydet(); root.vitrin.Yenile(); if (Y.m == "raket" || Y.m == "kordaj") { root.sfx.Deneme(); Yerel.Vurus("smash", "perfect", false); } else root.sfx.Tik();
            ustKirli = true; UstKur(); Degisti(); SoyunmaGovdeKur(true);
        }

        void YuvaKur(VisualElement c, Yuva Y, int L_)
        {
            var gr = Ayar.gear; int sv = gr.eko.sv[Y.k]; bool tam = sv >= 10; int f = tam ? 0 : Gear.YUK_FIYAT[sv - 1], gk = tam ? 0 : Gear.YUK_SV[sv - 1];
            bool kilit = !tam && L_ < gk, az = !tam && gr.eko.tuy < f;
            var kart = Kart("yuva"); c.Add(kart);
            var r = Satir("yuva-satir"); kart.Add(r);
            var sol = new VisualElement(); sol.AddToClassList("yuva-sol"); sol.pickingMode = PickingMode.Ignore; r.Add(sol);
            sol.Add(Yazi(L.T("Yuva kademesi"), 15, "kalin"));
            var bol = Satir("seviye-bolme"); bol.pickingMode = PickingMode.Ignore; sol.Add(bol);
            for (int i = 0; i < 10; i++) { var b = new VisualElement(); b.AddToClassList("bolme"); if (i < sv) b.AddToClassList("bolme--dolu"); b.pickingMode = PickingMode.Ignore; bol.Add(b); }
            sol.Add(Yazi(sv + "/10 · " + L.T(Y.n), 12, "soluk"));
            string ne = tam ? L.T("Tam") : kilit ? L.T("Sv {0} gerekli", gk) : az ? f + " " + L.T("Tüy") : L.T("Yükselt · {0}", f + " " + L.T("Tüy"));
            VisualElement dg;
            if (tam || kilit || az) dg = Pasif(ne);   // kapı varsa nedeni yazar, düğme pasif ama okunur (§2.3)
            else dg = Dugme(ne, () =>
            {
                var once = gr.YukDegerler(Y.k, 0);
                if (gr.Yukselt(Y.k)) { Ayar.GearHesap(); Ayar.Kaydet(); root.sfx.Yukselt(); Yerel.Hap("flow"); root.vitrin.Parlat(Y.k); yukOnce = once; yukK = Y.k; ustKirli = true; UstKur(); Degisti(); SoyunmaGovdeKur(true); }
            }, "dugme--birincil");
            dg.AddToClassList("yuva-dugme"); r.Add(dg);
            Erisim.Etiket(kart, L.T(Y.ad) + ", " + sv + "/10", "metin");
            if (!tam) Onizle(kart, Y.k);
        }

        // Yükseltme önizlemesi (HTML yukOnizle): çubuk 1. seviyeden 10. seviyeye; gri = şimdi, yeşil uç = yükseltince.
        void Onizle(VisualElement kart, string k)
        {
            var gr = Ayar.gear; int sv = gr.eko.sv[k]; var su = gr.YukDegerler(k, 0); var son = gr.YukDegerler(k, 1); var ta = gr.YukDegerler(k, 1 - sv); var tav = gr.YukDegerler(k, 10 - sv);
            bool var_ = false; bool kay = yukK == k && yukOnce != null && !Ayar.reduce;
            foreach (var a in Gear.YUK_ETKI[k])
            {
                float d = son[a] / su[a] - 1; if (d < .002f) continue; var_ = true; string aa = a;
                System.Func<float, float> W_ = v => Mathf.Clamp(4 + (v - ta[aa]) / (Mathf.Abs(tav[aa] - ta[aa]) > 1e-6f ? tav[aa] - ta[aa] : 1) * 96, 4, 100);
                var r = Satir("onizle"); r.pickingMode = PickingMode.Ignore; kart.Add(r);
                r.Add(Yazi(L.T(Gear.ETKI_AD[a]), 12, "onizle-ad"));
                var iz = new VisualElement(); iz.AddToClassList("onizle-iz"); iz.pickingMode = PickingMode.Ignore; r.Add(iz);
                var yes = new VisualElement(); yes.AddToClassList("onizle-yesil"); yes.style.width = Length.Percent(W_(son[a])); iz.Add(yes);
                var gri = new VisualElement(); gri.AddToClassList("onizle-gri"); iz.Add(gri);
                float hedef = W_(su[a]);
                // 2026-10-27 performans turu 2: kayma genişlik geçişiydi (0,5 sn boyunca her karede Yoga yerleşimi); şimdi genişlik
                // baştan hedefte, eski değerden yatay ölçekle (sol kenardan) 1'e geçer: yalnız dönüşüm, yerleşim değişmez.
                gri.style.width = Length.Percent(hedef);
                if (kay && yukOnce.ContainsKey(a) && hedef > 0) { float s0 = Mathf.Clamp(W_(yukOnce[a]) / hedef, 0, 4); gri.style.scale = new Scale(new Vector3(s0, 1, 1)); gri.AddToClassList("onizle-kayar"); gri.schedule.Execute(() => gri.style.scale = new Scale(Vector3.one)).StartingIn(33); }
                r.Add(Yazi("+" + Mathf.Max(1, Mathf.RoundToInt(d * 100)) + "%", 12, "onizle-yuzde"));
                Erisim.Etiket(r, L.T(Gear.ETKI_AD[a]) + " +" + Mathf.Max(1, Mathf.RoundToInt(d * 100)) + "%", "metin");
            }
            if (yukK == k) { yukK = null; yukOnce = null; }
            if (var_ && gr.eko.esit) kart.Add(Yazi(L.T("Eşit şartlar açık: serbest maçta sayılmaz, kariyerde sayılır"), 12, "soluk"));
        }

        void TelKur(VisualElement c)
        {
            var gr = Ayar.gear;
            telYazi = Yazi(TelBaslik(), 15, "grup-kalin"); c.Add(telYazi);
            var r = Satir("tel-satir"); c.Add(r);
            var eksi = Dugme("−", () => TelAdim(-1), "ikon-dugme"); Erisim.Etiket(eksi, L.T("Tel gerginliği") + " −1", "dugme"); r.Add(eksi);
            var sl = new Slider(18, 32) { value = gr.tel }; sl.AddToClassList("tel-kaydirici"); r.Add(sl);
            Erisim.Etiket(sl, L.T("Tel gerginliği"), "kaydirici", false, false, Mathf.RoundToInt(gr.tel) + " lbs");
            sl.RegisterValueChangedCallback(e =>
            {
                float yeni = Mathf.Round(e.newValue); if (yeni == gr.tel) return; gr.tel = yeni; Ayar.GearHesap(); telDegisti = true; TelYaz();
            });
            // bırakınca kaydet + deneme smacı (HTML: deneme smacı titreşimiyle); tutamak işaretçiyi yakaladığı için TrickleDown
            sl.RegisterCallback<PointerUpEvent>(e => TelBitti(), TrickleDown.TrickleDown);
            sl.RegisterCallback<PointerCaptureOutEvent>(e => TelBitti(), TrickleDown.TrickleDown);
            var arti = Dugme("+", () => TelAdim(1), "ikon-dugme"); Erisim.Etiket(arti, L.T("Tel gerginliği") + " +1", "dugme"); r.Add(arti);
            telNot = Yazi(L.T(Gear.TelStats(gr.tel).n) + " · " + L.T("Gerginlik bir beceri ayarıdır, seviye istemez."), 12, "soluk"); c.Add(telNot);
        }
        static string TelBaslik() { return L.T("Tel gerginliği") + " · " + Mathf.RoundToInt(Ayar.gear.tel) + " lbs"; }
        void TelYaz()
        {
            if (telYazi != null) telYazi.text = TelBaslik(); if (telNot != null) telNot.text = L.T(Gear.TelStats(Ayar.gear.tel).n) + " · " + L.T("Gerginlik bir beceri ayarıdır, seviye istemez.");
            CanliCubukYaz();
        }
        Slider TelKaydirici() { return altSayfa != null ? altSayfa.Q<Slider>() : null; }
        void TelAdim(int d)
        {
            var gr = Ayar.gear; float yeni = Mathf.Clamp(gr.tel + d, 18, 32); if (yeni == gr.tel) return;
            gr.tel = yeni; Ayar.GearHesap(); telDegisti = true; var sl = TelKaydirici(); if (sl != null) sl.SetValueWithoutNotify(yeni); TelYaz(); TelBitti();
        }
        void TelBitti()
        {
            if (!telDegisti) return; telDegisti = false; Ayar.Kaydet(); root.sfx.Deneme(); Yerel.Vurus("smash", "perfect", false);
            var sl = TelKaydirici(); if (sl != null) { var b = sl.userData as ErisimBilgi; if (b != null) b.deger = Mathf.RoundToInt(Ayar.gear.tel) + " lbs"; }
            // modellerin çubukları gerginliğe bağlı: bırakınca yuva sayfası ve Soyunma gövdesi yeniden kurulur (sürüklerken değil).
            sayfa.schedule.Execute(() => { if (gez.AcikAltSayfa == Gezinti.YUVA) { var eski = altSayfa != null ? altSayfa.Q<ScrollView>() : null; Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero; YuvaSayfaKur(); KaydirmaGeri(altSayfa, of); } SoyunmaGovdeKur(true); }).StartingIn(16);
        }

        // ---- Tarz segmenti (karakter, adım 6) ----
        void TarzGovde(VisualElement c)
        {
            int sv = Ayar.gear.Seviye(); string cur = Ayar.Tarz;
            c.Add(Yazi(L.T("Tarz bir kimliktir, güç değil: artısı ve eksisi var, ücretsiz, maç dışında istediğin an değiştir. Eşit şartlarda da geçerli."), 13, "soluk"));
            foreach (var t in Karakter.Tarzlar)
            {
                bool acik = sv >= t.sv, secili = cur == t.id; string id = t.id;
                var kart = Kart("tarz-kart"); if (secili) kart.AddToClassList("secili"); if (!acik) kart.AddToClassList("kilitli"); c.Add(kart);
                var bas = Satir("arali"); bas.pickingMode = PickingMode.Ignore; kart.Add(bas); bas.Add(Yazi(L.T(t.ad), 17, "kalin"));
                var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; bas.Add(dr); string durum;
                if (secili) { dr.Add(Ikon(Ikonlar.Tik, 14, TURUNCU)); durum = L.T("Seçili"); }
                else if (!acik) { dr.Add(Ikon(Ikonlar.Kilit, 14, SOLUK)); durum = L.T("Sv {0} gerekli", t.sv); }
                else durum = L.T("Seç");
                var dy = Yazi(durum, 12, secili ? "durum-gecti" : acik ? "durum-simdi" : "durum-kilit"); dy.style.marginLeft = 4; dr.Add(dy);
                if (!string.IsNullOrEmpty(t.arti)) TarzSatir(kart, Ikonlar.Yukari, TURUNCU, L.T("Artı") + ": " + L.T(t.arti));
                if (!string.IsNullOrEmpty(t.eksi)) TarzSatir(kart, Ikonlar.Asagi, MERCAN, L.T("Eksi") + ": " + L.T(t.eksi));
                if (acik && !secili) kart.AddManipulator(new Clickable(() =>
                {
                    Ayar.ilerleme.Tarz = id; Ayar.GearHesap(); Ayar.Kaydet(); Tik(); kirli[(int)Sekme.Profil] = true; SoyunmaGovdeKur(true);
                }));
                Erisim.Etiket(kart, L.T(t.ad) + (t.arti != "" ? ", " + L.T("Artı") + ": " + L.T(t.arti) : "") + (t.eksi != "" ? ", " + L.T("Eksi") + ": " + L.T(t.eksi) : "") + ", " + durum, acik && !secili ? "dugme" : "metin", secili, !acik);
            }
        }
        void TarzSatir(VisualElement kart, Texture2D ik, Color renk, string yazi)
        {
            var r = Satir("rakip-satir"); r.pickingMode = PickingMode.Ignore; kart.Add(r); var i = Ikon(ik, 12, renk); i.style.marginRight = 6; r.Add(i); r.Add(Yazi(yazi, 13, "esnek-yatay-sifir"));
        }

        // ---- Görünüm segmenti (karakter, adım 6) ----
        class Ornek { public string id, ad, renk, neden, kisa; public bool acik; }   // kisa: kilitli karede görünen kısa koşul ("Sv 14")

        // Renk örnekleri: kare (44 pt) + seçili ✓ ve kalın çerçeve; "modelin rengi" kişi simgesiyle; kilitli kilit + "Sv n".
        // Başlıkta seçilenin adı (renk körlüğü: hangisinin seçili olduğu yazıyla da okunur).
        void OrnekSira(VisualElement c, string baslik, List<Ornek> l, string secili, System.Action<string> sec, bool yuvarlak = false)
        {
            string sad = ""; foreach (var o in l) if (o.id == secili) sad = L.T(o.ad);
            c.Add(Grup(baslik + " · " + sad)); var s = Satir("renkler"); c.Add(s);
            foreach (var o in l)
            {
                bool on = o.id == secili; string id = o.id;
                var d = new VisualElement(); d.AddToClassList("renk"); if (yuvarlak) d.AddToClassList("renk--yuvarlak"); if (on) d.AddToClassList("renk--secili"); if (!o.acik) d.AddToClassList("renk--kilit");
                Color zemin = o.renk != null ? Kit.Hex(o.renk) : Kit.Hex("#22343a"); d.style.backgroundColor = zemin;
                bool acikZemin = o.renk != null && zemin.r * .3f + zemin.g * .59f + zemin.b * .11f > .55f; Color ik = acikZemin ? Color.black : Color.white;
                if (on) d.Add(Ikon(Ikonlar.Tik, 18, ik));
                else if (!o.acik) { d.Add(Ikon(Ikonlar.Kilit, 14, ik)); if (!string.IsNullOrEmpty(o.kisa)) { var sy = Yazi(o.kisa, 10, "renk-kilit-yazi"); sy.style.color = ik; d.Add(sy); } }
                else if (o.renk == null) d.Add(Ikon(Ikonlar.Kisi, 18, SOLUK));
                if (o.acik && !on) d.AddManipulator(new Clickable(() => { sec(id); Tik(); root.vitrin.Yenile(); SoyunmaGovdeKur(true); }));
                Erisim.Etiket(d, baslik + ", " + L.T(o.ad) + (o.acik ? "" : ", " + o.neden), "dugme", on, !o.acik); s.Add(d);
            }
        }

        void RenkSira(VisualElement c, string ad, string k)
        {
            c.Add(Grup(L.T(ad) + " · " + L.T(Vitrin.RAK_AD[Ayar.RGOR[k]]))); var s = Satir("renkler"); c.Add(s);
            foreach (var id in Vitrin.RAK_SIRA)
            {
                bool on = Ayar.RGOR[k] == id; string rid = id;
                var d = new VisualElement(); d.AddToClassList("renk"); if (on) d.AddToClassList("renk--secili"); d.style.backgroundColor = Kit.Hex(Vitrin.RAK_RENK[id]);
                if (on) d.Add(Ikon(Ikonlar.Tik, 18, id == "beyaz" || id == "limon" ? Color.black : Color.white));
                d.AddManipulator(new Clickable(() => { Ayar.RGOR[k] = rid; Ayar.Kaydet(); Tik(); root.vitrin.Yenile(); SoyunmaGovdeKur(true); }));
                Erisim.Etiket(d, L.T(ad) + ", " + L.T(Vitrin.RAK_AD[id]), "dugme", on); s.Add(d);
            }
        }
    }
}
