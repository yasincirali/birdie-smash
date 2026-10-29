using System; using System.Collections.Generic; using BirdieSmash.Core;
// Klip geçişi pop ölçümü (2026-10-27 performans turu 2, yasin: "animasyonlar her yerde smooth ... akıcı olmalı").
// PlayerView.Show'un klip değişimi kuralının taklidi (Unity'siz: Core bot maçı + Match.PickClip): önceki geçiş (0,08 sn,
// vuruştan dönüşte 0,2 sn) bitmeden yeni klip gelince ekrandaki karışımın ne kadarı TEK KAREDE düşüyor. Eski kural: tek
// prev (karışımın prev payı düşer). Yeni kural: aynı klibe dönüşte geçiş ters çevrilir, başka klipte cur ve prev birlikte
// söner (prev2). PlayerView'deki kural değişirse burası da değişmeli (iki yerde aynı kural; bilinçli kopya, ölçüm aracı).
// Ölçüt: yeni kuralda %25'ten büyük düşüş eski kuralın en çok %15'i. Çalıştırma: depo kökünden
//   mcs -out:$S/gecis.exe tool/unity_test/GecisOlc.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono $S/gecis.exe
static class GecisOlc {
  static float SS(float t){ t=Math.Max(0,Math.Min(1,t)); return t*t*(3-2*t); }
  static int Main() {
    int hata=0; var eski25=new Dictionary<int,int>();
    foreach (var hz in new[]{60,120}) foreach (var yeni in new[]{false,true}) {
      var rng = new Random(7); float dt=1f/hz; int sw=0, ara=0, d10=0, d25=0; double top=0;
      for (int m=0;m<6;m++){
        var M=new Match(11+m,new MatchOptions{easy=m%2==0,target=21});
        var cur=new string[2]; var prev=new string[2]; var prev2=new string[2]; var blend=new float[]{1,1}; var bs=new float[]{.08f,.08f}; var pay=new float[2];
        for(int i=0;i<hz*400&&!M.over;i++){
          if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .05) M.SwipeServe((float)(rng.NextDouble()*80-40), -60, .6f);
          Reach R; if (M.ReachInfo(out R) && rng.NextDouble() < .12) { double k=rng.NextDouble(); float sp=k<.2?.5f:k<.65?1.2f:2.2f; M.SwipeShot(new SwipeInput{dx=(float)(rng.NextDouble()*200-100),dy=-(float)(80+rng.NextDouble()*220),speed=sp,R=40,dist=(float)(rng.NextDouble()*60),side=rng.NextDouble()<.5?-1:1}); }
          M.Update(dt); M.events.Clear();
          for(int p=0;p<2;p++){ var o=p==0?M.me:M.op; string c; float ph; M.PickClip(o,p==0,out c,out ph);
            if(c!=cur[p]){
              bool takip=cur[p]!=null&&Match.CLIP_HIT.ContainsKey(cur[p])&&!Match.CLIP_HIT.ContainsKey(c); float dus=0;
              if(cur[p]!=null) sw++;
              if(prev[p]!=null&&blend[p]<1) ara++;
              float wc=SS(blend[p]);
              if(!yeni){ if(prev[p]!=null&&blend[p]<1) dus=1-wc; prev[p]=cur[p]; cur[p]=c; blend[p]=0; }
              else if(prev[p]!=null&&blend[p]<1&&c==prev[p]){ prev[p]=cur[p]; cur[p]=c; blend[p]=1-blend[p]; prev2[p]=null; }
              else { if(prev[p]!=null&&blend[p]<1){ float p2=prev2[p]!=null?pay[p]:0; dus=(1-wc)*p2; float wp=(1-wc)*(1-p2); prev2[p]=prev[p]; pay[p]=wc+wp>1e-4f?wp/(wc+wp):0; } else prev2[p]=null; prev[p]=cur[p]; cur[p]=c; blend[p]=0; }
              bs[p]=takip?.2f:.08f; top+=dus; if(dus>.10f)d10++; if(dus>.25f)d25++;
            }
            blend[p]=Math.Min(1,blend[p]+dt/bs[p]); if(blend[p]>=1) prev2[p]=null; }
        }
      }
      Console.WriteLine(hz+" Hz "+(yeni?"yeni":"eski")+": değişim "+sw+" · geçiş sürerken "+ara+" ("+(100*ara/Math.Max(1,sw))+"%) · tek karede düşen ağırlık >%10 "+d10+" · >%25 "+d25+" · toplam "+top.ToString("0"));
      if(!yeni) eski25[hz]=d25; else if(d25>eski25[hz]*.15f){ Console.WriteLine("HATA: "+hz+" Hz'de pop azalmadı ("+d25+" / "+eski25[hz]+")"); hata++; }
    }
    Console.WriteLine(hata==0?"TAMAM":"HATA "+hata); return hata==0?0:1;
  }
}
