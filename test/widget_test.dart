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
}
