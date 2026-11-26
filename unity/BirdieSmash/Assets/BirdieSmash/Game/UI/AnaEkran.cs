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
            // 2026-11-23 Oyna A detayı (detay.html): plaka sahnenin alt üçte birine biner (yayın alt yazısı gibi); sahne ile maç bilgisi
            // tek blok okunur, plakanın yediği boşluk sahneye döner. Yalnız negatif üst boşluk (USS .plaka--sahne), dokunuş aynı.
            if (c.childCount > 0) c[c.childCount - 1].AddToClassList("plaka--sahne");
            // 2026-11-09 (yasin: "kariyeri farklı zorluklarda oynayabilme gelmesi lazım"): seçim 5 Kas'tan beri Kariyer'deki maç
            // kartındaydı, Oyna'dan oynayan görmüyordu. Aynı dört çip (tek ayar tuytop_zorluk) Oyna düğmesinin hemen üstünde;
            // açıklama satırı yok (düğmenin alt satırı kademeyi zaten söylüyor, küçük ekranda bir satır kazanılır).
            // 2026-11-23 Oyna B (yasin seçti, menu-ux-ui-2026-11-23/secim.md): zorluk şeridi Oyna'dan kalktı; seçim Ayarlar › Oyun'da ve
            // Kariyer'in maç kartında sürer (aynı tuytop_zorluk), Oyna düğmesinin alt satırı kademeyi söylemeye devam eder. Kazanılan
            // satır sahneye (afiş) gider: ekranın yarısı mekân ve iki oyuncu, altında tek Oyna.
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
                // 2026-11-23 Oyna A detayı: düğmenin sağ parçası zorluğu söyler; dokununca düğmenin altında dört kademe açılır (Kariyer'in
                // maç kartı ve Ayarlar'la aynı tuytop_zorluk). Ayrı satır kalıcı değil: Oyna B'nin sadeliği korunur, seçim bir dokunuş uzakta.
                bool bolunmus = !ilk && Ayar.antrenman && !p.HepsiBitti;
                string alt = ilk ? null : !Ayar.antrenman ? L.T("İlk kez: 3 adımlık antrenman, sonra 5 sayılık maç") : p.HepsiBitti ? L.Cumle(Ayar.UstaZor ? Career.Line(p) : Career.Line(p).Replace("Usta yolu: ", "Şampiyon yolu: "))
                    : L.T("{0} sayı", kb.uzunluk > 0 ? kb.uzunluk : Ayar.target) + (bolunmus ? "" : " · " + L.T(Ayar.ZorlukAd))   // 2026-11-09: saha adı ("Okul (ahşap)") plakadaki yer adıyla iki ayrı yer gibi okunuyordu (inceleme 10 Kas 02:15); saha maç kartında
                      + (kb.degistirici != null && Gunluk.Deg(kb.degistirici) != null ? " · " + L.T(Gunluk.Deg(kb.degistirici).ad) : "");   // kural maçı adı
                c.Add(BuyukDugme(L.T("Oyna"), alt, () =>
                {
                    if (Ayar.antrenman && !Ayar.kariyerModu) { Ayar.kariyerModu = true; Ayar.GearHesap(); Ayar.Kaydet(); }
                    Ayar.kariyerSecim = -1;   // büyük Oyna her zaman sıradakini oynatır (haritadan seçilen yeniden oynama değil)
                    HareketOnce(root.Oyna);   // 2026-11-21: ilk Oyna'dan önce bir kez "Nasıl oynamak istersin?" (ArkadasEkran.cs)
                }));
                if (bolunmus) ZorlukParca(c[c.childCount - 1], c);
            }
            // 2026-11-20 online arkadaş maçı; 2026-11-21 (yasin: "default olarak açık gelsin", "buton da ilgi çekici olsun") herkese
            // açık, düz düğme yerine kendi kartı (ArkadasEkran.OnlineKart): kutucuklardan önce, büyük Oyna'nın altında. İlk açılışta yok
            // (ilk yol tek düğme, §2.3).
            // 2026-11-23 Oyna B: kart yerine aşağıdaki mod çiplerinden biri (taslakta seçilen yerleşim); online bayrağı aynı.
            // 2026-10-26 adım 7–8: Günün maçı · Hızlı maç · Antrenman. "Tam antrenman" kartı Antrenman sayfasında (Temel ders);
            // ilk açılışta (antrenman görülmedi) Günün maçı yok: ilk yol tek düğme kalsın (§2.3).
            if (ilk) return;
            // 2026-11-23 Oyna B: üç kutucuk + arkadaş kartı tek sıra küçük mod çipine indi (simge + ad, ≥ 44 pt). Ayrıntı satırları
            // (Tüy ödülü, yıldız, sayı) çipte gizli (USS .cip-modu), ekran okuyucu etiketinde aynen kalır; sayfaları açınca görünür.
            var row = Satir("karo-satir"); row.AddToClassList("cip-modu"); c.Add(row);
            if (Ayar.antrenman && !root.bekleyen) row.Add(GunlukKaro());
            // 2026-11-23 Oyna A detayı: çipte ad + tek kısa veri satırı (önceki karonun bilgisi: sayı, yıldız, ödül); 2×2 dizilir.
            row.Add(Karo(Ikonlar.Simsek, L.T("Hızlı maç"), L.T("{0} sayı", Ayar.target), GameRoot.HizliSecili() != null ? KariyerVeri.Stil[GameRoot.HizliSecili()].isim : L.T("Rastgele rakip"), null, HizliMacAc, L.T("Hızlı maç") + ", " + HizliOzet()));
            CipAlt(row[row.childCount - 1], L.T("{0} sayı", Ayar.target) + " · " + L.T(Ayar.ZorlukAd));
            var p2 = Ayar.ilerleme;
            row.Add(Karo(Ikonlar.Hedef, L.T("Antrenman"), p2.AntrenmanYildiz + "/" + Dril.ToplamYildiz, L.T("yıldız"), Ikonlar.YildizDolu, AntrenmanAc,
                L.T("Antrenman") + ", " + L.T("{0}/{1} yıldız · Temel ders ve {2} görev", p2.AntrenmanYildiz, Dril.ToplamYildiz, Dril.Liste.Length)));
            CipAlt(row[row.childCount - 1], p2.AntrenmanYildiz + "/" + Dril.ToplamYildiz + " " + L.T("yıldız"));
            if (Online.Bayrak && !root.bekleyen)
                row.Add(Karo(Ikonlar.Arkadas, L.T("Arkadaşınla"), "", null, null, () => HareketOnce(ArkadasAc), L.T("Arkadaşınla oyna") + ", " + L.T("Canlı maç · davet gönder ya da rakip bul"), "karo--arkadas"));
            if (Online.Bayrak && !root.bekleyen) CipAlt(row[row.childCount - 1], L.T("Canlı maç"));
            // 2026-11-10 (yasin: "catch yok", kıyas: savaş geçidi şeridi, Forza seviye ödülü): sıradaki seviyenin ödülü ana ekranda
            // tek satır; dokununca Seviye yolu (Sv 30'a kadar bütün ödüller). Sv 30'da satır yok.
            row.Query<Label>(null, "karo-etiket").ForEach(e => e.style.fontSize = e.style.fontSize.value.value * 13f / 11f);   // çipte ad tek bilgi: 11 → 13
            var sh = SvHedefSatir(); if (sh != null) c.Add(sh);
            // Menü ilk kez tam açılıyor (2026-11-12 inceleme, fikir 07:05 #3): öğreticiden sonra plaka, çipler, OYNA, kutucuklar ve
            // seviye satırı tek karede düşüyordu; nereye bakılacağı belli değildi. Yalnız bir kez (tuytop_menu_acildi): öğeler
            // yukarıdan aşağı 60 ms arayla 0,25 sn'de belirir (≤ 0,6 sn), dokunuş engellenmez. Hareketi azaltta anında son hâl.
            // Ek açıklama satırı ve "YENİ" rozeti yok: plaka sıradaki maçı zaten söylüyor. Güncelleyen oyuncu bir kez görür (zararsız).
            if (!root.bekleyen && PlayerPrefs.GetInt("tuytop_menu_acildi", 0) == 0)
            {
                PlayerPrefs.SetInt("tuytop_menu_acildi", 1); Ayar.KaydetSonra();
                if (!Ayar.reduce) KademeliAc(c);
            }
        }

        void KademeliAc(VisualElement c) { Kademeli(c.Children(), 40, 60, .25f, 14, int.MaxValue); }

        // 2026-11-19 inceleme (fikir 22:06 #5): Profil "Tümünü göster" de aynı girişi kullanır (daha kısa: 30 ms ara, .18 sn, 8 pt).
        // Yalnız ilk `ust` öğe kayar, kalanlar anında; yalnız opacity/translate (yerleşim yok). Çağıran hareketi azalt'a bakar.
        void Kademeli(System.Collections.Generic.IEnumerable<VisualElement> ogeler, int ilk, int arali, float sure, float dy, int ust)
        {
            int i = 0;
            foreach (var ch in ogeler)
            {
                if (i >= ust) break;
                var v = ch; int bas = ilk + i * arali; i++;
                v.style.opacity = 0f; v.style.translate = new Translate(0, dy, 0);
                v.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("opacity"), new StylePropertyName("translate") };
                v.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(sure, TimeUnit.Second) };
                v.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
                v.schedule.Execute(() => { v.style.opacity = 1f; v.style.translate = new Translate(0, 0, 0); Kalite.Hareket(); }).StartingIn(bas);
                v.schedule.Execute(() => { v.style.opacity = StyleKeyword.Null; v.style.translate = StyleKeyword.Null; v.style.transitionProperty = StyleKeyword.Null; v.style.transitionDuration = StyleKeyword.Null; v.style.transitionTimingFunction = StyleKeyword.Null; }).StartingIn(bas + (int)(sure * 1000) + 50);
            }
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
            d.AddManipulator(new Clickable(() => { Tik(); PerdeKaynakYaz(d); a(); }));   // 2026-10-27: perde v2'nin 3B topu buradan kalkar
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
            var dk = new VisualElement(); dk.AddToClassList("karo-deger-kap"); dk.pickingMode = PickingMode.Ignore; d.Add(dk);
            var dl = Yazi(deger, 20, "karo-deger"); dl.style.whiteSpace = WhiteSpace.NoWrap; dl.style.flexShrink = 0f; dk.Add(dl);
            float dt0 = dl.style.fontSize.value.value; dk.RegisterCallback<GeometryChangedEvent>(e => SigdirOlc(dk, dl, dt0, .75f));
            if (!string.IsNullOrEmpty(alt))
            {
                var ar = new VisualElement(); ar.AddToClassList("karo-alt-satir"); ar.pickingMode = PickingMode.Ignore; d.Add(ar);
                if (altIkon != null) ar.Add(Ikon(altIkon, 12, ALTIN)); ar.Add(Yazi(alt, 12, "karo-alt"));
            }
            d.AddManipulator(new Clickable(() => { Tik(); a(); })); Erisim.Etiket(d, erisim, "dugme");
            return d;
        }
        // Çip veri satırı (2026-11-23 Oyna A): yalnız .cip-modu içinde görünür (USS), ekran okuyucu karonun etiketini okur.
        // 2026-11-26 inceleme (fikir 17:06 #2): zorluk Oyna B'de yalnız bu satırda okunuyor; 11 pt ekranın en küçük yazısıydı,
        // tasarım dilinin alt sınırı 12 pt (21 Kas Erişilebilirlik kararıyla aynı çizgi). Büyük yazıda satır zaten sarar (USS).
        void CipAlt(VisualElement karo, string t) { var l = Yazi(t, 12, "cip-alt"); l.pickingMode = PickingMode.Ignore; karo.Add(l); }
        // Bölünmüş Oyna'nın sağ parçası (2026-11-23 Oyna A): zorluk adı + aşağı ok; dokunuş üst düğmeye geçmez (Clickable
        // basışı yakalar). Dört kademe düğmenin hemen altındaki gizli satırda; seçim ekranı yeniden kurar, satır yine kapalı doğar.
        void ZorlukParca(VisualElement cta, VisualElement c)
        {
            var z = new VisualElement(); z.AddToClassList("cta-zorluk");
            z.Add(Yazi(L.T("Zorluk"), 11, "cta-zorluk-ust")); z.Add(Yazi(L.T(Ayar.ZorlukAd), 15, "cta-zorluk-ad"));
            int ik = cta.IndexOf(cta.Q<VisualElement>(null, "cta-ikon")); if (ik < 0) cta.Add(z); else cta.Insert(ik, z);
            var satir = Satir("zorluk-acilir"); satir.style.display = DisplayStyle.None; ZorlukSatir(satir, c, false); c.Add(satir);
            z.AddManipulator(new Clickable(() =>
            {
                Tik(); bool ac = satir.resolvedStyle.display == DisplayStyle.None;
                satir.style.display = ac ? DisplayStyle.Flex : DisplayStyle.None; z.EnableInClassList("cta-zorluk--acik", ac);
                Erisim.Etiket(z, L.T("Zorluk") + ", " + L.T(Ayar.ZorlukAd), "dugme", ac);
            }));
            Erisim.Etiket(z, L.T("Zorluk") + ", " + L.T(Ayar.ZorlukAd), "dugme");
        }
        // Günün maçı kutucuğu (a-tile--gold): değiştiricinin adı, altında Tüy simgesi + "+40 Tüy" (alındıysa ✓ + "Bugün kazandın").
        // r16: ödül değerin sağında mutlak konumdaydı (a-tile), "Long rally" gibi uzun adın üstüne biniyordu; alt satıra indi.
        VisualElement GunlukKaro()
        {
            var g = Bugun(); var st = Match.STIL[g.stil]; bool alindi = Ayar.ilerleme.GunlukAlindi(g.tarih);
            // 2026-11-11 inceleme (fikir 19:05 #5): kazanınca kutucuk "Bugün kazandın"da bitiyordu; yarına somut, baskısız bir neden yok.
            // Kural tarihten belirlenimli (Gunluk.Bugun): yarının adı yazılır. Geri sayım, "kaçırma" dili, rozet yok.
            string altY = alindi ? L.T("Yarın: {0}", L.T(Gunluk.Bugun(System.DateTime.Now.AddDays(1)).deg.ad)) : L.T("+{0} Tüy", Gunluk.OdulTuy);
            var d = Karo(Ikonlar.Gunes, L.T("Günün maçı"), L.T(g.deg.ad), altY, alindi ? Ikonlar.Tik : Ikonlar.Tuy, GunlukAc,
                L.T("Günün maçı") + ", " + L.T(g.deg.ad) + ", " + L.T("Rakip") + " " + st.isim + ", " + (alindi ? L.T("Bugün kazandın") + ", " + altY : altY), "karo--altin");
            d.AddToClassList("karo--genis"); Doku(d, Ikonlar.AltinKaro); CipAlt(d, altY);
            // 2026-11-22 inceleme (fikir 07:06 #3): günün rakibinin portresi Oyna durgunken bir kez ısıtılır; Günün maçı sayfası
            // açıldığı karede FBX çizmez (Kariyer şeridi kalıbı, kare başına en çok bir ısıtma).
            if (Portre.RakipHazir(st) == null)
            {
                bool isitti = false;
                d.schedule.Execute(() => { if (isitti || !IsitmaSirasi()) return; isitti = true; Portre.Rakip(st); }).Every(300).Until(() => isitti || d.panel == null);
            }
            return d;
        }

        // Yayın plakası (a-plate): "Sıradaki" kartının yeni biçimi; içerik aynı, dokunuş maç kartını açar (2026-10-26). Rakip rengi yanında adıyla
        // (yalnız renkle değil), yıldızlar dolu/çerçeve biçimiyle ve ekran okuyucuya "1/3 yıldız"; basamak çizgisi biçimle
        // (geçilen dolu limon, şimdiki çizgili desen, gelecek soluk).
        VisualElement SiradakiKart()
        {
            var p = Ayar.ilerleme; int g = p.Siradaki(), tr = KariyerVeri.TurOf(g), i = KariyerVeri.BasOf(g); var kb = KariyerVeri.Al(g); var st = KariyerVeri.Rakip(g);
            bool tamam = p.HepsiBitti;
            var kart = new VisualElement(); kart.AddToClassList("plaka"); Doku(kart, Ikonlar.Plaka); // 2026-10-26 (yasin onayı, ekran/inceleme ile aynı davranış): plakaya dokunmak o basamağın maç kartını açar; kariyer
            // bittiyse (sıradaki basamak yok) Kariyer sekmesi.
            // Birleşme (2026-10-26, A + Ar-Ge) titreşimi iki yola da koymuştu; SekmeAc sekme değişince zaten "tick" verir: Kariyer
            // yolunda iki titreşim olmasın diye yalnız maç kartı yolunda.
            kart.AddManipulator(new Clickable(() => { if (tamam) SekmeAc(Sekme.Kariyer); else { Tik(); MacKartAc(g); } }));
            var bas = new VisualElement(); bas.AddToClassList("plaka-ust"); bas.pickingMode = PickingMode.Ignore; kart.Add(bas);
            // 2026-11-16 inceleme (fikir 02:05 #2, docs/arayuz_tasarim_dili.md §8): altın = ödül, turuncu = bas. "Sıradaki maç" bir
            // ödül değil; şeritsiz soluk etiket. Altın hap yalnız "Kariyer tamam" (gerçek ödül anı) kalır.
            var serit = new VisualElement(); serit.AddToClassList("serit"); if (tamam) Doku(serit, Ikonlar.EgikLimonKucuk); else serit.AddToClassList("serit--duz"); serit.pickingMode = PickingMode.Ignore; bas.Add(serit);
            serit.Add(Yazi(L.T(tamam ? "Kariyer tamam" : "Sıradaki maç"), 13, "serit-yazi"));   // 2026-11-06 unslop: büyük harf + geniş aralık etiket yerine cümle düzeni
            bas.Add(Yazi(L.T(TurAd(tr)) + " · " + (i + 1) + "/" + KariyerVeri.BASAMAK, 12, "plaka-tur"));
            if (K <= 1.35f) { var pb = Yazi(Buyuk(L.T(kb.yer)), Px(38, 32), "baslik"); pb.AddToClassList("plaka-baslik"); SatirSigdir(pb); kart.Add(pb); }   // 2026-11-16: tek satır (UiOge.SatirSigdir)
            else kart.Add(Baslik(L.T(kb.yer), Px(38, 32), "plaka-baslik"));
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
                bc.Add(b);   // 2026-11-16 (§8): şimdiki basamak çizgili turuncu doku yerine açık çerçeve (USS); geçilen limon = senin yolun
            }
            BasamakDolar(bc, tr, tamam);
            Erisim.Etiket(kart, L.T(tamam ? "Kariyer tamam" : "Sıradaki maç") + ", " + L.T(TurAd(tr)) + ", " + L.T(kb.yer) + ", " + L.T("Rakip") + " " + st.isim + " · " + L.T(st.ad) + ", " + L.T(CourtView.SAHA[kb.saha].ad) + ", " + (i + 1) + "/" + KariyerVeri.BASAMAK + ", " + L.T("{0}/3 yıldız", p.Yildiz(g)), "dugme");
            return kart;
        }

        // Basamak dolumu (2026-11-16 inceleme, fikir 02:05 #5): galibiyetten Oyna'ya dönen oyuncu yeni basamağı hazır görüyordu;
        // "ilerledim" anı yoktu (Tennis Clash tur haritası, Badminton League lig çubuğu dönüşte dolar). Oturum içi bellek (yeni
        // tuytop anahtarı yok): aynı turda geçilen sayısı arttıysa yalnız son geçilen basamak soldan sağa 0,35 sn dolar (scale,
        // yerleşim yeniden hesaplanmaz), bitince tek "tick". Soğuk açılışta, tur değişince, kariyer bitince ve hareketi azaltta
        // oynamaz. Maç sonu perdesi kalkmadan başlamaz (en çok 6 sn bekler). Takılma: ayırma yok, tek öğede stil geçişi.
        static int sonTur = -1, sonGecti = -1;
        void BasamakDolar(VisualElement bc, int tr, bool tamam)
        {
            VisualElement yeni = null; int say = 0;
            foreach (var ch in bc.Children()) if (ch.ClassListContains("basamak--gecti")) { say++; yeni = ch; }
            bool oynat = !tamam && !Ayar.reduce && yeni != null && sonTur == tr && sonGecti >= 0 && say > sonGecti;
            sonTur = tr; sonGecti = say;
            if (!oynat) return;
            var b = yeni; b.AddToClassList("basamak--dolar"); b.style.scale = new Scale(new Vector3(0f, 1f, 1f));
            float t0 = Time.realtimeSinceStartup; bool bitti = false;
            b.schedule.Execute(() =>
            {
                if (bitti) return;
                if (b.panel == null) { bitti = true; return; }   // yüzey yeniden kuruldu: yeni öğe son hâlde
                var hud = root.HudKatman; if (hud != null && hud.PerdeVar && Time.realtimeSinceStartup - t0 < 6f) return;
                bitti = true; Kalite.Hareket(); b.style.scale = new Scale(Vector3.one);
                b.schedule.Execute(() => { if (b.panel != null) Yerel.Hap("tick"); }).StartingIn(350);
            }).StartingIn(150).Every(100).Until(() => bitti);
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
            // iki satır küçük ekranda Oyna düğmesini iterdi).
            // 2026-11-15 (inceleme, fikir 22:05 #7): puanı bekleten oyuncu maç → XP → seviye ödülü döngüsünün tek göstergesini ana
            // ekranda hiç görmüyordu (Badminton League / Tennis Clash çubuğu hep tutar). Satır yine tek: XP çubuğu ve seviye hep
            // görünür; dağıtılmamış puan varken yalnız sağ blok "+Tüy" yerine "Dağıt (n) ›" olur ve dokunuş Gelişim'i açar.
            // Nokta/rozet yok, yazıyla (YAPMA listesindeki "seviye eşyasına nokta" ile çelişmez).
            int L_, kalan, gerek; Ayar.gear.OyuncuSv(out L_, out kalan, out gerek); if (gerek <= 0) return EfsaneSatir() ?? (Ayar.GelisimBos > 0 ? GelisimSatir() : null);
            int sv = L_ + 1; string ac = SvAcilanOzet(sv, 1);
            var d = new VisualElement(); d.AddToClassList("sv-hedef");
            var sol = new VisualElement(); sol.AddToClassList("sv-hedef-sol"); sol.pickingMode = PickingMode.Ignore; d.Add(sol);
            sol.Add(Yazi(L.T("Sv {0} ödülü · {1} XP kaldı", sv, gerek - kalan), 14, "kalin"));
            var c2 = Cubuk(kalan / (float)gerek, LIMON, 5); c2.AddToClassList("sv-hedef-cubuk"); sol.Add(c2); // 2026-11-20 yasin karar kartı "A · Limon": seviye/XP = limon, turuncu yalnız eylem/seçim (§8)
            var sag = new VisualElement(); sag.AddToClassList("sv-hedef-sag"); sag.pickingMode = PickingMode.Ignore; d.Add(sag);
            string bas = L.T("Sv {0} ödülü · {1} XP kaldı", sv, gerek - kalan);
            if (SvSagGelisim(d, sag, bas)) return d;
            sag.Add(Ikon(Ikonlar.Tuy, 16, ALTIN)); sag.Add(Yazi("+" + Gear.SvOdul(sv), 16, "rakam"));
            // açılan eşya sol blokta, çubuğun altında: sağda dururken 375 pt'te başlığı iki satıra sarıyordu
            if (ac != null && K <= 1.35f) { var a = Yazi(ac, 12, "soluk"); a.AddToClassList("sv-hedef-ac"); sol.Add(a); }
            d.AddManipulator(new Clickable(() => { Tik(); SeviyeYoluAc(); }));
            Erisim.Etiket(d, bas + ", " + L.T("+{0} Tüy", Gear.SvOdul(sv)) + (ac != null ? ", " + ac : "") + ", " + L.T("Seviye yolu"), "dugme");
            return d;
        }
        // Dağıtılmamış puan varken sağ blok (2026-11-15): "Dağıt (n) ›", dokunuş Gelişim; true = satır hazır.
        bool SvSagGelisim(VisualElement d, VisualElement sag, string bas)
        {
            int bos = Ayar.GelisimBos; if (bos <= 0) return false;
            d.AddToClassList("sv-hedef--gelisim");
            sag.Add(Yazi(L.T("Dağıt ({0})", bos) + " ›", 15, "kalin"));
            d.AddManipulator(new Clickable(() => { Tik(); GelisimAc(); }));
            Erisim.Etiket(d, bas + ", " + L.T("Dağıtılacak puan: {0}", bos) + ", " + L.T("Gelişim: oyuncunu güçlendir"), "dugme");
            return true;
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
            if (SvSagGelisim(d, sag, bas)) return d;
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
            if (gerek > 0) { var cb = Cubuk(kalan / (float)gerek, LIMON, 8); cb.style.marginTop = 6; cb.style.marginBottom = 4; c.Add(cb); }
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
            RakipSatir(c);
            OyunSecenek(c, true);
            AltEylem(Dugme(L.T("Hızlı maç oyna"), () =>
            {
                gez.Kapat(Gezinti.HIZLI); Uygula(gez.sekme, false);   // 2026-11-21 (fikir 22:06 #1): ilk maça giden her yolda bir kez sorulur; sayfa kapatılırsa mod yazılmaz
                HareketOnce(() => { Ayar.kariyerModu = false; Ayar.GearHesap(); Ayar.Kaydet(); root.Oyna(); });
            }, "dugme--birincil", K > 1.35f ? null : HizliOzet(), 19));   // 2026-11-07 (UX2): büyük yazıda özet satırı CTA'yı 3 satıra büyütüp listeyi daraltıyordu; seçimler çiplerde zaten görünür
        }

        // Hızlı maç rakibi (2026-11-11 inceleme, fikir 22:05 #1): rakip yalnız rastgeleydi, oyuncu "file ustasına karşı nasıl
        // oynanır"ı çalışamıyordu. "Rastgele" (varsayılan, bugünkü davranış) + havuzdaki tarzlar: ilk üç ve kariyerde yendiklerin;
        // yenilmemiş tarz listede yok (kilitli kalabalık değil). Çipte rakibin adı, altında tarzı; seçili turuncu + ✓ (Cip).
        // Satır sarar (cip-sarar), büyük yazıda çipler alt alta iner. Altında tek soluk satır: kariyerin listeyi büyüttüğü.
        void RakipSatir(VisualElement c)
        {
            c.Add(Grup(L.T("Rakip")));
            var r = Satir(); r.AddToClassList("cip-sarar"); r.AddToClassList("cip-iki"); c.Add(r);
            string sec = GameRoot.HizliSecili();
            r.Add(Cip(L.T("Rastgele"), sec == null, () => { Ayar.hizliRakip = ""; Ayar.KaydetSonra(); Degisti(); }, L.T("her maç başka")));
            foreach (var id in KariyerVeri.HizliHavuz(Ayar.ilerleme))
            {
                var st = KariyerVeri.Stil[id]; string sid = id;
                int kG, kY; string alt = Ayar.Karne(id, out kG, out kY) ? L.T(st.ad) + " · " + kG + "–" + kY : L.T(st.ad);   // karne (fikir 23:05 #2): sayıyla, renkle değil
                r.Add(Cip(st.isim, sec == id, () => { Ayar.hizliRakip = sid; Ayar.KaydetSonra(); Degisti(); }, alt));
            }
            // 2026-11-14 inceleme (fikir 02:05 #4): not havuz tamken de yazıyordu; yalnız kariyerde yenilmemiş rakip kaldıysa.
            if (KariyerVeri.HizliHavuzEksik(Ayar.ilerleme)) { var n = Yazi(L.T("Kariyerde yendiğin rakipler buraya eklenir."), 13, "soluk"); n.style.whiteSpace = WhiteSpace.Normal; c.Add(n); }
        }

        void AltSayfaYenidenKur(string altS) { if (altS == Gezinti.SEVIYE) SeviyeYoluKur(); else if (altS == Gezinti.GELISIM) GelisimKur(); else if (altS == Gezinti.KART) MacKartKur(); else if (altS == Gezinti.YUVA) YuvaSayfaKur(); else if (altS == Gezinti.DRIL) DrilKartKur(); else if (altS == Gezinti.GUNLUK) GunlukKartKur(); else if (altS == Gezinti.SEC) OyuncuSecKur(); else if (altS == Gezinti.ARKADAS) ArkadasKur(); else if (altS == Gezinti.HAREKET) HareketSecKur(); else HizliKur(); }

        // Alt sayfa iskeleti (Hızlı maç ve kariyer maç kartı ortak, 2026-10-26): tutamak (aşağı çekince kapanır, iOS kalıbı;
        // ayrıca Kapat, Geri ve zemine dokunma), başlık, kaydırmalı gövde. Dönüş: gövdenin içerik kabı.
        VisualElement AltSayfaKur(string baslik)
        {
            altKatman.Clear(); altSayfa = new VisualElement(); altSayfa.AddToClassList("alt-sayfa"); altSayfa.style.paddingBottom = altBosluk + 16; altKatman.Add(altSayfa);
            var tut = new VisualElement(); tut.AddToClassList("tutamak-alan"); var cz = new VisualElement(); cz.AddToClassList("tutamak"); cz.pickingMode = PickingMode.Ignore; tut.Add(cz); altSayfa.Add(tut);
            // 2026-11-11 inceleme (fikir 19:05 #1): tutamak sürüklemede sayfayı hiç kaydırmıyor, 60 px'te bir anda kapatıyordu ("bozuk"
            // sanılıyordu). Artık sayfa parmakla iner, zemin karartması aynı oranda solar; bırakınca yüksekliğin %25'i ya da hızlı fiske
            // (~900/sn) aşağı kayıp kapatır, değilse USS geçişiyle (.22 sn ease-out) yerine yaylanır (iOS sheet kalıbı, HIG Sheets).
            var sayfaV = altSayfa;
            float basY = 0, sonY = 0, sonT = 0, hiz = 0; bool cek = false;
            tut.RegisterCallback<PointerDownEvent>(e => { basY = sonY = e.position.y; sonT = Time.unscaledTime; hiz = 0; cek = true; tut.CapturePointer(e.pointerId); AltSurukle(sayfaV, 0, true); });
            tut.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!cek) return; float y = e.position.y, tn = Time.unscaledTime, dt = tn - sonT;
                if (dt > 0.0001f) hiz = Mathf.Lerp(hiz, (y - sonY) / dt, 0.5f); sonY = y; sonT = tn;
                AltSurukle(sayfaV, Mathf.Max(0, y - basY), true);
            });
            System.Action<int> birak = pid =>
            {
                if (!cek) return; cek = false; if (tut.HasPointerCapture(pid)) tut.ReleasePointer(pid);
                float dy = Mathf.Max(0, sonY - basY), h = Mathf.Max(1, sayfaV.resolvedStyle.height);
                if (dy > h * 0.25f || (hiz > 900 && dy > 12)) AltKapatKay(); else AltSurukle(sayfaV, 0, false);
            };
            tut.RegisterCallback<PointerUpEvent>(e => birak(e.pointerId));
            tut.RegisterCallback<PointerCancelEvent>(e => birak(e.pointerId));
            var bas = Satir("arali"); altSayfa.Add(bas);
            // 2026-11-05 unslop: başlık içerik genişliğinde kalıp Kapat'ın yanında iki satıra kırılıyordu ("Köşe / smacı"); kalan
            // genişliği alır, Kapat daralmaz.
            var t = Yazi(baslik, 19, "sayfa-baslik"); Erisim.Etiket(t, baslik, "baslik"); t.style.flexGrow = 1f; t.style.flexShrink = 1f; t.style.minWidth = 0f; t.style.marginRight = 12f; bas.Add(t);
            var kp = Dugme(L.T("Kapat"), AltKapatKay, "kucuk-dugme"); kp.AddToClassList("kapat-dugme"); kp.style.flexShrink = 0f; bas.Add(kp);
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
            var z = Satir(); z.AddToClassList("sigdir-grup"); c.Add(z);
            // 2026-11-05 unslop incelemesi (yasin "yap"): açıklama çipin içindeydi, çipler üç satıra kırılıyordu; "Kolay · topa kendin
            // gidersin" de ters okunuyordu (Kolay'da koşu otomatik). Çipte kısa ad, seçilenin açıklaması altında tek soluk satır.
            ZorlukSatir(z, c); }
            c.Add(Grup(L.T("Maç uzunluğu")));
            var u = Satir(); c.Add(u); UzunlukSatir(u, c);
            if (!saha) return;
            c.Add(Grup(L.T("Saha")));
            // 2026-11-07 inceleme (fikir 02:06 #5, defter): saha 2×2 yazı çipiydi ("Okul (ahşap)" parantezli) ve sahanın neye
            // benzediğini söylemiyordu. Şimdi tek sıra çip (2026-11-26 Neon salon ile beş); her birinde sahanın kendi renkleriyle çizilmiş küçük kort (çevre,
            // mat, file çizgisi; okulda tahta çizgileri, gecede yıldız) ve kısa ad. Seçili çip turuncu + ✓ (yalnız renk değil).
            // Doku yok: renkler CourtView.SAHA'dan, saha rengi değişince önizleme de değişir. Dar ekranda/büyük yazıda Sigdir küçültür.
            var s1 = Satir(); s1.AddToClassList("sigdir-grup"); c.Add(s1);   // dört ad tek ölçüde (SigdirOlc grup)
            for (int i = 0; i < CourtView.SIRA.Length; i++)
            {
                string id = CourtView.SIRA[i]; var sh = CourtView.SAHA[id];
                var d = Cip(L.T(SAHA_KISA[i]), Ayar.saha == id, () => { Ayar.saha = id; Ayar.KaydetSonra(); root.SahaSecildi(); Degisti(); });
                d.style.paddingLeft = d.style.paddingRight = 4f; d.style.minHeight = 44f;
                Erisim.Etiket(d, L.T(sh.ad), "dugme", Ayar.saha == id, false);
                var ic = d.Q(null, "cip-ic"); var alt = new VisualElement(); alt.style.flexDirection = FlexDirection.Row; alt.style.alignItems = Align.Center; alt.pickingMode = PickingMode.Ignore;
                while (ic.childCount > 0) { var ch = ic[0]; ic.RemoveAt(0); alt.Add(ch); }
                ic.style.flexDirection = FlexDirection.Column; ic.Add(MiniSaha(sh)); ic.Add(alt);
                Sigdir(d); s1.Add(d);
            }
        }
        static readonly string[] SAHA_KISA = { "Neon", "Salon", "Turnuva", "Okul", "Gece" };   // CourtView.SIRA sırası; uzun ad erişim etiketinde
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
            if (sh.neon) { kort.style.borderTopColor = kort.style.borderBottomColor = kort.style.borderLeftColor = kort.style.borderRightColor = new Color(.6f, 1, 1, .95f); file.style.backgroundColor = new Color(.6f, 1, 1, .95f); cev.style.borderBottomWidth = 2f; cev.style.borderBottomColor = new Color(1, .31f, .78f, .9f); }   // neon: camgöbeği çizgi, pembe kenar ışığı
            if (sh.ad == "Gece") for (int k = 0; k < 2; k++) { var y = new VisualElement(); y.pickingMode = PickingMode.Ignore; y.style.position = Position.Absolute; y.style.width = y.style.height = 2f; y.style.top = 2f; y.style.left = 5f + k * 30f; y.style.backgroundColor = new Color(1, .95f, .7f, .9f); cev.Add(y); }
            return cev;
        }

        // Zorluk çipleri (2026-11-05, Ayar.zorluk notu): Hızlı maç ve kariyer kartı aynı satırı kullanır (Ayarlar 2026-11-07'de çıktı). Dört kısa ad tek
        // satırda; seçilenin açıklaması altında tek soluk satır (unslop: açıklama çipe girmez). Koşu yardımı ayrı açma/kapama
        // değil, Zor ile Usta'yı ayıran şey (eski "Koşu yardımı açık/Kapalı" satırı buraya katıldı).
        // 2026-11-14 (yasin: "zorluk seviyesi rakibin zorluğunu ifade etsin"): açıklamalar yalnız rakibi anlatır; koşu Ayarlar ›
        // Oyuncu hareketi (Ayar.hareket, HAREKET_ACIK).
        static readonly string[] ZORLUK_ACIK = {
            "Rakip yavaş, sık hata yapar; geride kalınca gevşer",
            "Rakip dengeli; geride kalınca biraz gevşer",
            "Rakip hızlı, az hata yapar",
            "Rakip en güçlü hâlinde; hiç gevşemez" };
        internal static readonly string[] HAREKET_ACIK = {
            "Oyuncun topa kendisi koşar, sen yalnız vurursun",
            "Koşmak sende; geç kalırsan oyuncun koşar, dokunursan yine sen yönlendirirsin",
            "Her adım senin dokunuşunla" };
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
                int v = Ls[i]; var cp = Cip(L.T(La[i]), Ayar.target == v, () => { Ayar.target = v; Ayar.KaydetSonra(); Degisti(); });
                Erisim.Etiket(cp, L.T(La[i]) + ". " + L.T(Lb[i]), "dugme", Ayar.target == v); satir.Add(cp);
            }
            int si = System.Array.IndexOf(Ls, Ayar.target); if (si >= 0) c.Add(Yazi(L.T(Lb[si]), 12, "soluk"));
        }
        void ZorlukSatir(VisualElement satir, VisualElement c, bool aciklama = true)
        {
            satir.AddToClassList("cip-sarar");   // dört çip: büyük yazıda sözcük kırılmaz, satır sarar
            for (int i = 0; i < 4; i++)
            {   // VoiceOver: çip adı + kademenin açıklaması (açıklama satırı yalnız seçili için görünür; UX incelemesi 2026-11-05)
                int v = i; var cp = Cip(L.T(Ayar.ZORLUK_AD[i]), Ayar.zorluk == v, () => { Ayar.zorluk = v; Ayar.KaydetSonra(); Degisti(true); });
                Erisim.Etiket(cp, L.T(Ayar.ZORLUK_AD[i]) + ". " + L.T(ZORLUK_ACIK[i]), "dugme", Ayar.zorluk == v);
                // 2026-11-14 inceleme (ikinci göz, r168): dört kademe yazı genişliğinde ve seçilenin ✓'ü onu büyütüyordu; satır
                // eğri duruyor, seçim değişince çipler yer değiştiriyordu (Maç uzunluğu satırı eşit). Normal yazıda dört eşit pay;
                // büyük yazıda (K > 1,35) bugünkü sarma kalır.
                if (K <= 1.35f) { cp.style.flexGrow = 1f; cp.style.flexBasis = 0f; }
                satir.Add(cp);
            }
            // 2026-11-16 inceleme (fikir 02:05 #3, HIG segmented control): dört ayrı cam çip göze dört kart sayılıyordu (Oyna'da
            // §7.3 bütçesi aşılıyordu). Normal yazıda tek cam kapsül içinde dört segment (USS .segment); büyük yazıda sarma kalır.
            // 2026-11-16 12:05 inceleme (#1): sınıf genel ".segment" idi; Dolap/Karakter/Antrenman sekme şeritleri de "segment"
            // taşıdığı için cam kapsül + turuncu çerçeve onlara bulaşıyor, AD1'in "sekme = çizgi" dili bozuluyordu. Ayrı ad.
            // (#4, HIG UISegmentedControl): seçili zemin dört çipte değil tek "thumb" öğede; dokununca eski kademeden yenisine kayar.
            if (K <= 1.35f) { satir.AddToClassList("segment--kapsul"); KapsulThumb(satir, Mathf.Clamp(Ayar.zorluk, 0, 3)); }
            if (aciklama) c.Add(Yazi(L.T(ZORLUK_ACIK[Mathf.Clamp(Ayar.zorluk, 0, 3)]), 12, "soluk"));
        }
        // Kapsül thumb'ı (2026-11-16 12:05 inceleme #4): zorluk dokunuşu yüzeyi baştan kuruyor (#124 kapsamı); thumb eski kademenin
        // yerinde, geçişsiz doğar, bir sonraki karede yeni kademeye translate ile kayar (.2 sn, USS). Yalnız transform: düzen yeniden
        // hesaplanmaz, sınıf yalnız kurulumda yazılır. Hareketi azaltta (.az-hareket) süre 0. Konum ölçülen çipten (yüzde değil):
        // kapsül iç boşluğu ve ✓ genişliği yüzdeyle kayardı. Eski kademe tek int; Oyna ve Kariyer aynı değeri paylaşır.
        static int kapsulEski = -1;
        void KapsulThumb(VisualElement satir, int hedef)
        {
            int eski = kapsulEski < 0 ? hedef : kapsulEski; kapsulEski = hedef;
            var th = new VisualElement(); th.AddToClassList("segment-thumb"); th.AddToClassList("anisiz"); th.pickingMode = PickingMode.Ignore;
            th.style.width = Length.Percent(25f); th.style.translate = new Translate(Length.Percent(eski * 100f), 0);
            satir.Insert(0, th); bool ilk = true;
            satir.RegisterCallback<GeometryChangedEvent>(e =>
            {
                int ci = ilk && eski != hedef ? eski : hedef; var cp = ci + 1 < satir.childCount ? satir[ci + 1] : null; if (cp == null) return;
                th.style.width = cp.layout.width; th.style.translate = new Translate(cp.layout.x, 0);
                if (!ilk) return; ilk = false;
                if (eski == hedef) { th.RemoveFromClassList("anisiz"); return; }
                th.schedule.Execute(() =>
                {
                    var hc = hedef + 1 < satir.childCount ? satir[hedef + 1] : null; if (hc == null) return;
                    th.RemoveFromClassList("anisiz"); th.style.width = hc.layout.width; th.style.translate = new Translate(hc.layout.x, 0);
                });
            });
        }
        // Ayar değişti: görünen yüzey (alt sayfa / sayfa / sekme) yeniden kurulur; kaydırma yeri ScrollView'de kalır.
        // yalnizZorluk (2026-11-16 inceleme, fikir 02:05 #4): zorluk çipi dört sekmeyi ve üst şeridi kirletiyordu; Dolap ve Profil
        // bir sonraki açılışta boşuna yeniden kuruluyordu. Zorluğu yalnız Oyna (çipler, OYNA alt satırı) ve Kariyer (maç kartı)
        // gösterir; yalnız onlar kirlenir, üst şerit kurulmaz. Görünen yüzey bugünkü gibi yeniden kurulur (✓ ölçüsü Sigdir'de).
        // Ayrı aşırı yükleme (isteğe bağlı parametre değil): Degisti yöntem grubu olarak Action'a veriliyor (KarakterOdasi, SoyunmaOdasi,
        // DegistiSonra); isteğe bağlı parametre o dönüşümü kırıyordu (r195 CS1503).
        void Degisti() { Degisti(false); }
        void Degisti(bool yalnizZorluk)
        {
            if (yalnizZorluk) { kirli[(int)Sekme.Oyna] = true; kirli[(int)Sekme.Kariyer] = true; }
            else { for (int i = 0; i < 4; i++) kirli[i] = true; ustKirli = true; }
            string altS = gez.AcikAltSayfa;
            if (altS != null) { var eski = altSayfa != null ? altSayfa.Q<ScrollView>() : null; Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero; AltSayfaYenidenKur(altS); KaydirmaGeri(altSayfa, of); }
            string u = gez.Ust; if (u != null && !Gezinti.AltSayfa(u)) { var eski = sayfa.Q<ScrollView>(); Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero; SayfaKur(u); KaydirmaGeri(sayfa, of); }
            else if (u == null) SekmeYerindeKur(gez.sekme);
            if (ustKirli) UstKur();
        }
        // Sekme gövdesini kaydırma yerini koruyarak yeniden kurar (Degisti ve Profil'in unvan/ad seçimi, 2026-11-19).
        void SekmeYerindeKur(Sekme s)
        {
            var k = sekmeKok[(int)s]; var eski = k.Q<ScrollView>(null, "govde-kaydir") ?? k.Q<ScrollView>(); Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero; SekmeKur(s); KaydirmaGeri(k, of);
        }
        // Yeniden kurulan gövdede kaydırma yeri korunur (yerleşimden sonra; ilk karede içerik yüksekliği henüz yok).
        static void KaydirmaGeri(VisualElement v, Vector2 of)
        {
            if (v == null || of.y <= 0) return; var sv = v.Q<ScrollView>(null, "govde-kaydir") ?? v.Q<ScrollView>(); if (sv == null) return;
            sv.scrollOffset = of; sv.schedule.Execute(() => sv.scrollOffset = of).StartingIn(33);
        }
    }
}
