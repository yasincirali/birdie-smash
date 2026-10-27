// Doku içe aktarma ayarları (2026-10-27 performans turu 2, yasin: "Texturelar ve animasyonlar her yerde smooth ve performanslı
// akıcı olmalı"). Depoda hiç .meta yok (Unity her taze kopyada üretir, CI'da da): bir dokunun biçimi, boyu, mip'i ancak bir
// AssetPostprocessor'la sabitlenir; elle Inspector'da verilen ayar CI'ın taze kopyasına taşınmaz. KarakterImport'tan SONRA
// koşar (GetPostprocessOrder): o tür/sRGB kararını verir (normal haritası, doğrusal maske), burada yalnız platform biçimi,
// boy, süzme ve mip yazılır. CiBuild.iOS build'den önce Denetle() ile her dokuyu yeniden okur; ayar tutmayan varsa yeniden
// içe aktarır ve rapor satırı basar (Unity'nin postprocessor sürümünü bağımlılık sayıp saymadığına güvenmeden).
//
// Sınıflar (iOS; Android aynı ASTC, hat yok ama Editor'de platform değişince sürpriz olmasın):
//   karakter rengi (Resources/karakter/*: gövde/baş/forma atlası, saç): ASTC 6×6, ≤1024, mip açık, üç çizgili (trilinear).
//     Neden 6×6: Apple GPU'larının (A9+, iOS 15 alt sınırı) hepsi ASTC LDR çözer; 6×6 = 3,56 bit/piksel, RGBA32'nin ~1/9'u.
//     Figür kortta ~100 px boyunda, vitrinde (720 px RT) ~600 px: 1024 atlas vitrinde gerekli, kortta mip'ler devralır.
//     Trilinear: figür öne-arkaya koşarken mip sınırı karede bir atlıyordu (bilinear'da "pırpır"), maliyeti bir örnek daha.
//   normal haritası (*_n): ASTC 5×5 (normal bloklaşması 6×6'da yüzde görünür), ≤512. 1024 → 512: normal ayrıntısı en yakın
//     planda (vitrin Yüz, ~400 px baş) bile 512'nin altında; bellek ¼.
//   maske (*_mg, doğrusal, alfa = pürüzsüzlük): ASTC 6×6, ≤512 (KarakterImport da 512 der; burada platforma da yazılır).
//   arayüz (Resources/ dışında karakter olmayan görüntü: logo): ASTC 4×4, mip KAPALI (arayüz hep 1:1'e yakın çizilir, mip
//     yalnız bellek ve bulanıklık), NPOT ölçekleme YOK (logo 192 px: varsayılan "en yakın 2'nin kuvveti" 256'ya büyütüyor,
//     hem bulanık hem %78 fazla bellek), kenar sıkıştırma (clamp).
//   uygulama ikonu (BirdieSmash/Icon): sıkıştırmasız, mip yok: PlayerSettings ikon boylarını bundan üretir; sıkıştırılmış
//     kaynak ikonu bloklu yapardı.
// Hepsinde Read/Write kapalı (CPU kopyası yok; kod GetPixel ile okumuyor) ve sıkıştırma kalitesi Normal.
// FBX'e gömülü atlas (oyuncak*.fbx içindeki 512'lik govde.png) modelin alt varlığıdır, bu sınıflara girmez: PlayerView
// Resources/karakter/<model>_govde dokusunu bağlıyor; gömülü kopya yalnız modelle birlikte yüklenir (açık soru, raporda).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BirdieSmash.EditorTools
{
    public class DokuAyar : AssetPostprocessor
    {
        public override int GetPostprocessOrder() { return 100; }   // KarakterImport (0) tür/sRGB'yi yazdıktan sonra
        public override uint GetVersion() { return 1; }             // değişince Unity dokuları yeniden içe aktarır

        public struct Hedef { public string sinif; public TextureImporterFormat bicim; public int boy; public bool mip; public FilterMode suzme; public int aniso; public bool npotYok; public bool sikistirmasiz; }

        // Yol → hedef ayar; Assets dışı ya da bilinmeyen (null) dokuya dokunulmaz.
        public static Hedef? Sinif(string yol)
        {
            var p = yol.Replace('\\', '/'); string ad = Path.GetFileNameWithoutExtension(p);
            if (!p.StartsWith("Assets/")) return null;
            if (p.Contains("/BirdieSmash/Icon/")) return new Hedef { sinif = "ikon", bicim = TextureImporterFormat.RGBA32, boy = 1024, mip = false, suzme = FilterMode.Bilinear, npotYok = true, sikistirmasiz = true };
            if (p.Contains("Resources/karakter/"))
            {
                if (ad.EndsWith("_n")) return new Hedef { sinif = "normal", bicim = TextureImporterFormat.ASTC_5x5, boy = 512, mip = true, suzme = FilterMode.Trilinear, aniso = 1 };
                if (ad.EndsWith("_mg")) return new Hedef { sinif = "maske", bicim = TextureImporterFormat.ASTC_6x6, boy = 512, mip = true, suzme = FilterMode.Trilinear, aniso = 1 };
                return new Hedef { sinif = "karakter", bicim = TextureImporterFormat.ASTC_6x6, boy = 1024, mip = true, suzme = FilterMode.Trilinear, aniso = 1 };
            }
            return new Hedef { sinif = "arayuz", bicim = TextureImporterFormat.ASTC_4x4, boy = 1024, mip = false, suzme = FilterMode.Bilinear, npotYok = true };
        }

        void OnPreprocessTexture()
        {
            var h = Sinif(assetPath); if (h == null) return;
            Uygula((TextureImporter)assetImporter, h.Value);
        }

        // true: bir şey değişti (Denetle yeniden içe aktarır).
        public static bool Uygula(TextureImporter ti, Hedef h)
        {
            bool d = false;
            if (ti.isReadable) { ti.isReadable = false; d = true; }
            if (ti.mipmapEnabled != h.mip) { ti.mipmapEnabled = h.mip; d = true; }
            if (ti.filterMode != h.suzme) { ti.filterMode = h.suzme; d = true; }
            if (ti.anisoLevel != h.aniso && h.aniso > 0) { ti.anisoLevel = h.aniso; d = true; }
            if (ti.maxTextureSize > h.boy) { ti.maxTextureSize = h.boy; d = true; }
            if (h.npotYok && ti.npotScale != TextureImporterNPOTScale.None) { ti.npotScale = TextureImporterNPOTScale.None; d = true; }
            if (h.sinif == "arayuz" && ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; d = true; }
            if (h.sikistirmasiz && ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; d = true; }
            foreach (var plat in new[] { "iPhone", "Android" })
            {
                var s = ti.GetPlatformTextureSettings(plat);
                if (s.overridden && s.format == h.bicim && s.maxTextureSize == h.boy && s.compressionQuality == 50) continue;
                s.overridden = true; s.format = h.bicim; s.maxTextureSize = h.boy; s.compressionQuality = 50;
                s.textureCompression = h.sikistirmasiz ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
                ti.SetPlatformTextureSettings(s); d = true;
            }
            return d;
        }

        // CiBuild.iOS'tan, build'den önce: her doku hedefinde mi; değilse yeniden içe aktarılır. Satırlar build günlüğüne
        // (Actions loguna) düşer: "doku <yol> <sınıf> <en>x<boy> → iOS <biçim> ≤<boy> mip <..> · tahmini <KB>".
        [MenuItem("Birdie Smash/Doku ayarlarını denetle")]
        public static void Denetle()
        {
            try { DenetleIc(); } catch (System.Exception e) { Debug.LogWarning("Birdie Smash doku denetimi atlandı: " + e.Message); }   // build'i kırmaz
        }
        static void DenetleIc()
        {
            long once = 0, sonra = 0; int duzelen = 0; var satir = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid); var ti = AssetImporter.GetAtPath(yol) as TextureImporter; if (ti == null) continue;
                var h = Sinif(yol); if (h == null) continue;
                int w, hh; KaynakBoy(ti, yol, out w, out hh);
                if (Uygula(ti, h.Value)) { ti.SaveAndReimport(); duzelen++; }
                long a = Bayt(w, hh, 32, 1, 1, true, false), b = Tahmin(w, hh, h.Value);   // önce: RGBA32 + mip (Unity'nin otomatik biçimi bilinmiyorsa en kötü durum)
                once += a; sonra += b;
                satir.Add("doku " + yol + " " + h.Value.sinif + " " + w + "x" + hh + " → iOS " + h.Value.bicim + " ≤" + h.Value.boy + " mip " + (h.Value.mip ? "açık" : "kapalı") + " · tahmini " + (b / 1024) + " KB");
            }
            foreach (var s in satir) Debug.Log("Birdie Smash " + s);
            Debug.Log("Birdie Smash doku denetimi: " + satir.Count + " doku, " + duzelen + " yeniden içe aktarıldı · GPU belleği (hepsi yüklü olsa) RGBA32 " + (once / 1024) + " KB → " + (sonra / 1024) + " KB");
        }
        // Kaynak boy: TextureImporter.GetSourceTextureWidthAndHeight (2021.2+; bulut derleme denetimi 2021.1 Editor DLL'iyle
        // koştuğu için yansımayla), yoksa içe aktarılmış dokunun boyu.
        static void KaynakBoy(TextureImporter ti, string yol, out int w, out int h)
        {
            w = h = 0;
            try
            {
                var m = typeof(TextureImporter).GetMethod("GetSourceTextureWidthAndHeight", new[] { typeof(int).MakeByRefType(), typeof(int).MakeByRefType() });
                if (m != null) { var a = new object[] { 0, 0 }; m.Invoke(ti, a); w = (int)a[0]; h = (int)a[1]; }
            }
            catch (System.Exception) { w = h = 0; }   // denetim build'i asla kırmaz
            if (w <= 0) { var t = AssetDatabase.LoadAssetAtPath<Texture2D>(yol); if (t != null) { w = t.width; h = t.height; } }
        }
        static long Tahmin(int w, int h, Hedef t)
        {
            float k = Mathf.Min(1f, (float)t.boy / Mathf.Max(w, h)); w = Mathf.Max(1, Mathf.RoundToInt(w * k)); h = Mathf.Max(1, Mathf.RoundToInt(h * k));
            switch (t.bicim)
            {
                case TextureImporterFormat.ASTC_4x4: return Bayt(w, h, 0, 4, 4, t.mip, true);
                case TextureImporterFormat.ASTC_5x5: return Bayt(w, h, 0, 5, 5, t.mip, true);
                case TextureImporterFormat.ASTC_6x6: return Bayt(w, h, 0, 6, 6, t.mip, true);
                default: return Bayt(w, h, 32, 1, 1, t.mip, false);
            }
        }
        // ASTC: her blok 16 bayt; mip zinciri 1×1'e kadar.
        static long Bayt(int w, int h, int bit, int bx, int by, bool mip, bool astc)
        {
            long top = 0;
            while (true)
            {
                top += astc ? (long)((w + bx - 1) / bx) * ((h + by - 1) / by) * 16 : (long)w * h * bit / 8;
                if (!mip || (w == 1 && h == 1)) break; w = Mathf.Max(1, w / 2); h = Mathf.Max(1, h / 2);
            }
            return top;
        }
    }
}
