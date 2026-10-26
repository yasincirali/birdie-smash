// Kariyer sekmesi (2026-10-26, adım 1): bugünkü altı basamaklık merdiven (Match.KARIYER) düz liste. Harita, turlar,
// yıldızlar ve maç kartı alt sayfası adım 2'de gelir (tasarım §2.3, §3.1); bu liste o haritanın yerini tutar.
// Durum yalnız renkle değil biçimle de okunur: geçilen = ✓ simgesi + "Geçildi", şimdiki = halka + "ŞİMDİ" + Oyna düğmesi,
// kilitli = kilit simgesi + "Kilitli", soluk. Oyun yalnız açık basamağı oynatır (Career.Basamak); şampiyonda son basamak
// yeniden oynanır. Açılınca şimdiki basamak görünür alana kaydırılır.
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        void KariyerKur(VisualElement k)
        {
            k.Clear(); var sv = Kaydirma(k); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            var t = Yazi(L.T("Kariyer"), 24, "sekme-baslik"); Erisim.Etiket(t, L.T("Kariyer"), "baslik"); c.Add(t);
            c.Add(Yazi(L.Cumle(Career.Line(Ayar.kariyer)), 14, "soluk"));
            if (Ayar.sampiyon) c.Add(Yazi(L.T("Kariyer tamam! Şampiyon forması açıldı."), 14, "altin"));
            c.Add(Bosluk(8));
            int acik = Career.Basamak(Ayar.kariyer); VisualElement simdiki = null;
            for (int i = 0; i < Match.KARIYER.Length; i++)
            {
                var kb = Match.KARIYER[i]; var st = Match.STIL[kb.stil];
                bool gecti = i < Ayar.kariyer, simdi = i == acik, kilit = !gecti && !simdi;
                var kart = Kart("basamak-kart"); if (kilit) kart.AddToClassList("kilitli"); if (simdi) kart.AddToClassList("simdiki"); c.Add(kart);
                var r = Satir("basamak-satir"); kart.Add(r);
                var rozet = new VisualElement(); rozet.AddToClassList("basamak-rozet"); rozet.pickingMode = PickingMode.Ignore; r.Add(rozet);
                rozet.Add(Ikon(gecti ? Ikonlar.Tik : simdi ? Ikonlar.Halka : Ikonlar.Kilit, 22, gecti ? LIMON : simdi ? CAMGOBEGI : SOLUK));
                rozet.Add(Yazi((i + 1).ToString(), 12, "soluk"));
                var orta = new VisualElement(); orta.AddToClassList("basamak-orta"); orta.pickingMode = PickingMode.Ignore; r.Add(orta);
                orta.Add(Yazi(L.T(kb.yer) + (i == Match.KARIYER.Length - 1 ? " · " + L.T("Final") : ""), 17, "kalin"));
                var rk = Satir("rakip-satir"); rk.pickingMode = PickingMode.Ignore; orta.Add(rk);
                var nok = new VisualElement(); nok.AddToClassList("renk-nokta"); nok.style.backgroundColor = Kit.Hex(st.renk); rk.Add(nok);
                rk.Add(Yazi(L.T("Rakip") + " " + st.isim + " · " + L.T(st.ad) + " · " + L.T(CourtView.SAHA[kb.saha].ad), 13, "soluk"));
                string durum = L.T(gecti ? "Geçildi" : simdi ? "ŞİMDİ" : "Kilitli");
                r.Add(Yazi(durum, 12, gecti ? "durum-gecti" : simdi ? "durum-simdi" : "durum-kilit"));
                Erisim.Etiket(kart, L.T("Kariyer") + ", " + (i + 1) + ". " + L.T("basamak") + ", " + L.T(kb.yer) + ", " + st.isim + ", " + durum, "metin", simdi);
                if (simdi)
                {
                    kart.Add(Yazi(L.T(st.ipucu), 13, "ipucu"));
                    kart.Add(Dugme(L.T(gecti || Ayar.sampiyon ? "Yeniden oyna" : "Oyna") + ": " + L.T(kb.yer), () =>
                    {
                        Ayar.kariyerModu = true; Ayar.GearHesap(); Ayar.Kaydet(); root.Oyna();
                    }, "dugme--birincil", null, 17));
                    simdiki = kart;
                }
            }
            if (simdiki != null) { var hedef = simdiki; sv.schedule.Execute(() => sv.ScrollTo(hedef)).StartingIn(50); }
        }
    }
}
