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
            oynaSahne = esnek; esnek.RegisterCallback<GeometryChangedEvent>(e => AfisYerlestir());   // 2026-10-27: afiş bu boşluğa oturur (Duyarli.cs)
            // 2026-11-06 canlı afiş: afişin sol yarısına (senin oyuncun) dokununca Dolap'ta seçili sevinç oynar. Süs dokunuşu:
            // sağ yarı boş geçer (rakip), ekran okuyucuda tek eylem "Sevinci oynat".
            esnek.pickingMode = PickingMode.Position;
            esnek.RegisterCallback<PointerUpEvent>(e => { if (e.localPosition.x < esnek.layout.width * .55f && root.afis != null && root.afis.Acik) { Yerel.Hap("tick"); root.afis.Sevinc(); } });
            Erisim.Etiket(esnek, L.T("Sevinci oynat"), "dugme");
            // 2026-10-26 Usta yolu: sekiz tur bittiyse (ve Usta yolu açıksa) "Sıradaki" kartının yerini Usta kartı alır (kendi
            // Oyna düğmesiyle); finali yeniden oynamak Kariyer haritasından sürer.
            bool usta = Ayar.antrenman && UstaYolu.Acik(Ayar.ilerleme) && Ayar.ilerleme.HepsiBitti;
            // 2026-10-31 (yasin "yap", inceleme 31 Eki 12:10 madde 2): ilk açılışta plaka "Sıradaki maç · Mika" diyordu ama OYNA
            // önce antrenmanı açıyor; üç kutucuk da ilk yolu bölüyordu. Öğretici bitene kadar (hiç galibiyet yok ve antrenman
            // görülmedi) tek kart ilk maçın ne olduğunu söyler, kutucuklar yok. Galibiyeti olan (devirle gelen dahil) aynı ekranı görür.
            bool ilk = !Ayar.antrenman && Ayar.wins == 0 && !root.bekleyen;
            if (ilk) c.Add(IlkKart()); else if (usta && !root.bekleyen) c.Add(UstaKart()); else c.Add(SiradakiKart());
            // 2026-11-09 (yasin: "kariyeri farklı zorluklarda oynayabilme gelmesi lazım"): seçim 5 Kas'tan beri Kariyer'deki maç
            // kartındaydı, Oyna'dan oynayan görmüyordu. Aynı dört çip (tek ayar tuytop_zorluk) Oyna düğmesinin hemen üstünde;
            // açıklama satırı yok (düğmenin alt satırı kademeyi zaten söylüyor, küçük ekranda bir satır kazanılır).
            if (Ayar.antrenman && !usta && !root.bekleyen && !ilk) { var zs = Satir("oyna-zorluk"); c.Add(zs); ZorlukSatir(zs, c, false); }
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
                string alt = ilk ? null : !Ayar.antrenman ? L.T("İlk kez: 3 adımlık antrenman, sonra 5 sayılık maç") : p.HepsiBitti ? L.Cumle(Ayar.UstaZor ? Career.Line(p) : Career.Line(p).Replace("Usta yolu: ", "Şampiyon yolu: "))
                    : L.T("{0} sayı", kb.uzunluk > 0 ? kb.uzunluk : Ayar.target) + " · " + L.T(Ayar.ZorlukAd)   // 2026-11-09: saha adı ("Okul (ahşap)") plakadaki yer adıyla iki ayrı yer gibi okunuyordu (inceleme 10 Kas 02:15); saha maç kartında
                      + (kb.degistirici != null && Gunluk.Deg(kb.degistirici) != null ? " · " + L.T(Gunluk.Deg(kb.degistirici).ad) : "");   // kural maçı adı
                c.Add(BuyukDugme(L.T("Oyna"), alt, () =>
                {
                    if (Ayar.antrenman && !Ayar.kariyerModu) { Ayar.kariyerModu = true; Ayar.GearHesap(); Ayar.Kaydet(); }
                    Ayar.kariyerSecim = -1;   // büyük Oyna her zaman sıradakini oynatır (haritadan seçilen yeniden oynama değil)
                    root.Oyna();
                }));
            }
            // 2026-10-26 adım 7–8: Günün maçı · Hızlı maç · Antrenman. "Tam antrenman" kartı Antrenman sayfasında (Temel ders);
            // ilk açılışta (antrenman görülmedi) Günün maçı yok: ilk yol tek düğme kalsın (§2.3).
            if (ilk) return;
            var row = Satir("karo-satir"); c.Add(row);
            if (Ayar.antrenman && !root.bekleyen) row.Add(GunlukKaro());
            row.Add(Karo(Ikonlar.Simsek, L.T("Hızlı maç"), L.T("{0} sayı", Ayar.target), L.T("Rastgele rakip"), null, HizliMacAc, L.T("Hızlı maç") + ", " + HizliOzet()));
            var p2 = Ayar.ilerleme;
            row.Add(Karo(Ikonlar.Hedef, L.T("Antrenman"), p2.AntrenmanYildiz + "/" + Dril.ToplamYildiz, L.T("yıldız"), Ikonlar.YildizDolu, AntrenmanAc,
                L.T("Antrenman") + ", " + L.T("{0}/{1} yıldız · Temel ders ve {2} görev", p2.AntrenmanYildiz, Dril.ToplamYildiz, Dril.Liste.Length)));
            // 2026-11-10 (yasin: "catch yok", kıyas: savaş geçidi şeridi, Forza seviye ödülü): sıradaki seviyenin ödülü ana ekranda
            // tek satır; dokununca Seviye yolu (Sv 30'a kadar bütün ödüller). Sv 30'da satır yok.
            var sh = SvHedefSatir(); if (sh != null) c.Add(sh);
        }

        // İlk maç kartı (yalnız ilk açılış): plakanın yerinde, rakip ve basamak çizgisi yok (henüz kariyer başlamadı).
        VisualElement IlkKart()
        {
            var kart = Kart("ilk-kart"); kart.Add(Yazi(L.T("İlk maçın"), 20, "kalin"));
            kart.Add(Yazi(L.T("3 adım antrenman, sonra 5 sayı"), 15, "soluk"));
            Erisim.Etiket(kart, L.T("İlk maçın") + ", " + L.T("3 adım antrenman, sonra 5 sayı"), "metin"); return kart;
        }

        string HizliOzet() { return L.T("{0} sayı · {1} · {2}", Ayar.target, L.T(Ayar.ZorlukAd), L.T(CourtView.SAHA[Ayar.saha].ad)); }

        // Büyük eğik düğme (a-cta): limon gradyanlı eğik doku (9 dilim), solda dar italik başlık + alt satır, sağda koyu daire
        // içinde oynat simgesi. Işık süpürmesi: 3,2 sn'de bir soldan sağa, tek öğe (her düğmede değil); hareketi azaltta hiç
        // başlamaz (2026-10-27 yön C: döngü değil, girişte bir kez; Supur). Basınca .96 ölçek (USS :active) ve hafif titreşim. Dokunma alanı ≥ 80 pt.
        VisualElement BuyukDugme(string t, string alt, System.Action a)
        {
            var d = new VisualElement(); d.AddToClassList("cta"); Doku(d, Ikonlar.EgikLimon);
            var sup = new Image { image = Ikonlar.Parilti, scaleMode = ScaleMode.StretchToFill }; sup.AddToClassList("cta-supurme"); sup.pickingMode = PickingMode.Ignore; d.Add(sup);
            var yz = new VisualElement(); yz.AddToClassList("cta-yazilar"); yz.pickingMode = PickingMode.Ignore; d.Add(yz);
            yz.Add(Baslik(t, Px(40, 34), "cta-baslik"));   // kısa ekranda (SE) 34: düğme 80 yerine ~70 pt, afişe yer kalır
            if (!string.IsNullOrEmpty(alt)) yz.Add(Yazi(alt, 14, "cta-alt"));
            var ik = new VisualElement(); ik.AddToClassList("cta-ikon"); ik.pickingMode = PickingMode.Ignore; ik.Add(Ikon(Ikonlar.Oyna, 24, CTA_OK)); d.Add(ik);   // 2026-10-27 yön C: koyu daire içinde turuncu ok
            d.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); PerdeKaynakYaz(d); a(); }));   // 2026-10-27: perde v2'nin 3B topu buradan kalkar
            Erisim.Etiket(d, string.IsNullOrEmpty(alt) ? t : t + ", " + alt, "dugme");
            if (!Ayar.reduce) Supur(sup);
            return d;
        }
        // Işık süpürmesi: konum yüzde olarak (düğme genişliğinden bağımsız), 33 ms adımla; düğme panelden çıkınca durur.
        // 2026-10-26 gece: menü gizliyken (maçta kök display:none) ya da başka sekmedeyken de 30 Hz stil yazıyordu (yerleşim
        // kirlenir); görünmüyorsa adım atlanır. Ağaç yeniden kurulunca eski öğe panelden düşer ve iş durur (sızıntı yok).
        void Supur(VisualElement sup)
        {
            // Saat menü GERÇEKTEN göründüğünde başlar (tur 2 inceleme): soğuk açılışta menü açılış perdesinin (en az ,95 sn) altında,
            // maçtan dönüşte geçiş perdesinin altında kuruluyor; kurulumdan sayınca süpürme (,6 + ,9 sn) çoğu kez perdenin
            // arkasında geçip hiç görülmüyordu.
            float t0 = -1;
            IVisualElementScheduledItem is_ = null;
            is_ = sup.schedule.Execute(() =>
            {
                if (sup.panel == null || Ayar.reduce) { sup.style.display = DisplayStyle.None; if (is_ != null) is_.Pause(); return; }
                if (root.ekran != Ekran.Menu || gez.sekme != Sekme.Oyna || gez.Ust != null && !Gezinti.AltSayfa(gez.Ust)) return;
                // 2026-10-27 yön C mikro hareket (araştırma §4.3: "girişte bir kez ışık süpürmesi, döngü yok"; ekranda tek döngü
                // afişin nefesi): süpürme menü kurulunca bir kez, 0,6 sn bekleyip 0,9 sn'de geçer, sonra durur. Eskiden 3,2 sn'de bir
                // döngüydü ve style.left yazıyordu (her adımda yerleşim); artık yalnız translate (USS left −40% sabit). Yüzde
                // translate öğenin kendi genişliğine göredir: düğmenin %170'i = süpürmenin (%30) 5,67 katı.
                if (t0 < 0) { var hud = root.HudKatman; if (hud != null && (hud.PerdeVar || hud.PerdeOrtu > 0)) return; t0 = Time.realtimeSinceStartup; }
                float e = (Time.realtimeSinceStartup - t0 - .6f) / .9f; if (e < 0) return;
                float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(e));
                sup.style.translate = new Translate(Length.Percent(k * 566.7f), 0);
                if (e >= 1) { sup.style.display = DisplayStyle.None; is_.Pause(); }
            }).Every(33);
        }

        // Kutucuk (a-tile): üstte simge + küçük etiket (r55: büyük harf kalktı, "MATCH OF THE DAY" Nunito ExtraBold 11 px'te 105 px'e
        // sığmıyor, yazı 11'in altına küçülmüyor; cümle düzeninde iki satıra sarılabilir), ortada dar italik değer, altta açıklama (isteğe bağlı simge).
        VisualElement Karo(Texture2D ikon, string etiket, string deger, string alt, Texture2D altIkon, System.Action a, string erisim, string cls = null)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("karo"); if (cls != null) d.AddToClassList(cls);
            var ust_ = new VisualElement(); ust_.AddToClassList("karo-ust"); ust_.pickingMode = PickingMode.Ignore; d.Add(ust_);
            ust_.Add(Ikon(ikon, 13, cls == "karo--altin" ? ALTIN : SOLUK)); var et = Yazi(etiket, 11, "karo-etiket"); ust_.Add(et);
            // 2026-11-02 (Ar-Ge 19:40 #1, r118): "8/63" karoya sığmayınca "8/6" satırda kalıp "3" alt satıra, yıldız ikonunun
            // altına düşüyordu (oyuncu hedefi geçmiş sanıyordu). Değer tek satır; sığmazsa %75'e kadar küçülür (Sigdir hesabı).
            // Kap ikonsuz: SigdirOlc yatay satırdaki ikonu genişlikten düşer, karonun üst satırındaki ikon burada sayılmasın.
            var dk = new VisualElement(); dk.pickingMode = PickingMode.Ignore; d.Add(dk);
            var dl = Yazi(deger, 20, "karo-deger"); dl.style.whiteSpace = WhiteSpace.NoWrap; dl.style.flexShrink = 0f; dk.Add(dl);
            float dt0 = dl.style.fontSize.value.value; dk.RegisterCallback<GeometryChangedEvent>(e => SigdirOlc(dk, dl, dt0, .75f));
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
            serit.Add(Yazi(L.T(tamam ? "Kariyer tamam" : "Sıradaki maç"), 13, "serit-yazi"));   // 2026-11-06 unslop: büyük harf + geniş aralık etiket yerine cümle düzeni
            bas.Add(Yazi(L.T(TurAd(tr)) + " · " + (i + 1) + "/" + KariyerVeri.BASAMAK, 12, "plaka-tur"));
            kart.Add(Baslik(L.T(kb.yer), Px(38, 32), "plaka-baslik"));
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
        // ---- Seviye yolu (2026-11-10) ----
        // Seviye atlamak bugüne kadar yalnız kilit kaldırıyordu ve sıradaki ödül hiçbir yerde görünmüyordu (Profil'de tek cümle).
        // Kıyas: Fortnite/COD savaş geçidi şeridi (sıradaki ödül hep ekranda), Forza (her seviyede sabit ödül). Tablo sabit ve
        // rastgelesiz: Gear.SvOdul Tüy'ü + o seviyede açılanlar (Karakter.AcilanlarTurlu). 5'in katları "Büyük ödül" etiketiyle
        // (yalnız renk değil). Satır dokunma hedefi değil, okunur liste; tek eylem Kapat.
        static string SvAcilanOzet(int sv, int azami)
        {
            var tl = Karakter.AcilanlarTurlu(sv); if (tl.Count == 0) return null;
            var ad = new System.Collections.Generic.List<string>(); for (int i = 0; i < tl.Count && i < azami; i++) ad.Add(L.T(tl[i][1]));
            return string.Join(", ", ad.ToArray()) + (tl.Count > azami ? " +" + (tl.Count - azami) : "");
        }
        VisualElement SvHedefSatir()
        {
            // 2026-11-10: dağıtılmamış gelişim puanı varken satır onu söyler ve Gelişim sayfasını açar (tek "sıradaki iş" satırı;
            // iki satır küçük ekranda Oyna düğmesini iterdi). Puan dağıtılınca satır yine seviye ödülüne döner.
            if (Ayar.GelisimBos > 0) return GelisimSatir();
            int L_, kalan, gerek; Ayar.gear.OyuncuSv(out L_, out kalan, out gerek); if (gerek <= 0) return EfsaneSatir();
            int sv = L_ + 1; string ac = SvAcilanOzet(sv, 1);
            var d = new VisualElement(); d.AddToClassList("sv-hedef");
            var sol = new VisualElement(); sol.AddToClassList("sv-hedef-sol"); sol.pickingMode = PickingMode.Ignore; d.Add(sol);
            sol.Add(Yazi(L.T("Sv {0} ödülü · {1} XP kaldı", sv, gerek - kalan), 14, "kalin"));
            var c2 = Cubuk(kalan / (float)gerek, TURUNCU, 5); c2.AddToClassList("sv-hedef-cubuk"); sol.Add(c2);
            var sag = new VisualElement(); sag.AddToClassList("sv-hedef-sag"); sag.pickingMode = PickingMode.Ignore; d.Add(sag);
            sag.Add(Ikon(Ikonlar.Tuy, 16, ALTIN)); sag.Add(Yazi("+" + Gear.SvOdul(sv), 16, "rakam"));
            // açılan eşya sol blokta, çubuğun altında: sağda dururken 375 pt'te başlığı iki satıra sarıyordu
            if (ac != null && K <= 1.35f) { var a = Yazi(ac, 12, "soluk"); a.AddToClassList("sv-hedef-ac"); sol.Add(a); }
            d.AddManipulator(new Clickable(() => { Tik(); SeviyeYoluAc(); }));
            Erisim.Etiket(d, L.T("Sv {0} ödülü · {1} XP kaldı", sv, gerek - kalan) + ", " + L.T("+{0} Tüy", Gear.SvOdul(sv)) + (ac != null ? ", " + ac : "") + ", " + L.T("Seviye yolu"), "dugme");
            return d;
        }
        // Sv 30'dan sonra satır boş kalıyordu (2026-11-10 Efsane kademeleri, Gear.EFSANE_XP): aynı kalıp, sıradaki kademe ve +300 Tüy.
        VisualElement EfsaneSatir()
        {
            int n, kalan; if (!Ayar.gear.EfsaneIlerleme(out n, out kalan)) return null;
            string bas = L.T("Efsane {0} · {1} XP kaldı", n + 1, Sayi(Gear.EFSANE_XP - kalan));
            var d = new VisualElement(); d.AddToClassList("sv-hedef");
            var sol = new VisualElement(); sol.AddToClassList("sv-hedef-sol"); sol.pickingMode = PickingMode.Ignore; d.Add(sol);
            sol.Add(Yazi(bas, 14, "kalin"));
            var c2 = Cubuk(kalan / (float)Gear.EFSANE_XP, ALTIN, 5); c2.AddToClassList("sv-hedef-cubuk"); sol.Add(c2);
            var sag = new VisualElement(); sag.AddToClassList("sv-hedef-sag"); sag.pickingMode = PickingMode.Ignore; d.Add(sag);
            sag.Add(Ikon(Ikonlar.Tuy, 16, ALTIN)); sag.Add(Yazi("+" + Gear.EFSANE_TUY, 16, "rakam"));
            d.AddManipulator(new Clickable(() => { Tik(); SeviyeYoluAc(); }));
            Erisim.Etiket(d, bas + ", " + L.T("+{0} Tüy", Gear.EFSANE_TUY) + ", " + L.T("Seviye yolu"), "dugme");
            return d;
        }
        void SeviyeYoluKur()
        {
            var c = AltSayfaKur(L.T("Seviye yolu"));
            int L_, kalan, gerek; Ayar.gear.OyuncuSv(out L_, out kalan, out gerek);
            int en, ek_; bool ef = Ayar.gear.EfsaneIlerleme(out en, out ek_);
            c.Add(Yazi(gerek > 0 ? L.T("Sv {0} · sonraki seviyeye {1} XP", L_, gerek - kalan) : en > 0 ? L.T("Sv 30 · Efsane {0}", en) : L.T("Sv 30 · en üst seviye"), 15, "kalin"));
            if (gerek > 0) { var cb = Cubuk(kalan / (float)gerek, TURUNCU, 8); cb.style.marginTop = 6; cb.style.marginBottom = 4; c.Add(cb); }
            else if (ef)
            {
                // Sv 30: liste boş kalırdı; sayfa Efsane kademesini anlatır (tek kart, sabit ödül, rastgelelik yok).
                var cb = Cubuk(ek_ / (float)Gear.EFSANE_XP, ALTIN, 8); cb.style.marginTop = 6; cb.style.marginBottom = 4; c.Add(cb);
                var k = new VisualElement(); k.AddToClassList("sv-yol"); k.AddToClassList("sv-yol--buyuk"); k.AddToClassList("sv-yol--siradaki");
                var no = Yazi((en + 1).ToString(), 18, "kalin"); no.AddToClassList("sv-yol-no"); k.Add(no);
                var or2 = new VisualElement(); or2.AddToClassList("esnek-yatay"); k.Add(or2);
                or2.Add(Yazi(L.T("Efsane {0} · {1} XP kaldı", en + 1, Sayi(Gear.EFSANE_XP - ek_)), 14, "kalin"));
                var sg = Satir(); sg.Add(Ikon(Ikonlar.Tuy, 16, ALTIN)); sg.Add(Yazi("+" + Gear.EFSANE_TUY, 16, "rakam")); k.Add(sg);
                Erisim.Etiket(k, L.T("Efsane {0} · {1} XP kaldı", en + 1, Sayi(Gear.EFSANE_XP - ek_)) + ", " + L.T("+{0} Tüy", Gear.EFSANE_TUY), "metin");
                var ea = Yazi(L.T("Sv 30'dan sonra her {0} XP bir Efsane kademesi ve {1} Tüy verir. Kademe yalnız gösteriştir: güç ya da kapı açmaz.", Sayi(Gear.EFSANE_XP), Gear.EFSANE_TUY), 13, "soluk"); ea.style.whiteSpace = WhiteSpace.Normal; ea.style.marginBottom = 6;
                c.Add(ea); c.Add(k); return;
            }
            var acik = Yazi(L.T("Her seviye Tüy ve bir gelişim puanı verir, 5'in katları büyük ödül. Açılanlar Dolap ve Atölye'de seni bekler."), 13, "soluk"); acik.style.whiteSpace = WhiteSpace.Normal; acik.style.marginBottom = 6; c.Add(acik);
            for (int sv = L_ + 1; sv <= 30; sv++)
            {
                bool buyuk = sv % 5 == 0; string ac = SvAcilanOzet(sv, 3);
                var k = new VisualElement(); k.AddToClassList("sv-yol"); if (buyuk) k.AddToClassList("sv-yol--buyuk"); if (sv == L_ + 1) k.AddToClassList("sv-yol--siradaki"); c.Add(k);
                var no = Yazi(L.T("Sv {0}", sv), 20, "rakam"); no.AddToClassList("sv-yol-no"); k.Add(no);
                var orta = new VisualElement(); orta.AddToClassList("sv-yol-orta"); k.Add(orta);
                var tr = new VisualElement(); tr.AddToClassList("sv-yol-tuy"); orta.Add(tr);
                tr.Add(Ikon(Ikonlar.Tuy, 16, ALTIN)); tr.Add(Yazi(L.T("+{0} Tüy", Gear.SvOdul(sv)), 16, "kalin"));
                if (buyuk) { var be = Yazi(L.T("Büyük ödül"), 12, "sv-yol-etiket"); tr.Add(be); }
                if (ac != null) { var ay = Yazi(L.T("Açılır: {0}", ac), 13, "soluk"); ay.style.whiteSpace = WhiteSpace.Normal; orta.Add(ay); }
                Erisim.Etiket(k, L.T("Sv {0}", sv) + ", " + (buyuk ? L.T("Büyük ödül") + ", " : "") + L.T("+{0} Tüy", Gear.SvOdul(sv)) + (ac != null ? ", " + L.T("Açılır: {0}", ac) : ""), "metin");
            }
        }

        void HizliKur()
        {
            var c = AltSayfaKur(L.T("Hızlı maç"));
            c.Add(Yazi(L.T("Rakip her maçta rastgele. Kariyerde yendiğin rakipler de gelir."), 13, "soluk"));
            OyunSecenek(c, true);
            AltEylem(Dugme(L.T("Hızlı maç oyna"), () =>
            {
                Ayar.kariyerModu = false; Ayar.GearHesap(); Ayar.Kaydet(); gez.Kapat(Gezinti.HIZLI); Uygula(gez.sekme, false); root.Oyna();
            }, "dugme--birincil", K > 1.35f ? null : HizliOzet(), 19));   // 2026-11-07 (UX2): büyük yazıda özet satırı CTA'yı 3 satıra büyütüp listeyi daraltıyordu; seçimler çiplerde zaten görünür
        }

        void AltSayfaYenidenKur(string altS) { if (altS == Gezinti.SEVIYE) SeviyeYoluKur(); else if (altS == Gezinti.GELISIM) GelisimKur(); else if (altS == Gezinti.KART) MacKartKur(); else if (altS == Gezinti.YUVA) YuvaSayfaKur(); else if (altS == Gezinti.DRIL) DrilKartKur(); else if (altS == Gezinti.GUNLUK) GunlukKartKur(); else if (altS == Gezinti.SEC) OyuncuSecKur(); else HizliKur(); }

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
            // 2026-11-05 unslop: başlık içerik genişliğinde kalıp Kapat'ın yanında iki satıra kırılıyordu ("Köşe / smacı"); kalan
            // genişliği alır, Kapat daralmaz.
            var t = Yazi(baslik, 19, "sayfa-baslik"); Erisim.Etiket(t, baslik, "baslik"); t.style.flexGrow = 1f; t.style.flexShrink = 1f; t.style.minWidth = 0f; t.style.marginRight = 12f; bas.Add(t);
            var kp = Dugme(L.T("Kapat"), AltKapat, "kucuk-dugme"); kp.style.flexShrink = 0f; bas.Add(kp);
            var sv = Kaydirma(altSayfa); sv.AddToClassList("alt-kaydir");
            // 2026-11-07 (UX2): içerik görünen alandan uzunsa altta solma; sona kaydırınca kalkar (kaydırılabilir olduğu belli olsun).
            var sol = new Image { image = Ikonlar.Solma, scaleMode = ScaleMode.StretchToFill }; sol.AddToClassList("kaydir-solma"); sol.pickingMode = PickingMode.Ignore; sv.hierarchy.Add(sol);
            System.Action solGuncelle = () => { var vs = sv.verticalScroller; bool kalan = vs.highValue > 2 && vs.value < vs.highValue - 2; var ds = kalan ? DisplayStyle.Flex : DisplayStyle.None; if (sol.resolvedStyle.display != ds) sol.style.display = ds; };
            sv.verticalScroller.valueChanged += v => solGuncelle(); sv.RegisterCallback<GeometryChangedEvent>(e => solGuncelle()); sv.contentContainer.RegisterCallback<GeometryChangedEvent>(e => solGuncelle());
            return sv.contentContainer;
        }

        // Zorluk, maç uzunluğu (ve istenirse saha): Ayarlar ile Hızlı maç aynı işleyiciyi paylaşır (aynı anahtarlar).
        void OyunSecenek(VisualElement c, bool saha)
        {
            // 2026-11-07 inceleme (fikir 00:03 #3): zorluk Ayarlar'dan kalktı; "bu maç nasıl olsun" kararı, oynamadan hemen önce
            // görülen iki yerde kalır (Hızlı maç sayfası, kariyer kartı). Ayarlar'da değişip kariyeri sessizce değiştirmiyor.
            if (saha) { c.Add(Grup(L.T("Zorluk")));
            var z = Satir(); c.Add(z);
            // 2026-11-05 unslop incelemesi (yasin "yap"): açıklama çipin içindeydi, çipler üç satıra kırılıyordu; "Kolay · topa kendin
            // gidersin" de ters okunuyordu (Kolay'da koşu otomatik). Çipte kısa ad, seçilenin açıklaması altında tek soluk satır.
            ZorlukSatir(z, c); }
            c.Add(Grup(L.T("Maç uzunluğu")));
            var u = Satir(); c.Add(u); UzunlukSatir(u, c);
            if (!saha) return;
            c.Add(Grup(L.T("Saha")));
            // 2026-11-07 inceleme (fikir 02:06 #5, defter): saha 2×2 yazı çipiydi ("Okul (ahşap)" parantezli) ve sahanın neye
            // benzediğini söylemiyordu. Şimdi tek sıra dört çip; her birinde sahanın kendi renkleriyle çizilmiş küçük kort (çevre,
            // mat, file çizgisi; okulda tahta çizgileri, gecede yıldız) ve kısa ad. Seçili çip turuncu + ✓ (yalnız renk değil).
            // Doku yok: renkler CourtView.SAHA'dan, saha rengi değişince önizleme de değişir. Dar ekranda/büyük yazıda Sigdir küçültür.
            var s1 = Satir(); c.Add(s1);
            for (int i = 0; i < 4; i++)
            {
                string id = CourtView.SIRA[i]; var sh = CourtView.SAHA[id];
                var d = Cip(L.T(SAHA_KISA[i]), Ayar.saha == id, () => { Ayar.saha = id; Ayar.Kaydet(); root.SahaDegisti(); Degisti(); });
                d.style.paddingLeft = d.style.paddingRight = 4f; d.style.minHeight = 44f;
                Erisim.Etiket(d, L.T(sh.ad), "dugme", Ayar.saha == id, false);
                var ic = d.Q(null, "cip-ic"); var alt = new VisualElement(); alt.style.flexDirection = FlexDirection.Row; alt.style.alignItems = Align.Center; alt.pickingMode = PickingMode.Ignore;
                while (ic.childCount > 0) { var ch = ic[0]; ic.RemoveAt(0); alt.Add(ch); }
                ic.style.flexDirection = FlexDirection.Column; ic.Add(MiniSaha(sh)); ic.Add(alt);
                Sigdir(d); s1.Add(d);
            }
        }
        static readonly string[] SAHA_KISA = { "Salon", "Turnuva", "Okul", "Gece" };   // CourtView.SIRA sırası; uzun ad erişim etiketinde
        static VisualElement MiniSaha(Saha sh)
        {
            var cev = new VisualElement(); cev.pickingMode = PickingMode.Ignore; cev.style.width = 44f; cev.style.height = 26f; cev.style.marginBottom = 4f;
            cev.style.backgroundColor = Kit.Hex(sh.cevre); cev.style.borderTopLeftRadius = cev.style.borderTopRightRadius = cev.style.borderBottomLeftRadius = cev.style.borderBottomRightRadius = 5f;
            cev.style.alignItems = Align.Center; cev.style.justifyContent = Justify.Center; cev.style.overflow = Overflow.Hidden;
            var kort = new VisualElement(); kort.pickingMode = PickingMode.Ignore; kort.style.width = 34f; kort.style.height = 18f; kort.style.backgroundColor = Kit.Hex(sh.kort);
            var beyaz = new Color(1, 1, 1, .75f);
            kort.style.borderTopWidth = kort.style.borderBottomWidth = kort.style.borderLeftWidth = kort.style.borderRightWidth = 1f;
            kort.style.borderTopColor = kort.style.borderBottomColor = kort.style.borderLeftColor = kort.style.borderRightColor = beyaz;
            if (sh.tahta) for (int k = 1; k < 4; k++) { var t = new VisualElement(); t.pickingMode = PickingMode.Ignore; t.style.position = Position.Absolute; t.style.left = 0f; t.style.right = 0f; t.style.top = k * 4f; t.style.height = 1f; t.style.backgroundColor = new Color(0, 0, 0, .18f); kort.Add(t); }
            var file = new VisualElement(); file.pickingMode = PickingMode.Ignore; file.style.position = Position.Absolute; file.style.top = 0f; file.style.bottom = 0f; file.style.left = 16f; file.style.width = 1f; file.style.backgroundColor = beyaz; kort.Add(file);
            cev.Add(kort);
            if (sh.ad == "Gece") for (int k = 0; k < 2; k++) { var y = new VisualElement(); y.pickingMode = PickingMode.Ignore; y.style.position = Position.Absolute; y.style.width = y.style.height = 2f; y.style.top = 2f; y.style.left = 5f + k * 30f; y.style.backgroundColor = new Color(1, .95f, .7f, .9f); cev.Add(y); }
            return cev;
        }

        // Zorluk çipleri (2026-11-05, Ayar.zorluk notu): Hızlı maç ve kariyer kartı aynı satırı kullanır (Ayarlar 2026-11-07'de çıktı). Dört kısa ad tek
        // satırda; seçilenin açıklaması altında tek soluk satır (unslop: açıklama çipe girmez). Koşu yardımı ayrı açma/kapama
        // değil, Zor ile Usta'yı ayıran şey (eski "Koşu yardımı açık/Kapalı" satırı buraya katıldı).
        static readonly string[] ZORLUK_ACIK = {
            "Oyuncun topa kendisi koşar; rakip daha yavaş, sık hata yapar",
            "Oyuncun topa kendisi koşar, sen yalnız vurursun",
            "Koşmak sende; geç kalırsan oyuncun koşar, dokunursan yine sen yönlendirirsin",
            "Her adım senin dokunuşunla; rakip daha hızlı, az hata yapar" };
        // Maç uzunluğu çipleri (tuytop_target). 2026-11-08 (inceleme turu): Hızlı maç sayfası ve kariyer maç ayrıntısı aynı satırı
        // kullanır; Ayarlar › Oyun grubu kalktı. Kariyer kartı "senin seçimin" yazıp seçimi başka sayfada değiştiriyordu, Hızlı
        // maçta 21 seçen oyuncunun kariyeri sessizce 21 sayı oluyordu. Seçim oynamadan hemen önce görülen yerde; anahtar aynı.
        void UzunlukSatir(VisualElement satir, VisualElement c)
        {
            satir.AddToClassList("cip-sarar");
            // 2026-11-08 inceleme (fikir 07:05 #3): 5 sayının açıklaması "Hızlı maç" sayfasında mod adını tekrar ediyordu; süre kaldı.
            int[] Ls = { 5, 11, 21 }; string[] La = { "5 sayı", "11 sayı", "21 sayı" }, Lb = { "Yaklaşık 2 dakika", "Kısa maç", "Resmî maç uzunluğu" };
            for (int i = 0; i < 3; i++)
            {
                int v = Ls[i]; var cp = Cip(L.T(La[i]), Ayar.target == v, () => { Ayar.target = v; Ayar.Kaydet(); Degisti(); });
                Erisim.Etiket(cp, L.T(La[i]) + ". " + L.T(Lb[i]), "dugme", Ayar.target == v); satir.Add(cp);
            }
            int si = System.Array.IndexOf(Ls, Ayar.target); if (si >= 0) c.Add(Yazi(L.T(Lb[si]), 12, "soluk"));
        }
        void ZorlukSatir(VisualElement satir, VisualElement c, bool aciklama = true)
        {
            satir.AddToClassList("cip-sarar");   // dört çip: büyük yazıda sözcük kırılmaz, satır sarar
            for (int i = 0; i < 4; i++)
            {   // VoiceOver: çip adı + kademenin açıklaması (açıklama satırı yalnız seçili için görünür; UX incelemesi 2026-11-05)
                int v = i; var cp = Cip(L.T(Ayar.ZORLUK_AD[i]), Ayar.zorluk == v, () => { Ayar.zorluk = v; Ayar.Kaydet(); Degisti(); });
                Erisim.Etiket(cp, L.T(Ayar.ZORLUK_AD[i]) + ". " + L.T(ZORLUK_ACIK[i]), "dugme", Ayar.zorluk == v); satir.Add(cp);
            }
            if (aciklama) c.Add(Yazi(L.T(ZORLUK_ACIK[Mathf.Clamp(Ayar.zorluk, 0, 3)]), 12, "soluk"));
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
