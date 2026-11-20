// Online taşıma köprüsü (2026-11-20, yasin: "ücretsiz bir hali yok mu" → Game Center): GameKit'in GKMatch'i iki telefon
// arasında bayt taşır, eşleştirir ve arkadaş davetini verir; sunucu ve ücret yok. C# tarafı Game/Online.cs, oturum kuralları
// Core/Oturum.cs. Bu dosya yalnız taşır: maç kuralı, kura, gecikme ölçümü burada değil.
// Arayüz Apple'ın kendi eşleştirme sayfası (GKMatchmakerViewController): arkadaş çağırma ve "şimdi oyna" onun içinde,
// ayrı lobi ekranı ve kod yazılmadı.
// C# her karede sorar (BirdieOnlineDurum / BirdieOnlineAl); geri çağrı yok, IL2CPP sıralaması gerekmez. GameKit temsilci
// çağrıları ve Unity döngüsü ana iş parçacığında; kuyruk yine de kilitli (GameKit bunu belgede söz vermiyor).
// Gönderim hep güvenilir kipte: Core/Uzak sıralı ve kayıpsız taşıma varsayar.
// shortcut: konum paketi de güvenilir gidiyor (10 Hz, kayıpta sıra bekletir); takılma ölçülürse konum güvenilmez kipe alınır.
#import <GameKit/GameKit.h>
#import "UnityAppController.h"

// Durum: 0 boş, 1 eşleştirme sayfası açık, 2 bağlı (iki oyuncu da hazır), 3 bitti (iptal, hata ya da rakip koptu)
@interface BirdieOnline : NSObject <GKMatchmakerViewControllerDelegate, GKMatchDelegate, GKLocalPlayerListener>
@property (nonatomic, strong) GKMatch *mac;
@property (nonatomic, strong) NSMutableArray<NSData *> *gelen;
@property (nonatomic, assign) int durum;
@property (nonatomic, assign) BOOL dinliyor;
@end

@implementation BirdieOnline

+ (BirdieOnline *)tek
{
    static BirdieOnline *t; static dispatch_once_t bir;
    dispatch_once(&bir, ^{ t = [BirdieOnline new]; t.gelen = [NSMutableArray array]; });
    return t;
}

- (void)sayfaAc:(GKMatchmakerViewController *)vc
{
    if (vc == nil) { self.durum = 3; return; }
    vc.matchmakerDelegate = self; self.durum = 1;
    [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
}

- (void)ara
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) { self.durum = 3; return; }
    [self kapat];
    GKMatchRequest *r = [GKMatchRequest new]; r.minPlayers = 2; r.maxPlayers = 2;
    [self sayfaAc:[[GKMatchmakerViewController alloc] initWithMatchRequest:r]];
}

- (void)kapat
{
    if (self.mac != nil) { self.mac.delegate = nil; [self.mac disconnect]; self.mac = nil; }
    @synchronized (self.gelen) { [self.gelen removeAllObjects]; }
    self.durum = 0;
}

- (void)hazirMi { if (self.mac != nil && self.mac.expectedPlayerCount == 0) self.durum = 2; }

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
