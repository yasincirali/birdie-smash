// Cihaza göre kalite (2026-10-26, yasin: "performansta her telefonda akıcı çalışacak şekilde olmalı"). Üç basamak:
//   2 yüksek: 4× MSAA, yumuşak gölge, gölge mesafesi 40 m, yerel çözünürlük
//   1 orta  : 2× MSAA, sert gölge, 25 m, çözünürlük ×0,85
//   0 düşük : MSAA yok, gerçek zamanlı gölge yok (oyuncuların disk gölgesi kalır), 2 kemik ağırlık, çözünürlük ×0,7
//             (2026-10-27: açılışta 0 ise mip'li dokular yarı çözünürlük; gölge haritası 2'de yüksek, 1'de orta, kararlı sığdırma)
// Başlangıç basamağı donanımdan (bellek, çekirdek, GPU belleği) tahmin edilir; maç sırasında son 3 sn'nin ortalama kare
// süresi 1/50 sn'yi aşarsa bir basamak iner (en çok 5 sn'de bir), 20 sn boyunca 1/58'in altında kalırsa bir basamak çıkar
// (başlangıç basamağının üstüne çıkmaz). Çözünürlük yalnız inişte değişir: her geçişte yeniden boyutlama takılma yapar.
// Ekran turunda (-bsekran, yazılım GL) uyum kapalı ve basamak 2: kareler yavaş ama görüntü tam kalitede ölçülsün.
// Kare hızı (2026-10-26, yasin Ar-Ge önerisi "ekle": ProMotion'da maç 120 Hz, şartlı): yalnız maç/antrenman oynanırken,
// en üst basamakta (ve cihazın tavanı da en üstse), düşük güç modu kapalıyken, ekran ≥ 90 Hz ise ekranın hızı (en çok 120);
// menü, duraklat ve maç sonu 60. 120'de son 3 sn ortalaması 1/100 sn'yi aşarsa oturum boyunca 60'a döner (kalite inmeden);
// ısınıp basamak inerse de 60. Ayar seçeneği yok. Benzetim kare hızından bağımsız: tool/unity_test/KareHizi.cs aynı
// tohumlarla 60/120 Hz'i karşılaştırır (400 maç: galibiyet %87,5 / %90,3, ralli 17,97 / 18,04; fark gürültü içinde).
using UnityEngine;

