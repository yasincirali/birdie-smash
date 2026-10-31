// Açılış perdesi, motor açılmadan (2026-10-30, yasin: "Splash gifi ilk açıldığı andan itibaren başlamalı").
// Unity'nin ilk karesi motor ve ilk sahne yüklenince gelir (telefonda bir iki saniye); o ana kadar ekranda yalnız düz
// açılış rengi duruyordu, dönen top ancak sonra başlıyordu. Burada UnityAppController'ın alt sınıfı uygulama açılır açılmaz
// Unity penceresinin üstüne dokunuş almayan ikinci bir pencere koyar: Hud perdesinin birebir aynısı (beş halka, aynı
// boyutlar ve dönüş: dış halka R = köşegen × .55, iç halkalar R × .62 × [.86 .66 .47 .30], 1,2 rad/sn, tekler ters yönde)
// ve ortada HTML'deki çizilmiş tüy top (16 kare, eksen dönüşü 2,4 rad/sn, mantar 44°'de sağ altta; Hud.SeritCiz ile aynı
// geometri). Görseller StreamingAssets'ten (Data/Raw): tuy_don.png Resources/ui ile aynı dosya, perde_N.png Hud.PerdeDoku'nun
// ürettiği dokunun PNG'si (scratchpad üreteci, Unity karesiyle ortalama fark 0,45/255).
// Dönüşler Core Animation'da: ana iş parçacığı motor açılışıyla meşgulken de render sunucusu çizer, top takılmaz.
// Devir: C# (Hud.PerdeTick, Yukleyici) her kare BirdieAcilisSure()'yi okur ve perde açısını, top fazını bu süreden yazar;
// Unity perdesi kesintisiz birkaç kare çizince BirdieAcilisBitir() pencereyi kaldırır (altında aynı görüntü). C# hiç
// çağırmazsa 15 sn sonra kendisi kalkar. Hareketi azalt: dönme yok, tek kare (Unity'deki gibi).
// Açılış ekranının kendisi (LaunchScreen) düz renk + aynı topun 0. karesi (CiBuild.IosAcilis); bu pencere ilk karede
// aynı topu aynı yerde gösterir, halkalar .25 sn'de belirir.
// 2026-10-31 akşam (yasin: "splash gifimiz yine geç giriyor ... tıklanır tıklanmaz ... maça geçiş anı ... takılma olmamalı"):
// (a) Sistem açılış ekranı didFinishLaunching dönene dek ekranda kalır; perde onun içinde kurulsa da Unity'nin motor
//     başlatması (super, il2cpp ve veri yükleme) bitene dek görünmüyordu. Klasik yaşam döngüsünde super artık bir sonraki
//     çevrime ertelenir: önce perde görünür, sonra motor başlar (ana iş parçacığı meşgulken dönüşleri render sunucusu çizer).
//     Arada gelen yaşam döngüsü çağrıları super hazır olunca sırayla iletilir. Sahne tabanlı yaşam döngüsünde (Info.plist
//     UIApplicationSceneManifest) ya da iki ertelenmiş açılış Unity'ye ulaşmadan bittiyse eski eşzamanlı yol.
// (b) Aynı perde maça geçişte de kullanılır (BirdieAcilisGoster): Unity perdesi kapandığı anda yerel perde aynı açıyla
//     üstüne konur, maç kurulurken (figür, ağ, ses, gölgelendirici derlemesi) halkalar takılmadan döner; Unity perdeyi
//     ancak kareler akıcılaşınca açar ve o anda yerel perde kalkar.
#import "UnityAppController.h"
#import <QuartzCore/QuartzCore.h>

@interface BsPerdeVC : UIViewController
@end
@implementation BsPerdeVC
- (BOOL)prefersStatusBarHidden { return YES; }
- (BOOL)prefersHomeIndicatorAutoHidden { return YES; }
@end

static UIWindow *bsPerde = nil;
static CFTimeInterval bsT0 = -1;
static int bsNesil = 0;   // zaman aşımı eski bir perdeyi değil yalnız kendi perdesini kaldırsın
static NSString *const BS_ERTELE = @"bs_acilis_ertele_bekleyen";   // ertelenmiş açılış Unity'ye ulaşmadı sayacı (NSUserDefaults)

static UIImage *bsResim(NSString *ad)
{
    NSString *p = [[[[NSBundle mainBundle] bundlePath] stringByAppendingPathComponent:@"Data/Raw"] stringByAppendingPathComponent:ad];
    return [UIImage imageWithContentsOfFile:p];
}

