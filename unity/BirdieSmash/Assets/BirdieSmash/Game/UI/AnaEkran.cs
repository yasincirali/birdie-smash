// Oyna sekmesi ve Hızlı maç alt sayfası (tasarım §2.2–2.3, 2026-10-26).
// Ana yol bir dokunuş: "Sıradaki" kartı (kariyerin açık basamağı: yer, rakip, saha, basamak çubuğu) ve altında tek parlak
// düğme "Oyna" (başparmak bölgesi, alt üçte bir). Kartın kendisine dokunmak o maçın kartını açar (2026-10-26; eskiden Kariyer sekmesi).
// Mod eşlemesi (tuytop_mod aynı anahtar, aynı anlam: "son oynanan mod"): eskiden ana menüde Hızlı maç / Kariyer çipleri
// vardı ve Oyna seçili modu başlatıyordu. Şimdi mod hangi düğmeye basıldığından gelir: büyük Oyna kariyeri, Hızlı maç alt
// sayfasındaki düğme serbest maçı başlatır; ikisi de Ayar.kariyerModu'nu yazar (eşit şartlar ve maç sonu "Bir maç daha"
// bu moda bakmayı sürdürür). İlk açılışta (antrenman görülmedi) mod değiştirilmez: Oyna önce 3 adımlık antrenmanı, sonra
// 5 sayılık Kolay ilk maçı açar (bugünkü akış birebir).
// Bekleyen maç (HTML ekran()): birincil düğme "Maça devam et", ikincil "Yeni maç başlat" (mod değişmez).
// Logo ve slogan yalnız ilk açılışta (antrenman görülmeden) üstte; sonra kort ve oyuncu görünür kalır (§2.3).
// Zorluk ve uzunluk kariyer maçına da uygulandığı için Ayarlar › Oyun'da da durur; saha yalnız serbest maçta anlamlı
// (kariyerde basamaktan gelir), bu yüzden yalnız burada.
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        Texture2D logo; bool logoYok;

        // 2026-10-26 arayüz yönü A "Yayın Gecesi" (faz 1): Oyna bir yayın afişi. Üstte VS afişi (zemin; UiKok.ZeminKur, Afis),
        // altta yayın plakası (sıradaki maç: etiket şeridi, basamak adı büyük dar italik, rakip noktası + adı, yıldızlar,
        // basamak çizgisi), tek büyük eğik OYNA (ışık süpürmesi; hareketi azaltta yok) ve üç kutucuk: Günün maçı · Hızlı maç ·
        // Antrenman. Davranış eskisiyle aynı: plakaya dokunmak Kariyer'i, OYNA kariyerin sıradakini (ilk açılışta antrenmanı),
        // kutucuklar eskiden kartların açtığı sayfaları açar. Eski "Sıradaki: …" alt satırı kalktı (plaka zaten söylüyor);
        // yerine maçın şartı: "11 sayı · Turnuva sahası · Kolay". Büyük yazıda kutucuklar alt alta.
        void OynaKur(VisualElement k)
        {
            k.Clear(); var sv = Kaydirma(k, true); var c = sv.contentContainer; c.AddToClassList("oyna-icerik");
            if (!Ayar.antrenman)
            {
                if (logo == null && !logoYok) { logo = Resources.Load<Texture2D>("logo"); logoYok = logo == null; }
                var m = new VisualElement(); m.AddToClassList("marka"); m.pickingMode = PickingMode.Ignore; c.Add(m);
                if (logo != null) { var li = new Image { image = logo, scaleMode = ScaleMode.ScaleToFit }; li.AddToClassList("logo"); li.pickingMode = PickingMode.Ignore; m.Add(li); }
                m.Add(Yazi("Birdie Smash", 34, "marka-ad")); m.Add(Yazi(L.T("Kaydır, kes, smaçla."), 15, "soluk"));
            }
            var esnek = new VisualElement(); esnek.AddToClassList("esnek"); esnek.AddToClassList("oyna-sahne"); esnek.pickingMode = PickingMode.Ignore; c.Add(esnek);
            // 2026-10-26 Usta yolu: dört tur bittiyse (ve Usta yolu açıksa) "Sıradaki" kartının yerini Usta kartı alır (kendi
            // Oyna düğmesiyle); finali yeniden oynamak Kariyer haritasından sürer.
            bool usta = Ayar.antrenman && UstaYolu.Acik(Ayar.ilerleme) && Ayar.ilerleme.HepsiBitti;
            if (usta && !root.bekleyen) c.Add(UstaKart()); else c.Add(SiradakiKart());
            if (root.bekleyen)
            {
                c.Add(BuyukDugme(L.T("Maça devam et"), null, () => root.MacaDon()));
                c.Add(Dugme(L.T("Yeni maç başlat"), () => root.Oyna()));
            }
            else if (!usta)
            {
                // 2026-10-26 (yasin onayı, inceleme 2026-10-26 §2): "Sıradaki: {yer}" alt yazısı kalktı; yer hemen üstteki plakada
                // yazıyordu. A tasarımında alt satır maçın biçimini söyler (sayı · saha · zorluk), ilk açılış ve şampiyon satırı kalır.
                var p = Ayar.ilerleme; var kb = KariyerVeri.Al(p.Siradaki());
                string alt = !Ayar.antrenman ? L.T("İlk kez: 3 adımlık antrenman, sonra 5 sayılık maç") : p.HepsiBitti ? L.Cumle(Career.Line(p))
                    : L.T("{0} sayı", kb.uzunluk > 0 ? kb.uzunluk : Ayar.target) + " · " + L.T(CourtView.SAHA[kb.saha].ad) + " · " + L.T(Ayar.easy ? "Kolay" : "Usta");
                c.Add(BuyukDugme(L.T("Oyna"), alt, () =>
                {
                    if (Ayar.antrenman && !Ayar.kariyerModu) { Ayar.kariyerModu = true; Ayar.GearHesap(); Ayar.Kaydet(); }
                    Ayar.kariyerSecim = -1;   // büyük Oyna her zaman sıradakini oynatır (haritadan seçilen yeniden oynama değil)
                    root.Oyna();
                }));
            }
            // 2026-10-26 adım 7–8: Günün maçı · Hızlı maç · Antrenman. "Tam antrenman" kartı Antrenman sayfasında (Temel ders);
            // ilk açılışta (antrenman görülmedi) Günün maçı yok: ilk yol tek düğme kalsın (§2.3).
            var row = Satir("karo-satir"); c.Add(row);
            if (Ayar.antrenman && !root.bekleyen) row.Add(GunlukKaro());
            row.Add(Karo(Ikonlar.Simsek, L.T("Hızlı maç"), L.T("{0} sayı", Ayar.target), L.T("Rastgele rakip"), null, HizliMacAc, L.T("Hızlı maç") + ", " + HizliOzet()));
            var p2 = Ayar.ilerleme;
            row.Add(Karo(Ikonlar.Hedef, L.T("Antrenman"), p2.AntrenmanYildiz + "/" + Dril.ToplamYildiz, L.T("yıldız"), Ikonlar.YildizDolu, AntrenmanAc,
                L.T("Antrenman") + ", " + L.T("{0}/{1} yıldız · Temel ders ve 8 görev", p2.AntrenmanYildiz, Dril.ToplamYildiz)));
        }

        string HizliOzet() { return L.T("{0} sayı · {1} · {2}", Ayar.target, L.T(Ayar.easy ? "Kolay" : "Usta"), L.T(CourtView.SAHA[Ayar.saha].ad)); }

        // Büyük eğik düğme (a-cta): limon gradyanlı eğik doku (9 dilim), solda dar italik başlık + alt satır, sağda koyu daire
        // içinde oynat simgesi. Işık süpürmesi: 3,2 sn'de bir soldan sağa, tek öğe (her düğmede değil); hareketi azaltta hiç
        // başlamaz (2026-10-27 yön C: döngü değil, girişte bir kez; Supur). Basınca .96 ölçek (USS :active) ve hafif titreşim. Dokunma alanı ≥ 80 pt.
        VisualElement BuyukDugme(string t, string alt, System.Action a)
        {
            var d = new VisualElement(); d.AddToClassList("cta"); Doku(d, Ikonlar.EgikLimon);
            var sup = new Image { image = Ikonlar.Parilti, scaleMode = ScaleMode.StretchToFill }; sup.AddToClassList("cta-supurme"); sup.pickingMode = PickingMode.Ignore; d.Add(sup);
            var yz = new VisualElement(); yz.AddToClassList("cta-yazilar"); yz.pickingMode = PickingMode.Ignore; d.Add(yz);
            yz.Add(Baslik(t, 40, "cta-baslik"));
            if (!string.IsNullOrEmpty(alt)) yz.Add(Yazi(alt, 14, "cta-alt"));
            var ik = new VisualElement(); ik.AddToClassList("cta-ikon"); ik.pickingMode = PickingMode.Ignore; ik.Add(Ikon(Ikonlar.Oyna, 24, CTA_OK)); d.Add(ik);   // 2026-10-27 yön C: koyu daire içinde turuncu ok
            d.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); a(); }));
            Erisim.Etiket(d, string.IsNullOrEmpty(alt) ? t : t + ", " + alt, "dugme");
            if (!Ayar.reduce) Supur(sup);
            return d;
        }
        // Işık süpürmesi: konum yüzde olarak (düğme genişliğinden bağımsız), 33 ms adımla; düğme panelden çıkınca durur.
        // 2026-10-26 gece: menü gizliyken (maçta kök display:none) ya da başka sekmedeyken de 30 Hz stil yazıyordu (yerleşim
        // kirlenir); görünmüyorsa adım atlanır. Ağaç yeniden kurulunca eski öğe panelden düşer ve iş durur (sızıntı yok).
        void Supur(VisualElement sup)
        {
            float t0 = Time.realtimeSinceStartup;
            IVisualElementScheduledItem is_ = null;
            is_ = sup.schedule.Execute(() =>
            {
                if (sup.panel == null || Ayar.reduce) { sup.style.display = DisplayStyle.None; if (is_ != null) is_.Pause(); return; }
                if (root.ekran != Ekran.Menu || gez.sekme != Sekme.Oyna || gez.Ust != null && !Gezinti.AltSayfa(gez.Ust)) return;
                // 2026-10-27 yön C mikro hareket (araştırma §4.3: "girişte bir kez ışık süpürmesi, döngü yok"; ekranda tek döngü
                // afişin nefesi): süpürme menü kurulunca bir kez, 0,6 sn bekleyip 0,9 sn'de geçer, sonra durur. Eskiden 3,2 sn'de bir
                // döngüydü ve style.left yazıyordu (her adımda yerleşim); artık yalnız translate (USS left −40% sabit). Yüzde
                // translate öğenin kendi genişliğine göredir: düğmenin %170'i = süpürmenin (%30) 5,67 katı.
                float e = (Time.realtimeSinceStartup - t0 - .6f) / .9f; if (e < 0) return;
                float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(e));
                sup.style.translate = new Translate(Length.Percent(k * 566.7f), 0);
                if (e >= 1) { sup.style.display = DisplayStyle.None; is_.Pause(); }
            }).Every(33);
        }

        // Kutucuk (a-tile): üstte simge + BÜYÜK küçük etiket, ortada dar italik değer, altta açıklama (isteğe bağlı simge).
        VisualElement Karo(Texture2D ikon, string etiket, string deger, string alt, Texture2D altIkon, System.Action a, string erisim, string cls = null)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("karo"); if (cls != null) d.AddToClassList(cls);
            var ust_ = new VisualElement(); ust_.AddToClassList("karo-ust"); ust_.pickingMode = PickingMode.Ignore; d.Add(ust_);
            ust_.Add(Ikon(ikon, 13, cls == "karo--altin" ? ALTIN : SOLUK)); var et = Yazi(Buyuk(etiket), 11, "karo-etiket"); ust_.Add(et);
            d.Add(Yazi(deger, 20, "karo-deger"));
            if (!string.IsNullOrEmpty(alt))
            {
                var ar = new VisualElement(); ar.AddToClassList("karo-alt-satir"); ar.pickingMode = PickingMode.Ignore; d.Add(ar);
                if (altIkon != null) ar.Add(Ikon(altIkon, 12, ALTIN)); ar.Add(Yazi(alt, 12, "karo-alt"));
            }
            d.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); a(); })); Erisim.Etiket(d, erisim, "dugme");
            return d;
        }
        // Günün maçı kutucuğu (a-tile--gold): değiştiricinin adı, altında Tüy simgesi + "+40 Tüy" (alındıysa ✓ + "Bugün kazandın").
        // r16: ödül değerin sağında mutlak konumdaydı (a-tile), "Long rally" gibi uzun adın üstüne biniyordu; alt satıra indi.
        VisualElement GunlukKaro()
        {
            var g = Bugun(); var st = Match.STIL[g.stil]; bool alindi = Ayar.ilerleme.GunlukAlindi(g.tarih);
            var d = Karo(Ikonlar.Gunes, L.T("Günün maçı"), L.T(g.deg.ad), alindi ? L.T("Bugün kazandın") : L.T("+{0} Tüy", Gunluk.OdulTuy), alindi ? Ikonlar.Tik : Ikonlar.Tuy, GunlukAc,
                L.T("Günün maçı") + ", " + L.T(g.deg.ad) + ", " + L.T("Rakip") + " " + st.isim + ", " + (alindi ? L.T("Bugün kazandın") : L.T("+{0} Tüy", Gunluk.OdulTuy)), "karo--altin");
            d.AddToClassList("karo--genis"); Doku(d, Ikonlar.AltinKaro);
            return d;
        }

        // Yayın plakası (a-plate): "Sıradaki" kartının yeni biçimi; içerik aynı, dokunuş maç kartını açar (2026-10-26). Rakip rengi yanında adıyla
        // (yalnız renkle değil), yıldızlar dolu/çerçeve biçimiyle ve ekran okuyucuya "1/3 yıldız"; basamak çizgisi biçimle
        // (geçilen dolu limon, şimdiki çizgili desen, gelecek soluk).
        VisualElement SiradakiKart()
        {
            var p = Ayar.ilerleme; int g = p.Siradaki(), tr = KariyerVeri.TurOf(g), i = KariyerVeri.BasOf(g); var kb = KariyerVeri.Al(g); var st = Match.STIL[kb.stil];
            bool tamam = p.HepsiBitti;
            var kart = new VisualElement(); kart.AddToClassList("plaka"); Doku(kart, Ikonlar.Plaka); // 2026-10-26 (yasin onayı, ekran/inceleme ile aynı davranış): plakaya dokunmak o basamağın maç kartını açar; kariyer
            // bittiyse (sıradaki basamak yok) Kariyer sekmesi.
            // Birleşme (2026-10-26, A + Ar-Ge) titreşimi iki yola da koymuştu; SekmeAc sekme değişince zaten "tick" verir: Kariyer
            // yolunda iki titreşim olmasın diye yalnız maç kartı yolunda.
            kart.AddManipulator(new Clickable(() => { if (tamam) SekmeAc(Sekme.Kariyer); else { Yerel.Hap("tick"); MacKartAc(g); } }));
            var bas = new VisualElement(); bas.AddToClassList("plaka-ust"); bas.pickingMode = PickingMode.Ignore; kart.Add(bas);
            var serit = new VisualElement(); serit.AddToClassList("serit"); Doku(serit, Ikonlar.EgikLimonKucuk); serit.pickingMode = PickingMode.Ignore; bas.Add(serit);
            serit.Add(Yazi(Buyuk(L.T(tamam ? "Kariyer tamam" : "Sıradaki maç")), 13, "serit-yazi"));
            bas.Add(Yazi(Buyuk(L.T(TurAd(tr))) + " · " + (i + 1) + "/" + KariyerVeri.BASAMAK, 12, "plaka-tur"));
            kart.Add(Baslik(L.T(kb.yer), 38, "plaka-baslik"));
            var rk = new VisualElement(); rk.AddToClassList("plaka-meta"); rk.pickingMode = PickingMode.Ignore; kart.Add(rk);
            var sol = new VisualElement(); sol.AddToClassList("plaka-rakip"); sol.pickingMode = PickingMode.Ignore; rk.Add(sol);
            var nok = new VisualElement(); nok.AddToClassList("renk-nokta"); nok.AddToClassList("plaka-nokta"); nok.style.backgroundColor = Kit.Hex(st.renk); sol.Add(nok);
            sol.Add(Yazi(st.isim + " · " + L.T(st.ad), 15, "plaka-rakip-yazi"));
            rk.Add(UcYildiz(p.Maske(g), 18));
            var bc = Satir("basamak-cubugu"); bc.pickingMode = PickingMode.Ignore; kart.Add(bc);
            for (int j = 0; j < KariyerVeri.BASAMAK; j++)
            {
                var b = new VisualElement(); b.AddToClassList("basamak"); b.pickingMode = PickingMode.Ignore; int gj = KariyerVeri.Global(tr, j);
                bool simdi = gj == g && !tamam; b.AddToClassList(p.Kazanildi(gj) && !simdi ? "basamak--gecti" : simdi ? "basamak--simdi" : "basamak--kilit");
                if (simdi) Doku(b, Ikonlar.Serit); bc.Add(b);
            }
            Erisim.Etiket(kart, L.T(tamam ? "Kariyer tamam" : "Sıradaki maç") + ", " + L.T(TurAd(tr)) + ", " + L.T(kb.yer) + ", " + L.T("Rakip") + " " + st.isim + " · " + L.T(st.ad) + ", " + L.T(CourtView.SAHA[kb.saha].ad) + ", " + (i + 1) + "/" + KariyerVeri.BASAMAK + ", " + L.T("{0}/3 yıldız", p.Yildiz(g)), "dugme");
            return kart;
        }

        // ---- Hızlı maç alt sayfası ----
        void HizliKur()
        {
            var c = AltSayfaKur(L.T("Hızlı maç"));
            c.Add(Yazi(L.T("Rakip her maçta rastgele: Smaşçı, Duvar ya da File oyuncusu."), 13, "soluk"));
            OyunSecenek(c, true);
            c.Add(Bosluk(8));
            c.Add(Dugme(L.T("Hızlı maç oyna"), () =>
            {
                Ayar.kariyerModu = false; Ayar.GearHesap(); Ayar.Kaydet(); gez.Kapat(Gezinti.HIZLI); Uygula(gez.sekme, false); root.Oyna();
            }, "dugme--birincil", HizliOzet(), 19));
        }

        void AltSayfaYenidenKur(string altS) { if (altS == Gezinti.KART) MacKartKur(); else if (altS == Gezinti.YUVA) YuvaSayfaKur(); else if (altS == Gezinti.DRIL) DrilKartKur(); else if (altS == Gezinti.GUNLUK) GunlukKartKur(); else HizliKur(); }

        // Alt sayfa iskeleti (Hızlı maç ve kariyer maç kartı ortak, 2026-10-26): tutamak (aşağı çekince kapanır, iOS kalıbı;
        // ayrıca Kapat, Geri ve zemine dokunma), başlık, kaydırmalı gövde. Dönüş: gövdenin içerik kabı.
        VisualElement AltSayfaKur(string baslik)
        {
            altKatman.Clear(); altSayfa = new VisualElement(); altSayfa.AddToClassList("alt-sayfa"); altSayfa.style.paddingBottom = altBosluk + 16; altKatman.Add(altSayfa);
            var tut = new VisualElement(); tut.AddToClassList("tutamak-alan"); var cz = new VisualElement(); cz.AddToClassList("tutamak"); cz.pickingMode = PickingMode.Ignore; tut.Add(cz); altSayfa.Add(tut);
            float basY = 0; bool cek = false;
            tut.RegisterCallback<PointerDownEvent>(e => { basY = e.position.y; cek = true; tut.CapturePointer(e.pointerId); });
            tut.RegisterCallback<PointerMoveEvent>(e => { if (cek && e.position.y - basY > 60) { cek = false; tut.ReleasePointer(e.pointerId); AltKapat(); } });
            tut.RegisterCallback<PointerUpEvent>(e => { cek = false; tut.ReleasePointer(e.pointerId); });
            var bas = Satir("arali"); altSayfa.Add(bas);
            var t = Yazi(baslik, 19, "sayfa-baslik"); Erisim.Etiket(t, baslik, "baslik"); bas.Add(t);
            bas.Add(Dugme(L.T("Kapat"), AltKapat, "kucuk-dugme"));
            var sv = Kaydirma(altSayfa); sv.AddToClassList("alt-kaydir"); return sv.contentContainer;
        }

        // Zorluk, maç uzunluğu (ve istenirse saha): Ayarlar ile Hızlı maç aynı işleyiciyi paylaşır (aynı anahtarlar).
        void OyunSecenek(VisualElement c, bool saha)
        {
            c.Add(Grup(L.T("Zorluk")));
            var z = Satir(); c.Add(z);
            z.Add(Cip(L.T("Kolay · topa kendin gidersin, sen vur"), Ayar.easy, () => { Ayar.easy = true; Ayar.Kaydet(); Degisti(); }));
            z.Add(Cip(L.T("Usta · koşmak da sende"), !Ayar.easy, () => { Ayar.easy = false; Ayar.Kaydet(); Degisti(); }));
            c.Add(Grup(L.T("Maç uzunluğu")));
            var u = Satir(); c.Add(u); int[] Ls = { 5, 11, 21 }; string[] La = { "5 sayı (hızlı, ~2 dk)", "11 sayı (kısa)", "21 sayı (resmî)" };
            for (int i = 0; i < 3; i++) { int v = Ls[i]; u.Add(Cip(L.T(La[i]), Ayar.target == v, () => { Ayar.target = v; Ayar.Kaydet(); Degisti(); })); }
            if (!saha) return;
            c.Add(Grup(L.T("Saha")));
            var s1 = Satir(); var s2 = Satir(); c.Add(s1); c.Add(s2);
            for (int i = 0; i < 4; i++)
            {
                string id = CourtView.SIRA[i];
                (i < 2 ? s1 : s2).Add(Cip(L.T(CourtView.SAHA[id].ad), Ayar.saha == id, () => { Ayar.saha = id; Ayar.Kaydet(); root.SahaDegisti(); Degisti(); }));
            }
        }

        // Ayar değişti: görünen yüzey (alt sayfa / sayfa / sekme) yeniden kurulur; kaydırma yeri ScrollView'de kalır.
        void Degisti()
        {
            for (int i = 0; i < 4; i++) kirli[i] = true; ustKirli = true;
            string altS = gez.AcikAltSayfa;
            if (altS != null) { var eski = altSayfa != null ? altSayfa.Q<ScrollView>() : null; Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero; AltSayfaYenidenKur(altS); KaydirmaGeri(altSayfa, of); }
            string u = gez.Ust; if (u != null && !Gezinti.AltSayfa(u)) { var eski = sayfa.Q<ScrollView>(); Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero; SayfaKur(u); KaydirmaGeri(sayfa, of); }
            else if (u == null) { var k = sekmeKok[(int)gez.sekme]; var eski = k.Q<ScrollView>(null, "govde-kaydir") ?? k.Q<ScrollView>(); Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero; SekmeKur(gez.sekme); KaydirmaGeri(k, of); }
            if (ustKirli) UstKur();
        }
        // Yeniden kurulan gövdede kaydırma yeri korunur (yerleşimden sonra; ilk karede içerik yüksekliği henüz yok).
        static void KaydirmaGeri(VisualElement v, Vector2 of)
        {
            if (v == null || of.y <= 0) return; var sv = v.Q<ScrollView>(null, "govde-kaydir") ?? v.Q<ScrollView>(); if (sv == null) return;
            sv.scrollOffset = of; sv.schedule.Execute(() => sv.scrollOffset = of).StartingIn(33);
        }
    }
}
