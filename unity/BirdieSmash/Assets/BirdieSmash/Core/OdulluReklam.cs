// Maç sonu ödüllü reklam kuralı (2026-11-07). Karar kaydı: arge/yapilacaklar.md "7 Kas 14:30 incelemesi" (inceleme turu
// kuralı + geliştirme thread'inin ekonomi kararı). Bu dosya saf mantıktır (UnityEngine yok), BotMac'te sınanır; reklamı
// yükleyip gösteren Game/ReklamSdk.cs, düğmeyi çizen Hud.
//
//  - Ek ödül +%50 Tüy, yalnız Tüy: XP, yıldız ve görev değişmez. Taban maçın Tüy ödülüdür (zorluk çarpanı ve maç ekleri
//    uygulanmış hâli, kartta görünen sayı). "Katla" denmez: ödül iki katı değil (düğme "Ödüle +%50 · reklam izle").
//  - Günde en çok 1 (yerel gün). Gerekçe tool/ekonomi_denge.py: günlük ~527 Tüy, reklam +~44 (%8); Sv 30'da kozmetik payı
//    %34 → ~%43, hedef bandı %30–45. 2× ya da günde 2 bunu ~%53'e çıkarır ("her şeyi alamaz" kararı, yasin 1 Kas).
//  - İlk 3 galibiyete kadar hiç (yeni oyuncu önce oyunu tanır), maç başına en çok 1 (düğme tek yuvada, bir kez).
//  - Kartın tek ikincil yuvası doluysa (antrenman önerisi, yükselt, Usta daveti, maçın anı…) o maç teklif yok; ana düğme
//    hiç yer değiştirmez.
//  - 3 ardışık ret (teklif görüldü, izlenmeden kart kapandı) → sonraki 3 maç teklif yok. Ret sayılır, ceza değildir.
//  - Yarıda kapanan reklam ödül vermez, sayaç da yanmaz (yazı suçlamaz; düğme o maç bir daha çıkmaz).
using System;

namespace BirdieSmash.Core
{
    public static class OdulluReklam
    {
        public const string ANAHTAR = "tuytop_odullu_reklam";
        public const int ILK_GALIBIYET = 3, GUNLUK = 1, RET_SERI = 3, SUS_MAC = 3;

        public static int Ek(int tuy) { return tuy <= 0 ? 0 : (int)Math.Floor(tuy * .5 + .5); }

        public class Durum
        {
            public string gun = ""; public int bugun, ret, sus;
            public static Durum Oku(string s)
            {
                var d = new Durum(); if (string.IsNullOrEmpty(s)) return d;
                var p = s.Split('|'); if (p.Length < 4) return d;
                int n; d.gun = p[0]; if (int.TryParse(p[1], out n)) d.bugun = n; if (int.TryParse(p[2], out n)) d.ret = n; if (int.TryParse(p[3], out n)) d.sus = n;
                return d;
            }
            public string Yaz() { return gun + "|" + bugun + "|" + ret + "|" + sus; }
            public int Bugun(string g) { return gun == g ? bugun : 0; }
        }

        public static string Gun(DateTime t) { return t.Year + "-" + t.Month + "-" + t.Day; }

        // Bu maçın kartında teklif çıkar mı? reklamHazir: SDK'da yüklenmiş, gösterilebilir reklam var.
        public static bool Uygun(Durum d, string gun, int galibiyet, int odulTuy, bool baskaIkincil, bool buMacKullanildi, bool reklamHazir)
        {
            if (!reklamHazir || buMacKullanildi || baskaIkincil || odulTuy <= 0) return false;
            if (galibiyet < ILK_GALIBIYET || d.sus > 0) return false;
            return d.Bugun(gun) < GUNLUK;
        }

        // İzlendi, ödül hak edildi: günlük sayaç artar, ret serisi sıfırlanır.
        public static void Izlendi(Durum d, string gun)
        {
            if (d.gun != gun) { d.gun = gun; d.bugun = 0; }
            d.bugun++; d.ret = 0;
        }

        // Kart kapandı. teklifGoruldu: düğme bu maç ekrana çizildi; izlendi: ödül alındı.
        // Sus sayacı yalnız teklif çıkabilecekken geçen maçlarda değil, her bitmiş maçta azalır (3 maç = 3 maç).
        public static void KartKapandi(Durum d, bool teklifGoruldu, bool izlendi)
        {
            if (d.sus > 0) { d.sus--; return; }
            if (!teklifGoruldu || izlendi) return;
            d.ret++; if (d.ret >= RET_SERI) { d.ret = 0; d.sus = SUS_MAC; }
        }
    }
}