static void bsBitir(void)
{
    if (bsPerde == nil) return;
    bsPerde.hidden = YES; bsPerde.rootViewController = nil; bsPerde = nil; bsT0 = -1;
}

// aci: halkaların başlangıç açısı (rad, Unity perdeAci ile aynı); topGecikme: top ve yazı bu kadar sn sonra belirir (0: hemen,
// < 0: hiç); yazi: topun altındaki etiket (nil: yok); yaziOlcek: Ayar.YaziK. Açılışta (aci 0, ilk kurulum) halkalar .25 sn'de belirir.
static void bsPerdeKur(UIWindow *unity, CGFloat aci, CGFloat topGecikme, NSString *yazi, CGFloat yaziOlcek, BOOL belir)
{
    UIImage *top = bsResim(@"tuy_don.png");
    if (top == nil) return;   // dosya yoksa eski davranış: Unity perdesi ilk karesinde başlar
    CGRect b = [UIScreen mainScreen].bounds; CGFloat W = b.size.width, H = b.size.height, m = MIN(W, H);
    BOOL rm = UIAccessibilityIsReduceMotionEnabled();
    UIWindow *w = nil;
    if (@available(iOS 13.0, *))
    {
        if (unity != nil && unity.windowScene != nil) w = [[UIWindow alloc] initWithWindowScene:unity.windowScene];
        else for (UIScene *s in [UIApplication sharedApplication].connectedScenes)
            if ([s isKindOfClass:[UIWindowScene class]]) { w = [[UIWindow alloc] initWithWindowScene:(UIWindowScene *)s]; break; }
    }
    if (w == nil) w = [[UIWindow alloc] initWithFrame:b];
    w.frame = b; w.windowLevel = UIWindowLevelNormal + 1; w.userInteractionEnabled = NO;
    w.backgroundColor = [UIColor colorWithRed:0xa3 / 255.0 green:0x23 / 255.0 blue:0x4b / 255.0 alpha:1];
    BsPerdeVC *vc = [[BsPerdeVC alloc] init]; vc.view.backgroundColor = [UIColor clearColor]; vc.view.frame = b; w.rootViewController = vc;
    CALayer *kok = vc.view.layer; CFTimeInterval simdi = CACurrentMediaTime(), t0 = simdi - (rm ? 0 : aci / 1.2);   // t0: dönüşlerin sıfır anı (açı süreden yazılır)

    // halkalar (Hud.PerdeCiz): sıra dıştan içe, her biri merkezde
    CGFloat R = sqrt(W * W + H * H) * .55, rr = R * .62; const CGFloat F[5] = { 0, .86, .66, .47, .30 };
    for (int i = 0; i < 5; i++)
    {
        UIImage *h = bsResim([NSString stringWithFormat:@"perde_%d.png", i]); if (h == nil) continue;
        CGFloat r = i == 0 ? R : F[i] * rr;
        CALayer *L = [CALayer layer]; L.contents = (__bridge id)h.CGImage; L.bounds = CGRectMake(0, 0, 2 * r, 2 * r); L.position = CGPointMake(W / 2, H / 2);
        L.minificationFilter = kCAFilterLinear; L.magnificationFilter = kCAFilterLinear;
        if (!rm)
        {
            CABasicAnimation *d = [CABasicAnimation animationWithKeyPath:@"transform.rotation.z"];
            d.fromValue = @0; d.toValue = @((i % 2 == 1 ? -1 : 1) * M_PI * 2); d.duration = M_PI * 2 / 1.2; d.repeatCount = HUGE_VALF; d.beginTime = t0;
            d.removedOnCompletion = NO; [L addAnimation:d forKey:@"don"];
            if (belir) { CABasicAnimation *g = [CABasicAnimation animationWithKeyPath:@"opacity"]; g.fromValue = @0; g.toValue = @1; g.duration = .25; g.beginTime = simdi; [L addAnimation:g forKey:@"gir"]; }
        }
        [kok addSublayer:L];
    }

    // top (Hud.TopDurum açılış + SeritCiz): p = merkez − .026·m yukarı, kare boyu k = .3·m, mantar p + yön·.56k, kare
    // mantar çevresinde yön + 135° döner; karede mantar (39,36), kare 300×299, şerit 4×4.
    CGFloat yon = 44 * M_PI / 180, k = round(m * .3), s = k / 300;
    CGPoint p = CGPointMake(round(W / 2), round(H / 2 - m * .026)), cork = CGPointMake(p.x + cos(yon) * .56 * k, p.y + sin(yon) * .56 * k);
    CALayer *T = [CALayer layer]; T.contents = (__bridge id)top.CGImage; T.bounds = CGRectMake(0, 0, 300 * s, 299 * s);
    T.anchorPoint = CGPointMake(39 / 300.0, 36 / 299.0); T.position = cork; T.transform = CATransform3DMakeRotation(yon + M_PI * 135 / 180, 0, 0, 1);
    T.contentsRect = CGRectMake(0, 0, .25, .25); T.minificationFilter = kCAFilterTrilinear; T.magnificationFilter = kCAFilterLinear;
    if (!rm)
    {
        NSMutableArray *v = [NSMutableArray arrayWithCapacity:16]; NSMutableArray *kt = [NSMutableArray arrayWithCapacity:17];
        for (int fi = 0; fi < 16; fi++) { [v addObject:[NSValue valueWithCGRect:CGRectMake((fi % 4) * .25, (fi / 4) * .25, .25, .25)]]; [kt addObject:@(fi / 16.0)]; }
        [v addObject:v[0]]; [kt addObject:@1];
        CAKeyframeAnimation *a = [CAKeyframeAnimation animationWithKeyPath:@"contentsRect"];
        a.values = v; a.keyTimes = kt; a.calculationMode = kCAAnimationDiscrete; a.duration = (M_PI * 2 / 16) / 2.4; a.repeatCount = HUGE_VALF; a.beginTime = t0;
        a.removedOnCompletion = NO; [T addAnimation:a forKey:@"don"];
    }
    if (topGecikme >= 0) [kok addSublayer:T];

    // "Hazırlanıyor…" (Hud.PerdeCiz perdeBek): topun altında, koyu bordo zemin üstünde kalın yazı; Unity'deki boyutlar
    // (u = min(H/640, W/340), yazı 13·u·YaziK, yatay pay 14·u, dikey 10·u), aynı gecikmeyle belirir.
    CALayer *Y = nil;
    if (topGecikme >= 0 && yazi.length > 0)
    {
        CGFloat u = MIN(H / 640, W / 340); UIFont *f = [UIFont boldSystemFontOfSize:13 * u * (yaziOlcek > 0 ? yaziOlcek : 1)];
        UILabel *lb = [[UILabel alloc] init]; lb.text = yazi; lb.font = f; lb.textColor = [UIColor whiteColor]; lb.textAlignment = NSTextAlignmentCenter;
        CGSize sz = [lb sizeThatFits:CGSizeMake(W, H)];
        lb.frame = CGRectMake(W / 2 - sz.width / 2 - 14 * u, H / 2 + k * .85, sz.width + 28 * u, sz.height + 10 * u);
        lb.backgroundColor = [UIColor colorWithRed:74 / 255.0 green:10 / 255.0 blue:34 / 255.0 alpha:.88];
        [vc.view addSubview:lb]; Y = lb.layer;
    }
    if (topGecikme > 0)
    {   // render sunucusunda bekleyen görünmezlik: ana iş parçacığı maç kurarken meşgul olsa da zamanında belirir
        for (CALayer *x in Y != nil ? @[T, Y] : @[T])
        {
            CABasicAnimation *g = [CABasicAnimation animationWithKeyPath:@"opacity"]; g.fromValue = @0; g.toValue = @0; g.duration = topGecikme; g.beginTime = simdi;
            [x addAnimation:g forKey:@"bekle"];
        }
    }

    w.hidden = NO; bsPerde = w; bsT0 = t0; int nesil = ++bsNesil;
    [CATransaction flush];   // motor açılışı ana iş parçacığını tutmadan önce ilk çizim gitsin
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(15 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{ if (nesil == bsNesil) bsBitir(); });
}

