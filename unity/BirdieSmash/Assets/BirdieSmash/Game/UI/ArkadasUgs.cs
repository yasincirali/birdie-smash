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
        // 2026-11-28 inceleme (fikir 07:06 #1): dereceli arama her 2 sn UgsEsles.surum'u yalnız sayaç için artırıyor; bölüm baştan
        // kurulunca İptal parmağın altında yeniden doğup dokunuş kayboluyordu. Yapı anahtarı sayacı saymaz: aramada yalnız bu
        // değişmediyse sayaç etiketinin metni yerinde güncellenir (tek string), öğe ayrılmaz.
        static int UgsYapi() { return Ugs.surum + UgsAg.surum + (int)UgsEsles.hal * 100003; }
        Label ugsSayac; int ugsYapiGorulen = -1, ugsSayacSn = -1;
        List<UgsSira.Satir> ugsListe; bool ugsListeYukleniyor, ugsListeYok;

        // Bölüm, durum değişince (hazırlanıyor → kod hazır → bağlandı / hata) yalnız kendi kabını yeniden kurar; yoklama yalnız
        // tamsayı karşılaştırır (ayırma yok) ve sayfa kapanınca durur. Bağlandığında Online.Tik maçı kurar, sayfa zaten kapanır.
        void UgsBolumKur(VisualElement c)
        {
            if (!Ugs.Bayrak) return;
            var kab = ugsKab = new VisualElement(); c.Add(kab); UgsKur();
            IVisualElementScheduledItem iz = null;
            iz = kab.schedule.Execute(() => { if (kab.panel == null) { iz.Pause(); return; } if (ugsKab == kab && UgsSurum() != ugsGorulen && !ugsYaziyor) UgsYenile(); }).Every(400);
        }

        void UgsYenile()
        {
            if (UgsEsles.Araniyor && ugsSayac != null && ugsSayac.panel != null && UgsYapi() == ugsYapiGorulen)
            {
                ugsGorulen = UgsSurum();
                if (ugsSayacSn != UgsEsles.saniye) { ugsSayacSn = UgsEsles.saniye; ugsSayac.text = UgsSayacMetni(); }
                return;
            }
            UgsKur();
        }

        static string UgsSayacMetni() { return L.T("Rakip aranıyor… {0} sn", UgsEsles.saniye) + " " + L.T("Uygulamayı açık tut."); }

        // Alt eylem (Rakip bul / Oda kur) arama sürerken ya da oda açıkken gizlenir: o anda sayfanın işi İptal'dir.
        void UgsAltGoster()
        {
            if (onlineAlt == null) return;
            bool gizle = onlineSekme == 0 ? UgsEsles.Araniyor || UgsAg.Aktif : UgsAg.Aktif || UgsEsles.Araniyor;
            onlineAlt.style.display = gizle ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // 2026-11-28 (yasin: "anlaşılır olmalı"): bölüm sekmeye göre tek işi kurar. Dereceli: lig kartı, durum, sıralama (açılınca
        // kendiliğinden yüklenir; düğme yok). Oda: ne olduğunu söyleyen tek cümle, açık oda varsa büyük kod + gönder + iptal, yoksa
        // kod alanı + Katıl. Birincil eylemler sayfanın altındadır (ArkadasKur).
        void UgsKur()
        {
            if (ugsKab == null) return; ugsKab.Clear(); ugsGorulen = UgsSurum(); ugsYapiGorulen = UgsYapi(); ugsSayac = null;
            UgsAltGoster();
            if (onlineSekme == 0) UgsDereceliKur(); else UgsOdaKur2();
        }

        void UgsDereceliKur()
        {
            var lk = Kart("lig-kart"); lk.style.marginTop = 8; ugsKab.Add(lk);
            lk.Add(Yazi(L.T("Ligin"), 12, "soluk"));
            lk.Add(Yazi(Derece.LigAdi, 22, "kalin"));
            if (Derece.Kupa >= 0) lk.Add(Yazi(Derece.Kupa + " " + L.T("kupa"), 16, null));
            var ac = Bilgi(L.T("Puanına yakın biriyle 11 sayılık maç. Kazanınca kupa kazanırsın, kaybedince kupa gider. İlk 5 maç yerleşme.")); ac.style.marginTop = 6; lk.Add(ac);
            if (UgsAg.Aktif && !UgsEsles.Araniyor) { ugsKab.Add(Bilgi(L.T("Açık bir odan var. Dereceli aramak için Oda sekmesinden iptal et."))); }
            if (UgsEsles.Araniyor)
            {
                ugsSayacSn = UgsEsles.saniye; ugsSayac = Yazi(UgsSayacMetni(), 16, "kalin"); ugsSayac.style.whiteSpace = WhiteSpace.Normal; ugsSayac.style.marginTop = 10; ugsKab.Add(ugsSayac);
                var ia = Dugme(L.T("Aramayı iptal et"), () => { Online.Kapat(); UgsKur(); }, null, null, 14); ia.style.marginTop = 4; ugsKab.Add(ia);
            }
            else if (UgsEsles.hal == UgsEsles.Hal.Bulunamadi) ugsKab.Add(Bilgi(L.T("Şu an uygun rakip bulunamadı. Biraz sonra dene ya da Oda sekmesinden arkadaşınla oyna.")));
            else if (UgsEsles.hal == UgsEsles.Hal.Hata) ugsKab.Add(Bilgi(L.T("Dereceli arama şu an çalışmıyor. Arkadaşlar sekmesinden Game Center ile oynayabilirsin.")));

            ugsKab.Add(Grup(L.T("Sıralama · tüm oyuncular")));
            if (ugsListe == null && !ugsListeYukleniyor && !ugsListeYok) UgsListeYukle();
            if (ugsListeYukleniyor) ugsKab.Add(Bilgi(L.T("Sıralama yükleniyor…")));
            else if (ugsListe == null || ugsListeYok) ugsKab.Add(Bilgi(L.T("Sıralama henüz hazır değil.")));
            else if (ugsListe.Count == 0) ugsKab.Add(Bilgi(L.T("Henüz sıralamada kimse yok. İlk çevrimiçi maçını oyna.")));
            else foreach (var sr in ugsListe)
            {
                var rw = new VisualElement(); rw.AddToClassList("arkadas-satir"); rw.style.minHeight = 0f;
                var no = Yazi("#" + sr.sira, 12, "soluk"); no.style.width = 46f; no.style.flexShrink = 0f; rw.Add(no);
                var ad = Yazi(sr.ad + (sr.ben ? " · " + L.T("sen") : ""), 16, sr.ben ? "kalin" : null); ad.style.flexGrow = 1f; ad.style.flexShrink = 1f; ad.style.minWidth = 0f; rw.Add(ad);
                var kp = Yazi(sr.kupa.ToString(), 16, "kalin"); kp.style.flexShrink = 0f; rw.Add(kp);
                ugsKab.Add(rw);
            }
        }

        void UgsOdaKur2()
        {
            ugsKab.Add(Bilgi(L.T("Arkadaşınla özel maç: biri oda kurar ve 6 haneli kodu gönderir, öbürü kodu yazıp katılır. Kupa değişmez.")));
            if (UgsEsles.Araniyor) { ugsKab.Add(Bilgi(L.T("Dereceli arama sürüyor. Oda kurmak için Dereceli sekmesinden iptal et."))); return; }
            switch (UgsAg.hal)
            {
                case UgsAg.Hal.Kuruyor:
                    ugsKab.Add(Bilgi(L.T("Oda hazırlanıyor…"))); return;
                case UgsAg.Hal.Bekliyor:
                case UgsAg.Hal.Bagli:
                    var ok = Kart("oda-kart"); ok.style.marginTop = 8; ugsKab.Add(ok);
                    if (!string.IsNullOrEmpty(UgsAg.kod))
                    {
                        // Kurucu: büyük kod + paylaş + rakip bekleniyor. Katılan taraf (kod yok) yalnız "bağlanıyor…" görür.
                        string k = UgsAg.kod;
                        ok.Add(Yazi(L.T("Oda kodun"), 12, "soluk"));
                        var kod = Yazi(string.Join(" ", k.ToCharArray()), 28, "kalin"); kod.style.unityTextAlign = TextAnchor.MiddleCenter; kod.style.marginTop = 2; ok.Add(kod);
                        Erisim.Etiket(kod, L.T("Oda kodun: {0}", string.Join(" ", k.ToCharArray())), "metin");
                        if (Yerel.PaylasVar)
                        {
                            var pd = Dugme(L.T("Kodu gönder"), () => Yerel.Paylas(L.T("Birdie Smash'te maça gel! Kod: {0}", k)), null, null, 16); pd.style.marginTop = 8; ok.Add(pd);
                            Erisim.Etiket(pd, L.T("Oda kodunu gönder"), "dugme");
                        }
                        ok.Add(Bilgi(L.T("Arkadaşın bu kodu yazınca maç başlar. Uygulamayı açık tut.")));
                    }
                    else ok.Add(Bilgi(L.T("Odaya bağlanıyor…")));
                    var ip = Dugme(L.T("Odayı kapat"), () => { Online.Kapat(); UgsKur(); }, null, null, 14); ip.style.marginTop = 6; ugsKab.Add(ip);
                    return;
            }
            ugsKab.Add(Grup(L.T("Koda katıl")));
            var sat = Satir("oda-katil"); sat.style.alignItems = Align.Center; ugsKab.Add(sat);
            var tf = new TextField { value = ugsGirdi, maxLength = 8 }; tf.AddToClassList("ad-alan"); tf.style.flexGrow = 1f; tf.style.flexShrink = 1f; tf.style.minWidth = 0f; tf.style.marginRight = 8;
            tf.RegisterValueChangedCallback(e => ugsGirdi = e.newValue);
            Erisim.Etiket(tf, L.T("Arkadaşının oda kodu"), "metin_alani", false, false, ugsGirdi); sat.Add(tf);
            // Yer tutucu biçimi gösterir (Profil'deki ad alanıyla aynı kendi etiketimiz; Unity 6 placeholder mono derlemesinde yok).
            var ph = Yazi(L.T("6 haneli kod"), 16, "soluk"); ph.AddToClassList("ad-yer-tutucu"); ph.pickingMode = PickingMode.Ignore; tf.Add(ph);
            ugsYaziyor = false;
            System.Action phG = () => ph.style.display = string.IsNullOrEmpty(tf.value) && !ugsYaziyor ? DisplayStyle.Flex : DisplayStyle.None;
            tf.RegisterCallback<FocusInEvent>(e => { ugsYaziyor = true; ph.style.display = DisplayStyle.None; }); tf.RegisterCallback<FocusOutEvent>(e => { ugsYaziyor = false; phG(); }); tf.RegisterValueChangedCallback(e => phG()); phG();
            var kd = Dugme(L.T("Katıl"), UgsKatil, null, null, 16); kd.style.flexShrink = 0f; sat.Add(kd);
            Erisim.Etiket(kd, L.T("Koda katıl"), "dugme");
            if (UgsAg.hal == UgsAg.Hal.Hata)
                ugsKab.Add(Bilgi(UgsAg.kodYanlis ? L.T("Bu kodla oda bulunamadı. Kodu kontrol et ya da arkadaşından yenisini iste.")
                                                 : L.T("Çevrimiçi oda şu an kurulamadı. Arkadaşlar sekmesinden Game Center ile çağırabilirsin.")));
        }

        // Sıralama sekme açılınca bir kez yüklenir; sayfa her açılışta tazeler (ArkadasAc ugsListe'yi sıfırlar).
        async void UgsListeYukle()
        {
            ugsListeYukleniyor = true; ugsListeYok = false;
            var l = await UgsSira.Getir(); ugsListe = l; ugsListeYok = l == null; ugsListeYukleniyor = false;
            if (ugsKab != null && ugsKab.panel != null && onlineSekme == 0) UgsKur();
        }
        async void UgsOdaKur() { Online.Kapat(); await UgsAg.OdaKur(); if (ugsKab != null && ugsKab.panel != null) UgsKur(); }
        async void UgsKatil() { string g = ugsGirdi; Online.Kapat(); await UgsAg.Katil(g); if (ugsKab != null && ugsKab.panel != null) UgsKur(); }
    }
}
