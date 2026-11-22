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
// düdük, seyirci uğultusu (bandpass 520 → lowpass 1400; 2026-11-19 rallide sessiz, yalnız sayı tepkileri), vuruş penceresinde "su altı"
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
// 2026-11-19 "Tüy Pop" sevimli sürüm (yasin: "elektronik ve korkunç olmuş, sevimli bir ezgi olmalı", sonra "Ok ekle"):
// eski FM piyano/minör akor/uzun yankı bırakıldı; Do majör 120 BPM, ukulele (Karplus-Strong), ksilofon, çan, ıslık,
// pizzicato bas, yumuşak davul, kısa oda yankısı; 72 sn dikişsiz döngü. Taslağı muzik/sevimli.py, çıktısı onunla eşleşir.
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
        // Salon yansıması (2026-11-05 ses turu): Schroeder; 4 sönümlü tarak + 2 tümgeçiren, giriş 500 Hz highpass (kuyruk
        // boğuk olmasın), 14 ms ön gecikme, çıkış 4,5 kHz lowpass. Kayıtlı badminton vuruşları 0,2–0,5 sn salon kuyruğu
        // taşıyor; eski sentez 80 ms'de kesiliyordu ("kuru ve hep aynı"). Maliyet O(n), vuruş başına ~1 ms.
        public void Salon( float wet, float rt, float damp)
        {
          float[] b = buf; int pre = (int)(.014f * SR), n = b.Length; var hp = new Biquad("highpass", 500, .707f);
          var gir = new float[n]; for (int i = pre; i < n; i++) gir[i] = hp.Run(b[i - pre]);
          float[] cd = { .0297f, .0371f, .0411f, .0437f }; var outv = new float[n];
          for (int c = 0; c < 4; c++)
          {
            int d = (int)(cd[c] * SR), p = 0; float g = Mathf.Pow(10, -3 * cd[c] / rt) * .25f, lp = 0; var line = new float[d];
            // .25 karışım kazancı geri beslemeye gömülü: hat içeriği ¼ ölçekte tutulur, çıkış doğrudan toplanır
            for (int i = 0; i < n; i++) { float y = line[p]; lp = y * (1 - damp) + lp * damp; line[p] = gir[i] * .25f + lp * g * 4; outv[i] += y; if (++p == d) p = 0; }
          }
          foreach (var ad in new[] { .005f, .0017f })
          {
            int d = (int)(ad * SR), p = 0; var line = new float[d]; const float g = .7f;
            for (int i = 0; i < n; i++) { float bu = line[p], x = outv[i]; outv[i] = -g * x + bu; line[p] = x + g * bu; if (++p == d) p = 0; }
          }
          var lp2 = new Biquad("lowpass", 4500, .707f); for (int i = 0; i < n; i++) b[i] += lp2.Run(outv[i]) * wet;
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
        AudioSource src, trb, opSrc, yerSrc; readonly AudioSource[] vurusSrc = new AudioSource[4]; int vurusSira; AudioLowPassFilter suAlti; float suHedef = 22000; System.Random rnd = new System.Random(5); int seed = 11;
        AudioSource crowd; float crowdHedef; Swoosh swo;
        // Menü müziği: döngü ilk açılışta arka iş parçacığında sentezlenir (≈1,1 M örnek), clip ana iş parçacığında kurulur.
        // menude GameRoot'tan: menüdeyken ve perde menüyü örtmüyorken. Düzey .25: döngü .8 tepeye normalize, menü tık
        // sesleri (.04–.06) üstünde duyulsun diye müzik tepesi ≈.2'de kalır.
        public System.Func<bool> menude; AudioSource muzik; float[] muzikVeri; volatile bool muzikHazir; float muzikDuzey, digerT; bool muzikCalar, digerCaliyor;
        const float MUZIK_DUZEY = .25f;
        readonly Dictionary<string, AudioClip[]> vses = new Dictionary<string, AudioClip[]>(); readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly int[] PENTA = { 0, 2, 4, 7, 9 };
        // Tür karışımı (2026-11-05 ses turu): pat = smaçın kuru şaklaması (2,9 kHz bandpass), rt/wet = salon kuyruğu süresi
        // ve düzeyi. Drop kısık, parlak ve kısa; smaç kuru ve sert; clear en çok tel çınlaması.
        class Mix { public float a, str, frm, crk, ring, hiss, pat, rt, wet; }
        static readonly Dictionary<string, Mix> HITMIX = new Dictionary<string, Mix> {
            {"smash",new Mix{a=1,str=.8f,frm=.7f,crk=1,ring=.5f,hiss=1,pat=1,rt=.5f,wet=.5f}},
            {"clear",new Mix{a=.78f,str=1,frm=.5f,crk=.45f,ring=1,hiss=.5f,pat=.5f,rt=.55f,wet=.5f}},
            {"drive",new Mix{a=.8f,str=.9f,frm=.6f,crk=.7f,ring=.55f,hiss=.6f,pat=.75f,rt=.45f,wet=.45f}},
            {"drop",new Mix{a=.42f,str=.75f,frm=.1f,crk=.4f,ring=.6f,hiss=0,pat=.35f,rt=.35f,wet=.4f}}};

        public static Sfx Build(Transform root, Transform listener)
        {
            var g = new GameObject("ses"); g.transform.SetParent(root, false); var s = g.AddComponent<Sfx>(); s.Init(listener); return s;
        }
        void Init(Transform listener)
        {
            src = gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
            trb = gameObject.AddComponent<AudioSource>(); trb.playOnAwake = false; trb.spatialBlend = 0;   // tribün tek-atımlıkları: düzeyi Ayar.SeyirciK (Update)
            // Vuruşlar perdeli çaldığı için ayrı kaynaklardan (PlayOneShot perde almaz); 4 kaynak üst üste binen vuruşa yeter.
            for (int i = 0; i < vurusSrc.Length; i++) { vurusSrc[i] = gameObject.AddComponent<AudioSource>(); vurusSrc[i].playOnAwake = false; vurusSrc[i].spatialBlend = 0; }
            // Top düşüşü ve file kendi kaynağında: konum panı (pan notu) menü/ralli seslerinin çaldığı src'yi kaydırmasın.
            yerSrc = gameObject.AddComponent<AudioSource>(); yerSrc.playOnAwake = false; yerSrc.spatialBlend = 0;
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
            Sira("clear", "good", false, false, true, false); Sira("drop", "good", false, false, true, false);
            foreach (var t in HITMIX.Keys) foreach (var q in new[] { "good", "perfect", "poor" }) { string tt = t, qq = q; Sira(tt, qq, false, false, false, false); }
            Sira("smash", "perfect", false, true, false, false); Sira("smash", "good", false, true, false, false);
            Sira("jump", "perfect", false, false, false, false); Sira("jump", "good", false, false, false, false);
            foreach (var t in HITMIX.Keys) foreach (var q in new[] { "good", "perfect", "poor" }) { string tt = t, qq = q; Sira(tt, qq, false, false, false, true); }
            Sira("clear", "good", false, false, true, true); Sira("drop", "good", false, false, true, true);
            Sira("jump", "good", false, false, false, true);
            // İnceleme 2026-11-15 12:05 #1: kesme (slice) vuruşu anahtara girdiği hâlde ısıtılmıyordu; oturumun ilk aldatmasında
            // (oyunun ustalık anı) iki tampon + yankı o vuruş karesinde sentezleniyordu. Slice poor'da açılmaz (Aldatma:241), bu
            // yüzden yalnız good/perfect, iki taraf. Rakibin tam smacı ve sıçrama perfect'i de aynı delikti.
            foreach (var t in HITMIX.Keys) foreach (var q in new[] { "good", "perfect" }) foreach (var op in new[] { false, true })
            { string tt = t, qq = q; bool oo = op; Sira(tt, qq, true, false, false, oo); }
            Sira("smash", "perfect", false, true, false, true); Sira("smash", "good", false, true, false, true);
            Sira("jump", "perfect", false, false, false, true);
            ArtKur();
        }
        // Ralli sesleri (İnceleme turu 2026-11-10 17:08 #4): ralli notası (15 basamak × 3 renk) ve perfect serisi tonları (13)
        // ralli ortasında ilk kez sentezleniyordu: en heyecanlı anda ana iş parçacığında tampon. Ayrı, düşük öncelikli sıra:
        // Hazir'a sayılmaz (perde bunlar için beklemez), vuruş sırası bitince sakin anlarda karede bir tane. Önbellek kalıcı
        // olduğundan bir kez kurulur.
        readonly Queue<System.Action> artSira = new Queue<System.Action>(); bool artKuruldu;
        void ArtKur()
        {
            if (artKuruldu) return; artKuruldu = true;
            for (int i = 0; i < 15; i++) { int n = i; artSira.Enqueue(() => RallyKlip(n, true, true)); artSira.Enqueue(() => RallyKlip(n, true, false)); artSira.Enqueue(() => RallyKlip(n, false, false)); }
            for (int i = 0; i <= 12; i++) { int n = i; artSira.Enqueue(() => PerfectKlip(n)); }
            artSira.Enqueue(() => KickKlip());
            // Sayı ve maç sonu sesleri (2026-11-12, yasin: "sayı sonrası animasyonda kasma, maç sonu sevinçte donma"): coşku (1,2 sn
            // süzgeçli gürültü), iç çekiş, alkış ve sayı arpeji ilk kez tam sayı ya da maç sonu anında sentezleniyordu; her biri
            // ana iş parçacığında birkaç ms tampon + AudioClip.SetData, sevinç ve sayı yazısıyla aynı karede. Kayıtlı ses varsa
            // (Kayit) sentez hiç çağrılmaz; kayıtsız yolun klipleri burada, aynı anahtarlarla önceden kurulur. Dans motifleri de.
            foreach (var v in new[] { .14f, .18f, .24f }) { float vv = v; artSira.Enqueue(() => CheerKlip(vv)); }
            foreach (var v in new[] { .06f, .07f, .08f, .09f }) { float vv = v; artSira.Enqueue(() => GaspKlip(vv, 0)); }
            artSira.Enqueue(() => GaspKlip(.07f, .38f));
            for (int c = 0; c < 2; c++) { int cc = c; artSira.Enqueue(() => AlkisKlip(false, cc)); artSira.Enqueue(() => AlkisKlip(true, cc)); }
            artSira.Enqueue(() => ArpejKlip(523, SAYI_ARPEJ, .18f, .08f, .07f)); artSira.Enqueue(() => TonKlip(220, .25f, "sawtooth", .05f));
            foreach (var d in PlayerView.DANSLAR) { string dd = d; artSira.Enqueue(() => DansIsit(dd)); }
        }
        static readonly int[] SAYI_ARPEJ = { 0, 4, 7, 12 };
        // Dans() ile aynı parametreler; çalmadan yalnız klipleri kurar (anahtarlar aynı olduğu için Dans() önbellekten çalar).
        void DansIsit(string id)
        {
            switch (id)
            {
                case "robot": TonKlip(880, .06f, "square", .03f); TonKlip(660, .08f, "square", .03f, .12f); break;
                case "tavuk": TonKlip(620, .05f, "square", .028f); TonKlip(820, .04f, "square", .026f, .07f); TonKlip(560, .07f, "square", .028f, .14f); break;
                case "twist": TonKlip(523, .12f, "triangle", .05f); TonKlip(659, .16f, "triangle", .05f, .18f); break;
                case "dalga": ArpejKlip(523, new[] { 0, 4, 7, 12 }, .12f, .04f, .06f); GurultuKlip(.3f, "bandpass", 2500, .03f, 1, .05f); break;
                case "helikopter": GurultuKlip(.06f, "lowpass", 320, .07f); GurultuKlip(.06f, "lowpass", 320, .07f, 1, .11f); GurultuKlip(.06f, "lowpass", 320, .07f, 1, .22f); break;
                case "gitar": ArpejKlip(196, new[] { 0, 7, 12, 16, 19 }, .4f, .035f, .025f); break;
                case "fiskiye": GurultuKlip(.03f, "highpass", 4000, .04f); GurultuKlip(.03f, "highpass", 4000, .04f, 1, .17f); GurultuKlip(.03f, "highpass", 4000, .04f, 1, .34f); break;
                case "ucak": TonKlip(392, .3f, "triangle", .04f); TonKlip(523, .3f, "triangle", .04f, .15f); break;
                case "yildiz": ArpejKlip(784, new[] { 0, 7, 12 }, .07f, .04f, .05f); break;
                case "aglak": TonKlip(440, .14f, "triangle", .045f); TonKlip(349, .2f, "triangle", .045f, .16f); break;
                case "kulak": TonKlip(523, .07f, "triangle", .04f); TonKlip(784, .1f, "triangle", .04f, .09f); break;
                case "toz": GurultuKlip(.05f, "highpass", 3000, .035f); GurultuKlip(.05f, "highpass", 3000, .035f, 1, .14f); break;
                case "esne": TonKlip(392, .25f, "triangle", .04f); TonKlip(294, .35f, "triangle", .04f, .2f); break;
                case "kivir": TonKlip(523, .06f, "triangle", .04f); TonKlip(659, .06f, "triangle", .04f, .1f); TonKlip(784, .08f, "triangle", .04f, .2f); break;
                case "disko": TonKlip(440, .07f, "square", .028f); TonKlip(880, .09f, "square", .028f, .14f); break;
                case "nanik": TonKlip(784, .08f, "triangle", .045f); TonKlip(659, .08f, "triangle", .045f, .1f); TonKlip(784, .08f, "triangle", .045f, .2f); TonKlip(587, .14f, "triangle", .045f, .3f); break;
                case "gelgel": TonKlip(392, .07f, "triangle", .04f); TonKlip(523, .1f, "triangle", .04f, .1f); break;
                case "opucuk": GurultuKlip(.04f, "highpass", 4000, .04f); TonKlip(1175, .08f, "sine", .04f, .06f); break;
                case "cikcik": GurultuKlip(.025f, "highpass", 2500, .04f); GurultuKlip(.025f, "highpass", 2500, .04f, 1, .16f); break;
            }
        }
        // derinSakin (2026-11-12): perde kapalı ya da menü durağan. Ek sıra (ralli notaları, sayı/maç sonu sesleri) yalnız orada işlenir,
        // karede dört: önceden "sakin" anlarda karede bir taneydi ve sakin anlar sayı arasını (sevinç + sayı yazısı) da kapsıyordu;
        // sentez tam o karelere biniyordu. Maç perdesi en az .6 sn kapalı kaldığı için sıra ilk perdede biter.
        public System.Func<bool> derinSakin;
        void IsitIsle()
        {
            if (sakin != null && !sakin()) return;
            if (isitSira.Count == 0)
            {
                if (artSira.Count > 0 && derinSakin != null && derinSakin())
                    for (int i = 0; i < 4 && artSira.Count > 0; i++) { var g = artSira.Dequeue(); try { g(); } catch (System.Exception) { } }
                return;
            }
            for (int i = 0; i < 2 && isitSira.Count > 0; i++) { var f = isitSira.Dequeue(); try { f(); } catch (System.Exception) { } }
        }
        // İnceleme 2026-11-16 12:05 #2: rövanşta tel/raket aynıysa 52 işin hepsi önbellekten dönüyordu ama sıra karede ikişer
        // işlendiği için perde ~26 kare (60 Hz'de ~0,43 sn) boşuna kapalı kalıyordu. Önbellekte olan sıraya girmez; ilk maç ve
        // tel değişimi bugünkü gibi. Anahtar tek fonksiyondan (Vurus da aynısını kullanır), ikisi ayrışamaz.
        void Sira(string type, string q, bool slice, bool super_, bool serve, bool op)
        {
            if (vses.ContainsKey(VurusAnahtar(type, q, slice, super_, serve, op))) return;
            isitSira.Enqueue(() => Vurus(type, q, slice, super_, serve, op));
        }
        static string VurusAnahtar(string type, string q, bool slice, bool super_, bool serve, bool op)
        {
            var GS = Ayar.GS; bool jump = type == "jump"; type = Tur(type);
            return type + q + slice + super_ + serve + op + jump + GS.ton.ToString("0.000") + GS.kafa.ToString("0.000");
        }
        static string Tur(string type) { return type == "jump" || type == "kill" ? "smash" : type == "lift" ? "clear" : type == "block" ? "drop" : type == "push" ? "drive" : HITMIX.ContainsKey(type ?? "") ? type : "clear"; }
        AudioClip Vurus(string type, string q, bool slice, bool super_, bool serve, bool op)
        {
            // sıçrayarak smaç smaç karışımını kullanır ama kuyruğu dolu (Dolu): önbellek anahtarına girer
            string k = VurusAnahtar(type, q, slice, super_, serve, op);
            var GS = Ayar.GS; bool jump = type == "jump"; type = Tur(type); bool dolu = super_ || jump;
            AudioClip[] v; if (!vses.TryGetValue(k, out v))
            {
                v = new AudioClip[2];
                for (int j = 0; j < 2; j++) { var s = new Synth(dolu ? .65f : .45f, seed++); VurusCal(s, type, q, slice, super_, serve, GS.ton, GS.kafa, jump); v[j] = op ? s.Clip("vurus_op", .5f, .03f, 3000) : s.Clip("vurus"); }
                vses[k] = v;
            }
            return v[rnd.Next(2)];
        }
        // 2026-11-21 (yasin: "benim daha önce eklediğim gerçekçi sesleri ekle"): 5 Kasım kaydından kesilen vuruşlar Resources/ses/vurus_*
        // olarak girdi (smaç ×3, clear, drive, drop ×2, servis, slice); Hit önce kaydı çalar (Kayit: ad, sonra ad_1…_8), yoksa bu sentez.
        // Düzey: smaç tepe −6 dBFS, clear/drive −8, slice −11, servis −12, drop −14 (sentezin tür sıralaması). Net ve blok Tur ile
        // drop/clear kaydına düşer.
        // 2026-11-05 ses turu (yasin: "tatminkar ve gerçeğe yakın"): yasin'in örnek kayıtları yalnız ölçü olarak kullanıldı
        // (lisansı belirsiz, oyuna girmez). Kayıtlarda enerjinin çoğu 0,5–2 kHz'te ve 0,2–0,5 sn kuyruk var; eski sentez
        // 80 ms, tiz gürültü ağırlıklıydı. Eklenenler: tel yatağının alt modu (≈780 Hz) ve çerçeve gövdesi (1,1 kHz),
        // beşinci tel kısmisi, smaçta kuru "pat", salon kuyruğu; sıçrayarak/süper smaçta dolu gövde (175 Hz) ve takip
        // savruluşu. Tür/isabet/slice çarpanları ve rakip süzgeci aynı kural.
        void VurusCal(Synth A, string type, string q, bool slice, bool super_, bool serve, float ton, float kafa, bool jump)
        {
          var T = HITMIX[type]; bool dolu = super_ || jump;
          float a = T.a * (super_ ? 1.3f : 1) * (serve ? .55f : 1), str = T.str, frm = T.frm, crk = T.crk, ring = T.ring, hiss = T.hiss, pat = T.pat;
          if (q == "poor") { str *= .22f; frm *= 2.2f; crk *= .3f; ring *= .4f; pat *= .4f; } else if (q == "perfect") { str *= 1.15f; frm *= .6f; crk *= 1.15f; pat *= 1.15f; }
          if (slice) { str *= .55f; crk *= .25f; frm *= .7f; pat *= .4f; }
          float bus = a * 1.2f, f0 = 1480 * ton * (1 + A.R() * .025f); float[] m = { 1, 1.63f, 2.41f, 3.3f, 4.37f }, g = { .2f, .13f, .1f, .07f, .045f };
          // tel yatağı: beş kısmi, tiz kısmiler eskisinden güçlü (parlak "ping")
          for (int i = 0; i < 5; i++) A.Decay(0, f0 * m[i], "sine", g[i] * str * bus, (.11f - i * .017f) * ring / ton, 1.045f);
          // ani çatlama: çok kısa highpass gürültü (tel yatağının topa çarpışı) + bandpass "pat" (smaçın kuru şaklaması)
          A.Burst(0, .003f, "bandpass", 4600 * ton, .5f * str * bus, .8f, 2);
          // tel yatağının alt modu (≈780 Hz) ve çerçeve gövdesi: gerçek raket sesinin enerjisi 0,5–2 kHz'te
          A.Decay(0, 780 * Mathf.Sqrt(ton) * kafa, "sine", .3f * str * bus * (type == "drop" ? .35f : 1), .05f * ring / ton + .02f, 1.06f);
          A.Burst(0, .012f, "bandpass", 1100 * kafa, .5f * bus * (.4f + .6f * frm), 1.2f, 3);
          if (crk > 0) A.Burst(0, .012f, "bandpass", 4300 * Mathf.Sqrt(ton), .55f * crk * bus, 1.1f, 4);
          if (pat > 0) A.Burst(0, .009f, "bandpass", 2900 * Mathf.Sqrt(ton), .7f * pat * bus, 1.6f, 3);
          // kafa/çerçeve
          A.Decay(0, 235 * kafa, "triangle", .16f * frm * bus, .04f, 1.15f); A.Burst(0, .025f, "lowpass", 380 * kafa, .25f * frm * bus, .8f, 3);
          if (q == "poor") A.Decay(0, 640 * kafa, "square", .1f * bus, .06f, 1.1f);
          // mantar tık + tüy hışırtısı
          A.Burst(.001f, .006f, "bandpass", 1500, .18f * bus, 2, 2);
          if (hiss > 0) A.Burst(.003f, .03f, "bandpass", 6800, .05f * hiss * bus, .7f, 2);
          if (dolu)
          {
            // sıçrayarak / süper smaç: dolu gövde (180 Hz) + takip savruluşu (raket havayı yarar, 3 kHz → 1,1 kHz iner)
            A.Decay(0, 175 * kafa, "sine", .1f * bus, .09f, 1.3f);
            A.NoiseSweep(.012f, .26f, 3200, 1000, 1.4f, .1f * bus, true, u => Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Min(1, u * 1.3f)), 1.5f));
          }
          if (slice)
          {
            A.NoiseSweep(0, .07f, 2200 * ton, 5200 * ton, 3, .2f * bus, true, u => Mathf.Sin(Mathf.PI * u));
            for (int k = 0; k < 4; k++) A.Burst(.08f + k * .028f, .014f, "bandpass", 4800, (.05f - k * .01f) * bus, 1, 2);
          }
          // salon: smaçta kısa ve kuru, sıçramada uzun ve dolu
          A.Salon(T.wet * (dolu ? 1.5f : 1), T.rt * (dolu ? 1.7f : 1), .35f);
        }

        AudioClip Bir(string k, System.Action<Synth> f, float sec)
        {
            AudioClip c; if (cache.TryGetValue(k, out c)) return c; var s = new Synth(sec, seed++); f(s); c = s.Clip(k); cache[k] = c; return c;
        }
        void Ton(float f, float d, string type, float v, float delay = 0) { src.PlayOneShot(TonKlip(f, d, type, v, delay)); }
        AudioClip TonKlip(float f, float d, string type, float v, float delay = 0) { return Bir("t" + f + d + type + v + delay, s => s.Tone(delay, f, d, type, v), d + delay + .05f); }
        void Gurultu(float d, string type, float f, float v, float q = 1, float delay = 0) { src.PlayOneShot(GurultuKlip(d, type, f, v, q, delay)); }
        AudioClip GurultuKlip(float d, string type, float f, float v, float q = 1, float delay = 0) { return Bir("n" + d + type + f + v + q + delay, s => s.Burst(delay, d, type, f, v, q, 4), d + delay + .05f); }
        void Arpej(float f0, int[] semi, float d, float v, float ara) { src.PlayOneShot(ArpejKlip(f0, semi, d, v, ara)); }
        AudioClip ArpejKlip(float f0, int[] semi, float d, float v, float ara) { return Bir("a" + f0 + string.Join(",", semi) + d + v + ara, s => { for (int i = 0; i < semi.Length; i++) s.Tone(i * ara, f0 * Mathf.Pow(2, semi[i] / 12f), d, "triangle", v); }, semi.Length * ara + d + .05f); }

        // ---- olaylar (HTML çağrı yerleri) ----
        // hiz: topun çıkış hızı (m/s; 0 = bilinmiyor, eski düzey). Güç katsayısı p: 12 m/s drop ≈ 0, 60 m/s smaç = 1.
        // Konum panı (inceleme 2026-11-11, fikir 20:05 #6): vuruş, file ve top düşüşü ekrandaki yatay konumdan hafifçe sağa/sola
        // kayar; kulaklıkta topun hangi köşeye gittiği sesle de duyulur. Sınır ±PAN_AZAMI: hoparlörde fark edilmez, rahatsız etmez.
        // 3B ses (spatialBlend) yerine pan: kamera her ekranda yeniden çözülüyor, mesafe zayıflaması rakip vuruşunu boğardı.
        // Oyun her olaydan önce yazar (GameRoot.OnEvent); menü sesleri src'de, panlanmaz. Mono ses (iOS Erişilebilirlik) sistemce birleşir.
        public const float PAN_AZAMI = .35f;
        public float pan;
        public void Hit(string type, string q, bool slice, bool super_, bool serve, bool mine, int rally, bool flow, float hiz = 0)
        {
            float p = hiz > 0 ? Mathf.Clamp01((hiz - 12) / 48f) : .5f, kaz = .8f + .35f * p, perde = 1.03f - .06f * p + (float)(rnd.NextDouble() - .5) * .02f;
            // Ses eşlemesi (yasin 2026-11-21: "sesler karışmış, hangi ses hangi durumda netleştirelim"): kayıt vuruş TÜRÜNE göre seçilir.
            // Kesik (slice) kendi kaydını seçmiyor: vurus_slice drop kaydının aynı kesimiydi, kesik clear/drive de yumuşak drop sesi
            // çalıyordu. Kesik vuruş kendi türünün kaydını çalar; kesik his tüyün yolunda ve sentez yedeğinde kalır.
            // Sıçrayarak smaçın kendi kaydı var (yasin 2026-11-21: eski drive kesimi "sıçrayarak smaça daha uygun"); yoksa smaç kaydı.
            string t = Tur(type), kayitAd = serve ? "vurus_servis" : type == "jump" && Kayit("vurus_jump") != null ? "vurus_jump" : "vurus_" + t;
            var kv = Kayit(kayitAd + "_" + q) ?? Kayit(kayitAd);
            if (kv != null) { var c = kv[rnd.Next(kv.Length)]; if (mine) VurusCal(c, kaz, perde); else { opSrc.pitch = perde; opSrc.panStereo = pan; opSrc.PlayOneShot(c, .5f * kaz); } }
            else if (mine) VurusCal(Vurus(type, q, slice, super_, serve, false), kaz, perde);
            else { var k = Kaynak(); k.pitch = perde; k.panStereo = pan; k.PlayOneShot(Vurus(type, q, slice, super_, serve, true), kaz); }
            if (mine && !serve && (type == "smash" || type == "jump" || type == "kill")) Govde(.5f + .5f * p * (super_ ? 1.3f : 1));
            // Gerçek kayıt çalınca vuruş anında yalnız raket sesi (+ smaçta gövde) duyulur. Ralli notası, 6. vuruştan sonraki
            // davul, smaç düdüğü ve 5'te bir ton gerçek raket sesinin üstüne binip "karışık" duyuluyordu (yasin 2026-11-21).
            // Kaydı olmayan (sentez) yolda bu katmanlar eskisi gibi çalar.
            if (kv != null) return;
            RallyNote(rally - 1, mine, mine && flow);
            if (rally >= 6) Kick(.12f + Mathf.Min(.12f, rally * .008f));
            if (Tur(type) == "smash" && (type == "smash" || type == "jump")) Whistle();
            if (rally > 2 && rally % 5 == 0) Ton(523 * Mathf.Pow(2, Mathf.Min(rally, 30) / 24f), .2f, "triangle", .07f);
        }
        AudioSource Kaynak() { var k = vurusSrc[vurusSira]; vurusSira = (vurusSira + 1) % vurusSrc.Length; return k; }
        void VurusCal(AudioClip c, float kaz, float perde) { var k = Kaynak(); k.pitch = perde; k.panStereo = pan; k.PlayOneShot(c, kaz); }
        // Gövde darbesi: alçak sinüs süpürmesi, iki düzeyde önbellekte (kazanç oynatmada).
        void Govde(float v) { if (Kayitli("govde", v)) return; src.PlayOneShot(Bir("govde", s => s.Sweep(0, .09f, "sine", t => 80 * Mathf.Pow(45f / 80, Mathf.Min(1, t / .07f)), t => .32f * (t < .002f ? t / .002f : Mathf.Pow(.0005f, Mathf.Min(1, (t - .002f) / .08f)))), .1f), Mathf.Min(1.3f, v)); }
        // Seyirci "ooo": geniş formant (380 → 620 Hz), 0,9 sn kabarıp söner. 2026-11-19'dan beri rallide çağrılmıyor (aşağıda CrowdStart notu).
        void Ooo(float k) { if (Kayitli("ooo", .5f + .5f * k, trb)) return; trb.PlayOneShot(Bir("ooo", s => { s.NoiseSweep(0, .9f, 380, 620, 1.6f, .09f, false, u => Mathf.Sin(Mathf.PI * Mathf.Min(1, u * 1.3f)) * (1 - u * .4f)); s.NoiseSweep(0, .9f, 900, 1150, 2.2f, .035f, false, u => Mathf.Sin(Mathf.PI * Mathf.Min(1, u * 1.3f))); }, .95f), .5f + .5f * k); }
        // Alkış: rastgele zamanlı el çırpışları (her biri 4–7 ms gürültü, bandpass 900–2600 Hz), yoğunluk başta yüksek, sonda
        // seyrek. Ralli uzunluğuna göre iki boy (kısa 1,4 sn, uzun 2,4 sn); her boy iki çekimle önbellekte.
        // Art arda sayı (GameRoot.SeriSay): uzun ralli alkışıyla aynı örnek, 5+ seride uzun boy.
        public void SeriAlkis(bool uzun) { Alkis(uzun); }
        // Ayarlar › Seyirci örneği (2026-11-22 inceleme, fikir 17:06 #3): seçilen düzeyde tek kısa alkış. Düzey Update'i beklemeden
        // yazılır (Kapalı → Az ilk karede sessiz kalmasın). Kayıt yoksa yalnız menüde ısıtılmış sentez çalar; ısınmadıysa susar
        // ki dokunuşta 1,4 sn'lik klip sentezlenmesin (takılma kuralı).
        public void SeyirciOrnek()
        {
            if (!Ayar.ses || Ayar.seyirci <= 0) return;
            trb.volume = Ayar.SeyirciK;
            if (Kayitli("alkis", 1, trb)) return;
            AudioClip c; if (cache.TryGetValue("alkis" + false + rnd.Next(2), out c)) trb.PlayOneShot(c, .9f);
        }
        void Alkis(bool uzun)
        {
            if (Kayitli(uzun ? "alkis_uzun" : "alkis", 1, trb)) return;
            trb.PlayOneShot(AlkisKlip(uzun, rnd.Next(2)), .9f);
        }
        AudioClip AlkisKlip(bool uzun, int cek)
        {
            float sure = uzun ? 2.4f : 1.4f;
            return Bir("alkis" + uzun + cek, s =>
            {
                var r = new System.Random(uzun ? 41 + cek : 17 + cek); int n = uzun ? 260 : 130;
                for (int i = 0; i < n; i++)
                {
                    float u = (float)Mathf.Pow((float)r.NextDouble(), 1.7f), t = u * sure;
                    s.Burst(t, .004f + (float)r.NextDouble() * .003f, "bandpass", 900 + (float)r.NextDouble() * 1700, (.05f + (float)r.NextDouble() * .05f) * (1 - u * .6f), 1.2f, 2);
                }
            }, sure + .1f);
        }
        // Ekipman ekranında rakete/tele dokununca o takımın smaç sesi (HTML racketHit({type:'smash',q:'perfect'})).
        public void Deneme() { src.PlayOneShot(Vurus("smash", "perfect", false, false, false, false)); }
        void RallyNote(int n, bool mine, bool harm) { src.PlayOneShot(RallyKlip(n, mine, harm)); }
        AudioClip RallyKlip(int n, bool mine, bool harm)
        {
            int i = Mathf.Min(Mathf.Max(0, n), 14), semi = i / 5 * 12 + PENTA[i % 5]; float f = (mine ? 392 : 196) * Mathf.Pow(2, semi / 12f);
            return Bir("r" + f + mine + harm, s => { s.Tone(0, f, .32f, "triangle", mine ? .05f : .03f); s.Tone(.01f, f * 2, .18f, "sine", mine ? .02f : .01f); if (harm) { s.Tone(.04f, f * 1.5f, .4f, "sine", .03f); s.Tone(.08f, f * 2.52f, .3f, "sine", .015f); } }, .5f);
        }
        // Tek klip (İnceleme turu 2026-11-10 17:08 #4): eskiden her güç değeri ayrı klipti ("k" + v), ralli 6–15 arası her vuruş
        // yeni bir tampon sentezliyordu. Klip en güçlü değerde (.24) bir kez kurulur, güç PlayOneShot kazancıyla verilir; zarf
        // .0001 tabanına üstel indiği için .12–.24 arasında eğri farkı duyulmaz.
        const float KICK_V = .24f;
        AudioClip KickKlip() { return Bir("k", s => s.Sweep(0, .25f, "sine", t => 130 * Mathf.Pow(42f / 130, Mathf.Min(1, t / .14f)), t => KICK_V * Mathf.Pow(.0001f / KICK_V, Mathf.Min(1, t / .22f))), .26f); }
        void Kick(float v) { src.PlayOneShot(KickKlip(), Mathf.Clamp01(v / KICK_V)); }
        void Whistle() { if (Kayitli("duduk", .6f)) return; src.PlayOneShot(Bir("duduk", s => s.Sweep(0, .52f, "sine", t => 2600 * Mathf.Pow(700f / 2600, Mathf.Min(1, t / .45f)), t => .025f * Mathf.Pow(.004f, Mathf.Min(1, t / .5f))), .53f)); }
        public void Squeak() { if (Kayitli("gicirti", .8f)) return; src.PlayOneShot(Bir("gicirti", s => s.Sweep(0, .13f, "sine", t => t < .06f ? Mathf.Lerp(2300, 3400, t / .06f) : Mathf.Lerp(3400, 2800, Mathf.Min(1, (t - .06f) / .04f)), t => .025f * Mathf.Pow(.004f, Mathf.Min(1, t / .12f))), .14f)); }
        // Yer (2026-11-05): eski 50 ms boğuk gürültü yerine mantarın tıkırtısı + gövde, tüy topun sekip ikinci/üçüncü
        // küçük vuruşu (125 ve 220 ms) ve tüylerin yere yatma hışırtısı; kayıttaki gibi enerji 0,5–1 kHz'te.
        public void Floor(bool big) { yerSrc.panStereo = pan; if (Kayitli(big ? "yer_sert" : "yer", big ? 1 : .7f, yerSrc)) { if (big) Kick(.18f); return; } yerSrc.PlayOneShot(Bir(big ? "yer_s" : "yer_y", sy => YerCal(sy, big), .5f), big ? .8f : .6f); if (big) Kick(.18f); }
        static void YerCal(Synth A, bool big)
        {
          float k = big ? 1.5f : 1;
          // ilk temas: mantarın tıkırtısı (3 kHz) + hafif gövde; tüy top seker, ikinci küçük vuruş, sonra tüyler üstüne yatar
          float[] t = { 0, .125f, .22f }, v = { 1, .33f, .12f };
          for (int i = 0; i < 3; i++)
          {
            A.Burst(t[i], .003f, "bandpass", 2800 * (1 + i * .06f), .25f * v[i] * k, 1.4f, 2);
            A.Burst(t[i], .014f, "bandpass", 720 * (1 + i * .05f), .9f * v[i] * k, 1.5f, 3);
            A.Decay(t[i], 610 * (1 + i * .08f), "sine", .14f * v[i] * k, .03f);
            A.Burst(t[i], .025f, "lowpass", 600, .1f * v[i] * k, .8f, 3);
          }
          A.NoiseSweep(.12f, .1f, 4800, 7000, 1.6f, .025f * k, true, u => Mathf.Sin(Mathf.PI * u));
          A.Salon(.4f, .45f, .4f);
        }
        // file: k 0 drop … 1 smaç; smaçta ağ "puf" diye yutar, dropta yalnız hışırtı; bant kordon tınlar
        public void Net(float k, bool tape)
        {
            yerSrc.panStereo = pan; if (Kayitli(tape ? "file_bant" : "file", .5f + .5f * k, yerSrc)) return;
            yerSrc.PlayOneShot(Bir("file" + k.ToString("0.0") + tape, s =>
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
            if (delay == 0 && Kayitli("ic_cekis", v / .09f, trb)) return;
            trb.PlayOneShot(GaspKlip(v, delay));
        }
        AudioClip GaspKlip(float v, float delay) { return Bir("ic" + v + delay, s => s.NoiseSweep(delay, .8f, 380, 900, 2, v, false, u => u < .1875f ? u / .1875f : Mathf.Pow(.0001f, (u - .1875f) / .8125f)), delay + .85f); }
        public void Cheer(float v)
        {
            if (Kayitli("cosku", v / .18f, trb)) return;
            trb.PlayOneShot(CheerKlip(v));
        }
        AudioClip CheerKlip(float v) { return Bir("cosku" + v, s => { var b = new Synth.Biquad("bandpass", 1200, .6f); int n = (int)(Synth.SR * 1.2f); for (int i = 0; i < n; i++) s.buf[i] += b.Run(s.R() * Mathf.Sin(Mathf.PI * i / n) * (.6f + .4f * Mathf.Sin(i / 900f))) * v; }, 1.25f); }
        // ralli: sayının vuruş sayısı (0 = bilinmiyor). Uzun ralliyi seyirci alkışlar (kimin aldığından bağımsız; rakibin
        // sayısında kısa alkış, benim sayımda sevinçle birlikte).
        public void Point(bool mine, bool smacSayisi, int ralli = 0)
        {
            // Maç sayısı rallisi bitti (bayrak SalonCanli'nin bir sonraki karesinde değişir): susan salon kazanana göre patlar ya da
            // nefesini tutar. Gerilim sessizlikten, ödül patlamadan (2026-11-11 inceleme #1).
            if (macSayisi) { if (mine) Cheer(.24f); else Gasp(.08f); }
            if (ralli >= 10) Alkis(ralli >= 18);
            if (mine && Kayitli("sayi_ben")) { if (smacSayisi) Gasp(.09f); return; }
            if (!mine && Kayitli("sayi_rakip")) return;
            if (mine) { if (ralli < 10) Cheer(.14f); Arpej(523, SAYI_ARPEJ, .18f, .08f, .07f); if (smacSayisi) Gasp(.09f); }
            else Ton(220, .25f, "sawtooth", .05f);
        }
        // İmza vuruşu hazırlığı (2026-10-26, Core/Imza): kare + üçgen iki ton, perde imzaya göre (ses de imzayı tanıtır,
        // yalnız ışığa güvenilmez); okunan imza: kısa yükselen arpej (görev arpejinden bir oktav yukarıda başlar).
        public void Imza(string id) { float f = id == "yildirim" ? 440 : id == "duvarblok" ? 330 : id == "tuldrop" ? 587 : id == "mermi" ? 392 : 494; Ton(f, .14f, "square", .035f); Ton(f * 1.5f, .2f, "triangle", .05f, .1f); }
        public void Okudun() { Arpej(784, new[] { 0, 4, 7, 12 }, .12f, .05f, .05f); }
        public void Aldat() { Ton(740, .1f, "triangle", .06f); Ton(988, .12f, "triangle", .06f, .08f); }
        public void Split() { Ton(1200, .04f, "square", .03f); }
        public void Perfect(int pstreak) { src.PlayOneShot(PerfectKlip(pstreak)); }
        AudioClip PerfectKlip(int pstreak) { return TonKlip(660 * Mathf.Pow(2, Mathf.Min(Mathf.Max(0, pstreak), 12) / 12f), .12f, "sine", .06f, .02f); }
        public void Akis() { Arpej(523, new[] { 0, 2, 4, 7, 9, 12 }, .25f, .05f, .06f); }
        // Sonsuz ralli (2026-11-06 Ar-Ge 22:03 #2): rekoru geçtiğin an yükselen iki nota; kademe atlayınca kısa yükselen tık.
        public void Rekor() { Ton(880, .1f, "triangle", .05f); Ton(1320, .18f, "triangle", .055f, .08f); }
        public void Hizlandi() { Ton(990, .04f, "square", .025f); Ton(1320, .05f, "square", .025f, .05f); }
        public void AkisSondu() { Arpej(784, new[] { 0, -5 }, .16f, .028f, .09f); }   // Akış arpejinin tersi, kısık: ceza değil, "söndü" (2026-11-06)
        public void Gorev() { Arpej(660, new[] { 0, 4, 7, 12, 16 }, .18f, .05f, .07f); }
        public void OgrTamam() { Arpej(660, new[] { 0, 4, 7 }, .14f, .05f, .06f); }
        public void Yukselt() { Arpej(523, new[] { 0, 4, 7, 12 }, .14f, .05f, .06f); }
        public void Tik(float f = 880) { Ton(f, .05f, "triangle", .04f); }
        // 2026-11-13 inceleme #2: tel kaydırıcısında her tam lbs bir çınlama; perde gerginlikle bir oktav yükselir (18 lbs tok,
        // 32 lbs tiz). Gerçek oyuncu gerginliği sesinden tanır. 15 ayrı perde: klipler Bir() önbelleğinde kalır.
        public void TelTik(float lbs) { float k = Mathf.Clamp01((lbs - 18) / 14f); Ton(Mathf.Round(520 * Mathf.Pow(2, k)), .04f, "triangle", .028f); }
        // Smaç hazır (2026-11-11): iki kısa yükselen ton (E6 → A6), perfect tonundan tiz ve kısa; vuruş sesinin altında kalmasın diye hafif.
        public void SmacHazir() { Ton(1319, .06f, "sine", .05f); Ton(1760, .09f, "sine", .05f, .06f); }
        // Dans imzaları (2026-11-12 inceleme, fikir 12:05 #1): #88'in altı dansı yalnız görüntüydü; "her olay üç kanal" kuralının dışında kalan tek
        // kozmetik ailesi, Dolap'taki alım kararı da sessiz önizlemeye bakıyordu. Her dansa kendi sentezimizle (kayıt yok, CC0 kuralı) 1–2 vuruşluk motif;
        // seçildiğinde bir kez (döngü her turda tekrar etmez, sinir etmesin) ve maç sonu zirvesinde bir kez. Sayı arası sevinçlerinde çalmaz (sayı arası kısa).
        // Seviyeler Tik/SmacHazir düzeyinde (.03–.05): vuruş ve alkışın altında kalır. Hareketi azaltta ses sürer.
        public void Dans(string id)
        {
            switch (id)
            {
                case "robot": Ton(880, .06f, "square", .03f); Ton(660, .08f, "square", .03f, .12f); break;                                   // iki bip
                case "tavuk": Ton(620, .05f, "square", .028f); Ton(820, .04f, "square", .026f, .07f); Ton(560, .07f, "square", .028f, .14f); break;   // gıdak
                case "twist": Ton(523, .12f, "triangle", .05f); Ton(659, .16f, "triangle", .05f, .18f); break;                              // iki notalık swing
                case "dalga": Arpej(523, new[] { 0, 4, 7, 12 }, .12f, .04f, .06f); Gurultu(.3f, "bandpass", 2500, .03f, 1, .05f); break;   // yükselen + alkış hışırtısı
                case "helikopter": Gurultu(.06f, "lowpass", 320, .07f); Gurultu(.06f, "lowpass", 320, .07f, 1, .11f); Gurultu(.06f, "lowpass", 320, .07f, 1, .22f); break;   // vuvuvu
                case "gitar": Arpej(196, new[] { 0, 7, 12, 16, 19 }, .4f, .035f, .025f); break;                                            // açık akor tıngırtısı
                case "fiskiye": Gurultu(.03f, "highpass", 4000, .04f); Gurultu(.03f, "highpass", 4000, .04f, 1, .17f); Gurultu(.03f, "highpass", 4000, .04f, 1, .34f); break;   // tsk tsk tsk (DansIsit ile aynı anahtarlar)
                case "ucak": Ton(392, .3f, "triangle", .04f); Ton(523, .3f, "triangle", .04f, .15f); break;                                  // vınn (iki ton yükselir)
                case "yildiz": Arpej(784, new[] { 0, 7, 12 }, .07f, .04f, .05f); break;                                                 // boing arpeji
                case "aglak": Ton(440, .14f, "triangle", .045f); Ton(349, .2f, "triangle", .045f, .16f); break;   // vah vah (iki inen ton)
                case "kulak": Ton(523, .07f, "triangle", .04f); Ton(784, .1f, "triangle", .04f, .09f); break;    // "ha?" (yükselen soru)
                case "toz": Gurultu(.05f, "highpass", 3000, .035f); Gurultu(.05f, "highpass", 3000, .035f, 1, .14f); break;   // iki fırça
                case "esne": Ton(392, .25f, "triangle", .04f); Ton(294, .35f, "triangle", .04f, .2f); break;     // uzun esneme (inen ton)
                // 2026-11-20 yeni danslar ve kışkırtmalar (DansIsit ile aynı parametreler)
                case "kivir": Ton(523, .06f, "triangle", .04f); Ton(659, .06f, "triangle", .04f, .1f); Ton(784, .08f, "triangle", .04f, .2f); break;   // üç adım yukarı
                case "disko": Ton(440, .07f, "square", .028f); Ton(880, .09f, "square", .028f, .14f); break;                                       // oktav sıçrayışı
                case "nanik": Ton(784, .08f, "triangle", .045f); Ton(659, .08f, "triangle", .045f, .1f); Ton(784, .08f, "triangle", .045f, .2f); Ton(587, .14f, "triangle", .045f, .3f); break;   // "na-na-na-naa"
                case "gelgel": Ton(392, .07f, "triangle", .04f); Ton(523, .1f, "triangle", .04f, .1f); break;                                     // iki çağrı
                case "opucuk": Gurultu(.04f, "highpass", 4000, .04f); Ton(1175, .08f, "sine", .04f, .06f); break;                                  // öpücük sesi
                case "cikcik": Gurultu(.025f, "highpass", 2500, .04f); Gurultu(.025f, "highpass", 2500, .04f, 1, .16f); break;                     // cık cık
            }
        }
        public void Iyi() { Ton(784, .12f, "triangle", .05f); }
        public void Hedefte() { Ton(1320, .1f, "triangle", .05f); }
        public void Iska() { if (Kayitli("iska", .6f)) return; Gurultu(.05f, "highpass", 1200, .05f); }
        public void MacSonu(bool won) { crowdHedef = 0; SuAlti(false); if (won) Cheer(.18f); }
        // Vuruş penceresi: ses su altındaymış gibi boğulur (lowpass 750 Hz), vuruşla açılır; girişte alçak bir "vuuu".
        public void SuAlti(bool on) { if (on && suHedef > 1000) Gurultu(.25f, "lowpass", 300, .08f); suHedef = on ? 750 : 22000; }
        public void Ugultu(bool on, float sp) { swo.Ayarla(on, sp); }

        // ---- kayıtlı ses kancası (yukarıdaki başlık notu) ----
        public static readonly string[] KAYIT_ADLARI = { "vurus_smash", "vurus_jump", "vurus_clear", "vurus_drive", "vurus_drop", "vurus_servis", "govde", "file", "file_bant", "bant", "yer", "yer_sert",
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
        bool Kayitli(string ad, float kaz = 1, AudioSource k = null) { var v = Kayit(ad); if (v == null) return false; (k ?? src).PlayOneShot(v[rnd.Next(v.Length)], Mathf.Min(1.5f, kaz)); return true; }

        void Update()
        {
            IsitIsle();
            float dt = Time.unscaledDeltaTime; bool sessiz = !Ayar.ses;
            trb.volume = Ayar.SeyirciK;
            crowd.volume += ((sessiz ? 0 : crowdHedef * 3.5f * Ayar.SeyirciK) - crowd.volume) * (1 - Mathf.Exp(-dt / .4f));   // Web Audio setTargetAtTime(.4)
            float tc = suHedef < 1000 ? .05f : .02f; suAlti.cutoffFrequency += (suHedef - suAlti.cutoffFrequency) * (1 - Mathf.Exp(-dt / tc));
            MuzikIsle(dt);
        }
        // Menü müziği: açılır .8 sn'de (yumuşak giriş), söner .35 sn'de (perdenin kapanışı ≈.4 sn; maç sesi müziğin üstüne
        // binmesin). Sönünce duraklatılır, menüye dönünce kaldığı yerden sürer. Başka uygulamanın sesi 2 sn'de bir sorulur:
        // oyuncu menüdeyken Spotify'ı açarsa müzik çekilir, kapatırsa geri gelir.
        int muzikPos;
        void MuzikKonum(int p) { muzikPos = p; }
        void MuzikOku(float[] o)
        {
            var v = muzikVeri; int n = v.Length, p = muzikPos;
            for (int i = 0; i < o.Length; i++) { o[i] = v[p]; if (++p >= n) p = 0; }
            muzikPos = p;
        }
        void MuzikIsle(float dt)
        {
            if (muzik == null)
            {
                if (!muzikHazir) return;
                // Akış klibi (2026-11-16 inceleme, fikir 17:05 #2; yasin 15 Kas "takılma yaşamamalıyız"): ~51 sn × 44,1 kHz = ~2,27 M
                // örnek eskiden SetData ile ana iş parçacığında TEK karede klibe kopyalanıyordu; bu kare menünün ilk açılışına
                // (afiş, perde, sekme girişi) denk geliyordu ve dizi + klip birkaç saniye iki kopya (~18 MB) yaşıyordu. Şimdi klip
                // diziden okur (PCMReaderCallback, istek başına birkaç bin örnek): SetData yok, kopya yok, döngü konumu MuzikKonum.
                var c = AudioClip.Create("menu_muzik", muzikVeri.Length, 1, Synth.SR, true, MuzikOku, MuzikKonum);
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
        // Maç sayısında salon susar (GameRoot.SalonCanli). 2026-11-11 inceleme #1: Hit her vuruşta crowdHedef'i yeniden yazdığı
        // için sessizlik yalnız servis beklerken sürüyor, ralli uzadıkça uğultu ve "Ooo" geri geliyordu; bayrak açıkken Hit
        // uğultuya ve Ooo'ya dokunmaz. Ralli tonları ve raket sesi kalır: sessiz salonda daha net duyulur.
        bool macSayisi;
        // 2026-11-19 (yasin: "Maç içinde beyaz gürültü sesi geliyor. Badminton maçları sesli olmaz. Sadece sayı olursa vs olur."):
        // tribün uğultusu döngüsü (.01'den ralliyle .07'ye) ve uzun rallide her 4 vuruşta "Ooo" kalktı. Gerçek salonda ralli sırasında
        // seyirci susar, ses yalnız sayıda gelir (alkış, coşku, iç çekiş Point'te kalır). Uğultu kaynağı sessiz kalır (0).
        public void CrowdStart() { crowdHedef = 0; macSayisi = false; }
        public void CrowdSus() { crowdHedef = 0; macSayisi = true; }
        // Rakibin balıklamayla kurtarışı (Ev.Kurtaris): parkeye düşen gövdenin kısık "küt"ü + tribün nefesi. Anın tek sessiz
        // büyük hareketiydi (2026-11-11 inceleme #4). Titreşim yok: rakibin hareketi elde hissedilmez.
        public void Kurtaris() { Govde(.35f); Gasp(.06f); }
        // Oyuncunun kendi balıklaması (2026-11-13 inceleme, fikir #1): rakibinki üç kanallıydı, en zor savunma hareketin
        // sessizdi. Gövdenin hafif "küt"ü + ayakkabı sürtmesi; tribün nefesi yok (o rakibin anı, ayrışsın).
        public void Uzan() { Govde(.22f); Squeak(); }

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
        // 2026-11-19 (aynı istek): parmak hızlı kaydıkça sürekli duyulan hışırtı da "beyaz gürültü" gibi geliyordu. Tavan .22 → .10;
        // GameRoot yalnız vuruş penceresinde hız verir (kısa savuruş sesi, sürekli değil).
        public void Ayarla(bool on, float sp) { acik = on && Ayar.ses; hedefG = acik ? Mathf.Min(.10f, sp * .06f) : 0; hedefF = 500 + Mathf.Min(3, sp) * 900; }
        // Kaynak uygulama boyunca döngüdeydi; menüde de ses iş parçacığı her örnekte gürültü + bandpass hesaplıyordu (kazanç 0
        // olsa da). Uğultu kapalı ve sönmüşken duraklar, açılınca kaldığı yerden sürer (2026-11-11 inceleme #4c, pil/ısı).
        void Update()
        {
            if (acik) { if (!a.isPlaying) a.UnPause(); }
            else if (g < .0005f && a.isPlaying) a.Pause();
        }
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

    // "Tüy Pop" menü müziği. Geçmiş: 2026-10-30 yasin seçimi aday B (112 BPM kare pluck + sinüs davul, 12 ölçü ≈25,7 sn);
    // 2026-11-12 B bölümü (A-B ≈51 sn). 2026-11-19 (yasin: müziği "kendi sentezimizi geliştir" + "Enstrüman kullan"; ilk enstrümanlı
    // taslak FM piyano ve Em/Am ağırlığıyla "elektronik ve korkunç olmuş, sevimli bir ezgi olmalı"; ikinci taslağa "Ok ekle"):
    // "sevimli" sürüm, /mnt/project-files/birdie-smash/muzik/sevimli.py'nin birebir kopyası (dinleme: artifact EuBVYNUGm714u44RAKf4J3).
    // Do majör, 120 BPM, 36 ölçü = 72 sn: Giriş 4 · A 8 (ezgi ksilofon) · B 8 (ıslık) · A' 8 (çan + ksilofon) · Ara 4 (ukulele + el
    // çırpma) · Dönüş 4. Enstrümanlar fiziksel/kip modelleri, dosya ve lisans yok: ukulele ve pizzicato bas Karplus-Strong (kesirli
    // gecikmeli iki uçlu ortalama geri besleme, akort ±0), ksilofon (1, 3.93) ve çan (1, 2.76, 5.40) titreşim kipleri, ıslık sinüs +
    // geç vibrato, yumuşak kick, el çırpma, shaker; kısa sıcak oda yankısı (Schroeder: 4 sönümlü tarak + 2 tüm geçiren).
    // Dikişsiz: tampon 4 sn uzun, kuyruk başa eklenir. Sentez arka iş parçacığında (Sfx.Start); tampon 72 sn × 44,1 kHz ≈ 12,7 MB
    // (eskisi ≈9 MB). Rastgele sayı üreteci sabit tohumlu: her cihazda aynı parça.
    public static class MenuMuzik
    {
        static readonly Dictionary<char, int> NOT = new Dictionary<char, int> { { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 }, { 'G', 7 }, { 'A', 9 }, { 'B', 11 } };
        static int M(string s) { return 12 * (s[s.Length - 1] - '0' + 1) + NOT[s[0]] + (s.IndexOf('#') >= 0 ? 1 : 0); }
        static float Hz(int m) { return 440 * Mathf.Pow(2, (m - 69) / 12f); }
        const int SR = Synth.SR;
        static float[] buf, yank; static System.Random rnd;
        static float R() { return (float)(rnd.NextDouble() * 2 - 1); }
        static void Ekle(float t, float[] x, float v, float gonder = 0)
        {
            int i0 = Mathf.RoundToInt(t * SR), n = Mathf.Min(x.Length, buf.Length - i0);
            for (int i = 0; i < n; i++) { float s = x[i] * v; buf[i0 + i] += s; if (gonder > 0) yank[i0 + i] += s * gonder; }
        }
        // Karplus-Strong: D = SR/f, P = tam kısım, w = kesir; geri besleme P'de (1-w), P+1'de w (iki uçlu süzgecin gecikmesi w: toplam SR/f).
        static float[] Ks(float f, float sure, float rho, float parlak, float sustur)
        {
            float D = SR / f; int P = (int)D; float w = D - P; int n = (int)(sure * SR); var y = new float[n]; float lp = 0, tepe = 1e-9f;
            for (int i = 0; i < n; i++)
            {
                float ex = i <= P ? R() : 0; lp = parlak * ex + (1 - parlak) * lp;
                float v = lp + (i >= P ? rho * (1 - w) * y[i - P] : 0) + (i >= P + 1 ? rho * w * y[i - P - 1] : 0); y[i] = v;
            }
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, g = Mathf.Clamp01((sure - t) / .02f); if (sustur > 0 && t >= sustur) g *= Mathf.Exp(-(t - sustur) / .03f);
                y[i] *= g; tepe = Mathf.Max(tepe, Mathf.Abs(y[i]));
            }
            for (int i = 0; i < n; i++) y[i] /= tepe;
            return y;
        }
        static float[] Kip(float f, float sure, float[] oran, float[] gen, float[] tau, float atak)
        {
            int n = (int)(sure * SR); var y = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, s = 0; for (int k = 0; k < oran.Length; k++) s += gen[k] * Mathf.Sin(2 * Mathf.PI * f * oran[k] * t) * Mathf.Exp(-t / tau[k]);
                y[i] = s * Mathf.Min(1, t / atak) * Mathf.Clamp01((sure - t) / .02f);
            }
            return y;
        }
        static float[] Ksilofon(float f, float sure) { float s = Mathf.Sqrt(523 / f); return Kip(f, sure, new[] { 1f, 3.93f }, new[] { 1f, .25f }, new[] { .35f * s, .06f * s }, .0008f); }
        static float[] Can(float f, float sure) { return Kip(f, sure, new[] { 1f, 2.76f, 5.40f }, new[] { 1f, .22f, .08f }, new[] { .9f, .25f, .08f }, .002f); }
        static float[] Islik(float f, float sure)
        {
            int n = (int)(sure * SR); var y = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR; ph += f * (1 + .006f * Mathf.Sin(2 * Mathf.PI * 5.5f * t) * Mathf.Clamp01((t - .12f) / .15f)) / SR;
                float g = Mathf.Min(1, t / .03f) * Mathf.Clamp01((sure - t) / .06f);
                y[i] = (Mathf.Sin(2 * Mathf.PI * (float)(ph % 1)) + .08f * Mathf.Sin(4 * Mathf.PI * (float)(ph % 1))) * g;
            }
            return y;
        }
        static float[] Kick()
        {
            int n = (int)(.25f * SR); var y = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++) { float t = (float)i / SR; ph += (55 + 70 * Mathf.Exp(-t / .025f)) / SR; y[i] = Mathf.Sin(2 * Mathf.PI * (float)(ph % 1)) * Mathf.Exp(-t / .11f); }
            return y;
        }
        static float[] Alkis()
        {
            int n = (int)(.2f * SR); var y = new float[n]; var bq = new Synth.Biquad("bandpass", 1300, 1.1f); float[] d = { 0, .009f, .018f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, g = 0; for (int k = 0; k < 3; k++) if (t >= d[k]) g += Mathf.Exp(-(t - d[k]) / (k < 2 ? .006f : .05f));
                y[i] = bq.Run(R()) * g * 2.5f;
            }
            return y;
        }
        static float[] Shaker()
        {
            int n = (int)(.08f * SR); var y = new float[n]; var bq = new Synth.Biquad("bandpass", 6500, .9f);
            for (int i = 0; i < n; i++) { float s = Mathf.Sin(Mathf.PI * i / (float)n); y[i] = bq.Run(R()) * s * s; }
            return y;
        }
        static readonly Dictionary<string, string[]> UKE = new Dictionary<string, string[]> { { "C", new[] { "G4", "C4", "E4", "C5" } }, { "F", new[] { "A4", "C4", "F4", "A4" } }, { "G", new[] { "G4", "D4", "G4", "B4" } } };
        static readonly Dictionary<string, string> KOK = new Dictionary<string, string> { { "C", "C3" }, { "F", "F2" }, { "G", "G2" } };
        static readonly string[] A_AK = { "C", "F", "G", "C" }, B_AK = { "F", "C", "G", "C" };
        static readonly string[] EZ_A = { "G4 .5", "C5 .5", "E5 .5", "G5 .5", "E5 1", "C5 1", "A4 .5", "C5 .5", "F5 .5", "A5 .5", "G5 1", "F5 1",
            "G5 .5", "F5 .5", "E5 .5", "D5 .5", "B4 1", "D5 1", "C5 .5", "E5 .5", "G5 1", "C6 2",
            "G4 .5", "C5 .5", "E5 .5", "G5 .5", "E5 1", "C5 1", "A4 .5", "C5 .5", "F5 .5", "A5 .5", "G5 1", "F5 1",
            "D5 .5", "E5 .5", "F5 .5", "G5 .5", "A5 1", "B5 1", "C6 1", "G5 .5", "E5 .5", "C5 2" };
        static readonly string[] EZ_B = { "A5 1", "G5 .5", "F5 .5", "G5 1", "A5 1", "G5 1.5", "E5 .5", "C5 2", "D5 .5", "E5 .5", "F5 .5", "G5 .5", "A5 1", "G5 1", "G5 2", "E5 1", "C5 1",
            "A5 1", "G5 .5", "F5 .5", "G5 1", "A5 1", "G5 1.5", "E5 .5", "C5 2", "D5 .5", "E5 .5", "F5 .5", "G5 .5", "A5 1", "B5 1", "C6 1", "E5 1", "C5 2" };
        static readonly float[] TEL_B = { 0, 1, 1.5f, 2.5f, 3, 3.5f }, TEL_V = { 1, .7f, .55f, .55f, .75f, .5f };   // ukulele: aşağı aşağı yukarı yukarı aşağı yukarı
        struct Bolum { public int o0, n; public string[] ak; public string dv, ez; public Bolum(int a, int b, string[] c, string d, string e) { o0 = a; n = b; ak = c; dv = d; ez = e; } }
        static readonly Bolum[] BOLUM = { new Bolum(0, 4, A_AK, "shaker", null), new Bolum(4, 8, A_AK, "tam", "ksilofon"), new Bolum(12, 8, B_AK, "tam", "islik"),
            new Bolum(20, 8, A_AK, "tam", "can"), new Bolum(28, 4, A_AK, "alkis", null), new Bolum(32, 4, B_AK, "shaker", null) };

        public static float[] TuyPop()
        {
            float b = 60f / 120, OLC = 4 * b; int N = Mathf.RoundToInt(36 * OLC * SR);
            buf = new float[N + 4 * SR]; yank = new float[buf.Length]; rnd = new System.Random(7);
            var kick = Kick(); var govde = new Dictionary<string, float[]>();
            foreach (var bl in BOLUM)
                for (int k = 0; k < bl.n; k++)
                {
                    int o = bl.o0 + k; float t0 = o * OLC; string a = bl.ak[k % 4];
                    for (int s = 0; s < TEL_B.Length; s++)
                    {
                        float bt = TEL_B[s]; var tel = UKE[a]; bool yukari = bt != Mathf.Floor(bt);
                        for (int q = 0; q < 4; q++) Ekle(t0 + bt * b + q * .008f, Ks(Hz(M(tel[yukari ? 3 - q : q])), .5f, .992f, .85f, .32f), .055f * TEL_V[s], .15f);
                    }
                    float f0 = Hz(M(KOK[a]));
                    float[] bv = bl.dv != "shaker" ? new[] { 0f, 2f, 2.5f } : new[] { 0f, 2f };
                    foreach (float bt in bv)
                    {
                        Ekle(t0 + bt * b, Ks(f0, .4f, .985f, .4f, .3f), .30f);
                        float[] gv; if (!govde.TryGetValue(a, out gv)) { int nn = (int)(.4f * SR); gv = new float[nn]; for (int i = 0; i < nn; i++) { float t = (float)i / SR; gv[i] = Mathf.Sin(2 * Mathf.PI * f0 * t) * Mathf.Exp(-t / .18f) * Mathf.Min(1, t / .004f); } govde[a] = gv; }
                        Ekle(t0 + bt * b, gv, .22f);
                    }
                    for (int j = 0; j < 8; j++)
                    {
                        float tj = t0 + j * b / 2;
                        Ekle(tj + (j % 2 == 1 ? .02f : 0), Shaker(), j % 2 == 1 ? .05f : .035f);   // geç vuruş: kıpır kıpır his
                        if (bl.dv == "tam" && (j == 0 || j == 4)) Ekle(tj, kick, .42f);
                        if ((bl.dv == "tam" || bl.dv == "alkis") && (j == 2 || j == 6)) Ekle(tj, Alkis(), .16f, .2f);
                    }
                    if (bl.o0 == 0 && k == 3) Ekle(t0 + 3 * b, Can(Hz(M("G5")), 1.2f), .10f, .3f);   // girişten ana bölüme çan
                }
            foreach (var bl in BOLUM)
            {
                if (bl.ez == null) continue; float te = bl.o0 * OLC; var ezgi = bl.ez == "islik" ? EZ_B : EZ_A;
                foreach (var e in ezgi)
                {
                    var p = e.Split(' '); float d = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture) * b, f = Hz(M(p[0]));
                    if (bl.ez == "ksilofon") Ekle(te, Ksilofon(f, d + .35f), .22f, .2f);
                    else if (bl.ez == "islik") Ekle(te, Islik(f, d * .92f), .11f, .25f);
                    else { Ekle(te, Can(f, d + .6f), .13f, .25f); Ekle(te, Ksilofon(f / 2, d + .3f), .10f, .15f); }
                    te += d;
                }
            }
            var yk = Yanki(yank);
            var cikis = new float[N]; float tepe = 1e-6f;
            for (int i = 0; i < buf.Length; i++) { float x = buf[i] + yk[i] * .4f; cikis[i % N] += x; }
            for (int i = 0; i < N; i++) { cikis[i] = (float)System.Math.Tanh(cikis[i] * 1.3f) / 1.3f; tepe = Mathf.Max(tepe, Mathf.Abs(cikis[i])); }
            float g = .8f / tepe; for (int i = 0; i < N; i++) cikis[i] *= g;
            buf = yank = null; rnd = null;
            return cikis;
        }
        // Kısa sıcak oda: sönümlü tarak y[n] = x[n] - d·x[n-1] + d·y[n-1] + g(1-d)·y[n-D]; tüm geçiren y[n] = -g·x[n] + x[n-D] + g·y[n-D].
        static float[] Yanki(float[] x)
        {
            int n = x.Length; var top = new float[n]; int[] TD = { 1116, 1188, 1277, 1356 }; float[] TG = { .70f, .69f, .71f, .68f }; const float dd = .4f;
            var y = new float[n];
            for (int c = 0; c < 4; c++)
            {
                int D = TD[c]; float g = TG[c] * (1 - dd);
                for (int i = 0; i < n; i++) { float v = x[i] - (i > 0 ? dd * x[i - 1] - dd * y[i - 1] : 0) + (i >= D ? g * y[i - D] : 0); y[i] = v; top[i] += v; }
            }
            int[] AD = { 225, 341 };
            foreach (int D in AD)
            {
                var z = new float[n];
                for (int i = 0; i < n; i++) z[i] = -.5f * top[i] + (i >= D ? top[i - D] + .5f * z[i - D] : 0);
                top = z;
            }
            for (int i = 0; i < n; i++) top[i] *= .25f;
            return top;
        }
    }
}
