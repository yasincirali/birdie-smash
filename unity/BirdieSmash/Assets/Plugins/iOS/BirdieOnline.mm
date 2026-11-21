// Online taşıma köprüsü (2026-11-20, yasin: "ücretsiz bir hali yok mu" → Game Center): GameKit'in GKMatch'i iki telefon
// arasında bayt taşır, eşleştirir ve arkadaş davetini verir; sunucu ve ücret yok. C# tarafı Game/Online.cs, oturum kuralları
// Core/Oturum.cs. Bu dosya yalnız taşır: maç kuralı, kura, gecikme ölçümü burada değil.
// Arayüz Apple'ın kendi eşleştirme sayfası (GKMatchmakerViewController): arkadaş çağırma ve "şimdi oyna" onun içinde,
// ayrı lobi ekranı ve kod yazılmadı.
// C# her karede sorar (BirdieOnlineDurum / BirdieOnlineAl); geri çağrı yok, IL2CPP sıralaması gerekmez. GameKit temsilci
// çağrıları ve Unity döngüsü ana iş parçacığında; kuyruk yine de kilitli (GameKit bunu belgede söz vermiyor).
// Gönderim hep güvenilir kipte: Core/Uzak sıralı ve kayıpsız taşıma varsayar.
// 2026-11-21 (yasin: "arkadaşını ekleyebilsin, oyunda mı görebilsin, oyundaysa ona oynama isteği gönderebilsin"): arkadaş
// listesi (loadFriends, iOS 14.5+, Info.plist NSGKFriendListUsageDescription CiBuild'de), arkadaşa doğrudan davet (eşleştirme
// sayfası alıcı seçili açılır; davetin gittiği, kabul ya da reddedildiği Apple'ın sayfasında görünür) ve arkadaş ekleme (iOS
// 15.4+ Apple'ın arkadaşlık isteği sayfası, eskisinde Game Center arkadaş listesi). Game Center arkadaşın o an oyunda olup
// olmadığını vermez (çevrimiçi durumu API'de yok); davet telefonuna bildirim olarak gider, kabul ederse maç kurulur.
// shortcut: konum paketi de güvenilir gidiyor (10 Hz, kayıpta sıra bekletir); takılma ölçülürse konum güvenilmez kipe alınır.
#import <GameKit/GameKit.h>
#import <UIKit/UIKit.h>

// Durum: 0 boş, 1 eşleştirme sayfası açık, 2 bağlı (iki oyuncu da hazır), 3 bitti (iptal, hata ya da rakip koptu)
@interface BirdieOnline : NSObject <GKMatchmakerViewControllerDelegate, GKMatchDelegate, GKLocalPlayerListener, GKGameCenterControllerDelegate>
@property (nonatomic, strong) GKMatch *mac;
@property (nonatomic, strong) NSMutableArray<NSData *> *gelen;
@property (nonatomic, assign) int durum;
@property (nonatomic, assign) BOOL dinliyor;
// Arkadaş listesi: 0 hiç istenmedi, 1 yükleniyor, 2 hazır, 3 izin yok (oyuncu reddetti ya da kısıtlı), 4 kullanılamıyor
@property (nonatomic, strong) NSArray<GKPlayer *> *arkadaslar;
@property (nonatomic, assign) int arkDurum;
@end

@implementation BirdieOnline

+ (BirdieOnline *)tek
{
    static BirdieOnline *t; static dispatch_once_t bir;
    dispatch_once(&bir, ^{ t = [BirdieOnline new]; t.gelen = [NSMutableArray array]; });
    return t;
}

// Kök denetleyici BirdieYerel.mm (BirdiePaylas) ile aynı yoldan; üstte açık bir sayfa varsa onun üstünden sunulur
// (ikinci sunum yoksa sessizce düşer).
- (UIViewController *)kok
{
    UIViewController *kok = nil;
    for (UIScene *sc in [UIApplication sharedApplication].connectedScenes)
        if ([sc isKindOfClass:[UIWindowScene class]]) for (UIWindow *w in ((UIWindowScene *)sc).windows) if (w.isKeyWindow) kok = w.rootViewController;
    while (kok != nil && kok.presentedViewController != nil) kok = kok.presentedViewController;
    return kok;
}

- (void)sayfaAc:(GKMatchmakerViewController *)vc
{
    if (vc == nil) { self.durum = 3; return; }
    UIViewController *kok = [self kok];
    if (kok == nil) { self.durum = 3; return; }
    vc.matchmakerDelegate = self; self.durum = 1;
    [kok presentViewController:vc animated:YES completion:nil];
}

