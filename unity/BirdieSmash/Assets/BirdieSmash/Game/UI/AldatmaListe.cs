// Aldatıcı vuruşlar listesi (2026-10-26, Core/Aldatma; Antrenman sayfasının altında). Neden Antrenman: aldatma bir beceridir,
// ödül ya da eşya değil; oyuncu onu "ne öğrenebilirim" sorusunun yanında arar. Profil istatistik sayfası, Soyunma odası
// ekipman sayfası olarak kalır (ikisinin düzenine dokunulmadı).
// Her vuruş bir kart: adı, durumu (Açık ✓ / kilit + "Sv n"), ne yaptığı; açıksa tek satır "Pozisyon · Jest", kilitliyse yalnız
// eksik kapı ✗ ile (2026-11-15; önce dört sabit satırdı). Durum hiçbir yerde yalnız renkle verilmez (✓/✗ ve kelime). Kartlar dokunulmaz (bilgi):
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
            // 2026-11-15 inceleme #4: giriş üç fikirli 3 satırdı; tek cümle. "Bonus yok, tutmazsa normal vuruş" kartlarda zaten okunuyor.
            var ac = Yazi(L.T("Seviye, ekipman ve pozisyon tutunca maçta ◆ ile görünür."), 13, "soluk"); Erisim.Etiket(ac, ac.text, "metin"); c.Add(ac);
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
            string durum = acik ? L.T("Açık") : !svOk ? L.T("Sv {0}", t.sv) : L.T("Ekipman gerekli");
            if (!svOk) { var dr = Satir("rakip-satir"); dr.pickingMode = PickingMode.Ignore; dr.style.flexShrink = 0f;   // kilit + "Lv 15" daralmasın (2026-10-30 dil turu: İngilizce kabından taşıyordu)
                dr.Add(Ikon(Ikonlar.Kilit, 14, SOLUK)); var dy = Yazi(durum, 12, "durum-kilit"); dy.style.marginLeft = 4; dr.Add(dy); r.Add(dr); }
            else r.Add(acik ? IkonYazi(true, durum, 12, "durum-gecti", SOLUK) : Yazi(durum, 12, "durum-kilit"));
            // 2026-11-20 (fikir 17:06 #2): ✓/✗ öneki metinden kalktı (yazı tipi düşmesi); kapı satırında işaret ikon, okuyucuya söz.
            string s1 = L.T("Seviye") + ": " + (svOk ? L.T("Sv {0}", t.sv) : L.T("Sv {0} gerekli", t.sv));
            string s2 = L.T("Ekipman") + ": " + L.T(t.ekAd);
            string s3 = L.T("Pozisyon") + ": " + L.T(t.konum), s4 = L.T("Jest") + ": " + L.T(t.jest);
            // 2026-11-15 inceleme #4: kart başına dört sabit satır yerine: açık kartta yalnız nasıl yapılır (✓'li iki kapı bilgi taşımıyordu),
            // kilitli kartta yalnız eksik kapı(lar). Ekran okuyucu etiketi tüm bilgiyi okumaya devam eder.
            if (acik) kart.Add(Yazi(s3 + " · " + s4, 12, "soluk"));
            else { if (!svOk) kart.Add(IkonYazi(false, s1, 12, "soluk", SOLUK)); if (!ekOk) kart.Add(IkonYazi(false, s2, 12, "soluk", SOLUK)); }
            // 2026-11-27 inceleme (fikir 07:06 #7): seviyesi yeten ama ekipmanı eksik kart sorunu söyleyip çözüme yol vermiyordu.
            // Yalnız o kart dokunulur ve Atölye'yi açar (Kariyer'deki "Atölye'ye git" ile aynı gerekçe); seviye kilidinin çözümü oynamak.
            bool atolye = svOk && !ekOk;
            if (atolye)
            {
                kart.Add(Yazi(L.T("Atölye'de") + " ›", 12, "soluk")); kart.AddToClassList("dokunulur");
                kart.AddManipulator(new Clickable(() => { Tik(kart); SekmeAc(Sekme.Atolye); }));
            }
            Erisim.Etiket(kart, L.T(t.ad) + ", " + durum + ". " + L.T(t.kisa) + " " + s1 + ". " + s2 + ". " + s3 + ". " + s4 + (atolye ? ". " + L.T("Atölye'yi aç") : ""), atolye ? "dugme" : "metin");
            return kart;
        }
    }
}
