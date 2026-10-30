// Ses: HTML'deki Web Audio sentezinin C# karşılığı. Dosya yok; her ses kodla üretilir. Web Audio düğümleri
// (osilatör, gürültü tamponu, BiquadFilter, üstel zarf) burada çevrimdışı örnek örnek hesaplanır (Synth), sonuç
// AudioClip olarak önbelleğe girer (HTML VSES: vuruş sesi her bileşimde bir kez, iki farklı rastgele çekimle kaydedilir).
// Raket sesi (HTML vurusCal), üç katman:
//   (1) tel modları: f0 = 1480·ton (±%2,5), kısmiler ×[1, 1,63, 2,41, 3,3] kazanç [.24 .13 .08 .045], sönüm (.1 − i·.018)·ring/ton,
//       1,045 kat yukarıdan 12 ms'de iner; tel ani patlaması highpass 3200·ton; çatırtı bandpass 4300·√ton;
//   (2) kafa/çerçeve: üçgen 235·kafa + lowpass gürültü 360·kafa; sıyırmada kare dalga 640·kafa "klonk";
//   (3) mantar tıkırtısı bandpass 1300, tüy hışırtısı bandpass 6200; slice'ta 2200→5200·ton "zzip" + 4 kanat çırpışı;
//   salon yansıması (55 ms) ve lowpass 1700 oda kuyruğu. Tür karışımı HITMIX, isabet ve slice çarpanları HTML ile aynı.
//   Rakip: ×0,5, lowpass 3 kHz, 30 ms geç. Tel gerginliği (ton = √(T/26)) ve raket kafası (kafa) ekipmandan gelir.
// Diğerleri HTML ile aynı: ralli melodisi (beşli dizi, ben 392 Hz / rakip 196 Hz, akışta armoni), ralli ≥ 6'da davul, smaçta
// düdük, seyirci uğultusu (bandpass 520 → lowpass 1400, düzey .01 + min(.06, ralli·.0045)), vuruş penceresinde "su altı"
// (tüm ses lowpass 750 Hz: AudioLowPassFilter dinleyicide), file "puf", bant tınısı, yer pıtırtısı, çizgi kamerası
// davulu, sevinç/iç çekiş, yön değişiminde ayakkabı gıcırtısı, parmak hızıyla raket uğultusu (canlı bandpass).
// Web Audio'daki son kompresör yok (Unity mikserinde yok; düzeyler HTML'deki kazançlarla).
// Titreşim: desen burada hesaplanır (Desen, HTML hapHit), Yerel.Vurus iOS eklentisiyle çalar (Plugins/iOS/BirdieYerel.mm).
// 2026-10-27 his turu (yasin: "sesle ilgili sen kendin tatmin edici şekilde ilerle, ben gerçekçi sesler eklerim ilerde"):
//  - Kayıtlı ses kancası: Assets/Resources/ses/<ad>.wav|ogg|mp3 varsa o ad için sentez yerine kayıt çalar, kod değişmez.
//    Aynı adın çeşitlemeleri <ad>_1, <ad>_2 … (sırayla, ilk eksikte durur) rastgele seçilir. Adlar KAYIT_ADLARI'nda; vuruşta
//    önce isabetli ad (vurus_smash_perfect), yoksa türün adı (vurus_smash). Rakibin vuruşu aynı kaydı kısık ve 3 kHz
//    süzgeçli kaynaktan çalar (sentezdeki uzaklık kuralı). Kayıt yoksa ses birebir eski sentezdir. Lisans: yalnız CC0 ya
//    da kendi kaydımız, bildirimi legal/ucuncu_taraf.md (repo kuralı).
//  - Vuruşun gücü sese biner: topun çıkış hızı (m/s) kazancı ve perdeyi hafifçe değiştirir (yavaş drop kısık ve bir tık
//    tiz, 60 m/s smaç dolu ve tok); smaç/file vuruşu/sıçrayarak smaçta altta kısa bir gövde darbesi (80 → 45 Hz, 70 ms):
//    telefon hoparlöründe duyulmaz ama kulaklıkta "punch" verir, iOS hoparlöründe tiz katman zaten taşıyor.
//  - Seyirci ralliye tepki verir: 8, 12, 16 … vuruşta yükselen "ooo" (gerilim), 10+ vuruşluk ralliden sonra alkış
//    (ralli uzadıkça uzun ve gür); kısa ralli eski sevinç sesiyle kalır (her sayıda alkış ucuzlar).
// 2026-10-30 menü müziği (Ar-Ge 30 Eki #1, yasin "1-2 yap", aday "B"): MenuMuzik aşağıda. Yalnız menüde; maçta müzik yok
// (perde kapanırken söner, seyirci yükselir). Ayrı "Müzik" çipi (tuytop_muzik, varsayılan açık); başka uygulama ses
// çalıyorsa başlamaz (iOS secondaryAudioShouldBeSilencedHint). Hareketi azalt müziği etkilemez (hareket değil ses).
using System.Collections.Generic;
using UnityEngine;

namespace BirdieSmash.Game
{
    // Çevrimdışı mini Web Audio: her katman kendi süzgecinden geçip ortak tampona eklenir.
    public class Synth
    {
        public const int SR = 44100; public readonly float[] buf; readonly System.Random rnd;
        public Synth(float sec, int seed) { buf = new float[Mathf.CeilToInt(SR * sec)]; rnd = new System.Random(seed); }
        public float R() { return (float)(rnd.NextDouble() * 2 - 1); }

