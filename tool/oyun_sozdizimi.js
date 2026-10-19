// assets/game/index.html içindeki <script> bloğunu derler; hata varsa CI kırmızı.
const fs = require('fs');
const h = fs.readFileSync('assets/game/index.html', 'utf8');
const m = h.match(/<script>([\s\S]*)<\/script>/);
if (!m) { console.error('script bloğu yok'); process.exit(1); }
new Function(m[1]);
if (/__tt\(|M\.log/.test(h)) { console.error('debug kancası kalmış (__tt / M.log)'); process.exit(1); }
console.log('oyun script sözdizimi tamam', h.length, 'bayt');
