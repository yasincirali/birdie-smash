// Kodlu oda (UGS Relay) bölümü (2026-11-27, yasin TestFlight'ta: "invite kısmında sadece kodla invite edilebilsin, tüm isim çok
// uzun; davet edince oyna adımına geçilemiyor, karşıya tekrar davet butonu çıkıyor, sonsuz döngü, maça geçemiyorum, eklediğim
// arkadaşı göremiyorum"). Kök neden: ad kodlu arkadaş + yalnız mesaj akışında davetin ulaşacağı bir maç taşıması yoktu ve alıcıda
// "Katıl" yerine yine "Davet et" çıkıyordu. Arkadaş ekleme/davet iletisi bu yüzden kalktı; yerine tek ve kısa yol: biri oda kurar,
// 6 haneli kodu (Paylaş ile) gönderir, öbürü kodu yazar, iki telefon aynı Relay'e bağlanır ve maç Game Center yoluyla aynı
// Oturum katmanından başlar. Game Center bölümleri (liste, davet, "Rastgele rakip bul") aynen kalır; UGS kurulamazsa bölüm tek
// cümle söyler (kartsız çalışıyoruz: kota bitince burası durur, Game Center sürer). Kod yazılırken bölüm yeniden kurulmaz.
using UnityEngine;
using UnityEngine.UIElements;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        VisualElement ugsKab; string ugsGirdi = ""; int ugsGorulen = -1; bool ugsYaziyor;
        static int UgsSurum() { return Ugs.surum + UgsAg.surum; }

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

        async void UgsOdaKur() { Online.Kapat(); await UgsAg.OdaKur(); if (ugsKab != null && ugsKab.panel != null) UgsKur(); }
        async void UgsKatil() { string g = ugsGirdi; Online.Kapat(); await UgsAg.Katil(g); if (ugsKab != null && ugsKab.panel != null) UgsKur(); }
    }
}
