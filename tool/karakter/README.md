# Karakter sprite üretimi

Oyuncu figürü Blender'da (bpy, `pip install bpy`) parçalı gövde + iki-kemik IK ile kurulur; eklem
konumları hedef olarak verilir, uzuv boyları hiç bozulmaz. 10 klip × 2 bakış render edilir,
`sheet.py` sprite sheet + maske (R forma, G raket çerçevesi, B kafa bandı; ayrı bileklik) +
`spr.js` (base64) üretir; `spr.js` oyun dosyasındaki `SPR_META`/`SPR_SRC` bloğunun yerine geçer.

```bash
python3 -I karakter.py -- /tmp/bs full      # ~3 dk CPU (keys: yalnız anahtar pozlar, önizleme)
python3 -I sheet.py /tmp/bs                # klip başına renk/maske/bileklik PNG → spr.js
```

Arcade görünüm (2026-10-23, yasin "Sportif arcade"): `BS_STIL=sportif BS_OLCEK=2 python3 -I karakter.py -- /tmp/gA full`
(büyük kafa/el/ayak, cel gölge, dış çizgi, 2× hücre). Yüz çizgileri (kaş, kirpik, ağız) gölgelendiricide `yuzuv`
koordinatından çizilir (`insan.py` `_yuz_dugum`). 360° ve hakemler aynı `BS_STIL` ile `tur` / `hakem` modlarında.

## Vuruş biyomekaniği turu (2026-10-24, `arastirma/animasyon_denetimi_2026-10-24.md` #6–#9, #12–#15)

- **Kare ızgarası:** `sample()` anahtar zamanlarını kare ızgarasına oturtur; her anahtar pozu bir karede birebir çizilir.
  Klip `hit`/`hold`/`drop` kare numaralarını taşır ve `meta.json` klip kaydına yazılır (oyunun `CLIP_HIT`,
  `PREP_KARE` değerleri buradan: `CLIP_HIT = (hit+0,05)/n`, `PREP_KARE = hold`).
- **Temasa hızlanarak:** temasa giren parça u², çıkan parça 1-(1-u)²; diğerleri kosinüs. `drop` klibi `yavas=True`.
- **Raket düşüşü:** `S_DROP` (kupa `S_PREP` → düşüş → temas). smash/clear/jump/drop 14 kare, aynı ızgara
  (0 hazır, 2 kupa = bekleme, 5 düşüş, 9 temas); ilk 6 kare dördünde aynı (aldatma).
- **Kalça önden:** pozda `pelvis` (twist işaretiyle); yoksa eski kural twist·0,35. `lead=1` leğeni bekleme karesinden
  sonra bir kare önden okur.
- **Ön kol burulması:** `burgu=1` olan pozda `rkFace` avuç yönüdür; `insan.on_kol_burgu` üst kol/ön kol/el arasında
  paylaştırır, nötr = dirsek bükme düzlemine dik avuç, ±100°. `burgu` yoksa eski görünüm birebir (360° verisi değişmez).
- **Dirsek:** `bukuk(poz, açı)` bilek hedefini kol boyunun içine çeker (kill 120°, net/alçak/hamle 150°).
- `rk` ve `rkFace` küre üstünde karışır (slerp); ters yönlü anahtarların ortası artık sıfırdan geçmez.
- Yeni klipler `drop` (clear gövdesi, yavaşlayan temas, kısa takip) ve `net` (low gövdesi, takipsiz) listenin sonunda.

## İki model (2026-10-24, yasin: "iki modeli de seçenek sun, defaultta gerçekçi olmayan olsun")

