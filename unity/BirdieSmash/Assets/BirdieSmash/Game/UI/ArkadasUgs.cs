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
        VisualElement ugsKab; string ugsGirdi = "", ugsNot;

        void UgsBolumKur(VisualElement c)
        {
            if (!Ugs.Bayrak) return;
            ugsKab = new VisualElement(); c.Add(ugsKab); UgsKur(); UgsAc();
        }

        async void UgsAc()
        {
            var kab = ugsKab;
            await Ugs.Baslat();
            if (kab != null && kab.panel != null) UgsKur();
        }

        void UgsKur()
        {
            if (ugsKab == null) return; ugsKab.Clear();
            ugsKab.Add(Grup(L.T("Çevrimiçi arkadaşlar")));
            if (Ugs.durum == Ugs.Durum.Hata) { ugsKab.Add(Bilgi(L.T("Çevrimiçi hesap şu an kurulamadı. Game Center ile arkadaşını yine çağırabilirsin."))); return; }
            if (Ugs.durum != Ugs.Durum.Hazir) { ugsKab.Add(Bilgi(L.T("Çevrimiçi hesabın hazırlanıyor…"))); return; }

            if (!string.IsNullOrEmpty(Ugs.adKodu))
            {
                var kod = Yazi(L.T("Senin kodun: {0}", Ugs.adKodu), 15, "kalin"); kod.style.whiteSpace = WhiteSpace.Normal; kod.style.marginBottom = 4; ugsKab.Add(kod);
                ugsKab.Add(Bilgi(L.T("Arkadaşın bu kodla seni ekler. Onun kodunu aşağıya yaz.")));
            }
            var tf = new TextField { value = ugsGirdi, maxLength = 40 }; tf.AddToClassList("ad-alan");
            tf.RegisterValueChangedCallback(e => ugsGirdi = e.newValue);
            Erisim.Etiket(tf, L.T("Arkadaşının kodu"), "metin_alani", false, false, ugsGirdi); ugsKab.Add(tf);
            var ek = Dugme(L.T("Arkadaş ekle"), UgsEkle, null, null, 15); ek.style.marginTop = 4; ek.style.marginBottom = 4; ugsKab.Add(ek);
            if (!string.IsNullOrEmpty(ugsNot)) ugsKab.Add(Bilgi(ugsNot));

            var ist = Ugs.Istekler();
            foreach (var a in ist) UgsSatir(a, L.T("Kabul et"), true);
            var l = Ugs.Arkadaslar();
            foreach (var a in l)
            {
                string ad = a.ad; bool cagirdi = Ugs.bekleyenDavet.Contains(a.id);
                UgsSatir(a, L.T("Davet et"), false);
                if (cagirdi) ugsKab.Add(Bilgi(L.T("{0} seni maça çağırdı.", ad)));
            }
            if (l.Count == 0 && ist.Count == 0) ugsKab.Add(Bilgi(L.T("Henüz çevrimiçi arkadaşın yok. Kodunu bir arkadaşına gönder, o da seninkini eklesin.")));
        }

        async void UgsEkle()
        {
            string g = ugsGirdi; bool ok = await Ugs.ArkadasEkle(g);
            ugsNot = ok ? L.T("İstek gönderildi. Karşı taraf da seni ekleyince arkadaş olursunuz.") : L.T("İstek gönderilemedi. Kodu (Ad#1234) kontrol et.");
            if (ok) ugsGirdi = "";
            if (ugsKab != null && ugsKab.panel != null) UgsKur();
        }

        void UgsSatir(Ugs.Arkadas a, string eylem, bool kabul)
        {
            string id = a.id, ad = a.ad;
            var r = Kart("arkadas-satir"); var sol = new VisualElement(); sol.AddToClassList("esnek-yatay"); sol.pickingMode = PickingMode.Ignore; r.Add(sol);
            var at = Yazi(ad, 16, "kalin"); at.style.flexShrink = 1f; at.style.minWidth = 0f; sol.Add(at);
            var d = Dugme(eylem, async () =>
            {
                bool ok = kabul ? await Ugs.ArkadasEkle(id) : await Ugs.DavetEt(id);
                ugsNot = ok ? (kabul ? L.T("Arkadaş eklendi.") : L.T("Davet gönderildi.")) : L.T("İstek gönderilemedi. Kodu (Ad#1234) kontrol et.");
                if (ugsKab != null && ugsKab.panel != null) UgsKur();
            }, null, null, 15);
            d.AddToClassList("kucuk-dugme"); d.style.flexShrink = 0f; r.Add(d);
            Erisim.Etiket(d, eylem + ", " + ad, "dugme");
            ugsKab.Add(r);
        }
    }
}
