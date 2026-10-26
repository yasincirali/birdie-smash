// FileTemasOlc.cs ile aynı durumlar; kod index.html'den kesilip çalıştırılır (kopya değil). node tool/unity_test/file_temas_parite.js
const fs=require('fs'); const h=fs.readFileSync(require('path').join(__dirname,'../../assets/game/index.html'),'utf8').split('\n');
const idx=(p,from=0)=>{ const i=h.findIndex((l,k)=>k>=from&&l.startsWith(p)); if(i<0) throw p; return i; };
const a=idx('const L=13.4'), b=idx('/* ---- ses ---- */');
const n0=idx('const NETN=33'), n1=idx('function netAt(');
const g0=idx('function agAdim('), g1=idx('/* Katı engeller');
const l0=h.findIndex(l=>l.includes('// canlı fizik: tahminle aynı adım')), l1=h.findIndex((l,k)=>k>l0&&l.startsWith('  const sp=P(s.x,s.y,s.z); M.trail'));
const src=[h.slice(a,b).join('\n'), h.slice(n0,n1+2).join('\n'), h.slice(g0,g1).join('\n'),
 'function tick(s,d){ const land=()=>{ s.dead=true; };\n'+h.slice(l0+1,l1).join('\n')+'\n}'].join('\n');
const f=new Function('reduce','P','burst','netThud','hap','txt','Tr','tone', src+'\nreturn {tick,simulate,NET,NETW,KD,netStep};')(false,()=>({x:0,y:0,s:1,ppm:1}),()=>{},()=>{},()=>{},()=>{},x=>x,()=>{});
const C=[[3.05,1.00,50,0],[3.05,1.30,6,0],[2.00,1.20,20,0],[3.05,1.47,20,0],[3.05,1.51,30,0],[3.05,1.51,4,0],[3.05,.60,25,0]];
const F=(v,n=3)=>(v>=0?'+':'-')+Math.abs(v).toFixed(n), G=(v,n=3)=>v.toFixed(n);
C.forEach((c,ci)=>{ const W=f.NETW; W.a.fill(0); W.v.fill(0); W.tape=W.tapeV=W.glow=0; W.act=false;
  const s={x:c[0],y:f.NET+.3,z:c[1],vx:0,vy:-c[2],vz:c[3],from:'me',type:'drop',t:0,netDone:false,dead:false,kd:f.KD}; s.netPred=null;
  let t=0,maxU=0,maxA=0,tAg=0; const d=1/60;
  for(let k=0;k<600&&!s.dead;k++){ if(W.act||W.glow>0){ let r=d; while(r>0){ const hh=Math.min(1/240,r); f.netStep(hh); r-=hh; } } f.tick(s,d); t+=d;
    const uz=-(s.y-f.NET); if(uz>maxU) maxU=uz; if(s.agda) tAg=t; for(const v of W.a) maxA=Math.max(maxA,Math.abs(v)); }
  console.log(ci+' sonY '+F(s.y-f.NET)+' x '+G(s.x)+' tIn '+G(t)+' maxKarsi '+G(maxU)+' maxA '+G(maxA)+' agSon '+G(tAg)+' cord '+(!!s.netCord?'True':'False')+' alt '+(!!s.altindan?'True':'False')+' hit '+(!!s.netHit?'True':'False')+' vz '+G(s.inVz,2)); });
