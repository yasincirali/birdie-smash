# Game Center başarım görselleri (1024×1024, RGB, saydamlıksız; Apple başarım görseli koşulu).
# 2026-11-08 inceleme turu: ilk 11 görsel (PR #11) elle/araçsız üretilmişti, 8 başarımın görseli yoktu
# (bs_tur_2..4, bs_efsane, bs_antrenor, bs_gunun_maci, bs_usta_6, bs_imza_okur) ve ASC kaydı görselsiz
# yapılamıyor. Bu betik aynı dili (koyu zemin, renkli halka, iç disk, ortada tek kısa simge) koddan üretir;
# yeni başarım gelince ROZETLER'e bir satır eklenir, eski 11 görsel olduğu gibi kalır (yeniden üretilmez).
# Kullanım: python3 tool/gorsel/game_center_rozet.py  (yalnız eksik dosyaları yazar; --hepsi ile üzerine yazar)
import os, sys
from PIL import Image, ImageDraw, ImageFont

ZEMIN, DISK = (11, 26, 18), (20, 48, 31)
LIME, MAVI, ALTIN, TURUNCU = (216, 255, 90), (122, 215, 255), (255, 210, 58), (255, 138, 60)
# Renk dili mevcut 11 görselden: lime = başlangıç/antrenman, mavi = orta, altın = zirve, turuncu = hücum/usta.
ROZETLER = {
    "bs_tur_2": ("II", MAVI),        # Asya Turu finali (tur numarası Roma rakamı: kariyer basamaklarının 1–6'sıyla karışmaz)
    "bs_tur_3": ("III", MAVI),       # Avrupa Turu finali
    "bs_tur_4": ("IV", ALTIN),       # Dünya Finalleri
    "bs_efsane": ("30", ALTIN),      # Seviye 30
    "bs_antrenor": ("24", LIME),     # 24 antrenman yıldızı
    "bs_gunun_maci": ("☀", ALTIN),  # Günün maçı: güneş
    "bs_usta_6": ("U6", TURUNCU),    # Usta yolunda 6 basamak
    "bs_imza_okur": ("◉", TURUNCU),  # İmza vuruşu okuru: göz bebeği
}
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"

def ciz(simge, renk):
    im = Image.new("RGB", (1024, 1024), ZEMIN); d = ImageDraw.Draw(im)
    d.ellipse((90, 90, 934, 934), fill=renk); d.ellipse((136, 136, 888, 888), fill=ZEMIN); d.ellipse((170, 170, 854, 854), fill=DISK)
    # Tek simge (güneş, göz) DejaVu'da rakamdan küçük çizilir: aynı görsel ağırlık için büyütülür.
    boy = 620 if not simge.isalnum() else {1: 400, 2: 330, 3: 290}.get(len(simge), 290)
    f = ImageFont.truetype(FONT, boy)
    x0, y0, x1, y1 = d.textbbox((0, 0), simge, font=f)
    d.text((512 - (x0 + x1) / 2, 512 - (y0 + y1) / 2), simge, font=f, fill=renk)
    return im

if __name__ == "__main__":
    kok = os.path.join(os.path.dirname(__file__), "..", "..", "store_listing", "game_center")
    hepsi = "--hepsi" in sys.argv
    for ad, (simge, renk) in ROZETLER.items():
        yol = os.path.join(kok, ad + ".png")
        if os.path.exists(yol) and not hepsi: continue
        ciz(simge, renk).save(yol, optimize=True); print("yazıldı", ad)
