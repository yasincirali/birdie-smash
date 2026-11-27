// Kodlu oda (UGS Relay) bölümü (2026-11-27, yasin TestFlight'ta: "invite kısmında sadece kodla invite edilebilsin, tüm isim çok
// uzun; davet edince oyna adımına geçilemiyor, karşıya tekrar davet butonu çıkıyor, sonsuz döngü, maça geçemiyorum, eklediğim
// arkadaşı göremiyorum"). Kök neden: ad kodlu arkadaş + yalnız mesaj akışında davetin ulaşacağı bir maç taşıması yoktu ve alıcıda
// "Katıl" yerine yine "Davet et" çıkıyordu. Arkadaş ekleme/davet iletisi bu yüzden kalktı; yerine tek ve kısa yol: biri oda kurar,
// 6 haneli kodu (Paylaş ile) gönderir, öbürü kodu yazar, iki telefon aynı Relay'e bağlanır ve maç Game Center yoluyla aynı
// Oturum katmanından başlar. Game Center bölümleri (liste, davet, "Rastgele rakip bul") aynen kalır; UGS kurulamazsa bölüm tek
// cümle söyler (kartsız çalışıyoruz: kota bitince burası durur, Game Center sürer). Kod yazılırken bölüm yeniden kurulmaz.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        VisualElement ugsKab; string ugsGirdi = ""; int ugsGorulen = -1; bool ugsYaziyor;
        static int UgsSurum() { return Ugs.surum + UgsAg.surum + UgsEsles.surum; }
        List<UgsSira.Satir> ugsListe; bool ugsListeAcik, ugsListeYukleniyor, ugsListeYok;

        // Bölüm, durum değişince (hazırlanıyor → kod hazır → bağlandı / hata) yalnız kendi kabını yeniden kurar; yoklama yalnız
        // tamsayı karşılaştırır (ayırma yok) ve sayfa kapanınca durur. Bağlandığında Online.Tik maçı kurar, sayfa zaten kapanır.
        void UgsBolumKur(VisualElement c)
        {
            if (!Ugs.Bayrak) return;
            var kab = ugsKab = new VisualElement(); c.Add(kab); UgsKur();
            IVisualElementScheduledItem iz = null;
            iz = kab.schedule.Execute(() => { if (kab.panel == null) { iz.Pause(); return; } if (ugsKab == kab && UgsSurum() != ugsGorulen && !ugsYaziyor) UgsKur(); }).Every(400);
        }

        void UgsKur()
        {
            if (ugsKab == null) return; ugsKab.Clear(); ugsGorulen = UgsSurum();
            // Dereceli (UgsEsles): önce bu bölüm; arama sürerken oda kodu gösterilmez (aynı Relay odası eşleşmeye ait).
            ugsKab.Add(Grup(L.T("Dereceli maç")));
            ugsKab.Add(Bilgi(Derece.LigAdi + (Derece.Kupa >= 0 ? " · " + Derece.Kupa + " " + L.T("kupa") : "")));
            if (UgsEsles.Araniyor)
            {
                ugsKab.Add(Bilgi(L.T("Rakip aranıyor… {0} sn", UgsEsles.saniye) + " " + L.T("Uygulamayı açık tut.")));
                var ia = Dugme(L.T("İptal"), () => { Online.Kapat(); UgsKur(); }, null, null, 15); ia.style.marginTop = 4; ugsKab.Add(ia);
                return;
            }
            if (UgsEsles.hal == UgsEsles.Hal.Bulunamadi) ugsKab.Add(Bilgi(L.T("Şu an uygun rakip bulunamadı. Biraz sonra dene, kodla ya da Game Center ile oyna.")));
            else if (UgsEsles.hal == UgsEsles.Hal.Hata) ugsKab.Add(Bilgi(L.T("Dereceli arama şu an çalışmıyor. Game Center ile oynayabilirsin.")));
            var rb = Dugme(L.T("Rakip bul"), () => { Online.DereceliAra(); UgsKur(); }, "dugme--birincil", L.T("Puanına yakın biri"), 17); rb.style.marginTop = 4; ugsKab.Add(rb);
            var sl = Dugme(ugsListeAcik ? L.T("Sıralamayı gizle") : L.T("Sıralama"), UgsListeAc, null, null, 15); sl.style.marginTop = 4; ugsKab.Add(sl);
            if (ugsListeAcik)
            {
                if (ugsListeYukleniyor) ugsKab.Add(Bilgi(L.T("Sıralama yükleniyor…")));
                else if (ugsListe == null || ugsListeYok) ugsKab.Add(Bilgi(L.T("Sıralama henüz hazır değil.")));
                else if (ugsListe.Count == 0) ugsKab.Add(Bilgi(L.T("Henüz sıralamada kimse yok. İlk dereceli maçını oyna.")));
                else foreach (var sr in ugsListe)
                {
                    var rw = new VisualElement(); rw.AddToClassList("arkadas-satir"); rw.style.minHeight = 0f;
                    var no = Yazi("#" + sr.sira, 14, "soluk"); no.style.width = 46f; no.style.flexShrink = 0f; rw.Add(no);
                    var ad = Yazi(sr.ad + (sr.ben ? " · " + L.T("sen") : ""), 15, sr.ben ? "kalin" : null); ad.style.flexGrow = 1f; ad.style.flexShrink = 1f; ad.style.minWidth = 0f; rw.Add(ad);
                    var kp = Yazi(sr.kupa.ToString(), 15, "kalin"); kp.style.flexShrink = 0f; rw.Add(kp);
                    ugsKab.Add(rw);
                }
            }
            ugsKab.Add(Grup(L.T("Kodla oyna")));
            switch (UgsAg.hal)
            {
                case UgsAg.Hal.Kuruyor:
                    ugsKab.Add(Bilgi(L.T("Oda hazırlanıyor…"))); return;
                case UgsAg.Hal.Bekliyor:
                case UgsAg.Hal.Bagli:
                    if (!string.IsNullOrEmpty(UgsAg.kod))
                    {
                        // Kurucu: büyük kod + paylaş + rakip bekleniyor. Katılan taraf (kod yok) yalnız "bağlanıyor…" görür.
                        string k = UgsAg.kod;
                        var kod = Yazi(k, 34, "kalin"); kod.style.unityTextAlign = TextAnchor.MiddleCenter; kod.style.marginTop = 4; ugsKab.Add(kod);
                        Erisim.Etiket(kod, L.T("Oda kodun: {0}", string.Join(" ", k.ToCharArray())), "metin");
                        if (Yerel.PaylasVar)
                        {
                            var pd = Dugme(L.T("Kodu gönder"), () => Yerel.Paylas(L.T("Birdie Smash'te maça gel! Kod: {0}", k)), "dugme--birincil", null, 17); pd.style.marginTop = 4; ugsKab.Add(pd);
                            Erisim.Etiket(pd, L.T("Oda kodunu gönder"), "dugme");
                        }
                        ugsKab.Add(Bilgi(L.T("Arkadaşın bu kodu yazınca maç başlar. Uygulamayı açık tut.")));
                    }
                    else ugsKab.Add(Bilgi(L.T("Bağlanıyor…")));
                    var ip = Dugme(L.T("İptal"), () => { Online.Kapat(); UgsKur(); }, null, null, 15); ip.style.marginTop = 4; ugsKab.Add(ip);
                    return;
            }
            ugsKab.Add(Bilgi(L.T("Bir oda kur, kodu arkadaşına gönder. Arkadaşın kodu yazınca maç başlar.")));
            var od = Dugme(L.T("Oda kur"), UgsOdaKur, "dugme--birincil", null, 17); od.style.marginTop = 4; ugsKab.Add(od);

            var tf = new TextField { value = ugsGirdi, maxLength = 8 }; tf.AddToClassList("ad-alan"); tf.style.marginTop = 8;
            tf.RegisterValueChangedCallback(e => ugsGirdi = e.newValue);
            Erisim.Etiket(tf, L.T("Arkadaşının oda kodu"), "metin_alani", false, false, ugsGirdi); ugsKab.Add(tf);
            // Yer tutucu biçimi gösterir (Profil'deki ad alanıyla aynı kendi etiketimiz; Unity 6 placeholder mono derlemesinde yok).
            var ph = Yazi(L.T("Oda kodu"), 16, "soluk"); ph.AddToClassList("ad-yer-tutucu"); ph.pickingMode = PickingMode.Ignore; tf.Add(ph);
            ugsYaziyor = false;
            System.Action phG = () => ph.style.display = string.IsNullOrEmpty(tf.value) && !ugsYaziyor ? DisplayStyle.Flex : DisplayStyle.None;
            tf.RegisterCallback<FocusInEvent>(e => { ugsYaziyor = true; ph.style.display = DisplayStyle.None; }); tf.RegisterCallback<FocusOutEvent>(e => { ugsYaziyor = false; phG(); }); tf.RegisterValueChangedCallback(e => phG()); phG();
            var kd = Dugme(L.T("Koda katıl"), UgsKatil, null, null, 15); kd.style.marginTop = 4; kd.style.marginBottom = 4; ugsKab.Add(kd);
            if (UgsAg.hal == UgsAg.Hal.Hata)
                ugsKab.Add(Bilgi(UgsAg.kodYanlis ? L.T("Bu kodla oda bulunamadı. Kodu kontrol et ya da arkadaşından yenisini iste.")
                                                 : L.T("Çevrimiçi oda şu an kurulamadı. Game Center ile arkadaşını yine çağırabilirsin.")));
        }

        async void UgsListeAc()
        {
            ugsListeAcik = !ugsListeAcik; if (!ugsListeAcik) { UgsKur(); return; }
            ugsListeYukleniyor = true; ugsListeYok = false; UgsKur();
            var l = await UgsSira.Getir(); ugsListe = l; ugsListeYok = l == null; ugsListeYukleniyor = false;
            if (ugsKab != null && ugsKab.panel != null) UgsKur();
        }
        async void UgsOdaKur() { Online.Kapat(); await UgsAg.OdaKur(); if (ugsKab != null && ugsKab.panel != null) UgsKur(); }
        async void UgsKatil() { string g = ugsGirdi; Online.Kapat(); await UgsAg.Katil(g); if (ugsKab != null && ugsKab.panel != null) UgsKur(); }
    }
}
