"""
Yasal sayfa üreteci: legal/*.md -> docs/*.html (GitHub Pages, main dalı /docs).

Çalıştırma (repo kökünden):
    python3 docs/_build_legal.py

## Neden böyle (2026-10-24)
- Tek kaynak md'dir; HTML elle düzenlenmez. Metin değişince md'deki "Sürüm"
  artar, betik koşulur, ikisi aynı commit'e girer. Her HTML başlığına kaynak
  md'nin sha256'sı gömülür (`birdie-kaynak-sha256`): md değişip betik
  koşulmadıysa fark buradan görünür.
- Bağımlılık yok (pip `markdown` paketi gerekmez): metin bilerek sade tutulur
  (başlık, paragraf, madde, kalın, bağlantı, `kod`). Daha zengin biçim
  gerekirse önce buradaki dönüştürücü genişletilir.
- Sayfa telefonda okunur: viewport, sistem yazı tipi, açık/koyu tema
  (prefers-color-scheme), dış kaynak yok (oyunun "ağ yok" ilkesi sayfada da).
"""
import hashlib
import html
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LEGAL = ROOT / "legal"
DOCS = ROOT / "docs"

# kaynak md -> (çıktı, dil)
SAYFALAR = {
    "gizlilik.md": ("gizlilik.html", "tr"),
    "privacy.md": ("privacy.html", "en"),
}

CSS = """
:root{--bg:#f7f5ef;--fg:#1d211e;--muted:#5b625d;--line:#d9d6cc;--accent:#1f6b43;color-scheme:light dark}
@media (prefers-color-scheme:dark){:root{--bg:#0b1a12;--fg:#e8ece9;--muted:#a3ada6;--line:#26382d;--accent:#7fd3a3}}
*{box-sizing:border-box}
html{-webkit-text-size-adjust:100%}
body{margin:0;background:var(--bg);color:var(--fg);font:17px/1.6 -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif}
main{max-width:680px;margin:0 auto;padding:32px 16px 48px;padding-left:max(16px,env(safe-area-inset-left));padding-right:max(16px,env(safe-area-inset-right))}
h1{font-size:1.6em;line-height:1.25;margin:0 0 .4em}
h2{font-size:1.15em;margin:1.8em 0 .4em;padding-top:1em;border-top:1px solid var(--line)}
p,ul{margin:.6em 0}
ul{padding-left:1.3em}
li{margin:.3em 0}
a{color:var(--accent);text-underline-offset:2px}
code{font:.9em ui-monospace,SFMono-Regular,Menlo,monospace}
.surum{color:var(--muted);font-size:.9em}
""".strip()


def satir_ici(s: str) -> str:
    s = html.escape(s, quote=False)
    s = re.sub(r"`([^`]+)`", r"<code>\1</code>", s)
    s = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", s)
    s = re.sub(
        r"\[([^\]]+)\]\(([^)\s]+)\)",
        lambda m: f'<a href="{html.escape(m.group(2))}">{m.group(1)}</a>',
        s,
    )
    return s


def donustur(md: str) -> tuple[str, str]:
    """(başlık, gövde HTML). Boş satır paragrafı, '- ' maddeyi ayırır."""
    baslik = ""
    parcalar: list[str] = []
    paragraf: list[str] = []
    liste: list[str] = []

    def bosalt():
        if paragraf:
            metin = " ".join(paragraf)
            sinif = ' class="surum"' if re.match(r"^(Sürüm|Version):", metin) else ""
            parcalar.append(f"<p{sinif}>{satir_ici(metin)}</p>")
            paragraf.clear()
        if liste:
            parcalar.append("<ul>" + "".join(f"<li>{satir_ici(x)}</li>" for x in liste) + "</ul>")
            liste.clear()

    for satir in md.splitlines():
        t = satir.strip()
        if not t:
            bosalt()
        elif t.startswith("# "):
            bosalt()
            baslik = t[2:].strip()
            parcalar.append(f"<h1>{satir_ici(baslik)}</h1>")
        elif t.startswith("## "):
            bosalt()
            parcalar.append(f"<h2>{satir_ici(t[3:].strip())}</h2>")
        elif t.startswith("- "):
            if paragraf:
                bosalt()
            liste.append(t[2:].strip())
        elif liste:
            liste[-1] += " " + t  # madde devamı
        else:
            paragraf.append(t)
    bosalt()
    return baslik, "\n".join(parcalar)


def main() -> None:
    for kaynak, (cikti, dil) in SAYFALAR.items():
        md = (LEGAL / kaynak).read_text(encoding="utf-8").lstrip("﻿").replace("\r\n", "\n").rstrip() + "\n"
        ozet = hashlib.sha256(md.encode("utf-8")).hexdigest()
        baslik, govde = donustur(md)
        sayfa = f"""<!doctype html>
<html lang="{dil}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<meta name="color-scheme" content="light dark">
<meta name="birdie-kaynak-sha256" content="{ozet}">
<title>{html.escape(baslik)}</title>
<style>
{CSS}
</style>
</head>
<body>
<!-- Bu dosya legal/{kaynak} kaynağından docs/_build_legal.py ile üretildi; elle düzenleme. -->
<main>
{govde}
</main>
</body>
</html>
"""
        (DOCS / cikti).write_text(sayfa, encoding="utf-8")
        print(f"{kaynak} -> docs/{cikti} ({ozet[:12]})")


if __name__ == "__main__":
    main()
