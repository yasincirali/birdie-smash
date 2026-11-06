using UnityEngine;

namespace BirdieSmash.Game
{
    // Reklam şeridi için ayrılan alan (yasin 2026-11-06: "oyun ekranında ve menüde ince bir reklam için alan ayır, profesyonel
    // olmalı, sinir etmemeli"). Henüz reklam SDK'sı yok: bu dosya yalnız yeri ayırır ve yer tutucuyu çizer.
    //  - Boyut: standart telefon şeridi 320×50 pt. Uyarlanabilir şerit (AdMob anchored adaptive) telefonda ~50–62 pt; SDK
    //    gelince yükseklik onun bildirdiği değerle değiştirilir (PT tek yer).
    //  - Yer: menüde sekme çubuğunun hemen üstü (içerik kaydırılır, şerit sabit; başparmak bölgesindeki sekmelerle arasında
    //    çizgi var). Maçta EKRANIN ÜSTÜ: alt yarı kaydırma/vuruş alanı, orada şerit yanlış dokunuş demek (reklam ağlarının
    //    "oyun kontrollerinin yanında reklam" yasağı da bu). HUD ve kamera kadrajı şeridin altına iner, kort kapanmaz.
    //  - Sinir etmemesi için: geçiş (tam ekran) reklam yok, ses yok, maç içinde yenileme yok (SDK'da yenileme kapatılacak,
    //    şerit maç arasında değişir), şerit hareket etmez.
    //  - ALAN açıkken SDK yoksa yer tutucu ("Reklam") görünür. Mağaza 1.0 gönderiminden önce ya SDK bağlanır ya ALAN false
    //    yapılır (boş kutu mağazada profesyonel durmaz). YAPMAN_GEREKENLER.md'de yazılı.
    public static class Reklam
    {
        public const bool ALAN = true;
        public const float PT = 50f;
        // Nokta → piksel: iOS'ta 1 pt = dpi/163 piksel (Retina 2×, Pro 3×); dpi okunamazsa ekran genişliğinden (375 pt varsayımı).
        public static float Piksel()
        {
            float olcek = Screen.dpi > 50 ? Screen.dpi / 163f : Screen.width / 375f;
            return Mathf.Round(PT * Mathf.Clamp(olcek, 1f, 4f));
        }
        // Maçta (oyun ve maç sonu) üst pay; menüde sıfır (menü kendi şeridini sekme çubuğunun üstünde taşır).
        public static void MacGuncelle(bool macta) { Guvenli.reklamUst = ALAN && macta ? Piksel() : 0f; }
    }
}
