// Duyarlı yerleşim (2026-10-27, yasin build 41: "menüler ve sayfalar responsive, her telefonda açıkça gözükmeli").
// Ölçüm (r42, 390×845, güvenli alansız): panel her telefonda 375 birim genişlik (ScaleWithScreenSize, genişliğe eşleme), bu
// yüzden yatayda telefonlar arası fark yok; fark DİKEYDE: kullanılabilir yükseklik iPhone SE'de 667, iPhone 13'te çentik
// (47 pt) ve ev çizgisi (34 pt) düşünce ~734, Pro Max'te ~732 birim; CI karesi (812, güvenli alan yok) gerçek telefonların
// hepsinden uzundu, sorunlar karede görünmüyordu. Sayfa başına yama yerine dört kural:
//  1) Boyut sınıfı: güvenli alan düşüldükten sonra panel yüksekliği < 700 birim → kökte .kisa (SE, 8 Plus, zoom'lu
//     ekranlar). Kısa ekranda yalnız süs ve boşluk küçülür (afiş alanı, logo, OYNA ve plaka başlığı, kutucuk, vitrin,
//     şerit yükseklikleri); dokunma alanı 44 altına, yazı 11 pt altına inmez. Panel genişliği ≥ 500 → .genis (iPad):
//     içerik 560 birimlik ortalanmış sütun, alt sayfa aynı genişlikte; sekme çubuğu tam genişlik (iPadOS kalıbı).
//  2) Ölçek eşlemesi ekran boyu değişince yeniden seçilir (eskiden yalnız kurulumda: tur Boyut()'la ya da iPad'de bölünmüş
//     ekran gelseydi eşleme eski kalırdı). Telefon (en/boy ≤ 0,6) genişliğe, iPad yüksekliğe eşlenir; sınıf kararı panel
//     boyunun formülünden (yerleşimi beklemeden) verilir, ilk kurulum doğru sınıfla yapılır.
//  3) Oyna'nın VS afişi sabit yüzde (üst %8, boy %54, ScaleAndCrop) yerine üst şerit ile plaka arasındaki gerçek boşluğa
//     yerleşir; kutu afiş dokusunun en-boyunu korur (SE'de eskiden kutu 375×360 iken doku 640×748: başlar ve raketler
//     kırpılıyor, figürlerin yarısı plakanın altında kalıyordu). Alt %26'sı tasarımdaki gibi plakanın arkasında.
//  4) Alt sayfaların birincil düğmesi kaydırmanın dışında, sayfanın dibinde sabit (iOS sayfa kalıbı): SE'de Hızlı maç,
//     maç kartı, görev ve Günün maçı düğmesi kaydırma sonuna düşüp ilk bakışta görünmüyordu.
// Ayrıca TasmaDenetimi: ekran turunun her karesinde görünür öğeleri tarar (güvenli alan, sekme çubuğu çakışması, kesik
// yazı, 11 pt altı yazı, 44 pt altı dokunma alanı, çakışan dokunma alanı, etiketsiz düğme) ve rapor.txt'ye yazar.
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace BirdieSmash.Game
{
    // Güvenli alan tek kaynak (menü ve HUD). Ekran turu çentik ve ev çizgisini taklit etmek için piksel payı yazar (xvfb'de
    // Screen.safeArea tam ekran); mağaza build'inde test hiç açılmaz.
    public static class Guvenli
    {
        public static bool test; public static float ust, alt, sol, sag;   // piksel
        public static Rect Alan
        {
            get
            {
                if (!test) return Screen.safeArea;
                float W = Screen.width, H = Screen.height; return new Rect(sol, alt, Mathf.Max(1, W - sol - sag), Mathf.Max(1, H - ust - alt));
            }
        }
    }

    public partial class UiKok
    {
        public const float KISA_ESIK = 700, GENIS_ESIK = 500, SUTUN_EN = 560, AFIS_ALT_PAY = .26f;
        public bool Kisa { get; private set; }
        public bool Genis { get; private set; }
        int dSonW = -1, dSonH = -1; Rect dSonSafe;
        // Kısa ekranda küçülen taban boyut (yazı ve süs): normal / kısa.
        float Px(float normal, float kisa) { return Kisa ? kisa : normal; }

        // Panel boyu formülden (ScaleWithScreenSize, MatchWidthOrHeight 0 ya da 1; referans 375 × 812).
        static void PanelBoyu(out float pw, out float ph)
        {
            float W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height);
            if (W / H > .6f) { ph = 812; pw = 812 * W / H; } else { pw = 375; ph = 375 * H / W; }
        }

        // Ekran ya da güvenli alan değiştiyse eşleme ve boyut sınıfı. Dönüş: sınıf değişti (menü yeniden kurulmalı).
        bool DuyarliGuncelle()
        {
            var sa = Guvenli.Alan; if (Screen.width == dSonW && Screen.height == dSonH && sa == dSonSafe) return false;
            dSonW = Screen.width; dSonH = Screen.height; dSonSafe = sa;
            var p = doc != null ? doc.panelSettings : null;
            if (p != null && Screen.height > 0) { float m = (float)Screen.width / Screen.height > .6f ? 1 : 0; if (p.match != m) p.match = m; }
            float pw, ph; PanelBoyu(out pw, out ph); float s = pw / Mathf.Max(1, Screen.width);
            float kullan = ph - Mathf.Max(0, Screen.height - sa.yMax) * s - Mathf.Max(0, sa.yMin) * s;
            bool k = kullan < KISA_ESIK, g = pw >= GENIS_ESIK, d = k != Kisa || g != Genis; Kisa = k; Genis = g;
            if (kok != null) { kok.EnableInClassList("kisa", k); kok.EnableInClassList("genis", g); }
            return d;
        }

        // iPad: içerik sütunu ortada; telefonda 0.
        void SutunYaz(float pw)
        {
            // Dış pay (margin), dolgu değil: tam sayfa (.sayfa.tam) alan'ın mutlak çocuğu, dolguyu görmez (GuvenliAlan'daki not).
            if (alan == null) return; float yan = Genis ? Mathf.Max(0, (pw - guvPay.y - guvPay.z - SUTUN_EN) / 2) : 0;
            if (float.IsNaN(yan)) yan = 0; alan.style.marginLeft = yan; alan.style.marginRight = yan;
        }

        // ---- 3) afiş: üst şerit ile plaka arasındaki boşluğa, dokunun en-boyuyla ----
        VisualElement oynaSahne;
        void AfisYerlestir()
        {
            var sahne = oynaSahne; if (afisImg == null || vsYazi == null || sahne == null || sahne.panel == null || zemin == null) return;
            var z = zemin.worldBound; if (float.IsNaN(z.width) || z.width <= 1) return;
            float ustAlt = (ust.resolvedStyle.display == DisplayStyle.None ? z.y : ust.worldBound.yMax) - z.y, plaka = sahne.worldBound.yMax - z.y, A = plaka - ustAlt;
            var rt = root.afis != null ? root.afis.rt : null; float asp = rt != null && rt.height > 0 ? (float)rt.width / rt.height : .856f;
            if (K >= 1.35f || A < 120 || float.IsNaN(A))
            {   // büyük yazıda içerik kayar, afiş soluk süs (USS .buyuk-yazi): eski yüzde yerleşimi
                afisImg.style.left = StyleKeyword.Null; afisImg.style.right = StyleKeyword.Null; afisImg.style.top = StyleKeyword.Null; afisImg.style.width = StyleKeyword.Null; afisImg.style.height = StyleKeyword.Null;
                vsYazi.style.left = StyleKeyword.Null; vsYazi.style.top = StyleKeyword.Null; vsYazi.style.fontSize = StyleKeyword.Null; return;
            }
            float h = A / (1 - AFIS_ALT_PAY), w = h * asp; if (w > z.width) { w = z.width; h = w / asp; }
            float sol = (z.width - w) / 2, ust_ = plaka - (1 - AFIS_ALT_PAY) * h;
            afisImg.style.left = sol; afisImg.style.right = StyleKeyword.Auto; afisImg.style.top = ust_; afisImg.style.width = w; afisImg.style.height = h;
            // VS: 390×845 tasarım karesindeki yeri (kutunun %55 sağı, %42,6 aşağısı), boyu kutuyla (438 birimde 44)
            vsYazi.style.left = sol + .55f * w; vsYazi.style.top = ust_ + .426f * h; vsYazi.style.fontSize = Mathf.Round(44 * Mathf.Clamp(h / 438f, .75f, 1.1f));
        }

        // ---- 4) alt sayfanın birincil eylemi: kaydırmanın dışında, dipte sabit ----
        void AltEylem(VisualElement d) { if (altSayfa == null || d == null) return; d.AddToClassList("alt-eylem"); altSayfa.Add(d); }

        // ---- ekran turu denetimi ----
        // Görünür öğeleri tarar; panel birimi → pt: turda pencere pt cinsinden açılır (1 piksel = 1 pt), birim = W / panel eni.
        public string TasmaDenetimi(string ad, out int sorun)
        {
            sorun = 0; if (kok == null || doc == null || doc.rootVisualElement == null) return "taşma " + ad + " · menü yok";
            var r0 = doc.rootVisualElement; float pw = r0.layout.width, ph = r0.layout.height;
            if (float.IsNaN(pw) || pw <= 0) return "taşma " + ad + " · yerleşim yok";
            float pt = Screen.width / pw, s = pw / Screen.width; var sa = Guvenli.Alan;
            var guv = new Rect(sa.xMin * s, (Screen.height - sa.yMax) * s, sa.width * s, sa.height * s);
            bool altAcik = altKatman.resolvedStyle.display != DisplayStyle.None;
            Rect sekme = sekmeCubugu.resolvedStyle.display == DisplayStyle.None ? new Rect(0, 0, 0, 0) : sekmeCubugu.worldBound;
            var sb = new StringBuilder(); var oge = new List<VisualElement>(); var svl = new List<ScrollView>();
            Topla_(altAcik ? altKatman : kok, oge, svl, altAcik);
            if (altAcik) Topla_(sekmeCubugu, oge, svl, true);   // sayfa sekme çubuğunu da örter: çubuk yine denetlenir (çakışma hariç)
            var dokun = new List<KeyValuePair<VisualElement, Rect>>(); int n = 0;
            foreach (var sv in svl)
            {
                var v = sv.contentViewport.worldBound;
                if (Disinda(v, guv)) Sorun(sb, ref sorun, "kaydırma alanı güvenli alan dışında", Ad(sv), v);
                if (!altAcik && !Altinda(sv, sekmeCubugu) && Kesisim(v, sekme)) Sorun(sb, ref sorun, "kaydırma alanı sekme çubuğunun altında", Ad(sv), v);
            }
            foreach (var v in oge)
            {
                n++; var r = v.worldBound; bool kaydir; var kirp = Kirp(v, out kaydir); var gor = kaydir ? Kes(r, kirp) : r;
                bool cubukta = Altinda(v, sekmeCubugu);
                if (!kaydir && Disinda(r, guv)) Sorun(sb, ref sorun, "güvenli alan / ekran dışında", Ad(v), r);
                if (kaydir && (r.xMin < kirp.xMin - 1 || r.xMax > kirp.xMax + 1)) Sorun(sb, ref sorun, "yatay taşma (kaydırılamaz)", Ad(v), r);
                if (!cubukta && !altAcik && gor.height > 1 && Kesisim(gor, sekme)) Sorun(sb, ref sorun, "sekme çubuğuyla çakışıyor", Ad(v), r);
                var l = v as Label;
                if (l != null)
                {
                    float fs = l.resolvedStyle.fontSize * pt; if (fs < 10.99f) Sorun(sb, ref sorun, "küçük yazı " + fs.ToString("F1") + " pt", Ad(v), r);
                    float cw = l.contentRect.width, chh = l.contentRect.height;
                    if (cw > 1 && !float.IsNaN(cw))
                    {
                        if (l.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
                        { var m = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined); if (m.x > cw + 2) Sorun(sb, ref sorun, "yazı sığmıyor " + m.x.ToString("F0") + ">" + cw.ToString("F0"), Ad(v), r); }
                        else { var m = l.MeasureTextSize(l.text, cw, VisualElement.MeasureMode.AtMost, 0, VisualElement.MeasureMode.Undefined); if (m.y > chh + 2) Sorun(sb, ref sorun, "yazı kesik " + m.y.ToString("F0") + ">" + chh.ToString("F0"), Ad(v), r); }
                        // 2026-10-31 (yasin IMG_1517, Profil unvanları "Mahal/le/oyunc/usu"): en uzun sözcük kaba sığmıyorsa sarma onu
                        // harf harf böler; yükseklik sığdığı için "kesik" yakalamıyordu.
                        if (l.resolvedStyle.whiteSpace != WhiteSpace.NoWrap)
                        {
                            string uzun = ""; foreach (var sz in l.text.Split(' ', '\n')) if (sz.Length > uzun.Length) uzun = sz;
                            if (uzun.Length > 1) { var mw = l.MeasureTextSize(uzun, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined); if (mw.x > cw + 2) Sorun(sb, ref sorun, "sözcük bölünüyor \"" + uzun + "\" " + mw.x.ToString("F0") + ">" + cw.ToString("F0"), Ad(v), r); }
                        }
                    }
                    var e = v.hierarchy.parent; if (e != null && (r.xMax > e.worldBound.xMax + 6 || r.xMin < e.worldBound.xMin - 6)) Sorun(sb, ref sorun, "yazı kabından taşıyor", Ad(v), r);
                    continue;
                }
                var b = v.userData as ErisimBilgi;
                if (b == null) { Sorun(sb, ref sorun, "etiketsiz düğme (VoiceOver)", Ad(v), r); continue; }
                if (b.rol != "dugme" && b.rol != "sekme") continue;
                if (!b.pasif && (r.width * pt < 43.5f || r.height * pt < 43.5f)) Sorun(sb, ref sorun, "dokunma alanı " + (r.width * pt).ToString("F0") + "×" + (r.height * pt).ToString("F0") + " pt", Ad(v), r);
                if (gor.width > 2 && gor.height > 2)
                {
                    foreach (var o in dokun)
                        if (!Altinda(v, o.Key) && !Altinda(o.Key, v) && Kes(gor, o.Value).width > 2 && Kes(gor, o.Value).height > 2) Sorun(sb, ref sorun, "dokunma alanı çakışıyor: " + Ad(o.Key), Ad(v), r);
                    dokun.Add(new KeyValuePair<VisualElement, Rect>(v, gor));
                }
            }
            var bas = "taşma " + ad + " · ekran " + Screen.width + "x" + Screen.height + " · panel " + pw.ToString("F0") + "x" + ph.ToString("F0") + " · 1 birim " + pt.ToString("F2") + " pt · güvenli üst "
                + ((Screen.height - sa.yMax) * s).ToString("F0") + " alt " + (sa.yMin * s).ToString("F0") + (Kisa ? " · kısa" : "") + (Genis ? " · geniş" : "") + " · yazı ×" + K.ToString("F2") + " · " + n + " öğe · " + sorun + " sorun";
            return bas + sb.ToString();
        }
        static int sorunSinir = 40;
        static void Sorun(StringBuilder sb, ref int n, string tur, string ad, Rect r)
        {
            n++; if (n > sorunSinir) return;
            sb.Append("\n   ! " + tur + " · " + ad + " · [" + r.x.ToString("F0") + "," + r.y.ToString("F0") + " " + r.width.ToString("F0") + "x" + r.height.ToString("F0") + "]");
        }
        // Görünür yazılar ve dokunulabilir öğeler (zemin süsü ve perde kalkanı hariç).
        void Topla_(VisualElement v, List<VisualElement> oge, List<ScrollView> svl, bool altAcik)
        {
            if (v == null) return; var rs = v.resolvedStyle;
            if (rs.display == DisplayStyle.None || rs.visibility == Visibility.Hidden || rs.opacity <= .01f) return;
            if (v == zemin || v == kalkan || (!altAcik && v == altKatman)) return;
            var sv = v as ScrollView; if (sv != null) svl.Add(sv);
            var l = v as Label; var b = v.userData as ErisimBilgi;
            if (l != null) { if (!string.IsNullOrEmpty(l.text) && !(v.parent is TextField) && !Altinda(v, "unity-base-field")) oge.Add(v); }
            else if (b != null ? (b.rol == "dugme" || b.rol == "sekme") : v.ClassListContains("dugme")) oge.Add(v);
            for (int i = 0; i < v.hierarchy.childCount; i++) Topla_(v.hierarchy[i], oge, svl, altAcik);
        }
        static bool Altinda(VisualElement v, VisualElement ata) { for (var p = v; p != null; p = p.hierarchy.parent) if (p == ata) return true; return false; }
        static bool Altinda(VisualElement v, string cls) { for (var p = v.hierarchy.parent; p != null; p = p.hierarchy.parent) if (p.ClassListContains(cls)) return true; return false; }
        // En yakın kaydırma görünümlerinin kesişimi (öğe kaydırılabilir alanda mı).
        static Rect Kirp(VisualElement v, out bool kaydir)
        {
            kaydir = false; var k = new Rect(-1e5f, -1e5f, 2e5f, 2e5f);
            for (var p = v.hierarchy.parent; p != null; p = p.hierarchy.parent) { var sv = p as ScrollView; if (sv != null) { kaydir = true; k = Kes(k, sv.contentViewport.worldBound); } }
            return k;
        }
        static Rect Kes(Rect a, Rect b) { float x0 = Mathf.Max(a.xMin, b.xMin), y0 = Mathf.Max(a.yMin, b.yMin), x1 = Mathf.Min(a.xMax, b.xMax), y1 = Mathf.Min(a.yMax, b.yMax); return x1 > x0 && y1 > y0 ? Rect.MinMaxRect(x0, y0, x1, y1) : new Rect(x0, y0, 0, 0); }
        static bool Kesisim(Rect a, Rect b) { var k = Kes(a, b); return k.width > 1 && k.height > 1; }
        static bool Disinda(Rect r, Rect g) { return r.xMin < g.xMin - 1 || r.yMin < g.yMin - 1 || r.xMax > g.xMax + 1 || r.yMax > g.yMax + 1; }
        static string Ad(VisualElement v)
        {
            var b = v.userData as ErisimBilgi; if (b != null && !string.IsNullOrEmpty(b.etiket)) return Kisalt(b.etiket);
            var l = v as Label; if (l != null) return "\"" + Kisalt(l.text) + "\"";
            var sb = new StringBuilder(v.GetType().Name); foreach (var c in v.GetClasses()) sb.Append("." + c); return sb.ToString();
        }
        static string Kisalt(string t) { t = t.Replace("\n", " "); return t.Length > 40 ? t.Substring(0, 40) + "…" : t; }
    }
}
