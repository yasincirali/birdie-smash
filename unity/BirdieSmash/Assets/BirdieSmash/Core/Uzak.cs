// Uzak rakip (2026-11-07, yasin: "online karşılıklı oynanacak hale getirmeliyiz"; rapor
// https://claude.ai/artifact/V2uZN5PBYvFSP3GC3RAynf). Online 1v1'in çekirdek tarafı: rakibin yapay zekâsı kapanır, vuruşu,
// servisi, sayısı ve koşusu karşı telefondan paket olarak gelir. Taşıma (Relay/Lobi) burada YOK; bu dosya yalnız ne
// gönderileceğini ve gelenin nasıl uygulanacağını bilir, böylece BotMac iki çekirdeği gecikmeli bir kuyrukla birbirine
// bağlayıp ağsız test eder (IkizMaclar).
//
// Neden kare kare durum değil vuruş olayı: badmintonda karar vuruşta verilir, top uçuşu (Shuttle fiziği) aynı
// parametrelerle iki telefonda aynı hesaplanır. Vuran taraf vuruşu kendi çekirdeğinde hemen oynatır (gecikmesiz his),
// ~40 baytlık paket karşıya gider; karşı taraf aynalayıp gecikme kadar ileri sarar (100 ms = 0,6–1,4 sn'lik uçuşun %10'u).
//
// Otorite: sayıya topu KARŞILAYAN taraf karar verir (alıcı otoritesi). Rakibin vurduğu topu ben değerlendiririm (içeri,
// dışarı, file, yetişemedim) ve Sayi paketi gönderirim; kendi vurduğum top düşünce çekirdeğim bekler: o arada rakibin vuruş
// paketi gelirse top dirilir (rakip yere değmeden yetişmiştir), Sayi gelirse onun kararı uygulanır. Neden "düştüğü taraf"
// değil: fileye yakın düşüşte iki çekirdeğin ağ durumu (NetWave) farklı olabilir ve iki taraf aynı topa birlikte karar
// verirdi; her uçuşun tek hakemi olunca çift sayı imkânsız.
//
// Online'da zaman yerel olarak bükülmez: vuruş penceresi ağır çekimi (PencereAdim), vuruş donması (freeze) ve sayı ağır
// çekimi kapalıdır. Bükülen yerel saat karşı tarafın topu yerde bekletmesine (görünür "düştü-dirildi") yol açardı.
// Rakibe ait yapay zekâ yolları (PlanIntercept'in rakip kolu, imza, aldatma, OpServe, MoveAcc) uzak kipte çalışmaz.
// Uzak kip kapalıyken (uzak == null) Match davranışı birebir eskisi; her kanca "uzak != null" ile korunur.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BirdieSmash.Core
{
    public enum PaketTur : byte { Vurus = 1, Sayi = 2, Konum = 3 }

    // Tek paket türü (alanların kullanılmayanı 0): Vurus = temas noktası + hız + tür; Sayi = kazanan + neden + skor;
    // Konum = oyuncunun kort noktası. Koordinatlar GÖNDERENİN çerçevesinde; alan Uygula sırasında aynalar.
    public class UzakPaket
    {
        public PaketTur tur; public int sira;
        public float hx, hy, hz, vx, vy, vz;
        public bool servis, super, slice; public string tip, vurus, q, servisTur;
        public bool benKazandim; public string neden; public int sc0, sc1;
        public float x, y;

        public const byte SURUM = 1;
        public byte[] Yaz()
        {
            using (var ms = new MemoryStream(64)) using (var w = new BinaryWriter(ms, Encoding.UTF8))
            {
                w.Write(SURUM); w.Write((byte)tur); w.Write((ushort)Math.Max(0, Math.Min(65535, sira)));
                if (tur == PaketTur.Vurus)
                {
                    w.Write(hx); w.Write(hy); w.Write(hz); w.Write(vx); w.Write(vy); w.Write(vz);
                    w.Write((byte)((servis ? 1 : 0) | (super ? 2 : 0) | (slice ? 4 : 0)));
                    Yazi(w, tip); Yazi(w, vurus); Yazi(w, q); Yazi(w, servisTur);
                }
                else if (tur == PaketTur.Sayi) { w.Write(benKazandim); Yazi(w, neden); w.Write((byte)sc0); w.Write((byte)sc1); }
                else { w.Write(x); w.Write(y); }
                return ms.ToArray();
            }
        }
        public static UzakPaket Oku(byte[] b)
        {
            try
            {
                using (var r = new BinaryReader(new MemoryStream(b), Encoding.UTF8))
                {
                    if (r.ReadByte() != SURUM) return null;   // başka sürümün paketi: uygulanmaz (eşleştirme sürümü ayırır)
                    var p = new UzakPaket { tur = (PaketTur)r.ReadByte(), sira = r.ReadUInt16() };
                    if (p.tur == PaketTur.Vurus)
                    {
                        p.hx = r.ReadSingle(); p.hy = r.ReadSingle(); p.hz = r.ReadSingle(); p.vx = r.ReadSingle(); p.vy = r.ReadSingle(); p.vz = r.ReadSingle();
                        byte f = r.ReadByte(); p.servis = (f & 1) != 0; p.super = (f & 2) != 0; p.slice = (f & 4) != 0;
                        p.tip = Oku(r); p.vurus = Oku(r); p.q = Oku(r); p.servisTur = Oku(r);
                    }
                    else if (p.tur == PaketTur.Sayi) { p.benKazandim = r.ReadBoolean(); p.neden = Oku(r); p.sc0 = r.ReadByte(); p.sc1 = r.ReadByte(); }
                    else if (p.tur == PaketTur.Konum) { p.x = r.ReadSingle(); p.y = r.ReadSingle(); }
                    else return null;
                    return p;
                }
            }
            catch (Exception) { return null; }   // bozuk paket: yok sayılır (taşıma güvenilir; bu yalnız savunma)
        }
        static void Yazi(BinaryWriter w, string s) { var b = s == null ? new byte[0] : Encoding.UTF8.GetBytes(s); w.Write((byte)Math.Min(255, b.Length)); w.Write(b, 0, Math.Min(255, b.Length)); }
        static string Oku(BinaryReader r) { int n = r.ReadByte(); return n == 0 ? null : Encoding.UTF8.GetString(r.ReadBytes(n)); }
    }

    // Çekirdeğin ağ ucu: giden kuyruğu taşıma boşaltır, gelen paketleri Al ile verir. gecikme = tek yön tahmini (sn);
    // taşıma RTT/2 ölçüp yazar, rakibin vuruşu bu kadar ileri sarılır.
    public class UzakBag
    {
        public readonly List<byte[]> giden = new List<byte[]>();
        public readonly List<UzakPaket> gelen = new List<UzakPaket>();
        public float gecikme;
        public const float KONUM_ARALIK = .1f;   // 10 Hz: görünüm ara değerle yumuşatır, ayak işi mesafeden seçilir
        public int gidenSayi, gidenBayt;
        public void Al(byte[] b) { var p = UzakPaket.Oku(b); if (p != null) gelen.Add(p); }
        internal void Gonder(UzakPaket p) { var b = p.Yaz(); giden.Add(b); gidenSayi++; gidenBayt += b.Length; }
    }

    public partial class Match
    {
        public UzakBag uzak;
        float uzakIleri, uzakKonumT, uzakHx = float.NaN, uzakHy;
        bool uzakBekle, uzakSayiUyguluyor;
        public bool UzakBekliyor { get { return uzakBekle; } }   // top karşı kortta düştü, rakibin kararı bekleniyor

        // Ayna: herkes kendini kortun altında (y > NET) görür.
        static float AynaX(float x) { return Court.CW - x; }
        static float AynaY(float y) { return Court.L - y; }

        // StartFlight'tan (from == "me"): vuruşumu karşıya bildir. sira = bu uçuşun ralli numarası.
        void UzakVurusGonder(string q)
        {
            uzak.Gonder(new UzakPaket { tur = PaketTur.Vurus, sira = rally, hx = s.hx, hy = s.hy, hz = s.hz, vx = s.vx, vy = s.vy, vz = s.vz,
                servis = s.serve, super = s.super, slice = s.slice, tip = s.type, vurus = s.vurus, q = q, servisTur = s.servisTur });
            uzakBekle = false;
        }

        // Point'ten: kararı ben verdiysem (rakibin topunu ben karşıladım) karşıya bildir.
        void UzakSayiGonder(string winner, string why)
        {
            if (uzakSayiUyguluyor) return;
            int wi = winner == "me" ? 0 : 1;
            uzak.Gonder(new UzakPaket { tur = PaketTur.Sayi, sira = rally, benKazandim = winner == "me", neden = why, sc0 = sc[0] + (wi == 0 ? 1 : 0), sc1 = sc[1] + (wi == 1 ? 1 : 0) });
        }

        // Land'den: kendi vurduğum top düştüyse karar rakibin, sayı verilmez, bekle (true döner). Top yere değdiği an ölü
        // sayılmaz: rakip kendi çekirdeğinde yere değmeden yetişmiş olabilir ve paketi gecikme kadar sonra gelir. Bu payda top
        // zeminde durur (ses, sekme, çizgi kamerası yok), vuruş gelirse dirilir; pay bitince düşüş görünür, karar yine beklenir.
        float uzakYerAt = float.NaN;
        public const float UZAK_YER_PAY = .06f;   // + gecikme × 1,5 (titreşim payı)
        bool UzakDusus()
        {
            if (s.from != "me") return false;
            uzakBekle = true;
            if (float.IsNaN(uzakYerAt) || uzakYerTop != s) { uzakYerAt = clock; uzakYerTop = s; uzakInV[0] = s.inVx; uzakInV[1] = s.inVy; uzakInV[2] = s.inVz; }
            else { s.inVx = uzakInV[0]; s.inVy = uzakInV[1]; s.inVz = uzakInV[2]; }   // sonraki adımlar sıfır hızla gelir: iniş hızı ilk temasınki (yer sekmesi görünümü)
            s.z = 0; s.vx = s.vy = s.vz = 0;
            if (clock - uzakYerAt < UZAK_YER_PAY + uzak.gecikme * 1.5f) return true;
            s.dead = true; DrilDusus();
            if (!s.netHit) Emit(Ev.Floor, "me", s.type, s.x, s.y, 0, 0, null, null, s.type == "smash");
            return true;
        }
        Shuttle uzakYerTop; readonly float[] uzakInV = new float[3];

        // Her adımda (Adim, RunMe'den önce): gelen paketleri sırasıyla uygula, giden konumu yaz, rakibin konumunu yumuşat.
        void UzakTick(float dt)
        {
            for (int i = 0; i < uzak.gelen.Count; )
            {
                var p = uzak.gelen[i]; int r = UzakUygula(p);
                if (r == 0) break;                       // sırası gelmedi: sonrakiler de bekler (sıra korunur)
                uzak.gelen.RemoveAt(i);                  // uygulandı ya da bayat: düşer
            }
            uzakKonumT -= dt;
            if (uzakKonumT <= 0) { uzakKonumT = UzakBag.KONUM_ARALIK; uzak.Gonder(new UzakPaket { tur = PaketTur.Konum, sira = rally, x = me.x, y = me.y }); }
            if (!float.IsNaN(uzakHx) && op.sw <= 0)
            {   // 10 Hz noktalar arası: 0,1 sn'de ~%86'sına varan üstel yaklaşım (ani ışınlanma yok, gecikme ~1 paket)
                float k = 1 - (float)Math.Exp(-dt * 20);
                op.x += (uzakHx - op.x) * k; op.y += (uzakHy - op.y) * k;
            }
        }

        // 1 = uygulandı, -1 = bayat (atılır), 0 = henüz değil (bekler).
        int UzakUygula(UzakPaket p)
        {
            if (p.tur == PaketTur.Konum) { uzakHx = AynaX(p.x); uzakHy = AynaY(p.y); return 1; }
            if (p.tur == PaketTur.Sayi)
            {
                if (over) return -1;
                if (between) return p.sira >= rally ? 0 : -1;   // önceki sayının arası sürüyor: yeni ralliye ait olabilir
                if (p.sira < rally) return -1;
                if (p.sira > rally) return 0;
                uzakBekle = false; uzakSayiUyguluyor = true;
                float x = s != null ? s.x : Court.MID, y = s != null ? s.y : Court.NET;
                Point(p.benKazandim ? "op" : "me", p.neden ?? "", x, y);
                uzakSayiUyguluyor = false;
                return 1;
            }
            // Vurus
            if (over) return -1;
            if (p.servis)
            {
                if (between || s != null || server != "op" || svPending != null) return p.sira > rally ? 0 : -1;
                if (p.sira != 1) return -1;
            }
            else
            {
                if (p.sira <= rally) return -1;
                if (p.sira > rally + 1 || s == null || between) return 0;
            }
            if (!p.servis && s != null && s.landPred != null) UzakSapmaOlc(AynaX(p.hx), AynaY(p.hy), p.hz);
            if (p.servis && p.servisTur != null && SERVIS.ContainsKey(p.servisTur)) { op.svClip = SERVIS[p.servisTur].klip; servisTurSon = p.servisTur; }
            vurusAdi = p.vurus;
            float hx = AynaX(p.hx), hy = AynaY(p.hy);
            op.x = hx; op.y = hy + (p.servis ? -.45f : 0);   // vuran yerinde görünür (servis temas noktası gövdenin .45 önü)
            uzakBekle = false;
            StartFlight("op", -p.vx, -p.vy, p.vz, hx, hy, p.hz, p.servis, p.tip ?? "clear", p.super, p.q, p.slice);
            uzakIleri += Math.Max(0, uzak.gecikme);   // karşıda geçen yolu yakala (Adim'in uçuş döngüsü tüketir)
            return 1;
        }

        // Temas sapması: rakibin bildirdiği temas noktası benim çekirdeğimdeki topun yoluna ne kadar uzak (m). Hile denetiminin
        // (3. aşama, Bulut Kod) eşiği bu ölçümden çıkar; bugün yalnız sayılır, hiçbir vuruş reddedilmez. Diriliş: top bende
        // yere düşmüşken gelen vuruş (rakip kendi çekirdeğinde yere değmeden yetişti, gecikme kadar geç).
        public float uzakSapmaMax, uzakSapmaToplam; public int uzakSapmaN, uzakDirilis, uzakYerdenAlinan;   // yerdenAlinan: payda dirilen (düşüş görünmedi)
        void UzakSapmaOlc(float x, float y, float z)
        {
            // Yol 1/60 sn'de bir nokta (Shuttle.Simulate): smaçta noktalar ~0,5 m aralıklı, bu yüzden parçaya uzaklık ölçülür.
            float en = float.MaxValue; var P = s.landPred.path;
            for (int i = 0; i + 1 < P.Count; i++)
            {
                float ax = P[i].x, ay = P[i].y, az = P[i].z, bx = P[i + 1].x - ax, by = P[i + 1].y - ay, bz = P[i + 1].z - az;
                float bb = bx * bx + by * by + bz * bz, u = bb > 1e-9f ? Math.Max(0, Math.Min(1, ((x - ax) * bx + (y - ay) * by + (z - az) * bz) / bb)) : 0;
                float dx = ax + bx * u - x, dy = ay + by * u - y, dz = az + bz * u - z, d2 = dx * dx + dy * dy + dz * dz; if (d2 < en) en = d2;
            }
            if (en == float.MaxValue) return;
            float d = (float)Math.Sqrt(en); uzakSapmaMax = Math.Max(uzakSapmaMax, d); uzakSapmaToplam += d; uzakSapmaN++;
            if (s.dead) uzakDirilis++; if (s == uzakYerTop) uzakYerdenAlinan++;
        }

        float UzakIleriAl() { float v = uzakIleri; uzakIleri = 0; return v; }
    }
}
