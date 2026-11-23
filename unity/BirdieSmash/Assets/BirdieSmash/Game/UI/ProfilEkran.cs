// Profil sekmesi (2026-10-26, adım 1): oyuncu kartı (seviye, XP, Tüy, raket), istatistikler (eskiden ana menünün alt
// satırı: rekor ralli, galibiyet, görev; + kariyer basamağı) ve başarımlar (yerel liste, tuytop_basarim; Game Center
// bağlıysa "Game Center · Başarımlar"). Hepsi cihazda hesaplanır, hiçbir yere gönderilmez. Kazanılmamış başarım soluk +
// kilit ikonu (yalnız renkle değil; 2026-11-23'ten beri "Henüz değil" yazısı yerine sayılabilirse "14/20").
// 2026-10-26 adım 6 (karakter, tasarım §2.3, §5.5): oyuncu kartında ad (yalnız cihazda; Game Center takma adı okunmaz,
// izin ekranı ve ağ yok), unvan ve tarz; unvan seçimi (kazanılanlar çip, kazanılmayanlar kilit + nasıl alınır); maç ve
// smaç sayısı; "sıradaki açılış" (bir sonraki seviyede ne açılıyor: ara hedef görünür olsun, tasarım §5.7). Ad alanı
// isDelayed: yazarken değil, bitince kaydedilir (her tuşta kayıt ve yeniden kurulum yok).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        // Başarım kimlikleri App Store Connect'teki ile aynı (Basarim); adlar yalnız bu liste için.
        bool basarimTum;   // Profil başarım listesi açık (oturum boyunca; 2026-11-11)
        bool unvanTum;     // Profil kilitli unvan listesi açık (oturum boyunca; 2026-11-19)
        static readonly string[,] BASARIM = {
            { Basarim.ANTRENMAN, "Antrenmanı bitir" }, { Basarim.AKIS, "Akışa gir" }, { Basarim.RALLI20, "20 vuruşluk ralli" },
            { Basarim.SICRAMA, "Sıçrayarak smaç" }, { Basarim.GOREV3, "Bir maçta 3 görev" } };

        // Başarımın oyuncuya görünen adı (Profil listesi ve maç sonu kartı aynı adı kullanır; 2026-11-11).
        public static string BasarimAdi(string id)
        {
            for (int i = 0; i < BASARIM.GetLength(0); i++) if (BASARIM[i, 0] == id) return L.T(BASARIM[i, 1]);
            if (id.StartsWith("bs_kariyer_")) { int n; if (int.TryParse(id.Substring(11), out n) && n >= 1 && n <= Match.KARIYER.Length) return L.T("Kariyer") + ": " + L.T(Match.KARIYER[n - 1].yer); }
            if (id.StartsWith("bs_tur_")) { int n; if (int.TryParse(id.Substring(7), out n) && n >= 2) return L.T("{0} finalini kazan", L.T(TurAd(n - 1))); }
            if (id == Basarim.EFSANE) return L.T("Seviye 30'a ulaş");
            if (id == Basarim.ANTRENOR) return L.T("Antrenmanın 24 yıldızını topla");
            if (id == Basarim.GUNUN_MACI) return L.T("Bir Günün maçını kazan");
            if (id == Basarim.USTA6) return L.T("Usta zorlukta, Şampiyon yolunda 6 basamak geç");
            if (id == Basarim.IMZA_OKUR) return L.T("Toplam {0} imza vuruşu oku", Basarim.IMZA_OKUR_HEDEF);
            return id;
        }

        void ProfilKur(VisualElement k)
        {
            k.Clear(); var sv = Kaydirma(k); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            var t = Yazi(L.T("Profil"), 24, "sekme-baslik"); Erisim.Etiket(t, L.T("Profil"), "baslik"); c.Add(t);
            var gr = Ayar.gear; var p = Ayar.ilerleme; int L_, kalan, gerek; gr.OyuncuSv(out L_, out kalan, out gerek);
            var un = Karakter.Gosterilen(p, L_, p.AntrenmanYildiz); var tz = Karakter.Tarz(Ayar.Tarz);
            // 2026-11-23 Profil A (yasin seçimi "Profil A", taslak https://claude.ai/artifact/Q3WbQ4HGx1Pv6VhfwXpBpV; tarif
            // menu-ux-ui-2026-11-23/secim.md): sayfanın başı dört blok, her biri tek iş: (1) oyuncu kartı (yüz, ad, unvan, tarz ·
            // raket, dünya sırası, seviye çubuğu), (2) üç sayı, (3) kupalar 8 yuva, (4) son maçlar G/M tek satır. Eski düzende
            // (6 Kas fikir 5'ten beri) seviye küçük bir halkada saklıydı, üç sayı kartın içinde sıkışıktı, kupa rafı "+n" ile boş
            // görünüyor, son maçlar on tane aynı "✓ 11-7" hapıydı. Aşağıdaki listeler (gelişim, ad, unvan, mühür, istatistik,
            // başarım) yerinde kalır: seçim yalnız başı tarif ediyor, kaldırma yasin'e sorulmadan yapılmaz.
            // Kart altın dolgu + mürekkep kontur (§8 istisnası, dokümanda): oyuncu kartı bir ödül/koleksiyon kartı gibi okunsun;
            // sarı (--sun) değil altın (--gold), çünkü sarı "bas/seçili" demek. Kartın tamamı Dolap'ı açar (19 Kas: portreden
            // görünüm değiştirme), bu yüzden basılabilir kontur kuralı (§8.2.3) karta uyar. Seviye çubuğu altın üstünde limon
            // okunmaz (1,3:1); mürekkep (10:1).
            // Taslaktaki "Güç 87" yerine dünya sırası: oyuncunun tek sayılık gücü yok (Hikaye.Guc yalnız rakip merdiveni);
            // uydurma sayı yazılmaz. Dünya sırası Kariyer hikâyesindeki gerçek sayı (kazanılan basamaktan).
            var kart = Kart("profil-kart"); kart.AddToClassList("dokunulur"); if (K >= 1.35f) kart.AddToClassList("profil-kart--dikey"); c.Add(kart);
            kart.AddManipulator(new Clickable(() => { Tik(); SekmeAc(Sekme.Soyunma); }));
            var r = Satir("profil-ust"); r.pickingMode = PickingMode.Ignore; kart.Add(r);
            var av = new VisualElement(); av.AddToClassList("avatar"); av.AddToClassList("profil-yuz"); av.pickingMode = PickingMode.Ignore; av.style.backgroundColor = Kit.Hex(Ayar.FORMA); AvatarIc(av, Portre.Benim(), 48); r.Add(av);
            var ad = new VisualElement(); ad.AddToClassList("esnek-yatay"); ad.pickingMode = PickingMode.Ignore; r.Add(ad);
            ad.Add(Yazi(p.Ad ?? L.T("Oyuncu"), 22, "kalin")); var unY = Yazi(L.T(un.ad) + Karakter.KademeEki(un, p), 14, "kalin"); ad.Add(unY); SozcukSigdir(unY);   // 2026-11-07: ×2'de "Neighbourhoo/d" bölünüyordu
            // 2026-11-22 inceleme (fikir 02:06 #5): "Dengeli · Denge" kekeme okunuyordu, hangisinin tarz hangisinin raket olduğu söylenmiyordu.
            ad.Add(Yazi(L.T("{0} tarz · {1} raketi", L.T(tz.ad), L.T(Gear.GEAR["raket"][gr.raket].ad)), 13));
            int ds = Hikaye.OyuncuSira(Hikaye.Galibiyet(p));
            var dk = new VisualElement(); dk.AddToClassList("profil-sira"); dk.pickingMode = PickingMode.Ignore; r.Add(dk);
            dk.Add(Yazi(SiraYaz(ds), 26, "rakam")); dk.Add(Yazi(L.T("Dünya"), 12, "kalin"));
            int efN, efK; Ayar.gear.EfsaneIlerleme(out efN, out efK);
            // Seviye çubuğu tek yerde (22 Kas fikir 02:06 #4 üç yeri bire indirmişti; halka kalktı, çubuk karta geldi). En yüksek
            // seviyede aynı çubuk Efsane ilerlemesini gösterir.
            var cb = Cubuk(gerek > 0 ? kalan / (float)gerek : efK / (float)Gear.EFSANE_XP, CTA_MUREKKEP, 6); cb.AddToClassList("profil-cubuk"); kart.Add(cb);
            string svY = L.T("Seviye {0}", L_) + " · " + (gerek > 0 ? L.T("Sonraki seviyeye {0} XP", gerek - kalan) : efN > 0 ? L.T("Efsane {0}", efN) : L.T("En yüksek seviye"));
            kart.Add(Yazi(svY, 12, "kalin"));
            System.Action<UnvanBilgi> kartEtiket = u0 => Erisim.Etiket(kart, (p.Ad ?? L.T("Oyuncu")) + ", " + L.T(u0.ad) + Karakter.KademeEki(u0, p) + ", " + svY + ", " + L.T("Dünya sıralaman: {0}", SiraYaz(ds)) + ", " + L.T("Tarz") + " " + L.T(tz.ad) + ", " + L.T("{0} raketi", L.T(Gear.GEAR["raket"][gr.raket].ad)) + ". " + L.T("Görünümünü Dolap'ta değiştir"), "dugme");
            kartEtiket(un);
            // üç büyük sayı: kartın dışında, üç ayrı panel (taslak A). Büyük yazıda (K ≥ 1,35) alt alta.
            var uc = Satir("uc-sayi"); uc.AddToClassList("profil-uc"); if (K >= 1.35f) uc.AddToClassList("uc-sayi--dikey"); c.Add(uc);
            System.Action<string, string> Buyuk_ = (deger, et) => { var b = new VisualElement(); b.AddToClassList("uc-sayi-oge"); b.Add(Yazi(deger, 24, "rakam")); b.Add(Yazi(et, 12, "soluk")); Erisim.Etiket(b, et + " " + deger, "metin"); uc.Add(b); };
            // 2026-11-13 inceleme #5: ortadaki sayı en hızlı smaç rekoru (tuytop_smac_rekor; maç sonunda yazılıyordu ama yalnız o maçın
            // kartında anılıyordu). Badmintonun herkesin bildiği tek rakamı smaç hızı; raket/gelişim seçiminin somut sonucu burada
            // görünür. Rekor yoksa eski toplam smaç sayısı; toplam ızgaraya iner. Üç sayı kalır.
            int smacRekor = PlayerPrefs.GetInt("tuytop_smac_rekor", 0);
            Buyuk_(Ayar.bestRally.ToString(), L.T("Rekor ralli"));
            if (smacRekor > 0) Buyuk_(L.T("{0} km/s", smacRekor), L.T("En hızlı smaç")); else Buyuk_(p.Smac.ToString(), L.T("Smaç"));
            Buyuk_(Ayar.wins.ToString(), L.T("Galibiyet"));
            // sıradaki açılış (Sv 30'da yok): tek cümle; 23 Kas'tan beri kartın dışında, üç sayının altında (altın kart üstünde soluk yazı okunmaz)
            for (int s2 = L_ + 1; s2 <= 30; s2++)
            {
                // 2026-11-21 inceleme (fikir 07:06 #7): önce Acilanlar + AcilanlarTurlu her seviyede iki tam tarama yapıyordu; boşluk
                // kontrolü artık gösterilen listenin kendisi (yalnız Acilanlar'da olup listede görünmeyen seviye boş cümle basıyordu).
                var tl = Karakter.AcilanlarTurlu(s2); if (tl.Count == 0) continue;
                var parca = new List<string>(); var say = new Dictionary<string, int>();
                foreach (var a in tl) { int n; say.TryGetValue(a[1], out n); say[a[1]] = n + 1; }
                // 2026-11-06: aynı ad iki yuvada açılınca ("Kırmızı, Kırmızı") hangi eşya olduğu yazılır: "Kırmızı (Forma rengi)"
                foreach (var a in tl) parca.Add(say[a[1]] > 1 && a[0] != null ? L.T(a[1]) + " (" + L.T(a[0]) + ")" : L.T(a[1]));
                // 2026-11-23 inceleme (fikir 02:06 #6, §7.3): beş eşyalık liste "(Forma rengi)" ekleriyle iki satıra çıkıyordu; satırın işi
                // hedef işareti, envanter değil. İkiden fazlaysa sayı yazılır, tam liste erişim etiketinde kalır.
                string tam = L.T("{0}. seviyede açılır: {1}", s2, string.Join(", ", parca.ToArray()));
                var sa = Yazi(parca.Count > 2 ? L.T("{0}. seviyede {1} yeni eşya açılır", s2, parca.Count) : tam, 13, "soluk"); sa.style.marginTop = 10; sa.style.whiteSpace = WhiteSpace.Normal;
                // 2026-11-21 inceleme (fikir 07:06 #1): satır düz yazıydı; seviye atlama sayfasındaki "Dolap'ta dene" kalıbı burada da
                // (Brawl Stars / Clash Royale yolunda sıradaki ödüle dokunmak önizletir): Dolap eşyası varsa onu oyuncunun üstünde
                // dener, yalnız Atölye eşyası varsa o yuvayı açar, ikisi de yoksa düz yazı kalır. Tarama yalnız bulunan seviyede.
                string dn = null, yv = null; bool atl = false;
                foreach (var g in Karakter.AcilanAralikGruplu(s2 - 1, s2))
                {
                    if (dn == null && g[0] == "Dolap" && g.Length > 3 && g[3] != null) dn = g[3];
                    if (!atl && g[0] == "Atolye") { atl = true; yv = g[2]; }
                }
                if (dn != null || atl)
                {
                    sa.text += " ›"; sa.AddToClassList("dokunulur"); string dn_ = dn, yv_ = yv;
                    sa.AddManipulator(new Clickable(() => { Tik(); if (dn_ != null) { SekmeAc(Sekme.Soyunma); SeviyeDene(dn_); } else if (yv_ != null) YuvaAc(yv_); else SekmeAc(Sekme.Atolye); }));
                    Erisim.Etiket(sa, tam + " · " + L.T(dn != null ? "Dolap'ta dene" : "Atölye'de gör"), "dugme");
                }
                else Erisim.Etiket(sa, tam, "metin");
                c.Add(sa); break;
            }
            // Kupalar: her turun finali bir yuva, hepsi görünür (Profil A "kupalar 8 yuva"; 13 Kas fikir 07:05 #6'daki "kazanılanlar +
            // sıradaki + '+n'" sıkıştırmasının yerine: yasin hedefin bütününü görmek istedi). Kazanılan altın zeminli kupa, sıradaki
            // ince çerçeveli soluk kupa, kalanlar soluk (yalnız renk değil: zemin + çerçeve + erişim etiketi). Dokununca Kariyer o tur.
            // Yuva telefonda 4 × 2 (8 yan yana 39 pt'ye düşer, HIG 44); 8 × 48 pt sığan genişlikte (tablet) tek satır.
            // #4 (13 Kas): ilk kez görülen kupa yuvaya bir kez yukarıdan iner (.5 sn, tick + kısa ton); görülenler tuytop_kupa_gor
            // (global basamak biti; cihaz başına). Anahtar yokken (güncelleme sonrası ilk açılış) bugünkü kupalar sessizce görülmüş
            // sayılır: eski kupa "yeni" diye oynamaz. Hareketi azaltta iniş yok, yalnız ses + titreşim.
            var kupaK = Kart("profil-kupa"); c.Add(kupaK);
            int kz_ = 0, top_ = 0, sira_ = -1; var raf = Satir("profil-kupa-raf");
            raf.RegisterCallback<GeometryChangedEvent>(e => raf.EnableInClassList("profil-kupa-raf--tek", e.newRect.width >= 8 * 48));
            bool gorVar = PlayerPrefs.HasKey(KUPA_GOR); int gorMaske = PlayerPrefs.GetInt(KUPA_GOR, 0), gorYeni = gorMaske; bool yeniOldu = false;
            for (int tu = 0; tu < KariyerVeri.Turlar.Length; tu++)
            {
                if (!KariyerVeri.Turlar[tu].Var) continue; top_++; int g = KariyerVeri.Global(tu, KariyerVeri.BASAMAK - 1); bool var_ = p.Kazanildi(g); if (var_) kz_++;
                bool sira = !var_ && sira_ < 0; if (sira) sira_ = tu;
                var hk = new VisualElement(); hk.AddToClassList("profil-kupa-yuva"); hk.AddToClassList(var_ ? "profil-kupa-yuva--var" : sira ? "profil-kupa-yuva--sira" : "profil-kupa-yuva--yok");
                var ik = Ikon(Ikonlar.Kupa, 24, var_ ? ALTIN : SOLUK); ik.pickingMode = PickingMode.Ignore; hk.Add(ik); int tt = tu;
                hk.AddManipulator(new Clickable(() => { Tik(); KariyerTurSec(tt); SekmeAc(Sekme.Kariyer); }));
                Erisim.Etiket(hk, L.T(TurAd(tu)) + ", " + L.T(var_ ? "Kazanıldı" : "Kazanılmadı"), "dugme"); raf.Add(hk);
                int bit = 1 << (tu & 31);
                if (var_ && (gorMaske & bit) == 0) { gorYeni |= bit; if (gorVar) { yeniOldu = true; if (!Ayar.reduce) KupaIndir(hk); } }
            }
            if (gorYeni != gorMaske || !gorVar) PlayerPrefs.SetInt(KUPA_GOR, gorYeni);   // Save yok: bir sonraki Ayar.Kaydet yazar
            if (yeniOldu) { root.sfx.Tik(1320); Yerel.Hap("tick"); }
            var rb = Satir("arali"); kupaK.Add(rb); rb.Add(Yazi(L.T("Kupalar"), 14, "kalin")); var ksy = Yazi(kz_ + " / " + top_, 13, "soluk"); rb.Add(ksy);
            Erisim.Etiket(ksy, L.T("{0}/{1} kazanıldı", kz_, top_), "metin");
            kupaK.Add(raf);

            // Son maçlar (Profil A): tek satır G/M, en yeni solda, en çok 6. Skorlu haplar (14 Kas fikir 07:05 #1) bir bakışta "nasıl
            // gidiyorum"u değil on skoru okutuyordu; skor ve zorluk erişim etiketinde kalır. Harf + dolgu birlikte (yalnız renk değil:
            // G limon dolgu, M çerçeve). 19 Kas'tan beri oyuncu kartının hemen altında. Hiç maç yoksa satır yok.
            var sm = Ayar.SonMaclar();
            if (sm.Count > 0)
            {
                var sk = Kart("profil-son"); c.Add(sk);
                var sr = Satir("arali"); sr.AddToClassList("profil-son-satir"); sk.Add(sr); sr.Add(Yazi(L.T("Son maçlar"), 14, "kalin"));
                var gm = new VisualElement(); gm.AddToClassList("profil-gm"); gm.pickingMode = PickingMode.Ignore; sr.Add(gm);
                int gs = 0, n6 = Mathf.Min(6, sm.Count); var etk = new System.Text.StringBuilder();
                for (int i = 0; i < n6; i++)
                {
                    var m = sm[i]; if (m.g) gs++;
                    var h = Yazi(L.T(m.g ? "G" : "M"), 13, "kalin"); h.AddToClassList("profil-gm-harf"); h.AddToClassList(m.g ? "profil-gm-harf--g" : "profil-gm-harf--m"); gm.Add(h);
                    etk.Append(", ").Append(L.T(m.g ? "Kazanılan maç" : "Kaybedilen maç")).Append(" ").Append(m.ben).Append("–").Append(m.rakip).Append(" ").Append(L.T(Ayar.ZORLUK_AD[m.zorluk]));
                }
                Erisim.Etiket(sr, L.T("Son {0}: {1} galibiyet", n6, gs) + etk, "metin");
            }

            GelisimKart(c);   // 2026-11-10 oyuncu gelişimi: oyuncu kartının hemen altında (kartın anlattığı oyuncunun değerleri)

            // ad (yalnız cihazda)
            c.Add(Grup(L.T("Adın")));
            var tf = new TextField { value = p.Ad ?? "", maxLength = Ilerleme.AD_AZAMI, isDelayed = true }; tf.AddToClassList("ad-alan"); tf.style.fontSize = Mathf.Round(16 * K);
            tf.RegisterValueChangedCallback(e => { p.Ad = e.newValue; Ayar.KaydetSonra(); kirli[(int)Sekme.Profil] = true; var kk = sekmeKok[(int)Sekme.Profil]; kk.schedule.Execute(() => { if (gez.sekme == Sekme.Profil && gez.Ust == null) SekmeYerindeKur(Sekme.Profil); }).StartingIn(16); });
            Erisim.Etiket(tf, L.T("Adın") + ", " + L.T("cihazında ve kendi iCloud hesabında tutulur"), "metin_alani", false, false, p.Ad ?? ""); c.Add(tf);
            // Yer tutucu (2026-11-07, yasin "yap", arayüz cilası): boş alan ne yazılacağını söylesin. Unity 6 textEdition.placeholder
            // yerine kendi soluk etiketimiz (mono ile Unity'siz derleme 2021 başvurularıyla yapılıyor); alan boşken ve odak yokken görünür.
            var ph = Yazi(L.T("Adını yaz"), 16, "soluk"); ph.AddToClassList("ad-yer-tutucu"); ph.pickingMode = PickingMode.Ignore; tf.Add(ph);
            System.Action phG = () => ph.style.display = string.IsNullOrEmpty(tf.value) && tf.focusController?.focusedElement != tf ? DisplayStyle.Flex : DisplayStyle.None;
            tf.RegisterCallback<FocusInEvent>(e => ph.style.display = DisplayStyle.None); tf.RegisterCallback<FocusOutEvent>(e => phG()); tf.RegisterValueChangedCallback(e => phG()); phG();
            // 2026-11-07 (inceleme 16:05 #4): ad tuytop_ilerleme içinde BulutKayit ile oyuncunun kendi iCloud'una da gider;
            // "hiçbir yere gönderilmez" yanlıştı. Bize ya da başkasına gitmediği doğru, cümle bunu söyler.
            c.Add(Yazi(L.T("Adın cihazında ve kendi iCloud hesabında tutulur; bize ya da başkasına gönderilmez."), 12, "soluk"));

            // unvanlar
            c.Add(Grup(L.T("Unvan")));
            var kz = Karakter.Kazanilan(p, L_, p.AntrenmanYildiz); var us = Satir("cip-sarar"); c.Add(us);   // 2026-10-31 (yasin IMG_1517): 5 unvan eşit dar çipe sıkışıp harf harf bölünüyordu; çip yazısı kadar geniş, sığmayan alt satıra
            // 2026-11-20 inceleme (fikir 02:06 #4): çipe dokunmak bütün Profil'i (portre, kupa rafı, son maçlar, listeler) silip
            // kuruyor, kaydırma 33 ms sonra yerine oturuyordu. Değişen yalnız kartın unvan yazısı, çiplerin seçili hâli ve kademe
            // satırı: bunlar yerinde güncellenir (Atölye TarzYerinde kalıbı), sayfa kurulmaz. Unvan yazısı .15 sn opaklıkla gelir.
            var ks = Yazi("", 12, "soluk"); ks.style.whiteSpace = WhiteSpace.Normal;
            UnvanBilgi[] unSec = { un }; System.Action unvanKur = null;
            unvanKur = () =>
            {
                var su = unSec[0]; us.Clear();
                foreach (var u in kz)
                {
                    var uu = u;
                    us.Add(Cip(L.T(uu.ad) + Karakter.KademeEki(uu, p), uu.id == su.id, () =>
                    {
                        if (uu.id == unSec[0].id) return;
                        p.Unvan = uu.id; unSec[0] = uu; Ayar.KaydetSonra();
                        unY.text = L.T(uu.ad) + Karakter.KademeEki(uu, p); kartEtiket(uu);
                        if (!Ayar.reduce)
                        {
                            unY.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
                            unY.style.transitionDuration = new List<TimeValue> { new TimeValue(.15f, TimeUnit.Second) };
                            unY.style.opacity = 0f; unY.schedule.Execute(() => unY.style.opacity = 1f).StartingIn(16);
                        }
                        unvanKur(); Kalite.Hareket();
                    }));
                }
                // Kademe (2026-11-07): yalnız seçili beceri unvanının sıradaki eşiği, tek satır; yeni satır listesi yok.
                int se_ = Karakter.SonrakiEsik(su, p);
                if (se_ > 0) { ks.text = L.T("{0}: sonraki kademe {1}/{2}", L.T(su.ad) + Karakter.KademeEki(su, p), p.Sayac(su.sayac), se_); Erisim.Etiket(ks, ks.text, "metin"); ks.style.display = DisplayStyle.Flex; }
                else ks.style.display = DisplayStyle.None;
            };
            unvanKur(); c.Add(ks);
            // 2026-11-19 inceleme (fikir 17:07 #5): yeni oyuncuda 9–10 kilit satırı alt alta bir duvardı (Başarımlar 11 Kas'ta aynı
            // sorundan katlandı). Kapalıyken en yakın 2 kilitli unvan (sayaçlılar ilerleme oranına göre önce) + "Tümünü göster (n)";
            // açınca tam liste yerinde (sayfa kurulmaz, kaydırma yeri korunur). Liste oturum boyunca açık kalır.
            var kilitli = new System.Collections.Generic.List<UnvanBilgi>();
            foreach (var u in Karakter.Unvanlar) if (!kz.Contains(u)) kilitli.Add(u);
            System.Func<UnvanBilgi, float> oran = u => u.sayac != null && u.esik > 0 ? Mathf.Min(p.Sayac(u.sayac), u.esik) / (float)u.esik : -1f;
            var sirali = new System.Collections.Generic.List<UnvanBilgi>(kilitli);
            for (int a = 1; a < sirali.Count; a++) { var x = sirali[a]; int b = a - 1; while (b >= 0 && oran(sirali[b]) < oran(x)) { sirali[b + 1] = sirali[b]; b--; } sirali[b + 1] = x; }   // kararlı sıralama: eşitlerde veri sırası
            var ul = new VisualElement(); c.Add(ul);
            System.Action<UnvanBilgi> kilitSatir = u =>
            {
                var ur = Satir("basarim"); ur.AddToClassList("kilitli"); ul.Add(ur);
                ur.Add(Ikon(Ikonlar.Kilit, 18, SOLUK));
                // 2026-10-31 (SE ×2 turu: ad ve koşul yan yana sığmayıp "champion" harf harf bölünüyordu): ad üstte, koşul altında
                var us2 = new VisualElement(); us2.AddToClassList("esnek-yatay"); us2.pickingMode = PickingMode.Ignore; ur.Add(us2);
                // Beceri unvanı (2026-11-07): koşulun yanında ilerleme ("212/300"); oyuncu ne kadar kaldığını görür, yeni satır yok.
                string nasil = L.T(u.nasil) + (u.sayac != null ? " · " + Mathf.Min(p.Sayac(u.sayac), u.esik) + "/" + u.esik : "");
                us2.Add(Yazi(L.T(u.ad), 15)); us2.Add(Yazi(nasil, 12, "soluk"));
                Erisim.Etiket(ur, L.T("Unvan") + " " + L.T(u.ad) + ", " + L.T("Kilitli") + ", " + nasil, "metin");
            };
            if (unvanTum || kilitli.Count <= 3) foreach (var u in kilitli) kilitSatir(u);
            else
            {
                for (int a = 0; a < 2; a++) kilitSatir(sirali[a]);
                VisualElement ut = null;
                ut = Dugme(L.T("Tümünü göster ({0})", Karakter.Unvanlar.Length) + "  ›", () => { unvanTum = true; ul.Clear(); var yeni = new System.Collections.Generic.List<VisualElement>(); foreach (var u in kilitli) { kilitSatir(u); if (u != sirali[0] && u != sirali[1]) yeni.Add(ul[ul.childCount - 1]); } ut.RemoveFromHierarchy(); if (!Ayar.reduce) Kademeli(yeni, 0, 30, .18f, 8, 8); }, "kucuk-dugme");
                c.Add(ut);
            }

            // Tur mühürleri (2026-11-07, yasin karar kartı "Ekle"; GD5): her tur için mühür ya da sıradaki eşik. Fiyatsız prestij, satılmaz.
            // 2026-11-21 inceleme (fikir 17:06 #6, KPI hiyerarşisi): toplam kariyer yıldızı İstatistikler ızgarasından buraya,
            // başlığın sağına; tur yıldızları zaten bu kartta, ızgara başka yerde görünmeyen sayılara kalır.
            { var kys = YildizSayac(Ayar.ilerleme.ToplamYildiz, TurSayisiVeri() * 18, 13);
              Erisim.Etiket(kys, L.T("Kariyer yıldızı") + ": " + Ayar.ilerleme.ToplamYildiz + "/" + TurSayisiVeri() * 18, "metin");
              c.Add(GrupSatir(L.T("Tur mühürleri"), kys)); }
            var mk = Kart("muhur-kart"); c.Add(mk);
            for (int mt = 0; mt < KariyerVeri.Turlar.Length; mt++)
            {
                // 2026-11-08 inceleme (fikir 07:05 #2): kilitli tur "0/18" değil "Kilitli" der; tur yıldızları artık yalnız burada.
                if (!KariyerVeri.Turlar[mt].Var) continue; bool mac = p.TurAcik(mt); int mh = mac ? p.Muhur(mt) : 0, ys = p.TurYildiz(mt);
                // 2026-11-20 inceleme (fikir 07:06 #4): açık turun satırı Kariyer'de o turu açar (eksik yıldızı gören oyuncu ‹ › ile
                // aramaz; Profil'in kariyer kartıyla aynı yol). 44 pt, sonda "›"; kilitli tur dokunulmaz.
                var mr = Satir("arali"); mr.AddToClassList("muhur-satir"); mk.Add(mr);
                if (mac) { int tt = mt; mr.AddToClassList("muhur-satir--acik"); mr.AddToClassList("dokunulur"); mr.AddManipulator(new Clickable(() => { Tik(); KariyerTurSec(tt); SekmeAc(Sekme.Kariyer); })); }
                else mr.pickingMode = PickingMode.Ignore;
                var sol = Satir("rakip-satir"); sol.pickingMode = PickingMode.Ignore; mr.Add(sol);
                sol.Add(Ikon(mh >= 2 ? Ikonlar.YildizDolu : Ikonlar.YildizBos, 18, mh >= 2 ? ALTIN : mh == 1 ? BRONZ : SOLUK));
                var ay = Yazi(L.T(TurAd(mt)), 15); ay.style.marginLeft = 8; sol.Add(ay);
                string dr = !mac ? L.T("Kilitli") : mh >= 2 ? L.T("Altın") : mh == 1 ? L.T("Bronz") + " · " + ys + "/18" : ys + "/18";
                var sag = Satir(null); sag.pickingMode = PickingMode.Ignore; sag.style.alignItems = Align.Center; mr.Add(sag);
                var dy = Yazi(dr, 13, mh > 0 ? "durum-gecti" : "soluk"); dy.pickingMode = PickingMode.Ignore; sag.Add(dy);
                if (mac) { var ok = Yazi("›", 18, "soluk"); ok.pickingMode = PickingMode.Ignore; ok.style.marginLeft = 6; sag.Add(ok); }
                Erisim.Etiket(mr, L.T(TurAd(mt)) + ", " + (!mac ? L.T("Kilitli") : (mh >= 2 ? L.T("Altın tur mührü") : mh == 1 ? L.T("Bronz tur mührü") : L.T("{0}/{1} yıldız", ys, 18)) + ", " + L.T("Kariyer'de aç")), mac ? "dugme" : "metin");
            }
            // 2026-11-21 inceleme (fikir 02:06 #5): dipnot tek satır eşik; "Satılmaz…" cümlesi her açılışta tekrardı (mühür dükkânda yok).
            mk.Add(Yazi(L.T("Bronz {0} yıldız · Altın {1} yıldız", Ilerleme.MUHUR_BRONZ, Ilerleme.MUHUR_ALTIN), 12, "soluk"));

            c.Add(Grup(L.T("İstatistikler")));
            var iz = Satir("izgara"); c.Add(iz);
            // 2026-11-07 inceleme: Rekor ralli, Galibiyet, Smaç üstteki üç büyük sayıda (smaç rekoru varsa Smaç ızgarada, 2026-11-13); ızgarada tekrar edilmez (büyük yazıda üç satır kısa).
            Deger(iz, L.T("Maç"), p.Mac.ToString());
            if (smacRekor > 0) Deger(iz, L.T("Smaç"), p.Smac.ToString());   // 2026-11-13: üstte smaç hızı varken toplam buraya iner
            Deger(iz, L.T("Görev"), Ayar.gorev.ToString());
            // Tur başına yıldızlar ızgaradan kalktı (2026-11-08 inceleme, fikir 07:05 #2): "Tur mühürleri" kartı aynı bilgiyi
            // veriyordu ve kilitli turda iki yer farklı söylüyordu (0/18 ile Kilitli); büyük yazıda ızgara dört satır kısaldı.
            // 2026-10-26 adım 7–9
            Deger(iz, L.T("Antrenman yıldızı"), p.AntrenmanYildiz + "/" + Dril.ToplamYildiz);
            Deger(iz, L.T("Günün maçı"), p.GunlukSay.ToString());
            if (p.UstaAcik) Deger(iz, L.T(Ayar.YolAd), p.Usta.ToString());
            if (p.Mac == 0 && Ayar.wins > 0) c.Add(Yazi(L.T("Maç ve smaç sayısı bu sürümden sonra oynanan maçları sayar."), 12, "soluk"));

            c.Add(Grup(L.T("Başarımlar")));
            // Game Center listenin sonundan başlığın altına (2026-11-07 inceleme, fikir 20:09 #3): Ayarlar'daki kopya kalktı, tek yer
            // burası; uzun listenin dibinde aranmasın. Metin aynı anahtar ("Game Center'da aç" yeni çeviri isterdi).
            if (Basarim.acik) { c.Add(Dugme(L.T("Game Center · Başarımlar") + "  ›", () => { Basarim.Goster(); })); c.Add(Bosluk(6)); }
            // 2026-11-11 inceleme (fikir 19:05 #7): 23–25 satır alt alta, çoğu kilit + "Henüz değil"; sayfanın yarısı kilit duvarıydı.
            // Kapalıyken: kazanılanlar (gurur) + listedeki sıradaki 3 kilitli hedef + "Tümünü göster (n)". Açınca eski tam liste.
            var ids = new System.Collections.Generic.List<string>();
            for (int i = 0; i < BASARIM.GetLength(0); i++) ids.Add(BASARIM[i, 0]);
            for (int i = 1; i <= Match.KARIYER.Length; i++) ids.Add("bs_kariyer_" + i);
            for (int tu = 1; tu < KariyerVeri.Turlar.Length; tu++) if (KariyerVeri.Turlar[tu].Var) ids.Add("bs_tur_" + (tu + 1));
            ids.Add(Basarim.EFSANE); ids.Add(Basarim.ANTRENOR); ids.Add(Basarim.GUNUN_MACI); ids.Add(Basarim.USTA6); ids.Add(Basarim.IMZA_OKUR);
            int bKaz = 0; foreach (var id in ids) if (Basarim.Var(id)) bKaz++;
            var bs = Yazi(L.T("{0}/{1} kazanıldı", bKaz, ids.Count), 13, "soluk"); c.Add(bs);
            // 2026-11-23 inceleme (fikir 07:06 #3): kapalı listedeki "sıradaki 3 kilitli" veri sırasıyla değil, en yakın olan önce
            // (unvan kalıbı, kararlı sıralama: oranı olmayanlar veri sırasıyla sonra). Görünür üç hedef "en kolay sonraki adım" olur.
            // "Tümünü göster" açılınca tam liste eski sırada kalır (oyuncu yerini bilir).
            var yakin = new System.Collections.Generic.List<string>();
            foreach (var id in ids) if (!Basarim.Var(id)) yakin.Add(id);
            System.Func<string, float> bOran = id => { int n, e; return BasarimIlerleme(id, out n, out e) ? n / (float)e : -1f; };
            for (int a = 1; a < yakin.Count; a++) { var x = yakin[a]; float ox = bOran(x); int b = a - 1; while (b >= 0 && bOran(yakin[b]) < ox) { yakin[b + 1] = yakin[b]; b--; } yakin[b + 1] = x; }
            if (yakin.Count > 3) yakin.RemoveRange(3, yakin.Count - 3);
            var liste = new VisualElement(); c.Add(liste); int gizli = 0;
            foreach (var id in ids)
            {
                if (basarimTum || Basarim.Var(id) || yakin.Contains(id)) BasarimSatir(liste, id, BasarimAdi(id));
                else gizli++;
            }
            // 2026-11-19 inceleme (fikir 22:06 #5): "Tümünü göster" eskiden 15–25 satırı tek karede döküyordu; artık yalnız yeni
            // açılan satırlardan ilk 8'i kısa kademeli girer (eskiden görünenler yerinde durur), hareketi azaltta anında.
            if (gizli > 0)
            {   // yerinde açılır (sayfa yeniden kurulmaz, kaydırma yeri korunur)
                VisualElement td = null;
                td = Dugme(L.T("Tümünü göster ({0})", ids.Count) + "  ›", () => { Tik(); basarimTum = true; liste.Clear(); var yeni = new System.Collections.Generic.List<VisualElement>(); foreach (var id in ids) { bool gorunurdu = Basarim.Var(id) || yakin.Contains(id); BasarimSatir(liste, id, BasarimAdi(id)); if (!gorunurdu) yeni.Add(liste[liste.childCount - 1]); } td.RemoveFromHierarchy(); if (!Ayar.reduce) Kademeli(yeni, 0, 30, .18f, 8, 8); }, "kucuk-dugme");
                c.Add(td);
            }
            c.Add(Bosluk(20));
        }

        void Deger(VisualElement iz, string ad, string deger)
        {
            var k = Kart("deger-kart"); k.Add(Yazi(deger, 26, "deger")); k.Add(Yazi(ad, 13, "soluk")); Erisim.Etiket(k, ad + ", " + deger, "metin"); iz.Add(k);
        }

        void BasarimSatir(VisualElement c, string id, string ad)
        {
            bool var_ = Basarim.Var(id); var r = Satir("basarim"); if (!var_) r.AddToClassList("kilitli"); c.Add(r);
            // 2026-11-21 inceleme (#2): kazanılan altın (§8.1 ödül rengi, kupa rafı ve mühürle aynı); turuncu birincil eylemin.
            r.Add(Ikon(var_ ? Ikonlar.Tik : Ikonlar.Kilit, 20, var_ ? ALTIN : SOLUK));
            // 2026-11-23 inceleme (fikir 07:06 #1, #2): satır sonundaki 12 pt "Kazanıldı/Henüz değil" kalktı (Kariyer yıldızları 21 Kas
            // kalıbı; durum ikon şekliyle + kilitli soluklaşmayla okunuyor). Yerine kilitli ve sayılabilir başarımda "14/20": "ne kadar
            // kaldı" bir maç daha oynatır. Sayılamayanda sağ sütun boş. Erişim etiketi durumu söylemeye devam eder.
            r.Add(Yazi(ad, 15, "esnek-yatay")); string d = L.T(var_ ? "Kazanıldı" : "Henüz değil");
            int n, e;
            if (!var_ && BasarimIlerleme(id, out n, out e)) { string ie = n + "/" + e; r.Add(Yazi(ie, 13, "soluk")); d = L.T("Kilitli") + ", " + ie; }
            Erisim.Etiket(r, ad + ", " + d, "metin");
        }

        // Kilitli başarımın sayılabilir ilerlemesi (2026-11-23, fikir 07:06 #1). Yalnız verisi başarımın koşuluyla birebir olanlar:
        // USTA6 sayılmaz (p.Usta zorluk ayırmaz; "6/6 Kilitli" yanıltırdı). Eşiğe ulaşmış ama verilmemiş (eski kayıt) satırda sayı
        // yazmak çelişki olurdu: o zaman false.
        static bool BasarimIlerleme(string id, out int n, out int e)
        {
            n = 0; e = 0;
            if (id == Basarim.RALLI20) { n = Ayar.bestRally; e = 20; }
            else if (id == Basarim.EFSANE) { int sv, k, g; Ayar.gear.OyuncuSv(out sv, out k, out g); n = sv; e = 30; }
            else if (id == Basarim.ANTRENOR) { n = Ayar.ilerleme.AntrenmanYildiz; e = Basarim.ANTRENOR_YILDIZ; }
            else return false;
            if (n < 0) n = 0;
            return n < e;
        }
        const string KUPA_GOR = "tuytop_kupa_gor";
        // Yeni kupa inişi (2026-11-13 #4): yukarıdan 22 px, saydamdan; .5 sn geri yaylanan eğri. Satır içi stil sonra temizlenir
        // ki :active küçülmesi ve soluk sınıfı bozulmasın (AnaEkran.KademeliAc kalıbı).
        void KupaIndir(VisualElement v)
        {
            v.style.opacity = 0f; v.style.translate = new Translate(0, -22, 0);
            v.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity"), new StylePropertyName("translate") };
            v.style.transitionDuration = new List<TimeValue> { new TimeValue(.3f, TimeUnit.Second), new TimeValue(.5f, TimeUnit.Second) };
            v.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut), new EasingFunction(EasingMode.EaseOutBack) };
            v.schedule.Execute(() => { v.style.opacity = 1f; v.style.translate = new Translate(0, 0, 0); Kalite.Hareket(); }).StartingIn(180);
            v.schedule.Execute(() => { v.style.opacity = StyleKeyword.Null; v.style.translate = StyleKeyword.Null; v.style.transitionProperty = StyleKeyword.Null; v.style.transitionDuration = StyleKeyword.Null; v.style.transitionTimingFunction = StyleKeyword.Null; }).StartingIn(760);
        }
    }
}
