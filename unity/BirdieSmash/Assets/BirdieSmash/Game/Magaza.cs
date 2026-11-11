// Mağaza köprüsü (2026-11-01): Unity IAP (com.unity.purchasing 4.x, StoreKit) ile Tüy paketleri. Teslim ve defter
// Core/TuyDukkan.cs'te; burası yalnız mağazayla konuşur. Kurallar:
//   · Ürün listesi mağazadan gelmeden (internet yok, ürünler App Store Connect'te henüz yok) dükkân "şu an açılamıyor" der,
//     fiyat uydurmaz. Fiyat her zaman mağazanın yerel fiyat metni (localizedPriceString).
//   · Teslim ProcessPurchase içinde, Tüy cüzdana ve deftere yazıldıktan sonra Complete döner; uygulama arada kapanırsa
//     StoreKit işlemi yeniden teslim eder, defter aynı kimliği ikinci kez saymaz.
//   · Teslimden hemen sonra kayıt buluta yazılır (Bulut.Yaz): uygulamayı silen oyuncu satın aldığını kaybetmez.
//   · Makbuz doğrulaması StoreKit'e bırakılır (sunucu yok, sır yok). Tüketilen ürünü geri yükleme yok; "Satın alımları geri
//     yükle" düğmesi App Store kuralı gereği yine var ve iCloud kaydını da yeniden okur.
using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Magaza : IDetailedStoreListener
    {
        public static readonly Magaza O = new Magaza();
        IStoreController ctl; IExtensionProvider ext; bool basladi;
        public enum Durum { Kapali, Yukleniyor, Hazir, Hata }
        public Durum durum = Durum.Kapali;
        public string sonHata;
        // 2026-11-11 inceleme: satın alma sürerken (App Store sayfası 1–3 sn'de açılır) dükkân sessizdi ve ikinci dokunuş Unity IAP'te
        // ExistingPurchasePending → OnPurchaseFailed → "tamamlanmadı, ücret alınmadı" yazdırıyordu; ilk satın alma sürerken yanlış
        // metin. Sürenin kimliği tutulur, UI düğmeleri pasifler; 60 sn'de kendiliğinden düşer (mağaza yanıt vermezse kilit kalmasın).
        string suren; float surenBas; bool geriYukleniyor;
        public bool Suruyor { get { return suren != null && Time.realtimeSinceStartup - surenBas < 60f; } }
        public bool SurenMi(string id) { return Suruyor && suren == id; }
        public bool GeriYukleniyor { get { return geriYukleniyor; } }
        // UI dinler: durum değişti, satın alma bitti (verilen Tüy; 0 = iptal/başarısız).
        public event Action Degisti; public event Action<int> Bitti; public event Action ReklamsizBitti;   // Reklamsız alındı ya da geri yüklendi

        public void Baslat()
        {
            if (basladi && durum != Durum.Hata) return; basladi = true; durum = Durum.Yukleniyor; Degisti?.Invoke();
            try
            {
                var b = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (var p in TuyDukkan.PAKETLER) b.AddProduct(p.id, ProductType.Consumable);
                b.AddProduct(TuyDukkan.REKLAMSIZ, ProductType.NonConsumable);   // 2026-11-08 Reklamsız (tek seferlik)
                UnityPurchasing.Initialize(this, b);
            }
            catch (Exception e) { durum = Durum.Hata; sonHata = e.Message; Degisti?.Invoke(); }
        }

        public string Fiyat(string id) { var p = ctl != null ? ctl.products.WithID(id) : null; return p != null && p.availableToPurchase ? p.metadata.localizedPriceString : null; }
        public bool Satilir(string id) { return Fiyat(id) != null; }

        public void Al(string id)
        {
            if (ctl == null) { Bitti?.Invoke(0); return; }
            var p = ctl.products.WithID(id); if (p == null || !p.availableToPurchase) { Bitti?.Invoke(0); return; }
            if (Suruyor || geriYukleniyor) return;   // ikinci dokunuş: önceki istek sürüyor, sessizce yok sayılır
            suren = id; surenBas = Time.realtimeSinceStartup; Degisti?.Invoke();
            ctl.InitiatePurchase(p);
        }

        public void GeriYukle(Action<bool> sonuc)
        {
            Ayar.Yukle();   // iCloud kaydını yeniden okur (Bulut.Esitle Yukle'nin ilk adımı)
            var a = ext != null ? ext.GetExtension<IAppleExtensions>() : null;
            if (a == null) { sonuc?.Invoke(false); return; }
            if (geriYukleniyor || Suruyor) return;
            geriYukleniyor = true; Degisti?.Invoke();
            a.RestoreTransactions((ok, msg) => { geriYukleniyor = false; sonuc?.Invoke(ok); });
        }

        // Makbuzda Reklamsız varsa (yeniden kurulum, başka cihaz) sahiplik yerele yazılır; geri yükle düğmesine basmak gerekmez.
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            ctl = controller; ext = extensions; durum = Durum.Hazir;
            var r = controller.products.WithID(TuyDukkan.REKLAMSIZ);
            if (r != null && r.hasReceipt && TuyDukkan.ReklamsizTeslim(Ayar.Kayit, r.transactionID)) { PlayerPrefs.Save(); Reklam.Yenile(); Bulut.Yaz(); }
            Degisti?.Invoke();
        }
        public bool ReklamsizVar { get { return !Reklam.ALAN; } }
        public void OnInitializeFailed(InitializationFailureReason error) { durum = Durum.Hata; sonHata = error.ToString(); Degisti?.Invoke(); }
        public void OnInitializeFailed(InitializationFailureReason error, string message) { durum = Durum.Hata; sonHata = error + " " + message; Degisti?.Invoke(); }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            string id = e.purchasedProduct.definition.id; suren = null;
            int n = TuyDukkan.Teslim(Ayar.gear, Ayar.Kayit, id, e.purchasedProduct.transactionID);
            PlayerPrefs.Save(); Bulut.Yaz();
            if (id == TuyDukkan.REKLAMSIZ) { Reklam.Yenile(); ReklamsizBitti?.Invoke(); return PurchaseProcessingResult.Complete; }
            Bitti?.Invoke(n);
            return PurchaseProcessingResult.Complete;
        }
        // Bekleyen satın alma (ExistingPurchasePending) başarısızlık değildir: ilki sürüyor, not yazılmaz, kilit kalır.
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) { Basarisiz(reason); }
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription d) { Basarisiz(d != null ? d.reason : PurchaseFailureReason.Unknown); }
        void Basarisiz(PurchaseFailureReason r)
        {
            if (r == PurchaseFailureReason.ExistingPurchasePending) return;
            suren = null; Bitti?.Invoke(0);
        }
    }
}
