import 'dart:io';

import 'package:birdiesmash/kabuk.dart';
import 'package:flutter_test/flutter_test.dart';

// Kabuk ↔ oyun ↔ yerel iOS sözleşmesi (Ar-Ge 23 ve 25 Eki). Yerel kod ve WebView testte koşmaz; adların üç
// yerde aynı kaldığı ve devir ayıklamasının doğru olduğu burada kilitlenir.
void main() {
  group('devirAyikla', () {
    test('yalnız tuytop_ string değerleri geçer, bayrak ayıklanır', () {
      final d = devirAyikla({
        'tuytop_v2': '{"rally":12,"wins":3,"kariyer":2}',
        'tuytop_len': 'len11',
        'tuytop_html_devir': '1',
        'baska': 'x',
        'tuytop_sayi': 5,
      });
      expect(d, {'tuytop_v2': '{"rally":12,"wins":3,"kariyer":2}', 'tuytop_len': 'len11'});
    });
    test('boş, harita olmayan ya da aşırı büyük veri yazılmaz', () {
      expect(devirAyikla(null), isNull);
      expect(devirAyikla('tuytop_v2'), isNull);
      expect(devirAyikla({'tuytop_html_devir': '1'}), isNull);
      expect(devirAyikla({'tuytop_v2': 'x' * (64 * 1024 + 1)}), isNull);
    });
  });

  test('kanal adı ve bayrak oyun, kabuk ve AppDelegate arasında aynı', () {
    final swift = File('ios/Runner/AppDelegate.swift').readAsStringSync();
    expect(swift, contains('"$kabukKanalAdi"'));
    expect(swift, contains('"$devirBayrak"'));
    expect(swift, contains('"devirYaz"'));
    expect(swift, contains('"puanIste"'));
    final h = File('assets/game/index.html').readAsStringSync();
    expect(h, contains("kabuk({tip:'devir',veri:v})"));
    expect(h, contains("kabuk({tip:'puan'})"));
    expect(h, contains("k!=='$devirBayrak'"));
    final unity = File('unity/BirdieSmash/Assets/BirdieSmash/Core/Devir.cs').readAsStringSync();
    expect(unity, contains('"$devirBayrak"'));
  });

  test('puan dalı Android kontrolünden önce gelir', () {
    final m = File('lib/main.dart').readAsStringSync();
    expect(m.indexOf("v['tip'] == 'puan'"), lessThan(m.indexOf('if (!Platform.isIOS) return;')));
  });
}
