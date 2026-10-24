// Maç görevleri (HTML GOREV, gorevSec, gorevKontrol): her maç havuzdan 3 görev; biten görev anında kutlanır, toplam
// menüde birikir (best.gorev). Görevler oyuncuyu yeni vuruşlara iter, sayı kazanmaktan bağımsız ikinci bir ödül hattı.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class Gorev { public string id, ad; public int hedef; public Func<Match, int> say; public bool bitti; }

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
        public List<Gorev> gorevler = new List<Gorev>(); public int gorevBitti;

        List<Gorev> GorevSec()
        {
            var h = new List<Gorev>(GOREV); var o = new List<Gorev>();
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
