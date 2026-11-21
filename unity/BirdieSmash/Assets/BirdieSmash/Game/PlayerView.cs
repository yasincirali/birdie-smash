// Oyuncu: Blender figürünün FBX'i (tool/karakter/unity_fbx.py → Resources/karakter/<model>.fbx). Ayarlar › Oyuncu görünümü:
// "oyuncak" (Oyuncak 3B, varsayılan; 2026-10-26'dan beri MakeHuman CC0 tabanlı figür, 2026-10-27'den beri ~3,4 baş boylu tıknaz
// oyuncak oranı ve dört silüeti ayrı tip; eskiden KayKit Rogue CC0),
// "rocketbox" (Gerçekçi, Microsoft Rocketbox Sports_Male_04, MIT), 2026-10-27'den beri "sade" (Sade, Mesh2Motion CC0) ve
// 2026-10-29'dan beri "tuycuk" (Tüycük: Oyuncak atlet iskeleti ve klipleri, kendi yumurta gövdeli ağı; tool/karakter/tuycuk.py;
// gövde __forma takım rengi, __karin onun açığı, ayakkabılar Oyuncak'ın ayk_* parçaları). Hepsi iskeletli; 26 klip Legacy Animation olarak normalize zamanla
// örneklenir: klip ve faz Match.PickClip'ten gelir (HTML player() ile aynı karar), vuruş anı topla kare kare eşleşir.
// Faz → klip zamanı: sprite kare k'yı (k+.5)/n fazında gösterir; FBX'te pozlar eşit aralıklı olduğu için aynı faz aynı
// pozu verir (döngülü kliplerde kapanış pozu ek). Harmanlama süresi geçişin türüne göre (Match.GecisSure, 2026-11-05: temas
// 0,06, hazırlık 0,12, ayak işi 0,15, vuruştan dönüş 0,22 s; eskiden 0,08 ve dönüşte 0,2 s) (Ar-Ge 23 Eki animasyon B1: takip pozundan hazır duruşa "ışınlanma" yok; sprite'ta harmanlama
// hayalet yapıyordu, iskelette yapmaz, bu yüzden Blender'da yeniden render gerekmedi). FBX yoksa basit kapsül figür.
// Renk: parça adının "__" sonrası etiketi (HTML sprRenk/tint maskesi karşılığı): forma = takım rengi, cerceve = raket
// modelinin rengi, bant/bilek = seçili kafa bandı/bileklik (ekipman "Yok" ise gizli), grip/tel/tel2 = raket görünümü
// (tuytop_raket_gor; tel2 yalnız "ana ve çapraz ayrı" desende farklı). Rakipte raket #e9eef0, bant/bilek yok.
// Diğer parçalar FBX'in kendi malzemesini korur (Rocketbox dokusu, KayKit renkleri).
// 2026-10-26 karakter görünümü (adım 6): ten (__ten), saç (__sac) ve ayakkabı (__taban) yalnız oyuncu seçtiyse boyanır; null
// = modelin kendi rengi (var olan oyuncu ve rakipler değişmez). Dokulu parça (Gerçekçi model teni: DokuMalzeme) boyanmaz:
// dokunun üstüne düz renk yüzü siler; ten tonu orada bilinçli olarak yok (Soyunma odası bunu yazar). Etiketi olmayan modelde
// (yeni karakter hattında etiket değişirse) hiçbir şey boyanmaz, çökme yok. Grip "cizgi" dokusu (Havlu) koddan çizilir.
// 2026-10-26 karakter tasarımı (tip, saç biçimi, ayakkabı modeli; Core/Kozmetik, varlık sözleşmesi
// /mnt/project-files/birdie-smash/komple-oyun/karakter_varlik_sozlesmesi.md): yalnız "Karakter varyantları" bölümündeki
// yöntemler (ModelYolu, Varyant, AyakkabiBoya, ShowPoz) ve Gorunum alanları; hareket/ayak kodu bu bölüme girmez.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class PlayerView : MonoBehaviour
    {
        public bool isMe; Transform model, yaw; Animation anim; string cur, prev, prev2; float blend = 1, blendS = .08f, prevPh, lastPh, prev2Ph, prev2Pay;
        Transform fbRacket, shadow; LineRenderer ringBg, ringFg; float ringA;
        const float SCALE = 1.75f;   // sprite sürümündeki 1,75 kat okunurluk büyütmesi
        static readonly Dictionary<string, bool> LOOPS = new Dictionary<string, bool>();
        public bool hakem;

        // adim (2026-10-26): ayak işi klibinin bir döngüde taşıdığı yol, modelin kendi ölçeğinde metre (basan ayaktan ölçülür).
        // Varsa Match.ADIM_BOY'u ezer (oyun ölçeği = adim × SCALE); yoksa 0 gelir ve tahmin değerler kalır.
        [System.Serializable] class ClipMeta { public string name; public int poses; public bool loop; public float adim; }
        // 2026-10-26 karakter varlık sözleşmesi §5: tip (varsayılan saç/ayakkabı, saç rengi) ve saç başına bant uyumu.
        [System.Serializable] class TipMeta { public string id, sac, ayakkabi, sacRenk; public float boy; }
        [System.Serializable] class SacMeta { public string id; public bool bant = true; }
        // bant: kafa bandı halkası, bas__isaret'e göre model biriminde (dz yukarı, df ileri, r yarıçap; unity_fbx.py Blender'da ölçer).
        // yan/ust/on/arka: baş elipsoidi, r'ye oran (bant yüksekliğinde yan, ön, arka; bant merkezinden tepe). 0 = küre (1).
        // Oyuncak başı yumurta: küre çerçevede (r66) gözlük/boya/bıyık başın içinde kalıyordu. Yalnız kafatası (ön 1,25r, arka
        // 1,03r, yan ,83r, tepe ,74r; r67) şapkayı saçın içine gömdü: şapka saçın üstüne oturur, oranlar baş + kısa saçtan
        // (yan ,92r, tepe 1,11r, arka 1,13r) küçük payla; ön yüzden (saçsız). Gerçekçi/Sade/Tüycük küreyle oturuyor, alan yok.
        [System.Serializable] public class BantMeta { public float dz, df, r, yan, ust, on, arka; }
        [System.Serializable] class Meta { public ClipMeta[] clips; public TipMeta tip; public SacMeta[] sac; public BantMeta bant; public string klipKaynak; }

        // Görünüm: null alan = FBX'in kendi rengi (cerceve/grip/tel) ya da gizli (bant/bilek).
        public class Gorunum
        {
            public Color forma; public Color? cerceve, bant, bilek, grip, tel, tel2, ten, sac, ayakkabi, sort; public bool gripCizgi;
            // 2026-10-27 Gerçekçi aksesuarları: sort = şort rengi (null = modelin varsayılanı), kumas = forma kumaşı (Gear "kumas";
            // dokulu modelde formanın deseni ve parlaklığı, null = klasik).
            public string kumas;
            // 2026-11-20 (Core/Rakipler): figürün modeli; null = oyuncunun modeli (Ayar.model). Üretilmiş rakip kendi modelini
            // (kedi, robot ...) taşır, oyuncu hangi modeli seçmiş olursa olsun.
            public string model;
            // 2026-10-26: tip (Kozmetik.TIP), saç biçimi (Kozmetik.SAC) ve ayakkabı modeli (Gear "ayakkabi") kimlikleri; null =
            // varsayılan (listenin ilki: hakemler ve eski çağrılar bugünkü görünüşle kalır).
            public string tip, sacModel, aykModel;
            // 2026-10-30 Soyunma › Görünüm: şapka ve yüz aksesuarı kimlikleri (Kozmetik.SAPKA / YUZ; null ya da "yok" = takılı değil).
            public string sapka, yuz;
            // 2026-11-01 Dolap › Sırt: pelerin/omuzluk (Kozmetik.SIRT; hazır KayKit ağı, Aksesuar.Sirt).
            public string sirt;
            // 2026-11-12 Dolap › Kol: raketsiz ön kola kalkan (Kozmetik.KOL; Aksesuar.Kol).
            public string kol;
            // 2026-11-12 Dolap › Bel: leğene bağlı etek, kemer, kuyruk (Kozmetik.BEL; Aksesuar.Bel).
            public string bel;
            // 2026-11-14 Dolap › Ayakkabı görünümü: ekipman ayakkabısının yerine giyilen görünüş (Kozmetik.AYAK; Aksesuar.Ayak).
            public string ayak;
            public bool sacParla;   // 2026-11-01 mistik saç boyası: saç malzemesi hafifçe kendinden ışır
            public bool isin;       // 2026-11-01 ışın kaplaması: çerçeve ve kordaj kendinden ışır, hale + renkli salınım izi (IsinKur)
            // 2026-10-30 Dolap › Raket: kafa şekli (RaketSekil; null/"oval" = FBX'in kendi kafası) ve şaft rengi (null = FBX rengi).
            public string kafa; public Color? saft;
            // Sahnedeki figürü yeniden kurmak gerekip gerekmediğinin özeti (GameRoot, Vitrin).
            public Gorunum Kopya() { return (Gorunum)MemberwiseClone(); }
            public string Anahtar() { return "" + forma + cerceve + bant + bilek + grip + tel + tel2 + ten + sac + ayakkabi + sort + gripCizgi + "|" + tip + "|" + sacModel + "|" + aykModel + "|" + kumas + "|" + sapka + "|" + yuz + "|" + sirt + "|" + kol + "|" + bel + "|" + ayak + "|" + kafa + saft + (sacParla ? "|p" : "") + (isin ? "|i" : "") + (model != null ? "|m" + model : ""); }
        }
        static Color? Renk(string h) { return h != null ? Kit.Hex(h) : (Color?)null; }
        public static Gorunum Benim()
        {
            var gs = Ayar.GS; var r = Ayar.RGOR; string kp = Ayar.GorunumRenk("kaplama");
            return new Gorunum { forma = Kit.Hex(gs.forma), cerceve = Kit.Hex(kp ?? gs.raketRenk), bant = gs.bant != null ? Kit.Hex(gs.bant) : (Color?)null, bilek = gs.bileklik != null ? Kit.Hex(gs.bileklik) : (Color?)null,
                grip = Kit.Hex(Vitrin.RAK_RENK[r["grip"]]), tel = Kit.Hex(Vitrin.RAK_RENK[r["tel"]]), tel2 = Kit.Hex(Vitrin.RAK_RENK[r["desen"] == "iki" ? r["tel2"] : r["tel"]]),
                ten = Renk(Ayar.GorunumRenk("ten")), sac = Renk(Ayar.GorunumRenk("sac")), ayakkabi = Renk(Ayar.GorunumRenk("ayakkabi")), gripCizgi = gs.gripDoku == "cizgi",
                tip = Ayar.KOZ[Kozmetik.TIP], sacModel = Ayar.KOZ[Kozmetik.SAC], aykModel = Ayar.AykGorunum, sapka = Ayar.KOZ[Kozmetik.SAPKA], yuz = Ayar.KOZ[Kozmetik.YUZ], sirt = Ayar.KOZ[Kozmetik.SIRT], kol = Ayar.KOZ[Kozmetik.KOL], bel = Ayar.KOZ[Kozmetik.BEL], ayak = Ayar.KOZ[Kozmetik.AYAK], sacParla = Ayar.SacParla, isin = Ayar.KaplamaIsin,
                sort = Renk(Ayar.GorunumRenk("sort")), kumas = Ayar.gear != null ? Ayar.gear.kumas : null,
                kafa = r["kafa"], saft = r["saft"] != "model" && Vitrin.RAK_RENK.ContainsKey(r["saft"]) ? Kit.Hex(Vitrin.RAK_RENK[r["saft"]]) : (Color?)null };
        }
        // Rakip (2026-10-26): tip, saç biçimi/rengi, ten ve ayakkabı stil.json'daki gorunum'dan (rakip kimliği: Blaze hep dikenli
        // kızıl saçlı). Forma ve raket bugünkü gibi; veri yoksa bugünkü görünüş.
        // Tur rakip başına sayılır (2026-11-15 akşam, yasin "ana menüde takılma"): ilk sürümde tur = toplam maç sayısıydı, her maçtan
        // sonra BÜTÜN rakiplerin portre anahtarı değişiyor, Kariyer açılınca yedi portre ana iş parçacığında art arda yeniden
        // çiziliyordu (her biri bir FBX figürü). Şimdi yalnız oynadığın rakip bir sonraki görünüşüne geçer: maç başına en çok bir
        // yeni portre. Kayıt PlayerPrefs'te (ayrı Save yok; arka plana geçişteki toplu kayıtla yazılır), kaybolursa tur 0'dan başlar.
        public static int RakipTur(string stilId) { return PlayerPrefs.GetInt("rk_tur_" + stilId, 0); }
        public static void RakipTurIlerle(string stilId) { if (stilId != null) PlayerPrefs.SetInt("rk_tur_" + stilId, RakipTur(stilId) + 1); }
        // Figürü kurulacak model: görünüşün kendi modeli (dosyası build'deyse), yoksa oyuncunun modeli.
        public static string ModelAd(Gorunum g) { return g != null && g.model != null && Ayar.ModelVar(g.model) ? g.model : Ayar.model; }
        public static Gorunum Rakip(Style st, Color forma)
        {
            var g = new Gorunum { forma = forma, cerceve = Kit.Hex("#e9eef0") };
            if (st != null && st.model != null && Ayar.ModelVar(st.model)) g.model = st.model;   // 2026-11-20 üretilmiş rakibin modeli
            var r = st != null && st.gor != null ? st.gor : Kozmetik.RakipTur(st != null ? st.id : null, st != null ? RakipTur(st.id) : 0); if (r == null) return g;   // 2026-11-15: tip ve şapka maçtan maça değişir (Kozmetik.RakipTur)
            g.tip = r.tip; g.sacModel = r.sac; g.aykModel = r.ayakkabi; g.sapka = r.sapka;
            var o = r.sacRenk != null ? Karakter.Oge("sac", r.sacRenk) : null; if (o != null) g.sac = Renk(o.renk);
            o = r.ten != null ? Karakter.Oge("ten", r.ten) : null; if (o != null) g.ten = Renk(o.renk);
            o = r.aykRenk != null ? Karakter.Oge("ayakkabi", r.aykRenk) : null; if (o != null) g.ayakkabi = Renk(o.renk);
            return g;
        }
        // Uzak raketin kordajı (2026-10-30, yasin: "rakibin bazen raketi içi beyaz dolgu şeklinde gözüküyor"): maç kamerasından
        // rakip ~10 m ötede; 22 + 21 tel arası (~6 mm) ekranda bir pikselden dar, çoklu örnekleme telleri tek bir beyaz yüzeye
        // ortalıyor, raket dolu disk gibi görünüyordu (gölgesi de dolu). Gerçek yayında uzak kordaj ince bir pus: teller saydam
        // (alfa .3, ışıksız) ve gölge düşürmez. Yalnız maçtaki rakipte; yakın plan (vitrin, afiş) ve senin raketin aynı kalır.
        // 2026-11-14 (yasin: "maç içinde rakip raketinin içi beyaz olabiliyor, bunu çözmüştük ama arada karşılaştım"): sabit .3
        // alfa yetmiyordu. Kalite basamağı ralli içinde çözünürlüğü (Screen.SetResolution) ve MSAA'yı düşürüyor (Kalite.cs); düşük
        // basamakta tel aralığı ekranda bir pikselin altına iniyor, 43 tel üst üste binip .3 alfa birikerek yine dolu açık bir
        // disk veriyordu. "Arada" olması bu yüzden: yalnız kalite düştüğünde, rakip fileden uzakken. Şimdi alfa her karede tel
        // aralığının ekrandaki piksel boyuna göre: aralık ≤ 1,5 px'te .08 (ince pus), ≥ 4 px'te .45; renk de beyaz değil, gri-mavi.
        readonly List<Material> seyrekM = new List<Material>(); Renderer seyrekKafa; float seyrekA = -1;
        const int TEL_SAYI = 22; const float SEYREK_MIN = .08f, SEYREK_MAX = .45f;
        static readonly Color SEYREK_RENK = new Color(.80f, .84f, .88f);
        // Tek paylaşılan malzeme (2026-11-14 inceleme, fikir 22:05 #7): her tel parçası Kit.Clear ile kendi malzemesini alıyordu;
        // Kit.Topla dışında kaldıkları için figür yok edilince sahipsiz kalıyor, her maç yeni set birikiyordu. Seyrek kordaj yalnız
        // maçtaki tek rakipte, alfa da tek değer: tüm teller aynı malzemeyi paylaşır (tek yazım, aynı çizim grubu). Paylasilan'da,
        // figür yıkımı onu silmez.
        static Material seyrekPay;
        public void KordajSeyrek()
        {
            seyrekM.Clear(); seyrekKafa = null; seyrekA = -1; seyrekEksen = false;
            if (seyrekPay == null) { var t = Kit.Topla; Kit.Topla = null; seyrekPay = Kit.Clear(new Color(SEYREK_RENK.r, SEYREK_RENK.g, SEYREK_RENK.b, SEYREK_MIN)); Kit.Topla = t; Kit.Paylas(seyrekPay); }
            seyrekM.Add(seyrekPay);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string tag = Etiket(r.gameObject.name); if (tag != "tel" && tag != "tel2") continue;
                var m = seyrekPay;
                r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (tag == "tel" && seyrekKafa == null) seyrekKafa = r;
            }
        }
        // Karede bir: tel aralığının ekrandaki boyu (px) → alfa. Değişim küçükse malzemeye yazılmaz.
        // 2026-11-14 gece (yasin: "hala arasıra rakibin raketi içerisi beyaz oluyor", iki maç ekranı): aralık dünya AABB'sinin
        // kamera sağına izdüşümünden ölçülüyordu; raket yandan ya da eğik görününce (servis karşılama duruşu, raket dik) AABB
        // geniş kalıyor, aralık 4 px sanılıp alfa .45'e çıkıyordu, oysa 22 tel ekranda birkaç piksele üst üste biniyor ve .45
        // alfa birikerek dolu açık bir şerit/disk veriyordu. Şimdi aralık telin kendi ağından: yerel sınır kutusunun orta ekseni
        // (teller bu eksen boyunca dizili) ve uzun ekseni (enine teller) ayrı ayrı ekrana izdüşürülür, küçüğü alınır; yan bakışta
        // aralık sıfıra iner. Alfa da üst üste binmeyi hesaba katar: k = 1/aralık tel aynı piksele düşerse birikmiş örtü
        // 1-(1-a)^k; a = 1 - (1-SEYREK_ORTU)^aralık bunu SEYREK_ORTU'da tutar (aralık 1 px → .25, .3 px → .08), en çok .45.
        Vector3 seyrekMerkez, seyrekS, seyrekL; bool seyrekEksen;
        const float SEYREK_ORTU = .25f, SEYREK_ALT = .02f;
        void SeyrekEksen()
        {
            seyrekEksen = false; var mf = seyrekKafa != null ? seyrekKafa.GetComponent<MeshFilter>() : null; if (mf == null || mf.sharedMesh == null) return;
            var b = mf.sharedMesh.bounds; Vector3 sz = b.size; int[] ax = { 0, 1, 2 }; System.Array.Sort(ax, (p, q) => sz[q].CompareTo(sz[p]));
            seyrekMerkez = b.center; seyrekL = Vector3.zero; seyrekS = Vector3.zero;
            seyrekL[ax[0]] = b.extents[ax[0]]; seyrekS[ax[1]] = b.extents[ax[1]]; seyrekEksen = true;
        }
        static float EkranBoy(Camera cam, Transform t, Vector3 m, Vector3 e)
        {
            Vector3 p0 = cam.WorldToScreenPoint(t.TransformPoint(m - e)), p1 = cam.WorldToScreenPoint(t.TransformPoint(m + e));
            if (p0.z <= 0 || p1.z <= 0) return -1; return new Vector2(p1.x - p0.x, p1.y - p0.y).magnitude;
        }
        void KordajGuncelle()
        {
            if (seyrekKafa == null || seyrekM.Count == 0) return; var cam = Camera.main; if (cam == null) return;
            if (!seyrekEksen) { SeyrekEksen(); if (!seyrekEksen) return; }
            var t = seyrekKafa.transform; float s = EkranBoy(cam, t, seyrekMerkez, seyrekS), l = EkranBoy(cam, t, seyrekMerkez, seyrekL);
            if (s < 0 || l < 0) return;
            float aralik = Mathf.Min(s / TEL_SAYI, l / (TEL_SAYI - 1));
            float a = Mathf.Clamp(1 - Mathf.Pow(1 - SEYREK_ORTU, aralik), SEYREK_ALT, SEYREK_MAX);
            TaniTelAralik = aralik; TaniTelAlfa = a;
            if (Mathf.Abs(a - seyrekA) < .01f) return; seyrekA = a;
            foreach (var m in seyrekM) if (m != null) m.color = new Color(SEYREK_RENK.r, SEYREK_RENK.g, SEYREK_RENK.b, a);
        }
        public float TaniTelAralik = -1, TaniTelAlfa = -1;
        public static PlayerView Build(Transform root, string model, bool isMe, Color forma, bool hakem = false, Gorunum gor = null)
        {
            var g = new GameObject(hakem ? "hakem" : isMe ? "ben" : "rakip"); g.transform.SetParent(root, false);
            var pv = g.AddComponent<PlayerView>(); pv.isMe = isMe; pv.hakem = hakem;
            var onceki = Kit.Topla; pv.kendiM = new List<Material>(); Kit.Topla = pv.kendiM;
            try { pv.Init(model, gor ?? new Gorunum { forma = forma, cerceve = Kit.Hex("#e9eef0") }); }
            finally { Kit.Topla = onceki; }
            return pv;
        }
        // Kurulumda yaratılan malzemeler (Kit.Topla). Önbellekli olanlar (dokuM, sacM) başka figürlerle paylaşıldığı için silinmez.
        List<Material> kendiM;
        // Sık yeniden kurulan yüzeyler (Dolap vitrini, portreler, afiş) Destroy(gameObject) yerine bunu çağırır.
        public void Sil()
        {
            if (kendiM != null)
            {
                foreach (var m in kendiM) if (m != null && !Kit.Paylasilan.Contains(m)) Destroy(m);
                kendiM = null;
            }
            Destroy(gameObject);
        }

        // 2026-10-31 (Unity incelemesi D9): UnityEngine.Object'te `??` Unity'nin null karşılaştırmasını atlar; Resources.Load
        // bugün gerçek null döndürdüğü için çalışıyordu, ama yok edilmiş nesnede sessizce yanlış dala gider. Açık kontrol.
        static T IlkBulunan<T>(params string[] yollar) where T : Object
        {
            foreach (var y in yollar) { var o = Resources.Load<T>(y); if (o != null) return o; }
            return null;
        }
        // Paylaşılan klipler (2026-11-21, yasin: "oyun boyutunun makul düzeyde tutulması, küçültülmesi"): maskot modelleri
        // (kedi, tavşan, panda, robot, iki hamamböceği) Tüycük'ün iskeletine ve pişmiş kliplerine giydirilir (maskot.py); altı FBX
        // aynı 37 klibi ayrı ayrı taşıyordu. iOS build raporu (TF 21 Kas): Animations 36,6 MB, oyuncu varlıklarının %57'si.
        // Ölçüm (bpy, her klipte 3 karede bir, 53 kemik + raket/top nesneleri): altısı Tüycük'le dönüşte < 1°, konumda 1e-5 aynı.
        // Bu modellerin _clips.json'ı klipKaynak: "tuycuk" taşır; KarakterImport onların animasyonunu içe aktarmaz, burada kaynak
        // FBX'in klipleri modelin Animation'ına eklenir. Legacy eğri yolları kök altındaki kemik yoludur, kök adı girmez: aynı
        // iskelette yol birebir aynı. Oyuncak beden tipleri (uzun, kadın, güçlü) farklı kemik boylarıyla kendi IK'sını taşır,
        // paylaşılmaz. Kaynak klipler bir kez yüklenir (statik); kaynak FBX'in ağı belleğe gelir ama sahneye kurulmaz.
        static readonly Dictionary<string, AnimationClip[]> paylasilanKlip = new Dictionary<string, AnimationClip[]>();
        static Animation KlipleriBagla(Transform model, Animation anim, string kaynak)
        {
            if (anim != null && anim.GetClipCount() > 0) return anim;   // eski FBX kendi kliplerini taşıyorsa onları kullan
            AnimationClip[] kl;
            if (!paylasilanKlip.TryGetValue(kaynak, out kl))
            {
                var hepsi = Resources.LoadAll<AnimationClip>("karakter/" + kaynak); var l = new List<AnimationClip>();
                foreach (var c in hepsi) if (c != null && c.legacy && !c.name.StartsWith("__preview__")) l.Add(c);
                kl = l.ToArray(); paylasilanKlip[kaynak] = kl;
                if (kl.Length == 0) Debug.LogWarning("Birdie Smash: paylaşılan klip kaynağı boş: karakter/" + kaynak); else Debug.Log("Birdie Smash: paylaşılan klip " + kaynak + " · " + kl.Length + " klip");
            }
            if (kl.Length == 0) return anim;
            if (anim == null) { anim = model.gameObject.AddComponent<Animation>(); anim.playAutomatically = false; }
            foreach (var c in kl) if (anim[c.name] == null) anim.AddClip(c, c.name);
            return anim;
        }
        void Init(string modelAd, Gorunum gor)
        {
            yaw = new GameObject("yon").transform; yaw.SetParent(transform, false);
            string yol = ModelYolu(modelAd, gor.tip);
            var prefab = IlkBulunan<GameObject>("karakter/" + yol, "karakter/" + modelAd, "karakter/oyuncak");
            var js = IlkBulunan<TextAsset>("karakter/" + yol + "_clips", "karakter/" + modelAd + "_clips"); string klipKaynak = null;
            if (js != null)
            {
                var meta = JsonUtility.FromJson<Meta>(js.text); if (meta != null) klipKaynak = meta.klipKaynak; tipMeta = meta != null && meta.tip != null && !string.IsNullOrEmpty(meta.tip.id) ? meta.tip : null; sacMeta = meta != null ? meta.sac : null; bantMeta = meta != null && meta.bant != null && meta.bant.r > 0 ? meta.bant : null;
                if (meta != null && meta.clips != null) foreach (var c in meta.clips) { if (c.poses > 0) Match.CLIP_N[c.name] = c.poses; LOOPS[c.name] = c.loop; if (c.adim > 0 && Match.ADIM_BOY.ContainsKey(c.name)) { Match.ADIM_BOY[c.name] = c.adim * SCALE; adimBoy[c.name] = c.adim * SCALE; } }
            }
            if (prefab != null)
            {
                PrefabAdi = prefab.name; modelAdi = DokuAdi(prefab.name, modelAd); model = Instantiate(prefab, yaw).transform; model.localPosition = Vector3.zero; model.localScale = Vector3.one * SCALE;
                anim = model.GetComponentInChildren<Animation>();
                if (!string.IsNullOrEmpty(klipKaynak) && klipKaynak != prefab.name) anim = KlipleriBagla(model, anim, klipKaynak);
                // Hayvan tipi (2026-11-15): yeni FBX değil; tipin bedeni + kürk rengi (ten hücresi) + saçsız baş + hayvan kafası/kulak/kuyruk
                // (Aksesuar.Kur hayvan, Aksesuar.Bel). Çağıranın Gorunum'u değişmez: kopya üstünde ten ve saç ezilir.
                var hv = !hakem && modelAd == "oyuncak" && gor != null && gor.tip != null ? Kozmetik.Bul(Kozmetik.TIP, gor.tip) : null; if (hv != null && hv.aks == null) hv = null;
                tenDuz = hv != null; if (hv != null) { gor = gor.Kopya(); gor.ten = Renk(hv.renk); gor.sacModel = "kel"; }
                tenAtlas = gor.ten; Varyant(gor); Paint(gor); if (!hakem) RaketSekil.Uygula(model, gor.kafa);   // ten önce: Varyant gövde atlasını Paint'ten önce bağlıyor
                // 2026-11-01 (r115): ışın halesi çerçeve ağının kopyası; Paint içinde kurulunca kafa biçimi (kalp, elmas…) henüz uygulanmamıştı,
                // hale oval kaldı ve kalp çerçevenin dışında ikinci bir halka gibi göründü. Hale biçimden sonra kurulur.
                if (isinRenk.HasValue && kafa != null) IsinKur(isinRenk.Value);
                if (anim != null && anim["idle"] != null) Sample("idle", 0, null, 0, 1);
                Calibrate(); AltKatmanKur(); if (!hakem) BasKur();
                // 2026-11-14: hazır ağlı saç (karakter.json sac_model[].aks) şapkasızken kafaya takılır; şapkayla Varyant kısa kesimi açar
                var sacOge = gor.sacModel != null ? Kozmetik.Bul(Kozmetik.SAC, gor.sacModel) : null;
                string sacAks = !hakem && sacOge != null && !Aksesuar.Var(gor.sapka) ? sacOge.aks : null;
                if (!hakem) aksesuar = Aksesuar.Kur(model, kHead, Vector3.up, bantMeta, gor.sapka, gor.yuz, gor.forma, gor.sac, sacAks, hv != null ? hv.aks : null);
                if (!hakem) mimik = Aksesuar.MimikKur(model, kHead, Vector3.up, bantMeta, modelAd);
                if (!hakem && Aksesuar.Var(gor.sirt)) sirtKok = Aksesuar.Sirt(model, Vector3.up, gor.sirt);
                if (!hakem && Aksesuar.Var(gor.kol)) kolKok = Aksesuar.Kol(model, Vector3.up, gor.kol);
                if (!hakem && Aksesuar.Var(gor.bel)) Aksesuar.Bel(model, Vector3.up, gor.bel);
                else if (hv != null) { var ky = Aksesuar.Bel(model, Vector3.up, hv.aks + "_kuyruk"); if (ky != null) ky.gameObject.AddComponent<PelerinSalinim>().Kuyruk(); }   // kuyruk bel eşyasıyla aynı yerde: bel eşyası takılıyken yok; yay 2026-11-15
                if (!hakem && Aksesuar.Var(gor.ayak)) ayakKok = Aksesuar.Ayak(model, Vector3.up, gor.ayak);
                if (!hakem && kThigh.Count > 0)
                {   // ayak kilidi: boşta pozunda (taban y=0'da) bilek yüksekliği
                    ZincirKur(); bilek0 = new float[2];
                    for (int i = 0; i < 2; i++) bilek0[i] = kBacak[i * 3 + 2] != null ? Mathf.Max(.02f, yaw.InverseTransformPoint(kBacak[i * 3 + 2].position).y) : -1;
                }
            }
            else Fallback(gor.forma);
            shadow = Kit.Prim(PrimitiveType.Quad, "golge", transform, new Vector3(0, .003f, 0), new Vector3(.9f, .9f, 1), Kit.Clear(new Color(0, 0, 0, .35f), Kit.RadialTex())).transform;
            shadow.localRotation = Quaternion.Euler(90, 0, 0);
            if (!hakem) { ringBg = Ring("nefes_zemin", .11f); ringFg = Ring("nefes", .08f); }
            yaw.localRotation = Quaternion.Euler(0, isMe ? 0 : 180, 0) * calib;
        }

        LineRenderer Ring(string n, float w)
        {
            var g = new GameObject(n); g.transform.SetParent(transform, false); var lr = g.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.widthMultiplier = w; lr.material = Kit.Clear(Color.white); lr.numCapVertices = 4; lr.positionCount = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; return lr;
        }
        // Nefes halkası (HTML nefesHalka): doluyken görünmez; azaldıkça iki uçtan öne (kameraya) doğru kısalır,
        // %40 altında turuncu, %20 altında nabız atar. Anlam yalnız renkle verilmez: halkanın boyu miktarı söyler; kalıcı "Nefes"/"Yorgun"
        // etiketi 2026-11-14'te kalktı (yazı yerleşiminin dışındaydı), tek seferlik "Nefes al"/"Rakip yoruldu!" yazısı kalır.
        Transform takimDisk;
        void ShowRing(float n, float dt, bool reduce)
        {
            if (ringBg == null) return;
            ringA = Mathf.Clamp01(ringA + (n < .985f ? 1 : -1) * dt * 3.6f);
            bool on = ringA > 0; ringBg.enabled = ringFg.enabled = on; if (!on) return;
            const float R = .55f; const int N = 48;
            ringBg.positionCount = N + 1; for (int i = 0; i <= N; i++) { float a = i * Mathf.PI * 2 / N; ringBg.SetPosition(i, new Vector3(Mathf.Cos(a) * R, .01f, Mathf.Sin(a) * R)); }
            ringBg.startColor = ringBg.endColor = new Color(.03f, .06f, .07f, .55f * ringA);
            int m = Mathf.Max(2, Mathf.RoundToInt(N * n)); float c0 = -Mathf.PI / 2, half = n * Mathf.PI;   // kameraya bakan nokta: dünya -z
            ringFg.positionCount = n > .01f ? m + 1 : 0;
            for (int i = 0; i <= m && n > .01f; i++) { float a = c0 - half + 2 * half * i / m; ringFg.SetPosition(i, new Vector3(Mathf.Cos(a) * R, .012f, Mathf.Sin(a) * R)); }
            float nab = n < .2f && !reduce ? .55f + .45f * Mathf.Sin(Time.unscaledTime * 1000 / 110f) : 1;
            var col = n < .4f ? Kit.Hex("#ff9f7a") : Kit.Hex("#7ad7ff"); col.a = ringA * (n < .4f ? nab : 1);
            ringFg.startColor = ringFg.endColor = col;
        }
        public float RingAlpha { get { return ringA; } }

        Quaternion calib = Quaternion.identity;
        // Yön ve dikey hizalama İSKELETTEN (2026-10-26, yasin build 38: "3B olanlar ters görünüyor", "gerçekçi karakterler
        // render edilmiyor"). Unity ekran turu ölçtü: FBX kökünün doğrudan çocukları (bas__/goz__ işaretleri, servis topu
        // vekilleri) derili ağa göre X ekseninde 180° dönük geliyor; işaretten çıkarılan "ileri" bu yüzden tersti (Oyuncak'ta
        // yüz kameraya, rakipte sırt fileye bakıyordu) ve Rocketbox kökü yan yatmış, ağ zeminin 1 m altına gömülüydü.
        // Şimdi: boşta pozunda baş, iki üst kol ve iki ayak kemiğinden karakterin yukarı/sağ/ileri ekseni ölçülür, model
        // yaw'a göre dik ve +Z'ye bakar hâle döndürülür, tabanı y=0'a, ayak ortası eksene oturtulur. Kemik adları üç
        // iskeletin hepsinde bulunur: KayKit (head, upperarm.l, foot.l), Rocketbox (Bip01 Head, Bip01 L UpperArm,
        // Bip01 L Foot), sade/Mesh2Motion (head, upperarm_l, foot_l). İşaretler yalnız kemik bulunamazsa yedek.
        Transform kHead, kHip; readonly List<Transform> kThigh = new List<Transform>(), kCalf = new List<Transform>();
        static bool Sol(string n) { return n.EndsWith(".l") || n.EndsWith("_l") || n.Contains(" l ") || n.Contains("left"); }
        static bool Sag(string n) { return n.EndsWith(".r") || n.EndsWith("_r") || n.Contains(" r ") || n.Contains("right"); }
        void Calibrate()
        {
            Transform la = null, ra = null; var feet = new List<Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant(); if (n.Contains("__")) continue;   // ağ/parça nesneleri kemik değil
                if (kHead == null && (n == "head" || n.EndsWith(" head"))) kHead = t;
                if (kHip == null && (n == "hips" || n == "pelvis" || n.EndsWith(" pelvis"))) kHip = t;
                if (n.Contains("upperarm")) { if (Sol(n)) la = t; else if (Sag(n)) ra = t; }
                if (n.Contains("foot") && !n.Contains("step") && !n.Contains("ik")) feet.Add(t);
                if (n.Contains("thigh") || n.Contains("upperleg")) kThigh.Add(t);
                if (n.Contains("calf") || n.Contains("lowerleg")) kCalf.Add(t);
            }
            // Sınır gerçek pozdan (kırpılma yok); koşan/sıçrayan oyuncu için. 2026-11-16 inceleme (fikir 22:05 #5a): hakem
            // oturur ve rallide kıpırdamaz; her kare kemikten sınır hesabı boşa gidiyordu. Hakemde içe aktarma sınırı (ayakta
            // bağlama pozu) %40 büyütülüp sabit kalır: oturuşta öne çıkan diz ve işaret eden kol bu kutunun içinde.
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                r.updateWhenOffscreen = !hakem;
                if (hakem) { var lb = r.localBounds; lb.Expand(lb.size * .4f); r.localBounds = lb; }
            }
            if (kHead == null || la == null || ra == null || feet.Count == 0) { CalibrateIsaret(); return; }
            Vector3 fm = Vector3.zero; foreach (var f in feet) fm += yaw.InverseTransformPoint(f.position); fm /= feet.Count;
            Vector3 up = yaw.InverseTransformPoint(kHead.position) - fm, right = yaw.InverseTransformPoint(ra.position) - yaw.InverseTransformPoint(la.position);
            Vector3 fwd = Vector3.Cross(right, up); if (fwd.sqrMagnitude < 1e-8f || up.sqrMagnitude < 1e-8f) { CalibrateIsaret(); return; }
            var R0 = Quaternion.LookRotation(fwd, up);
            model.localRotation = Quaternion.Inverse(R0) * model.localRotation;
            model.localPosition = Vector3.zero; Zemin(); calib = Quaternion.identity;
            VekilDuzelt(true);
        }
        // Tabanı y=0'a, ayak ortasını yaw eksenine oturt (dönüşten sonra). Derili ağın sınırı (bounds) bir sonraki çizime
        // dek eski pozda kalır; bu yüzden kemik konumlarından: ayak/parmak kemiklerinin en alçağı − taban payı (3,5 cm).
        void Zemin()
        {
            float minY = float.MaxValue; Vector3 fm = Vector3.zero; int k = 0;
            foreach (var t in model.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant(); if (n.Contains("__") || n.Contains("step") || n.Contains("ik")) continue;
                bool ayak = n.Contains("foot"), uc = n.Contains("toe") || n.Contains("ball");
                if (!ayak && !uc) continue;
                var p = yaw.InverseTransformPoint(t.position); minY = Mathf.Min(minY, p.y); if (ayak) { fm += p; k++; }
            }
            if (minY == float.MaxValue) return; if (k > 0) fm /= k;
            model.localPosition -= new Vector3(fm.x, minY - .035f, fm.z);
        }
        // Kökün doğrudan çocukları (işaret, servis topu vekili) ağa göre dönük gelir: bas__isaret'i baş kemiğine en iyi
        // oturtan eksen dönüşü (24 aday) seçilir ve her örneklemeden sonra bu parçalara uygulanır (animasyon her karede
        // yerel değeri yeniden yazar; yazmadığı karede ikinci kez uygulanmasın diye son yazılan değer tutulur).
        Quaternion vekilF = Quaternion.identity; bool vekilVar; readonly List<Transform> vekil = new List<Transform>();
        readonly Dictionary<Transform, Pose> vekilSon = new Dictionary<Transform, Pose>();
        void VekilDuzelt(bool kur)
        {
            if (kur)
            {
                Transform bas = null;
                foreach (Transform c in model) { if (c.GetComponent<MeshRenderer>() == null) continue; if (c.name.StartsWith("bas__")) bas = c; vekil.Add(c); }
                if (bas == null || kHead == null) return;
                Vector3 hedef = model.InverseTransformPoint(kHead.position), m = bas.localPosition; float best = float.MaxValue;
                foreach (var e in new[] { Vector3.zero, new Vector3(180, 0, 0), new Vector3(0, 180, 0), new Vector3(0, 0, 180), new Vector3(90, 0, 0), new Vector3(-90, 0, 0), new Vector3(0, 0, 90), new Vector3(0, 0, -90), new Vector3(90, 180, 0), new Vector3(-90, 180, 0), new Vector3(0, 90, 0), new Vector3(0, -90, 0) })
                { var q = Quaternion.Euler(e); float d = (q * m - hedef).sqrMagnitude; if (d < best) { best = d; vekilF = q; } }
                vekilVar = vekilF != Quaternion.identity;
            }
            if (!vekilVar) return;
            foreach (var t in vekil)
            {
                Pose son; if (vekilSon.TryGetValue(t, out son) && son.position == t.localPosition && son.rotation == t.localRotation) continue;
                t.localPosition = vekilF * t.localPosition; t.localRotation = vekilF * t.localRotation; vekilSon[t] = new Pose(t.localPosition, t.localRotation);
            }
        }
        // Yedek (eski yol): işaretlerden "ileri".
        void CalibrateIsaret()
        {
            Transform head = null; var eyes = new List<Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>())
            {
                if (t.name.StartsWith("bas__")) head = t;
                if (t.name.StartsWith("goz") && t.name.Contains("__")) eyes.Add(t);
            }
            if (head == null || eyes.Count == 0) return;
            Vector3 e = Vector3.zero; foreach (var t in eyes) e += t.position; e /= eyes.Count;
            Vector3 f = yaw.InverseTransformDirection(e - head.position); f.y = 0;
            if (f.sqrMagnitude < 1e-8f) return;
            calib = Quaternion.FromToRotation(f.normalized, Vector3.forward);
        }

        public static string Etiket(string n)
        {
            int i = n.IndexOf("__"); string tag = i >= 0 ? n.Substring(i + 2) : "";
            int dot = tag.IndexOfAny(new[] { '.', ' ' }); if (dot >= 0) tag = tag.Substring(0, dot); return tag;
        }
        // Parça kanalları (HTML turParlat): 0 forma, 1 raket (cerceve/grip/tel/tel2), 2 bant, 3 bileklik.
        public readonly List<Renderer>[] kanal = { new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
        public Transform Model { get { return model; } }
        string modelAdi;
        // Gömülü doku Unity'de malzemeye bağlanmıyor (ekran turu rapor.txt: Rocketbox ten malzemesi siyah, doku yok): gövde ve
        // baş renk dokusu FBX'ten çıkarılıp Resources'a kondu (<model>_govde / <model>_bas.jpg), malzeme adına göre bağlanır.
        Material DokuMalzeme(Material asil)
        {
            if (asil == null) return null; string n = asil.name.ToLowerInvariant(); string ek = n.Contains("body") ? "_govde" : n.Contains("head") ? "_bas" : null;
            if (ek == null) return null; var tx = Resources.Load<Texture2D>("karakter/" + modelAdi + ek); if (tx == null) return null;
            string anahtar = modelAdi + ek; bool tenli = ek == "_govde" && tenAtlas.HasValue && modelAdi.StartsWith("oyuncak");
            if (tenli) anahtar += "#" + ColorUtility.ToHtmlStringRGB(tenAtlas.Value) + (tenDuz ? "k" : "");
            Material m;
            if (tenli) { tenSira.Remove(anahtar); tenSira.Add(anahtar); }
            if (!dokuM.TryGetValue(anahtar, out m) || m == null)
            {
                if (tenli)
                {
                    tx = TenliAtlas(tx, tenAtlas.Value, tenDuz);
                    // 2026-10-31 (Unity incelemesi O3): her model × ton bir atlas kopyası (~1,3 MB) üretir ve statik sözlük
                    // onu sonsuza dek tutuyordu. En eski tonlar sözlükten düşer; yok etmek yerine bırakılır, çünkü sahnedeki bir
                    // figür hâlâ kullanıyor olabilir. Menüye dönüşteki UnloadUnusedAssets kullanılmayanı boşaltır.
                    while (tenSira.Count > TEN_LRU) { dokuM.Remove(tenSira[0]); tenSira.RemoveAt(0); }
                }
                dokuM[anahtar] = m = Kit.LitTex(Color.white, tx, .18f); Kit.Paylas(m);
                // 2026-10-26: Gerçekçi model düz/çamurlu görünüyordu (yalnız renk dokusu): kıvrım/kas/yüz normalden,
                // ten/kumaş/ayakkabı farkı parlaklık haritasından. Dosya yoksa (oyuncak, sade) eski malzeme birebir.
                Kit.Bump(m, Resources.Load<Texture2D>("karakter/" + modelAdi + ek + "_n"));
                Kit.Gloss(m, Resources.Load<Texture2D>("karakter/" + modelAdi + ek + "_mg"));
            }
            return m;
        }
        static readonly Dictionary<string, Material> dokuM = new Dictionary<string, Material>();
        static readonly List<string> tenSira = new List<string>(); const int TEN_LRU = 6;
        // 2026-10-31 (yasin: "Ten renkleri değiştirilemiyor"): Oyuncak ailesinde ten ayrı parça değil, gövde ağının renk
        // atlasında bir hücre. "__ten" etiketi olmadığı için ten tonu hiçbir şeye bağlanmıyordu. Atlas GPU'da bir RT'ye
        // kopyalanıp okunur (doku Read/Write kapalı ve ASTC: DokuAyar), ten hücreleri seçilen tona boyanır, sonuç model+ton
        // başına bir kez üretilir. Ton yoksa ("Modelin rengi") atlas aynen.
        // r93 düzeltmesi: ilk yazım her modelde teni 221,179,151 sandı; Blender'da ağın UV'leri ölçülünce (yüzey alanı payı)
        // modeller üç ten hücresini farklı rollerde kullanıyor: oyuncak 129,97,63 (%55), güçlü 209,160,142 (%67), kadın ve
        // uzun 221,179,151 (%36 / %61). Diğer ikisi o modelde küçük ayrıntı (dudak, kulak içi). Bu yüzden atlas başına asıl
        // ten hücresi tabloda; üç hücre de seçilen tona asıl tene oranlarıyla boyanır (ayrıntı tenden açık/koyu kalır).
        // Atlas yeniden üretilip roller değişirse tabloyu tool/karakter ölçümüyle güncelle.
        Color? tenAtlas; bool tenDuz; public static string TenTani = "yok";   // ekran turu tanısı
        static readonly Color32[] TEN_HUCRE = { new Color32(221, 179, 151, 255), new Color32(209, 160, 142, 255), new Color32(129, 97, 63, 255) };
        static Color32 AsilTen(string atlas)
        {
            if (atlas == "oyuncak_govde") return TEN_HUCRE[2];
            if (atlas == "oyuncak_guclu_govde") return TEN_HUCRE[1];
            return TEN_HUCRE[0];
        }
        static bool Yakin(Color32 a, Color32 b) { return Mathf.Abs(a.r - b.r) <= 8 && Mathf.Abs(a.g - b.g) <= 8 && Mathf.Abs(a.b - b.b) <= 8; }
        static byte Oran(byte ton, byte hucre, byte asil) { return (byte)Mathf.Clamp(Mathf.RoundToInt(ton * (float)hucre / Mathf.Max(1, asil)), 0, 255); }
        // duz (2026-11-15 hayvan kürkü, r186): oranlı boyama insan teni için; kürkte ayrıntı hücresi asıl tenden açıksa oran 255'e
        // kırpılıp rengi kaydırıyordu (turuncu kedi kolu sarı-yeşil çıktı). Kürk üç hücreye de aynı renkle, oransız yazılır.
        static Texture2D TenliAtlas(Texture2D kaynak, Color ten, bool duz = false)
        {
            int w = kaynak.width, h = kaynak.height;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(kaynak, rt); var once = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true, false) { name = kaynak.name + "_ten", filterMode = kaynak.filterMode, anisoLevel = kaynak.anisoLevel, wrapMode = kaynak.wrapMode };
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0, false); RenderTexture.active = once; RenderTexture.ReleaseTemporary(rt);
            Color32 tn = ten, asil = AsilTen(kaynak.name); var yeni = new Color32[TEN_HUCRE.Length];
            for (int k = 0; k < TEN_HUCRE.Length; k++) { var c = TEN_HUCRE[k]; yeni[k] = duz ? new Color32(tn.r, tn.g, tn.b, 255) : new Color32(Oran(tn.r, c.r, asil.r), Oran(tn.g, c.g, asil.g), Oran(tn.b, c.b, asil.b), 255); }
            var px = t.GetPixels32(); int say = 0;
            for (int i = 0; i < px.Length; i++) for (int k = 0; k < TEN_HUCRE.Length; k++) if (Yakin(px[i], TEN_HUCRE[k])) { px[i] = yeni[k]; if (Yakin(TEN_HUCRE[k], asil)) say++; break; }
            TenTani = kaynak.name + " " + w + "x" + h + " asıl ten " + asil.r + "," + asil.g + "," + asil.b + " → " + tn.r + "," + tn.g + "," + tn.b + " eşleşen " + say;
            if (say == 0) { Object.Destroy(t); return kaynak; }   // atlasta ten hücresi yok: dokuya dokunma
            t.SetPixels32(px); t.Apply(true, true); return t;
        }
        void Paint(Gorunum g)
        {
            tenAtlas = g.ten;
            // Dokulu modelde (Gerçekçi) forma = gövde dokusu × takım rengi + normal: forma dokusu açık gri gövde, koyu gri
            // yaka/raglan/kol ağzı; takım rengi gölgesi ve iki tonlu kesimiyle görünür. Doku yoksa eski düz renk birebir.
            // 2026-10-26 (oyun tasarımı incelemesi, r22 oyuncak_12/15: iki oyuncu da lacivert): oyuncak ailesinin _govde
            // dokusu renk ATLASI (oyuncak.py, 8×8 düz hücre); forma ağı atlasa girmez ama UV'si bir hücreye (lacivert şort)
            // düşüyordu → takım rengi × lacivert = iki takım da koyu lacivert. Doku × renk yalnız tinte hazır (açık gri
            // forma dokusu) modelde: ölçüt normal haritası (_govde_n) — bugün yalnız Gerçekçi. Atlas modeli düz takım rengi.
            var govdeTx = modelAdi != null && Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_n") != null
                ? Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde") : null;
            // 2026-10-27 forma kumaşı görünür (yasin: "forma tişört"): dokulu modelde kumaşın kendi forma dokusu
            // (<model>_forma_<kumas>: rocketbox_kit.py _kumas; hafif yan panel + delik, nefes çift şerit, esnek çapraz kuşak) ve
            // parlaklığı. Dosya yoksa (klasik, ya da doku üretilmemiş model) gövde dokusu: bugünkü görünüş birebir.
            var formaTx = govdeTx != null && g.kumas != null ? Resources.Load<Texture2D>("karakter/" + modelAdi + "_forma_" + g.kumas) : null;
            var formaM = govdeTx != null ? Kit.LitTex(g.forma, formaTx ?? govdeTx, .25f) : Kit.Lit(g.forma, .25f);
            if (govdeTx != null)
            {
                Kit.Bump(formaM, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_n"));
                Kit.Gloss(formaM, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_mg"), KumasParlak(g.kumas));
            }
            var M = new Dictionary<string, Material> { { "forma", formaM } };
            // Şort (2026-10-27): Gerçekçi'de ayrı derili ağ (rb_sort__sort, doku açık gri) → doku × şort rengi, seçim yoksa eski
            // lacivert (SORT_VARSAYILAN; doku 2026-10-27'ye dek bu rengi basılı taşıyordu). Düz renkli modelde (Sade) yalnız
            // oyuncu seçtiyse boyanır; seçim yoksa FBX'in kendi rengi. Hakem şortu aşağıda ayrıca lacivert (#0a1530).
            if (govdeTx != null)
            {
                var sm = Kit.LitTex(g.sort ?? Kit.Hex(SORT_VARSAYILAN), govdeTx, .2f); Kit.Bump(sm, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_n"));
                Kit.Gloss(sm, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_mg")); M["sort"] = sm;
            }
            else if (g.sort.HasValue) M["sort"] = Kit.Lit(g.sort.Value, .2f);
            // Karın (2026-10-29, Tüycük: tc_karin__karin): gövde forma rengiyle boyandığı için karın aynı rengin açığı (beyaza %60);
            // rakip ve hakem de kendi renklerinin açığını alır. Etiket yalnız Tüycük'te var: diğer modeller birebir aynı.
            M["karin"] = Kit.Lit(Color.Lerp(g.forma, Color.white, .6f), .3f);
            aykDoku = govdeTx;
            if (g.cerceve.HasValue) M["cerceve"] = Kit.Lit(g.cerceve.Value, .6f);
            isinRenk = g.isin && g.cerceve.HasValue && !hakem ? g.cerceve : null;
            if (g.saft.HasValue) M["koyu"] = Kit.Lit(g.saft.Value, .45f);   // şaft, boğaz ve dip kapağı (raket_koyu__koyu)
            if (g.bant.HasValue) M["bant"] = Kit.Lit(g.bant.Value, .2f); if (g.bilek.HasValue) M["bilek"] = Kit.Lit(g.bilek.Value, .2f);
            if (g.grip.HasValue) M["grip"] = g.gripCizgi ? Kit.LitTex(g.grip.Value, CizgiDoku(), .1f) : Kit.Lit(g.grip.Value, .15f); if (g.tel.HasValue) M["tel"] = Kit.Lit(g.tel.Value, .5f); if (g.tel2.HasValue) M["tel2"] = Kit.Lit(g.tel2.Value, .5f);
            if (isinRenk.HasValue) IsinMalzeme(M, isinRenk.Value);
            if (g.ten.HasValue) M["ten"] = Kit.Lit(g.ten.Value, .3f); if (g.sac.HasValue) { M["sac"] = Kit.Lit(g.sac.Value, .2f); if (g.sacParla) Parlat(M["sac"]); } if (g.ayakkabi.HasValue) M["taban"] = Kit.Lit(g.ayakkabi.Value, .25f);
            bool bantGizle = sacBantGizle;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string tag = Etiket(r.gameObject.name);
                // hakemde raket ve top (pozla kurulan rijit parçalar = deri ağı olmayan MeshRenderer'lar) gizli
                if (tag == "ayakkabi") { AyakkabiBoya(r, g.ayakkabi); continue; }
                if (tag == "kulak" && Aksesuar.Var(g.sapka)) { r.enabled = false; continue; }   // maskot kulağı/anteni şapkayı delmesin (maskot.py)
                if (tag == "sac" && r.gameObject.name.StartsWith("sac_")) { SacBoya(r, g.sac, g.sacParla); continue; }
                if (tag == "isaret" || (tag == "bant" && (!g.bant.HasValue || bantGizle)) || (tag == "bilek" && !g.bilek.HasValue) || (hakem && !(r is SkinnedMeshRenderer))) { r.enabled = false; continue; }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                int kn = tag == "forma" ? 0 : tag == "cerceve" || tag == "grip" || tag == "tel" || tag == "tel2" ? 1 : tag == "bant" ? 2 : tag == "bilek" ? 3 : -1; if (kn >= 0) kanal[kn].Add(r);
                Material m; M.TryGetValue(tag, out m);
                if (m != null && (tag == "ten" || tag == "sac" || tag == "taban") && DokuluMu(r)) m = null;   // dokulu parça kendi dokusunu korur
                if (m == null)
                {
                    var asl = r.sharedMaterials; bool degisti = false;
                    for (int k = 0; k < asl.Length; k++) { var dm = DokuMalzeme(asl[k]); if (dm != null) { asl[k] = dm; degisti = true; } }
                    if (degisti) { r.sharedMaterials = asl; continue; }
                    if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name.Contains("Error")) m = Kit.Lit(Color.gray); else continue;
                }
                var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = m; if (arr.Length == 0) arr = new[] { m }; r.sharedMaterials = arr;
            }
            if (hakem) { var hg = Kit.Lit(Kit.Hex("#0a1530"), .2f); foreach (var r in model.GetComponentsInChildren<Renderer>()) if (r.gameObject.name.Contains("__sort")) r.sharedMaterial = hg; }
            // Salınım yayı (HTML player(): sw > .3 iken omuz çevresinde 3 beyaz yay, alfa sw·.5): 3B'de raket kafasının izi.
            // Hareketi azalt açıkken çizilmez (HTML ile aynı).
            if (!hakem) foreach (var t in model.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("raket_kafa")) { kafa = t; break; }
            if (kafa != null)
            {
                yay = kafa.gameObject.AddComponent<TrailRenderer>(); yay.time = .12f; yay.minVertexDistance = .03f; yay.emitting = false;
                yay.widthCurve = new AnimationCurve(new Keyframe(0, .14f), new Keyframe(1, 0)); yay.material = Kit.Additive(Color.white);
                yay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; yay.receiveShadows = false;
            }
        }
        Transform kafa; TrailRenderer yay; Color? isinRenk;
        // Işın raketi (2026-11-01, yasin: "ışın kılıcıyla oynama falan da olabilecek, yaratıcılıkta sınır tanıma"). Yalnız görünüş.
        // Sahnede bloom yok (yerleşik hat, mobil bütçe): parlama üç katmanla kurulur. (1) Çerçeve beyaza yakın çekirdek + güçlü
        // emisyon, kordaj aynı rengin emisyonu (ışık açısından bağımsız hep parlak). (2) Hale: çerçeve ağlarının eklemeli kopyası,
        // kafa merkezinden %9 büyük, gölge düşürmez; kordaj boşluğunu doldurmaz çünkü yalnız boru kopyalanır. (3) Salınım izi
        // beyaz yerine kaplama renginde, iki kat geniş. Hareketi azalt kuralı izde aynen geçerli (iz çizilmez, hale durur).
        static void IsinMalzeme(Dictionary<string, Material> M, Color c)
        {
            var cek = Kit.Lit(Color.Lerp(c, Color.white, .55f), .8f); cek.EnableKeyword("_EMISSION"); cek.SetColor("_EmissionColor", c * 1.3f);
            cek.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; M["cerceve"] = cek;
            var tel = Kit.Lit(Color.Lerp(c, Color.white, .3f), .5f); tel.EnableKeyword("_EMISSION"); tel.SetColor("_EmissionColor", c * .9f);
            tel.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; M["tel"] = tel; M["tel2"] = tel;
        }
        void IsinKur(Color c)
        {
            var hm = Kit.Additive(new Color(c.r, c.g, c.b, .55f));
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (Etiket(r.gameObject.name) != "cerceve" || r.name.EndsWith("_hale")) continue; var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var h = Kit.MeshObj(r.name + "_hale", r.transform, mf.sharedMesh, hm).transform;
                var mrk = r.transform.InverseTransformPoint(kafa.position); h.localScale = Vector3.one * 1.09f; h.localPosition = mrk * (1 - 1.09f);
            }
            yay.widthCurve = new AnimationCurve(new Keyframe(0, .28f), new Keyframe(1, 0)); yay.time = .16f; yay.material = Kit.Additive(c);
            yayRenk = new[] { new GradientColorKey(c, 0), new GradientColorKey(Color.Lerp(c, Color.white, .5f), 1) };
        }
        GradientColorKey[] yayRenk;
        BantMeta bantMeta; Transform aksesuar, sirtKok, kolKok; Transform[] ayakKok; public Transform[] AyakKok { get { return ayakKok; } } public Transform KolKok { get { return kolKok; } } public Transform AksesuarKok { get { return aksesuar; } } public Transform SirtKok { get { return sirtKok; } }
        readonly Gradient yayGrad = new Gradient(); readonly GradientAlphaKey[] yayS = new GradientAlphaKey[2]; float yayA = -1;
        static readonly GradientColorKey[] YAY_RENK = { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) };
        // Varsayılan şort: SORT_RENK (rocketbox_kit, düz lacivert .075/.095/.15) ÷ doku grisi .86 (gamma uzayında çarpım).
        public const string SORT_VARSAYILAN = "#161c2c";
        // Kumaşın parlaklık çarpanı (_GlossMapScale): esnek (likralı) parlak, hafif (delikli örgü) mat. Bilinmeyen = 1.
        public static float KumasParlak(string k) { return k == "esnek" ? 1.35f : k == "hafif" ? .8f : k == "nefes" ? .9f : 1f; }
        Texture2D aykDoku;
        bool DokuluMu(Renderer r) { foreach (var a in r.sharedMaterials) if (DokuMalzeme(a) != null) return true; return false; }
        // Havlu grip: sapı saran açık-koyu şerit (16×64, tekrar eden; bir kez üretilir, tüm figürler paylaşır).
        static Texture2D cizgi;
        static Texture2D CizgiDoku()
        {
            if (cizgi != null) return cizgi; cizgi = new Texture2D(16, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "grip_cizgi" };
            var px = new Color32[16 * 64]; for (int y = 0; y < 64; y++) for (int x = 0; x < 16; x++) { byte v = (byte)(((y + x / 2) / 6) % 2 == 0 ? 255 : 170); px[y * 16 + x] = new Color32(v, v, v, 255); }
            cizgi.SetPixels32(px); cizgi.Apply(); return cizgi;
        }
        // Yükseltme parlaması (HTML turParlat): o kanalın parçaları limona yanıp söner; k 0..1.
        public void Parlat(int kn, float k)
        {
            foreach (var r in kanal[kn]) foreach (var m in r.materials)
            {
                if (k > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.85f, 1, .35f) * k * 1.2f); }
                else m.SetColor("_EmissionColor", Color.black);   // anahtar açık kalır (2026-11-07): kapatıp açmak her parlamada varyant değiştiriyordu; siyah emisyon görüntüde aynıdır
            }
        }

        // ---- Karakter varyantları (2026-10-26; Core/Kozmetik, varlık sözleşmesi karakter_varlik_sozlesmesi.md) ----
        // Tip: Oyuncak modelin beden tipi ayrı FBX (oyuncak_<varlik>.fbx, aynı iskelet ailesi ve klip adları). Gerçekçi ve sade
        // modelde tip yok (tek beden; Soyunma odası bunu yazar). Dosya yoksa (karakter hattı henüz üretmediyse) taban model, hata
        // yok: Init önce bu yolu, sonra taban modeli dener. Beden yalnız görünüş: vuruş menzili ve fizik Core'da, boydan bağımsız.
        public static string ModelYolu(string modelAd, string tip)
        {
            if (modelAd != "oyuncak") return modelAd;
            var o = Kozmetik.Oge(Kozmetik.TIP, tip); return o != null && o.varlik != null ? modelAd + "_" + o.varlik : modelAd;
        }
        // Doku adı: tip FBX'inin kendi gövde dokusu yoksa taban modelinki (oyuncak_govde). Varsayım: tipler aynı UV yerleşimini
        // paylaşır (aynı taban ağdan şekil anahtarıyla türer); sözleşme başka derse tipin kendi dokusu konur, kod değişmez.
        static string DokuAdi(string prefabAd, string modelAd)
        {
            if (prefabAd == modelAd || Resources.Load<Texture2D>("karakter/" + prefabAd + "_govde") != null) return prefabAd;
            return Resources.Load<Texture2D>("karakter/" + modelAd + "_govde") != null ? modelAd : prefabAd;
        }
        // Saç ve ayakkabı parçaları: sac_<varlik>__sac, ayk_<varlik>__ayakkabi. Tam olarak biri açık, diğerleri SetActive(false)
        // (çizim çağrısı sıfır, deri hesabı da yok). Seçilen parça bu FBX'te yoksa varsayılanın (listenin ilki) parçası, o da
        // yoksa ilk bulunan: figür hiçbir zaman saçsız/ayakkabısız kalmaz. Kazıtılmış (varlik null) bilerek hiçbirini açmaz.
        // 2026-10-27: Karakter v3'ten (51f396a) beri dört oyuncak FBX'inin her birinde 11 saç + 5 ayakkabı parçası var (eski not
        // "bugünkü oyuncak.fbx'te yok" geçersiz). Sade modelde parça yok: sözlükler boş, hiçbir şey değişmez. 2026-10-27'den beri
        // Gerçekçi'de beş ayakkabı parçası var (rocketbox_aksesuar.py; saç yok: saç dokuda, beden tek).
        bool sacBantGizle; TipMeta tipMeta; SacMeta[] sacMeta;
        readonly Dictionary<string, float> adimBoy = new Dictionary<string, float>();
        // Açılan parçalar (tanı ve vitrin kadrajı): istenen bulunamayıp varsayılana düşüldüyse istenenden farklıdır.
        public string AcikSac { get; private set; }
        public string AcikAyk { get; private set; }
        public string PrefabAdi { get; private set; }
        void Varyant(Gorunum g)
        {
            var sac = new Dictionary<string, GameObject>(); var ayk = new Dictionary<string, GameObject>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                int i = t.name.IndexOf("__"); if (i <= 4) continue; string bas = t.name.Substring(0, i), tag = Etiket(t.name);
                if (tag == "sac" && bas.StartsWith("sac_")) sac[bas.Substring(4)] = t.gameObject;
                else if (tag == "ayakkabi" && bas.StartsWith("ayk_")) ayk[bas.Substring(4)] = t.gameObject;
            }
            // Seçim yoksa (hakem, eski çağrı) ya da seçilen parça bu FBX'te yoksa tipin varsayılanı (sözleşme §6.2: _clips.json tip).
            string sm = g.sacModel; if (Aksesuar.Var(g.sapka) && sm != null && (Aksesuar.HACIMLI.Contains(sm) || Aksesuar.AksSac(sm))) sm = "kisa";   // şapkanın altında hacimli saç (ve hazır ağlı saç) kısa kesime döner
            var so = sm != null ? Kozmetik.Bul(Kozmetik.SAC, sm) : null; var sv = Kozmetik.Oge(Kozmetik.SAC, null);
            string sacVars = tipMeta != null && !string.IsNullOrEmpty(tipMeta.sac) ? tipMeta.sac : sv != null ? sv.varlik : null;
            string acik = VaryantAc(sac, so != null ? so.varlik : sacVars, so != null && so.varlik == null, sacVars); AcikSac = acik;
            bool bant = so == null || so.bant; if (acik != null && sacMeta != null) foreach (var m in sacMeta) if (m != null && m.id == acik) bant = m.bant;   // FBX'in ölçtüğü kazanır
            sacBantGizle = acik != null && !bant;   // bant yalnız o saç gerçekten takılıysa gizlenir
            GearModel am = null; if (g.aykModel != null) Gear.GEAR["ayakkabi"].TryGetValue(g.aykModel, out am);
            GearModel av; Gear.GEAR["ayakkabi"].TryGetValue(Gear.Varsayilan("ayakkabi"), out av);
            string aykVars = tipMeta != null && !string.IsNullOrEmpty(tipMeta.ayakkabi) ? tipMeta.ayakkabi : av != null ? av.varlik ?? av.id : null;
            AcikAyk = VaryantAc(ayk, am != null ? am.varlik ?? am.id : aykVars, false, aykVars);
        }
        // Tanı satırı (2026-10-27, yasin build 41: "3B model kaybolmuş, seçmeme rağmen görmedim"): hangi FBX yüklendi, hangi
        // saç/ayakkabı parçası açık, kaç renderer gerçekten çiziliyor (açık + etkin, iz hariç), baş yüksekliği ve omuz genişliği
        // (kemikten: AABB figür döndükçe değişir, kemik aralığı değişmez), açık saç ve ayakkabı parçasının sınırı. Ekran turu
        // her tip/saç/ayakkabı için vitrinde ve maçta yazar; bir seçimin görünüşe gerçekten yansıyıp yansımadığı sayıyla okunur.
        public string VaryantOzet() { float b, o; Vector3 sb; int n; return VaryantOzet(out b, out o, out sb, out n); }
        public string VaryantOzet(out float basY, out float omuzG, out Vector3 sacBoyut, out int aktif)
        {
            basY = omuzG = -1; sacBoyut = Vector3.zero; aktif = 0;
            if (model == null) return "MODEL YOK";
            int n = 0; Bounds sacB = new Bounds(), aykB = new Bounds(); bool sv = false, av = false; Transform bas = null, ol = null, or_ = null;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r is TrailRenderer || !r.enabled) continue; n++; string tag = Etiket(r.gameObject.name);
                if (tag == "sac") { if (!sv) { sacB = r.bounds; sv = true; } else sacB.Encapsulate(r.bounds); }
                if (tag == "ayakkabi" || tag == "taban") { if (!av) { aykB = r.bounds; av = true; } else aykB.Encapsulate(r.bounds); }
            }
            foreach (var t in model.GetComponentsInChildren<Transform>())
            {
                string k = t.name.ToLowerInvariant(); if (k.Contains("__")) continue;
                if (bas == null && (k == "head" || k.EndsWith(" head"))) bas = t;
                if (ol == null && (k == "upperarm_l" || k == "upperarm.l" || k.EndsWith(" l upperarm"))) ol = t;
                if (or_ == null && (k == "upperarm_r" || k == "upperarm.r" || k.EndsWith(" r upperarm"))) or_ = t;
            }
            aktif = n; if (bas != null) basY = bas.position.y - transform.position.y; if (ol != null && or_ != null) omuzG = Vector3.Distance(ol.position, or_.position); if (sv) sacBoyut = sacB.size;
            var ci = System.Globalization.CultureInfo.InvariantCulture; System.Func<Vector3, string> V = v => v.x.ToString("0.00", ci) + "×" + v.y.ToString("0.00", ci) + "×" + v.z.ToString("0.00", ci);
            return "prefab " + PrefabAdi + " · tip " + (tipMeta != null ? tipMeta.id : "-") + " · saç " + (AcikSac ?? "yok") + " · ayk " + (AcikAyk ?? "yok") + " · aktif renderer " + n
                + " · baş " + (bas != null ? (bas.position.y - transform.position.y).ToString("0.000", ci) : "-") + " · omuz " + (ol != null && or_ != null ? Vector3.Distance(ol.position, or_.position).ToString("0.000", ci) : "-")
                + " · saç sınır " + (sv ? V(sacB.size) : "-") + " · ayk sınır " + (av ? V(aykB.size) : "-");
        }
        // Vitrinin Yüz planı: açık saç parçası (afro, topuz kadrajdan taşmasın diye kadraj saçın sınırına göre kurulur).
        public Renderer SacRenderer()
        {
            if (model == null) return null;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is TrailRenderer) && Etiket(r.gameObject.name) == "sac") return r;
            return null;
        }
        static string VaryantAc(Dictionary<string, GameObject> d, string istenen, bool hicbiri, string varsayilan)
        {
            if (d.Count == 0) return null;
            string ac = hicbiri ? null : istenen != null && d.ContainsKey(istenen) ? istenen : varsayilan != null && d.ContainsKey(varsayilan) ? varsayilan : null;
            if (ac == null && !hicbiri) foreach (var k in d.Keys) { ac = k; break; }
            foreach (var kv in d) kv.Value.SetActive(kv.Key == ac);
            return ac;
        }
        // Ayakkabı (sözleşme §3): iki malzeme yuvası. "ayk_ust" (saya) seçili ayakkabı rengiyle boyanır (null = FBX'in açık
        // rengi), "oy_body" (taban, bağcık, topuk) gövde atlasına bağlanır (DokuMalzeme), boyanmaz: kontrast kaybolmasın.
        // Eski (__taban) etiketli modellerde boyama aynen M["taban"] ile sürer.
        // 2026-10-27 Gerçekçi (rocketbox_aksesuar.py): beş ayakkabı modeli aynı yuva adlarıyla; saya dokulu: gövde dokusu (beyaz
        // saya, gri parçalar) × renk + normal (seçim yoksa beyaz = dokunun kendisi), taban "m301_body" (klasik: kauçuk, doku) ya
        // da "ayk_alt_*" (FBX'in düz rengi: beyaz ince/kalın taban, kömür karbon) boyanmaz.
        void AyakkabiBoya(Renderer r, Color? c)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var arr = r.sharedMaterials; bool degisti = false;
            for (int k = 0; k < arr.Length; k++)
            {
                if (arr[k] == null) continue;
                if (arr[k].name.StartsWith("ayk_ust") && aykDoku != null)
                {
                    var um = Kit.LitTex(c ?? Color.white, aykDoku, .3f); Kit.Bump(um, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_n"));
                    Kit.Gloss(um, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_mg")); arr[k] = um; degisti = true;
                }
                else if (arr[k].name.StartsWith("ayk_ust")) { if (c.HasValue) { arr[k] = Kit.Lit(c.Value, .3f); degisti = true; } }
                else { var dm = DokuMalzeme(arr[k]); if (dm != null) { arr[k] = dm; degisti = true; } }
            }
            if (degisti) r.sharedMaterials = arr;
        }
        // Saç (sözleşme §3): tel/AO dokusu (oyuncak_sac, gri tonlu) × saç rengi; seçim yoksa tipin varsayılan rengi (_clips.json
        // tip.sacRenk), o da yoksa FBX malzemesi. Doku yoksa düz renk (AO kaybolur, görünüş yine düzgün).
        static readonly Dictionary<string, Material> sacM = new Dictionary<string, Material>();
        static void Parlat(Material m) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", m.color * .5f); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        void SacBoya(Renderer r, Color? c, bool parla = false)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Color? renk = c ?? (tipMeta != null && !string.IsNullOrEmpty(tipMeta.sacRenk) ? Kit.Hex(tipMeta.sacRenk) : (Color?)null);
            var tx = Resources.Load<Texture2D>("karakter/oyuncak_sac"); if (!renk.HasValue && tx == null) return;
            if (tx != null) tx.wrapMode = TextureWrapMode.Repeat;   // tel dokusu UV'de 1'i aşar; .meta içe aktarma ayarına bağlı kalmasın
            Color rk = renk ?? (r.sharedMaterial != null ? r.sharedMaterial.color : Color.gray);
            // 2026-10-31 (Unity incelemesi Y1): 16 saç parçasının her biri ayrı Material açıyordu (figür başına 16, her
            // saç/renk değişikliğinde yeniden). Renk + doku başına tek malzeme paylaşılır; saç malzemesine sonradan
            // yazan kod yok (parlatma yalnız raket, kanal boyası forma/raket), bu yüzden paylaşmak görünüşü değiştirmez.
            string sk = ColorUtility.ToHtmlStringRGBA(rk) + (tx != null ? "t" : "d") + (parla ? "p" : ""); Material m;
            if (!sacM.TryGetValue(sk, out m) || m == null) { sacM[sk] = m = tx != null ? Kit.LitTex(rk, tx, .2f) : Kit.Lit(rk, .2f); Kit.Paylas(m); if (parla) Parlat(m); }
            var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = m; if (arr.Length == 0) arr = new[] { m }; r.sharedMaterials = arr;
        }
        // Vitrin tutuş görünümü (Soyunma odası › Raket: Forehand · Backhand · File · Servis): klibin temas pozunda durur; yalnız
        // poz değişince örneklenir (karede ayırma ve örnekleme yok).
        string pozK; float pozP = -1;
        public void ShowPoz(Vector3 pos, float yawDeg, string clip, float ph)
        {
            transform.position = pos; yaw.localRotation = Quaternion.Euler(0, yawDeg, 0) * calib;
            if (shadow != null) shadow.position = new Vector3(pos.x, .003f, pos.z);
            if (anim == null) return; if (D(clip) == null) clip = "idle";
            if (cur == clip && pozK == clip && Mathf.Abs(pozP - ph) < 1e-4f) return;
            cur = clip; pozK = clip; pozP = ph; Sample(clip, ph, null, 0, 1); UstKol(clip, ph, 1); GirmeKoru();
        }

        void Fallback(Color forma)
        {
            model = new GameObject("yedek").transform; model.SetParent(yaw, false);
            Kit.Prim(PrimitiveType.Capsule, "govde", model, new Vector3(0, .8f, 0), new Vector3(.45f, .55f, .35f), Kit.Lit(forma));
            Kit.Prim(PrimitiveType.Sphere, "bas", model, new Vector3(0, 1.55f, 0), Vector3.one * .38f, Kit.Lit(Kit.Hex("#eeb890")));
            if (hakem) return;
            fbRacket = new GameObject("raket").transform; fbRacket.SetParent(model, false); fbRacket.localPosition = new Vector3(.3f, 1.2f, 0);
            Kit.Prim(PrimitiveType.Cylinder, "sap", fbRacket, new Vector3(0, .3f, 0), new Vector3(.03f, .3f, .03f), Kit.Lit(Kit.Hex("#2b2f3a")));
            Kit.Prim(PrimitiveType.Cylinder, "kafa", fbRacket, new Vector3(0, .75f, 0), new Vector3(.4f, .005f, .5f), Kit.Lit(Color.white)).transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        static float ClipTime(string clip, float ph)
        {
            bool loop; int n;
            if (LOOPS.TryGetValue(clip, out loop) && loop) return Mathf.Repeat(ph, 1);
            if (!Match.CLIP_N.TryGetValue(clip, out n) || n < 2) return Mathf.Clamp01(ph);
            return Mathf.Clamp01((ph * n - .5f) / (n - 1));
        }

        // Alt beden katmanı (2026-10-26, yasin "model karakterlerin tüm hareketleri badmintona uygun olmalı"): hazırlık ve vuruş
        // klipleri bütün bedeni sürüyordu; oyuncu topa koşarken de bacaklar vuruş duruşunda donup zeminde kayıyordu (bot
        // ölçümü, tool/unity_test: hazırlık/vuruş karelerinin ~%45'inde gövde 0,3 m/s'den hızlı, ortalama 0,9–1,4 m/s). Spor
        // oyunlarının olağan çözümü: üst beden vuruştan, leğen ve bacaklar ayak işi klibinden. Ayak işi klibinin kopyası
        // (<ad>_alt) yalnız leğeni (özyinelemesiz) ve uyluk zincirlerini sürer, katman 1'dedir; Legacy Animation katman 1'in
        // ağırlığını yalnız onun sürdüğü kemiklerden düşer, omurga/kollar/raket vuruş klibinde kalır. Ağırlık gövde hızıyla
        // açılır (Match.ALT_HIZ üstel yumuşatma: pop yok; eşik ve istisnalar Match.AltHedef'te). Bacakları vuruşun kendisi olan kliplerde
        // (hamle, sıçrama, servis, split, yorgun) kapalı, istisnalar orada. Kemikler bulunamazsa kurulmaz: eski davranış birebir. HTML'de karşılığı yok (sprite tek
        // parça); bu yalnız görünüm, oyun kuralı değil.
        static readonly string[] AYAK_KLIP = { "chasse_r", "chasse_l", "fwd", "back_r", "back_l", "run", "kros_r", "kros_l", "geri_r", "geri_l" };   // run/kros_*: 2026-10-26 hız bantları
        bool altVar; float altW, altPh; string altKlip;
        void AltKatmanKur()
        {
            if (anim == null || kHip == null || kThigh.Count < 2) return;
            var kok = new List<Transform>();   // iç içe uyluk kemikleri (twist) bir kez: üstteki özyinelemeli eklenince altı da gelir
            foreach (var t in kThigh) { bool alt = false; for (var p = t.parent; p != null; p = p.parent) if (kThigh.Contains(p)) { alt = true; break; } if (!alt) kok.Add(t); }
            foreach (var c in AYAK_KLIP)
            {
                var st = anim[c]; if (st == null || st.clip == null) continue;
                anim.AddClip(st.clip, c + "_alt"); var a = anim[c + "_alt"]; if (a == null) continue;
                a.AddMixingTransform(kHip, false); foreach (var t in kok) a.AddMixingTransform(t, true);
                a.layer = 1; altVar = true;
            }
            durum.Clear(); durumlar = null;   // yeni _alt durumları eklendi
        }

        // Durum önbelleği (2026-10-27 performans turu 2): anim["ad"] her çağrıda yerelden yeni bir AnimationState sarmalayıcısı
        // döndürür, foreach (AnimationState in anim) bir sayaç nesnesi, alt + "_alt" yeni bir dizgi: figür başına karede ~12
        // ayırma, dört figür (iki oyuncu + iki hakem) ve afiş/vitrinle karede ~60. Durumlar ad başına bir kez sorulur (yoksa null
        // da saklanır: klip listesi yalnız AltKatmanKur'da değişir, orada önbellek sıfırlanır), tüm durumlar dizide tutulur.
        readonly Dictionary<string, AnimationState> durum = new Dictionary<string, AnimationState>(); AnimationState[] durumlar;
        static readonly Dictionary<string, string> ALT_AD = new Dictionary<string, string>();
        static string AltAd(string klip) { string a; if (!ALT_AD.TryGetValue(klip, out a)) ALT_AD[klip] = a = klip + "_alt"; return a; }
        AnimationState D(string ad)
        {
            if (anim == null || ad == null) return null; AnimationState st;
            if (!durum.TryGetValue(ad, out st)) { st = anim[ad]; durum[ad] = st; }
            return st;
        }
        AnimationState[] Durumlar()
        {
            if (durumlar != null) return durumlar;
            var l = new List<AnimationState>(); foreach (AnimationState st in anim) l.Add(st); durumlar = l.ToArray(); return durumlar;
        }
        int ornekNo;   // her Sample'da artar: ShowSit "pozum hâlâ benim" diye bakar
        // c (prev2): harmanlama sürerken gelen değişimde sönen üçüncü poz; eski karışımın (1 − w) payının cPay kadarı (Show notu).
        void Sample(string a, float pa, string b, float pb, float w, string alt = null, float aPh = 0, float aW = 0, string c = null, float pc = 0, float cPay = 0)
        {
            if (anim == null) return;
            ornekNo++; var tum = Durumlar(); for (int i = 0; i < tum.Length; i++) tum[i].enabled = false;
            var A = D(a); if (A == null) return;
            A.enabled = true; A.weight = w; A.normalizedTime = ClipTime(a, pa);
            if (b != null && w < 1)
            {
                var B = D(b);
                if (B != null)
                {
                    float wb = 1 - w; var C2 = c != null && cPay > .001f && c != a && c != b ? D(c) : null;
                    if (C2 != null) { C2.enabled = true; C2.weight = wb * cPay; C2.normalizedTime = ClipTime(c, pc); wb *= 1 - cPay; }
                    B.enabled = true; B.weight = wb; B.normalizedTime = ClipTime(b, pb);
                }
            }
            if (alt != null && aW > .01f) { var C = D(AltAd(alt)); if (C != null) { C.enabled = true; C.weight = aW; C.normalizedTime = ClipTime(alt, aPh); } }
            anim.Sample();
            for (int i = 0; i < tum.Length; i++) tum[i].enabled = false;
            VekilDuzelt(false);
        }

        // Hakem: oturur (BWF: baş hakem file hizasında yüksek sandalyede, servis hakemi karşıda normal sandalyede; yasin build
        // 38 "hakemler yok": eskiden ayaklığın üstünde ayakta, sırtı korta dönüktü).
        // 2026-10-26 (yasin: "hakemler fizik kurallarına uygun şekilde sandalyelerinde oturmalı"): eski sürüm boşta pozuna
        // sabit ±88° ekliyordu; boşta pozu badminton hazır duruşu (dizler kırık, gövde öne eğik) olduğundan dizler havada,
        // ayaklar ayaklığın üstünde boşlukta kalıyordu. Şimdi oturuş eklem hedeflerinden kurulur, her iskelette aynı:
        //  1) gövde dikleştirilir (kalça→baş ekseni düşey, 6° geriye: sırtlığa yaslanır),
        //  2) kalça eklemleri oturak yüzünün uyluk kalınlığı kadar üstüne, sırtlığın önüne (oturak merkezinin 0,1 m gerisi),
        //  3) iki kemik IK ile ayak bileği destek yüzüne (ayaklık ya da zemin + bilek yüksekliği), diz öne-yukarı,
        //  4) eller uylukların üstüne (dizden 1/3 geride), dirsek dışa-geriye.
        // ayakY: ayakların bastığı yüzeyin dünya yüksekliği; ayakIleri: ayakların oturak merkezinden ilerideki mesafesi.
        Transform[] kBacak, kKol;   // [sol uyluk, sol baldır, sol ayak, sağ uyluk, sağ baldır, sağ ayak]; kol: üst, ön, el
        static Transform Cocuk(Transform t, System.Func<string, bool> ok)
        {
            foreach (Transform c in t) { var n = c.name.ToLowerInvariant(); if (!n.Contains("__") && ok(n)) return c; }
            foreach (Transform c in t) { var r = Cocuk(c, ok); if (r != null) return r; }
            return null;
        }
        void ZincirKur()
        {
            kBacak = new Transform[6]; kKol = new Transform[6];
            foreach (var t in kThigh) { var n = t.name.ToLowerInvariant(); int i = Sol(n) ? 0 : Sag(n) ? 3 : -1; if (i < 0 || kBacak[i] != null) continue;
                kBacak[i] = t; kBacak[i + 1] = Cocuk(t, m => m.Contains("calf") || m.Contains("lowerleg"));
                if (kBacak[i + 1] != null) kBacak[i + 2] = Cocuk(kBacak[i + 1], m => m.Contains("foot") && !m.Contains("ik") && !m.Contains("roll")); }
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant(); if (n.Contains("__") || !n.Contains("upperarm")) continue; int i = Sol(n) ? 0 : Sag(n) ? 3 : -1; if (i < 0 || kKol[i] != null) continue;
                kKol[i] = t; kKol[i + 1] = Cocuk(t, m => m.Contains("forearm") || m.Contains("lowerarm"));
                if (kKol[i + 1] != null) kKol[i + 2] = Cocuk(kKol[i + 1], m => (m.Contains("hand") || m.Contains("wrist")) && !m.Contains("ik") && !m.Contains("slot"));
            }
            // Uzanma eğimi için sağ omzun atalarından omurga kemikleri, alttan üste (spine_01.. / Bip01 Spine..).
            kOmurga.Clear();
            for (var t = kKol[3] != null ? kKol[3].parent : null; t != null && t != model; t = t.parent)
                if (t.name.ToLowerInvariant().Contains("spine")) kOmurga.Insert(0, t);
        }
        readonly List<Transform> kOmurga = new List<Transform>();
        // ---- Baş içine girme koruması (2026-11-21, yasin: "animasyonlarda karakterin skinlerinin içinden geçmesi") ----
        // Maskotlar Tüycük'ün kliplerini taşır ama başları çok daha büyük: bpy ölçümünde backhand drop/over'da raket eli başın
        // arkasına 20–25 cm, backhand geri adımda 14 cm giriyordu; oyuncak tiplerinde servis, clear ve round-the-head'de 8–10 cm.
        // Klipleri model başına yeniden pişirmek boyutu geri büyütürdü (maskotların klip paylaşımı); bunun yerine poz bittikten
        // sonra kol başın dışına itilir. Baş küresi tool/karakter/carpisma_olc.py'den (Resources/karakter/carpisma.json, boyun
        // boyu L biriminde, eklem çerçevesinde). İlk çağrıda kemik çerçevesinden kurulur ve baş kemiğine yerel olarak saklanır,
        // sonra baş dönse de küre onunla gider. Her karede iki kol × üç nokta (ön kol ortası, bilek, el ucu) en çok 4 tur:
        // en derin nokta el ise ön kol dirsekten, ön kol ise üst kol omuzdan döner (tur başına en çok 35°, sıçrama yok).
        // Ayırma yok, idempotent: aynı poza ikinci kez uygulanırsa nokta zaten dışarıda olduğundan bir şey yapmaz.
        static Dictionary<string, Vector4> carpisma;
        bool basKuruldu; Vector3 basYerel; float basR, kolKalin;
        public float GirmeTani { get; private set; }   // son çağrıda düzeltmeden önceki en derin giriş (m, sahne ölçeğinde); ekran turu raporu
        static Vector4? CarpismaBul(string ad)
        {
            if (carpisma == null)
            {
                carpisma = new Dictionary<string, Vector4>();
                var ta = Resources.Load<TextAsset>("karakter/carpisma"); var t = ta != null ? ta.text : "";
                // {"ad": {"bas": [x, y, z, r]}, ...}: JsonUtility sözlük okumaz; biçim sabit, elle ayrıştırılır.
                int i = 0;
                while ((i = t.IndexOf("\"bas\"", i)) >= 0)
                {
                    int q1 = t.LastIndexOf('"', t.LastIndexOf('{', i) - 1), q0 = q1 > 0 ? t.LastIndexOf('"', q1 - 1) : -1;
                    int a = t.IndexOf('[', i), b = t.IndexOf(']', a); i = b;
                    if (q0 < 0 || a < 0 || b < 0) break;
                    var p = t.Substring(a + 1, b - a - 1).Split(','); if (p.Length < 4) continue;
                    var v = new float[4]; bool ok = true;
                    for (int k = 0; k < 4; k++) ok &= float.TryParse(p[k].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v[k]);
                    if (ok) carpisma[t.Substring(q0 + 1, q1 - q0 - 1)] = new Vector4(v[0], v[1], v[2], v[3]);
                }
            }
            Vector4 r; return ad != null && carpisma.TryGetValue(ad, out r) ? r : (Vector4?)null;
        }
        // Init'te boşta pozunun ilk karesi örneklendikten hemen sonra (ölçüm de idle'ın ilk karesinden): baş-boyun açısı ve omuz
        // yüksekliği vuruş pozlarında değiştiği için çerçeve başka pozda kurulursa küre kayardı.
        void BasKur()
        {
            basKuruldu = true; basR = 0; if (kHead == null) return; if (kKol == null) ZincirKur();
            var c = CarpismaBul(PrefabAdi); Transform boyun = kHead.parent, kl = kKol[0] != null ? kKol[0].parent : null, kr = kKol[3] != null ? kKol[3].parent : null;
            if (c == null || boyun == null || kl == null || kr == null || kKol[1] == null || kKol[2] == null) return;
            Vector3 u = kHead.position - boyun.position; float L = u.magnitude; if (L < 1e-4f) return; u /= L;
            Vector3 sv = Vector3.ProjectOnPlane(kr.position - kl.position, u).normalized, f = Vector3.Cross(sv, u);   // Blender u × s'nin karşılığı (sol el koordinatı)
            var cv = c.Value; Vector3 mer = kHead.position + (sv * cv.x + u * cv.y + f * cv.z) * L; float olc = Mathf.Max(1e-4f, kHead.lossyScale.x);
            basYerel = kHead.InverseTransformPoint(mer); basR = cv.w * L / olc;
            kolKalin = .12f * (kKol[2].position - kKol[1].position).magnitude / olc;
        }
        void GirmeKoru()
        {
            if (!basKuruldu || basR <= 0 || kHead == null || kKol == null) return;
            float olc = kHead.lossyScale.x; Vector3 mc = kHead.TransformPoint(basYerel); float R = (basR + kolKalin) * olc; GirmeTani = 0;
            for (int s = 0; s < 2; s++)
            {
                Transform ua = kKol[s * 3], fa = kKol[s * 3 + 1], h = kKol[s * 3 + 2]; if (ua == null || fa == null || h == null) continue;
                for (int tur = 0; tur < 4; tur++)
                {
                    Vector3 e = fa.position, w = h.position, uc = w + (w - e) * .5f;
                    Vector3 p0 = (e + w) * .5f; float d0 = R - (p0 - mc).magnitude, d1 = R - (w - mc).magnitude, d2 = R - (uc - mc).magnitude;
                    float d = Mathf.Max(d0, Mathf.Max(d1, d2)); if (tur == 0) GirmeTani = Mathf.Max(GirmeTani, d); if (d <= .002f * olc) break;
                    Vector3 p = d == d0 ? p0 : d == d1 ? w : uc; Vector3 n = p - mc; n = n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.up;
                    Transform don = d == d0 ? ua : fa; Vector3 piv = don.position;
                    var q = Quaternion.FromToRotation(p - piv, p + n * (d + .002f * olc) - piv);
                    don.rotation = Quaternion.RotateTowards(Quaternion.identity, q, 35) * don.rotation;
                }
            }
        }
        // İki kemikli IK: a (omuz/kalça) → b (dirsek/diz) → c (el/bilek) zinciri c hedefe, b kutup yönüne bükülür.
        static void IK(Transform a, Transform b, Transform c, Vector3 hedef, Vector3 kutup)
        {
            if (a == null || b == null || c == null) return;
            float la = (b.position - a.position).magnitude, lb = (c.position - b.position).magnitude; Vector3 d = hedef - a.position;
            float L = Mathf.Clamp(d.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f); Vector3 u = d.normalized;
            float ca = Mathf.Clamp((la * la + L * L - lb * lb) / (2 * la * L), -1, 1);
            Vector3 p = Vector3.ProjectOnPlane(kutup, u).normalized; if (p.sqrMagnitude < 1e-6f) p = Vector3.ProjectOnPlane(Vector3.up, u).normalized;
            Vector3 diz = a.position + u * (la * ca) + p * (la * Mathf.Sqrt(1 - ca * ca));
            a.rotation = Quaternion.FromToRotation(b.position - a.position, diz - a.position) * a.rotation;
            b.rotation = Quaternion.FromToRotation(c.position - b.position, a.position + u * L - b.position) * b.rotation;
        }
        // Oturuş bir kez (2026-10-27 performans turu 2): hakemler rallide kıpırdamaz ama her karede idle örneklenip dört IK
        // çözülüyordu (iki figür × iki Sample + kalça/baş/4 zincir). Girdiler (oturak, açılar, ayak yeri) aynı, araya başka
        // örnekleme girmemiş (ornekNo) ve kök yerinden oynamamışsa poz zaten kemiklerde: dönülür. Skor girişi ve servis
        // dönüşü açıyı değiştirdiği karelerde aynen yeniden kurulur.
        Vector3 sitO, sitKonum; Vector4 sitK; Quaternion sitYaw; int sitNo = -1;
        public void ShowSit(Vector3 oturak, float yawDeg, float tiltDeg, float ayakY = 0, float ayakIleri = .4f)
        {
            var sk = new Vector4(yawDeg, tiltDeg, ayakY, ayakIleri);
            if (sitNo == ornekNo && sitO == oturak && sitK == sk && transform.position == sitKonum && yaw.localRotation == sitYaw) return;
            ShowSitKur(oturak, yawDeg, tiltDeg, ayakY, ayakIleri);
            sitO = oturak; sitK = sk; sitNo = ornekNo; sitKonum = transform.position; sitYaw = yaw.localRotation;
        }
        void ShowSitKur(Vector3 oturak, float yawDeg, float tiltDeg, float ayakY, float ayakIleri)
        {
            yaw.localRotation = Quaternion.Euler(0, yawDeg, 0) * calib;
            transform.position = Vector3.zero;
            if (anim != null) Sample("idle", .25f, null, 0, 1); cur = "idle";
            if (kHip == null || kThigh.Count == 0) { transform.position = oturak; shadow.position = new Vector3(oturak.x, .003f, oturak.z); return; }
            if (kBacak == null) ZincirKur();
            Vector3 ileri = yaw.forward, sag = yaw.right;
            // Bilek yüksekliği ve boy, ayakta boşta pozundan (taban y=0'da).
            float bilek = kBacak[2] != null ? Mathf.Max(.04f, kBacak[2].position.y) : .08f, boy = kHead != null ? kHead.position.y : 1.6f, olc = boy / 1.6f;
            // 1) gövde dik, hafif geri
            if (kHead != null)
            {
                Vector3 dik = Quaternion.AngleAxis(-6, sag) * Vector3.up;
                kHip.rotation = Quaternion.FromToRotation(kHead.position - kHip.position, dik) * kHip.rotation;
            }
            if (kHead != null && tiltDeg != 0) kHead.rotation = Quaternion.AngleAxis(tiltDeg, sag) * kHead.rotation;
            // 2) kalça eklemlerinin ortası oturağa (uyluk kalınlığı ~0,08 m × ölçek)
            Vector3 kalca = kBacak[0] != null && kBacak[3] != null ? (kBacak[0].position + kBacak[3].position) / 2 : kHip.position;
            Vector3 hedef = oturak + Vector3.up * (.085f * olc) - ileri * .1f;
            transform.position = hedef - kalca;
            // 3) bacaklar: bilek destek yüzüne, dizler öne-yukarı; ayaklar kalça genişliğinde, hafif açık
            for (int s = 0; s < 2; s++)
            {
                var th = kBacak[s * 3]; if (th == null) continue; float yon = s == 0 ? -1 : 1;
                Vector3 bas = oturak + ileri * ayakIleri + sag * (yon * .13f * olc); bas.y = ayakY + bilek;
                IK(th, kBacak[s * 3 + 1], kBacak[s * 3 + 2], bas, ileri + Vector3.up * .6f);
            }
            // 4) eller uylukların üstünde
            for (int s = 0; s < 2; s++)
            {
                var ua = kKol[s * 3]; var th = kBacak[s * 3]; var kn = kBacak[s * 3 + 1]; if (ua == null || th == null || kn == null) continue; float yon = s == 0 ? -1 : 1;
                Vector3 el = Vector3.Lerp(th.position, kn.position, .62f) + Vector3.up * (.09f * olc) + sag * (yon * .02f);
                IK(ua, kKol[s * 3 + 1], kKol[s * 3 + 2], el, sag * yon * .5f - ileri);
            }
            shadow.position = new Vector3(transform.position.x, .003f, transform.position.z);
        }
        public void ShowStatic(Vector3 pos, float yawDeg, float tiltDeg)
        {
            transform.position = pos; yaw.localRotation = Quaternion.Euler(tiltDeg, yawDeg, 0) * calib;
            shadow.position = new Vector3(pos.x, .003f, pos.z);
            if (anim != null && cur != "idle") { cur = "idle"; Sample("idle", .25f, null, 0, 1); }
        }

        float jGor;
        const float KAY_SURE = .30f; float kaySonX = float.NaN, kaySonY, kayX, kayY, kayT = KAY_SURE;
        float KayK() { float u = 1 - kayT / KAY_SURE; return u * u * u; }   // kalan ofset payı: 1 → 0, kübik ease-out
        public void Show(Athlete o, Match M, float dt)
        {
            KordajGuncelle();   // yalnız maçtaki rakipte dolu (KordajSeyrek); diğer figürlerde hemen döner
            // kendi bacağının adımı ve adım tavanı (Match.Athlete.adimBoy/ayakTurMax notu): tavan chassé adımı 0,99 m'den
            // kısaysa o oranda yükselir (en çok 6 tur/sn); gerçek oranlı modellerde adım ≥ 0,99, tavan aynen 3,6.
            // Tur 2: her karede yazılır (adım boyu yoksa null/NaN): menü arkasındaki maç model değişiminden sonra da sürüyor,
            // eski figürün sözlüğü sporcuda kalıyordu.
            { float ch; o.adimBoy = adimBoy.Count > 0 ? adimBoy : null; o.ayakTurMax = adimBoy.TryGetValue("chasse_r", out ch) && ch > .05f ? Mathf.Clamp(Match.AYAK_TUR_MAX * .99f / ch, Match.AYAK_TUR_MAX, 6f) : float.NaN; }
            // Sıçrama: sin(jump·π) (tepe temasta), split step küçük hop (%20). 0,5 m ≈ HTML'deki 16 px/28 px-m ölçeği.
            string clip; float ph; M.PickClip(o, isMe, out clip, out ph);   // önce: split klibi kendi hop'unu taşır (Match.JumpH hopsuz)
            float j = Match.JumpH(o, clip == "split" && anim != null && D("split") != null) * .5f;
            jGor = j > jGor ? Mathf.MoveTowards(jGor, j, Match.GORSEL_YUKSELIS * dt) : j; j = jGor;   // yükseliş tavanı: ışınlanma yok
            // Sayı arası dönüş (2026-11-11 inceleme, fikir 21:05 #1): SetupServe iki oyuncuyu servis karesine tek karede yazıyor;
            // dip köşeden 3–5 m ışınlanma her sayıda "oyun sıfırlandı" gibi okunuyordu. Yalnız görünümde: bir karede 0,5 m'den uzun
            // sıçrama (koşu en çok ~0,3 m/kare) eski görsel konumdan 0,30 sn ease-out kayarak kapanır. Core konumu aynı (BotMac,
            // ikiz maçlar etkilenmez). Servis girişi bekletilmedi: servis klibi ≥ 0,32 sn, top fırlarken kayma zaten bitmiş olur.
            // Hareketi azaltta eski anında geçiş.
            float cx = o.x, cy = o.y;
            if (!float.IsNaN(kaySonX) && !Ayar.reduce && dt > 0 && dt < .2f)
            {
                float ddx = kaySonX + kayX * KayK() - cx, ddy = kaySonY + kayY * KayK() - cy;
                if (Mathf.Abs(cx - kaySonX) + Mathf.Abs(cy - kaySonY) > .5f) { kayX = ddx; kayY = ddy; kayT = 0; }
            }
            else kayT = KAY_SURE;
            kaySonX = cx; kaySonY = cy; kayT = Mathf.Min(KAY_SURE, kayT + dt);
            float kk = KayK(); transform.position = Kit.W(cx + kayX * kk, cy + kayY * kk, 0) + Vector3.up * j;
            // Gövde dik (ce47f5c): hamlede yatma yok; yalnız kıl payı kurtarışta (yat) en çok 15°. Koşu eğimi ±0,08 rad (~4,6°;
            // Ar-Ge 23 Eki B3: render edilmiş figürü düz resim gibi yatırmak sahte görünür, ayaklar yerden kalkar).
            // 2026-10-27 (ekran/kosu): gövde gidişe döner (Match.AyakIsi, Athlete.donus; arka sahaya yan dönük, ön köşeye
            // köşeye bakarak). Eğim gövdenin kendi yanına göre: dönükken dünya x'inden eğilmek öne/arkaya yatırırdı.
            float donR = o.donus * Mathf.Deg2Rad, yanK = isMe ? o.svx : -o.svx, ileriK = isMe ? -o.svy : o.svy;
            float lean = Mathf.Clamp((yanK * Mathf.Cos(donR) - ileriK * Mathf.Sin(donR)) * .02f, -.08f, .08f);
            float dive = (o.yat ? o.dive : 0) * (o.x < Court.MID ? -1 : 1) * .26f * (isMe ? 1 : -1);
            yaw.localRotation = Quaternion.Euler(0, (isMe ? 0 : 180) + o.donus, 0) * Quaternion.Euler(0, 0, -(dive + lean) * Mathf.Rad2Deg) * calib;
            shadow.position = Kit.W(o.x, o.y, .003f); shadow.localScale = Vector3.one * (.9f - j * .8f);
            // Takım diski (stüdyo incelemesi 2026-11-05, sanat 1): yüksek kameradan oyuncak figürün büyük başı gövdeyi örtüyor,
            // kim nerede ilk bakışta okunmuyordu. Ayak altında yumuşak kenarlı disk: sen limon, rakip orkide (palet C, HUD ile aynı).
            // Gölgenin altında, düşük alfa: kortu boyamaz, yalnız konumu işaretler. Hakemde yok.
            if (!hakem) { if (takimDisk == null) takimDisk = Kit.Prim(PrimitiveType.Quad, "takim_disk", transform, Vector3.zero, Vector3.one, Kit.Clear(isMe ? new Color(.77f, .93f, .25f, .2f) : new Color(.91f, .61f, 1f, .2f), Kit.RadialTex())).transform;
                takimDisk.rotation = shadow.rotation; takimDisk.position = Kit.W(o.x, o.y, .002f); takimDisk.localScale = Vector3.one * (1.1f - j * .5f); }
            ShowRing(o.nefes, dt, M.reduce); ImzaIsik(o, M.reduce, dt);
            if (yay != null)
            {
                // Stüdyo incelemesi (2026-11-05, teknik sanat 7): iz hazırlığın son kısmında (ileri salınım) da açık; eskiden yalnız
                // temastan sonra başlıyor, basamaklı hazırlık pozu kopuk görünüyordu. Duran raket iz bırakmaz (minVertexDistance).
                bool on = o.sw <= 0 && o.prepA > .9f && o.prepT < .2f;   // yalnız temasa 0,2 sn kala (koşarken kalkık raket iz bırakmasın)
                yay.emitting = (o.sw > .3f || on) && !M.reduce; float a = Mathf.Max(Mathf.Clamp01(o.sw), on ? .7f : 0) * .5f;
                // 2026-10-27: renk eğrisi her karede yeni Gradient + iki dizi idi; yalnız alfa değişince ve aynı nesneyle
                if (Mathf.Abs(a - yayA) > .002f) { yayA = a; yayS[0] = new GradientAlphaKey(a, 0); yayS[1] = new GradientAlphaKey(0, 1); yayGrad.SetKeys(yayRenk ?? YAY_RENK, yayS); yay.colorGradient = yayGrad; }
            }
            if (anim == null) { if (fbRacket != null) fbRacket.localRotation = Quaternion.Euler(0, 0, o.sw > 0 ? -140 * (1 - o.sw) + 40 : 20 - 40 * o.prepA); return; }
            if (D(clip) == null) clip = "idle";
            if (clip != cur)
            {
                // Harmanlama sürerken gelen değişim (2026-10-27 performans turu 2; ölçüm tool/unity_test/GecisOlc, Core bot maçı; kural orada kopya): klip
                // değişimlerinin %22'si (120 Hz'de %25) önceki 0,08/0,2 sn'lik geçiş bitmeden geliyordu (split → chassé → kros,
                // fwd → back → chassé); eski kodda o anki karışımın "prev" payı (ortalama %46–52) tek karede düşüyordu: pop.
                // Şimdi (1) aynı klibe geri dönüşte geçiş ters çevrilir (blend → 1 − blend, ağırlıklar sürekli), (2) başka klipte
                // ekranın o anki iki pozu (cur ve prev) birlikte söner: prev2 = prev, payı karışım içindeki oranı. Dördüncü poz
                // gelirse en eskisi (prev2) düşer. Ölçüm: %25'ten büyük tek kare düşüşü 1198 → 80 (60 Hz), 1459 → 137 (120 Hz),
                // toplam düşen ağırlık −%94 / −%92.
                TaniDegisim++; if (prev != null && blend < 1) TaniAra++;
                if (prev != null && blend < 1 && clip == prev) { prev = cur; prevPh = lastPh; cur = clip; blend = 1 - blend; prev2 = null; }
                else
                {
                    if (prev != null && blend < 1)
                    {
                        float wc = Mathf.SmoothStep(0, 1, blend), wp = (1 - wc) * (1 - (prev2 != null ? prev2Pay : 0));
                        float dus = (1 - wc) * (prev2 != null ? prev2Pay : 0); TaniDusen += dus; if (dus > .25f) TaniPop++;
                        prev2 = prev; prev2Ph = prevPh; prev2Pay = wc + wp > 1e-4f ? wp / (wc + wp) : 0;
                    }
                    else prev2 = null;
                    prev = cur; prevPh = lastPh; cur = clip; blend = 0;
                }
                // Süre geçişin türüne göre (Match.GecisSure; eskiden takip .2, kalan her şey .08). Ayak işinden vuruş klibine
                // geçişte alt beden katmanı hedef ağırlıktan başlar (Match.AltGecis): bacaklar adımını kesmez.
                blendS = Match.GecisSure(o, prev, cur);
                if (altVar && prev != null) altW = Match.AltGecis(o, prev, cur, altW);
            }
            blend = Mathf.Min(1, blend + dt / blendS); lastPh = ph; if (blend >= 1) prev2 = null;
            // Boşta duruşun genliği nefese bağlı (B4; hız Match.IdleIlerle'de): nefes %70'in altına inince yorgun klibi
            // (eller dizde) %35'e kadar karışır, beden soluklanır. Harmanlama bitmeden karıştırılmaz.
            if (cur == "idle" && blend >= 1 && o.nefes < .7f && D("yorgun") != null)
            { Sample("idle", ph, "yorgun", ph, 1 - .35f * Mathf.Clamp01((.7f - o.nefes) / .7f)); uzanVar = false; kalcaKay = Vector3.zero; FileItin(); AyakKilit(dt); Sevinc(M, dt); return; }
            {   // alt beden katmanı: vuruş/hazırlık klibinde koşarken bacaklar ayak işinden. Hangi klipte ve ne ağırlıkla:
                // Match.AltHedef (hamle yaklaşırken, servis temastan sonra, sıçrama yerdeyken; anlık hızla). 2026-10-26 gece Core'a
                // taşındı: ölçüm aracı (tool/unity_test/AnimOlc) aynı kuralı okur.
                if (o.yuru && o.ayakKlip != null) { altKlip = o.ayakKlip; altPh = o.runPh; }
                float hedef = altVar && altKlip != null ? Match.AltHedef(o, cur) : 0;
                altW += (hedef - altW) * (1 - Mathf.Exp(-dt * Match.ALT_HIZ)); if (altW < .01f && hedef == 0) altW = 0;   // üstel: 60/120 Hz aynı eğri
            }
            hazirKafa = UzanHazir(o, M) ? KafaVurusta(cur) : null;   // ana örneklemeden önce: önbellekte yoksa vuruş karesi örneklenir
            Sample(cur, ph, prev, prevPh, prev == null ? 1 : Mathf.SmoothStep(0, 1, blend), altKlip, altPh, altW, prev2, prev2Ph, prev2Pay);   // smoothstep (2026-10-26): doğrusal ağırlık geçişin başında ve sonunda hız kırığı veriyordu
            UstKol(cur, ph, prev == null ? 1 : Mathf.SmoothStep(0, 1, blend));   // üstten vuruşta kol başın üstünde (uzanmadan önce: uzanma bu pozdan ölçer)
            KalcaDonus(o);   // ana örneklemeden hemen sonra: uzanma kalçayı kaydırmadan, ayak kilidi IK'dan önce
            Uzan(o, M, j);   // kayma ve kalça: ayak kilidinden önce (kilit kalça kaymasını ayakta geri alır)
            FileItin(); AyakKilit(dt); HazirNefes(o, M); UzanKol(); ServisEller();
            if (isMe && !hakem && o.jump > 0 && (cur == "jump" || o.prepClip == "jump")) SmacHareket(Secim(Kozmetik.SMAC), 1 - o.jump, M.reduce);
            Sevinc(M, dt); GirmeKoru();   // en son: sevinç ve uzanma katmanları da kolu başa sokabilir
        }

        // ---- Sevinç (2026-11-01, Ar-Ge K3; yasin "maç sonunda da sevinç kategorisi kısmını gönder") ----
        // Sayıyı kazanan oyuncu sayılar arası beklemede kısa bir hareket yapar; maç bitince kazanan zafer hareketini, kaybeden
        // "iyi oyundu"yu (raketi rakibe kaldırıp başını eğer) döngüde yapar. Klip değil: örneklenen pozun üstüne kemik işlemi
        // (iki kemik IK kollar, omurga eğimi, sıçrama ve dönüş), bu yüzden dört modelde de yeni FBX'siz çalışır. Ağırlık 0→1→0
        // yumuşak; ralli başlayınca (between bitti) .15 sn'de söner, servis pozuna karışmaz. Hareketi azaltta sıçrama, dönüş ve
        // salınım yok, kollar yalnız kalkar. Oyuncunun seçimi Ayar.KOZ (Dolap › Sevinç); rakip tarzının sevincini yapar (2026-11-07, stil.json; eskiden hep varsayılan).
        // Raket her zaman sağ elde (kural): sağ kol raketi taşır, el sallama ve alkış sol elle.
        string sevId; float sevT = -1, sevW, sevSure = SEVINC_SURE; bool sevDongu; int sevTop = -1;
        public const float SEVINC_SURE = 1.0f;
        // 2026-11-12 Danslar (yasin: "sevinçlere danslar eklenebilir; sayı arasında uzun animasyon yapılmamalı ya da sadece sıçrayarak
        // smaç veya set sayısında"): robot, tavuk, twist, dalga, helikopter, gitar. Hepsi döngülü (t'nin periyodik işlevi), yani
        // süreyi sayının türü belirler, dans değil: rakip hatasında kısa (SevincKisa), kazanılmış sayıda sayı arası kadar (~.7 sn),
        // sıçrayarak smaçla alınan sayıda (Match.sovSayi; sayı arası 1,3 sn'ye uzar) SEVINC_UZUN, maç sonunda döngü.
        // Raket hep sağ elde; hareketi azaltta salınım yok, dansın duruşu sabit kalır.
        public const float SEVINC_UZUN = 1.25f;
        // Sevinç klibinin döngü süresi, tek kaynak (2026-11-12, inceleme 17:05): Dolap'taki alım anı (KarakterOdasi.AlimAni) kendi
        // listesini tutuyordu ve dansları bilmiyordu; dans 1,6 sn'de kesilip boşta duruşa atlıyor, dans sesi yarıda kalıyordu.
        public static float SevincSure(string id) { return id == "iyi_oyun" || id == "zafer" || id == "selam" || id == "egil" || Dans(id) ? 2.4f : 1.6f; }
        // 2026-11-20 (yasin: "yeni danslar; kudurtucu sevinçler, kışkırtıcı mimikler eklenebilir; hareketler akışkan ve akıcı olmalı"):
        // iki dans (kivir, disko) ve dört kışkırtma (nanik, gelgel, opucuk, cikcik). Hepsi bütün bedenle: kalça yana kayar (KalcaKay),
        // omuzlar ve baş kalçayı geriden izler (faz farkı), kollar yay çizer (yön Slerp), yüz parçaları katılır (MimikPoz).
        // Liste tek kaynak: Afis, Sfx ön ısıtma ve ekran turu buradan okur (eskiden dört ayrı kopyaydı).
        public static readonly string[] DANSLAR = { "robot", "tavuk", "twist", "dalga", "helikopter", "gitar", "fiskiye", "ucak", "yildiz", "aglak", "kulak", "toz", "esne", "kivir", "disko", "nanik", "gelgel", "opucuk", "cikcik" };
        public static bool Dans(string id) { return System.Array.IndexOf(DANSLAR, id) >= 0; }
        const float KIVIR_W = 8f, DISKO_P = 1.2f, OPUCUK_P = 1.5f;
        // Disko: 0 = el sağ altta (gövdenin önünden çapraz), 1 = sol üstte; iki uçta kısa kalış (kırpılmış sinüs + yumuşatma).
        static float DiskoA(float t) { return Mathf.SmoothStep(0, 1, Mathf.Clamp01(.5f + .8f * Mathf.Sin(t / DISKO_P * 2 * Mathf.PI))); }
        // Gel gel: ön kol saniyede 1,7 kez kendine çağırır (hızlı gelir, yavaş açılır).
        static float GelC(float t) { float p = Mathf.Repeat(t * 1.7f, 1); return p < .4f ? Evre(p, 0, .4f) : 1 - Evre(p, .4f, 1); }
        // Öpücük: el dudakta bekler, .33–.75 sn arasında rakibe açılır, turun sonunda dudağa döner.
        static float OpA(float t) { float p = Mathf.Repeat(t, OPUCUK_P) / OPUCUK_P; return Evre(p, .22f, .5f) * (1 - Evre(p, .84f, 1)); }
        // Kalçanın yana kayması (m, + sağ): leğen kayar, ayaklar yerinde kalır (SevincBacak). Yalnız id ve t'nin işlevi.
        static float KalcaKay(string id, float t, bool rm)
        {
            switch (id)
            {
                case "kivir": return rm ? 0 : .06f * Mathf.Sin(t * KIVIR_W);
                case "disko": return .055f * (2 * (rm ? 1 : DiskoA(t)) - 1);
                case "nanik": return rm ? 0 : -.045f * Mathf.Sin(t * 8);
                case "gelgel": case "cikcik": return .03f;
            }
            return 0;
        }
        // Kademeli saat: her tam sayıda bekler, vuruşun ilk %30'unda bir sonrakine geçer (robotun kesik hareketi; sıçrama yok).
        static float Kademe(float x) { float f = Mathf.Floor(x); return f + Mathf.SmoothStep(0, 1, Mathf.Clamp01((x - f) / .3f)); }
        // Kademeli sevinç (2026-11-07 inceleme, fikir 10:06 #1): her sayıda tam sevinç, rakibin dışarı attığı topla 15 vuruşluk
        // rallinin smacını aynı ödülle karşılıyordu; tekrar eden ödül değer kaybeder, sayı arası da dolardı. Tam sevinç yalnız
        // kazanılmış sayıda: son vuruş sayıyı alanınsa (rakip dönderemedi) ya da ralli >= SEV_UZUN. Rakip hatasında (dışarı,
        // file) seçili sevincin kısası: zirvesine kadar oynar, söner (zıpla dönüşünü, zafer inişini yarıda kesmesin diye zirve +.15).
        // Uzun ralliyi kaybeden kısa başını eğer ("of"), kollar maç duruşunda: rakibin sevinci karşısında okunur. Maç sonu aynı.
        // Yalnız Game katmanı; online'da da her telefonun yerel görüntüsü (Core'a, pakete dokunmaz).
        public const int SEV_UZUN = 8; const float SEV_OF = .6f; const int SEV_TER = 5; const float SEV_BEKLE = 1.05f;
        static float SevincKisa(string id) { return Mathf.Max(.55f, SevincZirve(id) + .15f); }
        // "zafer" maç sonu sevincinin evreleri (sn): hazırlık, iniş, pompa döngüsünün başı; pompa sıklığı (yarım dalga/sn).
        const float Z_HAZIR = .2f, Z_INIS = .62f, Z_DONGU = .8f, Z_POMPA = 2f;
        void Sevinc(Match M, float dt)
        {
            if (hakem || anim == null) return;
            int top = M.sc[0] + M.sc[1];
            if (M.ogr != null) { sevT = -1; sevTop = top; return; }
            if (sevTop < 0 || top < sevTop) sevTop = top;   // ilk kare ya da yeni maç
            if (top > sevTop)
            {
                sevTop = top; bool benimki = isMe == (M.server == "me");
                if (M.matchDone) { bool kazandim = isMe ? M.sc[0] > M.sc[1] : M.sc[1] > M.sc[0]; sevId = kazandim ? (isMe ? Secim(Kozmetik.MAC_SONU) : Kozmetik.RakipSecim(Kozmetik.MAC_SONU, M.stil != null ? M.stil.macSonu : null)) : "iyi_oyun"; sevDongu = true; sevT = 0; }
                else if (benimki)
                {
                    sevId = isMe ? Secim(Kozmetik.SEVINC) : Kozmetik.RakipSecim(Kozmetik.SEVINC, M.stil != null ? M.stil.sevinc : null); sevDongu = false; sevT = 0;
                    bool kazanilmis = M.rally >= SEV_UZUN || (M.s != null && M.s.from == M.server);   // server = sayıyı alan (Point yazdı)
                    sevSure = M.sovSayi ? SEVINC_UZUN : kazanilmis ? SEVINC_SURE : SevincKisa(sevId);
                }
                else if (M.rally >= SEV_UZUN) { sevId = "of"; sevDongu = false; sevT = 0; sevSure = SEV_OF; }
                // Sayı arası kısa bekleme hareketleri (2026-11-17, yasin: "yeni hareketler ekleyelim"): sayıyı kaybeden orta uzunlukta
                // ralliden sonra alnını siler, kısa rallide üç sayıda bir raketinin tellerini düzeltir. Kısa (0,75 sn; sayı arasını
                // uzatmaz: "sayı arasında uzun animasyon yok" kuralı), kemik katmanı (yeni FBX yok), sayıya bağlı belirlenimli.
                else if (M.rally >= SEV_TER) { sevId = "ter"; sevDongu = false; sevT = 0; sevSure = SEV_BEKLE; }
                else if (top % 3 == (isMe ? 1 : 2)) { sevId = "tel"; sevDongu = false; sevT = 0; sevSure = SEV_BEKLE; }
            }
            if (sevT < 0) { sevW = 0; return; }
            sevT += dt;
            // 2026-11-19 (yasin: "hareket çok kalas gibi, gerçekçi akıcı bir hareket görülmeli anatomik olarak"): ter/tel 0,75 sn'ye
            // sıkışmıştı ve sayı arası (0,7 sn) bitince kesiliyordu. Bu ikisi kaybedenin, yani servisi karşılayanın hareketi:
            // servis atılana kadar (top yok, ralli 0) karşılama duruşunun üstünde tamamlanır; servis çıkınca kesilir. Sayı arası uzamaz.
            bool bekleme = sevId == "ter" || sevId == "tel";
            bool surer = sevDongu ? M.matchDone : sevT < sevSure && (M.between || (bekleme && M.s == null && M.rally == 0 && !M.matchDone));
            float hedef = surer ? 1 : 0; sevW = Mathf.MoveTowards(sevW, hedef, dt / (surer ? .15f : .15f));
            if (!surer && sevW <= 0) { sevT = -1; return; }
            // 2026-11-04 (yasin: "galibiyet sevinci kısmı çok dandik duruyor"): zafer kamerası yan-önden (70°) geliyor, oyuncu ise
            // fileye bakmaya devam ediyordu; sevinç profilden, yüz görünmeden okunuyordu. Maç sonu sevincinde gövde kameraya
            // dönüyor (%85: tam karşıdan değil, raketli kol kadrajda kalsın diye hafif üç çeyrek). Hareketi azaltta dönüş kesme.
            if (sevDongu && isMe && zaferBak.HasValue)
            {
                Vector3 kb = zaferBak.Value - yaw.position; kb.y = 0; Vector3 fw0 = yaw.forward; fw0.y = 0;
                if (kb.sqrMagnitude > .01f && fw0.sqrMagnitude > .01f)
                {
                    float k = (M.reduce ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01(sevT / .45f))) * sevW;
                    yaw.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(fw0, kb, Vector3.up) * .85f * k, Vector3.up) * yaw.rotation;
                }
            }
            SevincPoz(sevId, sevT, sevW, M.reduce); if (!sevDongu || sevT < 6) Kalite.Canli();   // 2026-11-19: sevinç sürerken ekran hızı yüksek; maç sonu döngüsünde ilk 6 sn (pil: kart açık kalabilir)
        }
        // Zafer kamerasının varacağı yer (GameRoot.ZaferKamera her kare yazar; zafer yoksa null).
        [System.NonSerialized] public Vector3? zaferBak;
        // Maç sonu sevincinin saati (yoksa -1): GameRoot iniş anında halka ve kıvılcım patlatır.
        public float MacSonuAn { get { return sevDongu && sevT >= 0 ? sevT : -1; } }
        public string MacSonuId { get { return sevDongu ? sevId : null; } }
        // Sevincin "zirve" anı (iniş/patlama): zafer sıçrayıp .62'de iner, zıpla .6'da, diğerleri kollar kalkınca.
        public static float SevincZirve(string id) { return id == "zafer" ? .62f : id == "zipla" ? .6f : Dans(id) ? .5f : .3f; }
        public static string SevincSecimi() { return Secim(Kozmetik.SEVINC); }
        static string Secim(string kat) { string v; return Ayar.KOZ.TryGetValue(kat, out v) && v != null ? v : Kozmetik.Varsayilan(kat); }
        // Vitrin önizlemesi (Dolap › Sevinç): boşta pozunun üstüne, döngüde.
        public void ShowSevinc(Vector3 pos, float yawDeg, string id, float t)
        {
            transform.position = pos; yaw.localRotation = Quaternion.Euler(0, yawDeg, 0) * calib;
            shadow.position = new Vector3(pos.x, .003f, pos.z);
            if (anim == null) return; Sample("idle", .25f, null, 0, 1); cur = "_sevinc";   // ShowStatic çıkışta boşta pozunu yeniden örneklesin
            float d = SevincSure(id), tt = t % d;
            float w = Mathf.Clamp01(Mathf.Min(tt / .15f, (d - tt) / .25f));
            SevincPoz(id, tt, w, Ayar.reduce); GirmeKoru(); Kalite.Canli();   // Dolap › Hareketler önizlemesi de yüksek hızda
        }
        // ---- Smaç hareketi (2026-11-10, Kozmetik.SMAC; yasin: "xp ve levele özgü animasyonlar, cool smash tricks") ----
        // Sıçrayarak smaç klibinin (jump) üstüne kemik katmanı: sevinç katmanıyla aynı yöntem, yeni FBX yok, dört modelde aynı.
        // u = sıçramanın ilerlemesi (0 kalkış, SMAC_TEMAS'ta temas, 1 iniş; Match: jump 1 → 0, temasta .53). Poz yalnız u'nun
        // işlevi (saklı durum yok): Dolap önizlemesi ve maç aynı kareyi verir. Raket kolu ve temas anı klipte kalır: katman
        // yalnız omurga, bacak ve (temastan SONRA) kök dönüşüne dokunur, top raketten aynı yerde çıkar. Hareketi azalt: hiçbiri.
        //   yay     : temasa kadar sırt geriye yay, dizler arkaya kıvrık; temasta gövde öne kapanır
        //   makas   : havada bacaklar ön-arka açılır, temasta yer değiştirir
        //   donerek : vuruştan sonra inerken kendi ekseninde bir tam tur, yere önü dönük iner
        const float SMAC_TEMAS = .47f;
        static float Pencere(float u, float a, float b, float yum) { return Mathf.SmoothStep(0, 1, Mathf.Clamp01((u - a) / yum)) * Mathf.SmoothStep(0, 1, Mathf.Clamp01((b - u) / yum)); }
        readonly float[] smUy = new float[2], smBa = new float[2];
        void SmacHareket(string id, float u, bool rm)
        {
            if (rm || id == null || id == "duz" || u <= 0 || u >= 1) return;
            if (kBacak == null) ZincirKur(); if (kBacak == null) return;
            Vector3 up = yaw.up, ri = yaw.right;
            float w = Pencere(u, 0, 1, .14f), egil = 0, uylukSol = 0, uylukSag = 0, baldirSol = 0, baldirSag = 0, don = 0;
            float gec = Mathf.SmoothStep(0, 1, Mathf.Clamp01((u - SMAC_TEMAS + .06f) / .16f));   // temasın hemen öncesinden sonrasına 0 → 1
            switch (id)
            {
                case "yay": egil = Mathf.Lerp(-20, 15, gec); baldirSol = baldirSag = Mathf.Lerp(58, 12, gec); uylukSol = uylukSag = Mathf.Lerp(10, -14, gec); break;
                case "makas": uylukSol = Mathf.Lerp(-40, 30, gec); uylukSag = Mathf.Lerp(32, -36, gec); baldirSol = Mathf.Lerp(8, 46, gec); baldirSag = Mathf.Lerp(46, 8, gec); egil = Mathf.Lerp(-6, 8, gec); break;
                case "donerek": don = 360 * Mathf.SmoothStep(0, 1, Mathf.Clamp01((u - SMAC_TEMAS - .04f) / (1 - SMAC_TEMAS - .08f))); baldirSol = baldirSag = 30 * Pencere(u, SMAC_TEMAS, 1, .2f); break;
                default: return;
            }
            if (don != 0) { yaw.localRotation = Quaternion.AngleAxis(don, Vector3.up) * yaw.localRotation; up = yaw.up; ri = yaw.right; }   // tam turda başa döner: iniş karesi klibinkiyle aynı
            if (egil != 0 && kOmurga.Count > 0) kOmurga[0].rotation = Quaternion.AngleAxis(egil * w, ri) * kOmurga[0].rotation;
            // +açı: bacak arkaya (aşağı bakan kemik için sağ eksende artı dönüş ucu geriye taşır). Sıra: önce uyluk, sonra baldır.
            float[] uy = smUy, ba = smBa; uy[0] = uylukSol; uy[1] = uylukSag; ba[0] = baldirSol; ba[1] = baldirSag;   // alan dizileri: karede ayırma yok (2026-11-11)
            for (int i = 0; i < 2; i++)
            {
                Transform th = kBacak[i * 3], kn = kBacak[i * 3 + 1]; if (th == null || kn == null) continue;
                if (uy[i] != 0) th.rotation = Quaternion.AngleAxis(uy[i] * w, ri) * th.rotation;
                if (ba[i] != 0) kn.rotation = Quaternion.AngleAxis(ba[i] * w, ri) * kn.rotation;
            }
        }
        // Vitrin önizlemesi (Dolap › Hareketler › Smaç hareketi): 1,7 sn'lik döngüde hazırlan, sıçra (maçtaki 0,53 sn), in, bekle.
        public void ShowSmac(Vector3 pos, float yawDeg, string id, float t)
        {
            const float BAS = .35f, SURE = 1f / Match.JUMP_HIZ, DONGU = 1.7f;
            float tt = t % DONGU, u = Ayar.reduce ? -1 : (tt - BAS) / SURE; bool hava = u > 0 && u < 1;
            transform.position = pos + Vector3.up * (hava ? Mathf.Sin(u * Mathf.PI) * .5f : 0); yaw.localRotation = Quaternion.Euler(0, yawDeg, 0) * calib;
            shadow.position = new Vector3(pos.x, .003f, pos.z);
            if (anim == null) return; cur = "_sevinc";
            float hit; if (!Match.CLIP_HIT.TryGetValue("jump", out hit)) hit = .6f;
            if (D("jump") == null || u <= -.6f || u >= 1.5f) { Sample("idle", .25f, null, 0, 1); return; }
            // klip fazı: kalkıştan temasa 0 → hit, temastan sonra takip (maçta sw ile aynı yön); döngü başı/sonu boşta pozuyla harmanlanır
            float ph = u < SMAC_TEMAS ? hit * Mathf.Clamp01((u + .6f) / (SMAC_TEMAS + .6f)) : hit + (1 - hit) * Mathf.Clamp01((u - SMAC_TEMAS) / .75f);
            float gir = Mathf.Clamp01(Mathf.Min((u + .6f) / .25f, (1.5f - u) / .3f));
            Sample("jump", ph, "idle", .25f, Mathf.SmoothStep(0, 1, gir));
            if (hava) SmacHareket(id, u, false);
            GirmeKoru();
        }
        // Sevinçte bacakların dikliği (0 = boşta pozunun maç duruşu, 1 = dik). Zafer: hazırlıkta çömelir, sıçrayışta açılır,
        // inişte dizden yaylanır (.18 sn), pompada dik. Zıpla: havada dik, inişte yaylanma. Diğerleri dik durur.
        // Yumuşak evre ve zarf (2026-11-19 ter/tel): 5. derece yumuşatma (hız ve ivme uçlarda sıfır), giriş × çıkış.
        static float Evre(float t, float a, float b) { float x = Mathf.Clamp01((t - a) / (b - a)); return x * x * x * (x * (6 * x - 15) + 10); }
        static float Zarf(float t, float g0, float g1, float c0, float c1) { return Evre(t, g0, g1) * (1 - Evre(t, c0, c1)); }
        static float SevincDik(string id, float t, bool rm)
        {
            // ter/tel: dizler kilitlenmez (varsayılan 1 bacağı %97 düzleştiriyordu: "kalas"); hareketle birlikte hafif doğrulur, geri çöker.
            if (id == "ter") return .5f * Zarf(t, .04f, .34f, .72f, 1.05f);
            if (id == "tel") return .35f * Zarf(t, 0, .3f, .74f, 1.05f);
            if (rm) return 1;
            if (id == "zafer")
            {
                if (t < Z_HAZIR) return .55f * (1 - Mathf.SmoothStep(0, 1, t / Z_HAZIR));
                if (t < Z_INIS) return Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - Z_HAZIR) / .1f));
                if (t < Z_DONGU) return 1 - .7f * Mathf.Sin((t - Z_INIS) / (Z_DONGU - Z_INIS) * Mathf.PI);
                return 1;
            }
            if (id == "zipla") { float u = (t - .6f) / .18f; return u > 0 && u < 1 ? 1 - .6f * Mathf.Sin(u * Mathf.PI) : 1; }
            if (id == "iyi_oyun") return .7f;   // rakibe selam: tam dik değil, rahat
            if (id == "dinlen") return 1 - .45f * Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .5f));   // soluklanma: dizler bükük
            // danslar: dizler ritimle yaylanır (leğen iner kalkar), ayaklar yerde
            if (id == "twist") return 1 - .3f * Mathf.Abs(Mathf.Sin(t * 9));
            if (id == "tavuk") return .72f - .14f * Mathf.Sin(t * 12);
            if (id == "robot") return .9f;
            if (id == "dalga") return 1 - .15f * Mathf.Abs(Mathf.Sin(t * 6));
            if (id == "gitar") return .68f - .1f * Mathf.Max(0, Mathf.Sin(t * 10));
            if (id == "fiskiye") return .84f - .08f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 6));
            if (id == "ucak") return .8f;
            if (id == "aglak") return .85f - .05f * Mathf.Abs(Mathf.Sin(t * 10));   // hıçkırıkta dizler titrer
            if (id == "kulak") return .8f;
            if (id == "yildiz") return 1 - .42f * (1 - Mathf.Sin((t % YILDIZ_P) / YILDIZ_P * Mathf.PI));   // her inişte dizler yaylanır
            if (id == "kivir") return .82f - (rm ? 0 : .08f * Mathf.Abs(Mathf.Sin(t * KIVIR_W)));
            if (id == "disko") return .86f - (rm ? 0 : .07f * Mathf.Abs(Mathf.Sin(t / DISKO_P * 2 * Mathf.PI)));
            if (id == "nanik") return .74f - (rm ? 0 : .06f * Mathf.Abs(Mathf.Sin(t * 8)));   // rakibe doğru çömelik
            if (id == "gelgel" || id == "opucuk") return .9f;
            if (id == "cikcik") return .95f;
            return 1;
        }
        [System.NonSerialized] public int TaniBacakKod; [System.NonSerialized] public float TaniBacakR;
        public bool TaniKalcaAta { get { return kHip != null && kBacak != null && kBacak[0] != null && kBacak[0].IsChildOf(kHip); } }
        readonly Vector3[] sevAyak = new Vector3[2]; readonly Quaternion[] sevAyakR = new Quaternion[2];
        void SevincBacak(float k, Vector3 fw, float kay = 0)
        {
            // Tanı (ekran turu raporu, 2026-11-12): 0 çalıştı, 1 girdi yok, 2 bacak kemiği eksik, 3 erişim yok (disk < 0), 4 aralık dışı.
            TaniBacakKod = 1; TaniBacakR = 0;
            if ((k <= .001f && kay == 0) || kHip == null || kBacak == null) return;
            Vector3 up = yaw.up; float r = float.MaxValue;
            // 2026-11-20 kalça kayması: ayak yerleri leğen kaymadan ÖNCE kaydedilir, sonra leğen yana alınır; erişim (r) kaymış
            // leğenden ölçülür (uzak bacak daha çok uzar, leğen o kadar az kalkar), ayaklar IK ile yerine döner.
            for (int i = 0; i < 2; i++)
            {
                Transform ft0 = kBacak[i * 3 + 2]; if (kBacak[i * 3] == null || kBacak[i * 3 + 1] == null || ft0 == null) { TaniBacakKod = 2; return; }
                sevAyak[i] = ft0.position; sevAyakR[i] = ft0.rotation;
            }
            if (kay != 0) kHip.position += yaw.right * kay;
            for (int i = 0; i < 2; i++)
            {
                Transform th = kBacak[i * 3], kn = kBacak[i * 3 + 1], ft = kBacak[i * 3 + 2];
                float T = ((kn.position - th.position).magnitude + (ft.position - kn.position).magnitude) * .97f;
                Vector3 v = th.position - sevAyak[i]; float b = Vector3.Dot(v, up), disk = b * b - v.sqrMagnitude + T * T;
                if (disk < 0) { TaniBacakKod = 3; return; } r = Mathf.Min(r, -b + Mathf.Sqrt(disk));
            }
            TaniBacakR = r;
            if (r > .5f || (r <= 0 && kay == 0)) { TaniBacakKod = 4; return; }   // zaten dik ya da ölçek tuhaf: dokunma
            if (r < 0) r = 0;   // kalça kaydıysa leğen kalkmasa da ayaklar yerine çekilir
            TaniBacakKod = 0;
            kHip.position += up * r * k;
            for (int i = 0; i < 2; i++)
            {
                IK(kBacak[i * 3], kBacak[i * 3 + 1], kBacak[i * 3 + 2], sevAyak[i], fw + up * .2f);
                kBacak[i * 3 + 2].rotation = sevAyakR[i];
            }
        }
        // Raket dimdik: el, raket kafası verilen yöne bakacak şekilde döner (kollar havadayken raket başın arkasında yatıyordu).
        void RaketDik(Transform el, Vector3 yon)
        {
            if (el == null || kafa == null) return; Vector3 d = kafa.position - el.position; if (d.sqrMagnitude < 1e-4f) return;
            el.rotation = Quaternion.FromToRotation(d, yon.normalized) * el.rotation;
        }
        // Canlı VS afişi (2026-11-06 ek tur fikir 1): afiş boşta pozunu (maç duruşu) örnekledikten hemen sonra çağrılır;
        // her zaman ayakta (bacak katmanı tam), id varsa kısa hareket (raket çevir, raket omuzda, kollar kavuşuk ya da Dolap'taki
        // sevinç). Yalnız yeni örneklenmiş poza uygulanır (Afis bunu sağlar): bacak katmanı leğeni yükseltir, iki kez üst üste binmez.
        public void AfisPoz(string id, float t, float w)
        {
            if (anim == null) return;
            SevincPoz("dur", 0, 1, Ayar.reduce);
            if (id != null && w > .001f) SevincPoz(id, t, w, Ayar.reduce);
            GirmeKoru();
        }
        // Kök eğrisi (sıçrama m, dönüş °, öne eğilme °, burulma °, yana yatma °, uçak dairesi rad): yalnız id ve t'nin işlevi.
        // 2026-11-19: SevincPoz'dan ayrıldı ki baş için geriden (t − BAS_GECIKME) yeniden hesaplanabilsin.
        const float BAS_GECIKME = .08f, UCAK_R = .22f;
        static void KokEgri(string id, float t, bool rm, float salla, out float hop, out float don, out float egil, out float bur, out float yat, out float orbit)
        {
            hop = 0; don = 0; egil = 0; bur = 0; yat = 0; orbit = 0;
            switch (id)
            {
                case "kollar": hop = .06f * Mathf.Abs(Mathf.Sin(t * 7)); break;
                case "zafer":   // hazırlık (öne eğil) → sıçra (sırt geriye) → in → yumruk pompası (bkz. kollar bölümü)
                    if (t < Z_HAZIR) egil = 14 * Mathf.SmoothStep(0, 1, t / Z_HAZIR);
                    else if (t < Z_INIS) { float u = (t - Z_HAZIR) / (Z_INIS - Z_HAZIR); hop = .32f * Mathf.Sin(u * Mathf.PI); egil = Mathf.Lerp(14, -12, Mathf.SmoothStep(0, 1, u * 2.2f)); }
                    else if (t < Z_DONGU) { float u = (t - Z_INIS) / (Z_DONGU - Z_INIS); egil = Mathf.Lerp(-12, -5, u) + 6 * Mathf.Sin(u * Mathf.PI); }   // iniş: göğüs bir an öne yaylanır
                    else { float tl = t - Z_DONGU; hop = .07f * Mathf.Abs(Mathf.Sin(tl * Mathf.PI * Z_POMPA)); egil = -5 + 3 * Mathf.Sin(tl * Mathf.PI * Z_POMPA * 2); bur = 9 * Mathf.Sin(tl * Mathf.PI * Z_POMPA); }
                    if (rm) { egil = -5; bur = 0; }
                    break;
                case "zipla": hop = .22f * Mathf.Sin(Mathf.Clamp01(t / .6f) * Mathf.PI); don = 360 * Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .6f)); break;
                case "egil": egil = 32 * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t % 2.4f) / .5f)); break;
                case "iyi_oyun": egil = 10 * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .35f) / .4f)); break;
                case "of": egil = 12 * Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .25f)); break;   // uzun ralli kaybı: baş öne, kollar yerinde
                // Dinlenme (2026-11-13, Oyna afişi "yorulunca durabilirler"): giriş .5 sn, nefes 0,7 Hz (göğüs inip kalkar).
                // dinlen: eller dizlerin üstünde, gövde öne eğik soluklanma; bel: dik duruş, eller belde, omuzlar nefesle kalkar.
                case "ter": { float k = Zarf(t, .04f, .34f, .72f, 1.05f), a = rm ? .5f : Evre(t, .3f, .7f); egil = 6 * k; bur = Mathf.Lerp(6, -3, a) * k; yat = 3 * k; } break;   // sol omuz öne gelir, silerken açılır
                case "tel": { float k = Zarf(t, 0, .3f, .74f, 1.05f); egil = 8 * k; bur = -5 * k; } break;   // gövde rakete doğru hafif eğilir ve döner
                case "dinlen": egil = 30 * Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .5f)) + 3 * Mathf.Sin(t * 4.4f) * salla; break;
                case "bel": egil = -3 * Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .5f)) - 2.5f * Mathf.Max(0, Mathf.Sin(t * 4.4f)) * salla; break;
                // danslar (omurga): salla = 0 (hareketi azalt) iken duruş sabit
                case "robot": egil = 4; bur = 16 * Mathf.Cos(Kademe(t * 3.2f) * Mathf.PI) * salla; break;
                case "tavuk": egil = 8 + 8 * Mathf.Max(0, Mathf.Sin(t * 12)) * salla; hop = .03f * Mathf.Abs(Mathf.Sin(t * 12)); break;   // gagalama
                case "twist": egil = 6; bur = 24 * Mathf.Sin(t * 9) * salla; break;
                case "dalga": yat = 8 * Mathf.Sin(t * 6) * salla; break;
                case "helikopter": egil = -4; hop = .05f * Mathf.Abs(Mathf.Sin(t * 8)); break;
                case "gitar": egil = 10 + 8 * Mathf.Max(0, Mathf.Sin(t * 10)) * salla; bur = -10; break;
                // 2026-11-19 (yasin: "kudurtucu animasyonlar da ekleyelim, eğlence dozu yüksek olmalı"): üç coşkulu dans.
                // fiskiye: bel altı kademede sağdan sola süpürür (her kademede küçük zıplama), 0,2 sn'de hızla geri döner.
                case "fiskiye":
                    { float c = t % 1.2f, a = c < 1 ? Kademe(c * 6) / 6 : 1 - Mathf.SmoothStep(0, 1, (c - 1) / .2f);
                      egil = 3; bur = (35 - 70 * a) * salla; hop = .025f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 6)) * salla; } break;
                // ucak: kollar kanat, küçük bir daire çizerek koşar; gövde dönüşe yatar, adımlarda hafif seker.
                case "ucak": orbit = t * 2.6f * salla; don = -orbit * Mathf.Rad2Deg; egil = 12; yat = -16 * salla; hop = .02f * Mathf.Abs(Mathf.Sin(t * 10)) * salla; break;
                // yildiz: art arda yıldız zıplaması (0,5 sn), tepede kollar V, inişte kapanır ve dizler yaylanır.
                case "yildiz": { float u = Mathf.Sin((t % YILDIZ_P) / YILDIZ_P * Mathf.PI); hop = .2f * u * salla; egil = -4 * u; } break;
                // 2026-11-20 (yasin: "daha zorbalayıcı ve karşı tarafı rahatsız edici dansları eklemeliyiz"): dört alay. Hepsi rakibe
                // dönük ve yerinde (kök dönmez): sayı arası kısa kesilse de poz okunur. Müstehcen işaret yok (mağaza yaş sınırı).
                case "aglak": egil = 6; yat = 7 * Mathf.Sin(t * 5) * salla; hop = .015f * Mathf.Abs(Mathf.Sin(t * 10)) * salla; break;   // yalancı ağlama: iki yana sallanır, hıçkırır
                case "kulak": egil = 14 + 4 * Mathf.Sin(t * 5) * salla; bur = 14; yat = -6; break;   // "duyamadım": sol kulak rakibe, gövde öne uzanır
                case "toz": egil = -5; bur = -8; break;                                               // omuzdan toz silker: çene yukarıda
                case "esne": egil = -8 - 3 * Mathf.Sin(t * 2.6f) * salla; yat = 4 * Mathf.Sin(t * 1.3f) * salla; break;   // esner: sırt geriye, ağır ağır
                // 2026-11-20: omuzlar kalçadan yarım, bir radyan geriden gelir (faz farkı): gövde tek parça sallanmaz, dalga gibi akar
                case "kivir": { float f = t * KIVIR_W; egil = 4; yat = 7 * Mathf.Sin(f - .5f) * salla; bur = 8 * Mathf.Sin(f - 1f) * salla; hop = .02f * Mathf.Abs(Mathf.Sin(f)); } break;
                case "disko": { float a = rm ? 1 : DiskoA(t - .05f); egil = Mathf.Lerp(6, -4, a); bur = Mathf.Lerp(12, -10, a); yat = 6 * (2 * a - 1); hop = .02f * Mathf.Abs(Mathf.Sin(t / DISKO_P * 2 * Mathf.PI)); } break;
                case "nanik": egil = 16 + 3 * Mathf.Sin(t * 8) * salla; yat = 7 * Mathf.Sin(t * 8) * salla; hop = .03f * Mathf.Abs(Mathf.Sin(t * 8)); break;   // rakibe eğilir, iki yana sallanır
                case "gelgel": { float c = rm ? .3f : GelC(t); egil = -5 - 2 * c; bur = 8; yat = 3; } break;              // geriye yaslanmış, sol omuz önde
                case "opucuk": { float a = rm ? 1 : OpA(t); egil = Mathf.Lerp(8, -6, a); bur = Mathf.Lerp(6, -10, a); } break;   // öperken eğilir, yollarken açılır
                case "cikcik": egil = -4; bur = 5; yat = 2 * Mathf.Sin(t * 9 - .9f) * salla; break;
            }
        }
        const float YILDIZ_P = .5f;
        readonly Quaternion[] sevKayit = new Quaternion[8]; readonly Transform[] sevKem = new Transform[8];
        // Tek zincirin kendi ağırlığı: çözülen kemik, SevincPoz'un başında kaydedilen boşta dönüşüyle karışır (kayıt sırası: sevKem).
        void Kat(int i, Transform b, float k) { if (b != null && k < .999f) b.localRotation = Quaternion.Slerp(sevKayit[i], b.localRotation, k); }
        void SevincPoz(string id, float t, float w, bool rm)
        {
            if (kKol == null) ZincirKur(); if (kKol == null || w <= 0.001f) return;
            Transform SU = kKol[3], SE = kKol[4], SH = kKol[5], LU = kKol[0], LE = kKol[1], LH = kKol[2];
            Vector3 up = yaw.up, ri = yaw.right, fw = yaw.forward;
            float salla = rm ? 0 : 1;
            // 2026-11-19 (yasin: "sevinçlerde animasyon kalitesini artıralım, odun gibi hareket etmesin"): yumuşak giriş ve canlılık
            // katmanı. (1) Ağırlık smoothstep'le girer (doğrusal girişte kollar bir anda kalkıyordu). (2) Omurga eğilmesi tek göğüs
            // kemiği yerine bel ve göğüs arasında paylaşılır (%40 / %60): gövde kırık değil kavisli eğilir. (3) Baş geriden gelir
            // (follow-through): kök eğrisi 0,08 sn geriden de hesaplanır, fark başa ters yönde verilir; sıçrayışta baş hafif
            // gecikir, dönüşte bakış arkada kalır. (4) Sıçrayıştan inişte dizler kısa bir an yaylanır (iniş sönümü). Hepsi poz
            // fonksiyonu: saklı durum yok, Dolap önizlemesi ve maç aynı kareyi verir. Hareketi azaltta (salla = 0) hiçbiri.
            float we = w * w * (3 - 2 * w); w = we;
            float hop, don, egil, bur, yat, orbit; KokEgri(id, t, rm, salla, out hop, out don, out egil, out bur, out yat, out orbit);
            float hopG, donG, egilG, burG, yatG, orbitG; KokEgri(id, Mathf.Max(0, t - BAS_GECIKME), rm, salla, out hopG, out donG, out egilG, out burG, out yatG, out orbitG);
            if (orbit != 0 && salla > 0) transform.position += (ri * (Mathf.Cos(orbit) - 1) + fw * Mathf.Sin(orbit)) * UCAK_R * w;   // uçak: küçük daire
            transform.position += up * hop * w * salla;
            if (don != 0 && salla > 0) yaw.localRotation = Quaternion.AngleAxis(don, Vector3.up) * yaw.localRotation;
            // Bacak katmanı (2026-11-06, öneri artifact'i "sevinç" YAP; yasin: "maç sonundaki sevinç kısmı sorunlu"): kollar
            // boşta pozunun (maç duruşu, dizler bükük) üstüne kalkıyordu, her sevinç "çömelmiş teslim" gibi okunuyordu.
            // Leğen, iki bacağın da %97 boyuna erişeceği kadar dikey kalkar; ayaklar yerinde kalır (iki kemik IK, diz öne).
            // Zaferde hazırlık çömelmesi ve inişte dizden yaylanma bu katmanın ağırlığıyla verilir.
            float inis = hop < .04f ? Mathf.Clamp01((hopG - hop) * 10) * salla : 0;   // sıçrayıştan inerken dizler bir an yaylanır
            SevincBacak(SevincDik(id, t, rm) * (1 - .22f * inis) * w, fw, KalcaKay(id, t, rm) * w);
            // kayıt (yerel dönüşler): IK tam ağırlıkla çözülür, sonra kayıtla harmanlanır
            var kem = sevKem; kem[0] = SU; kem[1] = SE; kem[2] = LU; kem[3] = LE; kem[4] = kOmurga.Count > 0 ? kOmurga[0] : null; kem[5] = SH; kem[6] = kOmurga.Count > 1 ? kOmurga[1] : null; kem[7] = kHead;   // kare başı ayırma yok; el: RaketDik ağırlıkla karışsın
            for (int i = 0; i < kem.Length; i++) if (kem[i] != null) sevKayit[i] = kem[i].localRotation;
            if ((egil != 0 || bur != 0 || yat != 0) && kOmurga.Count > 0)
            {   // bel %40, göğüs %60 (tek kemik varsa hepsi göğüste): kavisli eğilme
                float pay = kOmurga.Count > 1 ? .4f : 0;
                if (pay > 0) { var bl = kOmurga[1]; bl.rotation = Quaternion.AngleAxis(bur * pay, up) * Quaternion.AngleAxis(egil * pay, ri) * Quaternion.AngleAxis(yat * pay, fw) * bl.rotation; }
                var sp = kOmurga[0]; float q = 1 - pay; sp.rotation = Quaternion.AngleAxis(bur * q, up) * Quaternion.AngleAxis(egil * q, ri) * Quaternion.AngleAxis(yat * q, fw) * sp.rotation;
            }
            float L = SU != null && SE != null && SH != null ? (SE.position - SU.position).magnitude + (SH.position - SE.position).magnitude : .5f;
            Vector3 sR = SU != null ? SU.position : Vector3.zero, sL = LU != null ? LU.position : Vector3.zero;
            float s = Mathf.Sin(t * 14) * salla;
            switch (id)
            {
                case "yumruk":   // raketli el yukarı itilir (iki kez), sol yumruk göğüste
                    IK(SU, SE, SH, sR + up * L * (.78f + .1f * Mathf.Max(0, s)) + ri * L * .12f + fw * L * .18f, ri - fw * .4f);
                    IK(LU, LE, LH, sL + fw * L * .4f - up * L * .25f + ri * L * .3f, -up - ri * .3f); break;
                case "zafer":
                    {   // hazırlıkta kollar aşağı-geri (raket kalça yanında), sıçrayışta ikisi birden yukarı fırlar (raket dimdik),
                        // sonra raket havada kalırken sol yumruk ve raket karşılıklı pompalanır (iki vuruş/sn).
                        Vector3 rAlt = sR - up * L * .55f + ri * L * .3f - fw * L * .15f, lAlt = sL - up * L * .55f - ri * L * .3f - fw * L * .15f;
                        Vector3 rUst = sR + up * L * .93f + ri * L * .3f + fw * L * .04f, lUst = sL + up * L * .93f - ri * L * .3f + fw * L * .06f;
                        Vector3 rH, lH;
                        if (rm) { rH = rUst; lH = lUst; }
                        else if (t < Z_HAZIR) { float a = Mathf.SmoothStep(0, 1, t / Z_HAZIR); rH = Vector3.Lerp(sR + up * L * .1f + ri * L * .3f + fw * L * .2f, rAlt, a); lH = Vector3.Lerp(sL + up * L * .1f - ri * L * .3f + fw * L * .2f, lAlt, a); }
                        else if (t < Z_DONGU) { float a = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - Z_HAZIR) / .2f)); rH = Vector3.Lerp(rAlt, rUst, a); lH = Vector3.Lerp(lAlt, lUst, a); }
                        else
                        {
                            float p = Mathf.Sin((t - Z_DONGU) * Mathf.PI * 2 * Z_POMPA * .5f);
                            rH = sR + up * L * (.78f + .17f * Mathf.Max(0, p)) + ri * L * .26f + fw * L * .06f;
                            lH = sL + up * L * (.5f + .38f * Mathf.Max(0, -p)) - ri * L * .3f + fw * L * (.22f - .14f * Mathf.Max(0, -p));
                        }
                        IK(SU, SE, SH, rH, ri - fw * .3f); IK(LU, LE, LH, lH, -ri - fw * .3f);
                        if (rm || t >= Z_HAZIR) RaketDik(SH, up + ri * .12f);
                    } break;
                case "kollar": case "zipla":   // iki kol tam açık V (2026-11-06: eller başın yanında kalıyordu; hedef kol boyunun %96'sı yukarı)
                    IK(SU, SE, SH, sR + up * L * .9f + ri * L * .34f + fw * L * .06f, ri - fw * .3f);
                    IK(LU, LE, LH, sL + up * L * .9f - ri * L * .34f + fw * L * .06f, -ri - fw * .3f);
                    RaketDik(SH, up + ri * .15f); break;
                case "el_salla": case "selam":   // sol el yukarıda sallanır, raket yanda; selam: raket de hafif kalkar
                    IK(LU, LE, LH, sL + up * L * .8f - ri * L * (.3f + .16f * Mathf.Sin(t * 10) * salla) + fw * L * .12f, -ri - fw * .3f);
                    if (id == "selam") IK(SU, SE, SH, sR + up * L * .55f + ri * L * .45f + fw * L * .15f, ri - fw * .3f); break;
                case "alkis":   // sol el raketin teline vurur (göğüs önünde)
                    { Vector3 orta = (sR + sL) * .5f + fw * L * .55f - up * L * .1f; float ac = L * (.1f + .1f * (1 + s) * .5f);
                      IK(SU, SE, SH, orta + ri * ac, -up + ri); IK(LU, LE, LH, orta - ri * ac, -up - ri); } break;
                case "egil":     // reverans: raketli kol göğse, sol kol yanda
                    IK(SU, SE, SH, (sR + sL) * .5f + fw * L * .3f - up * L * .15f, -up + ri); break;
                case "raket_cevir":   // afiş: raket göğüs önünde, bilek etrafında bir tur döner (0,9 sn), sonra dik
                    IK(SU, SE, SH, sR + fw * L * .55f + ri * L * .22f - up * L * .05f, ri - up * .5f);
                    RaketDik(SH, Quaternion.AngleAxis(rm ? 0 : 360 * Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .9f)), fw) * (up + ri * .2f)); break;
                case "omuz":          // afiş (Smaşçı): raket omuzda, kafası geride
                    IK(SU, SE, SH, sR + up * L * .12f + fw * L * .18f + ri * L * .04f, ri - fw * .2f - up * .4f);
                    RaketDik(SH, up * .6f - fw + ri * .15f); break;
                case "kavus":         // afiş (Duvar): kollar göğüste kavuşuk, raket aşağı
                    { Vector3 g = (sR + sL) * .5f + fw * L * .32f - up * L * .3f;
                      IK(SU, SE, SH, g - ri * L * .1f, -up + fw * .2f); IK(LU, LE, LH, g + ri * L * .1f + up * L * .04f, -up + fw * .2f); } break;
                case "robot":   // dirsekler dik açıda: bir el yukarı, öbürü aşağı; her vuruşta kesik kesik yer değişir
                    { float k = rm ? 1 : Mathf.Cos(Kademe(t * 3.2f) * Mathf.PI);
                      IK(SU, SE, SH, sR + ri * L * .5f + fw * L * .12f + up * L * .42f * k, ri - fw * .2f);
                      IK(LU, LE, LH, sL - ri * L * .5f + fw * L * .12f - up * L * .42f * k, -ri - fw * .2f);
                      RaketDik(SH, Vector3.Slerp(fw + up * .2f, up, (k + 1) * .5f)); } break;
                case "tavuk":   // eller koltuk altında, dirsekler kanat gibi çırpar; raket kuyruk gibi yana-geri
                    { float f = Mathf.Sin(t * 12) * salla;
                      IK(SU, SE, SH, sR + fw * L * .22f - up * L * .2f + ri * L * .1f, ri + up * (.1f + .7f * f) - fw * .2f);
                      IK(LU, LE, LH, sL + fw * L * .22f - up * L * .2f - ri * L * .1f, -ri + up * (.1f + .7f * f) - fw * .2f);
                      RaketDik(SH, ri * .8f + up * .3f - fw * .2f); } break;
                case "twist":   // bel sağa sola burulurken bükük kollar ters yöne gider
                    { float q = Mathf.Sin(t * 9) * salla;
                      IK(SU, SE, SH, sR + fw * L * (.35f + .25f * q) - up * L * .45f + ri * L * .25f, ri - fw * .5f);
                      IK(LU, LE, LH, sL + fw * L * (.35f - .25f * q) - up * L * .45f - ri * L * .25f, -ri - fw * .5f);
                      RaketDik(SH, up + fw * .4f); } break;
                case "dalga":   // kollar yana açık; dalga raketli elden sol ele geçer (faz farkı)
                    { float hr = Mathf.Sin(t * 6) * salla, hl = Mathf.Sin(t * 6 - 1.6f) * salla;
                      IK(SU, SE, SH, sR + ri * L * .85f + up * L * .35f * hr + fw * L * .1f, ri * .3f - up * hr - fw * .3f);
                      IK(LU, LE, LH, sL - ri * L * .85f + up * L * .35f * hl + fw * L * .1f, -ri * .3f - up * hl - fw * .3f);
                      RaketDik(SH, ri + up * hr * .6f); } break;
                case "helikopter":   // raket başın üstünde yatay döner (saniyede iki tur), sol el belde
                    IK(SU, SE, SH, sR + up * L * .9f + ri * L * .1f + fw * L * .04f, ri - fw * .3f);
                    IK(LU, LE, LH, sL - up * L * .72f - ri * L * .28f + fw * L * .06f, -ri - fw * .6f);
                    RaketDik(SH, rm ? up : Quaternion.AngleAxis(t * 720, up) * (ri + up * .15f)); break;
                case "gitar":   // raket gitar: sap sağ kalçada, kafası sol omza doğru; sol el "perdede" gidip gelir, baş ritimle öne
                    { Vector3 nd = (-ri * .9f + up * .5f + fw * .2f).normalized;
                      IK(SU, SE, SH, sR - up * L * .7f + fw * L * .3f + ri * L * .05f, ri - fw * .5f);
                      RaketDik(SH, nd);
                      if (SH != null) IK(LU, LE, LH, SH.position + nd * L * (.5f + .12f * Mathf.Sin(t * 5) * salla) + fw * L * .05f, -ri - up * .5f); } break;
                case "dinlen":   // avuçlar dizin hemen üstünde (uyluk), raket sağ elde ucu yere dönük
                    { float a = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .5f)); if (rm) a = 1;
                      var kR = sR + fw * L * .45f - up * L * .86f + ri * L * .02f; var kL = sL + fw * L * .45f - up * L * .86f - ri * L * .02f;   // omuz ve kol boyuna göre: her tipte (tombul/uzun) aynı yere
                      IK(SU, SE, SH, Vector3.Lerp(SH != null ? SH.position : kR, kR, a), ri * .6f + up * .2f - fw * .4f);
                      IK(LU, LE, LH, Vector3.Lerp(LH != null ? LH.position : kL, kL, a), -ri * .6f + up * .2f - fw * .4f);
                      RaketDik(SH, Vector3.Slerp(up, -up * .7f + fw * .5f + ri * .2f, a)); } break;
                case "bel":      // eller kalçada, dirsekler dışa; raket sapı elde, kafası aşağı-arkaya
                    { float a = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .5f)); if (rm) a = 1;
                      var hR = sR - up * L * .62f + ri * L * .14f - fw * L * .04f; var hL = sL - up * L * .62f - ri * L * .14f - fw * L * .04f;
                      IK(SU, SE, SH, Vector3.Lerp(SH != null ? SH.position : hR, hR, a), ri - fw * .5f);
                      IK(LU, LE, LH, Vector3.Lerp(LH != null ? LH.position : hL, hL, a), -ri - fw * .5f);
                      RaketDik(SH, Vector3.Slerp(up, -up - fw * .4f + ri * .3f, a)); } break;
                case "ter":      // sol ön kol alından geçer (sağdan sola siler); raket yana iner
                    // 2026-11-19 (yasin: "kalas gibi... anatomik olarak akıcı"): yalnız kol oynuyor, poz 0,15 sn'de doğrusal giriyordu.
                    // Şimdi evreli: raketli kol önce gevşer (kR), sol kol dirsekten kalkar (kL), baş öne eğilip ön kola doğru döner
                    // (el sola giderken baş sağa: alın kolun altından kayar), bilek geriden gelir, kol inerken gövde geri açılır.
                    // Kollar tam çözülüp boşta pozuyla DÖNÜŞ uzayında karışır (Kat): el düz çizgide değil yay çizerek kalkar.
                    // r198/r200 notları geçerli: hedef baş kemiğine göre alın; raket −up·0,2'den fazla inmez (zemin).
                    { float kL = Zarf(t, 0, .2f, .84f, 1.05f), kR = Zarf(t, 0, .28f, .66f, 1f), a = rm ? .5f : Evre(t, .3f, .7f);
                      // r203: çıkışta kol dönüş karışımıyla yana açılarak iniyordu (el sallar gibi). El artık hedefle taşınır: göğüs
                      // önünden kalkar, sildikten sonra yüzün önünden göğse iner; karışım yalnız en başta ve en sonda.
                      float in_ = (1 - Evre(t, .04f, .32f)) + Evre(t, .68f, .98f);
                      Vector3 alin = (sR + sL) * .5f + up * L * .4f + fw * L * .3f;
                      if (kHead != null)
                      { Quaternion hq = Quaternion.AngleAxis(Mathf.Lerp(-8, 12, a) * kL * w, up) * Quaternion.AngleAxis(10 * kL * w, ri) * Quaternion.AngleAxis(4 * kL * w, fw);
                        kHead.rotation = hq * kHead.rotation; alin = kHead.position + hq * (up * L * .75f + fw * L * .55f); }
                      IK(SU, SE, SH, sR + ri * L * .25f - up * L * .6f + fw * L * (.2f + .04f * Mathf.Sin(t * 5) * salla), ri - fw * .3f); RaketDik(SH, fw * .85f - up * .2f + ri * .25f);
                      IK(LU, LE, LH, alin + ri * L * (.28f - .56f * a) + up * L * .04f * Mathf.Sin(a * Mathf.PI) - (up * L * .95f + fw * L * .1f) * in_, -ri * .7f + fw * .5f + up * .25f * (1 - in_));
                      if (LH != null) LH.rotation = Quaternion.AngleAxis(-20 * Mathf.Sin(a * Mathf.PI) * kL * w * salla, fw) * LH.rotation;   // bilek geriden gelir
                      Kat(0, SU, kR); Kat(1, SE, kR); Kat(5, SH, kR); Kat(2, LU, kL); Kat(3, LE, kL); } break;
                case "tel":      // raket gövdenin önünde çapraz, kafası sol omzun önünde; sol elin parmakları telleri iki kez tarar
                    // 2026-11-19: 13 rad/sn titreme yerine iki yumuşak tarama; raket kendi ekseninde hafif döner (bakıyor), baş
                    // tellere eğilir. r198 notu geçerli: sol elin hedefi gerçek kafa konumundan (model oranı değişse de el telde).
                    { float k = Zarf(t, 0, .3f, .74f, 1.05f), p = rm ? .25f : Mathf.Clamp01((t - .3f) / .44f);
                      if (kHead != null) kHead.rotation = Quaternion.AngleAxis(-14 * k * w, up) * Quaternion.AngleAxis(18 * k * w, ri) * kHead.rotation;
                      IK(SU, SE, SH, sR + fw * L * .45f - up * L * .55f + ri * L * .15f, ri - up * .5f);
                      if (SH != null && kafa != null)
                      { // r204: kafa yüz hizasındaydı, önden bakınca el yüze dokunuyor gibi okunuyordu. Kafa sol omzun önüne ve dışına indi
                        // (raket boyu ≈ 1,6 kol; kafa merkezi elden ≈ 1,3 kol), el çemberin altına değil tellerin üstüne gider.
                        Vector3 nd = (sL + up * L * .2f + fw * L * .5f - ri * L * .25f - SH.position).normalized; RaketDik(SH, nd);
                        SH.rotation = Quaternion.AngleAxis(9 * Mathf.Sin(p * Mathf.PI * 2) * salla, nd) * SH.rotation;
                        Vector3 yan = Vector3.Cross(nd, fw).normalized;
                        IK(LU, LE, LH, Vector3.Lerp(SH.position, kafa.position, .88f + .12f * (.5f - .5f * Mathf.Cos(p * Mathf.PI * 4))) + yan * L * .05f * Mathf.Sin(p * Mathf.PI * 2) * salla - fw * L * .04f, -ri - up * .5f); }
                      Kat(0, SU, k); Kat(1, SE, k); Kat(5, SH, k); Kat(2, LU, k); Kat(3, LE, k); } break;
                case "fiskiye":   // sol el başın arkasında, raketli kol öne uzanır, raket ufka paralel (fıskiyenin borusu)
                    IK(SU, SE, SH, sR + fw * L * .85f + ri * L * .2f + up * L * .12f, ri - up * .4f);
                    if (kHead != null) IK(LU, LE, LH, kHead.position + up * L * .15f - fw * L * .22f - ri * L * .05f, -ri + up * .6f);
                    RaketDik(SH, fw + ri * .15f); break;
                case "ucak":   // kollar kanat gibi yana açık, dönüşe göre biri hafif yukarı biri aşağı
                    { float k = .12f * Mathf.Sin(t * 5) * salla;
                      IK(SU, SE, SH, sR + ri * L * .95f + up * L * (.08f + k) - fw * L * .05f, -up - fw * .3f);
                      IK(LU, LE, LH, sL - ri * L * .95f + up * L * (.08f - k) - fw * L * .05f, -up - fw * .3f);
                      RaketDik(SH, ri + up * .05f); } break;
                case "yildiz":   // tepede kollar V (raket dimdik), inişte yanlara kapanır
                    { float u = rm ? 1 : Mathf.Sin((t % YILDIZ_P) / YILDIZ_P * Mathf.PI);
                      IK(SU, SE, SH, sR + up * L * Mathf.Lerp(-.55f, .9f, u) + ri * L * Mathf.Lerp(.35f, .4f, u) + fw * L * .06f, ri - fw * .3f);
                      IK(LU, LE, LH, sL + up * L * Mathf.Lerp(-.55f, .9f, u) - ri * L * Mathf.Lerp(.35f, .4f, u) + fw * L * .06f, -ri - fw * .3f);
                      RaketDik(SH, Vector3.Slerp(-up + ri * .3f, up + ri * .15f, u)); } break;
                case "aglak":   // iki yumruk gözlerin önünde küçük daireler çizer (gözyaşı siler); raket yukarı-dışa, yüzü kesmez
                    { Vector3 g = kHead != null ? kHead.position + up * L * .38f + fw * L * .5f : (sR + sL) * .5f + up * L * .45f + fw * L * .3f;
                      float c = Mathf.Sin(t * 12) * salla, d = Mathf.Cos(t * 12) * salla;
                      IK(SU, SE, SH, g + ri * L * (.15f + .04f * c) + up * L * .03f * d, ri - up * .3f);
                      IK(LU, LE, LH, g - ri * L * (.15f + .04f * c) - up * L * .03f * d, -ri - up * .3f);
                      RaketDik(SH, up + ri * .6f); } break;
                case "kulak":   // sol el kulağın arkasında kepçe, baş sağa döner (sol kulak rakibe); raket kalçada, kafası aşağı-arkaya
                    if (kHead != null)
                    { kHead.rotation = Quaternion.AngleAxis(28 * w, up) * kHead.rotation;
                      IK(LU, LE, LH, kHead.position + up * L * .35f - ri * L * .42f + fw * L * .05f, -ri + up * .3f - fw * .3f); }
                    IK(SU, SE, SH, sR - up * L * .62f + ri * L * .14f - fw * L * .04f, ri - fw * .5f);
                    RaketDik(SH, -up - fw * .4f + ri * .3f); break;
                case "toz":     // sol el sağ omzun üstünü dışa doğru süpürür (hızlı gidiş, yavaş dönüş); raket yanda, yere değmez
                    { float p = rm ? .5f : (t * 2.2f) % 1, a = p < .35f ? Mathf.SmoothStep(0, 1, p / .35f) : 1 - Mathf.SmoothStep(0, 1, (p - .35f) / .65f);
                      IK(LU, LE, LH, sR + up * L * .12f + fw * L * .22f + ri * L * (-.12f + .26f * a), -ri - up * .5f + fw * .3f);
                      IK(SU, SE, SH, sR + ri * L * .25f - up * L * .6f + fw * L * .2f, ri - fw * .3f);
                      RaketDik(SH, fw * .85f - up * .2f + ri * .25f); } break;
                case "esne":    // sol el açık ağzı pat pat kapatır, raketli kol yukarı gerinir; baş geriye
                    { Vector3 agiz = kHead != null ? kHead.position + up * L * .2f + fw * L * .5f : (sR + sL) * .5f + up * L * .3f + fw * L * .3f;
                      if (kHead != null) kHead.rotation = Quaternion.AngleAxis(-14 * w, ri) * kHead.rotation;
                      IK(LU, LE, LH, agiz + fw * L * (.04f + .05f * Mathf.Max(0, Mathf.Sin(t * 9)) * salla), -ri - up * .4f);
                      IK(SU, SE, SH, sR + up * L * .85f + ri * L * .4f - fw * L * .1f, ri - fw * .3f);
                      RaketDik(SH, up + ri * .5f - fw * .2f); } break;
                case "kivir":   // yumruklar göğüs önünde birbirinin çevresinde döner (çark), çark kalçayı geriden izler; baş en son gelir
                    { float f = t * KIVIR_W, c = Mathf.Cos(t * 10) * salla, d = Mathf.Sin(t * 10) * salla;
                      Vector3 g = (sR + sL) * .5f + fw * L * .55f - up * L * .05f + ri * L * .1f * Mathf.Sin(f - .8f) * salla, cark = (up * c + fw * d) * L * .12f;
                      IK(SU, SE, SH, g + ri * L * .13f + cark, ri - up * .6f);
                      IK(LU, LE, LH, g - ri * L * .13f - cark, -ri - up * .6f);
                      RaketDik(SH, up + ri * (.5f + .15f * c) - fw * .1f);
                      if (kHead != null) kHead.rotation = Quaternion.AngleAxis(6 * Mathf.Sin(f - 1.4f) * salla * w, fw) * kHead.rotation; } break;
                case "disko":   // sol el sağ alttan sol üste yay çizerek işaret eder, kalça ters yana çıkar, baş eli geriden izler; raketli el belde
                    { float a = rm ? 1 : DiskoA(t), ag = rm ? 1 : DiskoA(t - BAS_GECIKME);
                      Vector3 yon = Vector3.Slerp((-up * .45f + ri * .55f + fw * .5f).normalized, (up * .85f - ri * .5f + fw * .12f).normalized, a);
                      IK(LU, LE, LH, sL + yon * L * .95f, -ri - up * .3f - fw * .3f);
                      IK(SU, SE, SH, sR - up * L * .55f + ri * L * .22f + fw * L * .05f, ri - fw * .6f);
                      RaketDik(SH, ri * .6f - up * .35f - fw * .6f);
                      if (kHead != null) kHead.rotation = Quaternion.AngleAxis(Mathf.Lerp(16, -20, ag) * w, up) * Quaternion.AngleAxis(Mathf.Lerp(10, -12, ag) * w, ri) * kHead.rotation; } break;
                case "nanik":   // iki el kulak hizasında açık, sırayla çırpar; baş iki yana yatar, çene rakibe kalkık; dil dışarıda (MimikPoz)
                    if (kHead != null)
                    { float f = t * 8, c = Mathf.Sin(t * 16) * salla;
                      kHead.rotation = Quaternion.AngleAxis(10 * Mathf.Sin(f - .6f) * salla * w, fw) * Quaternion.AngleAxis(-8 * w, ri) * kHead.rotation;
                      // r215: el baş kemiğine göre konunca maskotların iri kafasına giriyor, parmaklar yüzden çıkıyordu. Eller baş
                      // küresinin (mimik çerçevesi: merkez ve yarıçap) dışında, kulak hizasında durur.
                      float hr = BasR(); Vector3 g = hr > 0 ? mimik.kok.position - up * hr * .2f + fw * hr * .25f : kHead.position + up * L * .24f + fw * L * .12f;
                      float ac = hr > 0 ? Mathf.Max(L * .44f, hr * 1.32f) : L * .46f;
                      IK(SU, SE, SH, g + ri * ac + up * L * .05f * c, ri - fw * .25f + up * .1f);
                      IK(LU, LE, LH, g - ri * ac - up * L * .05f * c, -ri - fw * .25f + up * .1f);
                      RaketDik(SH, up * (.7f + .2f * c) + ri); } break;
                case "gelgel":  // sol kol rakibe uzanır, ön kol kendine çağırır (yay); çene her çağrıda kalkar; raket omuzda
                    { float c = rm ? .3f : GelC(t), cg = rm ? .3f : GelC(t - BAS_GECIKME);
                      Vector3 yon = Vector3.Slerp((fw + up * .05f - ri * .08f).normalized, (fw * .55f + up * .8f - ri * .05f).normalized, c);
                      IK(LU, LE, LH, sL + yon * L * Mathf.Lerp(.92f, .62f, c), -up - ri * .25f);
                      IK(SU, SE, SH, sR + up * L * .12f + fw * L * .18f + ri * L * .04f, ri - fw * .2f - up * .4f);
                      RaketDik(SH, up * .6f - fw + ri * .15f);
                      if (kHead != null) kHead.rotation = Quaternion.AngleAxis(5 * w, fw) * Quaternion.AngleAxis((-5 - 9 * cg) * w, ri) * kHead.rotation; } break;
                case "opucuk":  // sol el dudağa gider, sonra kol açılarak öpücüğü rakibe yollar; baş eli geriden izler; raket yanda
                    { float a = rm ? 1 : OpA(t), ag = rm ? 1 : OpA(t - BAS_GECIKME);
                      // r215: el dudak yerine göz hizasına gidiyordu; ağız yeri mimik çerçevesinden (modelin gerçek ağzı), elin payıyla önünde
                      float hr = BasR(); Vector3 agiz = hr > 0 ? mimik.kok.TransformPoint(mimik.agiz) + fw * hr * .45f - up * hr * .8f   /* r216: hedef bilek; parmaklar gözü kapatıyordu, bilek çene altına */ : kHead != null ? kHead.position + up * L * .08f + fw * L * .5f : (sR + sL) * .5f + up * L * .3f + fw * L * .3f;
                      Vector3 dis = sL + fw * L * .82f + up * L * .38f - ri * L * .38f;
                      IK(LU, LE, LH, sL + Vector3.Slerp(agiz - sL, dis - sL, a), -ri - up * .5f);
                      IK(SU, SE, SH, sR - up * L * .6f + ri * L * .22f - fw * L * .04f, ri - fw * .5f);
                      RaketDik(SH, ri * .5f - up * .5f + fw * .3f);
                      if (kHead != null) kHead.rotation = Quaternion.AngleAxis(-12 * ag * w, up) * Quaternion.AngleAxis(Mathf.Lerp(6, -8, ag) * w, ri) * kHead.rotation; } break;
                case "cikcik":  // sol ön kol dik, el iki yana "cık cık" sallar; baş geriden ters yöne hayır der; raketli el belde
                    { float f = t * 9;
                      IK(LU, LE, LH, sL + fw * L * .5f + up * L * .2f + ri * L * (-.1f + .13f * Mathf.Sin(f) * salla), -up - ri * .2f);   // r215: el yüzün önünü kapatıyordu, omuz hizasına ve yana alındı
                      IK(SU, SE, SH, sR - up * L * .55f + ri * L * .22f + fw * L * .05f, ri - fw * .6f);
                      RaketDik(SH, ri * .6f - up * .35f - fw * .6f);
                      if (kHead != null) kHead.rotation = Quaternion.AngleAxis(-10 * Mathf.Sin(f - .5f) * salla * w, up) * Quaternion.AngleAxis(-5 * w, ri) * kHead.rotation; } break;
                case "iyi_oyun": // raket rakibe doğru kalkar (fileden selam)
                    IK(SU, SE, SH, sR + fw * L * .7f + up * L * .45f + ri * L * .1f, ri - up * .3f); break;
            }
            // Baş geriden gelir (follow-through): kök eğrisinin 0,08 sn önceki hâliyle farkı başa ters verilir, ±18° ile sınırlı.
            if (kHead != null && salla > 0)
            {
                float bp = Mathf.Clamp((egilG - egil) * .8f + (hop - hopG) * 120, -18, 18), by = Mathf.Clamp((burG - bur) * .6f + (donG - don) * .25f, -18, 18), br = Mathf.Clamp((yatG - yat) * .8f, -12, 12);
                kHead.rotation = Quaternion.AngleAxis(by, up) * Quaternion.AngleAxis(bp, ri) * Quaternion.AngleAxis(br, fw) * kHead.rotation;
            }
            if (w < .999f) for (int i = 0; i < kem.Length; i++) if (kem[i] != null) kem[i].localRotation = Quaternion.Slerp(sevKayit[i], kem[i].localRotation, w);
            MimikPoz(id, t, w, rm);
        }
        // ---- Mimik (2026-11-20, yasin: "kışkırtıcı mimikler eklenebilir") ----
        // Modellerin yüzü sabit ağ (kemik ya da biçim anahtarı yok; eklemek on modeli yeniden dışa aktarmak ve boyutu büyütmek
        // demekti). Mimik, baş çerçevesine takılan üç küçük parçayla verilir: dil (dışarı uzar, iki yana oynar) ve iki kaş
        // (kalkar, çatılır, biri kalkık kalır). Parçalar model kurulurken bir kez yaratılır (Aksesuar.MimikKur), sevinç dışında
        // kapalıdır: maçta ek çizim yok, kare başına ayırma yok. Sevinç bitince LateUpdate kapatır (poz fonksiyonu çağrılmayan kare).
        Aksesuar.MimikSet mimik; int mimKare = -1;
        // dil 0–1, dilYan °, kalk/tek baş yarıçapı oranı (tek = yalnız sol kaş), kiz ° (+ çatık V, − üzgün ∧)
        static bool MimikEgri(string id, float t, bool rm, out float dil, out float dilYan, out float kalk, out float tek, out float kiz)
        {
            dil = 0; dilYan = 0; kalk = 0; tek = 0; kiz = 0; float salla = rm ? 0 : 1;
            switch (id)
            {
                case "nanik": dil = 1; dilYan = 16 * Mathf.Sin(t * 16) * salla; kalk = .12f; return true;
                case "gelgel": case "kulak": tek = .16f; kiz = 8; return true;             // alaycı: tek kaş kalkık
                case "cikcik": case "toz": kiz = 18; kalk = -.04f; return true;            // çatık, kendinden emin
                case "aglak": kiz = -20; kalk = .05f; return true;                         // yalancı üzüntü
                case "kivir": case "disko": kalk = .06f + .05f * Mathf.Sin(t * KIVIR_W) * salla; return true;
                case "opucuk": kalk = .1f * (rm ? 1 : OpA(t)); return true;
            }
            return false;
        }
        // Baş küresinin dünya yarıçapı (mimik çerçevesi yoksa 0): el hedefleri iri kafalı modellerde başın dışında kalsın diye.
        float BasR() { return mimik != null && mimik.kok != null ? mimik.kok.lossyScale.x : 0; }
        public string MimikTani { get { if (mimik == null || mimik.kok == null) return "mimik yok"; var k = mimik.kas[0]; return "kok " + mimik.kok.gameObject.activeSelf + " r " + BasR().ToString("F3") + " dil " + (mimik.dil != null && mimik.dil.gameObject.activeSelf) + " kas " + (k == null ? "yok" : k.gameObject.activeInHierarchy + " yer " + k.localPosition.ToString("F2") + " olcek " + k.lossyScale.ToString("F3")); } }
        void MimikPoz(string id, float t, float w, bool rm)
        {
            if (mimik == null || mimik.kok == null) return;
            float dil, dilYan, kalk, tek, kiz; if (!MimikEgri(id, t, rm, out dil, out dilYan, out kalk, out tek, out kiz)) return;
            mimKare = Time.frameCount; if (!mimik.kok.gameObject.activeSelf) mimik.kok.gameObject.SetActive(true);
            Aksesuar.MimikAyar(mimik, dil * w, dilYan, w, kalk, tek, kiz);
        }
        void LateUpdate() { if (mimik != null && mimik.kok != null && Time.frameCount > mimKare + 1 && mimik.kok.gameObject.activeSelf) mimik.kok.gameObject.SetActive(false); }

        // Kalça–omuz ayrışması (2026-10-27, ekran/kosu): hazırlıkta koşarken gövde fileye döner (Athlete.donus), bacaklar
        // gidişte kalır (Athlete.donusAyak, ayak işi klibi o çerçevede seçildi). Fark leğene verilir, omurganın ilk iki
        // kemiğinde geri alınır: göğüs, kollar ve raket vuruş klibinin pozunda, bacaklar gidiş yönünde. Ağırlık bacakların
        // ayak işinden gelen payı (ayak işi klibi gösteriliyorsa 1, vuruş klibinde alt katman ağırlığı). Omurga ya da leğen
        // bulunamazsa (ya da omurga leğenin çocuğu değilse) hiçbir şey yapılmaz (bütün beden dönerdi).
        public float TaniKalca;
        void KalcaDonus(Athlete o)
        {
            TaniKalca = 0;
            if (hakem || kHip == null || kOmurga.Count == 0 || !kOmurga[0].IsChildOf(kHip)) return;   // omurga leğenin altında değilse geri alınamaz
            float w = cur != null && Match.ADIM_BOY.ContainsKey(cur) ? 1 : altKlip != null ? altW : 0;
            float d = (o.donusAyak - o.donus) * w; if (Mathf.Abs(d) < .05f) return;
            Vector3 up = yaw.up; kHip.rotation = Quaternion.AngleAxis(d, up) * kHip.rotation;
            int n = Mathf.Min(2, kOmurga.Count); for (int i = 0; i < n; i++) kOmurga[i].rotation = Quaternion.AngleAxis(-d / n, up) * kOmurga[i].rotation;
            TaniKalca = d;
        }

        // Uzanma katmanı (2026-10-27 gece; koordinatör kararı, ekran turu ölçümü: temas karesinde raket kafası topa 0,6–1,5 m
        // uzak, "kırık" okunuyordu). Vuruş menzili Core'da (ReachInfo 1,15–1,45 m, hamle 1,75–2,2 m) klibin raket yayından geniş;
        // kural değişmez, kol yetişir. Sağ kol iki kemik IK (omuz → dirsek → bilek) + omurgada küçük eğim; el dünya dönüşünü
        // korur, böylece raket kafası elin ötelemesi kadar kayar ve temas noktasına KAFA oturur (el değil).
        //  - Temastan önce: hedef Core'un uçuş tahmini (icpt), kalan süre τ = prepT. Kayma = hedef − (vuruş karesindeki kafa,
        //    gövdenin τ sonraki tahmini konumunda); ağırlık son UZAN_GIR sn'de 0→1 (smoothstep). Geri salınım pozu bozulmaz,
        //    yalnız bütün kol hedefe doğru ötelenir. Tahmin geçerse (vuruş olmadı) UZAN_CIK içinde söner.
        //  - Temasta: hedef gerçek temas noktası (Athlete.tm*, StartFlight); ilk görünen karede kayma = hedef − kafa, sonra
        //    sabit kayma UZAN_CIK sn'de söner (takip klibin kendi yayıyla sürer). Servis: kayma yok (top elden, poz servis klibi).
        //  - Yetişmezse: kol boyunu aşan pay UZAN_PAY0'dan UZAN_PAY1'e dek ağırlığı sıfıra indirir (gergin kol takılıp kalmaz).
        // Durum yok sayılır: ağırlık τ'nun ve temastan geçen sürenin fonksiyonu; tek bellek temas karesinde ölçülen kayma.
        const float UZAN_GIR = .15f, UZAN_CIK = .1f, UZAN_EGIM = 20f, UZAN_EGIM_PAY = .6f, UZAN_PAY0 = .45f, UZAN_PAY1 = .8f,
            UZAN_KALCA = .45f, UZAN_KALCA_MAX = .2f, UZAN_BILEK = 40f, BILEK_SINIR = 32f;
        public float TaniBilek;
        Vector3? hazirKafa; int uzanN = -1; float uzanS0; Vector3 uzanD;
        readonly Dictionary<string, Vector3> vurusKafa = new Dictionary<string, Vector3>();
        public float TaniUzanW, TaniUzanOnce = -1, TaniUzanSonra = -1, TaniDirsek, TaniUzanPay;
        string Kim { get { return isMe ? "me" : "op"; } }
        bool UzanHazir(Athlete o, Match M)
        {
            return !hakem && kafa != null && o.sw <= 0 && !float.IsNaN(o.prepT) && o.prepT < UZAN_GIR && o.prepT > -UZAN_CIK
                && M.icpt != null && M.icpt.who == Kim && M.s != null && !M.s.dead && M.s.from != Kim && cur != null && Match.CLIP_HIT.ContainsKey(cur);
        }
        // Klibin vuruş karesinde raket kafasının yaw-yerel konumu (klip başına bir kez örneklenir; ardından ana örnekleme ezer).
        Vector3? KafaVurusta(string clip)
        {
            Vector3 v; if (vurusKafa.TryGetValue(clip, out v)) return v;
            float hit; if (anim == null || D(clip) == null || !Match.CLIP_HIT.TryGetValue(clip, out hit)) return null;
            Sample(clip, hit, null, 0, 1); UstKol(clip, hit, 1); v = yaw.InverseTransformPoint(kafa.position); vurusKafa[clip] = v; return v;
        }
        // Hazırlık tutuşu canlı (2026-10-27 gece, ölçüm: hazırlık pozu tek karede ~0,5 sn donuk, "takıldı" okunuyordu): tutuş
        // karesinde omurgada küçük nefes/yaylanma (öne-arkaya 1,5°, kupa burgusu 1°, 1,6 Hz). Genlik hazırlık ağırlığıyla gelir,
        // ileri salınım penceresinin (SALINIM) 0,1 sn öncesinden söner: salınım ve temas pozuna dokunmaz. Saat Core'dan
        // (M.clock): durum yok, 60/120 Hz aynı. Hareketi azaltta kapalı.
        void HazirNefes(Athlete o, Match M)
        {
            if (hakem || M.reduce || kOmurga.Count == 0 || o.sw > 0 || !(o.prepA > .08f) || o.prepClip == null || cur == null || !Match.CLIP_HIT.ContainsKey(cur)) return;
            float sal; if (!Match.SALINIM.TryGetValue(cur, out sal)) sal = Match.SALINIM_V;
            float g = float.IsNaN(o.prepT) ? 1 : Mathf.SmoothStep(0, 1, (o.prepT - sal - .05f) / .1f), A = Mathf.Min(1, o.prepA) * g; if (A < 1e-3f) return;
            float f = M.clock * 1.6f * 2 * Mathf.PI + (isMe ? 0 : 1.7f);
            var t = kOmurga[0]; t.rotation = Quaternion.AngleAxis(1.5f * A * Mathf.Sin(f), yaw.right) * Quaternion.AngleAxis(1f * A * Mathf.Sin(f * .5f), yaw.up) * t.rotation;
        }
        // 1. adım (ana örneklemenin hemen ardından, ayak kilidinden önce): kayma D ve ağırlık w; kalça kaymanın yatay payının
        // UZAN_KALCA'sı kadar (en çok UZAN_KALCA_MAX) hedefe, hedef alçaksa aşağı. Ayaklar AyakKilit'te kalça kaymasını geri
        // alan IK ile yerinde kalır (kalça önden gider, ayak basılı). Kol, omurga ve bilek 2. adımda (UzanKol).
        bool uzanVar; float uzanW; Vector3 uzanHedef, uzanKafaHedef, kalcaKay;
        void Uzan(Athlete o, Match M, float j)
        {
            uzanVar = false; kalcaKay = Vector3.zero;
            TaniUzanW = 0; TaniUzanOnce = TaniUzanSonra = -1; TaniDirsek = 0; TaniUzanPay = 0;
            if (hakem || kafa == null || model == null) return;
            if (kKol == null) ZincirKur();
            if (kKol[3] == null || kKol[4] == null || kKol[5] == null) return;
            Vector3 hedef, D; float w;
            bool servis = M.s != null && M.s.serve && M.s.from == Kim;
            if (o.sw > 0 && o.tmN > 0)
            {
                if (servis) return;
                float sig = (1 - o.sw) / Mathf.Max(.01f, o.swHiz); hedef = Kit.W(o.tmX, o.tmY, o.tmZ);
                if (o.tmN != uzanN) { uzanN = o.tmN; uzanS0 = sig; uzanD = hedef - kafa.position; TaniUzanOnce = uzanD.magnitude; }
                w = 1 - Mathf.SmoothStep(0, 1, (sig - uzanS0) / UZAN_CIK); D = uzanD;
            }
            else if (hazirKafa.HasValue && UzanHazir(o, M))
            {
                float tau = o.prepT; var I = M.icpt; hedef = Kit.W(I.x, I.y, I.z);
                w = tau >= 0 ? Mathf.SmoothStep(0, 1, 1 - tau / UZAN_GIR) : 1 - Mathf.SmoothStep(0, 1, -tau / UZAN_CIK);
                float t = Mathf.Max(0, tau);   // gövdenin temastaki tahmini yeri: yumuşatılmış hızla τ ileri
                Vector3 kayma = Kit.W(o.x + o.svx * t, o.y + o.svy * t, 0) + Vector3.up * j - transform.position;
                D = hedef - (yaw.TransformPoint(hazirKafa.Value) + kayma);
            }
            else return;
            if (w <= 1e-3f || D.sqrMagnitude < 1e-6f) return;
            uzanVar = true; uzanW = w; uzanHedef = hedef; uzanKafaHedef = kafa.position + D * w;
            if (kHip != null)
            {
                Vector3 y = new Vector3(D.x, 0, D.z) * (w * UZAN_KALCA); if (y.magnitude > UZAN_KALCA_MAX) y = y.normalized * UZAN_KALCA_MAX;
                kalcaKay = y + Vector3.up * Mathf.Max(-UZAN_KALCA_MAX, Mathf.Min(0, D.y * w * UZAN_KALCA));
                kHip.position += kalcaKay;
            }
        }
        // 2. adım: omurga eğimi (gereken açının UZAN_EGIM_PAY'ı, en çok UZAN_EGIM°), kol IK (el dünya dönüşünü korur), kalan
        // aralık için bilek: raket kafası el çevresinde hedefe döner (en çok UZAN_BILEK°; vuruşta bilek kırılması budur).
        void UzanKol()
        {
            if (!uzanVar) return;
            Transform S = kKol[3], E = kKol[4], H = kKol[5]; float w = uzanW; Vector3 kHedef = uzanKafaHedef;
            if (kOmurga.Count > 0)
            {
                Vector3 g = kOmurga[0].position, a = S.position - g, b = a + (kHedef - kafa.position); Vector3 ek = Vector3.Cross(a, b);
                float th = Mathf.Min(Vector3.Angle(a, b) * UZAN_EGIM_PAY, UZAN_EGIM);
                if (ek.sqrMagnitude > 1e-8f && th > .01f) { int n = Mathf.Min(2, kOmurga.Count); for (int i = 0; i < n; i++) kOmurga[i].rotation = Quaternion.AngleAxis(th / n, ek.normalized) * kOmurga[i].rotation; }
            }
            Vector3 H1 = H.position, Hd = H1 + (kHedef - kafa.position);
            float la = (E.position - S.position).magnitude, lb = (H.position - E.position).magnitude, pay = (Hd - S.position).magnitude - (la + lb);
            TaniUzanPay = pay;
            float f = pay > UZAN_PAY0 ? 1 - Mathf.SmoothStep(0, 1, (pay - UZAN_PAY0) / (UZAN_PAY1 - UZAN_PAY0)) : 1;   // yetişilmez: söner
            Hd = H1 + (Hd - H1) * f; kHedef = kafa.position + (kHedef - kafa.position) * f;
            Vector3 orta = (S.position + H1) * .5f, kutup = E.position - orta, ekO = (H1 - S.position).normalized;
            var rot = H.rotation; var elKlip = H.localRotation;
            IK(S, E, H, Hd, kutup.sqrMagnitude > 1e-6f ? kutup : -yaw.up);
            H.rotation = rot;
            {   // bilek
                Vector3 v = kafa.position - H.position, v2 = kHedef - H.position; Vector3 ek = Vector3.Cross(v, v2);
                float aci = Mathf.Min(Vector3.Angle(v, v2), UZAN_BILEK);
                if (ek.sqrMagnitude > 1e-8f && aci > .01f) H.rotation = Quaternion.AngleAxis(aci, ek.normalized) * H.rotation;
            }
            // Bilek sınırı (yasin 13 Kas: "raket sallama gerçekçi durmuyor, anatomik olarak"): el dünya dönüşünü koruyup kol
            // IK ile yer değiştirince el ön kola göre klipteki tutuştan sapıyordu; üstüne 40° bilek düzeltmesi binince bilek
            // kırık, raket ön kolun yanından "takılı" görünüyordu (gerçek bilek: bükülme ~70°, yana yalnız 20–30°). Elin ön kola
            // göre sapması klibin tutuşundan en çok BILEK_SINIR° olur; fazlası ön kolla birlikte döner. Kafa temasa birkaç cm daha
            // uzak kalabilir (raket yayı ve temas kuralı Core'da, değişmez); kırık bilekten iyidir.
            {
                float sap = Quaternion.Angle(elKlip, H.localRotation); TaniBilek = sap;
                if (sap > BILEK_SINIR) H.localRotation = Quaternion.Slerp(elKlip, H.localRotation, BILEK_SINIR / sap);
            }
            // Dirsek dönmesi: kutup yönünün IK öncesi ve sonrası (omuz–el eksenine dik düzlemde) açısı; 90°'den büyükse ters büküm.
            Vector3 ekS = (H.position - S.position).normalized, k1 = Vector3.ProjectOnPlane(E.position - (S.position + H.position) * .5f, ekS), k0p = Vector3.ProjectOnPlane(kutup, ekO);
            TaniDirsek = k1.sqrMagnitude > 1e-8f && k0p.sqrMagnitude > 1e-8f ? Vector3.Angle(k0p, k1) : 0;
            TaniUzanW = w * f; TaniUzanSonra = (uzanHedef - kafa.position).magnitude;
        }

        // Üstten vuruş kolu (yasin 13 Kas: "raket sallama gerçekçi durmuyor, anatomik olarak"). Klip şeridinde (r163
        // oyuncak_klip_clear) clear/smaç boyunca sağ el göğüs hizasında kalıyor, raket bilek çevresinde pervane gibi dönüyordu:
        // ortak iskelette pişen kol açıları tıknaz gövdede (kısa kol, büyük baş) eli omuzun üstüne çıkaramıyor. Gerçek üstten
        // vuruşta (clear, smaç, drop, sıçrayarak smaç, kafa arkası) dirsek omuz hizasına kalkar, el başın yanına/arkasına gider
        // (yay), temasta kol başın üstünde uzanır; serbest sol kol hazırlıkta topu gösterir, temasta aşağı çekilir (gövde döner).
        // Katman: el, sağ omuzdan kol boyuna oranlı hedefe iki kemik IK ile taşınır, dirsek dışa; el DÜNYA dönüşü korunur (raket
        // klibin yönünde kalır, temasta dik). Yalnız el hedefin altındaysa çalışır (kolu zaten yukarıda olan model etkilenmez).
        // Ağırlık temas kesrine göre: hazırlıkta açılır, temastan kısa süre sonra söner (takip klibin kendisi). Uzanma katmanı
        // bu pozdan ölçer (KafaVurusta da katmanı uygular), temas kuralı Core'da değişmez. Hareketi azalt: aynı (poz, hareket değil).
        static bool UstVurus(string c) { return c == "clear" || c == "smash" || c == "drop" || c == "jump" || c == "rth"; }
        public float TaniUstKol;
        void UstKol(string clip, float ph, float agirlik)
        {
            TaniUstKol = 0;
            if (hakem || clip == null || !UstVurus(clip) || anim == null) return;
            if (kKol == null) ZincirKur(); if (kKol == null) return;
            float hit; if (!Match.CLIP_HIT.TryGetValue(clip, out hit)) return;
            float w = Mathf.SmoothStep(0, 1, (ph - (hit - .4f)) / .25f) * (1 - Mathf.SmoothStep(0, 1, (ph - (hit + .06f)) / .22f)) * agirlik;
            if (w < .01f) return;
            Vector3 up = yaw.up, ri = yaw.right, fw = yaw.forward;
            Transform S = kKol[3], E = kKol[4], H = kKol[5];
            if (S != null && E != null && H != null)
            {
                float L = (E.position - S.position).magnitude + (H.position - E.position).magnitude;
                float s = Mathf.SmoothStep(0, 1, (ph - (hit - .2f)) / .2f);   // 0 = geri yay (el başın arkasında), 1 = temas (yukarıda uzanık)
                Vector3 geri = S.position + up * L * .5f + ri * L * .12f - fw * L * .12f, temas = S.position + up * L * .86f + ri * L * .1f + fw * L * .22f;
                Vector3 hedef = Vector3.Lerp(geri, temas, s), d = hedef - H.position;
                float alt = Vector3.Dot(d, up); float k = Mathf.Clamp01(alt / (.15f * L)) * w;   // el zaten hedef yüksekliğindeyse dokunma
                if (k > .01f)
                {
                    var rot = H.rotation;
                    IK(S, E, H, H.position + d * k, ri - fw * .2f);   // dirsek dışa (yana), hafif geride
                    H.rotation = rot; TaniUstKol = k;
                }
            }
            Transform LU = kKol[0], LE = kKol[1], LH = kKol[2];
            if (LU != null && LE != null && LH != null)
            {   // serbest kol: hazırlıkta topa doğru yukarı-öne, temasa yaklaşırken aşağı (gövdeye) çekilir
                float L = (LE.position - LU.position).magnitude + (LH.position - LE.position).magnitude;
                float g = w * (1 - Mathf.SmoothStep(0, 1, (ph - (hit - .12f)) / .14f));
                if (g > .01f) IK(LU, LE, LH, Vector3.Lerp(LH.position, LU.position + up * L * .62f + fw * L * .5f - ri * L * .08f, g), -ri - fw * .2f);
            }
        }

        // Kısa/flick servis duruşunda eller çeneden aşağıda (2026-10-28, ekran turu r44 model_mac_*): klipler ortak iskelette
        // göğüs hizasında tutuşla pişirilir; tıknaz oyuncakta (kısa gövde, büyük baş) aynı açılar sağ eli çeneye, raket sapını
        // yüzün önüne getiriyordu. El, sağ omuzdan SERVIS_EL_PAY (dünya m) aşağıya inene kadar iki el birlikte (tüy sol elde,
        // raket–top ilişkisi korunur) iki kemik IK ile indirilir; el dünya dönüşleri korunur. Gerçekçi ve Sade'de el zaten
        // aşağıda (inme 0). Yalnız görünüm: servis temas noktası ve süresi Core'da.
        const float SERVIS_EL_PAY = .12f; public float TaniServisIn;
        void ServisEller()
        {
            TaniServisIn = 0;
            if (hakem || kKol == null || (cur != "serve_bh" && cur != "serve_flick")) return;
            Transform rS = kKol[3], rE = kKol[4], rH = kKol[5]; if (rS == null || rE == null || rH == null) return;
            float d = rH.position.y - (rS.position.y - SERVIS_EL_PAY); if (d <= 1e-3f) return;
            Vector3 asagi = -yaw.up * d;
            for (int s = 1; s >= 0; s--)
            {
                Transform S = kKol[s * 3], E = kKol[s * 3 + 1], H = kKol[s * 3 + 2]; if (S == null || E == null || H == null) continue;
                Vector3 orta = (S.position + H.position) * .5f, kutup = E.position - orta; var rot = H.rotation;
                IK(S, E, H, H.position + asagi, kutup.sqrMagnitude > 1e-6f ? kutup : -yaw.up); H.rotation = rot;
            }
            TaniServisIn = d;
        }

        // İmza vuruşu hazırlığı (2026-10-26, Core/Imza; tasarım §3.3 kural 1): raket kafasında imzanın renginde ışık küresi +
        // nokta ışık (poz Match.Prep'te tutulur, ses Sfx.Imza'da: anlam yalnız renkle verilmez). Hareketi azaltta ışık sabit
        // kalır; parlama (ilk an büyüyüp küçülme) ve nabız kalkar. Mermi drive'da gövde 12 cm eğilir (yere eğilir).
        Transform imzaKure; Light imzaIsik; Material imzaMat; float imzaA, imzaYas; string imzaRenkId;
        // 2026-11-16 inceleme (fikir 22:05 #1): küre + toplamalı malzeme + nokta ışık ilk imza hazırlığında, ralli ortasında
        // kuruluyordu; Metal ışık geçişini ve malzemeyi o karede derleyip telgrafın ilk karelerini takıltıyordu. Patron rakipte
        // (GameRoot.Kur, perde kapalıyken) önceden kurulur ve birkaç kare neredeyse saydam çizilir (Fx.Isit deseni).
        int imzaIsitKare = -10;
        public void ImzaIsit()
        {
            if (hakem || imzaKure != null) return;
            ImzaKur(); imzaIsitKare = Time.frameCount; imzaKure.gameObject.SetActive(true);
            var c = Color.white; c.a = .004f; imzaMat.color = c; imzaIsik.color = Color.white; imzaIsik.intensity = .01f; imzaA = 0;
        }
        void ImzaKur()
        {
            imzaMat = Kit.Additive(Color.white);
            var g = Kit.Prim(PrimitiveType.Sphere, "imza_isik", kafa != null ? kafa : (model != null ? model : transform), kafa != null ? Vector3.zero : new Vector3(.35f, 1.5f, 0), Vector3.one * .32f, imzaMat);
            var col = g.GetComponent<Collider>(); if (col != null) Destroy(col);
            var r = g.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            imzaKure = g.transform; if (kafa != null) imzaKure.localScale = Vector3.one * (.32f / Mathf.Max(.01f, kafa.lossyScale.x));
            imzaIsik = g.AddComponent<Light>(); imzaIsik.type = LightType.Point; imzaIsik.range = 2.2f; imzaIsik.shadows = LightShadows.None;
        }
        void ImzaIsik(Athlete o, bool reduce, float dt)
        {
            bool on = !hakem && o.imzaT > 0 && o.imzaId != null;
            if (!on && imzaKure == null) return;
            if (imzaKure == null) ImzaKur();
            if (!on && Time.frameCount - imzaIsitKare <= 2) return;   // ısıtma çizimi birkaç kare sürsün
            if (on && imzaRenkId != o.imzaId)
            {
                imzaRenkId = o.imzaId; var b = KariyerVeri.Imza(o.imzaId); var c = Kit.Hex(b != null ? b.renk : "#ffffff"); imzaMat.color = c; imzaIsik.color = c; imzaYas = 0;
            }
            imzaA = Mathf.MoveTowards(imzaA, on ? 1 : 0, dt / .08f); imzaYas += dt;
            bool gor = imzaA > 0.001f; imzaKure.gameObject.SetActive(gor); if (!gor) { imzaRenkId = null; return; }
            float puls = reduce ? 1 : 1 + .18f * Mathf.Sin(imzaYas * 18) + Mathf.Max(0, .6f * (1 - imzaYas / .15f));   // nabız + ilk parlama
            var cc = imzaMat.color; cc.a = .9f * imzaA; imzaMat.color = cc;
            float s0 = kafa != null ? .32f / Mathf.Max(.01f, kafa.lossyScale.x) : .32f; imzaKure.localScale = Vector3.one * s0 * puls * imzaA;
            imzaIsik.intensity = 2.2f * imzaA * (reduce ? 1 : puls);
            if (o.imzaId == "mermi" && on) transform.position += Vector3.down * .12f * imzaA;
        }

        // Ayak kilidi (2026-10-26 gece, build 40 incelemesi; 3d-animator "pin the feet"). Faz yolla ilerlese de yerdeki ayak
        // kayıyordu: (1) kliplerde basan ayağın geriye gidişi döngü boyunca sabit değil ve adım ölçüsünden ~%10 kısa (Blender'da
        // FBX'ten ölçüm: fwd sol 0,81 / sağ 0,69 birim/döngü, json 0,82), (2) boşta, hazırlık ve vuruş pozları yerinde durur,
        // gövde ise 0,3–1 m/sn kayabilir, (3) klip geçişlerinde harmanlama ayağı sürükler. Unity izinde (ekran turu r31) ayak
        // işindeki yerdeki ayak gövde hızının %20–50'siyle kayıyordu. Çözüm, spor oyunlarının olağanı: ayak yere değdiği karede
        // dünya konumu kilitlenir, yerde kaldıkça iki kemik IK (uyluk–baldır) bileği orada tutar; ayak kalkınca ya da kilit
        // KILIT_MAX'tan fazla gerilirse (bacak gerilip diz kilitlenmesin) bırakılır ve fark KILIT_BIRAK hızıyla söner (pop yok).
        // Bileğin dünya dönüşü animasyondaki gibi kalır (taban açısı klipten). Yerde sayılma: bilek, boşta pozundaki
        // yüksekliğinin KILIT_Y kadar üstüne kadar. Havadayken (sıçrama, split hop) bilek yükseldiği için kendiliğinden kapalı.
        // Oyun kuralı değil; vuruş menzili Core'da. Kemikler bulunamazsa hiçbir şey yapılmaz.
        const float KILIT_Y = .025f, KILIT_MAX = .28f, KILIT_BIRAK = 18f;
        float[] bilek0; readonly Vector3[] kilitP = new Vector3[2], kilitO = new Vector3[2]; readonly bool[] kilitli = new bool[2];
        void AyakKilit(float dt)
        {
            if (hakem || model == null || kThigh.Count == 0) return;
            if (kBacak == null) ZincirKur();
            if (bilek0 == null) return;
            for (int i = 0; i < 2; i++)
            {
                Transform th = kBacak[i * 3], kn = kBacak[i * 3 + 1], ft = kBacak[i * 3 + 2]; if (th == null || kn == null || ft == null) continue;
                Vector3 A = ft.position - kalcaKay; bool yerde = A.y < bilek0[i] + KILIT_Y;   // A: kalça kaymasından önceki (klibin) ayak yeri
                if (yerde && !kilitli[i] && kilitO[i].sqrMagnitude < 1e-6f) { kilitli[i] = true; kilitP[i] = A; }
                else if (yerde && !kilitli[i]) { kilitli[i] = true; kilitP[i] = A + kilitO[i]; }   // bırakılış sönmeden yeniden bastı: kaldığı yerden
                Vector3 O = Vector3.zero;
                if (kilitli[i])
                {
                    O = new Vector3(kilitP[i].x - A.x, 0, kilitP[i].z - A.z);
                    // Gerilince bırakılır, fark söner; yarım metreden büyükse gövde ışınlandı (servis dizilişi): ayak hemen yerine.
                    if (!yerde || O.magnitude > KILIT_MAX) { kilitli[i] = false; kilitO[i] = O.magnitude > .5f ? Vector3.zero : O; O = kilitO[i]; }
                    else kilitO[i] = O;
                }
                else if (!kilitli[i]) { kilitO[i] *= Mathf.Exp(-dt * KILIT_BIRAK); if (kilitO[i].sqrMagnitude < 1e-6f) kilitO[i] = Vector3.zero; O = kilitO[i]; }
                if (O.sqrMagnitude < 1e-8f && kalcaKay.sqrMagnitude < 1e-8f) continue;
                var rot = ft.rotation; Vector3 kutup = kn.position - (th.position + A) * .5f;
                IK(th, kn, ft, A + O, kutup.sqrMagnitude > 1e-6f ? kutup : yaw.forward);
                ft.rotation = rot;
            }
        }
        public float TaniKilit { get { return (kilitli[0] ? 1 : 0) + (kilitli[1] ? 2 : 0); } }

        // Tanı (yalnız ekran turu, EkranTuru animasyon izi; 2026-10-26 gece): gösterilen klip, alt katman ağırlığı, iki ayak
        // bileğinin ve sağ elin dünya konumu, raket sapının ele uzaklığı. Ayak kayması = yerdeki ayağın kareler arası yatay
        // hızı; raket kayması = sapın el kemiğinden uzaklığı (rijit parça elle birlikte gitmeli). Oyunda çağıran yok.
        public string TaniKlip { get { return cur; } }
        // Klip geçişi tanısı (2026-10-27 performans turu 2; ekran turu "perf_gecis"): değişim, geçiş sürerken değişim, tek karede
        // düşen poz ağırlığının toplamı ve %25'i aşan düşüş (pop) sayısı. Oyunda okuyan yok.
        public int TaniDegisim, TaniAra, TaniPop; public float TaniDusen;
        public float TaniAlt { get { return altW; } }
        public float TaniFaz { get { return lastPh; } }
        public Vector3 TaniKafa { get { return kafa != null ? kafa.position : Vector3.zero; } }   // raket kafası (tel yüzeyinin merkezi)
        Transform raketSap;
        public bool TaniAyak(out Vector3 sol, out Vector3 sag, out Vector3 el, out float sapEl)
        {
            sol = sag = el = Vector3.zero; sapEl = -1;
            if (model == null || kThigh.Count == 0) return false;
            if (kBacak == null) ZincirKur();
            if (raketSap == null) foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("raket_sap")) { raketSap = t; break; }
            if (kBacak[2] == null || kBacak[5] == null) return false;
            sol = kBacak[2].position; sag = kBacak[5].position;
            var h = kKol[5]; if (h != null) { el = h.position; if (raketSap != null) { var r = raketSap.GetComponent<Renderer>(); var c = r != null ? r.bounds.ClosestPoint(h.position) : raketSap.position; sapEl = (c - h.position).magnitude; } }
            return true;
        }

        // File düzlemi (2026-10-26, yasin: "objelerde iç içe geçme olmasın"): Core gövde kökünü fileden .25 m uzak tutar
        // (Match.Engel), ama hamle/file pozlarında ön ayak kökten ~1,5 m öne uzanıyor (oyun ölçeğinde 1,75×) ve fileye
        // ~1,5 m'den yakın vuruşta ayak/diz file düzlemini geçiyordu. Gerçekte file düzlemini gövdeyle geçmek faul; burada
        // örneklenen pozun deri kemiklerinden (raket rijit parça, sayılmaz: takip vuruşunda raketin fileyi aşması serbest)
        // fileye en yakını bulunur ve figür PAY kadar geride kalacak biçimde tümden geri itilir. Kök Core'da kalır (vuruş
        // menzili değişmez); yalnız görüntü kayar ve kayma pozun en ileri anında ~0–20 cm'dir.
        const float FILE_PAY_G = .05f; Transform[] deriKemik;
        void FileItin()
        {
            if (hakem || model == null) return;
            if (deriKemik == null)
            {
                var hs = new HashSet<Transform>();
                foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) foreach (var b in r.bones) if (b != null) hs.Add(b);
                deriKemik = new Transform[hs.Count]; hs.CopyTo(deriKemik);
            }
            if (deriKemik.Length == 0) return;
            // dünya z = NET − y: ben z < 0 tarafındayım, rakip z > 0
            float uc = isMe ? -1e9f : 1e9f; foreach (var b in deriKemik) { float z = b.position.z; uc = isMe ? Mathf.Max(uc, z) : Mathf.Min(uc, z); }
            float asim = isMe ? uc + FILE_PAY_G : FILE_PAY_G - uc;   // > 0: düzlemi PAY içinde geçen kısım
            if (asim > 0) transform.position += new Vector3(0, 0, isMe ? -asim : asim);
        }
    }
}
