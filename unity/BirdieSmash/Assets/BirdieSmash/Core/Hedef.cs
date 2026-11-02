// Hedef eşya (2026-11-02, Ar-Ge 11:40 #1; yasin "Yap"): PR #29 ile kozmetik fiyatları nadirlik bantlarında, destansı/mistik
// bir eşya birkaç günlük oyun. Kıtlık yasin'in isteği ("hemen her şeyi alabilememesi lazım"); ama Tüy yetmeyince pasif kalan
// "n Tüy daha gerekli" düğmesi oyuncuya yapacak bir şey bırakmıyordu. Oyuncu tek bir eşyayı hedef yapar, maç sonu kartı
// hedefe ne kadar yaklaştığını gösterir (hedef gradyanı: yaklaştıkça çaba artar). Fiyatlara dokunmaz.
// Kurallar:
//   · Tek hedef; yenisi eskisinin yerine geçer. Kayıt "kat:id" (kozmetik kategorisi ya da ekipman model yuvası).
//   · Yalnız Tüy'le alınabilecek eşya hedef olur: seviye kilitli eşya olamaz (yasin 1 Kas: "seviye ve tüy ayrı leveller olsun
//     alınamasın"). Ekipman yuva yükseltmesi hedef değil; onun kendi düğmesi var ("Ekipmanı yükselt", #27).
//   · Eşya alınınca ya da veri değişip bulunamazsa hedef kendiliğinden düşer (Coz geçersiz döner, çağıran siler).
//   · Satın alma kartta değil, Dolap'ta/Atölye'de: "Hedefin hazır" yalnız oraya götürür.
// Saf: UnityEngine yok, BotMac sınar (HedefEsya()).
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public static class Hedef
    {
        public const string ANAHTAR = "tuytop_hedef";

        public struct Bilgi { public bool gecerli; public string kat, id, ad; public int fiyat; public bool model; }

        public static string Yaz(string kat, string id) { return kat + ":" + id; }
        public static bool Oku(string kayit, out string kat, out string id)
        {
            kat = id = null; if (string.IsNullOrEmpty(kayit)) return false;
            int i = kayit.IndexOf(':'); if (i <= 0 || i >= kayit.Length - 1) return false;
            kat = kayit.Substring(0, i); id = kayit.Substring(i + 1); return true;
        }

        // Model yuvası mı (raket, kordaj, …, ayakkabı) yoksa kozmetik kategorisi mi.
        public static bool ModelYuva(string kat) { return Array.IndexOf(Gear.MODEL_YUVA, kat) >= 0; }

        // Eşya hedef yapılabilir mi: var, sahip değil, seçili değil, seviyesi gelmiş. Tüy'ün yetip yetmemesi önemsiz
        // (yetiyorsa düğme zaten "Al" der, hedef düğmesi çıkmaz).
        public static bool Yapilir(ModelDurum du) { return du == ModelDurum.Alinir; }

        // Kaydı çözer. secili: kozmetik kategorisinin şu an seçili öğesi (Ayar.KOZ, Game tarafında).
        public static Bilgi Coz(Gear g, string kayit, Func<string, string> secili)
        {
            var b = new Bilgi(); string kat, id; if (g == null || !Oku(kayit, out kat, out id)) return b;
            b.kat = kat; b.id = id;
            if (ModelYuva(kat))
            {
                Dictionary<string, GearModel> l; GearModel m;
                if (!Gear.GEAR.TryGetValue(kat, out l) || !l.TryGetValue(id, out m)) return b;
                b.ad = m.ad; b.fiyat = m.fiyat; b.model = true; b.gecerli = Yapilir(g.Durum(kat, id)) && m.fiyat > 0;
                return b;
            }
            if (Array.IndexOf(Kozmetik.KAT, kat) < 0) return b;
            var o = Kozmetik.Bul(kat, id); if (o == null) return b;
            b.ad = o.ad; b.fiyat = o.fiyat; b.gecerli = Yapilir(Kozmetik.Durum(g, kat, id, secili != null ? secili(kat) : null)) && o.fiyat > 0;
            return b;
        }

        public static int Kalan(int fiyat, int tuy) { return Math.Max(0, fiyat - tuy); }
        public static float Oran(int fiyat, int tuy) { return fiyat <= 0 ? 1 : Math.Min(1f, Math.Max(0f, tuy / (float)fiyat)); }
        // Bu maçın Tüy'ü hedefi ilk kez yetişir yaptı mı ("Hedefin hazır" düğmesi yalnız o maçta; sonraki maçlarda satır "Hazır" der).
        public static bool YeniHazir(int fiyat, int tuyOnce, int tuySonra) { return fiyat > 0 && tuyOnce < fiyat && tuySonra >= fiyat; }

        // Maç sonu ödül alt yazısı (Ar-Ge 11:40 #2): ek ödüllerin hepsi değil, en büyüğü ve "+n bonus". Ek ödül yazıları
        // "ad +sayı" biçiminde (Gear.Odul); sayı okunamazsa ilk kalem. Tam döküm Ayrıntılar'da.
        public static string EnBuyukEk(List<string> ek, out int diger)
        {
            diger = 0; if (ek == null || ek.Count == 0) return null;
            int en = 0, enS = int.MinValue;
            for (int i = 0; i < ek.Count; i++) { int v = SonSayi(ek[i]); if (v > enS) { enS = v; en = i; } }
            diger = ek.Count - 1; return ek[en];
        }
        static int SonSayi(string s)
        {
            if (string.IsNullOrEmpty(s)) return int.MinValue; int i = s.LastIndexOf('+'); int v;
            return i >= 0 && int.TryParse(s.Substring(i + 1).Trim(), out v) ? v : int.MinValue;
        }
    }
}
