// Kare süresi ve GC ölçümü (2026-10-27 performans turu 2, yasin: "Texturelar ve animasyonlar her yerde smooth ve performanslı
// akıcı olmalı"). Yalnız ekran turu (-bsekran) kullanır; mağaza build'inde hiç kurulmaz. Bir bölüm (menü, perde, ralli,
// açılış) Basla() ile Bitir() arasındaki her kareyi kaydeder: kare süresi (gerçek zaman), kare başı GC ayırması ve GC
// toplama sayısı. Sonuç rapor.txt'de tek satır ("perf <bölüm> · ...").
// CI'daki Linux oyuncusu yazılım GL'de (llvmpipe) çizer: mutlak ms anlamsız, yalnız bölümler arası ve turlar arası GÖRELİ
// karşılaştırma içindir (ör. ralli p99 / p50 oranı takılmayı, GC kare başı bayt ayırmanın kendisini gösterir; GC sayısı
// GPU'dan bağımsızdır, cihazda da aynıdır).
// GC kaynağı: ProfilerRecorder "GC Allocated In Frame" (geçerliyse; Unity bunu yalnız geliştirme oyuncusunda doldurabilir),
// yoksa Profiler.GetMonoUsedSizeLong farkı (toplama olmayan karelerde ayrılan bayt; toplama karesi sayılmaz). Satır hangisinin
// kullanıldığını yazar. Ölçerin kendisi kayıt sırasında ayırma yapmaz (diziler önceden ayrılır).
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

namespace BirdieSmash.Game
{
    [DefaultExecutionOrder(-200)]
    public class KareOlcum : MonoBehaviour
    {
        const int AZAMI = 4096;
        readonly float[] dt = new float[AZAMI]; readonly long[] gc = new long[AZAMI]; int n; bool acik, ilk;
        long sonMono; int sonToplama, toplama0; ProfilerRecorder gcKayit; bool kayitVar;

        void OnEnable()
        {
            try { gcKayit = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"); } catch (System.Exception) { }
        }
        void OnDisable() { if (gcKayit.Valid) gcKayit.Dispose(); }

        public void Basla() { n = 0; acik = true; ilk = true; toplama0 = System.GC.CollectionCount(0); sonToplama = toplama0; sonMono = Profiler.GetMonoUsedSizeLong(); }
        public int Kare { get { return n; } }

        void Update()
        {
            if (!acik) return;
            // İlk kare: Basla'yı çağıran karenin süresi bölüme ait değil.
            long mono = Profiler.GetMonoUsedSizeLong(); int top = System.GC.CollectionCount(0);
            if (ilk) { ilk = false; sonMono = mono; sonToplama = top; return; }
            if (n >= AZAMI) return;
            kayitVar = gcKayit.Valid && gcKayit.LastValue > 0 || kayitVar;
            long g = gcKayit.Valid && kayitVar ? gcKayit.LastValue : (top == sonToplama ? System.Math.Max(0, mono - sonMono) : -1);   // -1: toplama karesi (bilinmiyor)
            dt[n] = Time.unscaledDeltaTime * 1000; gc[n] = g; n++;
            sonMono = mono; sonToplama = top;
        }

        public string Bitir(string bolum)
        {
            acik = false; int k = n; if (k == 0) return "perf " + bolum + " · kare yok";
            var s = new float[k]; System.Array.Copy(dt, s, k); System.Array.Sort(s);
            long topG = 0; int gN = 0, sifir = 0; var gs = new long[k]; int gk = 0;
            for (int i = 0; i < k; i++) { if (gc[i] < 0) continue; topG += gc[i]; gN++; if (gc[i] == 0) sifir++; gs[gk++] = gc[i]; }
            System.Array.Sort(gs, 0, gk);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            System.Func<float, string> F = v => v.ToString("0.0", ci);
            float ort = 0; for (int i = 0; i < k; i++) ort += s[i]; ort /= k;
            int takilma = 0; float med = Yuzde(s, k, .5f); for (int i = 0; i < k; i++) if (s[i] > med * 2) takilma++;
            return "perf " + bolum + " · kare " + k + " · dt ms p50 " + F(med) + " p95 " + F(Yuzde(s, k, .95f)) + " p99 " + F(Yuzde(s, k, .99f)) + " en " + F(s[k - 1]) + " ort " + F(ort)
                + " · p99/p50 " + (Yuzde(s, k, .99f) / Mathf.Max(.01f, med)).ToString("0.00", ci) + " · takılma (>2×p50) " + takilma
                + " · GC kare başı ort " + (gN > 0 ? (topG / gN).ToString() : "-") + " B p95 " + (gk > 0 ? gs[Mathf.Min(gk - 1, (int)(gk * .95f))].ToString() : "-") + " B · ayırmasız kare %" + (gN > 0 ? (100f * sifir / gN).ToString("0", ci) : "-")
                + " · GC toplama " + (System.GC.CollectionCount(0) - toplama0) + " · kaynak " + (kayitVar ? "ProfilerRecorder" : "mono farkı") + " · " + Kalite.Tani + " · GÖRELİ (yazılım GL)";
        }
        static float Yuzde(float[] s, int k, float p) { return s[Mathf.Clamp(Mathf.CeilToInt(p * k) - 1, 0, k - 1)]; }
    }
}
