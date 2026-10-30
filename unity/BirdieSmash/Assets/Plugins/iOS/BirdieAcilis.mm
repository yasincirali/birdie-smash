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

static void bsPerdeKur(UIWindow *unity)
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
    CALayer *kok = vc.view.layer; CFTimeInterval simdi = CACurrentMediaTime();

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
            d.fromValue = @0; d.toValue = @((i % 2 == 1 ? -1 : 1) * M_PI * 2); d.duration = M_PI * 2 / 1.2; d.repeatCount = HUGE_VALF; d.beginTime = simdi;
            d.removedOnCompletion = NO; [L addAnimation:d forKey:@"don"];
            CABasicAnimation *g = [CABasicAnimation animationWithKeyPath:@"opacity"]; g.fromValue = @0; g.toValue = @1; g.duration = .25; g.beginTime = simdi; [L addAnimation:g forKey:@"gir"];
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
        a.values = v; a.keyTimes = kt; a.calculationMode = kCAAnimationDiscrete; a.duration = (M_PI * 2 / 16) / 2.4; a.repeatCount = HUGE_VALF; a.beginTime = simdi;
        a.removedOnCompletion = NO; [T addAnimation:a forKey:@"don"];
    }
    [kok addSublayer:T];

    w.hidden = NO; bsPerde = w; bsT0 = simdi;
    [CATransaction flush];   // motor açılışı ana iş parçacığını tutmadan önce ilk çizim gitsin
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(15 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{ bsBitir(); });
}

@interface BirdieAppController : UnityAppController
@end
@implementation BirdieAppController
- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
    BOOL r = [super application:application didFinishLaunchingWithOptions:launchOptions];
    @try { bsPerdeKur(self.window); } @catch (NSException *e) { bsPerde = nil; bsT0 = -1; }
    return r;
}
@end
IMPL_APP_CONTROLLER_SUBCLASS(BirdieAppController)

// Yerel perde açıkken açıldığından beri geçen süre (sn), kapalıysa −1. C# açıyı (×1,2) ve top fazını (×2,4) bundan yazar.
extern "C" float BirdieAcilisSure(void)
{
    return bsPerde == nil || bsT0 < 0 ? -1 : (float)(CACurrentMediaTime() - bsT0);
}

extern "C" void BirdieAcilisBitir(void)
{
    if ([NSThread isMainThread]) bsBitir(); else dispatch_async(dispatch_get_main_queue(), ^{ bsBitir(); });
}
