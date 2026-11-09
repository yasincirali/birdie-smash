#!/usr/bin/env python3
"""Ekran turunun yazı taşma kapısı (2026-11-10 inceleme, fikir 02:05 #5).

EkranTuru.YaziTara her turda yazi_tasma.txt yazıyordu ama hiçbir şey okumuyordu: r143 114 → r147 97 arasında
bir kırık geri geldi ve kimse görmedi. Bu kapı sandık'taki "sayı yalnız azalabilir" kilitlerinin aynısı:
sayı tool/unity_test/yazi_tasma_tavan.txt'yi geçerse iş kırmızı olur ve yeni satırlar özetlenir. Sayı düşünce
tavan elle indirilir (kapı kendiliğinden indirmez ki düşüş görünür bir karar olsun).

Sayılmayanlar: dil düğmesinin kendi adı "Türkçe" (bilerek çevrilmez) ve 2 px'ten küçük "kesik" farkları
(yazılım GL ile cihaz arasındaki yuvarlama gürültüsü).
Kullanım: yazi_tasma_kapi.py <yazi_tasma.txt> <tavan.txt>
"""
import re
import sys


def sayilir(satir: str) -> bool:
    if "çevrilmemiş" in satir and satir.rstrip().endswith('"Türkçe"'):
        return False
    m = re.search(r"kesik (?:genişlik|yükseklik) (\d+)>(\d+)", satir)
    if m and int(m.group(1)) - int(m.group(2)) < 2:
        return False
    return True


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__)
        return 2
    try:
        satirlar = open(sys.argv[1], encoding="utf-8").read().splitlines()[1:]
    except FileNotFoundError:
        print("yazi_tasma.txt yok: ekran turu yazı taramasına ulaşmadı (kapı atlandı)")
        return 0
    tavan = int(open(sys.argv[2], encoding="utf-8").read().strip())
    say = [s for s in satirlar if s.strip() and sayilir(s)]
    print(f"yazı taşması: {len(say)} (tavan {tavan})")
    if len(say) > tavan:
        print("Tavan aşıldı. Satırlar:")
        for s in say:
            print("  " + s)
        return 1
    if len(say) < tavan:
        print(f"Düştü: tool/unity_test/yazi_tasma_tavan.txt dosyasını {len(say)} yap.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
