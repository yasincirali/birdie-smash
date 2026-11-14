// Kariyer hikâyesi (2026-11-14, yasin: "level hikâye yolu ve aşamalar hikâyeleştirilebilir; daha güçlü rakiplerle
// karşılaştığımı hissetmeliyim zaman geçtikçe"; öneri https://claude.ai/artifact/RypZsnuY2PGoeYuDweQzRX, seçim A).
// Hikâye yeni karakterle değil, var olan altı rakibin ve oyuncunun yükselişiyle anlatılır:
//  - Güç: rakibin görünen gücü tek sayı (1–99), koşu hızından (g × tarz hızı × 3,2; kariyer.json güç merdiveni). Merdiven tur tur
//    arttığı için sayı da artar (Tur 1 ilk maç ~21, Efsaneler finali 99). Yalnız kariyer basamağında gösterilir.
//  - Dünya sırası: oyuncunun sırası kazanılan basamak sayısından (kayıptan düşmez: ilerlemeyi suçlamayan ilke, ligdeki gibi);
//    1.840'tan Dünya Finalleri finaliyle (24. galibiyet) 1'e iner, sonra "unvanı koru". Rakibin sırası basamağından: oyuncunun o
//    noktadaki sırasının üstünde (final daha yukarıda), 5.–8. turda 2–13 arası.
//  - Son karşılaşma: aynı tarzın kariyerdeki bir önceki basamağı (sıralı kariyerde oraya gelen oyuncu onu kazanmıştır).
// Bölüm başlığı ve hikâye cümlesi kariyer.json'da (tur "bolum", "hikaye"). Saf C#: BotMac Hikayeler() kilitler.
using System;

namespace BirdieSmash.Core
{
    public static class Hikaye
    {
        public const int ILK_SIRA = 1840, ZIRVE_GALIBIYET = 24;   // 24 = dört turun 6'şar basamağı (Dünya Finalleri finali)

        // Görünen koşu hızından güç sayısı. Doğrusal: merdivenin 1,9–4,35 m/s aralığı 18–99'a.
        public static int Guc(Style st) { if (st == null) return 0; return Math.Max(1, Math.Min(99, (int)Math.Round(18 + (st.hiz * 3.2f - 1.9f) * 40))); }
        public static int Guc(int g) { return KariyerVeri.Var(g) ? Guc(KariyerVeri.Rakip(g)) : 0; }

        // Yuvarlak sıra: 100'ün üstü 10'a, 20'nin üstü 5'e yuvarlanır (gerçek sıralama listesi gibi "1.837" değil).
        static int Yuvarla(double r) { int v = (int)Math.Round(r); return v > 100 ? (v + 5) / 10 * 10 : v > 20 ? (v + 2) / 5 * 5 : Math.Max(1, v); }
        public static int OyuncuSira(int galibiyet)
        {
            if (galibiyet >= ZIRVE_GALIBIYET) return 1;
            return Yuvarla(Math.Pow(ILK_SIRA, 1 - Math.Max(0, galibiyet) / (double)ZIRVE_GALIBIYET));
        }
        public static int RakipSira(int g)
        {
            if (g >= ZIRVE_GALIBIYET) return Math.Max(2, 2 + (KariyerVeri.TUR * KariyerVeri.BASAMAK - 1 - g) / 2);
            return Math.Max(2, Yuvarla(OyuncuSira(g) * (KariyerVeri.Final(g) ? .4 : .6)));
        }
        public static int Galibiyet(Ilerleme p)
        {
            int n = 0; if (p == null) return 0;
            for (int t = 0; t < KariyerVeri.Turlar.Length; t++) for (int b = 0; b < KariyerVeri.BASAMAK; b++) if (p.Kazanildi(KariyerVeri.Global(t, b))) n++;
            return n;
        }
        // Aynı tarzın bir önceki kariyer basamağı (yoksa -1).
        public static int OncekiKarsilasma(int g)
        {
            var k = KariyerVeri.Al(g); if (k == null) return -1;
            for (int i = g - 1; i >= 0; i--) { var o = KariyerVeri.Al(i); if (o != null && o.stil == k.stil) return i; }
            return -1;
        }
    }
}
