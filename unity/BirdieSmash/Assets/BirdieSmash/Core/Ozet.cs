// Maç sonu özeti ve mağaza puanı kararı (HTML ozetKart / ozetIpucu / puanBak ile aynı kural). Saf C#: bot testi sırayı ve
// koşulu doğrular.
//   Özet (Ar-Ge 23 Eki #2): 12 sayılık paragraf yerine sabit öncelik listesinden sıfır olmayan ilk iki kart + tek ipucu.
//   Ortalamaya göre seçmek geçmiş kaydı isterdi; sabit liste yeter. Ralli 8'in altındaysa öne çıkmaz; hiçbir kart yoksa
//   en uzun ralli tek kart kalır. İpucu: yenilgide tek ölçü (dışarı giden vuruşlar, bırakılmayan dışarı top, mükemmel),
//   galibiyette ipucu yok (2026-10-30 Ar-Ge #3: "Bu maçta kalan görev" yıldız satırının tekrarıydı). Tam liste "Ayrıntılar" ardında (Hud).
//   Puan (Ar-Ge 23 Eki #1, App Store 5.6.1): kariyer basamağı açan galibiyet ya da 3. toplam galibiyet, ömür boyu en
//   fazla 3 kez; sonuç ekranından 1,5 sn sonra yalnız sistem penceresi (GameRoot). Kendi ön sorumuz yok.
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public static class Ozet
    {
        public struct Kart { public int deger; public string ad; }
        static readonly string[] SIRA = { "En uzun ralli", "Mükemmel", "Smaç", "Aldatma", "File vuruşu", "Uzanma", "Tepeden", "Split step" };
        static int Deger(Match M, int i)
        {
            switch (i) { case 0: return M.maxRally; case 1: return M.perfects; case 2: return M.smashes; case 3: return M.deceives; case 4: return M.kills; case 5: return M.saves; case 6: return M.tepeden; default: return M.splits; }
        }
        public static List<Kart> Kartlar(Match M)
        {
            var o = new List<Kart>();
            for (int i = 0; i < SIRA.Length && o.Count < 2; i++) { int v = Deger(M, i); if (v >= (i == 0 ? 8 : 1)) o.Add(new Kart { deger = v, ad = SIRA[i] }); }
            if (o.Count == 0) o.Add(new Kart { deger = M.maxRally, ad = SIRA[0] });
            return o;
        }
        // Kaybın tek nedeni (2026-11-08 inceleme, fikir 07:05 #1): ipucu yazısı ile önerilen ders düğmesi ayrı kurallarla seçiliyordu
        // (rakip 4 kez file önüne bıraktı → yazı "öne erken çık", düğme "Savunma"). İkisi bu sırayı okur: kendi dışarılarım >
        // rakibin sayı kaynağı (≥3) > genel. Böylece kayıp kartı tek bir "neden + ne yap" söyler.
        public const int NEDEN_GENEL = 0, NEDEN_DISARI = 1, NEDEN_RAKIP_SMAC = 2, NEDEN_RAKIP_FILE = 3;
        public static int Neden(Match M)
        {
            if (M.outsBen >= 2) return NEDEN_DISARI;
            int rs = M.rakipSmacSayi, rf = M.rakipFileSayi;
            if (System.Math.Max(rs, rf) >= 3) return rs >= rf ? NEDEN_RAKIP_SMAC : NEDEN_RAKIP_FILE;
            return NEDEN_GENEL;
        }
        // Dönüş: Türkçe kalıp + yer tutucu değeri (çeviri görünümde L.T ile; Core dil bilmez).
        public static string Ipucu(Match M, bool won, out object arg)
        {
            arg = null;
            if (won) return null;
            if (M.outsBen >= 2) { arg = M.outsBen; return "{0} vuruşun dışarı gitti: çizgiden biraz içeriye nişan al."; }
            // Rakibe özel ipucu (2026-11-07 inceleme, fikir 00:03 #2): genel cümle üçüncü kayıptan sonra okunmuyor; rakibin sayı
            // kaynağı ≥3 ise adıyla tek çare. Kendi dışarılarım önce gelir (kendi hatam daha kolay düzelir). Oyuncuyu suçlamaz.
            int rs = M.rakipSmacSayi, rf = M.rakipFileSayi;
            if (Neden(M) >= NEDEN_RAKIP_SMAC) { arg = M.RakipAd(false); return rs >= rf ? "{0} smaçla sayı topladı: biraz geride bekle, smaçı karşılamaya hazır ol." : "{0} file önüne bıraktı: vurduktan sonra ortaya dön, öne erken çık."; }
            if (M.outsLeft == 0) return "Dışarı gidecek topu bırak: sayı senin olur.";
            if (M.perfects == 0) return "Top parlayınca topun tam üstünden geç: mükemmel vuruş gelir.";
            return "Uzun rallide nefesini clear ile topla.";
        }
        public const int PUAN_AZAMI = 3;
        public static bool PuanIste(bool won, bool basamakAcildi, int winsSonra, int sayac)
        {
            return won && sayac < PUAN_AZAMI && (basamakAcildi || winsSonra == 3);
        }
    }
}
