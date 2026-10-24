// Kariyer (HTML KARIYER / endMatch): altı basamak, her biri bir mekân ve tarz. Maç sonu ana düğme sıradaki basamağı
// söyler (Ar-Ge 24 Eki, HTML d682adc; yalnız etiket, işlev aynı: yeni maç). Saf C#: bot testi etiketleri doğrular.
namespace BirdieSmash.Core
{
    public static class Career
    {
        // kariyerAfter: maç sonucu işlendikten sonraki basamak (açılan son basamak indeksi).
        public static string AgainLabel(bool kariyerMaci, bool won, int kariyerAfter)
        {
            if (kariyerMaci && won && kariyerAfter < Match.KARIYER.Length) return "Sıradaki: " + Match.KARIYER[kariyerAfter].yer;
            if (kariyerMaci && !won) return "Tekrar dene";
            return "Bir maç daha";
        }
        // Oynanan basamak i kazanıldıysa ve en son açılan basamaksa bir sonraki açılır.
        public static void Apply(ref int kariyer, int i, bool won, out string note)
        {
            note = "";
            if (won && i == kariyer)
            {
                kariyer = i + 1;
                note = kariyer >= Match.KARIYER.Length ? "Kariyer tamam! Şampiyon forması açıldı." : "Yeni basamak açıldı: " + Match.KARIYER[i + 1].yer + ", rakip " + Match.STIL[Match.KARIYER[i + 1].stil].ad + ".";
            }
            else if (!won) note = Match.KARIYER[i].yer + " basamağı seni bekliyor.";
        }
        public static string Line(int kariyer)
        {
            if (kariyer >= Match.KARIYER.Length) return "Şampiyon! Son basamağı yeniden oynarsın";
            var k = Match.KARIYER[kariyer]; return (kariyer + 1) + "/" + Match.KARIYER.Length + " · " + k.yer + " · " + Match.STIL[k.stil].ad;
        }
        public static int Basamak(int kariyer) { return System.Math.Min(kariyer, Match.KARIYER.Length - 1); }
    }
}