- (void)ara
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) { self.durum = 3; return; }
    [self kapat];
    GKMatchRequest *r = [GKMatchRequest new]; r.minPlayers = 2; r.maxPlayers = 2;
    [self sayfaAc:[[GKMatchmakerViewController alloc] initWithMatchRequest:r]];
}

// Arkadaşa doğrudan davet: alıcı seçili eşleştirme sayfası. Oyuncu listede yoksa (liste yenilendi) boş sayfa açılır.
- (void)davet:(NSString *)pid mesaj:(NSString *)mesaj
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) { self.durum = 3; return; }
    GKPlayer *p = nil;
    for (GKPlayer *f in self.arkadaslar) if ([f.gamePlayerID isEqualToString:pid]) { p = f; break; }
    [self kapat];
    GKMatchRequest *r = [GKMatchRequest new]; r.minPlayers = 2; r.maxPlayers = 2;
    if (p != nil) r.recipients = @[p];
    if (mesaj.length > 0) r.inviteMessage = mesaj;
    [self sayfaAc:[[GKMatchmakerViewController alloc] initWithMatchRequest:r]];
}

- (void)arkadasYukle
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) { self.arkDurum = 4; return; }
    if (@available(iOS 14.5, *))
    {
        self.arkDurum = 1;
        [[GKLocalPlayer localPlayer] loadFriendsAuthorizationStatus:^(GKFriendsAuthorizationStatus st, NSError *e1)
        {
            if (st == GKFriendsAuthorizationStatusDenied || st == GKFriendsAuthorizationStatusRestricted)
            { dispatch_async(dispatch_get_main_queue(), ^{ self.arkDurum = 3; }); return; }
            // Belirlenmemişse bu çağrı sistemin izin penceresini açar.
            [[GKLocalPlayer localPlayer] loadFriends:^(NSArray<GKPlayer *> *liste, NSError *e2)
            {
                dispatch_async(dispatch_get_main_queue(), ^{
                    if (e2 != nil) { self.arkDurum = e2.code == GKErrorFriendListDenied ? 3 : 4; return; }
                    self.arkadaslar = liste ?: @[]; self.arkDurum = 2;
                });
            }];
        }];
    }
    else self.arkDurum = 4;
}

- (void)arkadasEkle
{
    UIViewController *kok = [self kok];
    if (kok == nil || ![GKLocalPlayer localPlayer].isAuthenticated) return;
    if (@available(iOS 15.4, *))
    {
        NSError *e = nil;
        if ([[GKLocalPlayer localPlayer] presentFriendRequestCreatorFromViewController:kok error:&e] && e == nil) return;
    }
    if (@available(iOS 14.0, *))
    {
        GKGameCenterViewController *vc = [[GKGameCenterViewController alloc] initWithState:GKGameCenterViewControllerStateLocalPlayerFriendsList];
        vc.gameCenterDelegate = self;
        [kok presentViewController:vc animated:YES completion:nil];
    }
}
- (void)gameCenterViewControllerDidFinish:(GKGameCenterViewController *)vc { [vc dismissViewControllerAnimated:YES completion:nil]; }

- (void)kapat
{
    if (self.mac != nil) { self.mac.delegate = nil; [self.mac disconnect]; self.mac = nil; }
    @synchronized (self.gelen) { [self.gelen removeAllObjects]; }
    self.durum = 0;
}

// Bitti (3) kalıcıdır: kopmadan sonra gelen bir durum olayı bağlantıyı yeniden "bağlı" göstermesin.
- (void)hazirMi { if (self.durum != 3 && self.mac != nil && self.mac.expectedPlayerCount == 0) self.durum = 2; }

// ---- eşleştirme sayfası ----
- (void)matchmakerViewControllerWasCancelled:(GKMatchmakerViewController *)vc
{
    [vc dismissViewControllerAnimated:YES completion:nil]; self.durum = 3;
}
- (void)matchmakerViewController:(GKMatchmakerViewController *)vc didFailWithError:(NSError *)hata
{
    [vc dismissViewControllerAnimated:YES completion:nil]; self.durum = 3;
}
- (void)matchmakerViewController:(GKMatchmakerViewController *)vc didFindMatch:(GKMatch *)match
{
    [vc dismissViewControllerAnimated:YES completion:nil];
    self.mac = match; match.delegate = self; [self hazirMi];
}

// ---- maç ----
- (void)match:(GKMatch *)match didReceiveData:(NSData *)data fromRemotePlayer:(GKPlayer *)player
{
    if (match != self.mac || data == nil) return;
    @synchronized (self.gelen) { [self.gelen addObject:data]; }
}
- (void)match:(GKMatch *)match player:(GKPlayer *)player didChangeConnectionState:(GKPlayerConnectionState)state
{
    if (match != self.mac) return;
    if (state == GKPlayerStateDisconnected) self.durum = 3; else [self hazirMi];
}
- (void)match:(GKMatch *)match didFailWithError:(NSError *)hata { if (match == self.mac) self.durum = 3; }

