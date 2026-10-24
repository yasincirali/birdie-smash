// Antrenman (HTML OGR_ADIM / ogr*): top vuruş yerine gelince oyun durur, adımın istediği hareket yazılır, her vuruş iki
// kez (OGR_TEKRAR). İlk açılış (HTML d682adc, Ar-Ge 24 Eki): yalnız kısa servis, clear ve smaç, sonra 5 sayılık Kolay ilk
// maç; arcade oyuncusu on beş dersi atlıyor ya da oyunu kapatıyordu. Menüdeki Antrenman uzun listeyi oynatır.
// Unity demosunda dışarıda kalanlar: iki bilgi adımı (nefes halkasına / smaç gücü çubuğuna dokun) ve son "Antrenör ne
// derse" çağrı adımı; parmak izinin çizimi ve hız bandı yerine HUD yalnız canlı okumayı ("Şu an: Clear ✓") yazar.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class OgrAdim
    {
        public string bas, met, neden, servis, besle; public string[] tip; public bool guc, slice;
        public Func<Reach, Shuttle, bool> dur;
    }

    public class Training
    {
        public List<OgrAdim> liste; public bool kisa, dur, bitti, macaGec; public int i, n, miss, yildiz; public Shuttle top; public float bitisT, gecisT;
        public string son;   // son vuruşun okunuşu (HUD)
    }

    public partial class Match
    {
        public Training ogr;
        public const int OGR_TEKRAR = 2;
        public static readonly OgrAdim[] OGR_ADIM = {
            new OgrAdim{bas="Kısa servis",met="Oyuncunun önünden yukarı doğru YAVAŞ kaydır.",neden="Top fileyi sıyırır, rakibin önüne düşer. Tekler maçlarında en çok atılan servis.",servis="kisa"},
            new OgrAdim{bas="Flick servis",met="Aynı duruştan KISA ve SERT bir fiske yap.",neden="Hazırlık kısa servisle aynı görünür; rakip öne çıkarken top üstünden dibe gider.",servis="flick"},
            new OgrAdim{bas="Yüksek servis",met="UZUN ve HIZLI yukarı kaydır.",neden="Top yükselip rakibin dip çizgisine neredeyse dik iner.",servis="yuksek"},
            new OgrAdim{bas="Clear: dibe at",met="Topun içinden yukarı doğru UZUN ve orta hızda kaydır. Sağa sola eğersen top o yöne gider.",neden="Top rakibin arka çizgisine gider; sana yerine dönecek zaman kazandırır.",tip=new[]{"clear"},besle="clear",dur=(I,s)=>I.over},
            new OgrAdim{bas="Drop: fileye bırak",met="Topun üstünden YAVAŞ ve kısa geç.",neden="Top filenin hemen arkasına düşer; dipteki rakip öne koşmak zorunda kalır.",tip=new[]{"drop"},besle="clear",dur=(I,s)=>I.over},
            new OgrAdim{bas="Smaç",met="Top başının üstünde. HIZLI ve sert kaydır.",neden="Top aşağı doğru en hızlı iner; karşılaması en zor vuruş. Backhand tarafından smaç olmaz.",tip=new[]{"smash","jump"},besle="clear",dur=(I,s)=>I.over},
            new OgrAdim{bas="Drive: düz ve sert",met="Top omuz hizasında. HIZLI ve düz kaydır.",neden="Top fileyi alçaktan, düz geçer; rakibe hazırlanacak zaman kalmaz.",tip=new[]{"drive"},besle="drive",dur=(I,s)=>!I.over&&!I.lunge&&!I.kill},
            new OgrAdim{bas="Blok",met="Rakip smaç attı. Parmağını AŞAĞI doğru kısa çek.",neden="Raketi topun önüne koyarsın; smacın hızıyla top filenin dibine düşer.",tip=new[]{"block"},besle="smash",dur=(I,s)=>true},
            new OgrAdim{bas="Lift",met="Top alçakta. Yukarı doğru orta hızda kaydır.",neden="Alçak topu rakibin dibine kaldırırsın; fileden kurtulup yerine dönersin.",tip=new[]{"lift","clear"},besle="drop",dur=(I,s)=>!I.over&&s.z<1.4f},
            new OgrAdim{bas="File vuruşu",met="Top file üstünde yüksek kaldı. HIZLI ve sert kaydır.",neden="Fileye yakın yüksek top kaçırılmaz: aşağı vurursun, sayı neredeyse kesin.",tip=new[]{"kill"},besle="gevsek",dur=(I,s)=>I.kill},
            new OgrAdim{bas="Slice: kesme",met="Kaydırırken parmağınla bir YAY çiz; sert değil, clear hızında.",neden="Raket topu keserek alır; top takla atar, beklenenden kısa düşer ve rakip yanlış adım atar.",slice=true,besle="clear",dur=(I,s)=>I.over},
            new OgrAdim{bas="Sıçrayarak smaç",met="Güç dolu, top başının üstünde. HIZLI ve sert kaydır.",neden="Oyuncu sıçrar, top dik ve çok hızlı iner. Gücü bitirir; yeniden doldurmak için tam orta vur.",tip=new[]{"jump"},besle="clear",dur=(I,s)=>I.over,guc=true}};
        public static readonly int[] OGR_ILK = { 0, 3, 5 };   // Kısa servis, Clear, Smaç
        public static readonly Dictionary<string, float[]> OGR_HEDEF = new Dictionary<string, float[]> {
            {"clear",new[]{0f,2.4f}},{"lift",new[]{0f,2.8f}},{"slice",new[]{0f,4.2f}},{"drop",new[]{Court.NET-2.2f,Court.NET}},{"block",new[]{Court.NET-2.2f,Court.NET}},
            {"smash",new[]{.5f,Court.NET-1.2f}},{"jump",new[]{.5f,Court.NET-1.2f}},{"drive",new[]{.5f,4.6f}},{"kill",new[]{Court.NET-4.2f,Court.NET}}};
        public static readonly Dictionary<string, string> OGR_AD = new Dictionary<string, string> {
            {"kisa","Kısa servis"},{"flick","Flick servis"},{"yuksek","Yüksek servis"},{"clear","Clear"},{"lift","Lift"},{"drop","Drop"},{"drive","Drive"},{"smash","Smaç"},{"jump","Sıçrayarak smaç"},{"kill","File vuruşu"},{"block","Blok"},{"slice","Slice"}};

        void OgrKur(bool kisa)
        {
            var l = new List<OgrAdim>(); if (kisa) foreach (int i in OGR_ILK) l.Add(OGR_ADIM[i]); else l.AddRange(OGR_ADIM);
            ogr = new Training { liste = l, kisa = kisa }; easy = true;
            var st = STIL["duvar"].Clone(); st.ad = "Antrenör"; st.ipucu = ""; st.hata = .45f; stil = st; energy = 0;
        }
        public OgrAdim OgrA() { return ogr != null && ogr.i < ogr.liste.Count ? ogr.liste[ogr.i] : null; }
        public string OgrIstenen(OgrAdim a) { return a.servis ?? (a.slice ? "slice" : a.tip[0]); }

        bool OgrDurdur()
        {
            var a = OgrA(); if (a == null) return false; if (ogr.dur) return true;
            if (a.servis != null) return false; if (s == null || s.dead || s.from != "op" || ogr.top == s) return false;
            Reach I; if (!ReachInfo(out I) || !a.dur(I, s)) return false;
            ogr.top = s; ogr.dur = true; return true;
        }
        public static bool OgrUygun(OgrAdim a, string tip, string servis, bool slice)
        {
            if (a.servis != null) return servis == a.servis; if (tip == null) return false; if (a.slice) return slice; return Array.IndexOf(a.tip, tip) >= 0;
        }
        // Iska ipucu: ne çıktığına ve ne istendiğine göre tek bir düzeltme söylenir.
        static string OgrIpucu(OgrAdim a, string tip, string servis, float sp)
        {
            if (a.servis != null)
            {
                string w0 = a.servis; if (w0 == "kisa") return "Hızlıydın: yavaşça it";
                if (servis == "kisa") return w0 == "flick" ? "Yavaş kaldı: kısa ama SERT fiske yap" : "Yavaş kaldı: uzun ve hızlı kaydır";
                return w0 == "flick" ? "Hareket uzun oldu: kısa tut" : "Hareket kısa oldu: daha uzun kaydır";
            }
            string w = a.slice ? "slice" : a.tip[0], g = tip;
            if (w == "slice") return "Kavis okunmadı: kaydırırken bir yay çiz";
            if (w == "block") return "Parmağını AŞAĞI çek"; if (g == "block") return "Aşağı çektin: yukarı kaydır";
            if ((w == "smash" || w == "jump") && g == "clear" && sp > 1.7f) return "Backhand tarafından smaç olmaz";
            var B = new Dictionary<string, int> { { "drop", 0 }, { "clear", 1 }, { "lift", 1 }, { "smash", 2 }, { "jump", 2 }, { "drive", 2 }, { "kill", 2 } };
            int bw, bg; if (B.TryGetValue(w, out bw) && g != null && B.TryGetValue(g, out bg)) { if (bw > bg) return "Yavaş kaldın: daha hızlı kaydır"; if (bw < bg) return "Hızlıydın: daha yavaş kaydır"; }
            return "sıradaki topta yeniden dene";
        }
        void OgrOlay(string tip, string servis, bool slice, bool over_, float sp)
        {
            if (ogr == null) return; ogr.dur = false; var a = OgrA(); if (a == null) return;
            ogr.son = (servis != null ? OGR_AD[servis] : tip != null ? (slice ? "Slice" : (OGR_AD.ContainsKey(tip) ? OGR_AD[tip] : tip)) : "") + (OgrUygun(a, tip, servis, slice) ? " ✓" : " ✗");
            if (a.servis != null) { if (servis == null) return; if (servis == a.servis) OgrBasari(a); else OgrIska(a, tip, servis, sp); return; }
            if (tip == null) return;
            if (OgrUygun(a, tip, null, slice)) OgrBasari(a); else { OgrIska(a, tip, null, sp); if (a.guc) energy = 1; }
        }
        void OgrBasari(OgrAdim a)
        {
            ogr.n++; ogr.miss = 0; if (s != null && s.from == "me") { float[] h; if (OGR_HEDEF.TryGetValue(OgrIstenen(a), out h)) s.ogrHedef = h; }
            if (ogr.n >= OGR_TEKRAR) { OgrIlerle(a); return; }
            Say(null, "ogr", Court.MID, Court.NET + 2, 1, "Güzel! Bir daha", 3); if (a.guc) energy = 1;
        }
        void OgrIska(OgrAdim a, string tip, string servis, float sp)
        {
            ogr.miss++; string ad = servis != null ? OGR_AD[servis] : (tip != null && OGR_AD.ContainsKey(tip) ? OGR_AD[tip] : tip);
            Say(null, "ogr", Court.MID, Court.NET + 2, 1, ad + " oldu · " + OgrIpucu(a, tip, servis, sp), 3);
        }
        void OgrIlerle(OgrAdim a)
        {
            ogr.dur = false; ogr.i++; ogr.n = 0; ogr.miss = 0; ogr.son = null;
            Say(null, "ogr", Court.MID, Court.NET + 2, 1, "Tamam: " + a.bas, 3);
            var b = OgrA(); if (b == null) ogr.bitisT = 1.4f; else if (b.guc) energy = 1;
        }
        // Antrenmanı bitir: 1,4 sn sonra "Maç başlıyor!", 1,6 sn sonra görünüm ilk maça (kısa liste) ya da serbest maça geçer.
        void OgrTick(float dt)
        {
            if (ogr == null) return;
            if (ogr.bitisT > 0) { ogr.bitisT -= dt; if (ogr.bitisT <= 0) { ogr.bitti = true; ogr.gecisT = 1.6f; Say(null, "ogr", Court.MID, Court.NET, 2, ogr.kisa ? "Maç başlıyor!" : "Antrenman bitti. Maç başlıyor!", 3); } }
            else if (ogr.bitti && ogr.gecisT > 0) { ogr.gecisT -= dt; if (ogr.gecisT <= 0) ogr.macaGec = true; }
        }
    }
}
