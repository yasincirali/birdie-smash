// Dereceli rastgele rakip (2026-11-27, yasin: "ranka göre rastgele rakip bul UGS ile olmalı"). Eşleştirme UGS Lobby'siyle,
// Matchmaker'sız (Dashboard'da ek kurulum gerekmez): her arayan kendi Relay odasını kurar ve odanın kodunu açık bir Lobby
// kaydına yazar (Public veri; gizli puan N1 dizinli). Aynı anda iki arayan birbirini bulur: puan aralığındaki daha ESKİ lobiyi
// bulan kendi odasını kapatıp onun koduyla katılır; en eski lobinin sahibi kimseye katılmaz, beklemeye devam eder. Böylece iki
// kişi birbirine aynı anda katılmaya çalışıp ıskalamaz. Aralık zamanla genişler (±150 → ±1000). Relay'in tek istemcilik
// ayırması ikinci katılımcıyı kendiliğinden reddeder; bayatlamış lobi (sahibi çıkmış) katılım hatası verir ve atlanır.
// Süre dolarsa "rakip bulunamadı" der; Game Center ve kodlu oda yolları sürer. Kota bitince bu yol Hata'ya düşer.
// shortcut: lobi sahibi sinyal vermeden kapanırsa lobi 30 sn kadar görünür kalabilir (katılım hatası atlar); Cloud Code gelince temizlenir.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Debug = UnityEngine.Debug;

namespace BirdieSmash.Game
{
    public static class UgsEsles
    {
        public enum Hal { Yok, Araniyor, Hata, Bulunamadi }
        public static Hal hal = Hal.Yok;
        public static int surum;                  // hal değişince artar
        public static int saniye;                 // arama süresi (arayüz sayacı)
        public const int SURE = 45;               // bu kadar sn sonra vazgeç
        static int nesil;
        static string lobiId;

        public static bool Araniyor { get { return hal == Hal.Araniyor; } }

        public static async Task Ara(int puan)
        {
            Iptal(); int n = nesil; hal = Hal.Araniyor; saniye = 0; surum++;
            try
            {
                var sw = Stopwatch.StartNew(); int deneme = 0;
                while (n == nesil && sw.Elapsed.TotalSeconds < SURE && deneme++ < 4)
                {
                    if (!await UgsAg.OdaKur()) throw new Exception("oda kurulamadı");
                    if (n != nesil) return;
                    string kod = UgsAg.kod;
                    var lobi = await LobbyService.Instance.CreateLobbyAsync("bs", 2, new CreateLobbyOptions
                    {
                        IsPrivate = false,
                        Data = new Dictionary<string, DataObject>
                        {
                            { "kod", new DataObject(DataObject.VisibilityOptions.Public, kod) },
                            { "puan", new DataObject(DataObject.VisibilityOptions.Public, puan.ToString(), DataObject.IndexOptions.N1) }
                        }
                    });
                    lobiId = lobi.Id; var olusma = lobi.Created; long sonKalp = sw.ElapsedMilliseconds;
                    bool baska = false;
                    while (n == nesil && sw.Elapsed.TotalSeconds < SURE && !baska)
                    {
                        await Task.Delay(2000);
                        if (n != nesil) return;
                        saniye = (int)sw.Elapsed.TotalSeconds; surum++;
                        if (UgsAg.hal == UgsAg.Hal.Bagli) { Sil(); hal = Hal.Yok; surum++; return; }   // rakip bizim odamıza katıldı
                        if (!UgsAg.Aktif) break;                                                         // oda düştü: yeniden kur
                        if (sw.ElapsedMilliseconds - sonKalp > 10000) { sonKalp = sw.ElapsedMilliseconds; try { await LobbyService.Instance.SendHeartbeatPingAsync(lobiId); } catch (Exception) { } }
                        int aralik = 150 + (int)(sw.Elapsed.TotalSeconds * 20);
                        var adaylar = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                        {
                            Count = 10,
                            Filters = new List<QueryFilter>
                            {
                                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                                new QueryFilter(QueryFilter.FieldOptions.N1, (puan - aralik).ToString(), QueryFilter.OpOptions.GE),
                                new QueryFilter(QueryFilter.FieldOptions.N1, (puan + aralik).ToString(), QueryFilter.OpOptions.LE)
                            },
                            Order = new List<QueryOrder> { new QueryOrder(true, QueryOrder.FieldOptions.Created) }
                        });
                        foreach (var a in adaylar.Results)
                        {
                            if (a.Id == lobiId || a.Created >= olusma || a.Data == null || !a.Data.ContainsKey("kod")) continue;   // yalnız benden eski lobiler
                            Sil();
                            if (await UgsAg.Katil(a.Data["kod"].Value))
                            {
                                for (int k = 0; k < 16 && n == nesil && UgsAg.hal == UgsAg.Hal.Bekliyor; k++) await Task.Delay(500);
                                if (UgsAg.hal == UgsAg.Hal.Bagli) { hal = Hal.Yok; surum++; return; }
                            }
                            baska = true; break;    // katılım olmadı: bu turu kapat, kendi odamı yeniden kurup devam
                        }
                    }
                    Sil();
                }
                if (n == nesil) { UgsAg.Kapat(); hal = Hal.Bulunamadi; surum++; }
            }
            catch (Exception e)
            {
                if (n != nesil) return;
                Debug.LogWarning("[UgsEsles] " + e.GetType().Name + ": " + e.Message);
                Sil(); UgsAg.Kapat(); hal = Hal.Hata; surum++;
            }
        }

        // Online.Kapat ve İptal düğmesi çağırır: arama biter, lobi silinir.
        public static void Iptal() { nesil++; Sil(); if (hal == Hal.Araniyor) { hal = Hal.Yok; surum++; } }

        static async void Sil()
        {
            string id = lobiId; lobiId = null; if (id == null) return;
            try { await LobbyService.Instance.DeleteLobbyAsync(id); } catch (Exception) { }
        }
    }
}
