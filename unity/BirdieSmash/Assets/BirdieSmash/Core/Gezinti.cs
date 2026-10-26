// Menü gezintisi (komple-oyun tasarımı §2.1–2.2, adım 1; 2026-10-26). Saf C#: Unity'siz test edilir (tool/unity_test/BotMac).
// Neden ayrı: geri davranışı (sol üst "‹ Geri", iOS kenar kaydırması, Android geri tuşu = Escape) üç yerden gelir; kural
// tek yerde olsun ki üçü aynı sonucu versin.
//   · 4 sekme (Oyna, Kariyer, Soyunma odası, Profil). Sekme değişince açık sayfalar kapanır (iOS sekme çubuğu kalıbı).
//   · Sayfa yığını: Ayarlar (⚙) → Nasıl oynanır / Lisanslar; alt sayfa (Hızlı maç kurulumu, kariyer maç kartı) yığının
//     en üstünde durur. Aynı anda tek alt sayfa: yenisi açılınca öteki kapanır (iki sheet üst üste binmez; 2026-10-26).
//   · Geri: önce en üstteki sayfa kapanır; yığın boşsa Oyna dışındaki sekmeden Oyna'ya döner; Oyna'da geri bir şey yapmaz
//     (uygulamadan çıkmak sistemin işi).
//   · Sekmeler kendi durumunu tutar (kaydırma yeri vb. görünüm katmanında): burada yalnız hangi sekme ve hangi sayfa.
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public enum Sekme { Oyna = 0, Kariyer = 1, Soyunma = 2, Profil = 3 }

    public class Gezinti
    {
        public const string AYAR = "ayar", NASIL = "nasil", LISANS = "lisans", HIZLI = "hizli", KART = "kart";
        public Sekme sekme = Sekme.Oyna;
        readonly List<string> yigin = new List<string>();

        public int Derinlik { get { return yigin.Count; } }
        public string Ust { get { return yigin.Count > 0 ? yigin[yigin.Count - 1] : null; } }
        // Alt sayfa (sheet) tam sayfa değildir: altındaki ekran görünür kalır.
        public static bool AltSayfa(string s) { return s == HIZLI || s == KART; }
        public string AcikAltSayfa { get { return Acik(KART) ? KART : Acik(HIZLI) ? HIZLI : null; } }
        public bool Acik(string s) { return yigin.Contains(s); }

        // Sekme seçimi: değiştiyse true. Aynı sekmeye yeniden dokunmak açık sayfaları kapatır (iOS: köke dön).
        public bool SekmeSec(Sekme s) { bool d = s != sekme; sekme = s; yigin.Clear(); return d; }
        public void Ac(string sayfa)
        {
            if (string.IsNullOrEmpty(sayfa) || Ust == sayfa) return; yigin.Remove(sayfa);
            if (AltSayfa(sayfa)) { yigin.Remove(HIZLI); yigin.Remove(KART); }
            yigin.Add(sayfa);
        }
        public void Kapat(string sayfa) { yigin.Remove(sayfa); }
        public void Kok() { yigin.Clear(); }
        public bool Geri()
        {
            if (yigin.Count > 0) { yigin.RemoveAt(yigin.Count - 1); return true; }
            if (sekme != Sekme.Oyna) { sekme = Sekme.Oyna; return true; }
            return false;
        }
    }
}