        public class Biquad
        {
            float b0, b1, b2, a1, a2, x1, x2, y1, y2;
            public Biquad(string type, float f, float q) { Set(type, f, q); }
            // RBJ ses kitabı (Web Audio BiquadFilterNode aynı formüller; lowpass/highpass Q dB değil doğrusal kabul edildi)
            public void Set(string type, float f, float q)
            {
                f = Mathf.Clamp(f, 10, SR * .49f); float w = 2 * Mathf.PI * f / SR, cs = Mathf.Cos(w), sn = Mathf.Sin(w), al = sn / (2 * Mathf.Max(.0001f, q)), a0;
                if (type == "lowpass") { b0 = (1 - cs) / 2; b1 = 1 - cs; b2 = b0; }
                else if (type == "highpass") { b0 = (1 + cs) / 2; b1 = -(1 + cs); b2 = b0; }
                else { b0 = al; b1 = 0; b2 = -al; }   // bandpass, sabit 0 dB tepe
                a0 = 1 + al; a1 = -2 * cs; a2 = 1 - al; b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;
            }
            public float Run(float x) { float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = x; y2 = y1; y1 = y; return y; }
        }
        static float Wave(string type, float ph)
        {
            ph -= Mathf.Floor(ph);
            switch (type) { case "triangle": return 1 - 4 * Mathf.Abs(ph - .5f); case "square": return ph < .5f ? 1 : -1; case "sawtooth": return 2 * ph - 1; default: return Mathf.Sin(2 * Mathf.PI * ph); }
        }
        // HTML decay(): frekans f·glide'dan 12 ms'de f'e üstel iner; kazanç 1,5 ms'de v'ye çıkar, d'de .0001'e üstel iner.
        public void Decay(float t, float f, string type, float v, float d, float glide = 1, Biquad flt = null)
        {
            int s0 = (int)(t * SR), n = (int)((d + .0015f + .03f) * SR); float ph = 0;
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                float tt = i / (float)SR; float ff = glide != 1 && tt < .012f ? f * Mathf.Pow(glide, 1 - tt / .012f) : f; ph += ff / SR;
                float g = tt < .0015f ? v * tt / .0015f : tt < .0015f + d ? v * Mathf.Pow(.0001f / v, (tt - .0015f) / d) : 0;
                float x = Wave(type, ph) * g; if (flt != null) x = flt.Run(x); if (s0 + i >= 0) buf[s0 + i] += x;
            }
        }
        // HTML tone(): kazanç v'den d sonunda .0001'e üstel.
        public void Tone(float t, float f, float d, string type, float v, Biquad flt = null)
        {
            int s0 = (int)(t * SR), n = (int)((d + .02f) * SR); float ph = 0;
            for (int i = 0; i < n && s0 + i < buf.Length; i++) { float tt = i / (float)SR; ph += f / SR; float g = tt < d ? v * Mathf.Pow(.0001f / v, tt / d) : 0; float x = Wave(type, ph) * g; if (flt != null) x = flt.Run(x); buf[s0 + i] += x; }
        }
        // Frekansı değişen ton (düdük, gıcırtı, davul): fn(tt) Hz, gn(tt) kazanç.
        public void Sweep(float t, float d, string type, System.Func<float, float> fn, System.Func<float, float> gn)
        {
            int s0 = (int)(t * SR), n = (int)(d * SR); float ph = 0;
            for (int i = 0; i < n && s0 + i < buf.Length; i++) { float tt = i / (float)SR; ph += fn(tt) / SR; buf[s0 + i] += Wave(type, ph) * gn(tt); }
        }
        // HTML burstN(): zarf (1 − i/n)^env gürültü, süzgeç, kazanç.
        public void Burst(float t, float d, string type, float f, float v, float q = 1, float env = 3)
        {
            int s0 = (int)(t * SR), n = Mathf.Max(8, (int)(d * SR)); var b = new Biquad(type, f, q);
            for (int i = 0; i < n + SR / 50 && s0 + i < buf.Length; i++) { float x = i < n ? R() * Mathf.Pow(1 - i / (float)n, env) : 0; buf[s0 + i] += b.Run(x) * v; }
        }
        // Sinüs zarflı, süzgeç frekansı süpürülen gürültü (slice "zzip", iç çekiş).
        public void NoiseSweep(float t, float d, float f0, float f1, float q, float v, bool expo, System.Func<float, float> zarf)
        {
            int s0 = (int)(t * SR), n = (int)(d * SR); var b = new Biquad("bandpass", f0, q);
            for (int i = 0; i < n && s0 + i < buf.Length; i++) { float u = i / (float)n; if (i % 32 == 0) b.Set("bandpass", expo ? f0 * Mathf.Pow(f1 / f0, u) : Mathf.Lerp(f0, f1, u), q); buf[s0 + i] += b.Run(R()) * v * zarf(u); }
        }
        public AudioClip Clip(string ad, float kazanc = 1, float gecikme = 0, float lowpass = 0)
        {
            int off = (int)(gecikme * SR); var d = new float[buf.Length + off]; Biquad lp = lowpass > 0 ? new Biquad("lowpass", lowpass, .707f) : null;
            for (int i = 0; i < buf.Length; i++) { float x = buf[i] * kazanc; if (lp != null) x = lp.Run(x); d[i + off] = Mathf.Clamp(x, -1, 1); }
            var c = AudioClip.Create(ad, d.Length, 1, SR, false); c.SetData(d, 0); return c;
        }
    }

    public class Sfx : MonoBehaviour
    {
        AudioSource src, opSrc; readonly AudioSource[] vurusSrc = new AudioSource[4]; int vurusSira; AudioLowPassFilter suAlti; float suHedef = 22000; System.Random rnd = new System.Random(5); int seed = 11;
        AudioSource crowd; float crowdHedef; Swoosh swo;
        // Menü müziği: döngü ilk açılışta arka iş parçacığında sentezlenir (≈1,1 M örnek), clip ana iş parçacığında kurulur.
        // menude GameRoot'tan: menüdeyken ve perde menüyü örtmüyorken. Düzey .25: döngü .8 tepeye normalize, menü tık
        // sesleri (.04–.06) üstünde duyulsun diye müzik tepesi ≈.2'de kalır.
        public System.Func<bool> menude; AudioSource muzik; float[] muzikVeri; volatile bool muzikHazir; float muzikDuzey, digerT; bool muzikCalar, digerCaliyor;
        const float MUZIK_DUZEY = .25f;
        readonly Dictionary<string, AudioClip[]> vses = new Dictionary<string, AudioClip[]>(); readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly int[] PENTA = { 0, 2, 4, 7, 9 };
        class Mix { public float a, str, frm, crk, ring, hiss; }
        static readonly Dictionary<string, Mix> HITMIX = new Dictionary<string, Mix> {
            {"smash",new Mix{a=1,str=1,frm=.9f,crk=1,ring=.55f,hiss=1}},{"clear",new Mix{a=.72f,str=1,frm=.55f,crk=.25f,ring=1,hiss=.5f}},
            {"drive",new Mix{a=.8f,str=.9f,frm=.7f,crk=.55f,ring=.5f,hiss=.6f}},{"drop",new Mix{a=.34f,str=.8f,frm=.12f,crk=0,ring=.9f,hiss=0}}};

        public static Sfx Build(Transform root, Transform listener)
        {
            var g = new GameObject("ses"); g.transform.SetParent(root, false); var s = g.AddComponent<Sfx>(); s.Init(listener); return s;
        }
        void Init(Transform listener)
        {
            src = gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
            // Vuruşlar perdeli çaldığı için ayrı kaynaklardan (PlayOneShot perde almaz); 4 kaynak üst üste binen vuruşa yeter.
            for (int i = 0; i < vurusSrc.Length; i++) { vurusSrc[i] = gameObject.AddComponent<AudioSource>(); vurusSrc[i].playOnAwake = false; vurusSrc[i].spatialBlend = 0; }
            // Rakibin kayıtlı vuruşu: ayrı nesnede 3 kHz alçak geçiren (AudioLowPassFilter nesnedeki tüm kaynaklara uygulanır).
            var og = new GameObject("rakip_ses"); og.transform.SetParent(transform, false); opSrc = og.AddComponent<AudioSource>(); opSrc.playOnAwake = false; opSrc.spatialBlend = 0;
            var olp = og.AddComponent<AudioLowPassFilter>(); olp.cutoffFrequency = 3000;
            suAlti = listener.gameObject.AddComponent<AudioLowPassFilter>(); suAlti.cutoffFrequency = 22000; suAlti.lowpassResonanceQ = 1;
            // seyirci: 2 sn döngü gürültü, bandpass 520 (Q .8) → lowpass 1400
            var sy = new Synth(2, 3); var bp = new Synth.Biquad("bandpass", 520, .8f); var lp = new Synth.Biquad("lowpass", 1400, .707f);
            for (int i = 0; i < sy.buf.Length; i++) sy.buf[i] = lp.Run(bp.Run(sy.R()));
            Yumusat(sy.buf, 2000);
            var ks = Kayit("seyirci");
            crowd = gameObject.AddComponent<AudioSource>(); crowd.clip = ks != null ? ks[0] : sy.Clip("seyirci", 3); crowd.loop = true; crowd.volume = 0; crowd.playOnAwake = false; crowd.Play();
            var sg = new GameObject("ugultu"); sg.transform.SetParent(transform, false); swo = sg.AddComponent<Swoosh>();
            Isit();   // açılış perdesi bu sıra bitene kadar kapalı kalır (Hazir)
            var th = new System.Threading.Thread(() => { try { muzikVeri = MenuMuzik.TuyPop(); muzikHazir = true; } catch { } }); th.IsBackground = true; th.Start();
        }
        // döngü dikişinde tık olmasın: baş ve son karışır
        static void Yumusat(float[] b, int n) { for (int i = 0; i < n; i++) { float k = i / (float)n; b[i] = b[i] * k + b[b.Length - n + i] * (1 - k); } }

        // Sık vuruşlar maç öncesi hazırlanır (HTML vsesIsit): servis, her tür × üç isabet, süper smaç; Unity'de rakibin
        // sesi ayrı tampon olduğu için rakibin vuruşları da. HTML gibi tek seferde değil, sıraya girer ve yalnız sakin
        // anlarda (menü, perde, sayı arası, duraklatma, servis beklerken) karede ikişer işlenir (HTML vsesSakin): eskiden
        // açılışta tek karede ~30 tampon üretiliyor, tel gerginliği değişince ilk vuruşlar o karede sentezleniyordu.
        // Sıra boşalana kadar perde kapalı kalır (Hud: dönen tüy top + "Hazırlanıyor…").
        readonly Queue<System.Action> isitSira = new Queue<System.Action>(); public System.Func<bool> sakin;
        public bool Hazir { get { return isitSira.Count == 0; } }
        public void Isit()
        {
            isitSira.Clear();   // tel/raket değişip yeniden istenirse sıra ikilenmesin
            isitSira.Enqueue(() => Vurus("clear", "good", false, false, true, false)); isitSira.Enqueue(() => Vurus("drop", "good", false, false, true, false));
            foreach (var t in HITMIX.Keys) foreach (var q in new[] { "good", "perfect", "poor" }) { string tt = t, qq = q; isitSira.Enqueue(() => Vurus(tt, qq, false, false, false, false)); }
            isitSira.Enqueue(() => Vurus("smash", "perfect", false, true, false, false)); isitSira.Enqueue(() => Vurus("smash", "good", false, true, false, false));
            foreach (var t in HITMIX.Keys) foreach (var q in new[] { "good", "perfect", "poor" }) { string tt = t, qq = q; isitSira.Enqueue(() => Vurus(tt, qq, false, false, false, true)); }
            isitSira.Enqueue(() => Vurus("clear", "good", false, false, true, true)); isitSira.Enqueue(() => Vurus("drop", "good", false, false, true, true));
        }
        void IsitIsle()
        {
            if (isitSira.Count == 0 || (sakin != null && !sakin())) return;
            for (int i = 0; i < 2 && isitSira.Count > 0; i++) { var f = isitSira.Dequeue(); try { f(); } catch (System.Exception) { } }
        }
        static string Tur(string type) { return type == "jump" || type == "kill" ? "smash" : type == "lift" ? "clear" : type == "block" ? "drop" : type == "push" ? "drive" : HITMIX.ContainsKey(type ?? "") ? type : "clear"; }
        AudioClip Vurus(string type, string q, bool slice, bool super_, bool serve, bool op)
        {
            var GS = Ayar.GS; type = Tur(type);
            string k = type + q + slice + super_ + serve + op + GS.ton.ToString("0.000") + GS.kafa.ToString("0.000");
            AudioClip[] v; if (!vses.TryGetValue(k, out v))
            {
                v = new AudioClip[2];
                for (int j = 0; j < 2; j++) { var s = new Synth(.4f, seed++); VurusCal(s, type, q, slice, super_, serve, GS.ton, GS.kafa); v[j] = op ? s.Clip("vurus_op", .5f, .03f, 3000) : s.Clip("vurus"); }
                vses[k] = v;
            }
            return v[rnd.Next(2)];
        }
        void VurusCal(Synth A, string type, string q, bool slice, bool super_, bool serve, float ton, float kafa)
        {
            var T = HITMIX[type]; float a = T.a * (super_ ? 1.3f : 1) * (serve ? .55f : 1), str = T.str, frm = T.frm, crk = T.crk, ring = T.ring, hiss = T.hiss;
            if (q == "poor") { str *= .22f; frm *= 2.2f; crk *= .3f; ring *= .4f; } else if (q == "perfect") { str *= 1.15f; frm *= .6f; crk *= 1.1f; }
            if (slice) { str *= .55f; crk *= .25f; frm *= .7f; }
            float bus = a * 1.35f, f0 = 1480 * ton * (1 + A.R() * .025f); float[] m = { 1, 1.63f, 2.41f, 3.3f }, g = { .24f, .13f, .08f, .045f };
            for (int i = 0; i < 4; i++) A.Decay(0, f0 * m[i], "sine", g[i] * str * bus, (.1f - i * .018f) * ring / ton, 1.045f);
            A.Burst(0, .004f, "highpass", 3200 * ton, .5f * str * bus, 1, 2);
            if (crk > 0) A.Burst(0, .014f, "bandpass", 4300 * Mathf.Sqrt(ton), .5f * crk * bus, 1.1f, 4);
            A.Decay(0, 235 * kafa, "triangle", .2f * frm * bus, .045f, 1.15f); A.Burst(0, .03f, "lowpass", 360 * kafa, .32f * frm * bus, .8f, 3);
            if (q == "poor") A.Decay(0, 640 * kafa, "square", .1f * bus, .06f, 1.1f);
            A.Burst(.001f, .008f, "bandpass", 1300, .22f * bus, 2, 2);
            if (hiss > 0) A.Burst(.004f, .035f, "bandpass", 6200, .07f * hiss * bus, .7f, 2);
            if (slice)
            {
                A.NoiseSweep(0, .07f, 2200 * ton, 5200 * ton, 3, .2f * bus, true, u => Mathf.Sin(Mathf.PI * u));
                for (int k = 0; k < 4; k++) A.Burst(.08f + k * .028f, .014f, "bandpass", 4800, (.05f - k * .01f) * bus, 1, 2);
            }
            A.Decay(.055f, f0, "sine", .05f * str * bus, .05f); A.Burst(.02f, .17f, "lowpass", 1700, .075f * bus, .7f, 2);
        }

        AudioClip Bir(string k, System.Action<Synth> f, float sec)
        {
            AudioClip c; if (cache.TryGetValue(k, out c)) return c; var s = new Synth(sec, seed++); f(s); c = s.Clip(k); cache[k] = c; return c;
        }
        void Ton(float f, float d, string type, float v, float delay = 0) { src.PlayOneShot(Bir("t" + f + d + type + v + delay, s => s.Tone(delay, f, d, type, v), d + delay + .05f)); }
        void Gurultu(float d, string type, float f, float v, float q = 1, float delay = 0) { src.PlayOneShot(Bir("n" + d + type + f + v + q + delay, s => s.Burst(delay, d, type, f, v, q, 4), d + delay + .05f)); }
        void Arpej(float f0, int[] semi, float d, float v, float ara) { src.PlayOneShot(Bir("a" + f0 + string.Join(",", semi) + d + v + ara, s => { for (int i = 0; i < semi.Length; i++) s.Tone(i * ara, f0 * Mathf.Pow(2, semi[i] / 12f), d, "triangle", v); }, semi.Length * ara + d + .05f)); }

        // ---- olaylar (HTML çağrı yerleri) ----
        // hiz: topun çıkış hızı (m/s; 0 = bilinmiyor, eski düzey). Güç katsayısı p: 12 m/s drop ≈ 0, 60 m/s smaç = 1.
        public void Hit(string type, string q, bool slice, bool super_, bool serve, bool mine, int rally, bool flow, float hiz = 0)
        {
            float p = hiz > 0 ? Mathf.Clamp01((hiz - 12) / 48f) : .5f, kaz = .8f + .35f * p, perde = 1.03f - .06f * p + (float)(rnd.NextDouble() - .5) * .02f;
            string t = Tur(type), kayitAd = serve ? "vurus_servis" : slice ? "vurus_slice" : "vurus_" + t;
            var kv = Kayit(kayitAd + "_" + q) ?? Kayit(kayitAd);
            if (kv != null) { var c = kv[rnd.Next(kv.Length)]; if (mine) VurusCal(c, kaz, perde); else { opSrc.pitch = perde; opSrc.PlayOneShot(c, .5f * kaz); } }
            else if (mine) VurusCal(Vurus(type, q, slice, super_, serve, false), kaz, perde);
            else { var k = Kaynak(); k.pitch = perde; k.PlayOneShot(Vurus(type, q, slice, super_, serve, true), kaz); }
            if (mine && !serve && (type == "smash" || type == "jump" || type == "kill")) Govde(.5f + .5f * p * (super_ ? 1.3f : 1));
            if (rally >= 8 && rally % 4 == 0 && !serve) Ooo(Mathf.Min(1, (rally - 4) / 16f));
            RallyNote(rally - 1, mine, mine && flow);
            if (rally >= 6) Kick(.12f + Mathf.Min(.12f, rally * .008f));
            if (Tur(type) == "smash" && (type == "smash" || type == "jump")) Whistle();
            crowdHedef = .01f + Mathf.Min(.06f, rally * .0045f);
            if (rally > 2 && rally % 5 == 0) Ton(523 * Mathf.Pow(2, Mathf.Min(rally, 30) / 24f), .2f, "triangle", .07f);
        }
        AudioSource Kaynak() { var k = vurusSrc[vurusSira]; vurusSira = (vurusSira + 1) % vurusSrc.Length; return k; }
        void VurusCal(AudioClip c, float kaz, float perde) { var k = Kaynak(); k.pitch = perde; k.PlayOneShot(c, kaz); }
        // Gövde darbesi: alçak sinüs süpürmesi, iki düzeyde önbellekte (kazanç oynatmada).
        void Govde(float v) { if (Kayitli("govde", v)) return; src.PlayOneShot(Bir("govde", s => s.Sweep(0, .09f, "sine", t => 80 * Mathf.Pow(45f / 80, Mathf.Min(1, t / .07f)), t => .32f * (t < .002f ? t / .002f : Mathf.Pow(.0005f, Mathf.Min(1, (t - .002f) / .08f)))), .1f), Mathf.Min(1.3f, v)); }
        // Seyirci "ooo": geniş formant (380 → 620 Hz), 0,9 sn kabarıp söner; şiddet ralli uzunluğuyla.
        void Ooo(float k) { if (Kayitli("ooo", .5f + .5f * k)) return; src.PlayOneShot(Bir("ooo", s => { s.NoiseSweep(0, .9f, 380, 620, 1.6f, .09f, false, u => Mathf.Sin(Mathf.PI * Mathf.Min(1, u * 1.3f)) * (1 - u * .4f)); s.NoiseSweep(0, .9f, 900, 1150, 2.2f, .035f, false, u => Mathf.Sin(Mathf.PI * Mathf.Min(1, u * 1.3f))); }, .95f), .5f + .5f * k); }
        // Alkış: rastgele zamanlı el çırpışları (her biri 4–7 ms gürültü, bandpass 900–2600 Hz), yoğunluk başta yüksek, sonda
        // seyrek. Ralli uzunluğuna göre iki boy (kısa 1,4 sn, uzun 2,4 sn); her boy iki çekimle önbellekte.
        void Alkis(bool uzun)
        {
            if (Kayitli(uzun ? "alkis_uzun" : "alkis")) return;
            float sure = uzun ? 2.4f : 1.4f; int cek = rnd.Next(2);
            src.PlayOneShot(Bir("alkis" + uzun + cek, s =>
            {
                var r = new System.Random(uzun ? 41 + cek : 17 + cek); int n = uzun ? 260 : 130;
                for (int i = 0; i < n; i++)
                {
                    float u = (float)Mathf.Pow((float)r.NextDouble(), 1.7f), t = u * sure;
                    s.Burst(t, .004f + (float)r.NextDouble() * .003f, "bandpass", 900 + (float)r.NextDouble() * 1700, (.05f + (float)r.NextDouble() * .05f) * (1 - u * .6f), 1.2f, 2);
                }
            }, sure + .1f), .9f);
        }
        // Ekipman ekranında rakete/tele dokununca o takımın smaç sesi (HTML racketHit({type:'smash',q:'perfect'})).
        public void Deneme() { src.PlayOneShot(Vurus("smash", "perfect", false, false, false, false)); }
        void RallyNote(int n, bool mine, bool harm)
        {
            int i = Mathf.Min(Mathf.Max(0, n), 14), semi = i / 5 * 12 + PENTA[i % 5]; float f = (mine ? 392 : 196) * Mathf.Pow(2, semi / 12f);
            src.PlayOneShot(Bir("r" + f + mine + harm, s => { s.Tone(0, f, .32f, "triangle", mine ? .05f : .03f); s.Tone(.01f, f * 2, .18f, "sine", mine ? .02f : .01f); if (harm) { s.Tone(.04f, f * 1.5f, .4f, "sine", .03f); s.Tone(.08f, f * 2.52f, .3f, "sine", .015f); } }, .5f));
        }
        void Kick(float v) { src.PlayOneShot(Bir("k" + v.ToString("0.000"), s => s.Sweep(0, .25f, "sine", t => 130 * Mathf.Pow(42f / 130, Mathf.Min(1, t / .14f)), t => v * Mathf.Pow(.0001f / v, Mathf.Min(1, t / .22f))), .26f)); }
        void Whistle() { if (Kayitli("duduk", .6f)) return; src.PlayOneShot(Bir("duduk", s => s.Sweep(0, .52f, "sine", t => 2600 * Mathf.Pow(700f / 2600, Mathf.Min(1, t / .45f)), t => .025f * Mathf.Pow(.004f, Mathf.Min(1, t / .5f))), .53f)); }
        public void Squeak() { if (Kayitli("gicirti", .8f)) return; src.PlayOneShot(Bir("gicirti", s => s.Sweep(0, .13f, "sine", t => t < .06f ? Mathf.Lerp(2300, 3400, t / .06f) : Mathf.Lerp(3400, 2800, Mathf.Min(1, (t - .06f) / .04f)), t => .025f * Mathf.Pow(.004f, Mathf.Min(1, t / .12f))), .14f)); }
        public void Floor(bool big) { if (Kayitli(big ? "yer_sert" : "yer", big ? 1 : .7f)) { if (big) Kick(.18f); return; } Gurultu(.05f, "lowpass", big ? 600 : 1100, big ? .25f : .12f); if (big) Kick(.18f); }
        // file: k 0 drop … 1 smaç; smaçta ağ "puf" diye yutar, dropta yalnız hışırtı; bant kordon tınlar
        public void Net(float k, bool tape)
        {
            if (Kayitli(tape ? "file_bant" : "file", .5f + .5f * k)) return;
            src.PlayOneShot(Bir("file" + k.ToString("0.0") + tape, s =>
            {
                s.Burst(0, .05f + .1f * k, "lowpass", 260 + 240 * k, .05f + .22f * k, 1, 4); s.Burst(0, .08f, "bandpass", 1400, .03f + .05f * k, 2, 4);
                int n = 2 + Mathf.RoundToInt(3 * k); for (int i = 0; i < n; i++) s.Burst(i * .045f, .04f, "bandpass", 900 + i * 300, .04f + .06f * k, 3, 4);
                if (tape) s.Tone(0, 1500, .09f, "triangle", .06f);
            }, .5f));
        }
        public void Bant() { if (Kayitli("bant", .7f)) return; Ton(1900, .05f, "triangle", .05f); }
        public void Hawk(bool icerde)
        {
            if (Kayitli(icerde ? "hawk_ici" : "hawk_disari")) { Gasp(.07f, .38f); return; }
            src.PlayOneShot(Bir("davul", s => { for (int k = 0; k < 9; k++) s.Burst(k * .038f, .03f, "bandpass", 300, .06f + k * .008f, 1.5f, 4); }, .4f));
            src.PlayOneShot(Bir("hawk" + icerde, s => s.Tone(.38f, icerde ? 880 : 330, .3f, "triangle", .07f), .75f)); Gasp(.07f, .38f);
        }
        public void Gasp(float v, float delay = 0)
        {
            if (delay == 0 && Kayitli("ic_cekis", v / .09f)) return;
            src.PlayOneShot(Bir("ic" + v + delay, s => s.NoiseSweep(delay, .8f, 380, 900, 2, v, false, u => u < .1875f ? u / .1875f : Mathf.Pow(.0001f, (u - .1875f) / .8125f)), delay + .85f));
        }
        public void Cheer(float v)
        {
            if (Kayitli("cosku", v / .18f)) return;
            src.PlayOneShot(Bir("cosku" + v, s => { var b = new Synth.Biquad("bandpass", 1200, .6f); int n = (int)(Synth.SR * 1.2f); for (int i = 0; i < n; i++) s.buf[i] += b.Run(s.R() * Mathf.Sin(Mathf.PI * i / n) * (.6f + .4f * Mathf.Sin(i / 900f))) * v; }, 1.25f));
        }
        // ralli: sayının vuruş sayısı (0 = bilinmiyor). Uzun ralliyi seyirci alkışlar (kimin aldığından bağımsız; rakibin
        // sayısında kısa alkış, benim sayımda sevinçle birlikte).
        public void Point(bool mine, bool smacSayisi, int ralli = 0)
        {
            if (ralli >= 10) Alkis(ralli >= 18);
            if (mine && Kayitli("sayi_ben")) { if (smacSayisi) Gasp(.09f); return; }
            if (!mine && Kayitli("sayi_rakip")) return;
            if (mine) { if (ralli < 10) Cheer(.14f); Arpej(523, new[] { 0, 4, 7, 12 }, .18f, .08f, .07f); if (smacSayisi) Gasp(.09f); }
            else Ton(220, .25f, "sawtooth", .05f);
        }
        // İmza vuruşu hazırlığı (2026-10-26, Core/Imza): kare + üçgen iki ton, perde imzaya göre (ses de imzayı tanıtır,
        // yalnız ışığa güvenilmez); okunan imza: kısa yükselen arpej (görev arpejinden bir oktav yukarıda başlar).
        public void Imza(string id) { float f = id == "yildirim" ? 440 : id == "duvarblok" ? 330 : id == "tuldrop" ? 587 : id == "mermi" ? 392 : 494; Ton(f, .14f, "square", .035f); Ton(f * 1.5f, .2f, "triangle", .05f, .1f); }
        public void Okudun() { Arpej(784, new[] { 0, 4, 7, 12 }, .12f, .05f, .05f); }
        public void Aldat() { Ton(740, .1f, "triangle", .06f); Ton(988, .12f, "triangle", .06f, .08f); }
        public void Split() { Ton(1200, .04f, "square", .03f); }
        public void Perfect(int pstreak) { Ton(660 * Mathf.Pow(2, Mathf.Min(pstreak, 12) / 12f), .12f, "sine", .06f, .02f); }
        public void Akis() { Arpej(523, new[] { 0, 2, 4, 7, 9, 12 }, .25f, .05f, .06f); }
        public void Gorev() { Arpej(660, new[] { 0, 4, 7, 12, 16 }, .18f, .05f, .07f); }
        public void OgrTamam() { Arpej(660, new[] { 0, 4, 7 }, .14f, .05f, .06f); }
        public void Yukselt() { Arpej(523, new[] { 0, 4, 7, 12 }, .14f, .05f, .06f); }
        public void Tik(float f = 880) { Ton(f, .05f, "triangle", .04f); }
        public void Iyi() { Ton(784, .12f, "triangle", .05f); }
        public void Hedefte() { Ton(1320, .1f, "triangle", .05f); }
        public void Iska() { if (Kayitli("iska", .6f)) return; Gurultu(.05f, "highpass", 1200, .05f); }
        public void MacSonu(bool won) { crowdHedef = .004f; SuAlti(false); if (won) Cheer(.18f); }
        // Vuruş penceresi: ses su altındaymış gibi boğulur (lowpass 750 Hz), vuruşla açılır; girişte alçak bir "vuuu".
        public void SuAlti(bool on) { if (on && suHedef > 1000) Gurultu(.25f, "lowpass", 300, .08f); suHedef = on ? 750 : 22000; }
        public void Ugultu(bool on, float sp) { swo.Ayarla(on, sp); }

        // ---- kayıtlı ses kancası (yukarıdaki başlık notu) ----
        public static readonly string[] KAYIT_ADLARI = { "vurus_smash", "vurus_clear", "vurus_drive", "vurus_drop", "vurus_servis", "vurus_slice", "govde", "file", "file_bant", "bant", "yer", "yer_sert",
            "seyirci", "ooo", "alkis", "alkis_uzun", "cosku", "ic_cekis", "duduk", "gicirti", "hawk_ici", "hawk_disari", "sayi_ben", "sayi_rakip", "iska" };
        readonly Dictionary<string, AudioClip[]> kayit = new Dictionary<string, AudioClip[]>();
        // Yoksa null (sonuç önbellekte: Resources.Load ad başına bir kez sorulur).
        AudioClip[] Kayit(string ad)
        {
            AudioClip[] v; if (kayit.TryGetValue(ad, out v)) return v;
            var l = new List<AudioClip>(); var c = Resources.Load<AudioClip>("ses/" + ad); if (c != null) l.Add(c);
            for (int i = 1; i <= 8; i++) { var ci = Resources.Load<AudioClip>("ses/" + ad + "_" + i); if (ci == null) break; l.Add(ci); }
            v = l.Count > 0 ? l.ToArray() : null; kayit[ad] = v; return v;
        }
        bool Kayitli(string ad, float kaz = 1) { var v = Kayit(ad); if (v == null) return false; src.PlayOneShot(v[rnd.Next(v.Length)], Mathf.Min(1.5f, kaz)); return true; }

        void Update()
        {
            IsitIsle();
            float dt = Time.unscaledDeltaTime; bool sessiz = !Ayar.ses;
            crowd.volume += ((sessiz ? 0 : crowdHedef * 3.5f) - crowd.volume) * (1 - Mathf.Exp(-dt / .4f));   // Web Audio setTargetAtTime(.4)
            float tc = suHedef < 1000 ? .05f : .02f; suAlti.cutoffFrequency += (suHedef - suAlti.cutoffFrequency) * (1 - Mathf.Exp(-dt / tc));
            MuzikIsle(dt);
        }
        // Menü müziği: açılır .8 sn'de (yumuşak giriş), söner .35 sn'de (perdenin kapanışı ≈.4 sn; maç sesi müziğin üstüne
        // binmesin). Sönünce duraklatılır, menüye dönünce kaldığı yerden sürer. Başka uygulamanın sesi 2 sn'de bir sorulur:
        // oyuncu menüdeyken Spotify'ı açarsa müzik çekilir, kapatırsa geri gelir.
        void MuzikIsle(float dt)
        {
            if (muzik == null)
            {
                if (!muzikHazir) return;
                var c = AudioClip.Create("menu_muzik", muzikVeri.Length, 1, Synth.SR, false); c.SetData(muzikVeri, 0); muzikVeri = null;
                muzik = gameObject.AddComponent<AudioSource>(); muzik.clip = c; muzik.loop = true; muzik.volume = 0; muzik.playOnAwake = false; muzik.spatialBlend = 0;
            }
            bool menu = menude != null && menude();
            if (menu && (digerT -= dt) <= 0) { digerT = 2; digerCaliyor = Yerel.DigerSesCaliyor(); }
            bool ac = menu && Ayar.muzik && Ayar.ses && !digerCaliyor;
            muzikDuzey += ((ac ? MUZIK_DUZEY : 0) - muzikDuzey) * (1 - Mathf.Exp(-dt / (ac ? .8f : .35f)));
            if (ac && !muzikCalar) { if (muzik.time > 0) muzik.UnPause(); else muzik.Play(); muzikCalar = true; }
            else if (!ac && muzikCalar && muzikDuzey < .002f) { muzik.Pause(); muzikCalar = false; muzikDuzey = 0; }
            muzik.volume = muzikDuzey;
        }
        void OnApplicationFocus(bool odak) { if (odak) digerT = 0; }
        public void CrowdStart() { crowdHedef = .01f; }
        public void CrowdSus() { crowdHedef = 0; }

        // Titreşim deseni (HTML hapHit): süre vuruşun gücü, gerginlik dokusu. Yerel.Vurus çalar.
        public static readonly Dictionary<string, int> HIT_MS = new Dictionary<string, int> { { "smash", 44 }, { "jump", 62 }, { "kill", 26 }, { "drive", 28 }, { "clear", 32 }, { "drop", 9 }, { "lift", 24 }, { "block", 12 }, { "push", 16 } };
        public static int[] Desen(string type, string q, bool slice, float T)
        {
            float k = Mathf.Pow(26 / T, .45f); int ms; HIT_MS.TryGetValue(type ?? "", out ms); if (ms == 0) ms = 28; int d = Mathf.RoundToInt(ms * k); int[] p;
            if (q == "poor") p = new[] { Mathf.RoundToInt(d * .3f), 14, Mathf.RoundToInt(d * .25f), 12, Mathf.RoundToInt(d * .2f) };
            else if (slice) p = new[] { 4, 9, 4, 9, 5, 9, Mathf.RoundToInt(d * .55f) };
            else if (T <= 22) p = new[] { d, 18, Mathf.RoundToInt(d * .4f) }; else if (T >= 30) p = new[] { Mathf.Max(5, Mathf.RoundToInt(d * .7f)) }; else p = new[] { d };
            if (q == "perfect") p[0] = Mathf.RoundToInt(p[0] * 1.15f); return p;
        }
    }

    // Raket uğultusu (HTML swoosh*): döngü gürültü, bandpass Q 1,4; frekans 500 + min(3, hız)·900, kazanç min(.22, hız·.09).
    // Süzgeç ses iş parçacığında (OnAudioFilterRead) canlı uygulanır; değerler ana iş parçacığından yumuşatılarak gelir.
    public class Swoosh : MonoBehaviour
    {
        AudioSource a; volatile float hedefF = 700, hedefG; float f = 700, g; readonly System.Random r = new System.Random(9);
        Synth.Biquad bq = new Synth.Biquad("bandpass", 700, 1.4f); int sayac; bool acik;
        void Awake() { a = gameObject.AddComponent<AudioSource>(); a.playOnAwake = false; a.loop = true; a.clip = AudioClip.Create("sessiz", Synth.SR, 1, Synth.SR, false); a.Play(); }
        public void Ayarla(bool on, float sp) { acik = on && Ayar.ses; hedefG = acik ? Mathf.Min(.22f, sp * .09f) : 0; hedefF = 500 + Mathf.Min(3, sp) * 900; }
        void OnAudioFilterRead(float[] data, int ch)
        {
            float kg = 1 - Mathf.Exp(-1f / (Synth.SR * (hedefG > g ? .02f : .04f))), kf = 1 - Mathf.Exp(-1f / (Synth.SR * .03f));
            for (int i = 0; i < data.Length; i += ch)
            {
                g += (hedefG - g) * kg; f += (hedefF - f) * kf; if ((sayac++ & 63) == 0) bq.Set("bandpass", f, 1.4f);
                float x = bq.Run((float)(r.NextDouble() * 2 - 1)) * g * 2;
                for (int c = 0; c < ch; c++) data[i + c] = x;
            }
        }
    }

    // "Tüy Pop" menü döngüsü (yasin 2026-10-30 seçimi; dinleme sayfasındaki aday B, /mnt/project-files/birdie-smash/muzik/ezgi.py
    // aday_b'nin birebir kopyası): 112 BPM, 12 ölçü (≈25,7 sn), F–G–Em–Am ×3. Her vuruşta sinüs davul (130 → 42 Hz; güçlü
    // vuruşta .30, zayıfta .18), zayıf vuruşta el çırpma (bandpass 1500 gürültü), vuruş arası sinüs bas; akor tonlarından
    // sekizlik arpej (kare, .22 sn, alçak geçiren 2400 Hz); 5. ölçüden ezgi (üçgen) iki kez. Dosya yok, lisans sorusu yok.
    // Dikişsiz: tampon 2 sn uzun sentezlenir, döngü sonunu aşan kuyruk başa eklenir (çalan nota başta sönerek sürer).
    public static class MenuMuzik
    {
        static readonly Dictionary<char, int> NOT = new Dictionary<char, int> { { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 }, { 'G', 7 }, { 'A', 9 }, { 'B', 11 } };
        static int M(string s) { return 12 * (s[s.Length - 1] - '0' + 1) + NOT[s[0]] + (s.IndexOf('#') >= 0 ? 1 : 0); }
        static float Hz(int m) { return 440 * Mathf.Pow(2, (m - 69) / 12f); }
        public static float[] TuyPop()
        {
            float b = 60f / 112, L = 12 * 4 * b; int N = Mathf.RoundToInt(L * Synth.SR); var S = new Synth(L + 2, 21);
            string[] bas = { "F2", "G2", "E2", "A2" }; string[][] ak = { new[] { "F3", "A3", "C4" }, new[] { "G3", "B3", "D4" }, new[] { "E3", "G3", "B3" }, new[] { "A3", "C4", "E4" } };
            for (int k = 0; k < 12; k++)
            {
                float t0 = k * 4 * b; int kb = M(bas[k % 4]); var a = ak[k % 4];
                for (int j = 0; j < 4; j++)
                {
                    float v = j % 2 == 0 ? .30f : .18f;
                    S.Sweep(t0 + j * b, .25f, "sine", t => 130 * Mathf.Pow(42f / 130, Mathf.Min(1, t / .14f)), t => v * Mathf.Pow(.0001f / v, Mathf.Min(1, t / .22f)));
                    if (j % 2 == 1) S.Burst(t0 + j * b, .12f, "bandpass", 1500, .20f, .9f, 3);
                    S.Decay(t0 + j * b + .5f * b, Hz(kb), "sine", .26f, .30f);
                }
                int a0 = M(a[0]) + 12, a1 = M(a[1]) + 12, a2 = M(a[2]) + 12; int[] arp = { a0, a1, a2, a1, a0 + 12, a2, a1, a2 };
                for (int j = 0; j < 8; j++) S.Tone(t0 + j * b / 2, Hz(arp[j]), .22f, "square", .05f, new Synth.Biquad("lowpass", 2400, .8f));
            }
            string[] ezgi = { "A5 1", "G5 .5", "E5 .5", "G5 1", "C6 1", "B5 .5", "A5 .5", "G5 1", "D5 2", "E5 .5", "G5 .5", "B5 1", "A5 .5", "G5 .5", "E5 1", "A5 .5", "C6 .5", "A5 1", "E5 2" };
            float te = 4 * 4 * b;
            for (int r = 0; r < 2; r++)
                foreach (var e in ezgi)
                {
                    var p = e.Split(' '); float d = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture) * b;
                    S.Decay(te, Hz(M(p[0])), "triangle", .13f, Mathf.Min(.7f, d * 1.2f)); te += d;
                }
            var o = new float[N]; float tepe = 1e-6f;
            for (int i = 0; i < N; i++) { float x = S.buf[i] + (i + N < S.buf.Length ? S.buf[i + N] : 0); o[i] = x; tepe = Mathf.Max(tepe, Mathf.Abs(x)); }
            float g = .8f / tepe; for (int i = 0; i < N; i++) o[i] *= g;
            return o;
        }
    }
}
