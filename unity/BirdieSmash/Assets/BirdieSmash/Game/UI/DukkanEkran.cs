// Tüy dükkânı sayfası (2026-11-01, yasin: "tüy aldırma ya da IAP altyapısı"; karar kartı: önce iCloud). Üst çubuktaki Tüy
// hapına dokununca açılır (tam sayfa, Geri ile kapanır). Dürüst dükkân kuralları (ekonomi tasarımı §4): rastgele sandık
// yok, geri sayım yok, "son şans" yok; her kartta ne kadar Tüy geldiği ve mağazanın gerçek fiyatı. Bonus yüzdesi yazıyla
// ("+%17 fazla"), yalnız renkle değil. Fiyat mağazadan gelmediyse kart "şu an satın alınamıyor" der, fiyat uydurmaz.
// Altta: iCloud durumu (yedekleniyor mu) ve App Store'un istediği "Satın alımları geri yükle".
// 2026-11-12 inceleme turu (fikir 02:05, dükkân): hedef satırı + "Hedefine yeter", eş hizalı kartlar ve paket demeti, arka planda
// erken başlatma + "Fiyat alınıyor…", tek hata kartı + "Yeniden dene", satın alınan karttan cüzdana tüy uçuşu.
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        bool dukkanDinliyor, magazaIsitma; string dukkanNot, dukkanAlinan; int dukkanUcusOnce = -1; float dukkanY; ScrollView dukkanSv;

        public void DukkanAc() { var once = gez.sekme; dukkanY = 0; Magaza.O.Baslat(); gez.Ac(Gezinti.DUKKAN); Uygula(once, true); }

        // 2026-11-12 inceleme (fikir 02:05 #3a): mağaza yalnız dükkân ilk açılınca başlıyordu; oyuncu önce beş "Satın alınamıyor",
        // birkaç saniye sonra baştan kurulan sayfa görüyordu. Maçtan menüye dönüşte, menüde 8 sn kalınırsa arka planda başlar
        // (soğuk açılışta ve maçta değil; Baslat tekrar çağrılırsa bir şey yapmaz, Hata'da yeniden dener).
        void MagazaOnIsit()
        {
            if (magazaIsitma || Magaza.O.durum != Magaza.Durum.Kapali || kok == null) return; magazaIsitma = true;
            kok.schedule.Execute(() => { magazaIsitma = false; if (root.ekran == Ekran.Menu) Magaza.O.Baslat(); }).StartingIn(8000);
        }

        // 2026-11-12 inceleme (#3c): mağaza durumu her değiştiğinde sayfa baştan kuruluyordu, kaydırma başa dönüyordu. Sayfa yine
        // yeniden kurulur (tek yol, durum dalları basit kalır) ama kaydırma konumu korunur.
        void DukkanYenile()
        {
            if (gez.Ust != Gezinti.DUKKAN) return;
            dukkanY = dukkanSv != null ? dukkanSv.scrollOffset.y : 0; SayfaKur(Gezinti.DUKKAN);
        }

        void DukkanDinle()
        {
            if (dukkanDinliyor) return; dukkanDinliyor = true;
            Magaza.O.Degisti += DukkanYenile;
            Magaza.O.ReklamsizBitti += () => { dukkanNot = L.T("Reklamsız açık: menüdeki reklam şeridi kalktı."); root.sfx.Tik(); Yerel.Hap("win"); DukkanYenile(); };
            Magaza.O.Bitti += n =>
            {
                dukkanNot = n > 0 ? L.T("{0} Tüy cüzdanında.", UiKok.Sayi(n)) : L.T("Satın alma tamamlanmadı, ücret alınmadı.");
                if (n > 0)
                {
                    root.sfx.Tik(); Yerel.Hap("win"); ustKirli = true;
                    // 2026-11-12 inceleme (#5): tüyler satın alınan karttan cüzdan rakamına uçar, rakam sayar. Üst şerit menüye
                    // dönünce aynı artışı ikinci kez uçurmasın diye gösterilen değer burada güncellenir.
                    if (gez.Ust == Gezinti.DUKKAN && !Ayar.reduce) dukkanUcusOnce = Mathf.Max(0, Ayar.gear.eko.tuy - n);
                    gosterilenTuy = Ayar.gear.eko.tuy;
                }
                DukkanYenile();
            };
        }

        // Paket büyüklüğü görselde (2026-11-12 inceleme, #2b): dört pakette aynı tek tüy vardı, 1.000 ile 20.000 yalnız rakamla
        // ayrılıyordu. Paketin sırası kadar tüy hafif yelpaze (aynı ikon, yeni varlık yok); büyüklük süs değil, anlam.
        VisualElement Demet(int n)
        {
            var d = new VisualElement(); d.AddToClassList("dukkan-demet"); d.pickingMode = PickingMode.Ignore;
            d.style.width = 26 + (n - 1) * 8; d.style.height = 34;
            for (int i = 0; i < n; i++)
            {
                var f = Ikon(Ikonlar.Tuy, 26, ALTIN); f.pickingMode = PickingMode.Ignore; f.style.position = Position.Absolute;
                f.style.left = i * 8; f.style.top = 4 + Mathf.Abs(i - (n - 1) * .5f) * 2;
                f.style.rotate = new Rotate(new Angle((i - (n - 1) * .5f) * 14f, AngleUnit.Degree)); d.Add(f);
            }
            return d;
        }

        void DukkanKur(VisualElement p)
        {
            DukkanDinle();
            SayfaBasi(p, "Tüy dükkânı");
            var sv = Kaydirma(p); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            dukkanSv = sv; float y0 = dukkanY; dukkanY = 0;
            if (y0 > 0) { EventCallback<GeometryChangedEvent> yer = null; yer = e => { c.UnregisterCallback(yer); sv.scrollOffset = new Vector2(0, y0); }; c.RegisterCallback(yer); }
            int tuy = Ayar.gear.eko.tuy, ucusOnce = dukkanUcusOnce; dukkanUcusOnce = -1;
            var ust = Kart("dukkan-cuzdan"); c.Add(ust);
            var cz = new VisualElement(); cz.AddToClassList("satir"); cz.pickingMode = PickingMode.Ignore; ust.Add(cz);
            var czIkon = Ikon(Ikonlar.Tuy, 26, ALTIN); cz.Add(czIkon); var czSayi = Yazi(UiKok.Sayi(ucusOnce >= 0 ? ucusOnce : tuy), 26, "rakam"); cz.Add(czSayi);
            Erisim.Etiket(ust, L.T("Cüzdanında {0} Tüy", tuy), "metin");
            // 2026-11-12 inceleme (#1): satın almanın nedeni (Dolap/Atölye'deki hedef eşya) dükkânda görünmüyordu, oyuncu kafasında
            // hesap yapıyordu. Hedef varsa tek satır; "≈ N maç" aynı satırda oynayarak kazanma yolunu da gösterir (baskı yok).
            var hd = Ayar.HedefBilgi(); int eksik = hd.gecerli ? Hedef.Kalan(hd.fiyat, tuy) : 0;
            if (eksik > 0)
            {
                int mt = Hedef.MacTahmini(eksik, PlayerPrefs.GetString(Hedef.SON_ANAHTAR, ""));
                ust.Add(Yazi(L.T("Hedef: {0}", L.T(hd.ad)) + " · " + L.T("{0} Tüy eksik", UiKok.Sayi(eksik)) + (mt > 0 ? " · " + L.T("≈ {0} maç", mt) : ""), 13, "dukkan-hedef"));
            }
            // 2026-11-14 inceleme (fikir 12:05 #5): Tüy'ün ne işe yaradığı yalnız hedef satırı yokken; hedef satırı "ne için" ve
            // "maçla kazanılır"ı zaten söylüyor, iki satır aynı şeyi anlatıyordu. Fiyat ızgarası bir satır yukarı çıkar (SE).
            else ust.Add(Yazi(L.T("Tüy'le Dolap'ta skin, Atölye'de ekipman alırsın. Maç oynayarak da kazanırsın."), 13, "soluk"));
            if (!string.IsNullOrEmpty(dukkanNot)) { var n = Yazi(dukkanNot, 14, "kalin"); n.AddToClassList("dukkan-not"); c.Add(n); }

            var m = Magaza.O;
            bool yukleniyor = m.durum == Magaza.Durum.Yukleniyor || m.durum == Magaza.Durum.Kapali, hata = m.durum == Magaza.Durum.Hata;
            // 2026-11-12 inceleme (#4): mağaza açılamayınca beş kartta beş pasif "Satın alınamıyor" vardı, yeniden deneme yoktu
            // (oyuncu çıkıp girmeliydi). Şimdi tek durum kartı + "Yeniden dene"; paketler Tüy miktarını göstermeye devam eder.
            if (hata)
            {
                var hk = Kart("dukkan-durum"); c.Add(hk);
                hk.Add(Yazi(L.T("Mağazaya bağlanılamadı"), 15, "kalin"));
                hk.Add(Yazi(L.T("İnternet bağlantını kontrol edip yeniden dene."), 13, "soluk"));
                hk.Add(Dugme(L.T("Yeniden dene"), () => { Tik(); Magaza.O.Baslat(); }, "dugme--hayalet"));
            }

            // Hedefi karşılayan en küçük paket (#1b); hedef yoksa ya da hiçbiri yetmiyorsa -1.
            int yeter = -1;
            if (eksik > 0) for (int i = 0; i < TuyDukkan.PAKETLER.Length; i++) if (TuyDukkan.PAKETLER[i].tuy >= eksik) { yeter = i; break; }

            var iz = new VisualElement(); iz.AddToClassList("dukkan-izgara"); c.Add(iz);
            VisualElement alinanKart = null;
            for (int i = 0; i < TuyDukkan.PAKETLER.Length; i++)
            {
                var pk = TuyDukkan.PAKETLER[i];
                string id = pk.id, fiyat = m.Fiyat(id);
                var k = Kart("dukkan-kart"); iz.Add(k);
                if (id == dukkanAlinan && ucusOnce >= 0) alinanKart = k;
                // 2026-11-12 inceleme (#2a): ilk kartın bonus satırı olmadığı için fiyat düğmesi komşusundan yukarıda kalıyor, ızgara
                // tırtıklı görünüyordu. Bilgi üst blokta büyür, düğme kartın dibine yaslanır (aynı sıradaki kartlar eş boy).
                var bilgi = new VisualElement(); bilgi.AddToClassList("dukkan-kart-bilgi"); bilgi.pickingMode = PickingMode.Ignore; k.Add(bilgi);
                var ic = new VisualElement(); ic.AddToClassList("satir"); ic.pickingMode = PickingMode.Ignore; bilgi.Add(ic);
                ic.Add(Demet(i + 1)); ic.Add(Yazi(UiKok.Sayi(pk.tuy), 22, "rakam"));
                bilgi.Add(Yazi(L.T(pk.ad), 14, "kalin"));
                if (pk.ek > 0) { var e = Yazi(L.T("+%{0} fazla", pk.ek), 12, "dukkan-ek"); bilgi.Add(e); }
                if (i == yeter)
                {
                    var ys = new VisualElement(); ys.AddToClassList("satir"); ys.AddToClassList("dukkan-yeter"); ys.pickingMode = PickingMode.Ignore; bilgi.Add(ys);
                    ys.Add(Ikon(Ikonlar.Tik, 14, ALTIN)); ys.Add(Yazi(L.T("Hedefine yeter"), 12, "kalin"));
                }
                // 2026-11-06 (unslop, öneri artifact'i YAP): dört paketin dördü de dolu turuncu düğmeydi; her şey vurgulu olunca hiçbiri öne
                // çıkmıyordu. Fiyat düğmeleri ikincil (sakin) biçimde; "önerilen/en popüler" vurgusu da yok (geliştirme kararı: dükkân baskısız,
                // veri olmadan "en popüler" uydurma olur; bonus yüzdesi her kartta yazıyla duruyor).
                // 2026-11-11 inceleme: satın alma sürerken o kart "İşleniyor…", öteki fiyatlar pasif (ikinci istek yanlış "tamamlanmadı" yazdırıyordu).
                // 2026-11-12 inceleme (#3b, #4): bağlanırken "Fiyat alınıyor…"; bağlantı hatasında düğme yok (yukarıdaki tek kart);
                // "Satın alınamıyor" yalnız mağaza açıkken ürün gelmediyse.
                if (!hata)
                    k.Add(yukleniyor ? Pasif(L.T("Fiyat alınıyor…")) : fiyat == null ? Pasif(L.T("Satın alınamıyor")) : m.SurenMi(id) ? Pasif(L.T("İşleniyor…")) : m.Suruyor || m.GeriYukleniyor ? Pasif(fiyat)
                        : Dugme(fiyat, () => { dukkanNot = null; dukkanAlinan = id; Tik(); Magaza.O.Al(id); }));
                string durumYazi = hata ? L.T("Mağazaya bağlanılamadı") : yukleniyor ? L.T("Fiyat alınıyor…") : fiyat ?? L.T("Satın alınamıyor");
                Erisim.Etiket(k, L.T(pk.ad) + ", " + L.T("{0} Tüy", pk.tuy) + (pk.ek > 0 ? ", " + L.T("+%{0} fazla", pk.ek) : "") + (i == yeter ? ", " + L.T("Hedefine yeter") : "") + ", " + durumYazi, "dugme", false, fiyat == null || hata || yukleniyor);
            }
            if (ucusOnce >= 0)
            {
                var kk = alinanKart;
                if (kk != null) { kk.AddToClassList("dukkan-kart--alindi"); kk.schedule.Execute(() => kk.RemoveFromClassList("dukkan-kart--alindi")).StartingIn(600); }
                czSayi.schedule.Execute(() => TuyUcur(czIkon, czSayi, ucusOnce, tuy, kk)).StartingIn(250);   // yerleşim bitsin (kaynak ve hedefin yeri)
            }

            // 2026-11-08 Reklamsız (geliştirme kararı 7 Kas): Tüy paketlerinin altında tek kart; öne çıkarma, süreli etiket, "en
            // popüler" yok (dükkân baskısız). Ne kaldırdığı ve ne kaldırmadığı kartta yazılı: ödüllü reklam isteğe bağlı kalır.
            c.Add(Bosluk(12));
            var rk = Kart("dukkan-kart"); c.Add(rk);
            rk.Add(Yazi(L.T("Reklamsız"), 16, "kalin"));
            rk.Add(Yazi(L.T("Menüdeki reklam şeridi kalkar. Maç sonundaki ödüllü reklam yalnız sen istersen açılır. Tüy vermez, bir kez alınır."), 13, "soluk"));
            string rf = m.Fiyat(TuyDukkan.REKLAMSIZ); bool rVar = m.ReklamsizVar;
            if (rVar) rk.Add(Pasif(L.T("Alındı")));
            else if (hata) { }
            else if (yukleniyor) rk.Add(Pasif(L.T("Fiyat alınıyor…")));
            else rk.Add(rf == null ? Pasif(L.T("Satın alınamıyor")) : m.SurenMi(TuyDukkan.REKLAMSIZ) ? Pasif(L.T("İşleniyor…")) : m.Suruyor || m.GeriYukleniyor ? Pasif(rf)
                : Dugme(rf, () => { dukkanNot = null; Tik(); Magaza.O.Al(TuyDukkan.REKLAMSIZ); }));
            Erisim.Etiket(rk, L.T("Reklamsız") + ", " + (rVar ? L.T("Alındı") : hata ? L.T("Mağazaya bağlanılamadı") : yukleniyor ? L.T("Fiyat alınıyor…") : rf ?? L.T("Satın alınamıyor")), "dugme", false, rVar || rf == null || hata || yukleniyor);

            c.Add(Bosluk(12));
            c.Add(Yazi(Bulut.Acik ? L.T("İlerlemen ve aldığın Tüy iCloud'a yedekleniyor; uygulamayı silsen de geri gelir.")
                                  : L.T("iCloud kapalı: ilerlemen yalnız bu cihazda. Ayarlar'da iCloud'u açarsan yedeklenir."), 13, "soluk"));
            // 2026-11-12 inceleme (#4): geri yükleme paket düğmeleriyle aynı ağırlıktaydı; ikincil (hayalet) düğme, 48 pt yükseklik
            // ve görünürlük korunur (App Store gereği).
            if (m.GeriYukleniyor) c.Add(Pasif(L.T("Kontrol ediliyor…")));
            else if (m.Suruyor) c.Add(Pasif(L.T("Satın alımları geri yükle")));
            else c.Add(Dugme(L.T("Satın alımları geri yükle"), () => { Tik(); Magaza.O.GeriYukle(ok => { Reklam.Yenile(); dukkanNot = ok ? L.T("Kaydın yeniden okundu.") : L.T("Geri yüklenecek bir şey bulunamadı."); ustKirli = true; DukkanYenile(); }); }, "dugme--hayalet"));
            c.Add(Yazi(L.T("Tüy tüketilen bir üründür: harcayınca biter, süresi dolmaz. Rastgele ödül satılmaz."), 12, "soluk"));
            c.Add(Bosluk(24));
        }
    }
}
