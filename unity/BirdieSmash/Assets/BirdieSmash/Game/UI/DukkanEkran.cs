// Tüy dükkânı sayfası (2026-11-01, yasin: "tüy aldırma ya da IAP altyapısı"; karar kartı: önce iCloud). Üst çubuktaki Tüy
// hapına dokununca açılır (tam sayfa, Geri ile kapanır). Dürüst dükkân kuralları (ekonomi tasarımı §4): rastgele sandık
// yok, geri sayım yok, "son şans" yok; her kartta ne kadar Tüy geldiği ve mağazanın gerçek fiyatı. Bonus yüzdesi yazıyla
// ("+%17 fazla"), yalnız renkle değil. Fiyat mağazadan gelmediyse kart "şu an satın alınamıyor" der, fiyat uydurmaz.
// Altta: iCloud durumu (yedekleniyor mu) ve App Store'un istediği "Satın alımları geri yükle".
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        bool dukkanDinliyor; string dukkanNot;

        public void DukkanAc() { var once = gez.sekme; Magaza.O.Baslat(); gez.Ac(Gezinti.DUKKAN); Uygula(once, true); }

        void DukkanDinle()
        {
            if (dukkanDinliyor) return; dukkanDinliyor = true;
            Magaza.O.Degisti += () => { if (gez.Ust == Gezinti.DUKKAN) SayfaKur(Gezinti.DUKKAN); };
            Magaza.O.Bitti += n =>
            {
                dukkanNot = n > 0 ? L.T("{0} Tüy cüzdanında.", UiKok.Sayi(n)) : L.T("Satın alma tamamlanmadı, ücret alınmadı.");
                if (n > 0) { root.sfx.Tik(); Yerel.Hap("win"); ustKirli = true; }
                if (gez.Ust == Gezinti.DUKKAN) SayfaKur(Gezinti.DUKKAN);
            };
        }

        void DukkanKur(VisualElement p)
        {
            DukkanDinle();
            SayfaBasi(p, "Tüy dükkânı");
            var sv = Kaydirma(p); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            var ust = Kart("dukkan-cuzdan"); c.Add(ust);
            var cz = new VisualElement(); cz.AddToClassList("satir"); cz.pickingMode = PickingMode.Ignore; ust.Add(cz);
            cz.Add(Ikon(Ikonlar.Tuy, 26, ALTIN)); cz.Add(Yazi(UiKok.Sayi(Ayar.gear.eko.tuy), 26, "rakam"));
            Erisim.Etiket(ust, L.T("Cüzdanında {0} Tüy", Ayar.gear.eko.tuy), "metin");
            ust.Add(Yazi(L.T("Tüy'le Dolap'ta skin, Atölye'de ekipman alırsın. Maç oynayarak da kazanırsın."), 13, "soluk"));
            if (!string.IsNullOrEmpty(dukkanNot)) { var n = Yazi(dukkanNot, 14, "kalin"); n.AddToClassList("dukkan-not"); c.Add(n); }

            var m = Magaza.O;
            if (m.durum == Magaza.Durum.Yukleniyor) c.Add(Yazi(L.T("Mağazaya bağlanıyor…"), 14, "soluk"));
            else if (m.durum != Magaza.Durum.Hazir) c.Add(Yazi(L.T("Mağaza şu an açılamıyor. İnternet bağlantını kontrol edip birazdan yeniden dene."), 14, "soluk"));

            var iz = new VisualElement(); iz.AddToClassList("dukkan-izgara"); c.Add(iz);
            foreach (var pk in TuyDukkan.PAKETLER)
            {
                string id = pk.id, fiyat = m.Fiyat(id);
                var k = Kart("dukkan-kart"); iz.Add(k);
                var ic = new VisualElement(); ic.AddToClassList("satir"); ic.pickingMode = PickingMode.Ignore; k.Add(ic);
                ic.Add(Ikon(Ikonlar.Tuy, 22, ALTIN)); ic.Add(Yazi(UiKok.Sayi(pk.tuy), 22, "rakam"));
                k.Add(Yazi(L.T(pk.ad), 14, "kalin"));
                if (pk.ek > 0) { var e = Yazi(L.T("+%{0} fazla", pk.ek), 12, "dukkan-ek"); k.Add(e); }
                k.Add(fiyat != null ? Dugme(fiyat, () => { dukkanNot = null; Tik(); Magaza.O.Al(id); }, "dugme--birincil") : Pasif(L.T("Satın alınamıyor")));
                Erisim.Etiket(k, L.T(pk.ad) + ", " + L.T("{0} Tüy", pk.tuy) + (pk.ek > 0 ? ", " + L.T("+%{0} fazla", pk.ek) : "") + ", " + (fiyat ?? L.T("Satın alınamıyor")), "dugme", false, fiyat == null);
            }

            c.Add(Bosluk(12));
            c.Add(Yazi(Bulut.Acik ? L.T("İlerlemen ve aldığın Tüy iCloud'a yedekleniyor; uygulamayı silsen de geri gelir.")
                                  : L.T("iCloud kapalı: ilerlemen yalnız bu cihazda. Ayarlar'da iCloud'u açarsan yedeklenir."), 13, "soluk"));
            c.Add(Dugme(L.T("Satın alımları geri yükle"), () => { Tik(); Magaza.O.GeriYukle(ok => { dukkanNot = ok ? L.T("Kaydın yeniden okundu.") : L.T("Geri yüklenecek bir şey bulunamadı."); ustKirli = true; if (gez.Ust == Gezinti.DUKKAN) SayfaKur(Gezinti.DUKKAN); }); }));
            c.Add(Yazi(L.T("Tüy tüketilen bir üründür: harcayınca biter, süresi dolmaz. Rastgele ödül satılmaz."), 12, "soluk"));
            c.Add(Bosluk(24));
        }
    }
}
