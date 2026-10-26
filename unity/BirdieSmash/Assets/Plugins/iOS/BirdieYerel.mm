// Birdie Smash yerel köprüsü (iOS). Unity'de karşılığı olmayan üç sistem hizmeti; C# tarafı Game/Yerel.cs.
//   1) Titreşim deseni: HTML oyunu deseni ([ms, bekleme, ms, ...]) Flutter kabuğuna gönderiyordu, kabuk
//      (lib/main.dart _haptic) her darbeyi süresine göre UIImpactFeedbackGenerator'a çeviriyordu: ≥40 ms heavy,
//      ≥22 medium, ≥12 light, altı selection; tek indisler bekleme. Burada aynı çeviri, aynı eşikler: oyuncu HTML
//      sürümündeki dokuyu (vuruş türü, tel gerginliği, sıyırma, slice) aynen hisseder. Core Haptics gerekmedi.
//   2) Dynamic Type: sistemin tercih edilen yazı boyutu (UIContentSizeCategory) gövde metni oranı olarak (L = 1).
//   3) Hareketi azalt: UIAccessibilityIsReduceMotionEnabled (HTML prefers-reduced-motion karşılığı).
// ARC açık ya da kapalı derlenir: üreteçler bir kez ayrılır ve hiç bırakılmaz, bloklar yalnız sayı yakalar.
#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *bsAgir = nil, *bsOrta = nil, *bsHafif = nil;
static UISelectionFeedbackGenerator *bsSecim = nil;

static void bsHazirla(void)
{
    if (bsAgir != nil) return;
    bsAgir = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
    bsOrta = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
    bsHafif = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    bsSecim = [[UISelectionFeedbackGenerator alloc] init];
}

static void bsDarbe(int ms)
{
    if (ms <= 0) return;
    bsHazirla();
    if (ms >= 40) [bsAgir impactOccurred];
    else if (ms >= 22) [bsOrta impactOccurred];
    else if (ms >= 12) [bsHafif impactOccurred];
    else [bsSecim selectionChanged];
}

extern "C" void BirdieTitresim(const int *desen, int n)
{
    if (desen == NULL || n <= 0) return;
    if (n > 32) n = 32;
    double t = 0;
    for (int i = 0; i < n; i++)
    {
        int ms = desen[i];
        if (i % 2 == 1) { if (ms > 0) t += ms / 1000.0; continue; }
        if (ms <= 0) continue;
        if (t <= 0) { bsDarbe(ms); continue; }
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(t * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{ bsDarbe(ms); });
    }
}

// Gövde metni boyu / 17 pt (Apple HIG Dynamic Type tablosu: xS 14 … xxxL 23, erişilebilirlik 28 … 53).
extern "C" float BirdieYaziOlcek(void)
{
    NSString *c = [UIApplication sharedApplication].preferredContentSizeCategory;
    if (c == nil) return 1;
    if ([c isEqualToString:UIContentSizeCategoryExtraSmall]) return 14 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategorySmall]) return 15 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryMedium]) return 16 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryLarge]) return 1;
    if ([c isEqualToString:UIContentSizeCategoryExtraLarge]) return 19 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryExtraExtraLarge]) return 21 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryExtraExtraExtraLarge]) return 23 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryAccessibilityMedium]) return 28 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryAccessibilityLarge]) return 33 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryAccessibilityExtraLarge]) return 40 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryAccessibilityExtraExtraLarge]) return 47 / 17.0f;
    if ([c isEqualToString:UIContentSizeCategoryAccessibilityExtraExtraExtraLarge]) return 53 / 17.0f;
    return 1;
}

extern "C" int BirdieHareketAzalt(void)
{
    return UIAccessibilityIsReduceMotionEnabled() ? 1 : 0;
}

// Düşük güç modu (2026-10-26, ProMotion 120 Hz önerisi): açıkken maç 60 Hz'de kalır (pil isteği kullanıcının).
extern "C" int BirdieDusukGuc(void)
{
    return [[NSProcessInfo processInfo] isLowPowerModeEnabled] ? 1 : 0;
}
