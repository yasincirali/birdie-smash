// Raket çizimi ölçümü (2026-11-26, raket görseli gerçekçi): Game/RaketCizim.cs'i Unity'siz derler, sekme simgesini ve üç kafa
// şeklinin vitrin dokusunu çizer, kenar kırpması ve ortalama denetler, RGBA'yı /tmp/raket_*.rgba'ya döker (önizleme:
// python3 ile PNG'ye çevir). RaketSekil.Egri'yi kaynaktan kopyalar ki iki eğri ayrışmasın. Depo kökünden:
//   awk '/public static List<Vector2> Egri/{f=1} f{print} f&&/^        }$/{exit}' unity/BirdieSmash/Assets/BirdieSmash/Game/RaketSekil.cs > /tmp/egri.cs.txt
//   sed -e '/^        \/\/ EGRI$/r /tmp/egri.cs.txt' tool/unity_test/RaketCizimOlc.cs > /tmp/RaketCizimOlc.cs
//   mcs -out:/tmp/raketolc.exe /tmp/RaketCizimOlc.cs unity/BirdieSmash/Assets/BirdieSmash/Game/RaketCizim.cs && mono /tmp/raketolc.exe
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector2 { public float x, y; public Vector2(float a, float b) { x = a; y = b; } }
    public struct Color32 { public byte r, g, b, a; public Color32(byte R, byte G, byte B, byte A) { r = R; g = G; b = B; a = A; } }
    public struct Color
    {
        public float r, g, b, a; public Color(float R, float G, float B, float A = 1) { r = R; g = G; b = B; a = A; }
        public static Color white { get { return new Color(1, 1, 1); } }
        public static Color black { get { return new Color(0, 0, 0); } }
        public static Color operator *(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a * k); }
        public static Color Lerp(Color x, Color y, float t) { return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t); }
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Sin(float x) { return (float)Math.Sin(x); }
        public static float Cos(float x) { return (float)Math.Cos(x); }
        public static float Sqrt(float x) { return (float)Math.Sqrt(x); }
        public static float Abs(float x) { return Math.Abs(x); }
        public static float Pow(float x, float y) { return (float)Math.Pow(x, y); }
        public static float Sign(float x) { return x >= 0 ? 1 : -1; }
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static float Clamp(float x, float a, float b) { return x < a ? a : x > b ? b : x; }
        public static float Clamp01(float x) { return Clamp(x, 0, 1); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float Repeat(float t, float l) { return Clamp(t - (float)Math.Floor(t / l) * l, 0, l); }
        public static float SmoothStep(float from, float to, float t) { t = Clamp01(t); t = -2 * t * t * t + 3 * t * t; return to * t + from * (1 - t); }
    }
}
namespace BirdieSmash.Game
{
    public static class RaketSekil
    {
        // EGRI
    }
}
static class RaketCizimOlc
{
    static int hata;
    static void Kontrol(bool ok, string ne) { Console.WriteLine((ok ? "  ok  " : "  HATA ") + ne); if (!ok) hata++; }
    static void Dok(string ad, Color32[] px, int n)
    {
        var b = new byte[n * n * 4];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { var c = px[(n - 1 - y) * n + x]; int i = (y * n + x) * 4; b[i] = c.r; b[i + 1] = c.g; b[i + 2] = c.b; b[i + 3] = c.a; }
        File.WriteAllBytes("/tmp/raket_" + ad + ".rgba", b);
    }
    static void Kutu(Color32[] px, int n, out int x0, out int x1, out int y0, out int y1)
    {
        x0 = n; x1 = -1; y0 = n; y1 = -1;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) if (px[y * n + x].a > 24) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
    }
    static int Main()
    {
        // Sekme simgesi: Ikonlar.Ciz gibi 96 px, 2×2 örnek, W = .075
        const int N = 96; var sk = new Color32[N * N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                int say = 0;
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++) { float fx = (x + .25f + .5f * sx) / N * 2 - 1, fy = (y + .25f + .5f * sy) / N * 2 - 1; if (BirdieSmash.Game.RaketCizim.SekmeIc(fx, fy, .075f)) say++; }
                sk[y * N + x] = new Color32(255, 255, 255, (byte)(say * 255 / 4));
            }
        Dok("sekme", sk, N); int a0, a1, b0, b1; Kutu(sk, N, out a0, out a1, out b0, out b1);
        Console.WriteLine("sekme kutusu x " + a0 + "-" + a1 + " y " + b0 + "-" + b1);
        Kontrol(a0 > 1 && b0 > 1 && a1 < N - 2 && b1 < N - 2, "sekme simgesi çerçeveye değmiyor");
        Kontrol(Math.Abs((a0 + a1) / 2f - N / 2f) < 6 && Math.Abs((b0 + b1) / 2f - N / 2f) < 6, "sekme simgesi ortada (±6 px; düğmede kaymış görünmesin)");
        var r = new Color(.89f, .23f, .23f); var w = new Color(.95f, .95f, .93f); var k = new Color(.15f, .15f, .17f);
        foreach (var kafa in new[] { "oval", "izometrik", "kalp" })
        {
            const int V = 256; var sw = System.Diagnostics.Stopwatch.StartNew();
            var px = BirdieSmash.Game.RaketCizim.Vitrin(V, kafa, r, w, w, BirdieSmash.Game.RaketCizim.SAFT_VARSAYILAN, k);
            sw.Stop(); Dok(kafa, px, V); Kutu(px, V, out a0, out a1, out b0, out b1);
            Console.WriteLine(kafa + ": " + sw.ElapsedMilliseconds + " ms (mono, masaüstü), kutu x " + a0 + "-" + a1 + " y " + b0 + "-" + b1);
            Kontrol(a0 > 0 && b0 > 0 && a1 < V - 1 && b1 < V - 1, kafa + " vitrin dokusu kenara değmiyor (kırpılmıyor)");
        }
        Console.WriteLine(hata == 0 ? "TAMAM" : hata + " HATA"); return hata == 0 ? 0 : 1;
    }
}
