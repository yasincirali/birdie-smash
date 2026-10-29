// Core'un kare başı GC ayırması (2026-10-27 performans turu 2, yasin: "Texturelar ve animasyonlar her yerde smooth ve
// performanslı akıcı olmalı"). Bot maçı (BotMac'in botu) 60 ve 120 Hz'de oynanır; her karede Match.Update + iki PickClip
// (PlayerView'in her karede çağırdığı) arasında ayrılan bayt GC.GetAllocatedBytesForCurrentThread ile ölçülür.
// Ölçüt: karelerin yarısından fazlası ayırmasız (p50 = 0). Vuruş/sayı karelerinde (yol tahmini, olay, yazı) ayırma
// beklenir, sınır konmaz; satır p95/p99'u yazar. Önce: p50 96 B (Update'te iki foreach (new[] { me, op })), sonra 0.
// Bilinen kalan: rakibin topu pencere içindeyken HileFirsat her karede HileDenetim nesneleri kurar (~288 B, karelerin ~%6'sı).
// Çalıştırma (depo kökünden, veri/*.json okunur):
//   mcs -out:$S/ayirma.exe tool/unity_test/AyirmaOlc.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono $S/ayirma.exe
using System;
using System.Collections.Generic;
using BirdieSmash.Core;

static class AyirmaOlc
{
    static readonly Random rng = new Random(7);
    static void Bot(Match M)
    {
        if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .05) M.SwipeServe((float)(rng.NextDouble() * 80 - 40), -60, .6f);
        Reach R;
        if (M.ReachInfo(out R) && rng.NextDouble() < .12)
        {
            double k = rng.NextDouble(); float sp = k < .2 ? .5f : k < .65 ? 1.2f : 2.2f;
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * 60), side = rng.NextDouble() < .5 ? -1 : 1 });
        }
    }
    static int Main()
    {
        int hata = 0;
        foreach (var hz in new[] { 60, 120 })
        {
            float dt = 1f / hz; var M = new Match(5, new MatchOptions { easy = true, target = 21 }); var per = new List<long>();
            for (int i = 0; i < hz * 600 && !M.over; i++)
            {
                Bot(M); long b0 = GC.GetAllocatedBytesForCurrentThread();
                M.Update(dt); string c; float ph; M.PickClip(M.me, true, out c, out ph); M.PickClip(M.op, false, out c, out ph);
                per.Add(GC.GetAllocatedBytesForCurrentThread() - b0); M.events.Clear();
            }
            per.Sort(); long top = 0; int z = 0; foreach (var p in per) { top += p; if (p == 0) z++; }
            long p50 = per[per.Count / 2];
            Console.WriteLine(hz + " Hz: " + per.Count + " kare · kare başı ort " + (top / per.Count) + " B · p50 " + p50 + " p95 " + per[(int)(per.Count * .95)] + " p99 " + per[(int)(per.Count * .99)] + " · ayırmasız %" + (100 * z / per.Count));
            if (p50 != 0) { Console.WriteLine("HATA: " + hz + " Hz'de karelerin yarısı ayırıyor (p50 " + p50 + " B)"); hata++; }
        }
        Console.WriteLine(hata == 0 ? "TAMAM" : "HATA " + hata); return hata == 0 ? 0 : 1;
    }
}