static BOOL bsSahneli(void) { return [[NSBundle mainBundle] objectForInfoDictionaryKey:@"UIApplicationSceneManifest"] != nil; }
// Ertelenmiş açılış güvenli mi: iki kez üst üste Unity'ye ulaşmadan bittiyse (çöküş ya da takılma) eski eşzamanlı yol.
static BOOL bsErteleOlur(void)
{
    if (bsSahneli()) return NO;
    NSUserDefaults *d = [NSUserDefaults standardUserDefaults]; NSInteger n = [d integerForKey:BS_ERTELE];
    if (n >= 2) return NO;
    [d setInteger:n + 1 forKey:BS_ERTELE]; return YES;
}

@interface BirdieAppController : UnityAppController
@end
@implementation BirdieAppController
{
    BOOL bsHazir, bsAktifBekliyor; NSDictionary *bsSecenek;
}
- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
    // 2026-10-31 (yasin: "splash'te gife hemen girmiyor, 1,5–2 sn siyah bekletiyor"): perde super'den ÖNCE görünür.
    bsPerdeKur(nil, 0, 0, nil, 1, YES);   // UnityFramework Objective-C istisnaları kapalı derlenir (@try yok); dosya yoksa sessizce döner
    if (bsPerde != nil && bsErteleOlur())
    {
        // (a) super bir sonraki çevrimde: bu yöntem dönünce sistem açılış ekranı kalkar ve dönen perde görünür. Gecikme
        // kısa bir zamanlayıcı (dispatch_async aynı çevrimde, çizimden önce koşabilir).
        bsHazir = NO; bsSecenek = launchOptions;
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(.03 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{ [self bsBaslat:application]; });
        return YES;
    }
    bsHazir = YES;
    BOOL r = [super application:application didFinishLaunchingWithOptions:launchOptions];
    [self bsOneAl];
    return r;
}
- (void)bsOneAl
{
    if (bsPerde == nil) { bsPerdeKur(self.window, 0, 0, nil, 1, YES); return; }
    if (@available(iOS 13.0, *)) { if (bsPerde.windowScene == nil && self.window.windowScene != nil) bsPerde.windowScene = self.window.windowScene; }
    bsPerde.hidden = NO; [CATransaction flush];
}
- (void)bsBaslat:(UIApplication *)application
{
    [super application:application didFinishLaunchingWithOptions:bsSecenek];
    bsSecenek = nil; bsHazir = YES; [self bsOneAl];
    // arada etkinleştiyse Unity'ye şimdi bildir (Unity motoru didBecomeActive'te başlatır)
    if (bsAktifBekliyor && application.applicationState == UIApplicationStateActive) [super applicationDidBecomeActive:application];
    bsAktifBekliyor = NO;
}
- (void)applicationDidBecomeActive:(UIApplication *)application { if (!bsHazir) { bsAktifBekliyor = YES; return; } [super applicationDidBecomeActive:application]; }
- (void)applicationWillResignActive:(UIApplication *)application { if (!bsHazir) { bsAktifBekliyor = NO; return; } [super applicationWillResignActive:application]; }
- (void)applicationDidEnterBackground:(UIApplication *)application { if (!bsHazir) return; [super applicationDidEnterBackground:application]; }
- (void)applicationWillEnterForeground:(UIApplication *)application { if (!bsHazir) return; [super applicationWillEnterForeground:application]; }
@end
IMPL_APP_CONTROLLER_SUBCLASS(BirdieAppController)

