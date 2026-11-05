// Maçın anı (stüdyo incelemesi 2026-11-05, oyun tasarımı 3; yasin "hepsini ekle"): maç boyunca son rallinin görünüşü
// (iki figürün konumu, gövde dönüşü, klip ve fazı, topun yeri) 60 Hz'de kaydedilir; her sayı sonunda ralli puanlanır,
// en iyisinin son 5 saniyesi saklanır. Maç sonunda isteğe bağlı "Maçın anı" düğmesi onu oynatır: son 0,9 sn ağır çekim,
// sonra son karede kısa bekleme. Simülasyon yeniden koşulmaz (deterministik tekrar değil, görünüş kaydı); Core'a dokunmaz.
// Puan: vuruş sayısı + kazandığın sayı +6 + smaçla biten +4. Çok kısa (< 0,75 sn) ralli aday olmaz; sayı arası kaydedilmez. Bellek: en çok
// 12 sn × 60 kare × ~120 B ≈ 86 KB; ayırma yalnız sayı sonunda (en iyi değişince bir dizi).
using System.Collections.Generic;
using UnityEngine;

namespace BirdieSmash.Game
{
    public class MacAni
    {
        public struct Kare { public float zt; public Vector3 mp, op, top; public Quaternion my, oy; public string mk, ok; public float mf, of; public bool topVar; }
        const float ORN = 1 / 60f, YAVAS = .35f, YAVAS_SURE = .9f, BEKLE = .7f; const float SAKLA_SN = 5, EN_AZ_SN = .75f; const int MAX = 60 * 12;
        readonly List<Kare> ralli = new List<Kare>(MAX); float birik, saat, enIyiPuan = -1, t = -1; int oi;
        public Kare[] enIyi;
        public bool Var { get { return enIyi != null && enIyi.Length > 1; } }
        public bool Aktif { get { return t >= 0 && enIyi != null; } }
        public void Sifirla() { ralli.Clear(); enIyi = null; enIyiPuan = -1; birik = 0; saat = 0; t = -1; }
        public void Kaydet(float dt, PlayerView me, PlayerView op, bool topVar, Vector3 top)
        {
            // Zaman damgalı (inceleme 2026-11-05): 30/60/120 Hz'de oynatma gerçek hızda kalır; 60 Hz'den sık kare atlanır.
            saat += dt; birik += dt; if (birik < ORN) return; birik -= ORN; if (birik > ORN) birik = 0;
            if (ralli.Count >= MAX) ralli.RemoveAt(0);
            ralli.Add(new Kare { zt = saat, mp = me.transform.position, op = op.transform.position, my = me.KayitYaw, oy = op.KayitYaw, mk = me.TaniKlip, ok = op.TaniKlip, mf = me.TaniFaz, of = op.TaniFaz, topVar = topVar, top = top });
        }
        public void SayiBitti(bool benim, int vurus, bool smac)
        {
            float p = vurus + (benim ? 6 : 0) + (smac ? 4 : 0);
            float sure = ralli.Count > 1 ? ralli[ralli.Count - 1].zt - ralli[0].zt : 0;
            if (sure >= EN_AZ_SN && p > enIyiPuan) { int bas = ralli.Count - 1; float son = ralli[bas].zt; while (bas > 0 && son - ralli[bas - 1].zt <= SAKLA_SN) bas--; enIyi = ralli.GetRange(bas, ralli.Count - bas).ToArray(); enIyiPuan = p; }
            ralli.Clear(); birik = 0;
        }
        public void Baslat() { if (Var) { t = 0; oi = 0; } }
        public void Bitir() { t = -1; }
        // false: oynatma bitti (çağıran kartı geri açar).
        public bool Oynat(float dt, PlayerView me, PlayerView op, Transform top)
        {
            int n = enIyi.Length; float t0 = enIyi[0].zt, son = enIyi[n - 1].zt - t0;
            t += dt * (t > son - YAVAS_SURE ? YAVAS : 1); if (t >= son + BEKLE) { t = -1; if (top != null) top.gameObject.SetActive(false); return false; }
            while (oi < n - 1 && enIyi[oi + 1].zt - t0 <= t) oi++;
            var k = enIyi[oi];
            me.ShowKayit(k.mp, k.my, k.mk, k.mf); op.ShowKayit(k.op, k.oy, k.ok, k.of);
            if (top != null) { top.gameObject.SetActive(k.topVar); if (k.topVar) top.position = k.top; }
            return true;
        }
    }
}
