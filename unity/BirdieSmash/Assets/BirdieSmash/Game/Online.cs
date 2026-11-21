// Online arkadaş maçı, taşıma ucu (2026-11-20): Core/Oturum'un baytlarını Game Center (GKMatch) ile taşır. Köprü
// Plugins/iOS/BirdieOnline.mm; iOS dışında ve Editor'de hiçbir şey çağrılmaz (Ara hemen "bitti" olur).
// 2026-11-21 (yasin, TestFlight'ta denedikten sonra: "arkadaşınla oynayı test ettim, burası default olarak açık gelsin"):
// herkese açık; tanı katmanı şartı kalktı. Arkadaş listesi, arkadaşa davet ve arkadaş ekleme aynı köprüde (UiKok.ArkadasKur).
// Giriş Unity'nin Social API'siyle (Basarim ile aynı Game Center oturumu); eşleştirme ve davet Apple'ın sayfasında.
using System;
using UnityEngine;
using BirdieSmash.Core;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BirdieSmash.Game
{
    public static class Online
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BirdieOnlineDinle();
        [DllImport("__Internal")] static extern void BirdieOnlineAra();
        [DllImport("__Internal")] static extern int BirdieOnlineDurum();
        [DllImport("__Internal")] static extern void BirdieOnlineKapat();
        [DllImport("__Internal")] static extern void BirdieOnlineGonder(byte[] bayt, int n);
        [DllImport("__Internal")] static extern int BirdieOnlineAl(byte[] tampon, int kapasite);
        [DllImport("__Internal")] static extern void BirdieOnlineArkadasYukle();
        [DllImport("__Internal")] static extern int BirdieOnlineArkadasDurum();
        [DllImport("__Internal")] static extern int BirdieOnlineArkadaslar(byte[] tampon, int kapasite);
        [DllImport("__Internal")] static extern void BirdieOnlineDavet(string id, string mesaj);
        [DllImport("__Internal")] static extern void BirdieOnlineArkadasEkle();
        static void YerelArkYukle() { try { BirdieOnlineArkadasYukle(); } catch { } }
        static int YerelArkDurum() { try { return BirdieOnlineArkadasDurum(); } catch { return ARK_YOK; } }
        static int YerelArkadaslar(byte[] t) { try { return BirdieOnlineArkadaslar(t, t.Length); } catch { return 0; } }
        static void YerelDavet(string id, string m) { try { BirdieOnlineDavet(id, m); } catch { } }
        static void YerelArkEkle() { try { BirdieOnlineArkadasEkle(); } catch { } }
        static int Durum() { try { return BirdieOnlineDurum(); } catch { return 0; } }
        static void Gonder(byte[] b) { try { BirdieOnlineGonder(b, b.Length); } catch { } }
        static int Al(byte[] t) { try { return BirdieOnlineAl(t, t.Length); } catch { return 0; } }
        static void YerelAra() { try { BirdieOnlineAra(); } catch { } }
        static void YerelKapat() { try { BirdieOnlineKapat(); } catch { } }
        static void YerelDinle() { try { BirdieOnlineDinle(); } catch { } }
#else
        static int Durum() { return 0; }
        static void Gonder(byte[] b) { }
        static int Al(byte[] t) { return 0; }
        static void YerelAra() { }
        static void YerelKapat() { }
        static void YerelDinle() { }
        static void YerelArkYukle() { }
        static int YerelArkDurum() { return ARK_YOK; }
        static int YerelArkadaslar(byte[] t) { return 0; }
        static void YerelDavet(string id, string m) { }
        static void YerelArkEkle() { }
#endif
        const int BOS = 0, ARIYOR = 1, BAGLI = 2;

        public static bool Bayrak { get { return true; } }
        // Game Center yalnız iOS'ta; Editor ve Linux ekran build'inde düğme yine görünür (tasarım ve tur), dokunuş hiçbir şey açmaz.

        // ---- arkadaşlar (2026-11-21) ----
        // Durum köprüyle aynı: 0 istenmedi, 1 yükleniyor, 2 hazır, 3 izin yok, 4 kullanılamıyor (giriş yok, iOS 14.5 altı).
        public const int ARK_ISTENMEDI = 0, ARK_YUKLENIYOR = 1, ARK_HAZIR = 2, ARK_IZIN_YOK = 3, ARK_YOK = 4;
        public struct Arkadas { public string id, ad; }
        public static int ArkadasDurum { get { return YerelArkDurum(); } }
        // Girişten sonra listeyi ister (giriş yoksa önce giriş). İkinci çağrı listeyi yeniler.
        public static void ArkadasYukle() { Giris(ok => { if (ok) YerelArkYukle(); }); }
        static readonly byte[] arkTampon = new byte[16384];
        public static System.Collections.Generic.List<Arkadas> Arkadaslar()
        {
            var l = new System.Collections.Generic.List<Arkadas>(); int n = YerelArkadaslar(arkTampon); if (n <= 0) return l;
            foreach (var satir in System.Text.Encoding.UTF8.GetString(arkTampon, 0, n).Split('\n'))
            {
                int t = satir.IndexOf('\t'); if (t <= 0) continue;
                l.Add(new Arkadas { id = satir.Substring(0, t), ad = satir.Substring(t + 1) });
            }
            l.Sort((a, b) => string.Compare(a.ad, b.ad, StringComparison.CurrentCultureIgnoreCase));
            return l;
        }
        // Arkadaşa maç isteği: alıcısı seçili eşleştirme sayfası; davet onun telefonuna bildirim olarak gider.
        public static void Davet(string id, string mesaj) { Kapat(); Giris(ok => { if (ok) YerelDavet(id, mesaj); }); }
        public static void ArkadasEkle() { Giris(ok => { if (ok) YerelArkEkle(); }); }
        public static Oturum oturum;          // null = bağlantı yok
        static bool macIstendi, girisIstendi;
        static readonly byte[] tampon = new byte[2048];

        // Game Center girişi + davet dinleyicisi. Açılışta (bayrak açıksa) ve Ara'dan önce; ikinci çağrı hemen döner.
        public static void Giris(Action<bool> sonra)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { Social.localUser.Authenticate((bool ok) => { if (ok) YerelDinle(); if (sonra != null) sonra(ok); }); }
            catch (Exception) { if (sonra != null) sonra(false); }
#else
            if (sonra != null) sonra(false);
#endif
        }
        public static void Dinle() { if (Bayrak && !girisIstendi) { girisIstendi = true; Giris(null); } }

        // Apple'ın eşleştirme sayfasını açar (arkadaş çağır ya da rastgele rakip).
        public static void Ara() { Kapat(); Giris(ok => { if (ok) YerelAra(); }); }

        // Kendi kartım: ad Game Center takma adı (Apple'ın denetlediği ad; serbest metin girişi yok), seviye ekipman seviyesi.
        // Görünüş (model, kozmetik) henüz kartta değil: rakip varsayılan figürle çizilir.
        static OyuncuKarti Kartim()
        {
            string ad = null;
#if UNITY_IOS && !UNITY_EDITOR
            try { ad = Social.localUser.userName; } catch (Exception) { }
#endif
            return new OyuncuKarti { ad = ad, seviye = Ayar.gear != null ? Ayar.gear.Seviye() : 1 };
        }

        // Her karede (GameRoot.Update başı). bag = süren online maçın ağ ucu, yoksa null.
        // true: selamlaşma bitti, maç şimdi kurulmalı (bir kez döner).
        public static bool Tik(float dt, UzakBag bag)
        {
            if (!Bayrak && oturum == null) return false;
            int d = Durum();
            if (oturum == null)
            {
                if (d != BAGLI) return false;   // eşleştirme sayfası açık, iptal edildi ya da hiç başlamadı
                oturum = new Oturum(Environment.TickCount, Kartim()); macIstendi = false;
            }
            int n;
            while ((n = Al(tampon)) > 0) { var b = new byte[n]; Buffer.BlockCopy(tampon, 0, b, 0, n); oturum.Al(b); }
            if (d != BAGLI) oturum.Kop("baglanti");
            oturum.Tik(dt, bag);
            for (int i = 0; i < oturum.giden.Count; i++) Gonder(oturum.giden[i]);
            oturum.giden.Clear();
            if (!macIstendi && oturum.durum == Oturum.Durum.Hazir) { macIstendi = true; return true; }
            return false;
        }

        public static void Kapat() { if (oturum == null && Durum() == BOS) return; YerelKapat(); oturum = null; macIstendi = false; }
    }
}
