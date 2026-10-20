import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

// WebView testte yüklenmez; oyun dosyasının kabuğun beklediği sözleşmeyi
// taşıdığı burada kilitlenir: Haptic kanalı ve debug kancasının yokluğu.
void main() {
  test('oyun dosyası Haptic köprüsünü kullanır, debug kancası taşımaz', () {
    final h = File('assets/game/index.html').readAsStringSync();
    expect(h, contains('window.Haptic'));
    expect(h, isNot(contains('__tt')));
    expect(h, isNot(contains('M.log')));
  });

  // loadFlutterAsset dosyayı HTTP başlığı olmadan açar: charset meta'sı yoksa
  // WKWebView Latin-1 sayıp Türkçe harfleri "aÃ§Ä±k" diye bozuyordu (2026-10-20).
  // viewport meta'sı yoksa iOS sayfayı 980 px genişlikte yerleştirip küçültür.
  test('oyun dosyası UTF-8 ve telefon genişliğinde açılır', () {
    final h = File('assets/game/index.html').readAsStringSync();
    final bas = h.substring(0, 400).toLowerCase();
    expect(bas, contains('<meta charset="utf-8">'));
    expect(bas, contains('width=device-width'));
    expect(h, isNot(contains('fonts.googleapis.com')));
  });
}
