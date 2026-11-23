// Gelişim sayfası (2026-11-10, Core/Gelisim): oyuncunun kendi beş değeri. Her seviye bir puan verir; oyuncu burada dağıtır,
// istediği zaman ücretsiz geri alır. Sayfa üç yerden açılır: Oyna'nın altındaki satır (dağıtılmamış puan varken), Profil'deki
// "Gelişim" kartı ve EkranTuru. Görsel dil Seviye yolu ile aynı (sv-yol kartı); süs, parıltı, "YENİ" yok. Her dal satırı:
// ad + şu anki etki yüzdesi, tek cümle ne işe yaradığı, 10 basamaklı çubuk, 44 pt − ve + düğmeleri. Durum yalnız renkle
// verilmez: basamak sayısı "3/10" ve yüzde yazıyla da durur. Eşit şartlar açıkken etkisiz olduğu sayfanın başında yazar.
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        void GelisimKur()
        {
            var c = AltSayfaKur(L.T("Gelişim"));
            var p = Ayar.gelisim; int sv = Ayar.gear.Seviye(), bos = Ayar.GelisimBos, top = Gelisim.Toplam(sv);
            var bas = Satir("arali"); bas.pickingMode = PickingMode.Ignore; c.Add(bas);
            // Sv 1'de puan yok: "hepsi dağıtıldı" yanlış olurdu, ilk puanın ne zaman geleceği yazar.
            // 2026-11-20 (inceleme, fikir 17:06 #6; OYUNCUYU GÖSTER): "oyuncunu güçlendir" denen sayfada oyuncu yoktu (Oyna'dan
            // açılınca bağlam kayboluyordu). Başlığın solunda 36 pt portre; üst çubukta çizilen önbellekteki doku (Portre.Benim),
            // yeni figür/RT yok. Dokunuş yok: sayfanın tek işi puan. Yoksa eski insan simgesi (AvatarIc).
            var bsol = Satir("rakip-satir"); bsol.pickingMode = PickingMode.Ignore; bsol.style.marginTop = 0; bsol.style.flexShrink = 1f; bas.Add(bsol);
            var av = new VisualElement(); av.AddToClassList("avatar"); av.pickingMode = PickingMode.Ignore; av.style.width = 36; av.style.height = 36;
            av.style.borderTopLeftRadius = av.style.borderTopRightRadius = av.style.borderBottomLeftRadius = av.style.borderBottomRightRadius = 18;
            av.style.marginRight = 8; av.style.backgroundColor = Kit.Hex(Ayar.FORMA); AvatarIc(av, Portre.Benim(), 20); bsol.Add(av);
            // 2026-11-22 inceleme (fikir 12:06 #4, §7.4): "Dağıtılacak puan: 3" yanında "7/10 puan" aynı veriyi iki kez söylüyordu,
            // büyük yazıda başlık satırını sarıyordu. Tek etiket: kalan / toplam; bitince "10 puanın hepsi dağıtıldı".
            gelUst = Yazi(GelUstMetin(top, bos), 16, "kalin"); gelUst.style.whiteSpace = WhiteSpace.Normal; gelUst.style.flexShrink = 1; bsol.Add(gelUst);
            // 2026-11-15 (inceleme, fikir 22:05 #5): iki varyantlı uzun paragraf her açılışta aynı kuralı tekrar ediyor, beş dal
            // kartını ilk ekranın altına itiyordu (büyük yazıda iki dal görünürdü). Tek kısa satır; Sv 1'de üst satır yeter.
            if (top > 0) { var ac = Yazi(L.T("Dal başına en çok 10 · ücretsiz değiştir"), 13, "soluk"); ac.style.whiteSpace = WhiteSpace.Normal; ac.style.marginTop = 4; ac.style.marginBottom = 6; c.Add(ac); }
            if (Ayar.gear.eko.esit && !Ayar.kariyerModu) { var es = Yazi(L.T("Eşit şartlar açık: gelişim bu maçlarda sayılmaz."), 13, "ipucu"); es.style.whiteSpace = WhiteSpace.Normal; es.style.marginBottom = 6; c.Add(es); }
            for (int i = 0; i < Gelisim.DAL; i++) c.Add(GelisimDal(i, p[i], bos > 0, top > 0));
        }
        static string GelUstMetin(int top, int bos) { return top == 0 ? L.T("İlk puanın Sv 2'de") : bos > 0 ? L.T("Dağıtılacak puan: {0} / {1}", bos, top) : L.T("{0} puanın hepsi dağıtıldı", top); }

        // Basılı tutunca tekrar (2026-11-15 inceleme, fikir 22:05 #2; iOS UIStepper kalıbı): Sv 30'da 29 puan 29 ayrı dokunuş,
        // her adımda alt sayfa baştan kuruluyor ve senkron kayıt yapılıyordu. Artık ilk dokunuş bir adım; 0,4 sn tutunca ~7/sn,
        // 1,5 sn'den sonra ~14/sn. Her adımda sayfa KURULMAZ: yalnız o dalın basamağı (BasamakDol/BasamakSon), sayısı, erişim
        // bilgisi, üstteki etiket ve düğmelerin pasif sınıfı yerinde güncellenir (yalnız değişince yazılır). Ses her adımda
        // değil 3 adımda bir + tavana/sıfıra gelince; kayıt Ayar.KaydetSonra ile parmak kalkınca tek kez. Hareketi azaltta
        // basamaklar anında (Basamak* zaten kısa devre), tekrar aynı.
        Label gelUst;
        readonly Label[] gelSay = new Label[Gelisim.DAL];
        readonly VisualElement[] gelCb = new VisualElement[Gelisim.DAL], gelArti = new VisualElement[Gelisim.DAL], gelEksi = new VisualElement[Gelisim.DAL];
        IVisualElementScheduledItem gelTekrar; int gelAdim; float gelBasT; Vector3 gelBasP; bool gelBekle; VisualElement gelSahip;

        // Bir adım; true = değer değişti.
        bool GelisimAdim(int dal, bool arti)
        {
            int once = Ayar.gelisim[dal];
            if (!(arti ? Ayar.GelisimVer(dal) : Ayar.GelisimGeriAl(dal))) return false;
            gelAdim++;
            int n = Ayar.gelisim[dal], sv = Ayar.gear.Seviye(), bos = Ayar.GelisimBos, top = Gelisim.Toplam(sv);
            bool sinir = arti ? (bos == 0 || n >= Gelisim.TAVAN) : n == 0;
            if (gelAdim == 1 || gelAdim % 3 == 0 || sinir) { if (arti) root.sfx.Yukselt(); else root.sfx.Tik(740); Yerel.Hap("tick"); }
            var cb = gelCb[dal];
            if (cb != null)
            {
                int j = arti ? once : once - 1; var b = j >= 0 && j < cb.childCount ? cb[j] : null;
                if (b != null) { if (arti) { b.AddToClassList("gel-basamak--dolu"); BasamakDol(b); } else { b.RemoveFromClassList("gel-basamak--dolu"); BasamakSon(b); } }
                string yz = GelYuzde(dal, n);
                if (gelSay[dal] != null) gelSay[dal].text = GelSayi(dal, n, bos > 0);
                Erisim.Etiket(cb, L.T(Gelisim.AD[dal]) + ", " + n + "/" + Gelisim.TAVAN + (yz != "" ? ", " + yz : "") + GelSonrakiEt(dal, n, bos > 0) + ". " + L.T(Gelisim.NE[dal]), "metin");
            }
            for (int i = 0; i < SEKME_N; i++) kirli[i] = true; ustKirli = true;   // altta duran sekme (Oyna satırı, Profil kartı) kapanışta kurulur
            if (gelUst != null) gelUst.text = GelUstMetin(top, bos);
            for (int i = 0; i < Gelisim.DAL; i++)
            {
                GelPasif(gelArti[i], bos > 0 && Ayar.gelisim[i] < Gelisim.TAVAN); GelPasif(gelEksi[i], Ayar.gelisim[i] > 0);
                // son puan verilince ya da geri alınınca öbür dalların "→" önizlemesi de kalkar/gelir (yalnız değişince yazılır)
                if (gelSay[i] != null && gelArti[i] != null) { string t = GelSayi(i, Ayar.gelisim[i], bos > 0); if (gelSay[i].text != t) gelSay[i].text = t; }
            }
            return true;
        }
        // 2026-11-23 inceleme (fikir 12:06 #6): sonraki puanın etkisi ancak puan verilince görünüyordu, oyuncu dallar arasında
        // deneyerek seçiyordu. Puan varken ve tavanda değilken sayı satırının sonunda sıradaki puanın yüzdesi ("→ +%8"); puan
        // yokken ya da tavanda yazı yok (baskı ya da boş vaat yok). Metin yalnız değişince yazılır (GelisimAdim yolu).
        static string GelSayi(int dal, int n, bool puanVar)
        {
            string yz = GelYuzde(dal, n), s = n + "/" + Gelisim.TAVAN + (yz != "" ? " · " + yz : "");
            return puanVar && n < Gelisim.TAVAN ? s + "  → " + GelYuzde(dal, n + 1) : s;
        }
        static string GelSonrakiEt(int dal, int n, bool puanVar) { return puanVar && n < Gelisim.TAVAN ? ", " + L.T("bir puanla {0}", GelYuzde(dal, n + 1)) : ""; }
        static string GelYuzde(int dal, int n) { if (n == 0) return ""; return L.En ? "+" + Gelisim.Yuzde(dal, n) + "%" : "+%" + Gelisim.Yuzde(dal, n); }
        static void GelPasif(VisualElement d, bool etkin)
        {
            if (d == null || d.ClassListContains("gel-dugme--pasif") != etkin) return;   // yalnız değişince
            if (etkin) d.RemoveFromClassList("gel-dugme--pasif"); else d.AddToClassList("gel-dugme--pasif");
            var b = d.userData as ErisimBilgi; if (b != null) b.pasif = !etkin;
        }
        void GelTekrarDur() { gelBekle = false; if (gelTekrar != null) { gelTekrar.Pause(); gelTekrar = null; } }

        // 2026-11-11 (inceleme, fikir 02:05 #2): Sv 1'de (hiç puan yok) beş kartta on soluk − / + düğmesi ve beş kez "0/10 · +%0"
        // ilk açan oyuncuya pasif düğme duvarı gösteriyordu; o durumda düğmeler çizilmez (kart: ad + cümle + basamaklar), üstteki
        // "İlk puanın Sv 2'de" yeter. Yüzde yalnız n > 0 iken yazar. Puanı olan oyuncuda pasif düğme kalır (yerleşim oynamasın).
        VisualElement GelisimDal(int dal, int n, bool puanVar, bool dugmeli = true)
        {
            var k = new VisualElement(); k.AddToClassList("gel-dal");
            var ust = Satir("arali"); ust.pickingMode = PickingMode.Ignore; k.Add(ust);
            ust.Add(Yazi(L.T(Gelisim.AD[dal]), 16, "kalin"));
            string yz = GelYuzde(dal, n);
            var say = Yazi(GelSayi(dal, n, puanVar && dugmeli), 14, "rakam"); ust.Add(say); gelSay[dal] = say;
            var ne = Yazi(L.T(Gelisim.NE[dal]), 12, "soluk"); ne.style.whiteSpace = WhiteSpace.Normal; k.Add(ne);
            var alt = new VisualElement(); alt.AddToClassList("gel-alt"); k.Add(alt);
            int d = dal;
            gelEksi[dal] = gelArti[dal] = null;
            if (dugmeli) { gelEksi[dal] = GelisimDugme("-", n > 0, d, false, L.T("{0}: bir puan geri al", L.T(Gelisim.AD[dal]))); alt.Add(gelEksi[dal]); }
            var cb = new VisualElement(); cb.AddToClassList("gel-cubuk"); cb.pickingMode = PickingMode.Ignore; alt.Add(cb); gelCb[dal] = cb;
            for (int j = 0; j < Gelisim.TAVAN; j++)
            {
                var b = new VisualElement(); b.AddToClassList("gel-basamak"); if (j < n) b.AddToClassList("gel-basamak--dolu"); cb.Add(b);
            }
            Erisim.Etiket(cb, L.T(Gelisim.AD[dal]) + ", " + n + "/" + Gelisim.TAVAN + (yz != "" ? ", " + yz : "") + ". " + L.T(Gelisim.NE[dal]), "metin");
            bool ver = puanVar && n < Gelisim.TAVAN;
            if (dugmeli) { gelArti[dal] = GelisimDugme("+", ver, d, true, L.T("{0}: bir puan ver", L.T(Gelisim.AD[dal]))); alt.Add(gelArti[dal]); }
            return k;
        }

        // Puan verme anı (2026-11-11 inceleme, fikir 23:05 #4): ses ve tık vardı, göz bir şey görmüyordu; basamak tam dolu doğuyordu.
        // 2026-11-15'ten beri sayfa yeniden kurulmaz (GelisimAdim); yalnız o basamak soldan sağa 0,18 sn dolar.
        // Parıltı yok (sayfanın "süs yok" kuralı). Hareketi azaltta anında dolu; ses/tık aynı.
        // Geri alma (2026-11-14 inceleme, fikir 02:05 #6): − hareketsizdi, basamak bir anda boşalıyordu; hangi dalın azaldığı göz
        // ucuyla okunmuyordu. Boşalan basamak dolu doğar ve sağdan sola 0,12 sn söner; tık + ile simetrik ama üç yarım ton pes
        // (880 → 740 Hz). Hareketi azaltta anında boş.
        static void BasamakSon(VisualElement b)
        {
            if (Ayar.reduce) return;
            b.AddToClassList("gel-basamak--dolu");
            b.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50));
            b.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            b.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(.12f, TimeUnit.Second) };
            b.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseIn) };
            b.schedule.Execute(() => { b.style.scale = new Scale(new Vector3(0, 1, 1)); Kalite.Hareket(); }).StartingIn(16);
            b.schedule.Execute(() => { b.RemoveFromClassList("gel-basamak--dolu"); b.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0, TimeUnit.Second) }; b.style.scale = new Scale(Vector3.one); }).StartingIn(150);
        }
        static void BasamakDol(VisualElement b)
        {
            if (Ayar.reduce) return;
            b.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50));
            b.style.scale = new Scale(new Vector3(0, 1, 1));
            b.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            b.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(.18f, TimeUnit.Second) };
            b.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            b.schedule.Execute(() => { b.style.scale = new Scale(Vector3.one); Kalite.Hareket(); }).StartingIn(16);
        }

        VisualElement GelisimDugme(string isaret, bool etkin, int dal, bool arti, string etiket)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("gel-dugme"); if (!etkin) d.AddToClassList("gel-dugme--pasif");
            // "-" ASCII: U+2212 Nunito'da yoksa boş kutu çıkar. Büyük yazıda glif 56 pt düğmeyi aşmasın diye ölçek 1,4'te durur.
            var y = Yazi(isaret, 22 * Mathf.Min(K, 1.4f) / Mathf.Max(1f, K), "kalin"); y.pickingMode = PickingMode.Ignore; d.Add(y);
            // Pasiflik basışta sınıftan okunur (adımlar sınıfı yerinde değiştirir).
            // 2026-11-20 (inceleme, fikir 17:06 #1; HIG / iOS UIStepper "gesture cancellation"): adım PointerDown'da verilip işaretçi
            // hemen düğmeye bağlanıyordu; sayfayı kaydırmak için başparmağını kenardaki ± üstüne koyan oyuncu istemeden puan veriyor
            // ve ScrollView sürüklemeyi hiç almıyordu. Artık basışta hiçbir şey olmaz: tek dokunuş adımı parmak kalkınca verilir;
            // 0,4 sn tutunca ilk adım + bugünkü tekrar başlar ve işaretçi ancak o an bağlanır. Parmak 8 birimden fazla kayarsa ya da
            // işaretçiyi başka öğe (ScrollView) alırsa dokunuş iptal olur, kaydırma sayfanındır. Basılma görseli (:active) aynı.
            d.RegisterCallback<PointerDownEvent>(e =>
            {
                if (d.ClassListContains("gel-dugme--pasif")) return;
                GelTekrarDur(); gelAdim = 0; gelBasT = Time.unscaledTime; gelBasP = e.position; gelBekle = true; gelSahip = d; int pid = e.pointerId;
                gelTekrar = d.schedule.Execute(() =>
                {
                    if (gelBekle)
                    {
                        var tutan = d.panel != null ? d.panel.GetCapturingElement(pid) : null;
                        if (tutan != null && tutan != d) { GelTekrarDur(); return; }   // kaydırma başladı
                        if (Time.unscaledTime - gelBasT < .4f) return;
                        gelBekle = false; d.CapturePointer(pid);
                        if (!GelisimAdim(dal, arti)) GelTekrarDur();
                        return;
                    }
                    float t = Time.unscaledTime - gelBasT;
                    if (t < 1.5f && (gelTekrarSay++ & 1) == 1) return;   // 70 ms tik: 1,5 sn'ye dek her ikinci (~7/sn), sonra her biri (~14/sn)
                    if (!GelisimAdim(dal, arti)) GelTekrarDur();   // tavan/sıfır: durur (son adımın sesi + hap GelisimAdim'de)
                }).Every(70);
            });
            d.RegisterCallback<PointerMoveEvent>(e => { if (gelBekle && gelSahip == d && (e.position - gelBasP).sqrMagnitude > 64f) GelTekrarDur(); });
            d.RegisterCallback<PointerUpEvent>(e =>
            {
                bool tek = gelBekle && gelSahip == d; if (gelSahip == d) GelTekrarDur();   // ikinci parmak başka düğmeyi tutuyorsa onu bozma
                if (d.HasPointerCapture(e.pointerId)) d.ReleasePointer(e.pointerId);
                if (tek && !d.ClassListContains("gel-dugme--pasif")) { gelAdim = 0; GelisimAdim(dal, arti); }
            });
            d.RegisterCallback<PointerLeaveEvent>(e => { if (gelBekle && gelSahip == d) GelTekrarDur(); });
            d.RegisterCallback<PointerCancelEvent>(e => GelTekrarDur());
            d.RegisterCallback<PointerCaptureOutEvent>(e => GelTekrarDur());
            d.RegisterCallback<DetachFromPanelEvent>(e => GelTekrarDur());
            Erisim.Etiket(d, etiket, "dugme", false, !etkin);
            return d;
        }
        int gelTekrarSay;

        // Oyna'nın altındaki satır, dağıtılmamış puan varken (SvHedefSatir yerine).
        VisualElement GelisimSatir()
        {
            int bos = Ayar.GelisimBos;
            var d = new VisualElement(); d.AddToClassList("sv-hedef"); d.AddToClassList("sv-hedef--gelisim");
            var sol = new VisualElement(); sol.AddToClassList("sv-hedef-sol"); sol.pickingMode = PickingMode.Ignore; d.Add(sol);
            // 2026-11-20 (inceleme, fikir 17:06 #5; §7.3 tek mesaj): puan, "Gelişim: oyuncunu güçlendir" ve düz "Dağıt" aynı şeyi üç
            // kez söylüyordu, "Dağıt" düğmeye benzemiyordu. Alt satır kalktı; sağ blok kardeş satırın (SvSagGelisim) "›" kalıbıyla.
            sol.Add(Yazi(L.T("Dağıtılacak puan: {0}", bos), 14, "kalin"));
            var sag = new VisualElement(); sag.AddToClassList("sv-hedef-sag"); sag.pickingMode = PickingMode.Ignore; d.Add(sag);
            sag.Add(Yazi(L.T("Dağıt") + " ›", 15, "kalin"));
            d.AddManipulator(new Clickable(() => { Tik(); GelisimAc(); }));
            Erisim.Etiket(d, L.T("Dağıtılacak puan: {0}", bos) + ", " + L.T("Gelişim: oyuncunu güçlendir"), "dugme");
            return d;
        }

        // Profil kartı (2026-11-11, inceleme fikir 02:05 #4): beş çubuk Gelişim sayfasında ve Atölye'nin "Takımın şimdi" kartında
        // (gelişim payı ucunda) zaten var; Profil'de üçüncü kez beş satır kupa rafını aşağı itiyordu. Tek satır toplam + durum,
        // dokununca sayfa açılır.
        void GelisimKart(VisualElement c)
        {
            var p = Ayar.gelisim; if (p == null) return; int bos = Ayar.GelisimBos, top = Gelisim.Toplam(Ayar.gear.Seviye());
            c.Add(Grup(L.T("Gelişim")));
            var kart = Kart("gel-kart"); kart.AddToClassList("dugme"); c.Add(kart);
            var r = Satir("arali"); r.pickingMode = PickingMode.Ignore; kart.Add(r);
            string ust = top == 0 ? L.T("İlk puanın Sv 2'de") : L.T("{0}/{1} puan", top - bos, top);
            r.Add(Yazi(ust, 15, "kalin")); r.Add(Yazi("›", 18, "soluk"));
            // 2026-11-21 inceleme (fikir 17:06 #7): boş puan yokken "Dağılımı değiştir" kartın kendi işini söylüyordu; yerine oyuncunun
            // öne çıkan dalı (en yüksek, eşitlikte ilki). Hiç puan verilmemişse satır yok.
            int en = 0; for (int i = 1; i < Gelisim.DAL; i++) if (p[i] > p[en]) en = i;
            string alt = top == 0 ? "" : bos > 0 ? L.T("Dağıtılacak puan: {0}", bos) : p[en] > 0 ? L.T("Öne çıkan: {0} {1}", L.T(Gelisim.AD[en]), p[en]) : "";
            if (alt != "") { var ay = Yazi(alt, 13, bos > 0 ? "altin" : "soluk"); ay.style.marginTop = 4; ay.pickingMode = PickingMode.Ignore; kart.Add(ay); }
            kart.AddManipulator(new Clickable(() => { Tik(); GelisimAc(); }));
            Erisim.Etiket(kart, L.T("Gelişim") + ": " + ust + (alt != "" ? ", " + alt : ""), "dugme");
        }
    }
}
