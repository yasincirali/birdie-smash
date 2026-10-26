// İmza vuruşları (komple-oyun tasarımı §3.3, Ek A.2; adım 3, 2026-10-26). Yalnız patron rakipte (turun finali) açılır.
// Arcade okunurluğu kuralları (tasarımdaki dört madde) burada kilitli:
//   1. Her zaman görünür hazırlık: poz + renkli ışık (görünüm, PlayerView) + ses, en az IMZA_HAZIRLIK sn (Kolay'da +.2).
//      Hazırlık vuruştan IMZA_HAZIRLIK + .05 sn önce başlar (bir kare payı); vuruşa bu kadar zaman kalmayan topta imza
//      seçilmez. Duvar blokta karar anı rakibin değil oyuncunun vuruşudur: duruş, rakip yüksek top atınca başlar ve top
//      fileyi geçmeden (oyuncu ona daha vuramazken) hazırlık süresi kadar görünür.
//   2. Sıklık: maçta en çok IMZA_AZAMI kez, iki imza arasında en az IMZA_ARA sayı (ralli). İlk kullanımda tek seferlik
//      uyarı yazısı ("Yıldırım smaç: çapraz gelir!").
//   3. Her imzanın bir cevabı var; cevap verilince "Okudun!" (Ev.Okudun, maç sonunda +5 XP, "okudun" görevi sayılır).
//   4. İmza yalnız bir vuruştur: sayı kuralını değiştirmez, bonus sayı yok.
// İmzalar ve cevapları:
//   yildirim (Blaze)   sıçrayarak smaç, %12 hızlı, hep çapraz (rakibin bulunduğu kanadın karşı köşesi). Cevap: karşılamak.
//   duvarblok (Rocco)  duruş gösterilirken oyuncu smaçlarsa top fileye kısa bloklanır (rakip tepkisiz çıkar). Cevap:
//                      duruşu görüp smaç yerine drop/blok: rakip ters ayakta kalır (tepkisi .3 sn gecikir).
//   tuldrop (Mika)     fileyi az farkla geçen alçak file vuruşu. Cevap: karşılamak (erken öne çıkış).
//   mermi (Nova)       iki ardışık düz drive, ikincisi gövdeye. Cevap: ikinciyi blok, lift ya da drop ile almak.
//   golgeflick (Vex)   drop duruşundan arka çizgiye flick; Kolay'da kendiliğinden koşu .25 sn öne aldanır. Cevap: karşılamak.
//   karma (Aria)       duvarblok → mermi → golgeflick sırayla (veri: imzalar.karma.sira).
// Saf C#: BotMac hazırlık süresini, sıklığı ve cevabı ölçer.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public partial class Match
    {
        public const float IMZA_HAZIRLIK = .4f, IMZA_P = .5f; public const int IMZA_AZAMI = 4, IMZA_ARA = 4;
        public int imzaSay, okudun; int imzaSonPuan = 1 - IMZA_ARA, karmaI; bool duvarBlokla, imzaOkundu, mermiIkinci; string imzaUyarildi = "";
        public float ImzaHazirlik { get { return IMZA_HAZIRLIK + (easy ? .2f : 0); } }

        // Şimdi imza kullanılabilir mi; kullanılabilirse hangisi (karma sıradakini verir).
        string ImzaSec()
        {
            if (stil == null || !stil.patron || stil.imza == null || ogr != null || imzaSay >= IMZA_AZAMI || sc[0] + sc[1] - imzaSonPuan < stil.imzaAra) return null;
            if (stil.imza == "karma") { var b = KariyerVeri.Imza("karma"); return b != null && b.sira != null && b.sira.Length > 0 ? b.sira[karmaI % b.sira.Length] : null; }
            return stil.imza;
        }

        // Rakibe gelen top için (PlanIntercept): bu vuruş imza olsun mu. Yalnız rakibin kendi vuruşu olan imzalar.
        // Rakip topu normalde ya baş üstünde ya en alçak noktada alır; mermi drive göğüs hizası, tül drop ve gölge flick ön
        // alanda diz üstü ister. Bu yüzden imza seçilince karşılama noktası yoldan yeniden seçilir (imzalı rakip topu erken
        // alır: tarzın bir parçası). need: PlanIntercept'in yetişme süresi (aynı tepki ve hız).
        void ImzaPlanla(float t0, List<PathPt> cands, Func<PathPt, float, float> need)
        {
            var I = icpt; if (I == null || I.leave || s.serve) return;
            string id = ImzaSec(); if (id == null || id == "duvarblok") return;
            float H = ImzaHazirlik + .1f; PathPt yeni = null;
            switch (id)
            {
                case "yildirim": if (!(I.overhead && HandOf(op, "op", I.x, I.z) == "fore" && I.y < Court.NET - 2.4f && I.t - t0 >= H)) return; break;
                case "mermi": foreach (var p in cands) if (p.z > .9f && p.z < 1.9f && p.t - t0 >= H && need(p, 1.15f) <= p.t) { yeni = p; break; } if (yeni == null) return; break;
                default:   // tuldrop, golgeflick: ön alan, diz üstü
                    foreach (var p in cands) if (p.y > Court.NET - 2.8f && p.z > .5f && p.z < 1.45f && p.t - t0 >= H && need(p, 1.15f) <= p.t) { yeni = p; break; }
                    if (yeni == null) return; break;
            }
            if (R01() >= stil.imzaP) return;
            if (yeni != null) { I.t = yeni.t; I.x = yeni.x; I.y = yeni.y; I.z = yeni.z; I.overhead = false; I.kill = false; I.side = float.NaN; }
            I.imza = id; I.imzaBas = I.t - ImzaHazirlik - .05f;
            I.opType = id == "yildirim" ? "smash" : id == "mermi" ? "drive" : "drop";   // sıçrama ve hazırlık pozu buna bakar
        }

        // Hazırlık başladı: görünüm pozu tutar, ışığı yakar; ilk kullanımda uyarı yazısı.
        void ImzaBasla(string id, float sure)
        {
            op.imzaT = sure; op.imzaId = id; imzaSay++; imzaSonPuan = sc[0] + sc[1]; if (stil.imza == "karma") karmaI++;
            Emit(Ev.Imza, "op", id, op.x, op.y, 1.8f, sure);
            if (!imzaUyarildi.Contains("|" + id + "|")) { imzaUyarildi += "|" + id + "|"; var b = KariyerVeri.Imza(id); if (b != null) Say("op", "imza", op.x, op.y, 1.5f, b.uyari, 3); }
        }

        // Update içinde, rakibe gelen top uçarken: hazırlık zamanı geldiyse başlat; süre kısalır (duvar blok duruşu oyuncunun
        // vuruşuna kadar sürer, sayaç onda işlemez).
        void ImzaTick(Intercept I, float d)
        {
            if (op.imzaT > 0 && op.imzaId != "duvarblok") op.imzaT = Math.Max(0, op.imzaT - d);
            if (I != null && I.who == "op" && I.imza != null && !I.imzaBasladi && s != null && s.from == "me" && s.t >= I.imzaBas) { I.imzaBasladi = true; ImzaBasla(I.imza, I.t - s.t); }
        }

        // Rakip yüksek top attı (StartFlight sonrası, iniş tahmini hazır): duvar blok duruşu açılsın mı. Oyuncunun vurabileceği
        // ilk ana kadar hazırlık süresi kadar zaman olmalı.
        void ImzaDuvar(string type)
        {
            if (type != "clear" || s.landPred == null) return; string id = ImzaSec(); if (id != "duvarblok") return;
            // oyuncunun vurabileceği en erken an: top kendi tarafında ve 3,1 m'nin altında (ReachInfo'nun sınırı; uzaklık hesaba
            // katılmaz, yani gerçek süre bundan uzun olabilir, kısa olamaz)
            float gec = float.NaN; foreach (var p in s.landPred.path) if (p.y > Court.NET + .05f && p.z <= 3.1f) { gec = p.t; break; }
            if (float.IsNaN(gec) || gec - s.t < ImzaHazirlik + .05f || R01() >= stil.imzaP) return;
            ImzaBasla(id, 9);   // süre oyuncunun vuruşunda biter
        }

        // Oyuncunun vuruşu (SwipeShot, uçuştan önce): imzayı cevapladı mı.
        void ImzaCevap(string type)
        {
            if (s != null && s.from == "op" && s.imza != null)
            {
                string id = s.imza;
                if (id == "mermi2") { if (type == "block" || type == "lift" || type == "drop") Okudun("mermi"); }
                else if (id != "mermi" && id != "duvarblok") Okudun(id);
            }
            if (op.imzaT > 0 && op.imzaId == "duvarblok")
            {
                op.imzaT = 0; op.imzaId = null;
                if (type == "smash" || type == "jump") duvarBlokla = true;
                else if (type == "drop" || type == "block") { imzaOkundu = true; Okudun("duvarblok"); }
            }
        }

        void Okudun(string id)
        {
            okudun++; Emit(Ev.Okudun, "me", id, me.x, me.y, 1.2f);
            Say("me", "okudun", me.x, me.y, 1.2f, "Okudun!", 3); GorevKontrol();
        }

        // PlanIntercept'in rakip dalı: duvar blok kararı (tepkisiz çıkış) ya da okunan duvar (geç ve ters çıkış).
        float ImzaTepki(float react, out bool blokla)
        {
            blokla = duvarBlokla; duvarBlokla = false;
            if (blokla) react = 0;
            if (imzaOkundu) { imzaOkundu = false; react += .3f; op.fakeUntil = clock + .32f; op.fakeX = Clamp(2 * Court.MID - s.x, Court.SL, Court.SR); }
            return react;
        }

        // Rakibin imza vuruşu (OpHit başında). true: vuruş yapıldı.
        bool ImzaVurus(Intercept I)
        {
            float hx = s.x, hy = s.y, hz = Math.Max(.2f, s.z), vx, vy, vz; string id = I.imza, type; op.imzaT = 0; op.imzaId = null;
            switch (id)
            {
                case "yildirim":
                    {   // çapraz: rakibin bulunduğu kanadın karşı köşesi; derinlik normal smaç bandı
                        float tx = op.x < Court.MID ? Court.SR - .45f : Court.SL + .45f, ty = Court.NET + 3.4f + R01() * .8f;
                        type = "smash"; Shuttle.Aim(hx, hy, hz, tx, ty, false, 52 * 1.12f, out vx, out vy, out vz);
                        if (!FileGecer(hx, hy, hz, vx, vy, vz)) Shuttle.Aim(hx, hy, hz, tx, ty, true, .2f, out vx, out vy, out vz);
                        OpYor(.12f); break;
                    }
                case "mermi":
                    {
                        float tx = Court.MID + (R01() * 2 - 1) * .8f * (Court.SR - Court.SL) * .62f; type = "drive";
                        Shuttle.Aim(hx, hy, hz, tx, Court.NET + 4.6f, false, 22 * 1.12f, out vx, out vy, out vz);
                        if (!FileGecer(hx, hy, hz, vx, vy, vz)) Shuttle.Aim(hx, hy, hz, tx, Court.NET + 4.6f, true, .3f, out vx, out vy, out vz);
                        mermiIkinci = true; break;
                    }
                case "golgeflick":
                    {   // oyuncudan uzak arka köşe; düz ve hızlı yay. Kolay'da kendiliğinden koşu bir an öne (drop sandı).
                        float tx = me.x < Court.MID ? Court.SR - .5f : Court.SL + .5f; type = "clear";
                        AlcakYay(hx, hy, hz, tx, Court.L - .6f, .45f, out vx, out vy, out vz);
                        if (easy) { me.fakeUntil = clock + .25f; me.fakeX = me.x; me.fakeY = Math.Max(Court.NET + 1.2f, me.y - 1.5f); }
                        break;
                    }
                case "duvarblok":
                    {
                        float tx = Clamp(hx + (R01() - .5f) * 1.2f, Court.SL + .3f, Court.SR - .3f); type = "drop";
                        AlcakYay(hx, hy, hz, tx, Court.NET + .7f + R01() * .3f, .6f, out vx, out vy, out vz); break;
                    }
                default:   // tuldrop: fileyi az farkla geçen alçak yay, ön alana
                    {
                        float tx = Court.MID + (R01() - .5f) * 2.4f; type = "drop";
                        AlcakYay(hx, hy, hz, tx, Court.NET + .55f, .55f, out vx, out vy, out vz); break;
                    }
            }
            StartFlight("op", vx, vy, vz, hx, hy, hz, false, type, false, "good"); s.imza = id; return true;
        }

        // Mermi drive'ın ikincisi: rakip yetişip göğüs hizasında alabiliyorsa gövdeye düz drive.
        bool MermiIkinci(Intercept I)
        {
            if (!mermiIkinci) return false; mermiIkinci = false;
            float hx = s.x, hy = s.y, hz = Math.Max(.2f, s.z); if (I.overhead || hz < .7f || hz > 2.2f) return false;
            float tx = Clamp(me.x, Court.SL + .3f, Court.SR - .3f), ty = Clamp(me.y, Court.NET + 2, Court.L - 1), vx, vy, vz;
            Shuttle.Aim(hx, hy, hz, tx, ty, false, 22 * 1.15f, out vx, out vy, out vz);
            if (!FileGecer(hx, hy, hz, vx, vy, vz)) return false;
            StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drive", false, "good"); s.imza = "mermi2"; return true;
        }

        // Alçak yay: verilen açıdan başla, fileyi geçmiyorsa açıyı büyüt (top hiç fileye gitmez; imza hatayla bozulmaz).
        static void AlcakYay(float hx, float hy, float hz, float tx, float ty, float a, out float vx, out float vy, out float vz)
        {
            for (int i = 0; i < 6; i++) { Shuttle.Aim(hx, hy, hz, tx, ty, true, a + i * .12f, out vx, out vy, out vz); if (FileGecer(hx, hy, hz, vx, vy, vz)) return; }
            Shuttle.Aim(hx, hy, hz, tx, ty, true, 1.1f, out vx, out vy, out vz);
        }
        static bool FileGecer(float hx, float hy, float hz, float vx, float vy, float vz) { var c = Shuttle.Simulate(hx, hy, hz, vx, vy, vz, false, Shuttle.KD); return c.net == null || c.net.ok; }

        void ImzaSifirla() { op.imzaT = 0; op.imzaId = null; duvarBlokla = false; imzaOkundu = false; mermiIkinci = false; }

        // Görünüm için: hazırlık pozunun klibi (yalnız renk değil biçim de: sıçrama, file bileği, eğilme, drop duruşu, blok).
        public static string ImzaKlip(string id)
        {
            switch (id) { case "yildirim": return "jump"; case "mermi": return "drive"; case "duvarblok": return "block"; default: return "net"; }
        }

        // Aria (uyum): oyuncunun son 8 vuruşu arka alana gidiyorsa daha derinde, öne gidiyorsa daha önde bekler (±.6 m).
        readonly string[] sonVurus = new string[8]; int sonVurusN;
        void VurusKaydet(string type) { sonVurus[sonVurusN % sonVurus.Length] = type; sonVurusN++; }
        public float OpEv()
        {
            float ev = stil.ev; if (stil.uyum <= 0 || sonVurusN == 0) return ev;
            int n = Math.Min(sonVurus.Length, sonVurusN), on = 0, arka = 0;
            for (int i = 0; i < n; i++) { string t = sonVurus[i]; if (t == "drop" || t == "block" || t == "kill") on++; else if (t == "clear" || t == "lift" || t == "smash" || t == "jump") arka++; }
            return Clamp(ev + stil.uyum * 2 * (arka - on) / n, 1.8f, 4.2f);
        }
    }
}
