// Soyunma odası (eski Ekipman paneli, 2026-10-26): içerik ve davranış bugünkü IMGUI paneliyle birebir; yalnız yüzey UI
// Toolkit. Tarz ve Görünüm segmentleri sonraki adımlarda (tasarım §2.3, §5).
//   Üst: 360° vitrin (Vitrin.rt): sürükle = döndür (bırakınca yavaşlar), ‹ › 15° (raket görünümünde 20°), "Raketi incele" /
//   "Oyuncuya dön"; rakete dokunmak (sürüklemeden: < 8 pt, < 400 ms, kutunun %14'ü yarıçap) raket görünümünü açar.
//   Gövde (kayar): raket görünümündeyse grip / kordaj / desen / çapraz tel renkleri (yalnız süs); seviye + Tüy + XP;
//   altı yuva (●○ yerine 10 bölmeli çubuk: glif riski yok) + Yükselt (kapalıysa nedeni yazar) + önizleme çubukları
//   (gri = şimdi, yeşil uç = yükseltince; yükseltince gri .5 sn'de kayar, hareketi azaltta anında); Eşit şartlar; modeller;
//   tel gerginliği (kaydırıcı + − / + düğmeleri: kaydırıcının tutamağı 44 pt'den küçük, tek adımlık iş düğmeyle de yapılır);
//   istatistik satırı. Seçimde titreşim ve raket/telde deneme smacı (sesi ve titreşimi) aynı.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        Image vitrinIm; Label vitrinYazi, telYazi, telNot, statYazi; VisualElement soyunmaGovde; ScrollView soyunmaKaydir;
        Dictionary<string, float> yukOnce; string yukK; bool telDegisti;
        Vector2 vBas; float vBasT, vSonX, vSonT; bool vBasili;

        void SoyunmaKur(VisualElement k)
        {
            k.Clear(); var vt = root.vitrin;
            var ust = new VisualElement(); ust.AddToClassList("vitrin-alan"); k.Add(ust);
            vitrinIm = new Image { image = vt.rt, scaleMode = ScaleMode.ScaleAndCrop }; vitrinIm.AddToClassList("vitrin"); ust.Add(vitrinIm);
            Erisim.Etiket(vitrinIm, L.T("Oyuncun, 360 derece görünüm"), "resim");
            vitrinIm.RegisterCallback<PointerDownEvent>(VitrinBas); vitrinIm.RegisterCallback<PointerMoveEvent>(VitrinSurukle); vitrinIm.RegisterCallback<PointerUpEvent>(VitrinBirak);
            vitrinYazi = Yazi("", 12, "vitrin-yazi"); ust.Add(vitrinYazi);
            var ok = Satir("vitrin-oklar"); ust.Add(ok);
            var sol = new VisualElement(); sol.AddToClassList("dugme"); sol.AddToClassList("ikon-dugme"); sol.Add(Ikon(Ikonlar.Geri, 20, Color.white)); sol.AddManipulator(new Clickable(() => VitrinCevir(-1))); Erisim.Etiket(sol, L.T("Sola çevir"), "dugme"); ok.Add(sol);
            var orta = Dugme("", () => { vt.hedefGec = vt.hedefGec >= 1 ? 0 : 1; Tik(); SoyunmaGovdeKur(true); }, "vitrin-gecis"); ok.Add(orta);
            var sag = new VisualElement(); sag.AddToClassList("dugme"); sag.AddToClassList("ikon-dugme"); sag.Add(Ikon(Ikonlar.Ileri, 20, Color.white)); sag.AddManipulator(new Clickable(() => VitrinCevir(1))); Erisim.Etiket(sag, L.T("Sağa çevir"), "dugme"); ok.Add(sag);
            soyunmaKaydir = Kaydirma(k); soyunmaKaydir.AddToClassList("govde-kaydir"); soyunmaKaydir.AddToClassList("soyunma-kaydir");
            soyunmaGovde = soyunmaKaydir.contentContainer; soyunmaGovde.AddToClassList("sekme-icerik");
            SoyunmaGovdeKur(false);
        }

        void VitrinCevir(int yon)
        {
            var vt = root.vitrin;
            if (vt.hedefGec >= 1) { vt.rv = 0; vt.ra = Mathf.Round(vt.ra / 10) * 10 + 20 * yon; } else { vt.v = 0; vt.a = Mathf.Round(vt.a / 15) * 15 + 15 * yon; }
            Tik();
        }
        void VitrinBas(PointerDownEvent e)
        {
            var vt = root.vitrin; vt.suruk = true; vBasili = true; vSonX = e.position.x; vSonT = Time.unscaledTime; vBas = e.position; vBasT = vSonT;
            vitrinIm.CapturePointer(e.pointerId); e.StopPropagation();
        }
        void VitrinSurukle(PointerMoveEvent e)
        {
            var vt = root.vitrin; if (!vBasili) return; float w = Mathf.Max(1, vitrinIm.layout.width);
            float dx = e.position.x - vSonX, dt = Mathf.Max(.008f, Time.unscaledTime - vSonT); float deg = dx / w * 360; vSonX = e.position.x; vSonT = Time.unscaledTime;
            if (vt.hedefGec >= 1) { vt.ra += deg; vt.rv = deg / dt; } else { vt.a += deg; vt.v = deg / dt; }
        }
        void VitrinBirak(PointerUpEvent e)
        {
            var vt = root.vitrin; if (!vBasili) return; vBasili = false; vt.suruk = false; vitrinIm.ReleasePointer(e.pointerId);
            Vector2 vp; var lp = vitrinIm.WorldToLocal(e.position); var lb = vitrinIm.WorldToLocal(vBas);
            if (vt.hedefGec < 1 && (lp - lb).magnitude < 8 && Time.unscaledTime - vBasT < .4f && vt.RaketYeri(out vp))
            {   // ScaleAndCrop: dokunun RT içindeki yeri (Hud'daki hesabın aynısı, panel birimiyle)
                float W = vitrinIm.layout.width, H = vitrinIm.layout.height; float s = Mathf.Max(W / vt.rt.width, H / vt.rt.height), dw = vt.rt.width * s, dh = vt.rt.height * s;
                var gp = new Vector2((W - dw) / 2 + vp.x * dw, (H - dh) / 2 + (1 - vp.y) * dh);
                if ((lp - gp).magnitude < W * .14f) { vt.hedefGec = 1; Tik(); SoyunmaGovdeKur(true); }
            }
        }

        void SoyunmaGovdeKur(bool yerKoru)
        {
            if (soyunmaGovde == null) return; var vt = root.vitrin; bool raketG = vt.hedefGec >= 1;
            Vector2 of = yerKoru && soyunmaKaydir != null ? soyunmaKaydir.scrollOffset : Vector2.zero;
            vitrinYazi.text = L.T(raketG ? "Raketin, 360 derece görünüm" : "Oyuncun, 360 derece görünüm");
            var gecis = vitrinIm.parent.Q<VisualElement>(null, "vitrin-gecis");
            if (gecis != null) { var gy = gecis.Q<Label>(); string gt = L.T(raketG ? "Oyuncuya dön" : "Raketi incele"); if (gy != null) gy.text = gt; Erisim.Etiket(gecis, gt, "dugme"); }
            var c = soyunmaGovde; c.Clear(); var gr = Ayar.gear;
            if (raketG)
            {   // raket görünümü (yalnız süs, oyuna etkisi yok)
                RenkSira(c, "Grip", "grip"); RenkSira(c, "Kordaj", "tel");
                c.Add(Grup(L.T("Desen"))); var ds = Satir(); c.Add(ds);
                ds.Add(Cip(L.T("Tek renk"), Ayar.RGOR["desen"] == "tek", () => { Ayar.RGOR["desen"] = "tek"; Ayar.Kaydet(); vt.Yenile(); root.sfx.Tik(); SoyunmaGovdeKur(true); }));
                ds.Add(Cip(L.T("Ana ve çapraz ayrı"), Ayar.RGOR["desen"] == "iki", () => { Ayar.RGOR["desen"] = "iki"; Ayar.Kaydet(); vt.Yenile(); root.sfx.Tik(); SoyunmaGovdeKur(true); }));
                if (Ayar.RGOR["desen"] == "iki") RenkSira(c, "Çapraz teller", "tel2");
                c.Add(Bosluk(10));
            }
            // seviye ve Tüy
            int L_, kalan, gerek; gr.OyuncuSv(out L_, out kalan, out gerek);
            var sv = Satir("arali"); c.Add(sv); sv.Add(Yazi(L.T("Seviye {0}", L_), 16, "kalin")); sv.Add(Yazi(L.T("{0} Tüy", gr.eko.tuy), 16, "kalin"));
            c.Add(Cubuk(gerek > 0 ? kalan / (float)gerek : 1, CAMGOBEGI));
            c.Add(Yazi(gerek > 0 ? L.T("Sonraki seviyeye {0} XP · maç kazandıkça Tüy ve XP gelir", gerek - kalan) : L.T("En yüksek seviye"), 12, "soluk"));
            foreach (var Y in Gear.YUVA) YuvaKur(c, Y, L_);
            // eşit şartlar
            c.Add(Cip(L.T("Eşit şartlar"), gr.eko.esit, () => { gr.eko.esit = !gr.eko.esit; Ayar.GearHesap(); Ayar.Kaydet(); SoyunmaGovdeKur(true); }, L.T("Serbest maçta yükseltmeler sayılmaz")));
            c.Add(Yazi(L.T("Rakete ya da tele dokununca o takımın smaç sesini duyarsın."), 12, "soluk"));
            Modeller(c, "raket", "Raket");
            TelKur(c);
            Modeller(c, "bileklik", "Bileklik"); Modeller(c, "bant", "Kafa bandı"); Modeller(c, "forma", "Forma");
            statYazi = Yazi(StatSatiri(), 12, "soluk"); c.Add(statYazi); c.Add(Bosluk(20));
            if (of.y > 0) { soyunmaKaydir.scrollOffset = of; var sk = soyunmaKaydir; sk.schedule.Execute(() => sk.scrollOffset = of).StartingIn(33); }
        }

        static string StatSatiri()
        {
            var S = Ayar.GS; System.Func<float, string> pct = v => (v >= 1 ? "+" : "−") + Mathf.Abs(Mathf.Round((v - 1) * 100)) + "%";
            return L.T("Güç") + " " + pct(S.guc) + " · " + L.T("Kontrol") + " " + pct(S.kontrol) + " · " + L.T("Drive/file") + " " + pct(S.hiz) + " · " + L.T("Tatlı nokta") + " " + pct(S.tatli) + " · " + L.T("Koşu") + " " + pct(S.kosu) + " · " + L.T("Akış") + " " + L.T("{0} tam orta", S.akis);
        }

        void RenkSira(VisualElement c, string ad, string k)
        {
            c.Add(Grup(L.T(ad))); var s = Satir("renkler"); c.Add(s);
            foreach (var id in Vitrin.RAK_SIRA)
            {
                bool on = Ayar.RGOR[k] == id; string rid = id;
                var d = new VisualElement(); d.AddToClassList("renk"); if (on) d.AddToClassList("renk--secili"); d.style.backgroundColor = Kit.Hex(Vitrin.RAK_RENK[id]);
                if (on) d.Add(Ikon(Ikonlar.Tik, 18, id == "beyaz" || id == "limon" ? Color.black : Color.white));
                d.AddManipulator(new Clickable(() => { Ayar.RGOR[k] = rid; Ayar.Kaydet(); Tik(); root.vitrin.Yenile(); SoyunmaGovdeKur(true); }));
                Erisim.Etiket(d, L.T(ad) + ", " + L.T(Vitrin.RAK_AD[id]), "dugme", on); s.Add(d);
            }
        }

        void YuvaKur(VisualElement c, Yuva Y, int L_)
        {
            var gr = Ayar.gear; int sv = gr.eko.sv[Y.k]; bool tam = sv >= 10; int f = tam ? 0 : Gear.YUK_FIYAT[sv - 1], gk = tam ? 0 : Gear.YUK_SV[sv - 1];
            bool kilit = !tam && L_ < gk, az = !tam && gr.eko.tuy < f;
            var kart = Kart("yuva"); c.Add(kart);
            var r = Satir("yuva-satir"); kart.Add(r);
            var sol = new VisualElement(); sol.AddToClassList("yuva-sol"); sol.pickingMode = PickingMode.Ignore; r.Add(sol);
            sol.Add(Yazi(L.T(Y.ad), 16, "kalin"));
            var bol = Satir("seviye-bolme"); bol.pickingMode = PickingMode.Ignore; sol.Add(bol);
            for (int i = 0; i < 10; i++) { var b = new VisualElement(); b.AddToClassList("bolme"); if (i < sv) b.AddToClassList("bolme--dolu"); b.pickingMode = PickingMode.Ignore; bol.Add(b); }
            sol.Add(Yazi(sv + "/10 · " + L.T(Y.n), 12, "soluk"));
            string ne = tam ? L.T("Tam") : kilit ? L.T("Sv {0} gerekli", gk) : az ? f + " " + L.T("Tüy") : L.T("Yükselt · {0}", f + " " + L.T("Tüy"));
            VisualElement dg;
            if (tam || kilit || az) dg = Pasif(ne);   // kapı varsa nedeni yazar, düğme pasif ama okunur (§2.3)
            else dg = Dugme(ne, () =>
            {
                var once = gr.YukDegerler(Y.k, 0);
                if (gr.Yukselt(Y.k)) { Ayar.GearHesap(); Ayar.Kaydet(); root.sfx.Yukselt(); Yerel.Hap("flow"); root.vitrin.Parlat(Y.k); yukOnce = once; yukK = Y.k; ustKirli = true; UstKur(); SoyunmaGovdeKur(true); }
            }, "dugme--birincil");
            dg.AddToClassList("yuva-dugme"); r.Add(dg);
            Erisim.Etiket(kart, L.T(Y.ad) + ", " + sv + "/10", "metin");
            if (!tam) Onizle(kart, Y.k);
        }

        // Yükseltme önizlemesi (HTML yukOnizle): çubuk 1. seviyeden 10. seviyeye; gri = şimdi, yeşil uç = yükseltince.
        void Onizle(VisualElement kart, string k)
        {
            var gr = Ayar.gear; int sv = gr.eko.sv[k]; var su = gr.YukDegerler(k, 0); var son = gr.YukDegerler(k, 1); var ta = gr.YukDegerler(k, 1 - sv); var tav = gr.YukDegerler(k, 10 - sv);
            bool var_ = false; bool kay = yukK == k && yukOnce != null && !Ayar.reduce;
            foreach (var a in Gear.YUK_ETKI[k])
            {
                float d = son[a] / su[a] - 1; if (d < .002f) continue; var_ = true; string aa = a;
                System.Func<float, float> W_ = v => Mathf.Clamp(4 + (v - ta[aa]) / (Mathf.Abs(tav[aa] - ta[aa]) > 1e-6f ? tav[aa] - ta[aa] : 1) * 96, 4, 100);
                var r = Satir("onizle"); r.pickingMode = PickingMode.Ignore; kart.Add(r);
                r.Add(Yazi(L.T(Gear.ETKI_AD[a]), 12, "onizle-ad"));
                var iz = new VisualElement(); iz.AddToClassList("onizle-iz"); iz.pickingMode = PickingMode.Ignore; r.Add(iz);
                var yes = new VisualElement(); yes.AddToClassList("onizle-yesil"); yes.style.width = Length.Percent(W_(son[a])); iz.Add(yes);
                var gri = new VisualElement(); gri.AddToClassList("onizle-gri"); iz.Add(gri);
                float hedef = W_(su[a]);
                if (kay && yukOnce.ContainsKey(a)) { gri.style.width = Length.Percent(W_(yukOnce[a])); gri.AddToClassList("onizle-kayar"); gri.schedule.Execute(() => gri.style.width = Length.Percent(hedef)).StartingIn(33); }
                else gri.style.width = Length.Percent(hedef);
                r.Add(Yazi("+" + Mathf.Max(1, Mathf.RoundToInt(d * 100)) + "%", 12, "onizle-yuzde"));
                Erisim.Etiket(r, L.T(Gear.ETKI_AD[a]) + " +" + Mathf.Max(1, Mathf.RoundToInt(d * 100)) + "%", "metin");
            }
            if (yukK == k) { yukK = null; yukOnce = null; }
            if (var_ && gr.eko.esit) kart.Add(Yazi(L.T("Eşit şartlar açık: serbest maçta sayılmaz, kariyerde sayılır"), 12, "soluk"));
        }

        void Modeller(VisualElement c, string k, string ad)
        {
            var gr = Ayar.gear; c.Add(Grup(L.T(ad)));
            string cur = k == "raket" ? gr.raket : k == "bileklik" ? gr.bileklik : k == "bant" ? gr.bant : gr.forma;
            var izgara = Satir("izgara"); c.Add(izgara);
            foreach (var kv in Gear.GEAR[k])
            {
                var it = kv.Value; bool on = cur == kv.Key, kilit = it.kilit && !Ayar.sampiyon; string id = kv.Key;
                VisualElement d;
                if (kilit) { d = Pasif(L.T(it.ad), L.T(it.n)); var ki = Ikon(Ikonlar.Kilit, 14, SOLUK); ki.AddToClassList("kose-ikon"); d.Add(ki); }
                else d = Cip(L.T(it.ad), on, () =>
                {
                    if (k == "raket") gr.raket = id; else if (k == "bileklik") gr.bileklik = id; else if (k == "bant") gr.bant = id; else gr.forma = id;
                    Ayar.GearHesap(); Ayar.Kaydet(); root.vitrin.Yenile(); if (k == "raket") { root.sfx.Deneme(); Yerel.Vurus("smash", "perfect", false); } else root.sfx.Tik();
                    SoyunmaGovdeKur(true);
                }, L.T(it.n));
                d.AddToClassList("model");
                if (it.renk != null)
                {   // renk örneği (ad da yazılı: yalnız renkle değil)
                    var n = new VisualElement(); n.AddToClassList("renk-nokta"); n.AddToClassList("model-renk"); n.style.backgroundColor = Kit.Hex(it.renk); n.pickingMode = PickingMode.Ignore; d.Add(n);
                }
                izgara.Add(d);
            }
        }

        void TelKur(VisualElement c)
        {
            var gr = Ayar.gear;
            telYazi = Yazi(TelBaslik(), 15, "grup-kalin"); c.Add(telYazi);
            var r = Satir("tel-satir"); c.Add(r);
            var eksi = Dugme("−", () => TelAdim(-1), "ikon-dugme"); Erisim.Etiket(eksi, L.T("Tel gerginliği") + " −1", "dugme"); r.Add(eksi);
            var sl = new Slider(18, 32) { value = gr.tel }; sl.AddToClassList("tel-kaydirici"); r.Add(sl);
            Erisim.Etiket(sl, L.T("Tel gerginliği"), "kaydirici", false, false, Mathf.RoundToInt(gr.tel) + " lbs");
            sl.RegisterValueChangedCallback(e =>
            {
                float yeni = Mathf.Round(e.newValue); if (yeni == gr.tel) return; gr.tel = yeni; Ayar.GearHesap(); telDegisti = true; TelYaz();
            });
            // bırakınca kaydet + deneme smacı (HTML: deneme smacı titreşimiyle); tutamak işaretçiyi yakaladığı için TrickleDown
            sl.RegisterCallback<PointerUpEvent>(e => TelBitti(), TrickleDown.TrickleDown);
            sl.RegisterCallback<PointerCaptureOutEvent>(e => TelBitti(), TrickleDown.TrickleDown);
            var arti = Dugme("+", () => TelAdim(1), "ikon-dugme"); Erisim.Etiket(arti, L.T("Tel gerginliği") + " +1", "dugme"); r.Add(arti);
            telNot = Yazi(L.T(Gear.TelStats(gr.tel).n), 12, "soluk"); c.Add(telNot);
        }
        static string TelBaslik() { return L.T("Tel gerginliği") + " · " + Mathf.RoundToInt(Ayar.gear.tel) + " lbs"; }
        void TelYaz()
        {
            if (telYazi != null) telYazi.text = TelBaslik(); if (telNot != null) telNot.text = L.T(Gear.TelStats(Ayar.gear.tel).n); if (statYazi != null) statYazi.text = StatSatiri();
        }
        void TelAdim(int d)
        {
            var gr = Ayar.gear; float yeni = Mathf.Clamp(gr.tel + d, 18, 32); if (yeni == gr.tel) return;
            gr.tel = yeni; Ayar.GearHesap(); telDegisti = true; var sl = soyunmaGovde != null ? soyunmaGovde.Q<Slider>() : null; if (sl != null) sl.SetValueWithoutNotify(yeni); TelYaz(); TelBitti();
        }
        void TelBitti()
        {
            if (!telDegisti) return; telDegisti = false; Ayar.Kaydet(); root.sfx.Deneme(); Yerel.Vurus("smash", "perfect", false);
            var sl = soyunmaGovde != null ? soyunmaGovde.Q<Slider>() : null; if (sl != null) { var b = sl.userData as ErisimBilgi; if (b != null) b.deger = Mathf.RoundToInt(Ayar.gear.tel) + " lbs"; }
        }
    }
}
