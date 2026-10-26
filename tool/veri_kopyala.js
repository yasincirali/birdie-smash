// Ortak oyun verisi tek kaynak (komple-oyun tasarımı Ek A.4, 2026-10-26): tool/veri/*.json → Unity Resources/veri.
// HTML sürümü donduruldu (yasin), bu yüzden yalnız Unity hedefi var; HTML'e <script id="bs_veri_*"> kopyası yazılmaz.
// Kullanım: node tool/veri_kopyala.js        (kopyala)
//           node tool/veri_kopyala.js --denetle   (fark varsa çıkış kodu 1; BotMac de aynı denetimi yapar)
const fs = require('fs'), path = require('path');
const kok = path.join(__dirname, '..'), kaynak = path.join(kok, 'tool', 'veri'), hedef = path.join(kok, 'unity', 'BirdieSmash', 'Assets', 'Resources', 'veri');
const denetle = process.argv.includes('--denetle'); let fark = 0;
fs.mkdirSync(hedef, { recursive: true });
for (const ad of fs.readdirSync(kaynak).filter(f => f.endsWith('.json'))) {
  const icerik = fs.readFileSync(path.join(kaynak, ad), 'utf8'); JSON.parse(icerik);   // bozuk JSON kopyalanmaz
  const h = path.join(hedef, ad), eski = fs.existsSync(h) ? fs.readFileSync(h, 'utf8') : null;
  if (eski === icerik) continue;
  if (denetle) { console.log('FARK: ' + ad); fark++; } else { fs.writeFileSync(h, icerik); console.log('kopyalandı: ' + ad); }
}
process.exit(fark ? 1 : 0);
