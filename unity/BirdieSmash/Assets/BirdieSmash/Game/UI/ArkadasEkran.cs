// Arkadaş maçı girişi ve oyuncu hareketi seçimi (2026-11-21, yasin TestFlight'ta online arkadaş maçını denedikten sonra:
// "burası default olarak açık gelsin, ve oyuncu hareketleri en baştan assisted semi-assisted ve pro olacak şekilde seçilebilsin,
// arkadaşını ekleyebilsin, oyunda mı görebilsin, oyundaysa ona oynama isteği gönderebilsin. oyna sayfasındaki buton da ilgi
// çekici olsun").
//   · Oyna kartı (OnlineKart): kutucuklardan ayrı, tam genişlikte cam kart. Solda senin portren (limon halka) ile rakip yuvası
//     (orkide halka) yan yana: "sen ve biri" okunur, renk palet rolüyle aynı (limon = sen, orkide = rakip; §8). Turuncu yok: tek
//     birincil eylem büyük Oyna kalır (§7.2). Basınca .97 ölçek, büyük Oyna ile aynı dil.
//   · Arkadaş sayfası (ArkadasKur): Game Center arkadaş listesi, her satırda "Davet et" (alıcısı seçili Apple eşleştirme sayfası;
//     davetin gittiği, kabul ya da reddedildiği orada görünür), üstte "Arkadaş ekle" (Apple'ın arkadaşlık isteği sayfası) ve
//     "Rastgele rakip bul". Game Center arkadaşın o an oyunda olup olmadığını vermez (API'de çevrimiçi durumu yok); sayfa bunu
//     tek cümleyle söyler, uydurma "çevrimiçi" işareti yok. Gerçek çevrimiçi durumu sunuculu aşamada (UGS Lobby) gelir.
//   · Hareket seçimi (HareketSecKur): bir kez, ilk Oyna'dan (ya da ilk arkadaş maçından) önce. Üç kart: Yardımlı · Yarı yardımlı ·
//     Pro (Ayar.hareket 0/1/2, anahtar tuytop_hareket aynı). Seçili olan bugünkü ayar (yeni oyuncuda Yardımlı); "Bununla başla"
//     seçimi kaydedip bekleyen eylemi sürdürür. Sonradan Ayarlar › Oyuncu hareketi. Kayıt tuytop_hareket_secildi.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        const string HAREKET_SECILDI = "tuytop_hareket_secildi";
        System.Action hareketSonra; int hareketSecim = -1;
        readonly List<VisualElement> hareketKart = new List<VisualElement>(), hareketTik = new List<VisualElement>();

        // Eylemden önce bir kez hareket sorulur; sorulduysa eylem hemen çalışır.
        void HareketOnce(System.Action devam)
        {
            if (PlayerPrefs.GetInt(HAREKET_SECILDI, 0) == 1) { devam(); return; }
            hareketSonra = devam; hareketSecim = Mathf.Clamp(Ayar.hareket, 0, 2);
            var once = gez.sekme; gez.Ac(Gezinti.HAREKET); Uygula(once, true);
        }

        // EkranTuru için: sayfayı eylemsiz açar (bayrağa dokunmaz).
        public void HareketSecAc() { hareketSonra = null; hareketSecim = Mathf.Clamp(Ayar.hareket, 0, 2); var once = gez.sekme; gez.Ac(Gezinti.HAREKET); Uygula(once, true); }

        // 2026-11-21 inceleme (fikir 22:06 #5): adın yanındaki "Yeni başlayan için" alt yazısı kalktı. ×2'de sarılıp kart başına üç
        // katman, ilk açılışta dokuz parça okutuyordu; ad + tek cümle Ayarlar'daki çip + cümleyle aynı dil. Fikirdeki "Önerilen"
        // rozeti alınmadı (rozet yok kuralı): öneri, Yardımlı'nın varsayılan seçili gelmesiyle zaten görünür.
        void HareketSecKur()
        {
            var c = AltSayfaKur(L.T("Nasıl oynamak istersin?"));
            var g = Yazi(L.T("Vuruşlar hep senin. Koşuyu ne kadar oyuncuna bırakacağını seç; Ayarlar'dan her zaman değişir."), 13, "etiket");
            g.style.whiteSpace = WhiteSpace.Normal; c.Add(g);
            hareketKart.Clear(); hareketTik.Clear();
            for (int i = 0; i < 3; i++)
            {
                int ii = i; bool on = hareketSecim == i;
                var k = Kart("hareket-kart"); k.AddToClassList("dugme"); if (on) k.AddToClassList("secili");
                var ust = Satir("rakip-satir"); ust.pickingMode = PickingMode.Ignore; k.Add(ust);
                { var ik = Ikon(Ikonlar.Tik, 14, KOYU); ik.style.marginRight = 4; ik.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; ust.Add(ik); hareketTik.Add(ik); }
                ust.Add(Yazi(L.T(Ayar.HAREKET_AD[i]), 17, "kalin"));
                var ac = Yazi(L.T(HAREKET_ACIK[i]), 13, "ipucu"); ac.style.whiteSpace = WhiteSpace.Normal; ac.pickingMode = PickingMode.Ignore; k.Add(ac);
                k.AddManipulator(new Clickable(() => { HareketSecGoster(ii); Tik(); }));
                Erisim.Etiket(k, L.T(Ayar.HAREKET_AD[i]) + ". " + L.T(HAREKET_ACIK[i]), "dugme", on);
                c.Add(k); hareketKart.Add(k);
            }
            AltEylem(Dugme(L.T("Bununla başla"), HareketOnayla, "dugme--birincil", null, 19));
        }
        // Kart dokunuşu sayfayı baştan kurmaz (OyuncuSec ile aynı): yalnız seçili sınıf ve ✓ yer değiştirir.
        void HareketSecGoster(int i)
        {
            for (int j = 0; j < hareketKart.Count; j++)
            {
                bool on = j == i; var k = hareketKart[j]; if (on) k.AddToClassList("secili"); else k.RemoveFromClassList("secili");
                hareketTik[j].style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                var b = k.userData as ErisimBilgi; if (b != null) b.secili = on;
            }
            hareketSecim = i;
        }
        // Tutamakla kapatmak eylemi iptal eder ve hiçbir şeyi kaydetmez: sayfa sonraki Oyna'da yine gelir.
        public void HareketOnayla()
        {
            if (hareketSecim >= 0) Ayar.hareket = hareketSecim;
            PlayerPrefs.SetInt(HAREKET_SECILDI, 1); Ayar.KaydetSonra();
            var devam = hareketSonra; hareketSonra = null;
            var once = gez.sekme; gez.AltKapat(); Uygula(once, true);
            if (devam != null) devam();
        }

        // ---- Oyna sayfasındaki arkadaş maçı kartı ----
        VisualElement OnlineKart()
        {
            var d = new VisualElement(); d.AddToClassList("online-kart"); d.AddToClassList("dugme");
            var ik = new VisualElement(); ik.AddToClassList("online-yuzler"); ik.pickingMode = PickingMode.Ignore; d.Add(ik);
            var ben = new VisualElement(); ben.AddToClassList("avatar"); ben.AddToClassList("online-ben"); ben.pickingMode = PickingMode.Ignore; BenAvatar(ben, 22); ik.Add(ben);
            var o = new VisualElement(); o.AddToClassList("online-o"); o.pickingMode = PickingMode.Ignore; o.Add(Ikon(Ikonlar.Arkadas, 22, RAKIP)); ik.Add(o);
            var yz = new VisualElement(); yz.AddToClassList("online-yazilar"); yz.pickingMode = PickingMode.Ignore; d.Add(yz);
            yz.Add(Baslik(L.T("Arkadaşınla oyna"), Px(22, 20), "online-baslik"));
            var alt = Yazi(L.T("Canlı maç · davet gönder ya da rakip bul"), 13, "online-alt"); alt.style.whiteSpace = WhiteSpace.Normal; yz.Add(alt);
            var ok = new VisualElement(); ok.AddToClassList("online-ok"); ok.pickingMode = PickingMode.Ignore; ok.Add(Ikon(Ikonlar.Ileri, 16, RAKIP)); d.Add(ok);
            d.AddManipulator(new Clickable(() => { Tik(); HareketOnce(ArkadasAc); }));
            OnlineNabiz(o);
            Erisim.Etiket(d, L.T("Arkadaşınla oyna") + ", " + L.T("Canlı maç · davet gönder ya da rakip bul"), "dugme");
            return d;
        }

        // 2026-11-21 inceleme (fikir 22:06 #6): kart tamamen durağandı, büyük Oyna'nın altında gözden kaçabiliyordu (yasin "buton
        // da ilgi çekici olsun"). Oturumda bir kez, giriş hareketi bittikten sonra rakip yuvası iki yumuşak nabız atar ("burada
        // biri bekliyor"); döngü yok, birincil Oyna'yla yarışmaz. Yalnız sınıf değişir (USS geçişi), karede stil yazımı yok.
        // Hareketi azaltta hiç başlamaz.
        static bool onlineNabizOldu;
        void OnlineNabiz(VisualElement o)
        {
            if (onlineNabizOldu || Ayar.reduce) return; int n = 0;
            o.schedule.Execute(() => { onlineNabizOldu = true; o.ToggleInClassList("online-o--nabiz"); n++; }).StartingIn(600).Every(320).Until(() => n >= 4 || o.panel == null);
        }

        // ---- Arkadaş sayfası ----
        public void ArkadasAc()
        {
            if (Online.ArkadasDurum != Online.ARK_YUKLENIYOR) Online.ArkadasYukle();   // her açılışta yenile (yeni eklenen arkadaş, verilen izin)
            var once = gez.sekme; gez.Ac(Gezinti.ARKADAS); Uygula(once, true);
        }
        int arkGorulen = -1; VisualElement arkListe;
        void ArkadasKur()
        {
            var c = AltSayfaKur(L.T("Arkadaşınla oyna"));
            // 2026-11-21 inceleme (fikir 22:06 #3): "Listeyi yenile" kalktı. Sayfa her açılışta ve uygulamaya dönüşte (Ayarlar'dan
            // izin verip ya da Apple'ın arkadaşlık sayfasından gelince, ArkadasDonus) kendiliğinden yeniler; düğme yeni bir şey getirmiyordu.
            var ek = Dugme(L.T("Arkadaş ekle"), () => { Online.ArkadasEkle(); }, null, null, 15); ek.style.marginTop = 4; ek.style.marginBottom = 4; c.Add(ek);
            c.Add(Grup(L.T("Arkadaşların")));
            arkListe = new VisualElement(); c.Add(arkListe); ArkListeKur();
            UgsBolumKur(c);
            AltEylem(Dugme(L.T("Rastgele rakip bul"), () => { gez.AltKapat(); Uygula(gez.sekme, true); root.OnlineAra(); }, "dugme--birincil", L.T("11 sayı · eşit güç"), 19));
            // Liste köprüde yüklenir (izin penceresi dahil); durum değişince yalnız liste kabı yeniden kurulur. Sayfa kapanınca durur.
            // 2026-11-21 (fikir 22:06 #8): liste geldi ya da izin yoksa yoklama durur; dönüş yenilemesi yeniden başlatır.
            var lv = arkListe; IVisualElementScheduledItem iz = null;
            iz = lv.schedule.Execute(() =>
            {
                if (lv.panel == null) { iz.Pause(); return; }
                if (Online.ArkadasDurum != arkGorulen) ArkListeKur();
                if (arkGorulen == Online.ARK_HAZIR || arkGorulen == Online.ARK_IZIN_YOK) iz.Pause();   // giriş yok (ARK_YOK) eşzamansız girişle değişebilir: yoklama sürer
            }).Every(300);
            arkIz = iz;
        }
        IVisualElementScheduledItem arkIz;
        // Uygulamaya dönüşte (GameRoot.OnApplicationFocus) Arkadaş sayfası açıksa liste yeniden yüklenir.
        public void ArkadasDonus()
        {
            if (arkListe == null || arkListe.panel == null) return;
            if (Online.ArkadasDurum != Online.ARK_YUKLENIYOR) Online.ArkadasYukle();
            if (arkIz != null) arkIz.Resume();
        }
        void ArkListeKur()
        {
            if (arkListe == null) return; arkListe.Clear(); int d = Online.ArkadasDurum; arkGorulen = d;
            if (d == Online.ARK_HAZIR)
            {
                var l = Online.Arkadaslar();
                if (l.Count == 0) { arkListe.Add(Bilgi(L.T("Game Center'da henüz arkadaşın yok. \"Arkadaş ekle\" ile davet bağlantısı gönderebilirsin."))); return; }
                foreach (var a in l)
                {
                    string id = a.id, ad = a.ad;
                    var r = Kart("arkadas-satir"); var sol = new VisualElement(); sol.AddToClassList("esnek-yatay"); sol.pickingMode = PickingMode.Ignore; r.Add(sol);
                    var av = new VisualElement(); av.AddToClassList("avatar"); av.AddToClassList("arkadas-avatar"); av.pickingMode = PickingMode.Ignore; sol.Add(av);
                    // 2026-11-21 (fikir 22:06 #4): her satırda aynı kişi ikonu vardı, liste yalnız adla ayrışıyordu. Adın baş harfi
                    // (Kariyer şeridi kalıbı, sabit boy: daire yazı ölçeğiyle büyümez); harfle başlamayan adda ikon kalır.
                    if (!string.IsNullOrEmpty(ad) && char.IsLetterOrDigit(ad[0])) { var bh = Yazi(ad.Substring(0, 1).ToUpper(), 15, "kalin"); bh.style.color = RAKIP; bh.style.fontSize = 15; av.Add(bh); }
                    else av.Add(Ikon(Ikonlar.Kisi, 18, RAKIP));
                    var at = Yazi(ad, 16, "kalin"); at.style.flexShrink = 1f; at.style.minWidth = 0f; sol.Add(at);
                    var dv = Dugme(L.T("Davet et"), () => { gez.AltKapat(); Uygula(gez.sekme, true); Online.Davet(id, L.T("Birdie Smash'te bir maça ne dersin?")); }, null, null, 15);
                    dv.AddToClassList("kucuk-dugme"); dv.style.flexShrink = 0f; r.Add(dv);
                    Erisim.Etiket(dv, L.T("Davet et") + ", " + ad, "dugme");
                    arkListe.Add(r);
                }
                // 2026-11-21 (fikir 22:06 #7): iki cümlelik dipnot her durumda görünüyordu; boş liste ve izin yok kendi cümlesini
                // söylüyor. Yalnız liste doluyken ve tek cümle; "çevrimiçi göstermiyor" sözü sunuculu aşamada zaten değişecek.
                var not = Yazi(L.T("Davet bildirim olarak gider; kabul edince maç başlar."), 12, "soluk");
                not.style.whiteSpace = WhiteSpace.Normal; not.style.marginTop = 10; arkListe.Add(not);
                return;
            }
            string m = d == Online.ARK_IZIN_YOK ? L.T("Arkadaş listesine izin verilmedi. iPhone Ayarlar › Game Center › Arkadaş Listesi'nden açabilirsin; davet için rastgele rakip de bulabilirsin.")
                : d == Online.ARK_YOK ? L.T("Arkadaş listesi için Game Center'a giriş gerekiyor (iPhone Ayarlar › Game Center).")
                : L.T("Arkadaşların yükleniyor…");
            arkListe.Add(Bilgi(m));
        }
        Label Bilgi(string m) { var y = Yazi(m, 13, "etiket"); y.style.whiteSpace = WhiteSpace.Normal; return y; }
    }
}