namespace BirdieSmash.Game
{
    public class Kalite : MonoBehaviour
    {
        public static int Basamak { get; private set; } = 2;
        // 2026-11-12 (inceleme 00:05 #3, ikinci inceleme): gölge mesafesi 40/25 m sabitti, kort 13,4 m. Tek kademe StableFit gölge
        // haritasını o mesafeyi saran küreye yaydığı için figür gölgesi gereksiz bulanıktı. Kadraj çözülünce (FitCamera) kameradan en
        // uzak kort köşesine uzaklık + pay buraya yazılır; basamak tavanı (40/25) üst sınır kalır. 0 = henüz kadraj yok, tavan.
        // İlk incelemenin "kamera eğilince uzak dipte kesilir" kaygısı: takip/giriş/zafer kamerası konumu uzaklaştırmaz (eğim yalnız
        // döndürür, zafer yaklaşır); pay 2,5 m figür boyunu da kapsar.
        static float kadrajGolge;
        static float GolgeMesafe(int b) { float tavan = b == 2 ? 40 : 25; return kadrajGolge > 0 ? Mathf.Min(tavan, kadrajGolge) : tavan; }
        public static void KadrajGolge(float d) { kadrajGolge = d; QualitySettings.shadowDistance = GolgeMesafe(Basamak); }
        int tavan; float top, sayac, inisBekle, iyiSure, topY, sayY; bool sonYuksek; int natW, natH; Camera cam; public static bool Kilitli;
        GameRoot root; bool yuksekKapali; float gucBak; bool dusukGuc;
        // Isıl tavan (2026-11-11 inceleme, fikir 21:05 #3; 6 Kas'ta "ısınma geri bildirimi gelirse" diye bekletilmişti, yasin 11 Kas
        // "performans ilerletmeleri de içerilmeli"): sıcakta (2) 120 Hz kapanır ve basamak tavanı bir iner, kritikte (3) basamak 0
        // (gölge yok, çözünürlük ×.7). Isı 30 sn normal/ılık kalınca tavan kalkar, basamak eski "20 sn iyi kare" yoluyla kendisi
        // çıkar. İniş de Iste() üzerinden: ralli sürerken bekler (O5 kuralı).
        public static int Isil { get; private set; } int isilTavan = 2; float isilIyi;
        static int EkranHz()
        {
#if UNITY_2022_2_OR_NEWER
            return (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
#else
            return Screen.currentResolution.refreshRate;
#endif
        }
        void KareHedefi()
        {
            gucBak -= Time.unscaledDeltaTime; if (gucBak <= 0) { gucBak = 5; dusukGuc = Yerel.DusukGuc(); IsilBak(); }
            bool oyunda = root != null && root.ekran == Ekran.Oyun && !root.paused;
            // Menü yalnız hareket sürerken yüksek hızda (2026-10-28, yasin "her yerde smooth"): kaydırma, sürükleme, USS geçişi
            // ya da perde HAREKET_SURE içinde olduysa ProMotion'da 120, durunca 60 (pil). Durağan menü eskisi gibi 60.
            bool menuHareket = !oyunda && Time.unscaledTime - hareketSon < HAREKET_SURE;
            int hz = EkranHz(), hedef = 60;
            if ((oyunda || menuHareket) && !Kilitli && !yuksekKapali && !dusukGuc && Isil < 2 && Basamak == 2 && tavan == 2 && hz >= 90) hedef = Mathf.Min(120, hz);
            if (Application.targetFrameRate != hedef) Application.targetFrameRate = hedef;
            // Duraklatmada çizim yarı hızda (2026-11-07 inceleme, fikir 00:03 #4 küçültülmüş): sahne donuk ama kort, tribün ve gölge
            // her kare yeniden çiziliyordu. renderFrameInterval 2 = 30 fps çizim, Update/dokunuş 60 Hz kalır; duraklat menüsüne
            // dokunulunca (Hareket) hemen tam hıza döner. Menüde "8 sn dokunulmazsa" yapılmadı: canlı afiş 30'da kesik görünebilir.
            bool durgun = root != null && root.ekran == Ekran.Oyun && root.paused && !menuHareket && !Kilitli;
            int aralik = durgun ? 2 : 1;
            if (UnityEngine.Rendering.OnDemandRendering.renderFrameInterval != aralik) UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = aralik;
        }

        void IsilBak()
        {
            if (Kilitli) return;
            Isil = Yerel.Isil();
            int t = Isil >= 3 ? 0 : Isil == 2 ? Mathf.Max(0, tavan - 1) : tavan;
            if (t < isilTavan) { isilTavan = t; isilIyi = 0; if (Basamak > t) Iste(t, true); }
            else if (t > isilTavan) { isilIyi += 5; if (isilIyi >= 30) { isilTavan = t; isilIyi = 0; } }
            else isilIyi = 0;
        }

        const float HAREKET_SURE = .5f; static float hareketSon = -99;
        // Menü bir şey kıpırdattığında çağırır (UiKok: dokunma/sürükleme/tekerlek/USS geçişi; Hud: perde).
        public static void Hareket() { hareketSon = Time.unscaledTime; }

        public static Kalite Kur(GameObject g, Camera cam)
        {
            var k = g.AddComponent<Kalite>(); k.cam = cam; k.root = g.GetComponent<GameRoot>(); k.natW = Screen.width; k.natH = Screen.height;
            int b = 2; int mem = SystemInfo.systemMemorySize, cek = SystemInfo.processorCount, gpu = SystemInfo.graphicsMemorySize;
            if (mem > 0 && mem < 2500 || cek <= 4) b = 1;
            if (mem > 0 && mem < 1800 || cek <= 2 || gpu > 0 && gpu < 400) b = 0;
            Kilitli = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bsekran") >= 0; if (Kilitli) b = 2;
            k.tavan = b; k.isilTavan = b; k.Uygula(b, true);
            SesArabellek(b);
            // Düşük basamakta (2 çekirdek ya da < 1,8 GB: iPhone 6s/SE 1. nesil) mip'li dokular yarı çözünürlükte yüklenir: 1024'lük
            // atlas 512 olur, doku belleği ~¼. Yalnız açılışta: oturum içinde değişirse bütün dokular yeniden yüklenir (takılma).
            if (b == 0) MipSiniri(1);
            return k;
        }
        static void MipSiniri(int n)
        {
#if UNITY_2022_2_OR_NEWER
            QualitySettings.globalTextureMipmapLimit = n;
#else
            QualitySettings.masterTextureLimit = n;
#endif
        }
        // Ekran turu tanısı (2026-10-27): uygulanan kalite.
        // Ses arabelleği (2026-11-06 inceleme #2): vuruş sesi önceden sentezli ve PlayOneShot ile çalıyor, kalan gecikme DSP
        // arabelleğinde; titreşim yerel çağrıyla anında geldiği için ses bir arabellek kadar geride kalıyordu. AudioManager.asset
        // depoda yok (varsayılan). İlk ses çalmadan (Sfx.Build'den önce) 256 örneğe iner (48 kHz'de ~5 ms); zayıf cihazda (basamak 0)
        // çıtırtı riski yüzünden dokunulmaz. Değer tanı satırında, ekran turunda ve TestFlight'ta okunur.
        static void SesArabellek(int b)
        {
            if (b == 0 || Kilitli) return;
            var c = AudioSettings.GetConfiguration(); if (c.dspBufferSize > 0 && c.dspBufferSize <= 256) return;
            c.dspBufferSize = 256; AudioSettings.Reset(c);
        }

        // 2026-11-12 (inceleme, fikir 2a): kortun üç tavan spotu piksel ışık mı köşe ışığı mı, cihazda önce ölçülür; pişirme kararı bu satıra bakar.
        public static string Tani
        {
            get
            {
                return "basamak " + Basamak + " · MSAA " + QualitySettings.antiAliasing + " · gölge " + QualitySettings.shadows + " " + QualitySettings.shadowResolution + " " + QualitySettings.shadowDistance.ToString("0.0") + " m · piksel ışık " + QualitySettings.pixelLightCount + " · aniso "
                    + QualitySettings.anisotropicFiltering + " · kemik " + QualitySettings.skinWeights + " · hedef " + Application.targetFrameRate + " fps · vSync " + QualitySettings.vSyncCount
                    + " · çözünürlük " + Screen.width + "x" + Screen.height + " · bellek " + SystemInfo.systemMemorySize + " MB · çekirdek " + SystemInfo.processorCount + " · ses " + AudioSettings.GetConfiguration().dspBufferSize + "/" + AudioSettings.outputSampleRate + " Hz · ısı " + Isil;
            }
        }

        void Uygula(int b, bool cozunurluk)
        {
            Basamak = b;
            QualitySettings.antiAliasing = b == 2 ? 4 : b == 1 ? 2 : 0; if (cam != null) cam.allowMSAA = b > 0;
            QualitySettings.shadows = b == 2 ? ShadowQuality.All : b == 1 ? ShadowQuality.HardOnly : ShadowQuality.Disable;
            QualitySettings.shadowDistance = GolgeMesafe(b); QualitySettings.shadowCascades = 1;
            QualitySettings.skinWeights = b == 0 ? SkinWeights.TwoBones : SkinWeights.FourBones;
            // 2026-10-27 performans turu 2: gölge haritası boyu ve izdüşümü açıkça (depoda QualitySettings.asset yok; Unity'nin
            // iOS varsayılan düzeyi neyse oydu). Kamera artık topu izleyip eğiliyor: kararlı sığdırma (StableFit) gölge kenarının
            // kamera dönerken titremesini keser; 2'de yüksek (yumuşak kenar daha az bulanık), 1'de orta. Anizotropi doku başına
            // (ahşap zemin 4×, file 4×); 0'da kapalı.
            QualitySettings.shadowResolution = b == 2 ? ShadowResolution.High : ShadowResolution.Medium; QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.anisotropicFiltering = b > 0 ? AnisotropicFiltering.Enable : AnisotropicFiltering.Disable;
            if (cozunurluk && !Application.isEditor && natW > 0)
            {
                float s = b == 2 ? 1 : b == 1 ? .85f : .7f;
                Screen.SetResolution(Mathf.RoundToInt(natW * s), Mathf.RoundToInt(natH * s), Screen.fullScreenMode);
            }
        }

        // 2026-10-31 (Unity incelemesi O5): basamak değişimi Screen.SetResolution ve MSAA'yı değiştirir; ralli sürerken bu bir
        // kare takılma ve görüntü sıçraması demek (tam da oyuncunun topa baktığı an). Ralli sürüyorsa istek bekletilir ve sayı
        // arasında, duraklatmada ya da menüde uygulanır. Bekleyen istek varken yenisi gelirse son istek geçerli.
        int bekleyenB = -1; bool bekleyenCoz;
        bool RalliSuruyor { get { var M = root != null ? root.match : null; return root != null && root.ekran == Ekran.Oyun && !root.paused && M != null && !M.between && M.s != null; } }
        void Iste(int b, bool cozunurluk)
        {
            if (!RalliSuruyor) { Uygula(b, cozunurluk); bekleyenB = -1; return; }
            bekleyenCoz = bekleyenB >= 0 ? bekleyenCoz || cozunurluk : cozunurluk; bekleyenB = b;
        }

        void Update()
        {
            KareHedefi();
            if (Kilitli) return;
            if (bekleyenB >= 0 && !RalliSuruyor) { Uygula(bekleyenB, bekleyenCoz); bekleyenB = -1; }
            float dt = Time.unscaledDeltaTime; if (dt <= 0 || dt > .5f) return;   // arka plandan dönüş gibi tek uzun kareleri sayma
            inisBekle -= dt;
            // Yüksek hız kareleri ayrı ortalanır: menü 120 ile 60 arasında gidip gelince karışık pencere (60'ın 16,7 ms'si)
            // 120'yi tutamıyor sanıp maçta da kapatırdı. Hedef değiştiği kare iki tarafa da sayılmaz (geçiş karesi).
            bool yuksek = Application.targetFrameRate > 60; if (yuksek != sonYuksek) { sonYuksek = yuksek; return; }
            if (yuksek)
            {
                topY += dt; sayY++; if (topY < 3) return;
                float oy = topY / sayY; topY = 0; sayY = 0; if (oy > 1 / 100f) yuksekKapali = true;   // 120'yi tutamıyor: 60'a dön, kaliteye dokunma
                return;
            }
            top += dt; sayac++;
            if (top < 3) return;
            float ort = top / sayac; top = 0; sayac = 0;
            if (ort > 1 / 50f && Basamak > 0 && inisBekle <= 0) { Iste(Basamak - 1, true); inisBekle = 5; iyiSure = 0; return; }
            if (ort < 1 / 58f) { iyiSure += 3; if (iyiSure >= 20 && Basamak < Mathf.Min(tavan, isilTavan)) { Iste(Basamak + 1, false); iyiSure = 0; } }
            else iyiSure = 0;
        }
    }
}
