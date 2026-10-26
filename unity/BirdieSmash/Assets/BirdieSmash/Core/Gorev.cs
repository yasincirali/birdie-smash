// Maç görevleri (HTML GOREV, gorevSec, gorevKontrol): her maç havuzdan 3 görev; biten görev anında kutlanır, toplam
// menüde birikir (best.gorev). Görevler oyuncuyu yeni vuruşlara iter, sayı kazanmaktan bağımsız ikinci bir ödül hattı.
// 2026-10-26 kariyer haritası (tasarım §3.1): kariyer maçında ilk görev basamağın sabit görevidir (veri: kariyer.json
// "gorev"; rakibe göre seçildi, rastgele değil) ve 2. yıldızı verir; diğer ikisi havuzdan. Sabit görevlerden üçü yalnız
// kariyerde (GOREV_KARIYER): rastgele havuz ve Hızlı maç eskisi gibi kalsın, "imzayı oku" yalnız patron maçında anlamlı.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class Gorev { public string id, ad; public int hedef; public Func<Match, int> say; public bool bitti, basamak; }

    public partial class Match
    {
        public static readonly Gorev[] GOREV = {
            new Gorev{id="slice",ad="3 slice vuruşu",hedef=3,say=m=>m.slices},
            new Gorev{id="tepe",ad="Topu 2 kez tepeden al",hedef=2,say=m=>m.tepeden},
            new Gorev{id="ralli",ad="10 vuruşluk ralli",hedef=10,say=m=>m.maxRally},
            new Gorev{id="kill",ad="2 file vuruşu",hedef=2,say=m=>m.kills},
            new Gorev{id="perf",ad="5 tam orta",hedef=5,say=m=>m.perfects},
            new Gorev{id="aldat",ad="2 aldatma",hedef=2,say=m=>m.deceives},
            new Gorev{id="uzan",ad="3 uzanarak kurtarış",hedef=3,say=m=>m.saves},
            new Gorev{id="out",ad="Dışarı giden 2 topu bırak",hedef=2,say=m=>m.outsLeft},
            new Gorev{id="smac",ad="3 smaç sayısı",hedef=3,say=m=>m.smacSayi}};
        public static readonly Gorev[] GOREV_KARIYER = {
            new Gorev{id="karsila",ad="3 smaçı karşıla",hedef=3,say=m=>m.smacKarsila},
            new Gorev{id="drop",ad="2 drop sayısı",hedef=2,say=m=>m.dropSayi},
            new Gorev{id="okudun",ad="İmza vuruşunu oku",hedef=1,say=m=>m.okudun}};
        public static Gorev GorevBul(string id)
        {
            foreach (var g in GOREV) if (g.id == id) return g; foreach (var g in GOREV_KARIYER) if (g.id == id) return g; return null;
        }
        // Kariyer maçının sabit görevi (yoksa null): maç sonunda 2. yıldız buna bakar.
        public Gorev BasamakGorevi { get { return gorevler.Count > 0 && gorevler[0].basamak ? gorevler[0] : null; } }
        public List<Gorev> gorevler = new List<Gorev>(); public int gorevBitti;

        List<Gorev> GorevSec()
        {
            var h = new List<Gorev>(GOREV); var o = new List<Gorev>();
            var kb = stil != null ? KariyerVeri.Al(stil.kariyer) : null; var sabit = kb != null ? GorevBul(kb.gorev) : null;
            if (kb == null && stil != null && stil.usta >= 0) sabit = GorevBul(UstaYolu.GorevId(stil.usta));   // 2026-10-26 Usta yolu basamağının görevi
            if (sabit != null) { o.Add(new Gorev { id = sabit.id, ad = sabit.ad, hedef = sabit.hedef, say = sabit.say, basamak = true }); h.RemoveAll(g => g.id == sabit.id); }
            while (o.Count < 3) { int i = rng.Next(h.Count); var g = h[i]; h.RemoveAt(i); o.Add(new Gorev { id = g.id, ad = g.ad, hedef = g.hedef, say = g.say }); }
            return o;
        }
        void GorevKontrol()
        {
            foreach (var g in gorevler)
                if (!g.bitti && g.say(this) >= g.hedef)
                {
                    g.bitti = true; gorevBitti++;
                    Emit(Ev.Gorev, "me", g.id, Court.MID, Court.NET, 0, gorevBitti, g.ad);   // görünüm: "Görev tamam: …", arpej, kıvılcım
                }
        }
    }
}
