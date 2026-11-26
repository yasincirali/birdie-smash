// Unity Gaming Services bağlantısı (2026-11-26, yasin: "arkadaş ekleme, davet, rank eşleşmesi olacak"; proje "birdie-smash").
// İlk dilim yalnız kimlik + arkadaşlar + davet iletisi; maç taşıması (Relay/Lobby) sonraki dilim. Game Center yolu (Online.cs)
// AYRI ve bağımsız kalır: yasin kartsız çalışmak istiyor, ücretsiz kota dolarsa UGS durur ama Game Center ile arkadaş maçı
// sürmeli. Bu yüzden her çağrı try/catch içinde, hata "Hata" bayrağını kaldırır ve arayüz UGS düğmelerini gizler; hiçbir
// hata oyuncuya fırlamaz. Bayrak kapalı: açılışta ağa hiç çıkılmaz, bugünkü davranış birebir.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Friends;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class Ugs
    {
        // Açık ama tembel: ağa yalnız Arkadaş sayfası açılınca çıkılır (açılışta değil), hata olursa o sayfa Game Center'a bakar. İlk TestFlight denemesi (2026-11-27) bunu görmek için.
        public static bool Bayrak { get { return true; } }

        public enum Durum { Kapali, Baglaniyor, Hazir, Hata }
        public static Durum durum = Durum.Kapali;
        public static string adKodu;                   // "Ad#1234": arkadaşların seni bununla ekler
        public static readonly List<string> bekleyenDavet = new List<string>();   // davet gönderen UGS kimlikleri (sayfa gösterir)
        public static string hataNotu;                 // yalnız tanı günlüğü için; oyuncuya gösterilmez
        public struct Arkadas { public string id, ad; }
        public static event Action<string> DavetGeldi; // gönderenin UGS kimliği

        const string DAVET_ON = "birdie-davet";
        // Friends iletisi parametresiz kurucusu olan bir tür ister (string olmaz).
        public class Iletisi { public string tur; }
        static bool dinleniyor;

        // Anonim giriş + Friends. Ağ yoksa ya da kota bittiyse Hata'ya düşer; ikinci çağrı yeniden dener.
        // 2026-11-27 inceleme (fikir 02:06 #3): bağlanırken gelen ikinci çağrı eskiden hemen false dönüyordu; sayfa kapatıp açılınca
        // "hazırlanıyor" yazısı bağlantı bitse de güncellenmiyordu. Artık süren kurulum paylaşılır (ikinci çağıran aynı görevi bekler)
        // ve ağ asılırsa 10 sn sonra Hata'ya düşer (sayfa "Yeniden dene" gösterir). Zaman aşımından sonra biten iç görev zararsızdır:
        // durum Hata kalır, sonraki Baslat yeniden kurar.
        // İç kurulum tek görevdir: zaman aşımından sonra da sürer, "Yeniden dene" yeni bir kurulum başlatmaz, aynısını bekler
        // (üst üste SignIn/Initialize olmaz); bu arada bittiyse yeniden deneme hemen Hazir'a geçer.
        static Task<bool> suren; static Task icGorev;
        const int ZAMAN_ASIMI_MS = 10000;
        public static int surum;                        // durum ya da davet değişince artar; açık sayfa bunu yoklayıp yalnız değişince yeniden kurar
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
            adKodu = await AuthenticationService.Instance.GetPlayerNameAsync();
            await FriendsService.Instance.InitializeAsync();
            if (!dinleniyor) { FriendsService.Instance.MessageReceived += Gelen; dinleniyor = true; }
        }

        public static string Kimlik { get { try { return durum == Durum.Hazir ? AuthenticationService.Instance.PlayerId : null; } catch (Exception) { return null; } } }

        // Arkadaş isteği: kimlikle ya da oyuncu adıyla (ad "Ad#1234" biçimindedir). Karşı taraf kabul edene dek istek bekler.
        public static async Task<bool> ArkadasEkle(string kimlikVeyaAd)
        {
            if (string.IsNullOrWhiteSpace(kimlikVeyaAd) || !await Baslat()) return false;
            try
            {
                string s = kimlikVeyaAd.Trim();
                if (s.IndexOf('#') > 0) await FriendsService.Instance.AddFriendByNameAsync(s);
                else await FriendsService.Instance.AddFriendAsync(s);
                return true;
            }
            catch (Exception e) { hataNotu = e.GetType().Name + ": " + e.Message; Debug.LogWarning("[Ugs] " + hataNotu); return false; }
        }

        // Bana gelen arkadaş istekleri; kabul = aynı kişiye AddFriendAsync (UGS'de karşılıklı istek arkadaşlık olur).
        public static List<Arkadas> Istekler()
        {
            var l = new List<Arkadas>();
            try
            {
                if (durum != Durum.Hazir) return l;
                foreach (var r in FriendsService.Instance.IncomingFriendRequests)
                    l.Add(new Arkadas { id = r.Member.Id, ad = r.Member.Profile != null ? r.Member.Profile.Name : r.Member.Id });
            }
            catch (Exception e) { hataNotu = e.GetType().Name + ": " + e.Message; Debug.LogWarning("[Ugs] " + hataNotu); }
            return l;
        }

        // Kabul edilmiş arkadaşlar (istekler ayrı listede, burada yok).
        public static List<Arkadas> Arkadaslar()
        {
            var l = new List<Arkadas>();
            try
            {
                if (durum != Durum.Hazir) return l;
                foreach (var r in FriendsService.Instance.Friends)
                    l.Add(new Arkadas { id = r.Member.Id, ad = r.Member.Profile != null ? r.Member.Profile.Name : r.Member.Id });
                l.Sort((a, b) => string.Compare(a.ad, b.ad, StringComparison.CurrentCultureIgnoreCase));
            }
            catch (Exception e) { hataNotu = e.GetType().Name + ": " + e.Message; Debug.LogWarning("[Ugs] " + hataNotu); }
            return l;
        }

        // Arkadaşa "maça gel" iletisi. Karşı uygulama açıksa anında düşer; kapalıyken push bildirimi sonraki dilimde (APNs).
        public static async Task<bool> DavetEt(string arkadasId)
        {
            if (string.IsNullOrEmpty(arkadasId) || !await Baslat()) return false;
            try { await FriendsService.Instance.MessageAsync(arkadasId, new Iletisi { tur = DAVET_ON }); return true; }
            catch (Exception e) { hataNotu = e.GetType().Name + ": " + e.Message; Debug.LogWarning("[Ugs] " + hataNotu); return false; }
        }

        static void Gelen(Unity.Services.Friends.Notifications.IMessageReceivedEvent e)
        {
            try { if (e.GetAs<Iletisi>().tur != DAVET_ON) return; if (!bekleyenDavet.Contains(e.UserId)) bekleyenDavet.Add(e.UserId); surum++; if (DavetGeldi != null) DavetGeldi(e.UserId); }
            catch (Exception ex) { Debug.LogWarning("[Ugs] " + ex.Message); }
        }
    }
}
