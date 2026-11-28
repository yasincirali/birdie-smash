// Dereceli maç kaydı (2026-11-27, yasin: "ranka göre rakip bul, skor ve rank listesi"): Core/Lig'in Glicko-2 + kupa + lig
// yapısı artık oyuna bağlı. Durum yerelde (PlayerPrefs "tuytop_lig", Lig.Yaz biçimi); asıl doğrulama (iki tarafın sonucunu
// sunucuda eşleştirme) Cloud Code ile sonraki dilim. Bu yüzden skor listesi şimdilik doğrulanmamış sayılır (ArkadasUgs.cs notu).
// İlk 5 maç yerleştirme: kupa gösterilmez, gizli puan oturur (Lig.YERLESTIRME).
using BirdieSmash.Core;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class Derece
    {
        const string ANAHTAR = "tuytop_lig";
        static LigDurum ben;
        public static LigDurum Ben { get { if (ben == null) ben = Lig.Oku(PlayerPrefs.GetString(ANAHTAR, "")); return ben; } }
        public static int Puan { get { return (int)System.Math.Round(Ben.r); } }                // eşleştirme anahtarı: gizli puan
        public static int Kupa { get { return Ben.Yerlesti ? Ben.kupa : -1; } }                 // -1: yerleşmemiş
        public static string LigAdi { get { return Ben.Yerlesti ? L.T(Lig.Ad(Ben.kupa)) : L.T("Yerleşme maçları {0}/{1}", Ben.mac, Lig.YERLESTIRME); } }

        // Rakip kartından rakibin Glicko durumunu kurar: kupa gizli puanın izdüşümü (Lig.Sonuc'taki kupa=(r-1200)*1.2 tersi), belirsizlik orta.
        static LigDurum Rakip(OyuncuKarti k)
        {
            var r = new LigDurum();
            if (k != null && k.kupa >= 0) { r.r = 1200 + k.kupa / 1.2; r.rd = 120; }
            return r;
        }

        // Maç bitti: sonucu işler, görünen kupa farkını döner (yerleşmede 0).
        public static int Uygula(bool kazandi, OyuncuKarti rakip)
        {
            int fark = Lig.Sonuc(Ben, Rakip(rakip), kazandi);
            PlayerPrefs.SetString(ANAHTAR, Lig.Yaz(Ben)); Ayar.KaydetSonra();
            UgsSira.Gonder(Ben.kupa);   // 2026-11-28 (yasin: "leaderboard tüm online oynayan insanlara göre olmalı"): yerleşmede de yazılır (kupa 0), liste ilk maçtan itibaren herkesi gösterir
            return fark;
        }

        public static string Ozet(int fark)
        {
            if (!Ben.Yerlesti) return L.T("Yerleşme maçı {0}/{1}", Ben.mac, Lig.YERLESTIRME);
            return (fark >= 0 ? "+" : "−") + System.Math.Abs(fark) + " " + L.T("kupa") + " · " + L.T(Lig.Ad(Ben.kupa));
        }
    }
}
