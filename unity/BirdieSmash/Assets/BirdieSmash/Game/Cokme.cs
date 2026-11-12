// Çökme kaydedici (2026-11-11, yasin: "uygulama TF'de maç içinde crash oldu ... crash free olmalıyız").
// Neden: bulutta cihaz/Xcode yok; testçi TestFlight'ın "çökmeyi paylaş" sorusuna evet demezse ASC'de rapor olmaz (11 Kas'ta
// tool/tani/tf_cokme.py boş döndü). Bu dosya iki şeyi oyunun içinde tutar:
//   1) Oturum izi: 2 sn'de bir persistentDataPath/son_oturum.txt'ye tek küçük dosya (ekran, skor, ralli, bellek, kalite, ısı,
//      düşük bellek uyarısı sayısı, son 3 hata). Arka plana geçince "arka", düzgün çıkışta "cikis" yazılır. Açılışta dosya hâlâ
//      "onde" diyorsa önceki oturum ön plandayken bitti: çökme ya da iOS'un bellek yüzünden kapatması (jetsam). PlayerPrefs değil:
//      PlayerPrefs.Save eşzamanlı plist yazar (31 Eki O1 kararı), dosya yazımı ~0,1 ms ve yalnız 2 sn'de bir.
//   2) Unity CrashReport (iOS yerel çökme, CiBuild enableCrashReportAPI): açılışta son rapor izle birleştirilir, son_cokme.txt'de durur.
// Okuma: gizli FPS katmanı açıkken (Ayarlar başlığına 5 dokunuş) Ayarlar'ın sonunda "Tanı" bölümü, "Çökme kaydını paylaş" sistem
// paylaş sayfasıyla metni gönderir. Sunucu yok, kendiliğinden hiçbir yere gitmez; metinde kişisel veri yok (kayıt, ad, hesap yazılmaz).
// Düşük bellek uyarısında (Application.lowMemory) kullanılmayan varlıklar boşaltılır: jetsam'dan önce son şans.
using System.IO;
using System.Text;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class Cokme
    {
        static string izYol, cokmeYol; static float yazT; static int dusukBellek; static bool kurulu;
        static readonly string[] hata = new string[3]; static int hataN;
        static readonly StringBuilder sb = new StringBuilder(512);
        public static string Son { get; private set; }   // önceki ön plan bitişi / CrashReport metni; yoksa null

        public static void Basla()
        {
            if (kurulu) return; kurulu = true;
            izYol = Path.Combine(Application.persistentDataPath, "son_oturum.txt"); cokmeYol = Path.Combine(Application.persistentDataPath, "son_cokme.txt");
            string yeni = null;
            try { if (File.Exists(izYol)) { var s = File.ReadAllText(izYol); if (s.StartsWith("onde")) yeni = "Önceki oturum oyun açıkken bitti (çökme ya da bellek)\n" + s; } } catch { }
            try
            {
                var r = CrashReport.reports;
                if (r != null && r.Length > 0)
                {
                    var c = r[r.Length - 1]; string t = c.text ?? "";
                    yeni = (yeni ?? "") + "\n--- CrashReport " + c.time.ToString("u") + " (" + r.Length + " rapor)\n" + (t.Length > 12000 ? t.Substring(0, 12000) : t);
                    CrashReport.RemoveAll();
                }
            }
            catch { }
            try { if (yeni != null) File.WriteAllText(cokmeYol, yeni); Son = yeni ?? (File.Exists(cokmeYol) ? File.ReadAllText(cokmeYol) : null); } catch { Son = yeni; }
            Application.logMessageReceived += Gunluk;
            Application.lowMemory += DusukBellek;
            Yaz("onde", null);
        }

        public static void Temizle() { Son = null; try { File.Delete(cokmeYol); } catch { } }

        static void Gunluk(string m, string st, LogType t)
        {
            if (t != LogType.Exception && t != LogType.Assert) return;
            string s = m + " @ " + (st != null && st.Length > 300 ? st.Substring(0, 300) : st);
            hata[hataN % hata.Length] = System.DateTime.Now.ToString("HH:mm:ss") + " " + s; hataN++;
        }

        static void DusukBellek()
        {
            dusukBellek++;
            // Ralli ortasında da: iOS uyarıdan sonra saniyeler içinde kapatabilir, bir karelik takılma kapanmaktan iyidir.
            Resources.UnloadUnusedAssets(); System.GC.Collect();
            yazT = 0;
        }

        // GameRoot.Update her kare çağırır; dosya 2 sn'de bir (düşük bellek uyarısından hemen sonra beklemeden).
        public static void Tik(GameRoot root)
        {
            if (!kurulu) return;
            yazT -= Time.unscaledDeltaTime; if (yazT > 0) return; yazT = 2;
            Yaz("onde", root);
        }
        public static void Durum(bool arka) { if (kurulu) Yaz(arka ? "arka" : "onde", null); }
        public static void Cikis() { if (kurulu) Yaz("cikis", null); }

        static GameRoot sonRoot;
        static void Yaz(string durum, GameRoot root)
        {
            if (root != null) sonRoot = root; root = sonRoot;
            try
            {
                sb.Length = 0;
                sb.Append(durum).Append('\n').Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(" · sürüm ").Append(Application.version)
                  .Append(" · ").Append(SystemInfo.deviceModel).Append(" · ").Append(SystemInfo.operatingSystem).Append('\n');
                sb.Append("açık ").Append(Mathf.RoundToInt(Time.realtimeSinceStartup)).Append(" sn · bellek ")
                  .Append(UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576).Append(" MB ayrılmış / ").Append(SystemInfo.systemMemorySize).Append(" MB · mono ")
                  .Append(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576).Append(" MB · düşük bellek uyarısı ").Append(dusukBellek).Append('\n');
                sb.Append("kalite ").Append(Kalite.Basamak).Append(" · ısı ").Append(Kalite.Isil).Append(" · hedef ").Append(Application.targetFrameRate).Append(" fps · model ").Append(Ayar.model).Append(" · kamera ").Append(Ayar.kamera).Append('\n');
                if (root != null)
                {
                    sb.Append("ekran ").Append(root.ekran).Append(root.paused ? " (duraklatıldı)" : "");
                    var M = root.match;
                    if (M != null && root.ekran == Ekran.Oyun) sb.Append(" · skor ").Append(M.sc[0]).Append('-').Append(M.sc[1]).Append(" · ralli ").Append(M.rally).Append(M.between ? " · sayı arası" : "").Append(" · saha ").Append(M.saha).Append(M.Dril != null ? " · " + M.Dril.ad : "");
                    sb.Append('\n');
                }
                for (int i = 0; i < hata.Length; i++) { var h = hata[(hataN + i) % hata.Length]; if (h != null) sb.Append("hata: ").Append(h).Append('\n'); }
                File.WriteAllText(izYol, sb.ToString());
            }
            catch { }
        }
    }
}