// Yerel perde açıkken açıldığından beri geçen süre (sn), kapalıysa −1. C# açıyı (×1,2) ve top fazını (×2,4) bundan yazar.
extern "C" float BirdieAcilisSure(void)
{
    // C# her kare çağırır: ilk çağrı Unity'nin koştuğunu gösterir; ertelenmiş açılış sayacı sıfırlanır.
    static BOOL ulasti = NO; if (!ulasti) { ulasti = YES; [[NSUserDefaults standardUserDefaults] removeObjectForKey:BS_ERTELE]; }
    return bsPerde == nil || bsT0 < 0 ? -1 : (float)(CACurrentMediaTime() - bsT0);
}

// Unity perdeyi açarken çağırır: yerel perde kalkar; Unity'ye ulaşıldığı için ertelenmiş açılış sayacı sıfırlanır.
extern "C" void BirdieAcilisBitir(void)
{
    [[NSUserDefaults standardUserDefaults] removeObjectForKey:BS_ERTELE];
    if ([NSThread isMainThread]) bsBitir(); else dispatch_async(dispatch_get_main_queue(), ^{ bsBitir(); });
}

// Maça geçiş (Hud perdesi kapandı): aynı perde aynı açıyla, top ve yazı topGecikme sn sonra. Unity'nin ana iş parçacığından.
extern "C" void BirdieAcilisGoster(float aci, float topGecikme, const char *yazi, float yaziOlcek)
{
    NSString *y = yazi != NULL ? [NSString stringWithUTF8String:yazi] : nil;
    bsBitir();
    UIWindow *unity = nil; id del = [UIApplication sharedApplication].delegate;
    if ([del isKindOfClass:[UnityAppController class]]) unity = ((UnityAppController *)del).window;
    bsPerdeKur(unity, aci, topGecikme, y, yaziOlcek, NO);
}
