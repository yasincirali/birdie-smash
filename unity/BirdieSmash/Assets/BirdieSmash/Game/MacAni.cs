// Maçın anı (stüdyo incelemesi 2026-11-05, oyun tasarımı 3; yasin "hepsini ekle"): maç boyunca son rallinin görünüşü
// (iki figürün konumu, gövde dönüşü, klip ve fazı, topun yeri) 60 Hz'de kaydedilir; her sayı sonunda ralli puanlanır,
// en iyisinin son 5 saniyesi saklanır. Maç sonunda isteğe bağlı "Maçın anı" düğmesi onu oynatır: son 0,9 sn ağır çekim,
// sonra son karede kısa bekleme. Simülasyon yeniden koşulmaz (deterministik tekrar değil, görünüş kaydı); Core'a dokunmaz.
// Puan: vuruş sayısı + kazandığın sayı +6 + smaçla biten +4. Çok kısa (< 0,75 sn) ralli aday olmaz; sayı arası kaydedilmez. Bellek: en çok
// 12 sn × 60 kare × ~120 B ≈ 86 KB; ayırma yalnız sayı sonunda (en iyi değişince tek dizi, CopyTo; GetRange+ToArray iki dizi ayırıyordu).
// 2026-11-06 (yasin: "maçın anı görseli istenen gibi gözükmüyor"; öneri artifact'i YAP): top artık gerçek tüy top görünümüyle
// (ShuttleView, kendi izi) oynar: kayıttaki konumlardan sahte bir Shuttle beslenir, hız kare farkından. Rallinin vuruş sayısı
// ve smaçla bitip bitmediği saklanır (Hud etiketi). Hareketi azaltta ağır çekim yok (YavasK hep 0, kamera yaklaşmaz).
// 2026-11-12 (inceleme 00:05 #7): seçim yalnız ralli uzunluğuna bakıyordu; 13 vuruşluk kaybedilmiş ralli, maçı 2 vuruşta smaçla
// bitiren sayıyı geçiyordu. Bağlam eki: maçı bitiren sayı +10, rakibin maç sayısını kurtaran sayı +6 (bağlam GameRoot'un sayı
// öncesi maç sayısı durumundan, Core'a dokunmaz). Maçı kazandıysan kaybettiğin ralli an olmaz: kazandığın sayılar arasından ayrı
// bir en iyi tutulur, MacBitti(true) onu seçer; yenilgide genel en iyi kalır ("iyi direndin" anı). Bellek en çok iki kayıt (~170 KB).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class MacAni
    {
        public struct Kare { public float zt; public Vector3 mp, op, top; public Quaternion my, oy; public string mk, ok; public float mf, of; public bool topVar; }
        const float ORN = 1 / 60f, YAVAS = .35f, YAVAS_SURE = .9f, BEKLE = .7f; const float SAKLA_SN = 5, EN_AZ_SN = .75f; const int MAX = 60 * 12;
        readonly List<Kare> ralli = new List<Kare>(MAX); float birik, saat, enIyiPuan = -1, t = -1; int oi; Vector3 sonTop; bool sonVar;
        public int Vurus, Baglam; public bool Smac;   // oynatılan rallinin vuruş sayısı, bağlamı (0 yok, 1 maçı bitiren, 2 maç sayısı kurtarma), smaçla mı bitti
        Kare[] bIyi; float bPuan = -1; int bVurus, bBaglam; bool bSmac;   // yalnız kazandığın sayılardan en iyisi
        public Kare[] enIyi;
        public bool Var { get { return enIyi != null && enIyi.Length > 1; } }
        public bool Aktif { get { return t >= 0 && enIyi != null; } }
        public void Sifirla() { ralli.Clear(); enIyi = null; enIyiPuan = -1; birik = 0; saat = 0; t = -1; Vurus = 0; Baglam = 0; Smac = false; bIyi = null; bPuan = -1; }
        public void Kaydet(float dt, PlayerView me, PlayerView op, bool topVar, Vector3 top)
        {
            // Zaman damgalı (inceleme 2026-11-05): 30/60/120 Hz'de oynatma gerçek hızda kalır; 60 Hz'den sık kare atlanır.
            saat += dt; birik += dt; if (birik < ORN) return; birik -= ORN; if (birik > ORN) birik = 0;
            if (ralli.Count >= MAX) ralli.RemoveAt(0);
            ralli.Add(new Kare { zt = saat, mp = me.transform.position, op = op.transform.position, my = me.KayitYaw, oy = op.KayitYaw, mk = me.TaniKlip, ok = op.TaniKlip, mf = me.TaniFaz, of = op.TaniFaz, topVar = topVar, top = top });
        }
        public void SayiBitti(bool benim, int vurus, bool smac, int baglam = 0)
        {
            float p = vurus + (benim ? 6 : 0) + (smac ? 4 : 0) + (baglam == 1 ? 10 : baglam == 2 ? 6 : 0);
            float sure = ralli.Count > 1 ? ralli[ralli.Count - 1].zt - ralli[0].zt : 0;
            bool genel = p > enIyiPuan, ben = benim && p > bPuan;
            if (sure >= EN_AZ_SN && (genel || ben))
            {
                int bas = ralli.Count - 1; float son = ralli[bas].zt; while (bas > 0 && son - ralli[bas - 1].zt <= SAKLA_SN) bas--; int nk = ralli.Count - bas; var a = new Kare[nk]; ralli.CopyTo(bas, a, 0, nk);
                if (genel) { enIyi = a; enIyiPuan = p; Vurus = vurus; Smac = smac; Baglam = baglam; }
                if (ben) { bIyi = a; bPuan = p; bVurus = vurus; bSmac = smac; bBaglam = baglam; }   // aynı dizi paylaşılır, kopya yok
            }
            ralli.Clear(); birik = 0;
        }
        public void MacBitti(bool kazandi) { if (kazandi && bIyi != null) { enIyi = bIyi; enIyiPuan = bPuan; Vurus = bVurus; Smac = bSmac; Baglam = bBaglam; } bIyi = null; }
        public void Baslat() { if (Var) { t = 0; oi = 0; sonVar = false; YavasK = 0; } }
        // Ağır çekim payı (0 → 1, son 0,9 sn'ye girerken .25 sn'de): kamera bitiren ana bu oranla hafifçe yaklaşır.
        public float YavasK { get; private set; }
        public void Bitir() { t = -1; }
        // false: oynatma bitti (çağıran kartı geri açar).
        public bool Oynat(float dt, PlayerView me, PlayerView op, Shuttle top, bool rm, out bool topVar)
        {
            int n = enIyi.Length; float t0 = enIyi[0].zt, son = enIyi[n - 1].zt - t0; topVar = false;
            bool yavas = !rm && t > son - YAVAS_SURE; YavasK = rm ? 0 : Mathf.Clamp01((t - (son - YAVAS_SURE)) / .25f);
            float adim = dt * (yavas ? YAVAS : 1); t += adim; if (t >= son + BEKLE) { t = -1; YavasK = 0; return false; }
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
            // Sahte uçuş: dünya → Core koordinatı (Kit.W'nin tersi); hız kare farkından (ilk karede ya da top yeni belirince sıfır
            // değil, bir sonraki kayıt karesine doğru), saat oynatma adımıyla ilerler ki iz ağır çekimde de aynı yolu çizsin.
            if (top != null && k.topVar)
            {
                Vector3 w = k.top, v;
                if (sonVar && adim > 1e-5f) v = (w - sonTop) / adim;
                else { var b = oi < n - 1 ? enIyi[oi + 1] : k; float dz = Mathf.Max(1e-3f, b.zt - k.zt); v = b.topVar ? (b.top - k.top) / dz : Vector3.zero; }
                top.x = w.x + Court.MID; top.y = Court.NET - w.z; top.z = w.y; top.vx = v.x; top.vy = -v.z; top.vz = v.y;
                top.t += adim; top.dead = false; topVar = true;
            }
            sonVar = k.topVar; sonTop = k.top;
            me.ShowKayit(k.mp, k.my, k.mk, k.mf); op.ShowKayit(k.op, k.oy, k.ok, k.of);
            return true;
        }
    }
}
