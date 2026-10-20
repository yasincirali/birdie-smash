// Ses: hepsi açılışta sentezlenir (dosya yok). HTML sürümündeki fikir korunur: vuruş türü sesi
// değiştirir (smaç çatlar, clear çınlar, drop yumuşak), ralli uzadıkça pentatonik nota yükselir,
// file "puf" diye yutar, sayı arpejle kutlanır.
using UnityEngine;

namespace BirdieSmash.Game
{
    public class Sfx : MonoBehaviour
    {
        const int SR = 44100; AudioSource src; System.Random rnd = new System.Random(3);
        AudioClip hitSmash, hitClear, hitDrive, hitDrop, net, floorPat, win, lose, whistle; AudioClip[] notes;
        static readonly int[] PENTA = { 0, 2, 4, 7, 9 };

        public static Sfx Build(Transform root) { var g = new GameObject("ses"); g.transform.SetParent(root, false); var s = g.AddComponent<Sfx>(); s.Init(); return s; }

        void Init()
        {
            src = gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
            hitSmash = Hit(.12f, 1500, 1.0f, .9f); hitClear = Hit(.28f, 980, .55f, .35f); hitDrive = Hit(.16f, 1200, .75f, .6f); hitDrop = Hit(.1f, 760, .35f, .15f);
            net = Noise(.16f, .08f, .5f, 2.2f); floorPat = Noise(.07f, .12f, .6f, 6f);
            notes = new AudioClip[15];
            for (int i = 0; i < 15; i++) { int semi = (i / 5) * 12 + PENTA[i % 5]; notes[i] = Tone(392f * Mathf.Pow(2, semi / 12f), .22f, .35f); }
            win = Arp(new[] { 0, 4, 7, 12 }, 523f); lose = Tone(220, .3f, .3f, true); whistle = Tone(2100, .12f, .18f);
        }

        AudioClip Make(string n, float[] d) { var c = AudioClip.Create(n, d.Length, 1, SR, false); c.SetData(d, 0); return c; }
        // Raket: kısa gürültü patlaması (tel) + tını (çerçeve); crack: yüksek geçiren tık.
        AudioClip Hit(float dur, float f, float ring, float crack)
        {
            int n = (int)(SR * dur); var d = new float[n]; float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, w = (float)(rnd.NextDouble() * 2 - 1);
                lp += (w - lp) * .35f; float hp = w - lp;
                d[i] = hp * crack * Mathf.Exp(-t * 90) * .8f + Mathf.Sin(2 * Mathf.PI * f * t) * ring * Mathf.Exp(-t * 26) * .35f + Mathf.Sin(2 * Mathf.PI * f * 2.7f * t) * ring * Mathf.Exp(-t * 40) * .12f;
            }
            return Make("vurus", d);
        }
        AudioClip Noise(float dur, float lpk, float vol, float decay)
        {
            int n = (int)(SR * dur); var d = new float[n]; float lp = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)SR; lp += ((float)(rnd.NextDouble() * 2 - 1) - lp) * lpk; d[i] = lp * vol * 3 * Mathf.Exp(-t * decay * 10); }
            return Make("gurultu", d);
        }
        AudioClip Tone(float f, float dur, float vol, bool saw = false)
        {
            int n = (int)(SR * dur); var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR, ph = f * t % 1f; float w = saw ? ph * 2 - 1 : Mathf.Sin(2 * Mathf.PI * ph); d[i] = w * vol * Mathf.Min(1, t * 200) * Mathf.Exp(-t * 9); }
            return Make("ton", d);
        }
        AudioClip Arp(int[] semis, float f0)
        {
            int step = (int)(SR * .07f), n = step * semis.Length + (int)(SR * .25f); var d = new float[n];
            for (int k = 0; k < semis.Length; k++) { float f = f0 * Mathf.Pow(2, semis[k] / 12f); for (int i = 0; i < SR * .25f && k * step + i < n; i++) { float t = i / (float)SR; float ph = f * t % 1f; d[k * step + i] += (ph < .5f ? .25f : -.25f) * Mathf.Exp(-t * 10) * .6f; } }
            return Make("arp", d);
        }

        public void Hit(string type, int rally, bool mine)
        {
            var c = type == "smash" ? hitSmash : type == "clear" ? hitClear : type == "drive" ? hitDrive : hitDrop;
            src.PlayOneShot(c, mine ? 1 : .7f);
            src.PlayOneShot(notes[Mathf.Clamp(rally - 1, 0, 14)], mine ? .35f : .18f);
            if (type == "smash") src.PlayOneShot(whistle, .25f);
        }
        public void Net(float k) { src.PlayOneShot(net, .4f + .6f * k); }
        public void Floor(bool big) { src.PlayOneShot(floorPat, big ? 1 : .5f); }
        public void Point(bool mine) { src.PlayOneShot(mine ? win : lose, .8f); }
    }
}