// ---- gelen davet (arkadaş çağırdı; uygulama açıkken ya da bildirimden açılınca) ----
- (void)player:(GKPlayer *)player didAcceptInvite:(GKInvite *)invite
{
    [self kapat];
    [self sayfaAc:[[GKMatchmakerViewController alloc] initWithInvite:invite]];
}

@end

extern "C" {

// Oturum açık oyuncu için davet dinleyicisini kaydeder (bir kez). C# girişten sonra çağırır.
void BirdieOnlineDinle(void)
{
    BirdieOnline *o = [BirdieOnline tek];
    if (o.dinliyor || ![GKLocalPlayer localPlayer].isAuthenticated) return;
    [[GKLocalPlayer localPlayer] registerListener:o]; o.dinliyor = YES;
}

void BirdieOnlineAra(void) { [[BirdieOnline tek] ara]; }

// Arkadaşlar: yükle (sonuç BirdieOnlineArkadasDurum), sonra "gamePlayerID\tad\n..." UTF-8 tampona; tampon yetmezse sığan kadar.
void BirdieOnlineArkadasYukle(void) { [[BirdieOnline tek] arkadasYukle]; }
int BirdieOnlineArkadasDurum(void) { return [BirdieOnline tek].arkDurum; }
int BirdieOnlineArkadaslar(unsigned char *tampon, int kapasite)
{
    BirdieOnline *o = [BirdieOnline tek];
    if (tampon == NULL || kapasite <= 0 || o.arkadaslar == nil) return 0;
    NSMutableString *m = [NSMutableString string];
    NSCharacterSet *ayrac = [NSCharacterSet characterSetWithCharactersInString:@"\t\n\r"];
    for (GKPlayer *p in o.arkadaslar)
    {
        NSString *ham = p.displayName.length > 0 ? p.displayName : (p.alias ?: @"");
        NSString *ad = [[ham componentsSeparatedByCharactersInSet:ayrac] componentsJoinedByString:@" "];
        NSString *satir = [NSString stringWithFormat:@"%@\t%@\n", p.gamePlayerID ?: @"", ad];
        if ([m lengthOfBytesUsingEncoding:NSUTF8StringEncoding] + [satir lengthOfBytesUsingEncoding:NSUTF8StringEncoding] > (NSUInteger)kapasite) break;
        [m appendString:satir];
    }
    NSData *d = [m dataUsingEncoding:NSUTF8StringEncoding];
    memcpy(tampon, d.bytes, d.length);
    return (int)d.length;
}
void BirdieOnlineDavet(const char *pid, const char *mesaj)
{
    [[BirdieOnline tek] davet:(pid ? [NSString stringWithUTF8String:pid] : @"") mesaj:(mesaj ? [NSString stringWithUTF8String:mesaj] : @"")];
}
void BirdieOnlineArkadasEkle(void) { [[BirdieOnline tek] arkadasEkle]; }
int BirdieOnlineDurum(void) { return [BirdieOnline tek].durum; }
void BirdieOnlineKapat(void) { [[BirdieOnline tek] kapat]; }

void BirdieOnlineGonder(const unsigned char *bayt, int n)
{
    BirdieOnline *o = [BirdieOnline tek];
    if (o.mac == nil || bayt == NULL || n <= 0) return;
    NSError *hata = nil;
    [o.mac sendDataToAllPlayers:[NSData dataWithBytes:bayt length:(NSUInteger)n] withDataMode:GKMatchSendDataReliable error:&hata];
}

// Sıradaki mesajı tampona kopyalar, uzunluğunu döner; mesaj yoksa 0. Tampona sığmayan mesaj atılır (bizim en büyük
// mesajımız Merhaba, ~1 KB; daha büyüğü bizden gelmemiştir).
int BirdieOnlineAl(unsigned char *tampon, int kapasite)
{
    BirdieOnline *o = [BirdieOnline tek];
    if (tampon == NULL || kapasite <= 0) return 0;
    @synchronized (o.gelen)
    {
        while (o.gelen.count > 0)
        {
            NSData *d = o.gelen[0]; [o.gelen removeObjectAtIndex:0];
            if (d.length == 0 || d.length > (NSUInteger)kapasite) continue;
            memcpy(tampon, d.bytes, d.length);
            return (int)d.length;
        }
    }
    return 0;
}

}
