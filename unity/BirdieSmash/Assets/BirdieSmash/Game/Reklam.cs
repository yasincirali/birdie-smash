using UnityEngine;

namespace BirdieSmash.Game
{
    // Reklam şeridi için ayrılan alan (yasin 2026-11-06: "oyun ekranında ve menüde ince bir reklam için alan ayır, profesyonel
    // olmalı, sinir etmemeli"). Bu dosya yeri ayırır ve yer tutucuyu çizer; AdMob şeridi o yere Game/ReklamSdk.cs'den oturur
    // (2026-11-07, yasin: "şerit en başta bağlı olarak çıksın"; şerit yüklenmezse yer tutucu kalır).
    //  - Boyut: standart telefon şeridi 320×50 pt. Uyarlanabilir şerit (AdMob anchored adaptive) telefonda ~50–62 pt; SDK
    //    gelince yükseklik onun bildirdiği değerle değiştirilir (PT tek yer).
    //  - (2026-11-07: maç üstü şerit kaldırıldı, aşağıda MAC_SERIT; maçta reklam yeri kort kenarı panoları.)
    //  - Yer: menüde sekme çubuğunun hemen üstü (içerik kaydırılır, şerit sabit; başparmak bölgesindeki sekmelerle arasında
    //    çizgi var). Maçta EKRANIN ÜSTÜ: alt yarı kaydırma/vuruş alanı, orada şerit yanlış dokunuş demek (reklam ağlarının
    //    "oyun kontrollerinin yanında reklam" yasağı da bu). HUD ve kamera kadrajı şeridin altına iner, kort kapanmaz.
    //  - Sinir etmemesi için: geçiş (tam ekran) reklam yok, ses yok, maç içinde yenileme yok (SDK'da yenileme kapatılacak,
    //    şerit maç arasında değişir), şerit hareket etmez.
    //  - ALAN açıkken şerit yüklenmediyse (SDK yok, rıza yok, ağ yok) yer tutucu ("Reklam") görünür. SDK 2026-11-07'de bağlandı
    //    (Google test kimlikleri); mağaza 1.0 öncesi yasin'in AdMob kimlikleri girer. YAPMAN_GEREKENLER.md › Reklam.
    public static class Reklam
    {
        public const bool ALAN = true;
        public const float PT = 50f;
        // Kort kenarı LED panoları (2026-11-07, CourtView.KenarPano): oyun içi reklam yeri; şerit gibi ekrandan yer almaz.
        public const bool KENAR_PANO = true;
        // Nokta → piksel: iOS'ta 1 pt = dpi/163 piksel (Retina 2×, Pro 3×); dpi okunamazsa ekran genişliğinden (375 pt varsayımı).
        public static float Piksel()
        {
            float olcek = Screen.dpi > 50 ? Screen.dpi / 163f : Screen.width / 375f;
            return Mathf.Round(PT * Mathf.Clamp(olcek, 1f, 4f));
        }
        // Maçta (oyun ve maç sonu) üst pay; menüde sıfır (menü kendi şeridini sekme çubuğunun üstünde taşır).
        // Maç üstü şerit KAPALI (yasin kararı 2026-11-07, kart "Maçtaki reklam nerede dursun?" → "Kort kenarı panolar"):
        // ralli sırasında göz şeride kayıyor ve kort 50 pt kısalıyordu. Maçtaki reklam yeri artık kortun kendisi
        // (KENAR_PANO + arka LED pano); menü şeridi (ALAN) kalır. Kod duruyor: MAC_SERIT true yapılırsa şerit geri gelir.
        public const bool MAC_SERIT = false;
        public static void MacGuncelle(bool macta) { Guvenli.reklamUst = ALAN && MAC_SERIT && macta ? Piksel() : 0f; }
    }
}
