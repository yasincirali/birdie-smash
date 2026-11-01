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
        // UI dinler: durum değişti, satın alma bitti (verilen Tüy; 0 = iptal/başarısız).
        public event Action Degisti; public event Action<int> Bitti;

        public void Baslat()
        {
            if (basladi && durum != Durum.Hata) return; basladi = true; durum = Durum.Yukleniyor; Degisti?.Invoke();
            try
            {
                var b = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (var p in TuyDukkan.PAKETLER) b.AddProduct(p.id, ProductType.Consumable);
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
            ctl.InitiatePurchase(p);
        }

        public void GeriYukle(Action<bool> sonuc)
        {
            Ayar.Yukle();   // iCloud kaydını yeniden okur (Bulut.Esitle Yukle'nin ilk adımı)
            var a = ext != null ? ext.GetExtension<IAppleExtensions>() : null;
            if (a == null) { sonuc?.Invoke(false); return; }
            a.RestoreTransactions((ok, msg) => sonuc?.Invoke(ok));
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions) { ctl = controller; ext = extensions; durum = Durum.Hazir; Degisti?.Invoke(); }
        public void OnInitializeFailed(InitializationFailureReason error) { durum = Durum.Hata; sonHata = error.ToString(); Degisti?.Invoke(); }
        public void OnInitializeFailed(InitializationFailureReason error, string message) { durum = Durum.Hata; sonHata = error + " " + message; Degisti?.Invoke(); }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            int n = TuyDukkan.Teslim(Ayar.gear, Ayar.Kayit, e.purchasedProduct.definition.id, e.purchasedProduct.transactionID);
            PlayerPrefs.Save(); Bulut.Yaz();
            Bitti?.Invoke(n);
            return PurchaseProcessingResult.Complete;
        }
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) { Bitti?.Invoke(0); }
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription d) { Bitti?.Invoke(0); }
    }
}
