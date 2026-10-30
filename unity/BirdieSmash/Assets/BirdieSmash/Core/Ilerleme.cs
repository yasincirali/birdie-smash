// Kariyer ilerlemesi ve yıldızlar (komple-oyun tasarımı §3.1, Ek A.6; adım 2, 2026-10-26). Kayıt: tuytop_ilerleme (yeni,
// tek JSON). Saf C#: taşıma ve yıldız kuralları Unity'siz test edilir (BotMac).
// Yıldız kuralı (basamak başına 3, her biri ayrı kazanılır ve bir daha geri alınmaz):
//   KAZAN  maçı kazan.
//   GOREV  basamağın sabit görevini bitir (Match.BasamakGorevi). Kayıpta da sayılır: kaybetmek de ilerletir (ekonomi §3).
//   FARK   farkla kazan: rakip en çok hedef/2 sayı (5 → ≤ 2, 11 → ≤ 5, 21 → ≤ 10; tasarımdaki 5 ve 11 değerleri).
// Neden bit maskesi, tasarımdaki "yıldız sayısı" değil: üç yıldız farklı maçlarda toplanabilir (önce galibiyet, sonra
// görev); sayı tutulsaydı hangisinin eksik olduğu ve "ilk kez" ödülü (+20 Tüy) bilinmezdi. Sayı = maskenin bit sayısı.
// Açılış: tur, önceki turun finali kazanılınca açılır (seviyeye bağlı değil; yasin varsayılanı kabul etti). Basamak, aynı
// turdaki önceki basamak kazanılınca açılır. Açık basamak (kazanılmış dahil) her zaman yeniden oynanabilir (yıldız için).
// Taşıma (Ek A.6 kuralı: eski anahtar okunur, yeni biçim yazılır, eski silinmez): tuytop_ilerleme yoksa tuytop_v2.kariyer
// (Tur 1'de kazanılan basamak sayısı) Tur 1'in ilk n basamağına KAZAN olarak yazılır; kariyer = 6 → Tur 1 tamam (Şampiyon
// forması Ayar'da aynı kuralla) ve Asya Turu açık. Yeni kayıt varken de eski sayı büyükse birleştirilir (eski build'e dönüp
// ilerleyen oyuncu, güncellemeye geri gelince kaybetmesin). tuytop_v2.kariyer yazılmaya devam eder (Tur1Gecilen).
// Bilinmeyen alanlar okunup aynen geri yazılır (2026-10-26: usta, gunluk, antrenman, imzaOku artık bu sürümün alanları).
// 2026-10-26 karakter (adım 6, Ek A.6): tarz (oyun tarzı kimliği), unvan (seçilen unvan; yoksa kazanılanların en değerlisi),
// ad (oyuncu adı, yalnız cihazda; boş = "Oyuncu"), smac ve mac (Profil istatistiği: toplam smaç, oynanan maç; cihazda
// hesaplanır, hiçbir yere gönderilmez). Aynı JSON'da, aynı "bilinmeyeni koru" kuralıyla: eski build bu alanları okumaz ama
// tuytop_ilerleme'yi yazmaz da (anahtar yalnız bu sürümde var), kayıp olmaz.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class IlerlemeSonuc { public int yeniYildiz, maske, yeniMaske; public bool ilkGalibiyet, turBitti; public string not = ""; }

    public class Ilerleme
    {
        public const int KAZAN = 1, GOREV = 2, FARK = 4;
        public const string ANAHTAR = "tuytop_ilerleme";
        public readonly int[][] tur = new int[KariyerVeri.TUR][];
        Dictionary<string, object> diger = new Dictionary<string, object>();

        public Ilerleme() { for (int t = 0; t < tur.Length; t++) tur[t] = new int[KariyerVeri.BASAMAK]; }

        // ---- karakter alanları (diger sözlüğünde; Yaz aynen geri yazar) ----
        string S(string k) { object v; return diger.TryGetValue(k, out v) ? v as string : null; }
        int N(string k) { object v; return diger.TryGetValue(k, out v) && v is double ? (int)(double)v : 0; }
        public string Tarz { get { return S("tarz") ?? "dengeli"; } set { diger["tarz"] = value; } }
        public string Unvan { get { return S("unvan"); } set { if (value == null) diger.Remove("unvan"); else diger["unvan"] = value; } }
        // Ad: en çok 16 karakter, baştaki/sondaki boşluk atılır, denetim karakteri alınmaz (kayıt JSON'u ve ekran güvenli kalsın).
        public const int AD_AZAMI = 16;
        public string Ad { get { return S("ad"); } set { var a = AdTemizle(value); if (a == null) diger.Remove("ad"); else diger["ad"] = a; } }
        public static string AdTemizle(string s)
        {
            if (s == null) return null; var b = new System.Text.StringBuilder(); foreach (char c in s) if (!char.IsControl(c)) b.Append(c);
            string t = b.ToString().Trim(); if (t.Length > AD_AZAMI) t = t.Substring(0, AD_AZAMI).TrimEnd(); return t.Length == 0 ? null : t;
        }
        public int Smac { get { return N("smac"); } }
        public int Mac { get { return N("mac"); } }
        public void MacSay(int smac) { diger["mac"] = (double)(Mac + 1); diger["smac"] = (double)(Smac + Math.Max(0, smac)); }
        // Yardım (2026-10-29, yasin: "benim hesabımdan oynattığım çocuklar zor olduğunu söyledi"): rakibin gücü hesabın
        // ilerlemesine bağlı (Hızlı maç sıradaki basamağın g'si, kariyer basamağın kendisi); ilerlemiş bir hesapta yeni ya da
        // küçük bir oyuncu her maçı kaybediyordu. Kaybedilen her maç bir kademe ekler, kazanılan her maç bir kademe geri alır
        // (0–YARDIM_AZAMI); maçta rakibin hızı ×YardimK, hatası ÷YardimK (Match.YardimUygula). Ölçüm (zayıf bot, pHit .035,
        // Kolay, 11 sayı): ilk basamakta k 1 → %22, k ,7 → %42, k ,6 → %72. Kazanınca hemen geri döner: güçlü oyuncu tek
        // kayıpta rakibi kalıcı olarak yumuşatmaz. Usta yolu (bilerek seçilen sonsuz merdiven) ve antrenman sayılmaz.
        public const int YARDIM_AZAMI = 4;
        public int Yardim { get { return Math.Max(0, Math.Min(YARDIM_AZAMI, N("yardim"))); } }
        public void YardimIsle(bool won) { diger["yardim"] = (double)Math.Max(0, Math.Min(YARDIM_AZAMI, Yardim + (won ? -1 : 1))); }
        public static float YardimK(int y) { return 1 - .1f * Math.Max(0, Math.Min(YARDIM_AZAMI, y)); }
        // Antrenman sahası (adım 7) gelene kadar 0; unvan koşulu ("Antrenör") buradan okur.
        public int AntrenmanYildiz { get { var o = diger.ContainsKey("antrenman") ? Json.Obj(diger["antrenman"]) : null; int n = 0; if (o != null) foreach (var kv in o) if (kv.Value is double) n += (int)(double)kv.Value; return n; } }

        // ---- 2026-10-26 adım 7–9 alanları (aynı diger sözlüğünde, aynı "bilinmeyeni koru" kuralı) ----
        // antrenman: {görev id: en iyi yıldız 0–3}. Yıldız en iyi sonuçtur, geri alınmaz; DrilKaydet yalnız yeni yıldız sayısını döner.
        Dictionary<string, object> Alt(string k) { var o = diger.ContainsKey(k) ? Json.Obj(diger[k]) : null; if (o == null) { o = new Dictionary<string, object>(); diger[k] = o; } return o; }
        public int DrilYildiz(string id) { var o = diger.ContainsKey("antrenman") ? Json.Obj(diger["antrenman"]) : null; object v; return o != null && o.TryGetValue(id, out v) && v is double ? Math.Max(0, Math.Min(3, (int)(double)v)) : 0; }
        public int DrilKaydet(string id, int yildiz)
        {
            int eski = DrilYildiz(id), y = Math.Max(0, Math.Min(3, yildiz)); if (y <= eski) return 0;
            Alt("antrenman")[id] = (double)y; return y - eski;
        }
        // antrenmanGecmis (2026-10-30, vuruş laboratuvarı): {görev id: son 5 oturumun isabet yüzdesi}. Yıldız en iyi sonucu
        // tutar; gelişimi bu satır gösterir (görev satırındaki küçük çizgi). 4 toptan kısa bırakılan oturum yazılmaz.
        public const int GECMIS = 5;
        public List<int> DrilGecmis(string id)
        {
            var l = new List<int>(); var o = diger.ContainsKey("antrenmanGecmis") ? Json.Obj(diger["antrenmanGecmis"]) : null; object v;
            if (o != null && o.TryGetValue(id, out v) && v is List<object>) foreach (var x in (List<object>)v) if (x is double) l.Add(Math.Max(0, Math.Min(100, (int)(double)x)));
            return l;
        }
        public void DrilGecmisEkle(string id, int isabet, int top)
        {
            if (top < 4) return; var l = DrilGecmis(id); l.Add((int)Math.Round(100.0 * isabet / top)); while (l.Count > GECMIS) l.RemoveAt(0);
            var a = new List<object>(); foreach (var x in l) a.Add((double)x); Alt("antrenmanGecmis")[id] = a;
        }
        // gunluk: {son: "YYYY-AA-GG" (ödülü alınan son gün), say: toplam kazanılan Günün maçı}. Seri tutulmaz (kural: seri yok).
        public string GunlukSon { get { var o = diger.ContainsKey("gunluk") ? Json.Obj(diger["gunluk"]) : null; object v; return o != null && o.TryGetValue("son", out v) ? v as string : null; } }
        public int GunlukSay { get { var o = diger.ContainsKey("gunluk") ? Json.Obj(diger["gunluk"]) : null; object v; return o != null && o.TryGetValue("say", out v) && v is double ? (int)(double)v : 0; } }
        public bool GunlukAlindi(string tarih) { return GunlukSon == tarih; }
        // Bugünün ödülü ilk kez mi alındı (true → +40 Tüy, +30 XP). Saat geri alınırsa da aynı gün iki kez sayılmaz.
        public bool GunlukKazan(string tarih) { if (GunlukAlindi(tarih)) return false; var o = Alt("gunluk"); o["son"] = tarih; o["say"] = (double)(GunlukSay + 1); return true; }
        // usta: kazanılan Usta yolu basamağı sayısı (= sıradaki basamağın sırası). Kayıpta azalmaz.
        public int Usta { get { return N("usta"); } }
        public void UstaIlerle() { diger["usta"] = (double)(Usta + 1); }
        // Usta yıldızları (Core/UstaYolu): basamak başına kazan + görev. ustaGorev = görevi biten son basamak + 1 (0 = yok).
        public int UstaYildiz { get { return N("ustaYildiz"); } }
        public bool UstaGorevBitti(int n) { return N("ustaGorev") == n + 1; }
        // Usta maçı sonucu: yalnız sıradaki basamak sayılır (eski basamak yeniden oynanmaz). Dönüş: yeni yıldız sayısı.
        public int UstaIsle(int n, bool won, bool gorev)
        {
            if (n != Usta) return 0; int y = 0;
            if (gorev && !UstaGorevBitti(n)) { diger["ustaGorev"] = (double)(n + 1); y++; }
            if (won) { UstaIlerle(); y++; }
            if (y > 0) diger["ustaYildiz"] = (double)(UstaYildiz + y); return y;
        }
        // imza: okunan (karşılanan) imza vuruşu toplamı; bs_imza_okur başarımı (15) buradan.
        public int ImzaOkunan { get { return N("imzaOku"); } }
        public void ImzaOkuSay(int n) { if (n > 0) diger["imzaOku"] = (double)(ImzaOkunan + n); }
        // 2026-10-26 aldatıcı vuruşlar (Core/Aldatma): ilk fırsat açıklaması gösterilmiş vuruşlar (bir kez; aynı "bilinmeyeni
        // koru" kuralı). Liste, kimlik sırası önemsiz.
        public List<string> AldatmaGorulen { get { var l = new List<string>(); var o = diger.ContainsKey("aldatmaGor") ? diger["aldatmaGor"] as List<object> : null; if (o != null) foreach (var x in o) if (x is string) l.Add((string)x); return l; } }
        public void AldatmaGor(string id) { var l = AldatmaGorulen; if (l.Contains(id)) return; var o = new List<object>(); foreach (var x in l) o.Add(x); o.Add(id); diger["aldatmaGor"] = o; }
        // Usta yolu: son turun (Dünya Finalleri) finali kazanılınca açılır.
        public bool UstaAcik { get { int s = KariyerVeri.Turlar.Length - 1; return s >= 0 && KariyerVeri.Turlar[s].Var && TurAcik(s) && TurBitti(s); } }

        public static int Say(int m) { return (m & 1) + (m >> 1 & 1) + (m >> 2 & 1); }
        public int Maske(int g) { return tur[KariyerVeri.TurOf(g)][KariyerVeri.BasOf(g)]; }
        public int Yildiz(int g) { return Say(Maske(g)); }
        public bool Kazanildi(int g) { return (Maske(g) & KAZAN) != 0; }
        public int TurYildiz(int t) { int n = 0; foreach (var m in tur[t]) n += Say(m); return n; }
        public int ToplamYildiz { get { int n = 0; for (int t = 0; t < tur.Length; t++) n += TurYildiz(t); return n; } }

        // Tur verisi var ve önceki turun finali kazanıldı.
        public bool TurAcik(int t)
        {
            if (t < 0 || t >= KariyerVeri.Turlar.Length || !KariyerVeri.Turlar[t].Var) return false;
            return t == 0 || (tur[t - 1][KariyerVeri.BASAMAK - 1] & KAZAN) != 0;
        }
        // Önceki tur bitti ama bu turun verisi henüz yok ("Yakında").
        public bool TurYakinda(int t) { return t > 0 && t < KariyerVeri.TUR && (t >= KariyerVeri.Turlar.Length || !KariyerVeri.Turlar[t].Var) && (tur[t - 1][KariyerVeri.BASAMAK - 1] & KAZAN) != 0; }
        public bool Acik(int g) { int t = KariyerVeri.TurOf(g), b = KariyerVeri.BasOf(g); return TurAcik(t) && (b == 0 || (tur[t][b - 1] & KAZAN) != 0); }
        public bool TurBitti(int t) { return (tur[t][KariyerVeri.BASAMAK - 1] & KAZAN) != 0; }

        // Tur 1'de baştan art arda kazanılan basamak sayısı = eski tuytop_v2.kariyer anlamı.
        public int Tur1Gecilen() { int n = 0; while (n < KariyerVeri.BASAMAK && (tur[0][n] & KAZAN) != 0) n++; return n; }

        // Sıradaki maç: açık en yüksek turun ilk kazanılmamış basamağı; hepsi bittiyse açık son turun finali (yeniden).
        public int Siradaki()
        {
            int y = EnYuksekAcikTur(); for (int b = 0; b < KariyerVeri.BASAMAK; b++) if ((tur[y][b] & KAZAN) == 0) return KariyerVeri.Global(y, b);
            return KariyerVeri.Global(y, KariyerVeri.BASAMAK - 1);
        }
        public int EnYuksekAcikTur() { int y = 0; for (int t = 0; t < KariyerVeri.Turlar.Length; t++) if (TurAcik(t)) y = t; return y; }
        // Açık turların hepsi bitti (oynanacak yeni basamak yok): maç sonu "Bir maç daha", Oyna kartı "tamam".
        public bool HepsiBitti { get { return TurBitti(EnYuksekAcikTur()); } }

        public static int FarkSiniri(int target) { return target / 2; }

        // Maç sonucu: yeni yıldızlar, ilk galibiyet, not (Türkçe cümle; çeviri Lang.Cumle kalıplarıyla).
        public IlerlemeSonuc Isle(int g, bool won, bool gorev, int ben, int rakip, int target)
        {
            var r = new IlerlemeSonuc(); if (!KariyerVeri.Var(g)) return r;
            int t = KariyerVeri.TurOf(g), b = KariyerVeri.BasOf(g), eski = tur[t][b], m = 0;
            if (won) m |= KAZAN; if (gorev) m |= GOREV; if (won && rakip <= FarkSiniri(target)) m |= FARK;
            int yeni = m & ~eski; tur[t][b] = eski | m; r.maske = tur[t][b]; r.yeniMaske = yeni; r.yeniYildiz = Say(yeni); r.ilkGalibiyet = (yeni & KAZAN) != 0;
            var k = KariyerVeri.Al(g);
            if (r.ilkGalibiyet)
            {
                if (b < KariyerVeri.BASAMAK - 1) { var n = KariyerVeri.Al(g + 1); r.not = "Yeni basamak açıldı: " + n.yer + ", rakip " + Match.STIL[n.stil].ad + "."; }
                else
                {
                    r.turBitti = true; var tr = KariyerVeri.Turlar[t];
                    bool sonraki = t + 1 < KariyerVeri.Turlar.Length && KariyerVeri.Turlar[t + 1].Var;
                    r.not = t == 0 ? (sonraki ? "Kariyer tamam! Şampiyon forması ve " + KariyerVeri.Turlar[1].ad + " açıldı." : "Kariyer tamam! Şampiyon forması açıldı.")
                        : sonraki ? tr.ad + " tamam! " + KariyerVeri.Turlar[t + 1].ad + " açıldı."
                        : t == KariyerVeri.Turlar.Length - 1 && t == KariyerVeri.TUR - 1 ? tr.ad + " tamam! Usta yolu açıldı." : tr.ad + " tamam! Sonraki tur yakında.";
                }
            }
            else if (!won) r.not = k.yer + " basamağı seni bekliyor.";
            return r;
        }

        // ---- kayıt ----
        public static Ilerleme Oku(string json, int eskiKariyer, bool sampiyon)
        {
            var p = new Ilerleme(); var o = Json.Obj(Json.Parse(json));
            if (o != null)
            {
                var tl = o.ContainsKey("tur") ? o["tur"] as List<object> : null;
                for (int t = 0; tl != null && t < tl.Count && t < p.tur.Length; t++)
                {
                    var bl = tl[t] as List<object>;
                    for (int b = 0; bl != null && b < bl.Count && b < KariyerVeri.BASAMAK; b++) if (bl[b] is double) p.tur[t][b] = Math.Max(0, Math.Min(7, (int)(double)bl[b]));
                }
                foreach (var kv in o) if (kv.Key != "tur" && kv.Key != "v") p.diger[kv.Key] = kv.Value;
            }
            int n = Math.Max(0, Math.Min(KariyerVeri.BASAMAK, sampiyon ? KariyerVeri.BASAMAK : eskiKariyer));
            for (int b = 0; b < n; b++) p.tur[0][b] |= KAZAN;
            // Sıra tutarlılığı: kazanılmış basamağın öncekiler de kazanılmış olmalı (elle bozulmuş kayıt yolu kilitlemesin).
            for (int t = 0; t < p.tur.Length; t++) for (int b = KariyerVeri.BASAMAK - 1; b > 0; b--) if ((p.tur[t][b] & KAZAN) != 0) p.tur[t][b - 1] |= KAZAN;
            for (int t = p.tur.Length - 1; t > 0; t--) if (Array.Exists(p.tur[t], m => (m & KAZAN) != 0)) p.tur[t - 1][KariyerVeri.BASAMAK - 1] |= KAZAN;
            for (int t = 0; t < p.tur.Length; t++) for (int b = KariyerVeri.BASAMAK - 1; b > 0; b--) if ((p.tur[t][b] & KAZAN) != 0) p.tur[t][b - 1] |= KAZAN;
            return p;
        }
        public string Yaz()
        {
            var d = new Dictionary<string, object>(diger); d["v"] = 1.0; var tl = new List<object>();
            foreach (var row in tur) { var bl = new List<object>(); foreach (var m in row) bl.Add((double)m); tl.Add(bl); }
            d["tur"] = tl; return Json.Write(d);
        }
    }
}
