// Çevrimiçi (UGS) arkadaş bölümü (2026-11-27, yasin: "ranka göre eşleşme, arkadaşınla oynama, arkadaş ekleme, davet; karşıya
// bildirim gidecek"). Arkadaş sayfasında Game Center bölümünün altında durur; Game Center listesi ve "Rastgele rakip bul"
// aynen kalır. Bölüm ağa yalnız sayfa açılınca çıkar; kurulamazsa tek cümle söyler ve Game Center yoluna bırakır (kartsız
// çalışıyoruz: UGS kotası bitince burası kapanır, Game Center sürer). Davet iletisi karşı uygulama açıkken düşer; kapalıyken
// push bildirimi (APNs) ve davete katılma (Relay) sonraki dilim, o yüzden bekleyen davet şimdilik yalnız "seni çağırdı" der.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        VisualElement ugsKab; string ugsGirdi = "", ugsNot; int ugsGorulen = -1; bool ugsYaziyor;

        // 2026-11-27 inceleme (fikir 02:06 #6, #7): bölüm Ugs.surum'u yoklar; durum (hazırlanıyor → hazır/hata) ya da gelen davet
        // değişince yalnız bu kap yeniden kurulur. Eskiden sayfa açıkken gelen davet hiç görünmüyordu ve "hazırlanıyor" takılı
        // kalabiliyordu. Olay (Ugs.DavetGeldi) UGS geri çağrısından gelebilir; yoklama ana iş parçacığında kalmanın en basit yolu.
        // Kod yazılırken yeniden kurulmaz (klavye ve odak düşmesin); alan bırakılınca sıradaki yoklama yakalar. Yoklama yalnız tamsayı karşılaştırır (ayırma yok), sayfa kapanınca durur. İlk bağlantı alt sayfa giriş hareketi (.22 sn)
        // bittikten sonra başlar: UnityServices kurulumunun ilk ayırmaları kayan sayfayla aynı karelere düşmesin.
        void UgsBolumKur(VisualElement c)
        {
            if (!Ugs.Bayrak) return;
            var kab = ugsKab = new VisualElement(); c.Add(kab); UgsKur();
            IVisualElementScheduledItem iz = null;
            iz = kab.schedule.Execute(() => { if (kab.panel == null) { iz.Pause(); return; } if (ugsKab == kab && Ugs.surum != ugsGorulen && !ugsYaziyor) UgsKur(); }).Every(400);
            if (Ugs.durum != Ugs.Durum.Hazir) kab.schedule.Execute(() => { if (kab.panel != null) UgsAc(); }).StartingIn(260);
        }

        async void UgsAc()
        {
            var kab = ugsKab;
            await Ugs.Baslat();
            if (kab != null && kab.panel != null && kab == ugsKab) UgsKur();
        }

        void UgsKur()
        {
            if (ugsKab == null) return; ugsKab.Clear(); ugsGorulen = Ugs.surum;
            ugsKab.Add(Grup(L.T("Çevrimiçi arkadaşlar")));
            if (Ugs.durum == Ugs.Durum.Hata)
            {
                ugsKab.Add(Bilgi(L.T("Çevrimiçi hesap şu an kurulamadı. Game Center ile arkadaşını yine çağırabilirsin.")));
                var yd = Dugme(L.T("Yeniden dene"), UgsAc, null, null, 15); yd.style.marginTop = 4; ugsKab.Add(yd);
                return;
            }
            if (Ugs.durum != Ugs.Durum.Hazir) { ugsKab.Add(Bilgi(L.T("Çevrimiçi hesabın hazırlanıyor…"))); return; }

            if (!string.IsNullOrEmpty(Ugs.adKodu))
            {
                // 2026-11-27 inceleme (fikir 02:06 #2): kodu elle yazdırmak arkadaş eklemenin en büyük sürtünmesiydi. Kodun yanında
                // sistem paylaş sayfası (Yerel.Paylas, Ayarlar'daki çökme kaydıyla aynı köprü); paylaş olmayan yerde düğme çizilmez.
                var kr = new VisualElement(); kr.AddToClassList("arkadas-satir"); kr.style.minHeight = 0f; kr.style.paddingLeft = 0f;
                var kod = Yazi(L.T("Senin kodun: {0}", Ugs.adKodu), 15, "kalin"); kod.style.whiteSpace = WhiteSpace.Normal; kod.style.flexShrink = 1f; kod.style.flexGrow = 1f; kod.style.minWidth = 0f; kr.Add(kod);
                if (Yerel.PaylasVar)
                {
                    string ak = Ugs.adKodu;
                    var pd = Dugme(L.T("Paylaş"), () => Yerel.Paylas(L.T("Birdie Smash'te beni ekle: {0}", ak)), null, null, 15); pd.AddToClassList("kucuk-dugme"); pd.style.flexShrink = 0f; kr.Add(pd);
                    Erisim.Etiket(pd, L.T("Kodunu paylaş"), "dugme");
                }
                ugsKab.Add(kr);
                ugsKab.Add(Bilgi(L.T("Arkadaşın bu kodla seni ekler. Onun kodunu aşağıya yaz.")));
            }
            var tf = new TextField { value = ugsGirdi, maxLength = 40 }; tf.AddToClassList("ad-alan");
            tf.RegisterValueChangedCallback(e => ugsGirdi = e.newValue);
            Erisim.Etiket(tf, L.T("Arkadaşının kodu"), "metin_alani", false, false, ugsGirdi); ugsKab.Add(tf);
            // Yer tutucu biçimi gösterir (Profil'deki ad alanıyla aynı kendi etiketimiz; Unity 6 placeholder mono derlemesinde yok).
            var ph = Yazi("Ad#1234", 16, "soluk"); ph.AddToClassList("ad-yer-tutucu"); ph.pickingMode = PickingMode.Ignore; tf.Add(ph);
            ugsYaziyor = false;
            System.Action phG = () => ph.style.display = string.IsNullOrEmpty(tf.value) && !ugsYaziyor ? DisplayStyle.Flex : DisplayStyle.None;
            tf.RegisterCallback<FocusInEvent>(e => { ugsYaziyor = true; ph.style.display = DisplayStyle.None; }); tf.RegisterCallback<FocusOutEvent>(e => { ugsYaziyor = false; phG(); }); tf.RegisterValueChangedCallback(e => phG()); phG();
            var ek = Dugme(L.T("Arkadaş ekle"), UgsEkle, null, null, 15); ek.style.marginTop = 4; ek.style.marginBottom = 4; ugsKab.Add(ek);
            if (!string.IsNullOrEmpty(ugsNot)) ugsKab.Add(Bilgi(ugsNot));

            var ist = Ugs.Istekler();
            foreach (var a in ist) UgsSatir(a, L.T("Kabul et"), true, L.T("Seni eklemek istiyor"));
            var l = Ugs.Arkadaslar();
            foreach (var a in l) UgsSatir(a, L.T("Davet et"), false, Ugs.bekleyenDavet.Contains(a.id) ? L.T("Seni maça çağırdı") : null);
            if (l.Count == 0 && ist.Count == 0) ugsKab.Add(Bilgi(L.T("Henüz çevrimiçi arkadaşın yok. Kodunu bir arkadaşına gönder, o da seninkini eklesin.")));
        }

        bool ugsEkleniyor;
        async void UgsEkle()
        {
            if (ugsEkleniyor) return; ugsEkleniyor = true;
            string g = ugsGirdi; bool ok = await Ugs.ArkadasEkle(g); ugsEkleniyor = false;
            ugsNot = ok ? L.T("İstek gönderildi. Karşı taraf da seni ekleyince arkadaş olursunuz.") : L.T("İstek gönderilemedi. Kodu (Ad#1234) kontrol et.");
            if (ok) ugsGirdi = "";
            if (ugsKab != null && ugsKab.panel != null) UgsKur();
        }

        // 2026-11-27 inceleme (fikir 02:06 #4, #6): basınca düğme yerinde "Gönderiliyor…" olur ve devre dışı kalır (ikinci dokunuş
        // ikinci davet atmıyor), sonuç da yerinde görünür: "Gönderildi ✓" ya da "Gönderilemedi", 2 sn sonra eski hâli. Sonuç eskiden
        // kod alanının altında, satırdan uzakta çıkıyordu. Kabul başarılıysa kişi arkadaş listesine geçtiği için bölüm yeniden kurulur.
        // Davet/istek bilgisi adın altında alt satır (eskiden satırın altında ayrı düz yazıydı). Hareket yok: yalnız metin + hafif tık.
        void UgsSatir(Ugs.Arkadas a, string eylem, bool kabul, string alt)
        {
            string id = a.id, ad = a.ad;
            var r = Kart("arkadas-satir"); var sol = new VisualElement(); sol.AddToClassList("esnek-yatay"); sol.style.flexDirection = FlexDirection.Column; sol.style.alignItems = Align.FlexStart; sol.pickingMode = PickingMode.Ignore; r.Add(sol);
            var at = Yazi(ad, 16, "kalin"); at.style.flexShrink = 1f; at.style.minWidth = 0f; sol.Add(at);
            if (alt != null) { var al = Yazi(alt, 13, "soluk"); al.style.whiteSpace = WhiteSpace.Normal; sol.Add(al); }
            VisualElement d = null;
            d = Dugme(eylem, async () =>
            {
                var y = d.Q<Label>(className: "dugme-yazi"); if (!d.enabledSelf || y == null) return;
                d.SetEnabled(false); y.text = L.T("Gönderiliyor…");
                bool ok = kabul ? await Ugs.ArkadasEkle(id) : await Ugs.DavetEt(id);
                if (d.panel == null) return;
                if (ok && kabul) { UgsKur(); return; }
                y.text = ok ? L.T("Gönderildi ✓") : L.T("Gönderilemedi"); Erisim.Etiket(d, y.text + ", " + ad, "dugme");
                if (ok) Yerel.Hap("tick");
                d.schedule.Execute(() => { y.text = eylem; d.SetEnabled(true); Erisim.Etiket(d, eylem + ", " + ad, "dugme"); }).StartingIn(2000);
            }, null, null, 15);
            d.AddToClassList("kucuk-dugme"); d.style.flexShrink = 0f; r.Add(d);
            Erisim.Etiket(d, eylem + ", " + ad + (alt != null ? ", " + alt : ""), "dugme");
            ugsKab.Add(r);
        }
    }
}
