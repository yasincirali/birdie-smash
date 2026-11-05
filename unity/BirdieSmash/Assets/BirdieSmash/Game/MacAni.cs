// Maçın anı (stüdyo incelemesi 2026-11-05, oyun tasarımı 3; yasin "hepsini ekle"): maç boyunca son rallinin görünüşü
// (iki figürün konumu, gövde dönüşü, klip ve fazı, topun yeri) 60 Hz'de kaydedilir; her sayı sonunda ralli puanlanır,
// en iyisinin son 5 saniyesi saklanır. Maç sonunda isteğe bağlı "Maçın anı" düğmesi onu oynatır: son 0,9 sn ağır çekim,
// sonra son karede kısa bekleme. Simülasyon yeniden koşulmaz (deterministik tekrar değil, görünüş kaydı); Core'a dokunmaz.
// Puan: vuruş sayısı + kazandığın sayı +6 + smaçla biten +4. Çok kısa (< 0,75 sn) ralli aday olmaz; sayı arası kaydedilmez. Bellek: en çok
// 12 sn × 60 kare × ~120 B ≈ 86 KB; ayırma yalnız sayı sonunda (en iyi değişince tek dizi, CopyTo; GetRange+ToArray iki dizi ayırıyordu).
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
            if (sure >= EN_AZ_SN && p > enIyiPuan) { int bas = ralli.Count - 1; float son = ralli[bas].zt; while (bas > 0 && son - ralli[bas - 1].zt <= SAKLA_SN) bas--; int nk = ralli.Count - bas; var a = new Kare[nk]; ralli.CopyTo(bas, a, 0, nk); enIyi = a; enIyiPuan = p; }
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
            // Kareler arası ara değer (performans incelemesi 2026-11-05): 0,35x ağır çekimde her kayıt karesi ~3 (120 Hz'de ~6) ekran
            // karesi donuk kalıyor, figürler basamaklı görünüyordu. Konum/dönüş Lerp/Slerp; faz yalnız aynı klipte ve sarmadan.
            var k = enIyi[oi];
            if (oi < n - 1)
            {
                var b = enIyi[oi + 1]; float dz = b.zt - k.zt, a = dz > 1e-5f ? Mathf.Clamp01((t + t0 - k.zt) / dz) : 0;
                k.mp = Vector3.Lerp(k.mp, b.mp, a); k.op = Vector3.Lerp(k.op, b.op, a);
                k.my = Quaternion.Slerp(k.my, b.my, a); k.oy = Quaternion.Slerp(k.oy, b.oy, a);
                if (k.mk == b.mk && Mathf.Abs(b.mf - k.mf) < .5f) k.mf = Mathf.Lerp(k.mf, b.mf, a);
                if (k.ok == b.ok && Mathf.Abs(b.of - k.of) < .5f) k.of = Mathf.Lerp(k.of, b.of, a);
                if (k.topVar && b.topVar) k.top = Vector3.Lerp(k.top, b.top, a);
            }
            me.ShowKayit(k.mp, k.my, k.mk, k.mf); op.ShowKayit(k.op, k.oy, k.ok, k.of);
            if (top != null) { top.gameObject.SetActive(k.topVar); if (k.topVar) top.position = k.top; }
            return true;
        }
    }
}
