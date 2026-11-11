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
            bas.Add(Yazi(top == 0 ? L.T("İlk puanın Sv 2'de") : bos > 0 ? L.T("Dağıtılacak puan: {0}", bos) : L.T("Bütün puanların dağıtıldı"), 16, "kalin"));
            if (top > 0) bas.Add(Yazi(L.T("{0}/{1} puan", top - bos, top), 13, "soluk"));
            var ac = Yazi(sv >= 30 ? L.T("Her seviye bir puan verdi. Dal başına en çok 10; dağılımı istediğin zaman ücretsiz değiştirirsin.")
                : L.T("Her seviye bir puan verir. Dal başına en çok 10; dağılımı istediğin zaman ücretsiz değiştirirsin."), 13, "soluk");
            ac.style.whiteSpace = WhiteSpace.Normal; ac.style.marginTop = 4; ac.style.marginBottom = 6; c.Add(ac);
            if (Ayar.gear.eko.esit && !Ayar.kariyerModu) { var es = Yazi(L.T("Eşit şartlar açık: gelişim bu maçlarda sayılmaz."), 13, "ipucu"); es.style.whiteSpace = WhiteSpace.Normal; es.style.marginBottom = 6; c.Add(es); }
            for (int i = 0; i < Gelisim.DAL; i++) c.Add(GelisimDal(i, p[i], bos > 0, top > 0));
        }

        // 2026-11-11 (inceleme, fikir 02:05 #2): Sv 1'de (hiç puan yok) beş kartta on soluk − / + düğmesi ve beş kez "0/10 · +%0"
        // ilk açan oyuncuya pasif düğme duvarı gösteriyordu; o durumda düğmeler çizilmez (kart: ad + cümle + basamaklar), üstteki
        // "İlk puanın Sv 2'de" yeter. Yüzde yalnız n > 0 iken yazar. Puanı olan oyuncuda pasif düğme kalır (yerleşim oynamasın).
        VisualElement GelisimDal(int dal, int n, bool puanVar, bool dugmeli = true)
        {
            var k = new VisualElement(); k.AddToClassList("gel-dal");
            var ust = Satir("arali"); ust.pickingMode = PickingMode.Ignore; k.Add(ust);
            ust.Add(Yazi(L.T(Gelisim.AD[dal]), 16, "kalin"));
            string yz = "+%" + Gelisim.Yuzde(dal, n); if (L.En) yz = "+" + Gelisim.Yuzde(dal, n) + "%";
            if (n == 0) yz = "";
            ust.Add(Yazi(n + "/" + Gelisim.TAVAN + (yz != "" ? " · " + yz : ""), 14, "rakam"));
            var ne = Yazi(L.T(Gelisim.NE[dal]), 12, "soluk"); ne.style.whiteSpace = WhiteSpace.Normal; k.Add(ne);
            var alt = new VisualElement(); alt.AddToClassList("gel-alt"); k.Add(alt);
            int d = dal;
            if (dugmeli) alt.Add(GelisimDugme("-", n > 0, () => { if (Ayar.GelisimGeriAl(d)) { Tik(); Degisti(); } }, L.T("{0}: bir puan geri al", L.T(Gelisim.AD[dal]))));
            var cb = new VisualElement(); cb.AddToClassList("gel-cubuk"); cb.pickingMode = PickingMode.Ignore; alt.Add(cb);
            for (int j = 0; j < Gelisim.TAVAN; j++)
            {
                var b = new VisualElement(); b.AddToClassList("gel-basamak"); if (j < n) b.AddToClassList("gel-basamak--dolu"); cb.Add(b);
                if (dal == gelAniDal && j == gelAniBas && j < n) { gelAniDal = -1; BasamakDol(b); }
            }
            Erisim.Etiket(cb, L.T(Gelisim.AD[dal]) + ", " + n + "/" + Gelisim.TAVAN + (yz != "" ? ", " + yz : "") + ". " + L.T(Gelisim.NE[dal]), "metin");
            bool ver = puanVar && n < Gelisim.TAVAN;
            int n0 = n;
            if (dugmeli) alt.Add(GelisimDugme("+", ver, () => { if (Ayar.GelisimVer(d)) { root.sfx.Yukselt(); Yerel.Hap("tick"); gelAniDal = d; gelAniBas = n0; Degisti(); } }, L.T("{0}: bir puan ver", L.T(Gelisim.AD[dal]))));
            return k;
        }

        // Puan verme anı (2026-11-11 inceleme, fikir 23:05 #4): ses ve tık vardı, göz bir şey görmüyordu; basamak tam dolu doğuyordu.
        // Sayfa yine yeniden kurulur (Degisti, kaydırma korunur); yeni kurulan kartta yalnız o basamak soldan sağa 0,18 sn dolar.
        // Parıltı yok (sayfanın "süs yok" kuralı). Hareketi azaltta anında dolu; ses/tık aynı. Geri alma (−) hareketsiz.
        int gelAniDal = -1, gelAniBas = -1;
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

        VisualElement GelisimDugme(string isaret, bool etkin, System.Action a, string etiket)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("gel-dugme"); if (!etkin) d.AddToClassList("gel-dugme--pasif");
            // "-" ASCII: U+2212 Nunito'da yoksa boş kutu çıkar. Büyük yazıda glif 56 pt düğmeyi aşmasın diye ölçek 1,4'te durur.
            var y = Yazi(isaret, 22 * Mathf.Min(K, 1.4f) / Mathf.Max(1f, K), "kalin"); y.pickingMode = PickingMode.Ignore; d.Add(y);
            if (etkin) d.AddManipulator(new Clickable(a));
            Erisim.Etiket(d, etiket, "dugme", false, !etkin);
            return d;
        }

        // Oyna'nın altındaki satır, dağıtılmamış puan varken (SvHedefSatir yerine).
        VisualElement GelisimSatir()
        {
            int bos = Ayar.GelisimBos;
            var d = new VisualElement(); d.AddToClassList("sv-hedef"); d.AddToClassList("sv-hedef--gelisim");
            var sol = new VisualElement(); sol.AddToClassList("sv-hedef-sol"); sol.pickingMode = PickingMode.Ignore; d.Add(sol);
            sol.Add(Yazi(L.T("Dağıtılacak puan: {0}", bos), 14, "kalin")); sol.Add(Yazi(L.T("Gelişim: oyuncunu güçlendir"), 12, "soluk"));
            var sag = new VisualElement(); sag.AddToClassList("sv-hedef-sag"); sag.pickingMode = PickingMode.Ignore; d.Add(sag);
            sag.Add(Yazi(L.T("Dağıt"), 15, "kalin"));
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
            string alt = top == 0 ? "" : bos > 0 ? L.T("Dağıtılacak puan: {0}", bos) : L.T("Dağılımı değiştir");
            if (alt != "") { var ay = Yazi(alt, 13, bos > 0 ? "altin" : "soluk"); ay.style.marginTop = 4; ay.pickingMode = PickingMode.Ignore; kart.Add(ay); }
            kart.AddManipulator(new Clickable(() => { Tik(); GelisimAc(); }));
            Erisim.Etiket(kart, L.T("Gelişim") + ": " + ust + (alt != "" ? ", " + alt : ""), "dugme");
        }
    }
}
