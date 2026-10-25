// Kabuk ile yerel iOS kodu arasındaki sözleşme (Ar-Ge 23 ve 25 Eki, yasin onayı 25 Eki). Saf Dart: WebView
// testte yüklenmediği için oyundan gelen mesajın ayıklanması burada ve test/kabuk_test.dart'ta kilitlenir.
//
// İlerleme devri: Unity sürümü kaydı PlayerPrefs'ten okur; iOS'ta PlayerPrefs = NSUserDefaults.standard, anahtar
// adı olduğu gibi, string değer NSString. shared_preferences her anahtarın başına "flutter." koyduğu için
// kullanılmaz; yazma ios/Runner/AppDelegate.swift'teki yöntem kanalıyla birebir adla yapılır. Kanal yazmadan önce
// Unity'nin bayrağına (devirBayrak) bakar: Unity devri bir kez aldıysa eski HTML build'i onun üstüne yazamaz.

/// Yerel iOS koduyla konuşulan yöntem kanalı (AppDelegate.swift ile aynı ad).
const kabukKanalAdi = 'birdiesmash/kabuk';

/// Unity devri kabul edince koyar; HTML tarafı bu anahtarı asla göndermez, kabuk da yazmaz.
const devirBayrak = 'tuytop_html_devir';

/// Bozuk/kötü niyetli bir sayfa kullanıcı ayarlarını şişirmesin: oyunun en büyük kaydı birkaç KB.
const _azamiDeger = 64 * 1024;
const _azamiAnahtar = 64;
const _azamiAdet = 64;

/// Oyunun `{tip:'devir', veri:{...}}` mesajındaki `veri`yi yazılacak anahtar → değer haritasına çevirir.
/// Yalnız `tuytop_` ile başlayan, string değerli anahtarlar geçer; bayrak ayıklanır. Yazılacak bir şey yoksa null.
Map<String, String>? devirAyikla(Object? veri) {
  if (veri is! Map) return null;
  final o = <String, String>{};
  for (final e in veri.entries) {
    final k = e.key, v = e.value;
    if (k is! String || v is! String) continue;
    if (!k.startsWith('tuytop_') || k == devirBayrak) continue;
    if (k.length > _azamiAnahtar || v.length > _azamiDeger) continue;
    o[k] = v;
    if (o.length >= _azamiAdet) break;
  }
  return o.isEmpty ? null : o;
}
