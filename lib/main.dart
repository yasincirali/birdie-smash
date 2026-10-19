import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:webview_flutter/webview_flutter.dart';

// Birdie Smash: oyun tek bir HTML/canvas dosyasıdır (assets/game/index.html) ve
// prototip olarak https://claude.ai/artifact/4e8aG15tCWDe8hdtVCPryQ adresinde
// yaşar. Flutter kabuğu YALNIZ üç iş yapar: mağaza/TestFlight hattı, tam
// ekran WebView ve haptic köprüsü. Oyun mantığı Dart'a taşınmaz; oyun
// değiştiğinde HTML dosyası yenilenir (tool/oyun_sozdizimi.js doğrular).
void main() {
  WidgetsFlutterBinding.ensureInitialized();
  // Oyun dikey tasarlandı (kort derinliği ekran yüksekliğinde).
  SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]);
  SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  runApp(const BirdieSmashApp());
}

class BirdieSmashApp extends StatelessWidget {
  const BirdieSmashApp({super.key});

  @override
  Widget build(BuildContext context) {
    return const MaterialApp(
      title: 'Birdie Smash',
      debugShowCheckedModeBanner: false,
      home: OyunEkrani(),
    );
  }
}

class OyunEkrani extends StatefulWidget {
  const OyunEkrani({super.key});

  @override
  State<OyunEkrani> createState() => _OyunEkraniState();
}

class _OyunEkraniState extends State<OyunEkrani> {
  late final WebViewController _web;

  @override
  void initState() {
    super.initState();
    _web = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setBackgroundColor(const Color(0xFF0B1A12))
      ..addJavaScriptChannel('Haptic', onMessageReceived: _haptic)
      ..loadFlutterAsset('assets/game/index.html');
  }

  // Oyun navigator.vibrate(desen) çağırır; iOS WKWebView bunu hiç
  // desteklemez, Android WebView'da da sistem titreşimi kaba kalır. Desen
  // ([ms] ya da [ms, bekleme, ms...]) buraya gelir ve süreye göre
  // HapticFeedback'e çevrilir: tel gerilimi/vuruş türü farkı oyunun
  // ürettiği süreyle gelir, kabuk yorumlamaz.
  Future<void> _haptic(JavaScriptMessage m) async {
    List<num> desen;
    try {
      final v = jsonDecode(m.message);
      desen = v is List ? v.cast<num>() : [v as num];
    } catch (_) {
      return;
    }
    for (var i = 0; i < desen.length; i++) {
      final ms = desen[i];
      if (i.isOdd) {
        await Future<void>.delayed(Duration(milliseconds: ms.toInt()));
        continue;
      }
      if (ms <= 0) continue;
      if (ms >= 40) {
        await HapticFeedback.heavyImpact();
      } else if (ms >= 22) {
        await HapticFeedback.mediumImpact();
      } else if (ms >= 12) {
        await HapticFeedback.lightImpact();
      } else {
        await HapticFeedback.selectionClick();
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0B1A12),
      body: WebViewWidget(controller: _web),
    );
  }
}
