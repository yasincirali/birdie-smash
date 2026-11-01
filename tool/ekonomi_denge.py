# Tüy dengesi ölçümü (2026-11-01, yasin: "hemen her şeyi alabilememesi lazım, bu tasarımı best practice şeklinde yap").
# Soru: ücretsiz oyuncu bir seviyeye geldiğinde kazandığı Tüy, o seviyede açılmış her şeyin ne kadarını alır?
# Varsayım (tahmin): 11 sayılık Kolay maç, %60 galibiyet, maç başına 1,5 görev, 5 mükemmel vuruş; günde 5 maç +
# günün ilk galibiyeti (+50) + günün maçı (+40). Tek seferlik ödüller (kariyer basamağı, yıldız, antrenman) Sv 30'a
# kadar ~4.500 Tüy diye eklenir. Ödül formülü Core/Gear.cs Odul ile aynı.
# Kural: Tüy ve XP her maçta birlikte geldiği için oran oyun sıklığından bağımsızdır; dengeyi fiyatlar kurar.
# Hedef (F2P kozmetik ekonomisi yaygın uygulaması): güç ilerlemesinin (ekipman + yükseltme) tamamı oynayarak alınır;
# kozmetiklerin yalnız bir kısmı (~%30–45), oyuncu seçer. Paket Tüy'ü seviye kapısını aşmaz.
# Kullanım: python3 tool/ekonomi_denge.py
import json, math, os

KOK = os.path.join(os.path.dirname(__file__), 'veri')
JR = lambda v: int(math.floor(v + .5))
esik = lambda L: JR((100 + 60 * (L - 1) ** 1.35) / 10) * 10
YUK_FIYAT = [50, 110, 180, 290, 450, 680, 1000, 1450, 2100]; YUK_SV = [1, 2, 4, 6, 9, 12, 16, 20, 24]; YUVA = 6
KOZ = ('tipler', 'sac_model', 'sapka', 'sevinc', 'mac_sonu', 'yuz', 'sirt')

def mac(won, f=2, zor=1, g=1.5, p=5):
    return ((40 if won else 15) * f + g * 15 + p) * zor, ((50 if won else 20) * f + g * 15 + p * 2) * zor

def main():
    g = json.load(open(os.path.join(KOK, 'gear.json')))['modeller']; k = json.load(open(os.path.join(KOK, 'karakter.json')))
    (tw, xw), (tl, xl) = mac(True), mac(False)
    gun_t = 5 * (.6 * tw + .4 * tl) + 90; gun_x = 5 * (.6 * xw + .4 * xl) + 30
    xp = [0]; [xp.append(xp[-1] + esik(L)) for L in range(1, 30)]
    koz_top = sum(o['fiyat'] for kk in KOZ for o in k[kk])
    print('kozmetik katalog toplamı', koz_top, '· günlük ~%d Tüy' % gun_t)
    for L in (5, 10, 15, 20, 25, 30):
        gun = xp[L - 1] / gun_x; kaz = gun * gun_t + 4500 * L / 30
        guc = sum(m['fiyat'] for v in g.values() for m in (v.values() if isinstance(v, dict) else v) if m.get('sv', 1) <= L)
        guc += YUVA * sum(f for f, s in zip(YUK_FIYAT, YUK_SV) if s <= L)
        koz = sum(o['fiyat'] for kk in KOZ for o in k[kk] if o.get('sv', 1) <= L)
        artan = max(0, kaz - guc)
        print('Sv %2d · %3.0f gün · kazanılan %6.0f · açık güç %6d · açık kozmetik %6d · güçten artan kozmetiğin %%%.0f\'i'
              % (L, gun, kaz, guc, koz, 100 * artan / max(1, koz)))

if __name__ == '__main__':
    main()
