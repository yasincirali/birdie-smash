// Antrenman (HTML OGR_ADIM / ogr*): top vuruş yerine gelince oyun durur, adımın istediği hareket yazılır, her vuruş iki
// kez (OGR_TEKRAR). İlk açılış (HTML d682adc, Ar-Ge 24 Eki): yalnız kısa servis, clear ve smaç, sonra 5 sayılık Kolay ilk
// maç; arcade oyuncusu on beş dersi atlıyor ya da oyunu kapatıyordu. Menüdeki Antrenman uzun listeyi oynatır.
// 2026-10-24 ikinci tur: iki bilgi adımı (nefes halkasına / smaç gücü çubuğuna dokun: oyun durur), son "Antrenör ne
// derse" çağrı adımı (oyun durmaz, 4 top), ustalık kuralı (ikinci deneme izsiz, iki ıskada iz geri gelir) ve parmak
// izinin türü (iz) taşındı. Çizim (iz, hız bandı, hedef alanı) Game/OgrView'da.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class OgrAdim
    {
        public string bas, met, neden, servis, besle, iz, bilgi; public string[] tip; public bool guc, slice, drill;
        public DrilTanim dril;   // 2026-10-26 antrenman görevi (Core/Dril): adım tek başına bir görevdir
        public Func<Reach, Shuttle, bool> dur;
    }

    public class Training
    {
        public List<OgrAdim> liste; public bool kisa, dur, bitti, macaGec; public int i, n, miss, yildiz; public Shuttle top; public float bitisT, gecisT;
        public string son;   // son vuruşun okunuşu (HUD)
        public float sonSp = -1, sonT = -9; public string sonTip; public bool sonSlice;   // hız bandının iğnesi 2,2 sn son vuruşta kalır
        public int dene, isabet; public string cagri, cagriSon; public float cagriT; public bool gosterildi;   // "Antrenör ne derse"; görevde: top ve isabet sayısı
        public DrilTanim dril;   // antrenman görevi (Core/Dril); null = Temel ders
        public bool izsiz { get { var a = i < liste.Count ? liste[i] : null; return a != null && a.bilgi == null && !a.drill && n >= 1 && miss < 2; } }
    }

    public partial class Match
    {
        public Training ogr;
        public const int OGR_TEKRAR = 2;
        public static readonly OgrAdim[] OGR_ADIM = {
            new OgrAdim{bas="Kısa servis",met="Oyuncunun önünden yukarı doğru YAVAŞ kaydır.",neden="Top fileyi sıyırır, rakibin önüne düşer. Tekler maçlarında en çok atılan servis.",servis="kisa",iz="yavas"},
            new OgrAdim{bas="Flick servis",met="Aynı duruştan KISA ve SERT bir fiske yap.",neden="Hazırlık kısa servisle aynı görünür; rakip öne çıkarken top üstünden dibe gider.",servis="flick",iz="fiske"},
            new OgrAdim{bas="Yüksek servis",met="UZUN ve HIZLI yukarı kaydır.",neden="Top yükselip rakibin dip çizgisine neredeyse dik iner.",servis="yuksek",iz="uzun"},
            new OgrAdim{bas="Nefes",met="Ayağının altındaki halka nefesin. Halkaya dokun.",neden="Smaç, sıçrama ve uzanma nefes yakar; halka azalınca yavaşlarsın ve smaç zayıflar. Clear atıp sayı arasında dinlenince dolar.",bilgi="nefes"},
            new OgrAdim{bas="Clear: dibe at",met="Topun içinden yukarı doğru UZUN ve orta hızda kaydır. Sağa sola eğersen top o yöne gider.",neden="Top rakibin arka çizgisine gider; sana yerine dönecek zaman kazandırır.",tip=new[]{"clear"},iz="uzun",besle="clear",dur=(I,s)=>I.over},
            new OgrAdim{bas="Drop: fileye bırak",met="Topun üstünden YAVAŞ ve kısa geç.",neden="Top filenin hemen arkasına düşer; dipteki rakip öne koşmak zorunda kalır.",tip=new[]{"drop"},iz="yavas",besle="clear",dur=(I,s)=>I.over},
            new OgrAdim{bas="Smaç",met="Top başının üstünde. HIZLI ve sert kaydır.",neden="Top aşağı doğru en hızlı iner; karşılaması en zor vuruş. Backhand tarafından smaç olmaz.",tip=new[]{"smash","jump"},iz="hizli",besle="clear",dur=(I,s)=>I.over},
            new OgrAdim{bas="Drive: düz ve sert",met="Top omuz hizasında. HIZLI ve düz kaydır.",neden="Top fileyi alçaktan, düz geçer; rakibe hazırlanacak zaman kalmaz.",tip=new[]{"drive"},iz="hizli",besle="drive",dur=(I,s)=>!I.over&&!I.lunge&&!I.kill},
            new OgrAdim{bas="Blok",met="Rakip smaç attı. Parmağını AŞAĞI doğru kısa çek.",neden="Raketi topun önüne koyarsın; smacın hızıyla top filenin dibine düşer.",tip=new[]{"block"},iz="asagi",besle="smash",dur=(I,s)=>true},
            new OgrAdim{bas="Lift",met="Top alçakta. Yukarı doğru orta hızda kaydır.",neden="Alçak topu rakibin dibine kaldırırsın; fileden kurtulup yerine dönersin.",tip=new[]{"lift","clear"},iz="uzun",besle="drop",dur=(I,s)=>!I.over&&s.z<1.4f},
            new OgrAdim{bas="File vuruşu",met="Top file üstünde yüksek kaldı. HIZLI ve sert kaydır.",neden="Fileye yakın yüksek top kaçırılmaz: aşağı vurursun, sayı neredeyse kesin.",tip=new[]{"kill"},iz="hizli",besle="gevsek",dur=(I,s)=>I.kill},
            new OgrAdim{bas="Slice: kesme",met="Kaydırırken parmağınla bir YAY çiz; sert değil, clear hızında.",neden="Raket topu keserek alır; top takla atar, beklenenden kısa düşer ve rakip yanlış adım atar.",slice=true,iz="yay",besle="clear",dur=(I,s)=>I.over},
            new OgrAdim{bas="Smaç gücü",met="Alttaki çubuk smaç gücün. Çubuğa dokun.",neden="Topun tam ortasından geçen her vuruş doldurur. Dolunca bir sonraki smaç SIÇRAYARAK smaç olur: en sert vuruşun. Şimdi senin için doldurduk.",bilgi="guc"},
            new OgrAdim{bas="Sıçrayarak smaç",met="Güç dolu, top başının üstünde. HIZLI ve sert kaydır.",neden="Oyuncu sıçrar, top dik ve çok hızlı iner. Gücü bitirir; yeniden doldurmak için tam orta vur.",tip=new[]{"jump"},iz="hizli",besle="clear",dur=(I,s)=>I.over,guc=true},
            new OgrAdim{bas="Antrenör ne derse",met="Artık oyun durmuyor. Top gelirken antrenör bir vuruş söyler: onu at.",neden="Maçta topu görür görmez karar verirsin. Dört topta kaçını tutturacaksın?",drill=true}};
        public static readonly int[] OGR_ILK = { 0, 4, 6 };   // Kısa servis, Clear, Smaç (Nefes bilgi adımı 3. sıraya girdi)
        public const int OGR_CAGRI = 4;   // son adımda dört çağrı
        // Hız bandı (HTML ogrBolgeler): bölgeler oyunun kendi eşikleri (servis 1,0; vuruş 0,75 / 1,7 px/ms), ölçek 0–2,6.
        public const float OGR_MAX = 2.6f;
        public static float[] OgrBolgeSinir(OgrAdim a) { return a.servis != null ? new[] { 1.0f } : new[] { .75f, 1.7f }; }
        public static string[] OgrBolgeAd(OgrAdim a) { return a.servis != null ? new[] { "Yavaş: kısa", "Hızlı: flick / yüksek" } : new[] { "Yavaş", "Orta", "Hızlı" }; }
        // Hedef bölge; null = bant gizli (blok ve lift hızla değil yönle/uzanmayla okunur).
        public int? OgrHedefBolge(OgrAdim a)
        {
            if (a.bilgi != null) return null; string w = OgrIstenen(a); if (w == null) return null; if (a.servis != null) return w == "kisa" ? 0 : 1;
            switch (w) { case "drop": return 0; case "clear": case "slice": return 1; case "smash": case "jump": case "drive": case "kill": return 2; }
            return null;
        }
        public static readonly Dictionary<string, float[]> OGR_HEDEF = new Dictionary<string, float[]> {
            {"clear",new[]{0f,2.4f}},{"lift",new[]{0f,2.8f}},{"slice",new[]{0f,4.2f}},{"drop",new[]{Court.NET-2.2f,Court.NET}},{"block",new[]{Court.NET-2.2f,Court.NET}},
            {"smash",new[]{.5f,Court.NET-1.2f}},{"jump",new[]{.5f,Court.NET-1.2f}},{"drive",new[]{.5f,4.6f}},{"push",new[]{1.6f,4.6f}},{"kill",new[]{Court.NET-4.2f,Court.NET}}};
        public static readonly Dictionary<string, string> OGR_AD = new Dictionary<string, string> {
            {"kisa","Kısa servis"},{"flick","Flick servis"},{"yuksek","Yüksek servis"},{"clear","Clear"},{"lift","Lift"},{"drop","Drop"},{"drive","Drive"},{"smash","Smaç"},{"jump","Sıçrayarak smaç"},{"kill","File vuruşu"},{"block","Blok"},{"push","Push"},{"slice","Slice"}};

        void OgrKur(bool kisa)
        {
            var l = new List<OgrAdim>(); if (kisa) foreach (int i in OGR_ILK) l.Add(OGR_ADIM[i]); else l.AddRange(OGR_ADIM);
            ogr = new Training { liste = l, kisa = kisa }; easy = true;
            if (ogrDril != null) { DrilKur(ogrDril); ogr.kisa = false; }   // görev: tek adım, Temel dersin listesi kullanılmaz
            var st = STIL["duvar"].Clone(); st.ad = "Antrenör"; st.ipucu = ""; st.hata = .45f; stil = st; energy = 0;
        }
        public OgrAdim OgrA() { return ogr != null && ogr.i < ogr.liste.Count ? ogr.liste[ogr.i] : null; }
        public string OgrIstenen(OgrAdim a) { return a.drill ? ogr.cagri : a.servis ?? (a.slice ? "slice" : a.tip != null ? a.tip[0] : null); }

        bool OgrDurdur()
        {
            var a = OgrA(); if (a == null) return false; if (ogr.dur) return true;
            // 2026-10-30: s/ogr.top denetimi bir birleştirmede yorumun içinde kalmıştı (1afe7e2a); geri alındı.
            if (a.servis != null || a.bilgi != null || a.drill) return false; if (s == null || s.dead || s.from != "op" || ogr.top == s) return false;
            // Görevde (vuruş laboratuvarı) oyun yalnız İLK topta bir kez durur ve hareket gösterilir (2026-10-30, yasin: "antrenmanda
            // da vuruş için gereken aksiyonu göstermeli bir kere, sonrasında oyuncu denesin"); sonraki toplar durmadan gelir.
            if (a.dril != null)
            {
                if (ogr.gosterildi || ogr.dene > 0 || a.iz == null) return false; Reach J; if (!ReachInfo(out J) || !DrilDur(a, J)) return false;
                ogr.top = s; ogr.dur = true; ogr.gosterildi = true; return true;
            }
            Reach I; if (!ReachInfo(out I) || !a.dur(I, s)) return false;
            ogr.top = s; ogr.dur = true; return true;
        }
        // Görevin gösterim topunda durma anı: Temel dersin aynı vuruştaki koşulu (OGR_ADIM dur).
        bool DrilDur(OgrAdim a, Reach I)
        {
            string t = a.dril.olcu == "kill" ? "kill" : a.dril.olcu == "karsila" ? "block" : a.slice ? "clear" : a.tip != null && a.tip.Length > 0 ? a.tip[0] : null;
            switch (t)
            {
                case "clear": case "smash": case "jump": return I.over || a.dril.besle == "orta" && !I.lunge;
                case "drop": return a.dril.besle == "clear" ? I.over : !I.kill;
                case "kill": return I.kill;
                case "drive": return !I.over && !I.lunge && !I.kill;
                case "lift": return !I.over && s.z < 1.4f;
                default: return true;
            }
        }
        // Görevin hareketi (HUD hayalet iz): Temel dersteki aynı vuruşun izi. Karışık görevlerde (split, çeşit) ve push'ta yok.
        public static string DrilIz(DrilTanim d)
        {
            if (d.olcu == "servis") return (d.servis ?? "kisa") == "flick" ? "fiske" : "yavas";
            if (d.olcu == "aldat") return "yay"; if (d.olcu == "kill") return "hizli"; if (d.olcu == "karsila") return "asagi";
            if (d.olcu == "split" || d.olcu == "cesit" || d.tip == null || d.tip.Length == 0) return null;
            switch (d.tip[0]) { case "clear": case "lift": return "uzun"; case "drop": return "yavas"; case "smash": case "jump": case "drive": return "hizli"; case "block": return "asagi"; default: return null; }
        }
        public bool OgrUygunCanli(OgrAdim a, string tip, bool slice)
        {
            if (tip == null) return false; if (a.drill) return tip == ogr.cagri || ogr.cagri == "smash" && tip == "jump";
            if (a.servis != null) return tip == a.servis; if (a.slice) return slice; return a.tip != null && Array.IndexOf(a.tip, tip) >= 0;
        }
        public static bool OgrUygun(OgrAdim a, string tip, string servis, bool slice)
        {
            if (a.drill || a.bilgi != null) return false;
            if (a.servis != null) return servis == a.servis; if (tip == null) return false; if (a.slice) return slice; return Array.IndexOf(a.tip, tip) >= 0;
        }
        // Iska ipucu: ne çıktığına ve ne istendiğine göre tek bir düzeltme söylenir.
        string OgrIpucu(OgrAdim a, string tip, string servis, float sp)
        {
            if (a.servis != null)
            {
                string w0 = a.servis; if (w0 == "kisa") return "Hızlıydın: yavaşça it";
                if (servis == "kisa") return w0 == "flick" ? "Yavaş kaldı: kısa ama SERT fiske yap" : "Yavaş kaldı: uzun ve hızlı kaydır";
                return w0 == "flick" ? "Hareket uzun oldu: kısa tut" : "Hareket kısa oldu: daha uzun kaydır";
            }
            string w = a.drill ? ogr.cagri : a.slice ? "slice" : a.tip[0], g = tip;
            if (w == "slice") return "Kavis okunmadı: kaydırırken bir yay çiz";
            if (w == "block") return "Parmağını AŞAĞI çek"; if (g == "block") return "Aşağı çektin: yukarı kaydır";
            if ((w == "smash" || w == "jump") && g == "clear" && sp > 1.7f) return "Backhand tarafından smaç olmaz";
            var B = new Dictionary<string, int> { { "drop", 0 }, { "clear", 1 }, { "lift", 1 }, { "smash", 2 }, { "jump", 2 }, { "drive", 2 }, { "kill", 2 } };
            int bw, bg; if (B.TryGetValue(w, out bw) && g != null && B.TryGetValue(g, out bg)) { if (bw > bg) return "Yavaş kaldın: daha hızlı kaydır"; if (bw < bg) return "Hızlıydın: daha yavaş kaydır"; }
            return "sıradaki topta yeniden dene";
        }
        void OgrOlay(string tip, string servis, bool slice, bool over_, float sp)
        {
            if (ogr == null) return; ogr.dur = false; var a = OgrA(); if (a == null || a.bilgi != null) return;
            if (sp >= 0) { ogr.sonSp = sp; ogr.sonTip = servis ?? tip; ogr.sonSlice = slice; ogr.sonT = realClock; }
            if (a.drill) { if (tip != null) OgrCagriSonuc(tip); return; }
            if (a.dril != null) { if (tip != null || servis != null) drVurdu = true; ogr.son = null; return; }   // görev: ölçü topun düşüşünde (Core/Dril)
            ogr.son = (servis != null ? OGR_AD[servis] : tip != null ? (slice ? "Slice" : (OGR_AD.ContainsKey(tip) ? OGR_AD[tip] : tip)) : "") + (OgrUygun(a, tip, servis, slice) ? " ✓" : " ✗");
            if (a.servis != null) { if (servis == null) return; if (servis == a.servis) OgrBasari(a); else OgrIska(a, tip, servis, sp); return; }
            if (tip == null) return;
            if (OgrUygun(a, tip, null, slice)) OgrBasari(a); else { OgrIska(a, tip, null, sp); if (a.guc) energy = 1; }
        }
        void OgrBasari(OgrAdim a)
        {
            ogr.n++; ogr.miss = 0; if (s != null && s.from == "me") { float[] h; if (OGR_HEDEF.TryGetValue(OgrIstenen(a), out h)) s.ogrHedef = h; }
            if (ogr.n >= OGR_TEKRAR) { OgrIlerle(a); return; }
            Say(null, "ogr", Court.MID, Court.NET + 2, 1, "Güzel! Bir daha, bu sefer iz yok", 3); if (a.guc) energy = 1;
        }
        void OgrIska(OgrAdim a, string tip, string servis, float sp)
        {
            ogr.miss++; string ad = servis != null ? OGR_AD[servis] : (tip != null && OGR_AD.ContainsKey(tip) ? OGR_AD[tip] : tip);
            Say(null, "ogr", Court.MID, Court.NET + 2, 1, ad + " oldu · " + OgrIpucu(a, tip, servis, sp), 3);
            if (ogr.miss == 2 && ogr.n > 0) ogrIzGeri = .5f;   // HTML: 500 ms sonra "İz geri geldi"
        }
        float ogrIzGeri;
        // Son adım: antrenör her topta bir vuruş çağırır. Yüksek topta (servis, clear beslemesi) clear/drop/smaç; antrenör
        // drive çağırınca topu düz besler. Aynı çağrı arka arkaya gelmez. Smaç çağrısına sıçrayarak smaç da sayılır.
        string OgrCagriSec(bool yuksek)
        {
            var l = yuksek ? new[] { "clear", "drop", "smash" } : new[] { "clear", "drop", "smash", "drive" }; string c;
            do c = l[rng.Next(l.Length)]; while (c == ogr.cagriSon);
            ogr.cagri = c; ogr.cagriSon = c; ogr.cagriT = realClock; return c;
        }
        void OgrCagriSonuc(string tip)
        {
            string c = ogr.cagri; if (c == null) return; ogr.cagri = null; ogr.dene++;
            if (tip == c || c == "smash" && tip == "jump") { ogr.isabet++; float[] h; if (s != null && s.from == "me" && OGR_HEDEF.TryGetValue(c, out h)) s.ogrHedef = h; Say(null, "ogr", Court.MID, Court.NET + 2, 1, "Doğru: " + OGR_AD[c], 3); }
            else Say(null, "ogr", Court.MID, Court.NET + 2, 1, (OGR_AD.ContainsKey(tip) ? OGR_AD[tip] : tip) + " oldu, istenen " + OGR_AD[c], 3);
            OgrCagriBitti();
        }
        void OgrCagriBitti() { if (ogr.dene >= OGR_CAGRI) OgrIlerle(OgrA()); }
        // Bilgi adımı: işaretli alana dokunmak (ya da kartta "Anladım, devam") geçirir. İsabet testi görünümde (ekran koordinatı).
        public bool OgrBilgiGec() { var a = OgrA(); if (a == null || a.bilgi == null) return false; OgrIlerle(a); return true; }
        void OgrIlerle(OgrAdim a)
        {
            ogr.dur = false; ogr.i++; ogr.n = 0; ogr.miss = 0; ogr.son = null; ogr.sonSp = -1; if (a.bilgi == "nefes") me.nefes = 1;
            Say(null, "ogr", Court.MID, Court.NET + 2, 1, "Tamam: " + a.bas, 3); Emit(Ev.Gorev, "ogr", "tamam");
            var b = OgrA();
            if (b == null) ogr.bitisT = 1.4f;
            else { if (b.bilgi == "nefes") me.nefes = .55f; if (b.bilgi == "guc" || b.guc) energy = 1; if (b.bilgi != null) ogr.dur = true; }
        }
        // Antrenmanı bitir: 1,4 sn sonra "Maç başlıyor!", 1,6 sn sonra görünüm ilk maça (kısa liste) ya da serbest maça geçer.
        void OgrTick(float dt)
        {
            if (ogr == null) return;
            if (ogrIzGeri > 0) { ogrIzGeri -= dt; if (ogrIzGeri <= 0) Say(null, null, Court.MID, Court.NET + 2.6f, 1, "İz geri geldi", 3); }
            if (ogr.bitisT > 0)
            {
                ogr.bitisT -= dt;
                if (ogr.bitisT <= 0)
                {
                    ogr.bitti = true; ogr.gecisT = 1.6f;
                    if (ogr.dril != null) { Say(null, "ogr", Court.MID, Court.NET, 2, "Görev bitti: " + ogr.isabet + "/" + ogr.dril.top, 3); return; }   // görünüm sonuç ekranına geçer (maça değil)
                    Say(null, "ogr", Court.MID, Court.NET, 2, ogr.kisa ? "Maç başlıyor!" : "Antrenman bitti. Maç başlıyor!", 3);
                    if (!ogr.kisa) Say(null, null, Court.MID, Court.NET + 1, 2, "Antrenör çağrısı " + ogr.isabet + "/" + OGR_CAGRI + " · " + ogr.yildiz + " kez hedefte", 3);
                }
            }
            else if (ogr.bitti && ogr.gecisT > 0) { ogr.gecisT -= dt; if (ogr.gecisT <= 0) ogr.macaGec = true; }
        }
    }
}
