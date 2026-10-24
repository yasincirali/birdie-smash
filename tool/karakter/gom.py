# Bir oyuncu modelinin sprite ve 360° verisini oyuna gömer (2026-10-24, iki model seçeneği).
# Her model index.html'de ayrı bir <script type="application/json" id="bs_model_<ad>"> bloğunda durur; oyun yalnız seçili
# modelin bloğunu JSON.parse eder (seçilmeyenin 3 MB'lık metni JS olarak ayrıştırılmaz, açılış aynı kalır).
# Kullanım: python3 -I gom.py <ad> <spr.js> <tur.js>     (spr.js: sheet.py, tur.js: tur_js.py çıktısı)
#           python3 -I gom.py --tasi sportif            (eski gömülü const SPR_*/TUR_* satırlarını bloğa taşır, bir kez)
import json, re, sys
P='assets/game/index.html'

def js_nesne(satir):   # "const X=...;" → Python nesnesi (TUR_SRC tek tırnaklı anahtar: değer)
    v=satir.split('=',1)[1].rstrip().rstrip(';')
    try: return json.loads(v)
    except ValueError: return json.loads(re.sub(r"(\w+):'([^']*)'",r'"\1":"\2"',v))

def blok(ad,veri): return f'<script type="application/json" id="bs_model_{ad}">'+json.dumps(veri,separators=(',',':'))+'</script>'

def yaz(ad,veri,s):
    b=blok(ad,veri); m=re.search(rf'<script type="application/json" id="bs_model_{ad}">.*?</script>',s,re.S)
    if m: return s[:m.start()]+b+s[m.end():]
    i=s.index('\n<script>\n'); return s[:i]+'\n'+b+s[i:]

s=open(P).read()
if sys.argv[1]=='--tasi':
    L=s.split('\n'); al={}
    for k in ('SPR_META','SPR_SRC','TUR_META','TUR_SRC'):
        i=next(i for i,l in enumerate(L) if l is not None and l.startswith(f'const {k}=')); al[k]=js_nesne(L[i]); L[i]=None
    s='\n'.join(l for l in L if l is not None)
    s=yaz(sys.argv[2],{'spr':{'meta':al['SPR_META'],'src':al['SPR_SRC']},'tur':{'meta':al['TUR_META'],'src':al['TUR_SRC']}},s)
else:
    ad,spr,tur=sys.argv[1:4]; a=open(spr).read().strip().split('\n'); t=open(tur).read().strip().split('\n')
    s=yaz(ad,{'spr':{'meta':js_nesne(a[0]),'src':js_nesne(a[1])},'tur':{'meta':js_nesne(t[0]),'src':js_nesne(t[1])}},s)
open(P,'w').write(s); print('yazıldı',len(s)//1024,'KB')
