// Bulut kaydı köprüsü (2026-11-01): iCloud anahtar-değer deposu (NSUbiquitousKeyValueStore). C# tarafı Game/Bulut.cs, karar
// mantığı Core/BulutKayit.cs. Depo 1 MB; ilerleme görüntüsü onun çok altında. Uygulama iCloud (Key-value storage)
// yetkisini taşımıyorsa (App ID'de açılmamış, profil yenilenmemiş) depo yalnız bu cihazda bir sözlük gibi davranır, hiçbir
// şey kırılmaz; BirdieBulutAcik() o durumda 0 döner ki oyun "buluta yedeklendi" demesin.
// Dönen metin malloc'la kopyalanır: Unity IL2CPP sıralayıcısı onu kendisi serbest bırakır.
#import <Foundation/Foundation.h>

static char *bsKopya(NSString *s)
{
    if (s == nil) return NULL;
    const char *u = [s UTF8String]; if (u == NULL) return NULL;
    char *c = (char *)malloc(strlen(u) + 1); strcpy(c, u); return c;
}

extern "C" {

int BirdieBulutAcik(void)
{
    // ubiquityIdentityToken: oturum açık iCloud hesabı + iCloud yetkisi; KV deposu ayrıca yetki ister, ikisi aynı profilden gelir.
    return [[NSFileManager defaultManager] ubiquityIdentityToken] != nil ? 1 : 0;
}

char *BirdieBulutOku(const char *anahtar)
{
    if (anahtar == NULL) return NULL;
    NSUbiquitousKeyValueStore *d = [NSUbiquitousKeyValueStore defaultStore];
    [d synchronize];
    return bsKopya([d stringForKey:[NSString stringWithUTF8String:anahtar]]);
}

void BirdieBulutYaz(const char *anahtar, const char *deger)
{
    if (anahtar == NULL || deger == NULL) return;
    NSUbiquitousKeyValueStore *d = [NSUbiquitousKeyValueStore defaultStore];
    [d setString:[NSString stringWithUTF8String:deger] forKey:[NSString stringWithUTF8String:anahtar]];
    [d synchronize];
}

}
