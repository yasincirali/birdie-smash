// Oyna sekmesi ve Hızlı maç alt sayfası (tasarım §2.2–2.3, 2026-10-26).
// Ana yol bir dokunuş: "Sıradaki" kartı (kariyerin açık basamağı: yer, rakip, saha, basamak çubuğu) ve altında tek parlak
// düğme "Oyna" (başparmak bölgesi, alt üçte bir). Kartın kendisine dokunmak Kariyer sekmesini açar.
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

        void OynaKur(VisualElement k)
        {
            k.Clear(); var sv = Kaydirma(k, true); var c = sv.contentContainer; c.AddToClassList("oyna-icerik");
            if (!Ayar.antrenman)
            {
                if (logo == null && !logoYok) { logo = Resources.Load<Texture2D>("logo"); logoYok = logo == null; }
                var m = new VisualElement(); m.AddToClassList("marka"); m.pickingMode = PickingMode.Ignore; c.Add(m);
                if (logo != null) { var li = new Image { image = logo, scaleMode = ScaleMode.ScaleToFit }; li.AddToClassList("logo"); li.pickingMode = PickingMode.Ignore; m.Add(li); }
                m.Add(Yazi("Birdie Smash", 32, "marka-ad")); m.Add(Yazi(L.T("Kaydır, kes, smaçla."), 14, "soluk"));
            }
            var esnek = new VisualElement(); esnek.AddToClassList("esnek"); esnek.pickingMode = PickingMode.Ignore; c.Add(esnek);
            c.Add(SiradakiKart());
            if (root.bekleyen)
            {
                c.Add(Dugme(L.T("Maça devam et"), () => root.MacaDon(), "dugme--birincil", null, 19));
                c.Add(Dugme(L.T("Yeni maç başlat"), () => root.Oyna()));
            }
            else
            {
                var p = Ayar.ilerleme; var kb = KariyerVeri.Al(p.Siradaki());
                string alt = !Ayar.antrenman ? L.T("İlk kez: 3 adımlık antrenman, sonra 5 sayılık maç") : p.HepsiBitti ? L.Cumle(Career.Line(p)) : L.T("Sıradaki: {0}", L.T(kb.yer));
                c.Add(Dugme(L.T("Oyna"), () =>
                {
                    if (Ayar.antrenman && !Ayar.kariyerModu) { Ayar.kariyerModu = true; Ayar.GearHesap(); Ayar.Kaydet(); }
                    Ayar.kariyerSecim = -1;   // büyük Oyna her zaman sıradakini oynatır (haritadan seçilen yeniden oynama değil)
                    root.Oyna();
                }, "dugme--birincil", alt, 20));
            }
            var row = Satir("ikincil-satir"); c.Add(row);
            row.Add(Dugme(L.T("Hızlı maç"), HizliMacAc, "ikincil-kart", HizliOzet()));
            row.Add(Dugme(L.T("Tam antrenman"), () => root.Antrenman(), "ikincil-kart", L.T("Vuruş, servis, nefes")));
        }

        string HizliOzet() { return L.T("{0} sayı · {1} · {2}", Ayar.target, L.T(Ayar.easy ? "Kolay" : "Usta"), L.T(CourtView.SAHA[Ayar.saha].ad)); }

        // "Sıradaki" kartı: rakip rengi yanında adıyla (yalnız renkle değil), yıldızları (dolu/çerçeve + "1/3"), turun basamak
        // çubuğu biçimle (geçilen dolu, şimdiki uzun ve çerçeveli, gelecek soluk). 2026-10-26: tur ve yıldızlar haritadan
        // (Ilerleme); dokununca Kariyer sekmesi (harita) açılır.
        VisualElement SiradakiKart()
        {
            var p = Ayar.ilerleme; int g = p.Siradaki(), tr = KariyerVeri.TurOf(g), i = KariyerVeri.BasOf(g); var kb = KariyerVeri.Al(g); var st = Match.STIL[kb.stil];
            bool tamam = p.HepsiBitti;
            var kart = Kart("siradaki"); kart.AddManipulator(new Clickable(() => SekmeAc(Sekme.Kariyer)));
            var bas = Satir("arali"); kart.Add(bas);
            bas.Add(Yazi(L.T(tamam ? "Kariyer tamam" : "Sıradaki maç"), 12, "etiket"));
            bas.Add(Yazi(L.T(TurAd(tr)) + " " + (i + 1) + "/" + KariyerVeri.BASAMAK, 12, "etiket"));
            kart.Add(Yazi(L.T(kb.yer), 22, "kalin"));
            var rk = Satir("rakip-satir"); kart.Add(rk);
            var nok = new VisualElement(); nok.AddToClassList("renk-nokta"); nok.style.backgroundColor = Kit.Hex(st.renk); rk.Add(nok);
            rk.Add(Yazi(L.T("Rakip") + " " + st.isim + " · " + L.T(st.ad) + " · " + L.T(CourtView.SAHA[kb.saha].ad), 14, "soluk"));
            var yr = Satir("rakip-satir"); kart.Add(yr); yr.Add(UcYildiz(p.Maske(g), 16)); var yy = Yazi(p.Yildiz(g) + "/3", 12, "soluk"); yy.style.marginLeft = 4; yr.Add(yy);
            var bc = Satir("basamak-cubugu"); bc.pickingMode = PickingMode.Ignore; kart.Add(bc);
            for (int j = 0; j < KariyerVeri.BASAMAK; j++)
            {
                var b = new VisualElement(); b.AddToClassList("basamak"); b.pickingMode = PickingMode.Ignore; int gj = KariyerVeri.Global(tr, j);
                b.AddToClassList(p.Kazanildi(gj) && gj != g ? "basamak--gecti" : gj == g ? "basamak--simdi" : "basamak--kilit"); bc.Add(b);
            }
            Erisim.Etiket(kart, L.T(tamam ? "Kariyer tamam" : "Sıradaki maç") + ", " + L.T(TurAd(tr)) + ", " + L.T(kb.yer) + ", " + L.T("Rakip") + " " + st.isim + ", " + (i + 1) + "/" + KariyerVeri.BASAMAK + ", " + L.T("{0}/3 yıldız", p.Yildiz(g)), "dugme");
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
            if (altS != null) { var eski = altSayfa != null ? altSayfa.Q<ScrollView>() : null; Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero; if (altS == Gezinti.KART) MacKartKur(); else HizliKur(); KaydirmaGeri(altSayfa, of); }
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
