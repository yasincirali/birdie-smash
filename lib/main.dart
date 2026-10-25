import 'dart:convert';
import 'dart:io' show Platform;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:games_services/games_services.dart';
import 'package:webview_flutter/webview_flutter.dart';

import 'kabuk.dart';

// Birdie Smash: oyun tek bir HTML/canvas dosyasıdır (assets/game/index.html) ve
// prototip olarak https://claude.ai/artifact/4e8aG15tCWDe8hdtVCPryQ adresinde
// yaşar. Flutter kabuğu YALNIZ dört iş yapar: mağaza/TestFlight hattı, tam
// ekran WebView, haptic köprüsü ve platform hizmetleri köprüsü (Game Center, mağaza puanı, Unity'ye
// ilerleme devri).
// Oyun mantığı Dart'a taşınmaz; oyun değiştiğinde HTML dosyası yenilenir (tool/oyun_sozdizimi.js doğrular).
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
      ..addJavaScriptChannel('Oyun', onMessageReceived: _oyun)
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

  // Platform hizmetleri köprüsü (2026-10-22). Oyun ne zaman ne isteyeceğine
  // kendisi karar verir (giriş isteği ilk kariyer basamağından sonra gelir:
  // oyuncu oyunu sevmeden izin ekranı görmesin); kabuk yalnız iletir. Mesaj
  // {tip: ...} biçimindedir ki iCloud kaydı ({tip: 'kayit'}) aynı kanala
  // eklenebilsin. Yalnız iOS: Android hattı yok ve Play Games uygulama
  // kimliği tanımlı değil. Hata oyunu asla durdurmaz; başarım yerelde
  // tutulduğu için giriş sonradan yapılsa da gcDurum ile yeniden gönderilir.
  bool _gcAcik = false;
  static const _kanal = MethodChannel(kabukKanalAdi);
  String? _devirSon;

  Future<void> _oyun(JavaScriptMessage m) async {
    Map<String, dynamic> v;
    try {
      v = (jsonDecode(m.message) as Map).cast<String, dynamic>();
    } catch (_) {
      return;
    }
    // Mağaza puanı iOS kontrolünden önce (Ar-Ge 23 Eki #1): ne zaman isteneceğine oyun karar verir (kariyer
    // basamağı ya da 3. galibiyet, ömür boyu en çok 3), kabuk yalnız sistem penceresini çağırır. Android'de
    // yerel karşılık henüz yok (Android hattı yok); MissingPluginException sessiz geçer.
    if (v['tip'] == 'puan') {
      try {
        await _kanal.invokeMethod<void>('puanIste');
      } catch (_) {}
      return;
    }
    if (!Platform.isIOS) return;
    try {
      switch (v['tip']) {
        case 'devir':
          // HTML → Unity ilerleme devri: aynı içerik ikinci kez yazılmaz (oyun zaten değişmeyeni göndermez).
          final d = devirAyikla(v['veri']);
          if (d == null) break;
          final imza = jsonEncode(d);
          if (imza == _devirSon) break;
          await _kanal.invokeMethod<bool>('devirYaz', d);
          _devirSon = imza;
        case 'giris':
          if (!_gcAcik) {
            await GameAuth.signIn();
            _gcAcik = await GameAuth.isSignedIn;
          }
          if (_gcAcik) await _web.runJavaScript('gcDurum(true)');
        case 'basarimlar':
          final idler = (v['idler'] as List?)?.whereType<String>() ?? const [];
          for (final id in idler) {
            await _basarimAc(id);
          }
        case 'basarim':
          final id = v['id'];
          if (id is String) await _basarimAc(id);
        case 'basarimGoster':
          if (_gcAcik) await Achievements.showAchievements();
      }
    } catch (_) {
      // Game Center kapalı/iptal: oyun yerel ilerlemeyle sürer.
    }
  }

  Future<void> _basarimAc(String id) async {
    if (!_gcAcik) return;
    await Achievements.unlock(
      achievement: Achievement(iOSID: id, percentComplete: 100),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0B1A12),
      body: WebViewWidget(controller: _web),
    );
  }
}
