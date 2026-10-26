// Cihaza göre kalite (2026-10-26, yasin: "performansta her telefonda akıcı çalışacak şekilde olmalı"). Üç basamak:
//   2 yüksek: 4× MSAA, yumuşak gölge, gölge mesafesi 40 m, yerel çözünürlük
//   1 orta  : 2× MSAA, sert gölge, 25 m, çözünürlük ×0,85
//   0 düşük : MSAA yok, gerçek zamanlı gölge yok (oyuncuların disk gölgesi kalır), 2 kemik ağırlık, çözünürlük ×0,7
// Başlangıç basamağı donanımdan (bellek, çekirdek, GPU belleği) tahmin edilir; maç sırasında son 3 sn'nin ortalama kare
// süresi 1/50 sn'yi aşarsa bir basamak iner (en çok 5 sn'de bir), 20 sn boyunca 1/58'in altında kalırsa bir basamak çıkar
// (başlangıç basamağının üstüne çıkmaz). Çözünürlük yalnız inişte değişir: her geçişte yeniden boyutlama takılma yapar.
// Ekran turunda (-bsekran, yazılım GL) uyum kapalı ve basamak 2: kareler yavaş ama görüntü tam kalitede ölçülsün.
using UnityEngine;

namespace BirdieSmash.Game
{
    public class Kalite : MonoBehaviour
    {
        public static int Basamak { get; private set; } = 2;
        int tavan; float top, sayac, inisBekle, iyiSure; int natW, natH; Camera cam; public static bool Kilitli;

        public static Kalite Kur(GameObject g, Camera cam)
        {
            var k = g.AddComponent<Kalite>(); k.cam = cam; k.natW = Screen.width; k.natH = Screen.height;
            int b = 2; int mem = SystemInfo.systemMemorySize, cek = SystemInfo.processorCount, gpu = SystemInfo.graphicsMemorySize;
            if (mem > 0 && mem < 2500 || cek <= 4) b = 1;
            if (mem > 0 && mem < 1800 || cek <= 2 || gpu > 0 && gpu < 400) b = 0;
            Kilitli = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bsekran") >= 0; if (Kilitli) b = 2;
            k.tavan = b; k.Uygula(b, true); return k;
        }

        void Uygula(int b, bool cozunurluk)
        {
            Basamak = b;
            QualitySettings.antiAliasing = b == 2 ? 4 : b == 1 ? 2 : 0; if (cam != null) cam.allowMSAA = b > 0;
            QualitySettings.shadows = b == 2 ? ShadowQuality.All : b == 1 ? ShadowQuality.HardOnly : ShadowQuality.Disable;
            QualitySettings.shadowDistance = b == 2 ? 40 : 25; QualitySettings.shadowCascades = 1;
            QualitySettings.skinWeights = b == 0 ? SkinWeights.TwoBones : SkinWeights.FourBones;
            if (cozunurluk && !Application.isEditor && natW > 0)
            {
                float s = b == 2 ? 1 : b == 1 ? .85f : .7f;
                Screen.SetResolution(Mathf.RoundToInt(natW * s), Mathf.RoundToInt(natH * s), Screen.fullScreenMode);
            }
        }

        void Update()
        {
            if (Kilitli) return;
            float dt = Time.unscaledDeltaTime; if (dt <= 0 || dt > .5f) return;   // arka plandan dönüş gibi tek uzun kareleri sayma
            top += dt; sayac++; inisBekle -= dt;
            if (top < 3) return;
            float ort = top / sayac; top = 0; sayac = 0;
            if (ort > 1 / 50f && Basamak > 0 && inisBekle <= 0) { Uygula(Basamak - 1, true); inisBekle = 5; iyiSure = 0; return; }
            if (ort < 1 / 58f) { iyiSure += 3; if (iyiSure >= 20 && Basamak < tavan) { Uygula(Basamak + 1, false); iyiSure = 0; } }
            else iyiSure = 0;
        }
    }
}
