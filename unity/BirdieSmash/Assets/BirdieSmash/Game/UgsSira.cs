// Skor ve rank listesi (2026-11-27, yasin: "burada bir scorelist, ranklist olabilir"): UGS Leaderboards, kimlik "kupa".
// Dashboard'da Leaderboards ürünü açık ve "kupa" listesi (sıralama: azalan, güncelleme: son skor) yoksa çağrılar hata verir;
// arayüz o zaman "Liste henüz hazır değil" der, oyun etkilenmez. Skor istemciden gelir (doğrulanmamış; Cloud Code ile
// sonraki dilim): bu yüzden liste eğlence sıralamasıdır, ödül bağlanmaz. Yerleşmemiş oyuncu (kupa -1) listeye yazmaz.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class UgsSira
    {
        const string LISTE = "kupa";
        public struct Satir { public int sira; public string ad; public int kupa; public bool ben; }

        static string Temiz(string ad) { if (string.IsNullOrEmpty(ad)) return "?"; int i = ad.IndexOf('#'); return i > 0 ? ad.Substring(0, i) : ad; }

        // Maç bitince: ağa çıkılamazsa ya da liste yoksa sessiz geçer (Game Center yolu etkilenmez).
        public static async void Gonder(int kupa)
        {
            if (kupa < 0 || !Ugs.Bayrak) return;
            try { if (!await Ugs.Baslat()) return; await LeaderboardsService.Instance.AddPlayerScoreAsync(LISTE, kupa); }
            catch (Exception e) { Debug.LogWarning("[UgsSira] " + e.GetType().Name + ": " + e.Message); }
        }

        // İlk 20 + kendi sıram. null: liste alınamadı (arayüz "hazır değil" der).
        public static async Task<List<Satir>> Getir()
        {
            try
            {
                if (!await Ugs.Baslat()) return null;
                string ben = AuthenticationService.Instance.PlayerId; var l = new List<Satir>(); bool benVar = false;
                var r = await LeaderboardsService.Instance.GetScoresAsync(LISTE, new GetScoresOptions { Limit = 20 });
                foreach (var e in r.Results) { bool b = e.PlayerId == ben; benVar |= b; l.Add(new Satir { sira = e.Rank + 1, ad = Temiz(e.PlayerName), kupa = (int)e.Score, ben = b }); }
                if (!benVar)
                {
                    try { var m = await LeaderboardsService.Instance.GetPlayerScoreAsync(LISTE); l.Add(new Satir { sira = m.Rank + 1, ad = Temiz(m.PlayerName), kupa = (int)m.Score, ben = true }); }
                    catch (Exception) { }   // henüz skoru yok
                }
                return l;
            }
            catch (Exception e) { Debug.LogWarning("[UgsSira] " + e.GetType().Name + ": " + e.Message); return null; }
        }
    }
}