- **Oyuncak 3B** (varsayılan, 2026-10-26 yeniden tasarım; yasin: "fiziksel olarak gerçekçiye yakınsat, modellerimiz iyi
  görünmeli"): `oyuncak.py`. KayKit chibi'nin iri başı yüksek kameradan gövdeyi örtüyordu; şimdi ~7,3 baş boyunda atletik
  gövde (MakeHuman CC0 taban ağ + makro hedefler: kas .78, kilo .36; baş ×1.07, göz bölgesi ×1.24, el ×1.06 — stilize
  ama oransal). Kıyafet ayrı ağ değil, deri ağın kendisi alanlarla kesilip (yaka V'si, raglan biye, kol/etek/şort/çorap
  çizgisi) bölge başına normal yönünde kalınlaştırılır ve sınır basamağı duvar yüzlerle köprülenir: kıyafet deriyle aynı
  ağırlıkları taşır, smaç/hamlede ayrılmaz, yırtılmaz. Ayrı parçalar: göz (sklera/iris/bebek + ışık), kaş/kirpik şeridi,
  saç kabuğu (saç çizgisi alanıyla kesik, perçem + tutam), bant ve bileklik halkası, dışbükey zarflı spor ayakkabı +
  iki katlı taban (beyaz ara taban, kırmızı dış taban). İskelet UE adlı (Calibrate'in aradığı head/pelvis/upperarm/
  foot/ball/thigh/calf), parmaklar 3 boğum, iki elde (2026-10-26 tutuş turu; aşağıda). Ball kemiği taban altından 0,02 birim
  yukarıda (PlayerView.Zemin 3,5 cm sözleşmesi). Ayarlar `BS_OY_*` ortam değişkenleri.
  `BS_MODEL=oyuncak python3 -I tool/karakter/unity_fbx.py -- /tmp/oy oyuncak` → `oyuncak.fbx` + `oyuncak_clips.json`.
  - **Performans bütçesi** (yasin: "her telefonda akıcı"; ekranda 4 figür): gövde ~7,3–7,4k üçgen (govde+forma+bant+bilek; raket ayrıca ~1k),
    seçili saç (0,5–1,7k) + ayakkabı çifti (1,2–1,8k) ile ekrandaki en ağır figür ~10,7k; 53 kemik (iki elde 5×3 parmak:
    tutuş ve servis kıskacı kemikle okunsun diye), FBX ~2,9 MB (`BS_OY_ANIMSADE` 0,01 anahtar sadeleştirmesi; kayıpsız, 6,3 →
    2,9 MB). Sabit renkli her şey TEK derili ağ
    `oy_govde__govde` + TEK malzeme `oy_body` + 512 px renk atlası (`<model>_govde.png`; tip başına AYRI atlas, çünkü ten ve
    ayakkabı hücreleri tipe göre değişir; dışa aktarma klasörüne yazılır;
    Resources/karakter'e kopyalanır, PlayerView.DokuMalzeme "body" adlı malzemeye bağlar). Ayrı kalan yalnız Unity'nin boyadığı
    `oy_forma__forma`, `oy_bant__bant`, `oy_bilek__bilek` + seçili 1 saç (`oy_sac`) + 1 ayakkabı (`ayk_ust` + atlas):
    ekranda en çok 6 derili renderer. Şort atlasta lacivert (hakem rengiyle aynı).
  - **İç içe geçme çözücü** (`Carpisma`, `Oyuncak.coz`): her pozda derili ağ ölçülür; el/ön kol/üst kol gövde-baş-bacağa,
    baldır öbür bacağa, taban ve raket zemine, raket gövdeye girerse hedef itilir ve IK yeniden çözülür (bilek ≤7 cm, raket
    adımda ≤6°, toplam ≤25°). `ara_kare`: unity_fbx iki ara pozu da (mix) kurar, çözücü onlarda da çalışır (hızlı geçişte
    anahtarlar temizken arada ayak zemine 7 cm giriyordu). Ölçüt: her karede en küçük işaretli uzaklık (içeride < 0).
  - Raket ölçeği 0,8 (`BS_OY_RAKET`; 1,0 = oyunda 1,18 m). `insan.RAKET_LOD` raketin üçgenini düşürür (None = eski geometri).
- **Tutuş, tipler, saç, ayakkabı** (2026-10-26, yasin: "raket tutuşu net ve detaylı, 5 parmak badmintonun doğasındaki gibi
  ... tip seçebilme, garip saçlar ... ayakkabı seçebilecek"). Varlık sözleşmesi (C# tarafı buna göre):
  `/mnt/project-files/birdie-smash/komple-oyun/karakter_varlik_sozlesmesi.md`; tutuş kaynakları:
  `/mnt/project-files/birdie-smash/arastirma/tutus_notlari.md`.
  - **Parmaklar:** iki elde 5 parmak × 3 boğum (53 kemik). Uç boğumu orta boğuma katmak FBX'i ~1,9 MB küçültürdü ama
    yakın planda parmak ucu sapın üstünde düz dururdu (tutuşun okunduğu yer orası); bütçe ölçülüp karar verildi
    (sözleşme v1'deki boyutlar). El deri ağı sadeleştirmede korunur (`BS_OY_ELKORU`), parmaklar kıymıklaşmasın.
  - **Tutuş katmanı** `tutus.py`: klibe göre tutuş (el sıkışma / başparmak / panhandle / parmak), el yönü raket
    çerçevesinden, ön kol burulması + bilek sınırı; artakalan açı avuçta kaymaya bölünür: sapı eksen dışına eğme ≤20° (`BS_TT_KAYMA`),
    sap ekseni çevresinde parmaklarda çevirme ≤70° (`BS_TT_BURGUK`; oyuncular forehand↔backhand geçişinde sapı parmakta
    çevirir, eğmez). Bu da yetmezse el öbür tutuşa geçer (on↔bh, kil→bh; 0,35 rad ceza ki sebepsiz geçmesin) — yüksek
    backhand hazırlığında raket gövdeye 6–9 cm girmesin diye. Temas çevresinde (gerilim ≥ 0,9) klibin tutuşu kilitli:
    vuruşun okunduğu karede tutuş klibin adıyla tutarlı kalır. Parmaklar sapa ızgara eniyilemesiyle sarılır,
    deri ağıyla son denetim; gerilim temasa doğru sıkılaşır. Sol el serviste topu tüyünden çimdikler. Poz sözlükleri
    (karakter.py) değişmedi; `unity_fbx.py` klip/kare bilgisini `OY.klip_bilgi` ile verir.
  - **Tipler** (`BS_OY_TIP`: atlet, guclu, kadin, uzun; `TIPLER` sözlüğü): aynı iskelet adları ve klipler, MakeHuman
    makro hedefleri (cinsiyet, kas, kilo, boy oranı, etnik karışım) + omuz/kalça genişliği. Her tip kendi FBX'i
    (`oyuncak.fbx` = atlet, geriye uyum; `oyuncak_<tip>.fbx`), kendi atlası, `_clips.json`'da `tip` alanı.
  - **Saç** (`sac_ayk.py`, 11 biçim + kel): kafa derisinden kabuk (saç çizgisi alanla kesilir) + yordamsal parçalar
    (at kuyruğu tüpü, topuz elipsoidi, mohikan testere yelesi, anime dikenleri, örgü sıraları, dread tüpleri, mullet ense
    kanadı). Ağırlık en yakın 6 deri köşesinden (baş/boyun/üst sırt): at kuyruğu ucu sırtı izler. AO 14 ışınla pişip
    UV v'ye yazılır, u tel deseni; ortak doku `oyuncak_sac.png` (gri çarpan, Unity saç rengiyle çarpar). Saç oyunda
    görünen (sadeleştirilmiş) deriden en az 3,6 mm dışarıda (`aralik`, `aralik_son`; ince saçta ten lekesi deliyordu).
  - **Bant** deriye oturur (eskisi varsayılan saçın üstüne ölçülüyordu): her saç bandın altında `BANT_KAL`'a ezilir; ezme
    `BANT_SIK`'i (1,1 cm) aşan saçta bant gizlenir (`_clips.json` `sac[].bant=false`; ölçüm: yalnız afro).
  - **Ayakkabı** (5 çeşit): sayanın dışbükey zarfı (çeşide göre burun, en, hacim, bilekli yaka) + katmanlı taban (orta
    taban yan duvarı/taşması, karbon plaka). Taban altı hepsinde aynı: Zemin sözleşmesi değişmedi. Saya `ayk_ust` yuvası
    (Unity boyar), taban/topuk/şerit/bağcık atlasta (`oy_body`); bölgeler alanla kesilir (yüz seçimi seyrek ağda kamuflaj
    lekesi bırakıyordu).
  - **Çözücü** yalnız tipin varsayılan saçı ve klasik ayakkabıyla ölçer (afro her tipte kol yolunu iterdi); en hacimli
    saçla kalan girme denetimde raporlanır. Çeşitler render'da gizli (`BS_OY_SACSEC`, `BS_OY_AYKSEC` seçer),
    `unity_fbx.py` hepsini FBX'e koyar.
- **Oyuncak (KayKit, arşiv)**: `oyuncak_kaykit.py`, `BS_MODEL=oyuncak_kaykit`; KayKit Adventurers Rogue (CC0) gerekir.
- **Adım boyu** (2026-10-26, fizik denetimi): `unity_fbx.py` ayak işi döngülerinin (run, chasse_r/l, fwd, back_r/l) bir turda
  taşıdığı yolu basan ayaktan ölçüp `<model>_clips.json` `adim` alanına yazar (model birimi; Unity × SCALE ile ADIM_BOY'u ezer,
  ayak kaymaz). Koşu ve öne adımda ayak kaldırma evresi düzeltildi (geri vuruşun bir kısmı havadaydı); chassé alçak gövdeyle
  geniş. Hamle ön ayağı ~1,2 m, low/net bileği +10 cm, yorgunda raket dizde yatay. bh_over'da boş el sırtın arkasından gövdenin
  sol-önüne alındı (READY'ye dönüşte ön kol gövdeden geçiyordu), hazırlıkta raket eli çeneden 4 cm açıldı.
- **Gerçekçi**: Microsoft Rocketbox Sports_Male_02 (MIT, `aday.py`; FBX + dokular `tool/bulut_kurulum.sh` ile `~/AnimationLibrary/rocketbox`'a
  iner). Oyundaki blok adı `rocketbox`. 2026-10-26 revizyonu (yasin: "modellerimiz iyi görünmeli"):
  - kıyafet `rocketbox_kit.py`: futbol formasının dokusu UV adası + dinlenme z'si + baskın kemiğe göre badmintona boyanır
    (düz gri forma + koyu yaka/raglan/kol ağzı, lacivert şort, kısa beyaz çorap, beyaz kort ayakkabısı, kauçuk taban;
    krampon çivileri düz kalın tabana indirilir). Forma yüzleri `__forma` (oyun takım rengiyle çarpar);
  - normal + parlaklık haritaları (`rocketbox_govde_n.png`, `_mg.png` …; Unity tarafı için C# yaması gerekir, rapor);
  - mobil bütçe: 28 yüz kemiği başa katılır (52 kemik), köşe başına ≤4 ağırlık, FBX doku gömmez (4,5 MB), 8,1k üçgen gövde;
  - temas katmanı (`aday.Temas`): el/ön kol gövdeden, raket gövde/bacak/sol koldan dışarı, taban zemine; ortak pozlara dokunmaz.
  `BS_MODEL=aday python3 -I unity_fbx.py -- /tmp/rb rocketbox && python3 -I rocketbox_kit.py -- /tmp/rb` → Resources/karakter.
  `BS_ADAY=~/AnimationLibrary/rocketbox/Sports_Male_04/Export/Sports_Male_04.fbx` eski atletli modeli birebir kurar (HTML sprite bloğu).

```bash
BS_MODEL=oyuncak BS_OLCEK=2 python3 -I karakter.py -- /tmp/oy full && python3 -I sheet.py /tmp/oy
BS_MODEL=oyuncak python3 -I karakter.py -- /tmp/oy tur && python3 -I tur_js.py /tmp/oy
cd ../.. && python3 -I tool/karakter/gom.py oyuncak /tmp/oy/spr.js /tmp/oy/tur.js
```

- **Sade** (2026-10-26, yasin: oyuncağın iri başı yüksek kameradan gövdeyi örtüyordu, "normal oranlı, sade insan"):
  `sade.py`, Mesh2Motion mankeni + aynı paketin UE adlı iskeleti (CC0; `tool/bulut_kurulum.sh` → `~/AnimationLibrary/mesh2motion`,
  `BS_M2M`). İskelet mankene paketin kendi konumunda oturur; deri otomatik ağırlık (bone heat, ağırlıksız köşe 0). Ağ düzlemlerle
  kesilip düz renkli bölgelere ayrılır (forma, sort, ten, beyaz çorap, taban ayakkabı, sac kabuğu, bilek, bant). Basan taban her klipte
  0,4–0,5 cm (`BS_SADE_DZ`; koşunun z=0 bileği 0,05'e kırpılır, `BS_SADE_BILEK`). Uç (leaf) kemikleri silinir: FBX 5,2 MB.
  `BS_MODEL=sade python3 -I tool/karakter/unity_fbx.py -- /tmp/sd sade` → `sade.fbx` + `sade_clips.json` (Resources/karakter).

Oyunda her model ayrı `<script type="application/json" id="bs_model_<ad>">` bloğunda; oyun yalnız seçili olanı
ayrıştırır (`tuytop_model`, Ayarlar › Oyuncu görünümü). Hızlı önizleme: `stil` modu, 360° için `BS_TUR_N=8 BS_ONIZLEME=1`.
`aday.py`: hazır modelleri (Rocketbox, VRM) aynı pozlarla deneme (karşılaştırma sayfası için).

## Zemin ve Unity raketi (2026-10-25)

- `fwd` klibinde basan bilek z=0'dı (diğer ayak işi 0,05): Gerçekçi modelde ayak her fazda 3,8–5,7 cm gömülüyordu, şimdi en çok 1,4 cm.
- Oyuncak 3B'de taban her klipte 8–13 cm zeminin altındaydı (bilek hedefi bacak oranıyla küçülüyor, KayKit'in iri ayağında
  bilek tabandan 6,7 cm yukarıda; ayak baldırın eğimini devralıp burnu yere sokuyordu). `OyuncakIskelet`: bütün figür `dz`
  kadar yukarı, ayak dinlenme eğiminde düz basar; ayak işi kliplerinde taban 0,1–2,9 cm.
- `unity_fbx.py`: raket bir kez yerel çerçevede kurulur, etikete göre beş parça (`raket_kafa__cerceve`, `raket_tel__tel`,
  `raket_tel2__tel2`, `raket_sap__grip`, `raket_koyu__koyu`) el kemiğine bağlı `raket_pivot` altında; anahtarlanan yalnız pivot.
