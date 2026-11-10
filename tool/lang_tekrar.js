// Lang.cs çeviri sözlüğünde yinelenen anahtar denetimi (2026-11-10). UNITY_EK bir koleksiyon başlatıcısıdır: aynı anahtar
// iki kez yazılırsa derleme geçer ama oyun açılışta tür başlatıcısında ArgumentException ile düşer (mono ile yerel derleme
// de yakalamaz). 10 Kasım'da "+{0} Tüy" iki kez eklendi, push'tan önce elle fark edildi; bu betik CI'da yakalar.
const fs = require('fs');
const yol = 'unity/BirdieSmash/Assets/BirdieSmash/Game/Lang.cs';
const s = fs.readFileSync(yol, 'utf8');
const bas = s.indexOf('UNITY_EK = new');
if (bas < 0) { console.error('UNITY_EK bulunamadı: ' + yol); process.exit(1); }
const son = s.indexOf('\n        };', bas);
const blok = s.slice(bas, son < 0 ? s.length : son);
const re = /\{"((?:[^"\\]|\\.)*)",\s*"/g; const say = new Map(); let m;
while ((m = re.exec(blok))) say.set(m[1], (say.get(m[1]) || 0) + 1);
const tekrar = [...say].filter(([, n]) => n > 1).map(([k]) => k);
if (tekrar.length) { console.error('Lang.cs yinelenen anahtar (oyun açılışta düşer):\n  ' + tekrar.join('\n  ')); process.exit(1); }
console.log('Lang.cs anahtarları tek: ' + say.size);
