// Kodlu oda taşıması (2026-11-27, yasin TestFlight'ta: "invite sadece kodla olsun, davet edince maça geçemiyorum"): Core/Oturum'un
// baytlarını Unity Relay + Unity Transport ile taşır. Game Center yolu (Online.cs, BirdieOnline.mm) ayrı kalır; bu sınıf onun
// yerine geçmez, aynı üç çağrıyı (durum, gönder, al) sunar ve Online hangisinin açık olduğuna göre birini seçer.
// Akış: kurucu Oda kur → Relay ayırma + 6 haneli katılım kodu (Relay'in kendi kodu; kısa, büyük harf/rakam) → kod paylaşılır →
// katılan Koda katıl → aynı Relay'e bağlanır. İki taraf bağlanınca Online.Tik Oturum'u kurar, bundan sonrası Game Center'la aynı.
// Hata hiçbir yerden fırlamaz: kota biterse ya da kod yanlışsa hal Hata olur, sayfa tek cümle söyler, Game Center sürer.
// Güvenlik: DTLS ("dtls"), Relay ayırması kuran kişiye özel; kodu bilen herkes katılabilir (arkadaşa elle gönderilen kod).
// shortcut: tek bağlantı (1 rakip), yeniden bağlanma yok; kopunca Oturum zaten hükmen karar verir. Üçüncü kişi katılırsa yok sayılır.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class UgsAg
    {
        public enum Hal { Yok, Kuruyor, Bekliyor, Bagli, Hata }
        public static Hal hal = Hal.Yok;
        public static string kod;                  // kurucu için: paylaşılacak katılım kodu
        public static string hataNotu;             // tanı günlüğü; oyuncuya gösterilmez
        public static int surum;                   // hal değişince artar; sayfa yoklayıp yalnız değişince yeniden kurar
        public static bool kodYanlis;              // Katıl kodu bulunamadı (ayrı cümle için)

        // Online'ın seçici: kurulum sürerken, bağlıyken ya da bağlanılmayı beklerken bu yol açık.
        public static bool Aktif { get { return hal == Hal.Kuruyor || hal == Hal.Bekliyor || hal == Hal.Bagli; } }
        public const int BOS = 0, ARIYOR = 1, BAGLI = 2;   // Online ile aynı sayılar
        public static int Durum { get { return hal == Hal.Bagli ? BAGLI : Aktif ? ARIYOR : BOS; } }

        static NetworkDriver surucu; static NetworkConnection bag; static NetworkPipeline boru; static bool surucuVar, kurucu;
        static readonly Queue<byte[]> gelen = new Queue<byte[]>();
        static int nesil;   // Kapat'tan sonra biten eski kurulum yeni oturumu bozmasın

        public static async Task<bool> OdaKur()
        {
            Kapat(); int n = nesil; hal = Hal.Kuruyor; kod = null; kodYanlis = false; surum++;
            try
            {
                if (!await Ugs.Baslat()) throw new Exception("kimlik yok");
                var a = await RelayService.Instance.CreateAllocationAsync(1);
                string k = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
                if (n != nesil) return false;
                var rsd = new RelayServerData(a, "dtls");
                SurucuKur(rsd); surucu.Bind(NetworkEndpoint.AnyIpv4); surucu.Listen();
                kurucu = true; kod = k; hal = Hal.Bekliyor; surum++; return true;
            }
            catch (Exception e) { return Hata(n, e, false); }
        }

        public static async Task<bool> Katil(string girilen)
        {
            Kapat(); int n = nesil; hal = Hal.Kuruyor; kod = null; kodYanlis = false; surum++;
            try
            {
                string k = (girilen ?? "").Trim().ToUpperInvariant();
                if (k.Length == 0) throw new ArgumentException("kod boş");
                if (!await Ugs.Baslat()) throw new Exception("kimlik yok");
                var j = await RelayService.Instance.JoinAllocationAsync(k);
                if (n != nesil) return false;
                var rsd = new RelayServerData(j, "dtls");
                SurucuKur(rsd); surucu.Bind(NetworkEndpoint.AnyIpv4);
                kurucu = false; bag = surucu.Connect(rsd.Endpoint); hal = Hal.Bekliyor; surum++; return true;
            }
            catch (Exception e) { return Hata(n, e, true); }
        }

        static void SurucuKur(RelayServerData rsd)
        {
            var ayar = new NetworkSettings(); ayar.WithRelayParameters(ref rsd);
            surucu = NetworkDriver.Create(ayar); surucuVar = true;
            // Oturum paketleri küçük ama parçalama güvence; sıralı güvenilir = GKMatch Reliable ile aynı davranış.
            boru = surucu.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        }

        static bool Hata(int n, Exception e, bool katil)
        {
            if (n != nesil) return false;
            hataNotu = e.GetType().Name + ": " + e.Message; Debug.LogWarning("[UgsAg] " + hataNotu);
            // Relay "kod yok / süresi doldu" 404 döner; ayrı cümle için işaretle (ayrıntıya bağlı kalma: katılırken hata = kod yanlış sayılır).
            Kapat(); kodYanlis = katil && Ugs.durum == Ugs.Durum.Hazir; hal = Hal.Hata; surum++; return false;
        }

        // Her karede (Online.Tik). Olayları boşaltır: bağlanma, veri, kopma.
        public static void Guncelle()
        {
            if (!surucuVar) return;
            try
            {
                surucu.ScheduleUpdate().Complete();
                if (kurucu) { NetworkConnection c; while ((c = surucu.Accept()).IsCreated) { if (!bag.IsCreated) { bag = c; } else c.Disconnect(surucu); } }
                if (!bag.IsCreated) return;
                DataStreamReader r; NetworkEvent.Type ev;
                while ((ev = surucu.PopEventForConnection(bag, out r)) != NetworkEvent.Type.Empty)
                {
                    if (ev == NetworkEvent.Type.Connect) { }
                    else if (ev == NetworkEvent.Type.Data) { var b = new byte[r.Length]; r.ReadBytes(b); gelen.Enqueue(b); }
                    else if (ev == NetworkEvent.Type.Disconnect) { Kapat(); return; }
                }
                if (hal == Hal.Bekliyor && (kurucu || surucu.GetConnectionState(bag) == NetworkConnection.State.Connected)) { hal = Hal.Bagli; surum++; }
            }
            catch (Exception e) { hataNotu = e.GetType().Name + ": " + e.Message; Debug.LogWarning("[UgsAg] " + hataNotu); Kapat(); }
        }

        public static void Gonder(byte[] b)
        {
            if (hal != Hal.Bagli || !surucuVar || !bag.IsCreated) return;
            try
            {
                if (surucu.BeginSend(boru, bag, out var w) != 0) return;
                w.WriteBytes(b); surucu.EndSend(w);
            }
            catch (Exception e) { hataNotu = e.GetType().Name + ": " + e.Message; Debug.LogWarning("[UgsAg] " + hataNotu); }
        }

        public static int Al(byte[] tampon)
        {
            if (gelen.Count == 0) return 0;
            var b = gelen.Dequeue(); int n = Math.Min(b.Length, tampon.Length); Buffer.BlockCopy(b, 0, tampon, 0, n); return n;
        }

        public static void Kapat()
        {
            nesil++; gelen.Clear();
            if (surucuVar)
            {
                try { if (bag.IsCreated) bag.Disconnect(surucu); surucu.ScheduleUpdate().Complete(); surucu.Dispose(); } catch (Exception) { }
                surucuVar = false;
            }
            bag = default; kod = null;
            if (hal != Hal.Hata && hal != Hal.Yok) { hal = Hal.Yok; surum++; }
        }
    }
}
