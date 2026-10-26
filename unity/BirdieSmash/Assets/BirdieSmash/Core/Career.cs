// Kariyer (HTML KARIYER / endMatch): maç sonu ana düğme sıradaki basamağı söyler (Ar-Ge 24 Eki, HTML d682adc; yalnız
// etiket, işlev aynı: yeni maç). Saf C#: bot testi etiketleri doğrular.
// 2026-10-26 kariyer haritası (adım 2): basamak = genel indeks (tur·6 + basamak, KariyerVeri); ilerleme Ilerleme'de
// (yıldızlar, tur açılışı). Eski tek sayı (tuytop_v2.kariyer) Tur 1'in kazanılan basamak sayısı olarak sürer.
namespace BirdieSmash.Core
{
    public static class Career
    {
        // sonraki: maç işlendikten sonra sıradaki genel basamak; -1 ya da oynanacak yeni basamak yoksa "Bir maç daha".
        public static string AgainLabel(bool kariyerMaci, bool won, int sonraki)
        {
            if (kariyerMaci && won && KariyerVeri.Var(sonraki)) return "Sıradaki: " + KariyerVeri.Al(sonraki).yer;
            if (kariyerMaci && !won) return "Tekrar dene";
            return "Bir maç daha";
        }
        // Oyna kartının alt satırı: "2/6 · Asya Turu · Bangkok · File oyuncusu" ya da hepsi bittiyse şampiyon satırı.
        public static string Line(Ilerleme p)
        {
            if (p.HepsiBitti) return "Şampiyon! Finali yeniden oynarsın";
            int g = p.Siradaki(); var k = KariyerVeri.Al(g);
            return (KariyerVeri.BasOf(g) + 1) + "/" + KariyerVeri.BASAMAK + " · " + KariyerVeri.Turlar[KariyerVeri.TurOf(g)].ad + " · " + k.yer + " · " + Match.STIL[k.stil].ad;
        }
    }
}
