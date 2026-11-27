// Gelişim sayfası (2026-11-10, Core/Gelisim): oyuncunun kendi beş değeri. Her seviye bir puan verir; oyuncu burada dağıtır,
// istediği zaman ücretsiz geri alır. Sayfa üç yerden açılır: Oyna'nın altındaki satır (dağıtılmamış puan varken), Profil'deki
// "Gelişim" kartı ve EkranTuru. Görsel dil Seviye yolu ile aynı (sv-yol kartı); süs, parıltı, "YENİ" yok. Her dal satırı:
// ad + şu anki etki yüzdesi, tek cümle ne işe yaradığı, 10 basamaklı çubuk, 44 pt − ve + düğmeleri. Durum yalnız renkle
// verilmez: basamak sayısı "3/10" ve yüzde yazıyla da durur. Eşit şartlar açıkken etkisiz olduğu sayfanın başında yazar.
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        void GelisimKur()
        {
            var c = AltSayfaKur(L.T("Gelişim"));
            var p = Ayar.gelisim; int sv = Ayar.gear.Seviye(), bos = Ayar.GelisimBos, top = Gelisim.Toplam(sv);
            var bas = Satir("arali"); bas.pickingMode = PickingMode.Ignore; c.Add(bas);
            // Sv 1'de puan yok: "hepsi dağıtıldı" yanlış olurdu, ilk puanın ne zaman geleceği yazar.
            // 2026-11-20 (inceleme, fikir 17:06 #6; OYUNCUYU GÖSTER): "oyuncunu güçlendir" denen sayfada oyuncu yoktu (Oyna'dan
            // açılınca bağlam kayboluyordu). Başlığın solunda 36 pt portre; üst çubukta çizilen önbellekteki doku (Portre.Benim),
            // yeni figür/RT yok. Dokunuş yok: sayfanın tek işi puan. Yoksa eski insan simgesi (AvatarIc).
            var bsol = Satir("rakip-satir"); bsol.pickingMode = PickingMode.Ignore; bsol.style.marginTop = 0; bsol.style.flexShrink = 1f; bas.Add(bsol);
            // 2026-11-27 inceleme (fikir 12:06 #2): 36 → 44 pt; puan verilince portre kısa bir yayla büyür (PortrePop), "güçlenen benim".
            // 56–64 önerildi ama büyük yazıda başlık satırı sarıyor; 44 orta yol.
            var av = new VisualElement(); av.AddToClassList("avatar"); av.pickingMode = PickingMode.Ignore; av.style.width = 44; av.style.height = 44;
            av.style.borderTopLeftRadius = av.style.borderTopRightRadius = av.style.borderBottomLeftRadius = av.style.borderBottomRightRadius = 22;
            av.style.marginRight = 8; av.style.backgroundColor = Kit.Hex(Ayar.FORMA); BenAvatar(av, 24); bsol.Add(av); gelPortre = av;
            // 2026-11-22 inceleme (fikir 12:06 #4, §7.4): "Dağıtılacak puan: 3" yanında "7/10 puan" aynı veriyi iki kez söylüyordu,
            // büyük yazıda başlık satırını sarıyordu. Tek etiket: kalan / toplam; bitince "10 puanın hepsi dağıtıldı".
            gelUst = Yazi(GelUstMetin(top, bos), 16, "kalin"); gelUst.style.whiteSpace = WhiteSpace.Normal; gelUst.style.flexShrink = 1; bsol.Add(gelUst);
            // 2026-11-15 (inceleme, fikir 22:05 #5): iki varyantlı uzun paragraf her açılışta aynı kuralı tekrar ediyor, beş dal
            // kartını ilk ekranın altına itiyordu (büyük yazıda iki dal görünürdü). Tek kısa satır; Sv 1'de üst satır yeter.
            // 2026-11-27 inceleme (fikir 12:06 #4, §10.2): "en çok 10" her kartın "n/10"unu tekrar ediyordu; yeni bilgi yalnız geri alınabilmesi.
            if (top > 0) { var ac = Yazi(L.T("Puanları istediğin an geri alabilirsin."), 13, "soluk"); ac.style.whiteSpace = WhiteSpace.Normal; ac.style.marginTop = 4; ac.style.marginBottom = 6; c.Add(ac); }
            if (Ayar.gear.eko.esit && !Ayar.kariyerModu) { var es = Yazi(L.T("Eşit şartlar açık: gelişim bu maçlarda sayılmaz."), 13, "ipucu"); es.style.whiteSpace = WhiteSpace.Normal; es.style.marginBottom = 6; c.Add(es); }
            c.Add(GelRadarKur(p));
            foreach (var i in Gelisim.GORUNEN) c.Add(GelisimDal(i, p[i], bos > 0, top > 0));   // 2026-11-26: üç dal (Core/Gelisim)
        }

        // 2026-11-26 (yasin: "Growth kısmı düzgün çalışmıyor, orası animasyon ve görsel olarak tatmin etmeli"; "Growth ilerledikçe
        // skillerim gelişmeli"): sayfa beş düz kart ve küçük basamaklardan ibaretti; puan vermek "oyuncum gelişti" duygusunu
        // vermiyordu. Üstte beş dallı yetenek şekli (spor oyunlarının oyuncu kartındaki beşgen): eksenler dalların doluluğu,
        // limon alan = senin oyuncun (§8 "senin ilerlemen"). Puan verilince alan .28 sn'de yeni değerine esneyerek büyür, geri
        // alınca küçülür; hareketi azaltta anında. Çizim Painter2D, yalnız değer değişirken (16 ms, en çok ~20 kare) yeniden çizilir.
        // Kılavuz: 5 ve 10 halkası; adlar köşelerde. 2026-11-26 iki dal kalkınca beşgen üçgene döndü (eksen sayısı GORUNEN'den). Sayı ve "3/10" kartlarda kalır (renk tek başına anlam taşımaz).
        VisualElement gelRadar; readonly float[] gelRGor = new float[RN], gelRHedef = new float[RN];
        static readonly int RN = Gelisim.GORUNEN.Length;
        IVisualElementScheduledItem gelRAni; float gelRAniT; readonly float[] gelRBas = new float[RN];
        readonly Label[] gelREt = new Label[RN];
        VisualElement GelRadarKur(int[] p)
        {
            var kap = new VisualElement(); kap.AddToClassList("gel-radar-kap"); kap.pickingMode = PickingMode.Ignore;
            var r = new VisualElement(); r.AddToClassList("gel-radar"); r.pickingMode = PickingMode.Ignore; kap.Add(r); gelRadar = r;
            for (int i = 0; i < RN; i++) { int d = Gelisim.GORUNEN[i]; gelRGor[i] = gelRHedef[i] = p[d] / (float)Gelisim.TAVAN; var l = Yazi(L.T(Gelisim.AD[d]), 12, "gel-radar-ad");   // 2026-11-27: 12 pt alt sınır (fikir 12:06 #3) l.AddToClassList("dokunulur"); kap.Add(l); gelREt[i] = l;
                l.AddManipulator(new Clickable(() => GelDalaGit(d))); Erisim.Etiket(l, L.T("{0} dalına git", L.T(Gelisim.AD[d])), "dugme"); }
            r.generateVisualContent += GelRadarCiz;
            kap.RegisterCallback<GeometryChangedEvent>(e => GelRadarEtiket(kap));
            var et = ""; for (int i = 0; i < RN; i++) { int d = Gelisim.GORUNEN[i]; et += (i > 0 ? ", " : "") + L.T(Gelisim.AD[d]) + " " + p[d] + "/" + Gelisim.TAVAN; }
            Erisim.Etiket(kap, L.T("Gelişim") + ": " + et, "resim");
            return kap;
        }
        // 2026-11-26 inceleme (fikir 17:06 #4; FIFA/Brawl Stars yetenek ekranı): köşe adları yalnız resimdi, "Kontrol"e dokunan oyuncuya
        // hiçbir şey olmuyordu. Ad dokunulur (44 pt görünmez alan USS'te, tek yay basış ölçeği): o dalın kartına .25 sn kayar ve kart
        // bir kez parlar. Hareketi azaltta kaydırma anında, parlama yok. Ayırma yok (kart dizisi kurulumda dolar).
        readonly VisualElement[] gelKart = new VisualElement[Gelisim.DAL];
        void GelDalaGit(int dal)
        {
            var k = gelKart[dal]; if (k == null || k.panel == null) return; Tik();
            var sv = k.GetFirstAncestorOfType<ScrollView>(); if (sv != null) YumusakKaydir(sv, k);
            if (!Ayar.reduce) KartParla(k);
        }
        static Vector2 GelKose(int i, Vector2 m, float yr) { float a = -Mathf.PI / 2 + i * 2 * Mathf.PI / RN; return m + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * yr; }
        const float GEL_AD_PX = 10, GEL_AD_PY = 16;   // .gel-radar-ad dolgusu (yatay, dikey), USS ile aynı
        void GelRadarEtiket(VisualElement kap)
        {
            if (gelRadar == null) return; var rb = gelRadar.layout; if (float.IsNaN(rb.width) || rb.width < 10) return;
            var m = rb.center; float yr = rb.width / 2 + 4;
            for (int i = 0; i < RN; i++)
            {
                var l = gelREt[i]; if (l == null) continue; var k = GelKose(i, m, yr); var w = Mathf.Max(0, l.layout.width - 2 * GEL_AD_PX); var h = Mathf.Max(0, l.layout.height - 2 * GEL_AD_PY); if (float.IsNaN(w)) continue;   // yazının kendisi; dolgu dokunma alanı (USS)
                float x = Mathf.Abs(k.x - m.x) < 2 ? k.x - w / 2 : k.x > m.x ? k.x : k.x - w; float y = i == 0 ? k.y - h : Mathf.Abs(k.y - m.y) < rb.height * .2f ? k.y - h / 2 : k.y;
                l.style.left = Mathf.Clamp(x, 0, Mathf.Max(0, kap.layout.width - w)); l.style.top = y;
            }
        }
        void GelRadarCiz(MeshGenerationContext mgc)
        {
            var r = gelRadar; if (r == null) return; float W = r.contentRect.width, H = r.contentRect.height; if (float.IsNaN(W) || W < 10) return;
            var pz = mgc.painter2D; var m = new Vector2(W / 2, H / 2); float yr = Mathf.Min(W, H) / 2 - 2;
            pz.lineJoin = LineJoin.Round;
            for (int h = 1; h <= 2; h++)
            {   // kılavuz halkaları (5 ve 10) + eksenler
                pz.BeginPath(); for (int i = 0; i < RN; i++) { var k = GelKose(i, m, yr * h / 2f); if (i == 0) pz.MoveTo(k); else pz.LineTo(k); } pz.ClosePath();
                pz.strokeColor = new Color(1, 1, 1, h == 2 ? .28f : .14f); pz.lineWidth = 1; pz.Stroke();
            }
            pz.BeginPath(); for (int i = 0; i < RN; i++) { pz.MoveTo(m); pz.LineTo(GelKose(i, m, yr)); } pz.strokeColor = new Color(1, 1, 1, .1f); pz.Stroke();
            float top = 0; for (int i = 0; i < RN; i++) top += gelRGor[i]; if (top <= .001f) return;
            pz.BeginPath();
            for (int i = 0; i < RN; i++) { var k = GelKose(i, m, yr * Mathf.Max(.04f, gelRGor[i])); if (i == 0) pz.MoveTo(k); else pz.LineTo(k); }
            pz.ClosePath(); pz.fillColor = new Color(LIMON.r, LIMON.g, LIMON.b, .38f); pz.Fill(); pz.strokeColor = LIMON; pz.lineWidth = 2.5f; pz.Stroke();
        }
        void GelRadarGuncelle()
        {
            if (gelRadar == null || gelRadar.panel == null) return;
            for (int i = 0; i < RN; i++) { gelRBas[i] = gelRGor[i]; gelRHedef[i] = Ayar.gelisim[Gelisim.GORUNEN[i]] / (float)Gelisim.TAVAN; }
            if (Ayar.reduce) { for (int i = 0; i < RN; i++) gelRGor[i] = gelRHedef[i]; gelRadar.MarkDirtyRepaint(); return; }
            gelRAniT = Time.unscaledTime;
            if (gelRAni != null) return;
            gelRAni = gelRadar.schedule.Execute(() =>
            {
                float k = Mathf.Clamp01((Time.unscaledTime - gelRAniT) / .28f), e = 1 + 2.2f * Mathf.Pow(k - 1, 3) + 1.2f * Mathf.Pow(k - 1, 2);   // ease-out-back
                for (int i = 0; i < RN; i++) gelRGor[i] = Mathf.LerpUnclamped(gelRBas[i], gelRHedef[i], e);
                gelRadar.MarkDirtyRepaint(); Kalite.Hareket();
                if (k >= 1) { gelRAni.Pause(); gelRAni = null; }
            }).Every(16);
        }
        static string GelUstMetin(int top, int bos) { return top == 0 ? L.T("İlk puanın Sv 2'de") : bos > 0 ? L.T("Dağıtılacak puan: {0} / {1}", bos, top) : L.T("{0} puanın hepsi dağıtıldı", top); }

        // Basılı tutunca tekrar (2026-11-15 inceleme, fikir 22:05 #2; iOS UIStepper kalıbı): Sv 30'da 29 puan 29 ayrı dokunuş,
        // her adımda alt sayfa baştan kuruluyor ve senkron kayıt yapılıyordu. Artık ilk dokunuş bir adım; 0,4 sn tutunca ~7/sn,
        // 1,5 sn'den sonra ~14/sn. Her adımda sayfa KURULMAZ: yalnız o dalın basamağı (BasamakDol/BasamakSon), sayısı, erişim
        // bilgisi, üstteki etiket ve düğmelerin pasif sınıfı yerinde güncellenir (yalnız değişince yazılır). Ses her adımda
        // değil 3 adımda bir + tavana/sıfıra gelince; kayıt Ayar.KaydetSonra ile parmak kalkınca tek kez. Hareketi azaltta
        // basamaklar anında (Basamak* zaten kısa devre), tekrar aynı.
        Label gelUst;
        readonly Label[] gelSay = new Label[Gelisim.DAL];
        readonly VisualElement[] gelCb = new VisualElement[Gelisim.DAL], gelArti = new VisualElement[Gelisim.DAL], gelEksi = new VisualElement[Gelisim.DAL];
        IVisualElementScheduledItem gelTekrar; int gelAdim; float gelBasT; Vector3 gelBasP; bool gelBekle; VisualElement gelSahip;

        // Bir adım; true = değer değişti.
        bool GelisimAdim(int dal, bool arti)
        {
            int once = Ayar.gelisim[dal];
            if (!(arti ? Ayar.GelisimVer(dal) : Ayar.GelisimGeriAl(dal))) return false;
            gelAdim++;
            int n = Ayar.gelisim[dal], sv = Ayar.gear.Seviye(), bos = Ayar.GelisimBos, top = Gelisim.Toplam(sv);
            bool sinir = arti ? (bos == 0 || n >= Gelisim.TAVAN) : n == 0;
            // 2026-11-26: verilen her puan bir yarım ton yukarı (dalın doluluğu kulaktan da duyulur), tavana gelince arpej + "win"
            // titreşimi; geri alma pes tık. Basılı tutarken yine 3 adımda bir.
            if (gelAdim == 1 || gelAdim % 3 == 0 || sinir)
            {
                if (arti && n >= Gelisim.TAVAN) { root.sfx.Yukselt(); Yerel.Hap("win"); }
                else { root.sfx.Tik(arti ? 660f * Mathf.Pow(2f, n / 12f) : 740f * Mathf.Pow(2f, -(Gelisim.TAVAN - n) / 24f)); Yerel.Hap("tick"); }
            }
            GelRadarGuncelle();
            var cb = gelCb[dal];
            if (cb != null)
            {
                int j = arti ? once : once - 1; var b = j >= 0 && j < cb.childCount ? cb[j] : null;
                if (b != null) { if (arti) { b.AddToClassList("gel-basamak--dolu"); BasamakDol(b); } else { b.RemoveFromClassList("gel-basamak--dolu"); BasamakSon(b); } }
                string yz = GelYuzde(dal, n);
                if (gelSay[dal] != null) { gelSay[dal].text = GelSayi(dal, n, bos > 0); if (arti) SayiPop(gelSay[dal]); }
                if (arti) KartParla(cb.parent?.parent);
                Erisim.Etiket(cb, L.T(Gelisim.AD[dal]) + ", " + n + "/" + Gelisim.TAVAN + (yz != "" ? ", " + yz : "") + GelSonrakiEt(dal, n, bos > 0) + ". " + L.T(Gelisim.NE[dal]), "metin");
            }
            // 2026-11-27 inceleme (fikir 12:06 #1, TAKILMA YOK): her puan beş sekmeyi kirletiyordu; Dolap ve Kariyer gelişim okumadığı
            // hâlde Gelişim'den sonra ilk dokunuşta vitrin/mekân baştan kuruluyordu. Yalnız gelişimi gösterenler: Oyna (GelisimSatir),
            // Profil (GelisimKart), Atölye (Sahada çubukları). Dolap ya da Kariyer gelişim gösterirse buraya eklenmeli.
            kirli[(int)Sekme.Oyna] = kirli[(int)Sekme.Profil] = kirli[(int)Sekme.Atolye] = true; ustKirli = true;   // altta duran sekme kapanışta kurulur
            if (arti && (gelAdim == 1 || gelAdim % 3 == 0 || sinir)) PortrePop(gelPortre, n >= Gelisim.TAVAN ? 1.14f : 1.08f);
            if (gelUst != null) gelUst.text = GelUstMetin(top, bos);
            for (int i = 0; i < Gelisim.DAL; i++)
            {
                GelPasif(gelArti[i], bos > 0 && Ayar.gelisim[i] < Gelisim.TAVAN); GelPasif(gelEksi[i], Ayar.gelisim[i] > 0);
                // son puan verilince ya da geri alınınca öbür dalların "→" önizlemesi de kalkar/gelir (yalnız değişince yazılır)
                if (gelSay[i] != null && gelArti[i] != null) { string t = GelSayi(i, Ayar.gelisim[i], bos > 0); if (gelSay[i].text != t) gelSay[i].text = t; }
            }
            return true;
        }
        // 2026-11-23 inceleme (fikir 12:06 #6): sonraki puanın etkisi ancak puan verilince görünüyordu, oyuncu dallar arasında
        // deneyerek seçiyordu. Puan varken ve tavanda değilken sayı satırının sonunda sıradaki puanın yüzdesi ("→ +%8"); puan
        // yokken ya da tavanda yazı yok (baskı ya da boş vaat yok). Metin yalnız değişince yazılır (GelisimAdim yolu).
        static string GelSayi(int dal, int n, bool puanVar)
        {
            string yz = GelYuzde(dal, n), s = n + "/" + Gelisim.TAVAN + (yz != "" ? " · " + yz : "");
            return puanVar && n < Gelisim.TAVAN ? s + "  → " + GelYuzde(dal, n + 1) : s;
        }
        static string GelSonrakiEt(int dal, int n, bool puanVar) { return puanVar && n < Gelisim.TAVAN ? ", " + L.T("bir puanla {0}", GelYuzde(dal, n + 1)) : ""; }
        static string GelYuzde(int dal, int n) { if (n == 0) return ""; return L.En ? "+" + Gelisim.Yuzde(dal, n) + "%" : "+%" + Gelisim.Yuzde(dal, n); }
        static void GelPasif(VisualElement d, bool etkin)
        {
            if (d == null || d.ClassListContains("gel-dugme--pasif") != etkin) return;   // yalnız değişince
            if (etkin) d.RemoveFromClassList("gel-dugme--pasif"); else d.AddToClassList("gel-dugme--pasif");
            var b = d.userData as ErisimBilgi; if (b != null) b.pasif = !etkin;
        }
        void GelTekrarDur() { gelBekle = false; if (gelTekrar != null) { gelTekrar.Pause(); gelTekrar = null; } }

        // 2026-11-11 (inceleme, fikir 02:05 #2): Sv 1'de (hiç puan yok) beş kartta on soluk − / + düğmesi ve beş kez "0/10 · +%0"
        // ilk açan oyuncuya pasif düğme duvarı gösteriyordu; o durumda düğmeler çizilmez (kart: ad + cümle + basamaklar), üstteki
        // "İlk puanın Sv 2'de" yeter. Yüzde yalnız n > 0 iken yazar. Puanı olan oyuncuda pasif düğme kalır (yerleşim oynamasın).
        VisualElement GelisimDal(int dal, int n, bool puanVar, bool dugmeli = true)
        {
            var k = new VisualElement(); k.AddToClassList("gel-dal"); gelKart[dal] = k;
            var ust = Satir("arali"); ust.pickingMode = PickingMode.Ignore; k.Add(ust);
            ust.Add(Yazi(L.T(Gelisim.AD[dal]), 16, "kalin"));
            string yz = GelYuzde(dal, n);
            var say = Yazi(GelSayi(dal, n, puanVar && dugmeli), 14, "rakam"); ust.Add(say); gelSay[dal] = say;
            var ne = Yazi(L.T(Gelisim.NE[dal]), 12, "soluk"); ne.style.whiteSpace = WhiteSpace.Normal; k.Add(ne);
            var alt = new VisualElement(); alt.AddToClassList("gel-alt"); k.Add(alt);
            int d = dal;
            gelEksi[dal] = gelArti[dal] = null;
            if (dugmeli) { gelEksi[dal] = GelisimDugme("-", n > 0, d, false, L.T("{0}: bir puan geri al", L.T(Gelisim.AD[dal]))); alt.Add(gelEksi[dal]); }
            var cb = new VisualElement(); cb.AddToClassList("gel-cubuk"); cb.pickingMode = PickingMode.Ignore; alt.Add(cb); gelCb[dal] = cb;
            for (int j = 0; j < Gelisim.TAVAN; j++)
            {
                var b = new VisualElement(); b.AddToClassList("gel-basamak"); if (j < n) b.AddToClassList("gel-basamak--dolu"); cb.Add(b);
            }
            Erisim.Etiket(cb, L.T(Gelisim.AD[dal]) + ", " + n + "/" + Gelisim.TAVAN + (yz != "" ? ", " + yz : "") + ". " + L.T(Gelisim.NE[dal]), "metin");
            bool ver = puanVar && n < Gelisim.TAVAN;
            if (dugmeli) { gelArti[dal] = GelisimDugme("+", ver, d, true, L.T("{0}: bir puan ver", L.T(Gelisim.AD[dal]))); alt.Add(gelArti[dal]); }
            return k;
        }

        // Puan verme anı (2026-11-11 inceleme, fikir 23:05 #4): ses ve tık vardı, göz bir şey görmüyordu; basamak tam dolu doğuyordu.
        // 2026-11-15'ten beri sayfa yeniden kurulmaz (GelisimAdim); yalnız o basamak soldan sağa 0,18 sn dolar.
        // Parıltı yok (sayfanın "süs yok" kuralı). Hareketi azaltta anında dolu; ses/tık aynı.
        // Geri alma (2026-11-14 inceleme, fikir 02:05 #6): − hareketsizdi, basamak bir anda boşalıyordu; hangi dalın azaldığı göz
        // ucuyla okunmuyordu. Boşalan basamak dolu doğar ve sağdan sola 0,12 sn söner; tık + ile simetrik ama üç yarım ton pes
        // (880 → 740 Hz). Hareketi azaltta anında boş.
        static void BasamakSon(VisualElement b)
        {
            if (Ayar.reduce) return;
            b.AddToClassList("gel-basamak--dolu");
            b.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50));
            b.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            b.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(.12f, TimeUnit.Second) };
            b.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseIn) };
            b.schedule.Execute(() => { b.style.scale = new Scale(new Vector3(0, 1, 1)); Kalite.Hareket(); }).StartingIn(16);
            b.schedule.Execute(() => { b.RemoveFromClassList("gel-basamak--dolu"); b.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0, TimeUnit.Second) }; b.style.scale = new Scale(Vector3.one); }).StartingIn(150);
        }
        // Puan anı: sayı .9 → 1.15 → 1 yaylanır, dal kartı limon tonla bir kez parlar (.3 sn söner). Hareketi azaltta yalnız renk.
        static void SayiPop(VisualElement l)
        {
            if (Ayar.reduce) return;
            l.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            l.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0, TimeUnit.Second) };
            l.style.transformOrigin = new TransformOrigin(Length.Percent(100), Length.Percent(50)); l.style.scale = new Scale(new Vector2(1.18f, 1.18f));
            l.schedule.Execute(() => { l.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(.22f, TimeUnit.Second) };
                l.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOutBack) }; l.style.scale = new Scale(Vector2.one); Kalite.Hareket(); }).StartingIn(16);
        }
        VisualElement gelPortre;
        // Portre tepkisi (2026-11-27): yalnız scale (yeniden yerleşim yok), SayiPop kalıbı; hareketi azaltta yok, ses/haptik kalır.
        static void PortrePop(VisualElement v, float k)
        {
            if (v == null || Ayar.reduce) return;
            v.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            v.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0, TimeUnit.Second) };
            v.style.scale = new Scale(new Vector2(k, k));
            v.schedule.Execute(() => { v.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(.18f, TimeUnit.Second) };
                v.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOutBack) }; v.style.scale = new Scale(Vector2.one); Kalite.Hareket(); }).StartingIn(16);
        }
        static void KartParla(VisualElement k)
        {
            if (k == null || !k.ClassListContains("gel-dal")) return;
            k.AddToClassList("gel-dal--parla"); k.schedule.Execute(() => k.RemoveFromClassList("gel-dal--parla")).StartingIn(90);
        }
        static void BasamakDol(VisualElement b)
        {
            if (Ayar.reduce) return;
            b.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50));
            b.style.scale = new Scale(new Vector3(0, 1, 1));
            b.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            b.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(.18f, TimeUnit.Second) };
            b.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            b.schedule.Execute(() => { b.style.scale = new Scale(Vector3.one); Kalite.Hareket(); }).StartingIn(16);
        }

        VisualElement GelisimDugme(string isaret, bool etkin, int dal, bool arti, string etiket)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("gel-dugme"); if (!etkin) d.AddToClassList("gel-dugme--pasif");
            // "-" ASCII: U+2212 Nunito'da yoksa boş kutu çıkar. Büyük yazıda glif 56 pt düğmeyi aşmasın diye ölçek 1,4'te durur.
            var y = Yazi(isaret, 22 * Mathf.Min(K, 1.4f) / Mathf.Max(1f, K), "kalin"); y.pickingMode = PickingMode.Ignore; d.Add(y);
            // Pasiflik basışta sınıftan okunur (adımlar sınıfı yerinde değiştirir).
            // 2026-11-20 (inceleme, fikir 17:06 #1; HIG / iOS UIStepper "gesture cancellation"): adım PointerDown'da verilip işaretçi
            // hemen düğmeye bağlanıyordu; sayfayı kaydırmak için başparmağını kenardaki ± üstüne koyan oyuncu istemeden puan veriyor
            // ve ScrollView sürüklemeyi hiç almıyordu. Artık basışta hiçbir şey olmaz: tek dokunuş adımı parmak kalkınca verilir;
            // 0,4 sn tutunca ilk adım + bugünkü tekrar başlar ve işaretçi ancak o an bağlanır. Parmak 8 birimden fazla kayarsa ya da
            // işaretçiyi başka öğe (ScrollView) alırsa dokunuş iptal olur, kaydırma sayfanındır. Basılma görseli (:active) aynı.
            d.RegisterCallback<PointerDownEvent>(e =>
            {
                if (d.ClassListContains("gel-dugme--pasif")) return;
                GelTekrarDur(); gelAdim = 0; gelBasT = Time.unscaledTime; gelBasP = e.position; gelBekle = true; gelSahip = d; int pid = e.pointerId;
                gelTekrar = d.schedule.Execute(() =>
                {
                    if (gelBekle)
                    {
                        var tutan = d.panel != null ? d.panel.GetCapturingElement(pid) : null;
                        if (tutan != null && tutan != d) { GelTekrarDur(); return; }   // kaydırma başladı
                        if (Time.unscaledTime - gelBasT < .4f) return;
                        gelBekle = false; d.CapturePointer(pid);
                        if (!GelisimAdim(dal, arti)) GelTekrarDur();
                        return;
                    }
                    float t = Time.unscaledTime - gelBasT;
                    if (t < 1.5f && (gelTekrarSay++ & 1) == 1) return;   // 70 ms tik: 1,5 sn'ye dek her ikinci (~7/sn), sonra her biri (~14/sn)
                    if (!GelisimAdim(dal, arti)) GelTekrarDur();   // tavan/sıfır: durur (son adımın sesi + hap GelisimAdim'de)
                }).Every(70);
            });
            d.RegisterCallback<PointerMoveEvent>(e => { if (gelBekle && gelSahip == d && (e.position - gelBasP).sqrMagnitude > 64f) GelTekrarDur(); });
            d.RegisterCallback<PointerUpEvent>(e =>
            {
                bool tek = gelBekle && gelSahip == d; if (gelSahip == d) GelTekrarDur();   // ikinci parmak başka düğmeyi tutuyorsa onu bozma
                if (d.HasPointerCapture(e.pointerId)) d.ReleasePointer(e.pointerId);
                if (tek && !d.ClassListContains("gel-dugme--pasif")) { gelAdim = 0; GelisimAdim(dal, arti); }
            });
            d.RegisterCallback<PointerLeaveEvent>(e => { if (gelBekle && gelSahip == d) GelTekrarDur(); });
            d.RegisterCallback<PointerCancelEvent>(e => GelTekrarDur());
            d.RegisterCallback<PointerCaptureOutEvent>(e => GelTekrarDur());
            d.RegisterCallback<DetachFromPanelEvent>(e => GelTekrarDur());
            Erisim.Etiket(d, etiket, "dugme", false, !etkin);
            return d;
        }
        int gelTekrarSay;

        // Oyna'nın altındaki satır, dağıtılmamış puan varken (SvHedefSatir yerine).
        VisualElement GelisimSatir()
        {
            int bos = Ayar.GelisimBos;
            var d = new VisualElement(); d.AddToClassList("sv-hedef"); d.AddToClassList("sv-hedef--gelisim");
            var sol = new VisualElement(); sol.AddToClassList("sv-hedef-sol"); sol.pickingMode = PickingMode.Ignore; d.Add(sol);
            // 2026-11-20 (inceleme, fikir 17:06 #5; §7.3 tek mesaj): puan, "Gelişim: oyuncunu güçlendir" ve düz "Dağıt" aynı şeyi üç
            // kez söylüyordu, "Dağıt" düğmeye benzemiyordu. Alt satır kalktı; sağ blok kardeş satırın (SvSagGelisim) "›" kalıbıyla.
            sol.Add(Yazi(L.T("Dağıtılacak puan: {0}", bos), 14, "kalin"));
            var sag = new VisualElement(); sag.AddToClassList("sv-hedef-sag"); sag.pickingMode = PickingMode.Ignore; d.Add(sag);
            sag.Add(Yazi(L.T("Dağıt") + " ›", 15, "kalin"));
            d.AddManipulator(new Clickable(() => { Tik(); GelisimAc(); }));
            Erisim.Etiket(d, L.T("Dağıtılacak puan: {0}", bos) + ", " + L.T("Gelişim: oyuncunu güçlendir"), "dugme");
            return d;
        }

        // Profil kartı (2026-11-11, inceleme fikir 02:05 #4): beş çubuk Gelişim sayfasında ve Atölye'nin "Takımın şimdi" kartında
        // (gelişim payı ucunda) zaten var; Profil'de üçüncü kez beş satır kupa rafını aşağı itiyordu. Tek satır toplam + durum,
        // dokununca sayfa açılır.
        void GelisimKart(VisualElement c)
        {
            var p = Ayar.gelisim; if (p == null) return; int bos = Ayar.GelisimBos, top = Gelisim.Toplam(Ayar.gear.Seviye());
            // 2026-11-26 inceleme (fikir 02:06 #3): puan yokken kart 0 puanlı sayfayı açan sahte düğmeydi ve "Sv 2'de" bilgisini sıradaki
            // açılış satırıyla ikinci kez veriyordu; ilk puan o satıra eklendi (ProfilEkran), kart yalnız puan varken.
            if (top == 0) return;
            c.Add(Grup(L.T("Gelişim")));
            var kart = Kart("gel-kart"); kart.AddToClassList("dugme"); c.Add(kart);
            var r = Satir("arali"); r.pickingMode = PickingMode.Ignore; kart.Add(r);
            string ust = L.T("{0}/{1} puan", top - bos, top);
            r.Add(Yazi(ust, 15, "kalin")); r.Add(Yazi("›", 18, "soluk"));
            // 2026-11-21 inceleme (fikir 17:06 #7): boş puan yokken "Dağılımı değiştir" kartın kendi işini söylüyordu; yerine oyuncunun
            // öne çıkan dalı (en yüksek, eşitlikte ilki). Hiç puan verilmemişse satır yok.
            int en = Gelisim.GORUNEN[0]; foreach (var i in Gelisim.GORUNEN) if (p[i] > p[en]) en = i;
            string alt = bos > 0 ? L.T("Dağıtılacak puan: {0}", bos) : p[en] > 0 ? L.T("Öne çıkan: {0} {1}", L.T(Gelisim.AD[en]), p[en]) : "";
            if (alt != "") { var ay = Yazi(alt, 13, bos > 0 ? "altin" : "soluk"); ay.style.marginTop = 4; ay.pickingMode = PickingMode.Ignore; kart.Add(ay); }
            kart.AddManipulator(new Clickable(() => { Tik(); GelisimAc(); }));
            Erisim.Etiket(kart, L.T("Gelişim") + ": " + ust + (alt != "" ? ", " + alt : ""), "dugme");
        }
    }
}
