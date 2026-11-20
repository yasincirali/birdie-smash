// Online oturum (2026-11-20, yasin: "o zaman başlayalım bunu kurmaya"; rapor https://claude.ai/artifact/V2uZN5PBYvFSP3GC3RAynf
// 2. aşama "Bağlantı"). Core/Uzak maçın ne göndereceğini bilir; bu dosya iki telefonun maçtan ÖNCE ve maç BOYUNCA anlaşmasını
// yürütür: selamlaşma (sürüm + oyuncu kartı), ilk servis kurası, gecikme ölçümü (UzakBag.gecikme), sessizlikten kopma.
// Taşıma burada da YOK: Game/Online baytları Game Center (GKMatch) ile taşır, ileride UGS Relay aynı iki listeye bağlanır
// (giden boşaltılır, gelen Al ile verilir). Bu yüzden BotMac oturumu da ağsız, gecikmeli kuyrukla ölçer (Oturumlar()).
//
// Çerçeve: ilk bayt türü söyler. 1 = UzakPaket (kendi SURUM baytı; ek başlık yok, maç paketi bir bayt bile büyümez),
// 200 = Merhaba, 201 = Ping, 202 = Pong. Bilinmeyen tür yok sayılır (yeni sürümün ek mesajı eskiyi düşürmez).
//
// Servis kurası: iki taraf Merhaba'da birer rastgele sayı yollar, büyük olan servisi atar. Eşitse ikisi de yeniden atar.
// Neden oyuncu kimliği kıyası değil: kimlik taşımaya özgü (Game Center / UGS), kura çekirdekte kalırsa taşıma değişince aynı.
//
// Kopma: SESSIZ_KOPMA sn hiçbir bayt gelmezse (ping saniyede bir gider, yani rakip ya çıktı ya bağlantısı yok) oturum kopar
// ve maça Ayrildi paketi verilir: kalan taraf hükmen kazanır. Bağlantısı kopan taraf da kendi telefonunda "rakip ayrıldı"
// görür; arkadaş maçında sonuç hiçbir yere yazılmadığı için zararsız.
// shortcut: tek eşik (rapor 5 sn duraklat + 30 sn hükmen diyordu), dereceli maç gelince iki kademe + sunucu kararı.
using System;
using System.Collections.Generic;
using System.Text;

namespace BirdieSmash.Core
{
    public class Oturum
    {
        public const byte SURUM = 1;
        const byte MERHABA = 200, PING = 201, PONG = 202;
        public const float PING_ARALIK = 1f, SESSIZ_KOPMA = 10f, GECIKME_TAVAN = .5f;
        public enum Durum { Selam, Hazir, Koptu }

        public Durum durum = Durum.Selam;
        public string kopmaNeden;            // "sessiz" | "surum" | "baglanti"
        public OyuncuKarti rakip;            // null = kart okunamadı (varsayılan görünüş)
        public bool servisBende;
        public float gecikme;                // tek yön, sn (RTT / 2, yumuşatılmış)
        public readonly List<byte[]> giden = new List<byte[]>();   // taşıma boşaltır

        readonly List<byte[]> erken = new List<byte[]>();   // maç kurulmadan gelen maç paketleri
        readonly Random rnd; readonly byte[] kart;
        uint zar; float saat, pingT, sessiz; bool olcuVar, ayrilVerildi;

        public Oturum(int tohum, OyuncuKarti ben)
        {
            rnd = new Random(tohum); kart = Encoding.UTF8.GetBytes(ben != null ? ben.Yaz() : "");
            Selamla();
        }

        void Selamla()
        {
            zar = (uint)rnd.Next(1, int.MaxValue);
            var b = new byte[6 + kart.Length]; b[0] = MERHABA; b[1] = SURUM;
            BitConverter.GetBytes(zar).CopyTo(b, 2); kart.CopyTo(b, 6);
            giden.Add(b);
        }

        // Taşıma da çağırır ("baglanti": GKMatch rakibin koptuğunu sessizlik eşiğinden önce bildirdi).
        public void Kop(string neden) { if (durum == Durum.Koptu) return; durum = Durum.Koptu; kopmaNeden = neden; }

        // Taşımadan gelen her mesaj. Karşı telefonun verisidir: uzunluk ve tür denetlenir, bozuk olan yok sayılır.
        public void Al(byte[] b)
        {
            if (b == null || b.Length == 0 || durum == Durum.Koptu) return;
            sessiz = 0;
            switch (b[0])
            {
                case UzakPaket.SURUM: erken.Add(b); break;
                case MERHABA:
                    if (b.Length < 6) return;
                    if (b[1] != SURUM) { Kop("surum"); return; }   // iki uygulama sürümü konuşamıyor: maç kurulmaz
                    uint o = BitConverter.ToUInt32(b, 2);
                    if (o == zar) { Selamla(); return; }           // kura berabere: iki taraf da yeniden atar
                    rakip = OyuncuKarti.Oku(Encoding.UTF8.GetString(b, 6, b.Length - 6));
                    servisBende = zar > o; durum = Durum.Hazir;
                    break;
                case PING:
                    if (b.Length != 5) return;
                    var y = (byte[])b.Clone(); y[0] = PONG; giden.Add(y);
                    break;
                case PONG:
                    if (b.Length != 5) return;
                    float g = (saat - BitConverter.ToSingle(b, 1)) / 2;
                    if (!(g >= 0)) return;                          // NaN ya da gelecekten gelen saat: ölçüm değil
                    g = Math.Min(g, GECIKME_TAVAN);
                    gecikme = olcuVar ? gecikme + (g - gecikme) * .2f : g; olcuVar = true;
                    break;
            }
        }

        // Her karede. bag = maçın ağ ucu (maç kurulmadan önce null): gelen maç paketleri ona verilir, gideni alınır.
        public void Tik(float dt, UzakBag bag)
        {
            if (durum != Durum.Koptu)
            {
                saat += dt; sessiz += dt; pingT -= dt;
                if (pingT <= 0)
                {
                    pingT = PING_ARALIK;
                    var p = new byte[5]; p[0] = PING; BitConverter.GetBytes(saat).CopyTo(p, 1); giden.Add(p);
                }
                if (sessiz > SESSIZ_KOPMA) Kop("sessiz");
            }
            if (bag == null) return;
            if (durum == Durum.Koptu)
            {
                if (!ayrilVerildi) { ayrilVerildi = true; bag.Al(new UzakPaket { tur = PaketTur.Ayrildi }.Yaz()); }
                return;
            }
            bag.gecikme = gecikme;
            for (int i = 0; i < erken.Count; i++) bag.Al(erken[i]);
            erken.Clear();
            giden.AddRange(bag.giden); bag.giden.Clear();
        }
    }
}
