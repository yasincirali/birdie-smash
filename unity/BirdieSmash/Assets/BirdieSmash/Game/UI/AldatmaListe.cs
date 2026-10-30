// Aldatıcı vuruşlar listesi (2026-10-26, Core/Aldatma; Antrenman sayfasının altında). Neden Antrenman: aldatma bir beceridir,
// ödül ya da eşya değil; oyuncu onu "ne öğrenebilirim" sorusunun yanında arar. Profil istatistik sayfası, Soyunma odası
// ekipman sayfası olarak kalır (ikisinin düzenine dokunulmadı).
// Her vuruş bir kart: adı, durumu (Açık ✓ / kilit + "Sv n"), ne yaptığı, üç kapı ayrı satırda (Seviye, Ekipman: ✓ ya da ✗ ile;
// Pozisyon ve Jest: nasıl yapılır). Durum hiçbir yerde yalnız renkle verilmez (✓/✗ ve kelime). Kartlar dokunulmaz (bilgi):
// dokunma hedefi gerekmez, ekran okuyucu tek etiketle okur.
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        void AldatmaBolum(VisualElement c)
        {
            c.Add(Grup(L.T("Aldatıcı vuruşlar")));
            var ac = Yazi(L.T("Üst seviyelerde açılır. Seviye, ekipman ve pozisyon üçü birden tutunca maçta topun üstünde ◆ ile adı ve jesti görünür; tutmazsa jest normal vuruşa döner. Bonus sayı yok: rakibi geç ve ters çıkarır."), 13, "soluk"); Erisim.Etiket(ac, ac.text, "metin"); c.Add(ac);
            int sv = Ayar.gear != null ? Ayar.gear.Seviye() : 1; var gs = Ayar.GS;
            foreach (var t in Aldatma.Liste) c.Add(AldatmaKart(t, sv, gs));
        }

        VisualElement AldatmaKart(HileTanim t, int sv, GearStats gs)
        {
            bool svOk = sv >= t.sv, ekOk = Aldatma.Ekipman(t, gs), acik = svOk && ekOk;
            var kart = Kart("dugum"); if (!svOk) kart.AddToClassList("kilitli");
            var r = Satir("basamak-satir"); r.pickingMode = PickingMode.Ignore; kart.Add(r);
            var orta = new VisualElement(); orta.AddToClassList("basamak-orta"); orta.pickingMode = PickingMode.Ignore; r.Add(orta);
            orta.Add(Yazi(L.T(t.ad), 16, "kalin"));
            orta.Add(Yazi(L.T(t.kisa), 13, "soluk"));
            string durum = acik ? "✓ " + L.T("Açık") : !svOk ? L.T("Sv {0}", t.sv) : L.T("Ekipman gerekli");
            if (!svOk) { var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; dr.style.flexShrink = 0f;   // kilit + "Lv 15" daralmasın (2026-10-30 dil turu: İngilizce kabından taşıyordu)
                dr.Add(Ikon(Ikonlar.Kilit, 14, SOLUK)); var dy = Yazi(durum, 12, "durum-kilit"); dy.style.marginLeft = 4; dr.Add(dy); r.Add(dr); }
            else r.Add(Yazi(durum, 12, acik ? "durum-gecti" : "durum-kilit"));
            string s1 = (svOk ? "✓ " : "✗ ") + L.T("Seviye") + ": " + (svOk ? L.T("Sv {0}", t.sv) : L.T("Sv {0} gerekli", t.sv));
            string s2 = (ekOk ? "✓ " : "✗ ") + L.T("Ekipman") + ": " + L.T(t.ekAd);
            string s3 = L.T("Pozisyon") + ": " + L.T(t.konum), s4 = L.T("Jest") + ": " + L.T(t.jest);
            foreach (var sat in new[] { s1, s2, s3, s4 }) kart.Add(Yazi(sat, 12, "soluk"));
            Erisim.Etiket(kart, L.T(t.ad) + ", " + durum + ". " + L.T(t.kisa) + " " + s1 + ". " + s2 + ". " + s3 + ". " + s4, "metin");
            return kart;
        }
    }
}
