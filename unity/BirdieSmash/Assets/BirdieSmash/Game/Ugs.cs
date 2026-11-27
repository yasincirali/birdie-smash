// Unity Gaming Services bağlantısı (2026-11-26, yasin: "arkadaş ekleme, davet, rank eşleşmesi olacak"; proje "birdie-smash").
// Bu sınıf yalnız anonim kimliği kurar (Relay'in şartı); maç taşıması UgsAg.cs. 2026-11-27: yasin TestFlight'ta "sadece kodla invite"
// dedi; ad kodlu arkadaş + davet iletisi kalktı (maça götürmüyordu, ayrıntı ArkadasUgs.cs). Game Center yolu (Online.cs)
// AYRI ve bağımsız kalır: yasin kartsız çalışmak istiyor, ücretsiz kota dolarsa UGS durur ama Game Center ile arkadaş maçı
// sürmeli. Bu yüzden her çağrı try/catch içinde, hata "Hata" bayrağını kaldırır ve arayüz UGS düğmelerini gizler; hiçbir
// hata oyuncuya fırlamaz. Bayrak kapalı: açılışta ağa hiç çıkılmaz, bugünkü davranış birebir.
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class Ugs
    {
        // Açık ama tembel: ağa yalnız Arkadaş sayfası açılınca çıkılır (açılışta değil), hata olursa o sayfa Game Center'a bakar. İlk TestFlight denemesi (2026-11-27) bunu görmek için.
        public static bool Bayrak { get { return true; } }

        public enum Durum { Kapali, Baglaniyor, Hazir, Hata }
        public static Durum durum = Durum.Kapali;
        public static string hataNotu;                 // yalnız tanı günlüğü için; oyuncuya gösterilmez

        // Anonim giriş. Ağ yoksa ya da kota bittiyse Hata'ya düşer; ikinci çağrı yeniden dener.
        // 2026-11-27 inceleme (fikir 02:06 #3): bağlanırken gelen ikinci çağrı eskiden hemen false dönüyordu; sayfa kapatıp açılınca
        // "hazırlanıyor" yazısı bağlantı bitse de güncellenmiyordu. Artık süren kurulum paylaşılır (ikinci çağıran aynı görevi bekler)
        // ve ağ asılırsa 10 sn sonra Hata'ya düşer (sayfa "Yeniden dene" gösterir). Zaman aşımından sonra biten iç görev zararsızdır:
        // durum Hata kalır, sonraki Baslat yeniden kurar.
        // İç kurulum tek görevdir: zaman aşımından sonra da sürer, "Yeniden dene" yeni bir kurulum başlatmaz, aynısını bekler
        // (üst üste SignIn/Initialize olmaz); bu arada bittiyse yeniden deneme hemen Hazir'a geçer.
        static Task<bool> suren; static Task icGorev;
        const int ZAMAN_ASIMI_MS = 10000;
        public static int surum;                        // durum değişince artar; açık sayfa bunu yoklayıp yalnız değişince yeniden kurar
        public static Task<bool> Baslat()
        {
            if (!Bayrak) return Task.FromResult(false);
            if (durum == Durum.Hazir) return Task.FromResult(true);
            if (suren == null || suren.IsCompleted) suren = Kur();
            return suren;
        }

        static async Task<bool> Kur()
        {
            durum = Durum.Baglaniyor; surum++;
            try
            {
                if (icGorev == null || icGorev.IsFaulted || icGorev.IsCanceled) icGorev = KurIc();
                var ic = icGorev;
                if (await Task.WhenAny(ic, Task.Delay(ZAMAN_ASIMI_MS)) != ic) throw new TimeoutException("UGS kurulumu 10 sn'de bitmedi");
                await ic;
                durum = Durum.Hazir; hataNotu = null; surum++; return true;
            }
            catch (Exception e) { durum = Durum.Hata; hataNotu = e.GetType().Name + ": " + e.Message; Debug.LogWarning("[Ugs] " + hataNotu); surum++; return false; }
        }

        static async Task KurIc()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
