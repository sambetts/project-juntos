/* The front page's story (B-087, D-055): a made-up example of how Compicasa works, to live in or to rent out.
   One SVG scene drawn from a timeline of keyframes. It sets only SVG attributes and uses the Web Animations API,
   so it works under the site's CSP (no inline styles or scripts). Reduced motion shows still frames instead. */
(function(){
'use strict';
const NS='http://www.w3.org/2000/svg';
const END=30.4,CH=[0,5.6,13,18.2],CHEND=[5.6,13,18.2,END],STILL=[5.4,12.6,18,END];
const beats=live=>[[0,'fork'],[2.6,'c1'],[5.6,'c2a'],[10,'c2b'],[13,'c3a'],[live?16.2:15.5,'c3b'],[18.2,'c4a'],[22.2,'c4b'],[24.6,'c4c'],[27.2,'cF']];
const RM=window.matchMedia?matchMedia('(prefers-reduced-motion: reduce)'):{matches:false};

/* Easings, on progress p from 0 to 1 */
const E={lin:p=>p,in:p=>p*p*p,out:p=>1-(1-p)**3,io:p=>p<.5?4*p*p*p:1-(2-2*p)**3/2,
  back:p=>1+2.70158*(p-1)**3+1.70158*(p-1)**2,
  spring:p=>p>=1?1:1-Math.exp(-5.5*p)*Math.cos(3.5*Math.PI*p)};

/* Nodes: an SVG element plus keyframes per property. x, y, s (scale), sx (extra x scale) and r (rotation) make
   the transform; o is opacity; dash draws a stroke (needs pathLength 1); rad is a circle's radius; w a rect's width. */
const DEF={x:0,y:0,s:1,sx:1,r:0,o:1,dash:1,rad:0,w:0};
let NODES=[],UPD=[],seed=7,UID=0,FAM='system-ui,sans-serif',CTX;
function el(tag,a,parent){const e=document.createElementNS(NS,tag);if(a)for(const k in a)if(a[k]!=null)e.setAttribute(k,a[k]);if(parent)parent.appendChild(e);return e;}
function node(parent,b,tag,a){const n={el:el(tag||'g',a,parent),b:Object.assign({},b),k:{},cur:{}};NODES.push(n);return n;}
const bv=(n,p)=>p in n.b?n.b[p]:DEF[p];
/* Animate props from their previous value, starting at `at` (or when the previous key ends) for d seconds */
function to(n,at,d,props,e){
  for(const p in props){const ks=n.k[p]||(n.k[p]=[]),last=ks[ks.length-1],t0=last?Math.max(at,last.t):at;
    ks.push({t:t0,v:last?last.v:bv(n,p),e:'lin'},{t:Math.max(t0,at+d),v:props[p],e:e||'io'});}
  return n;
}
const set=(n,at,props)=>to(n,at,0,props,'lin');
function val(n,p,T){
  const ks=n.k[p];if(!ks)return bv(n,p);if(T<ks[0].t)return ks[0].v;
  let i=ks.length-1;while(i>0&&ks[i].t>T)i--;
  const a=ks[i],b=ks[i+1];return b?a.v+(b.v-a.v)*E[b.e]((T-a.t)/(b.t-a.t)):a.v;
}
const r2=v=>Math.round(v*100)/100,r3=v=>Math.round(v*1000)/1000;
function apply(n,T){
  const g=p=>n.k[p]?val(n,p,T):bv(n,p),c=n.cur,e=n.el,x=g('x'),y=g('y'),s=g('s'),sx=g('sx'),r=g('r');
  let tf=x||y?'translate('+r2(x)+' '+r2(y)+')':'';
  if(r)tf+=' rotate('+r2(r)+')';
  if(s!==1||sx!==1)tf+=' scale('+r3(s*sx)+' '+r3(s)+')';
  tf=tf.trim();
  if(tf!==c.tf){c.tf=tf;if(tf)e.setAttribute('transform',tf);else e.removeAttribute('transform');}
  const o=Math.min(1,Math.max(0,g('o'))),os=o>.999?'':o.toFixed(3);
  if(os!==c.o){c.o=os;if(os)e.setAttribute('opacity',os);else e.removeAttribute('opacity');}
  let hid=o<.002||Math.abs(s*sx)<.001;
  if(n.k.dash||'dash' in n.b){const d=Math.min(1,Math.max(0,g('dash'))),ds=(1-d).toFixed(4);if(ds!==c.d){c.d=ds;e.setAttribute('stroke-dashoffset',ds);}if(d<.001)hid=true;}
  if(n.k.rad||'rad' in n.b){const v=Math.max(0,r2(g('rad')));if(v!==c.rad){c.rad=v;e.setAttribute('r',v);}}
  if(n.k.w||'w' in n.b){const v=Math.max(0,r2(g('w')));if(v!==c.w){c.w=v;e.setAttribute('width',v);}}
  if(hid!==c.h){c.h=hid;if(hid)e.setAttribute('visibility','hidden');else e.removeAttribute('visibility');}
  c.s=s;
}
function frame(T){for(const n of NODES)apply(n,T);for(const u of UPD)u(T);}
/* Count a number up from 0 between at and at+d */
function count(te,at,d,v,fmt){let last;UPD.push(T=>{const s=fmt(Math.round(v*E.out(Math.min(1,Math.max(0,(T-at)/d)))));if(s!==last){last=s;te.textContent=s;}});}
const rnd=()=>(seed=seed*16807%2147483647)/2147483647;
function measure(s,size,wt){
  if(CTX===undefined){try{CTX=document.createElement('canvas').getContext('2d');}catch(e){CTX=null;}}
  if(!CTX)return s.length*size*.56;
  CTX.font=(wt||400)+' '+size+'px '+FAM;return CTX.measureText(s).width;
}
/* Text centred on (x,y) unless anchor says otherwise; squeezed to max if wider */
function txt(parent,x,y,s,size,wt,cls,anchor,max){
  const e=el('text',{x:x,y:y,'font-size':size,'font-weight':wt||null,class:cls,'text-anchor':anchor||'middle','dominant-baseline':'central'},parent);
  e.textContent=s;
  if(max&&measure(s,size,wt)>max){e.setAttribute('textLength',max);e.setAttribute('lengthAdjust','spacingAndGlyphs');}
  return e;
}
/* 12-unit icons centred on 0 */
const IC={
  pin:'M0 5.6C-2.8 2.6-4.2.6-4.2-1.4A4.2 4.2 0 0 1 4.2-1.4C4.2.6 2.8 2.6 0 5.6ZM1.4-1.4A1.4 1.4 0 1 1-1.4-1.4A1.4 1.4 0 1 1 1.4-1.4Z',
  laptop:'M-4.2-4H4.2V1.6H-4.2ZM-5.8 4H5.8',
  moon:'M1.6-4.8A5 5 0 1 0 4.9 1.4A3.8 3.8 0 0 1 1.6-4.8Z',
  pot:'M-4.6-1.2H4.6V2.4Q4.6 4.6 2.4 4.6H-2.4Q-4.6 4.6-4.6 2.4ZM-6-1.2H6M-1.6-3.6H1.6',
  flat:'M-4 5.4V-5H4V5.4M-5.8 5.4H5.8M-1.8-2.6H-1.2M1.2-2.6H1.8M-1.8 0H-1.2M1.2 0H1.8M-1 5.4V2.8H1V5.4',
  sun:'M2.4 0A2.4 2.4 0 1 1-2.4 0A2.4 2.4 0 1 1 2.4 0ZM0-5.6V-4.2M0 4.2V5.6M-5.6 0H-4.2M4.2 0H5.6M-4-4L-3-3M3 3L4 4M-4 4L-3 3M3-3L4-4',
  cal:'M-4.6-3.4H4.6V4.8H-4.6ZM-4.6-.6H4.6M-2.2-5.4V-2.4M2.2-5.4V-2.4',
  lock:'M-4-1H4V4.8H-4Z',shackle:'M-2.6-1V-3.2A2.6 2.6 0 0 1 2.6-3.2V-1',
  key:'M-.9 0A2.6 2.6 0 1 1-6.1 0A2.6 2.6 0 1 1-.9 0ZM-.9 0H6.1M4.1 0V2.4M6.1 0V2',
  coin:'M4.6 0A4.6 4.6 0 1 1-4.6 0A4.6 4.6 0 1 1 4.6 0ZM2.5 0A2.5 2.5 0 1 1-2.5 0A2.5 2.5 0 1 1 2.5 0Z',
  coins:'M4.4-2.4A4.4 1.8 0 1 1-4.4-2.4A4.4 1.8 0 1 1 4.4-2.4ZM-4.4-2.4V2.8A4.4 1.8 0 0 0 4.4 2.8V-2.4M-4.4.2A4.4 1.8 0 0 0 4.4.2',
  train:'M-3.4-5H3.4Q4.4-5 4.4-4V2Q4.4 3 3.4 3H-3.4Q-4.4 3-4.4 2V-4Q-4.4-5-3.4-5ZM-4.4-1H4.4M-2.4 3L-3.8 5.4M2.4 3L3.8 5.4M-2 1H-1.6M1.6 1H2',
  doc:'M-3.6-5H1.6L4-2.6V5H-3.6ZM1.6-5V-2.6H4M-1.6 0H2M-1.6 2.4H2',
  pie:'M-.8-4.4A4.4 4.4 0 1 0 3.6 0H-.8ZM1-1.8V-6A4.2 4.2 0 0 1 5.2-1.8Z',
  check:'M-3.2.2L-1 2.4L3.4-2.2',
  person:'M2-3.6A2 2 0 1 1-2-3.6A2 2 0 1 1 2-3.6ZM-4 5.4Q-4 .2 0 .2Q4 .2 4 5.4',
  // a thumbs-up, never a heart: this isn't a dating site (D-056); thumbL is its mirror image
  thumb:'M-4.98-.96H-2.72V4.76H-4.98ZM-1.95-.74L-.25-4.26A1.04 1.04 0 0 1 1.68-3.6L1.29-1.35H3.88A1.1 1.1 0 0 1 4.98-.03L4.21 3.66A1.32 1.32 0 0 1 2.89 4.76H-1.95Z',
  thumbL:'M4.98-.96H2.72V4.76H4.98ZM1.95-.74L.25-4.26A1.04 1.04 0 0 0-1.68-3.6L-1.29-1.35H-3.88A1.1 1.1 0 0 0-4.98-.03L-4.21 3.66A1.32 1.32 0 0 0-2.89 4.76H1.95Z'
};
function icon(parent,name,x,y,cls,sc,sw){
  return el('path',{d:IC[name],transform:'translate('+x+' '+y+')'+(sc&&sc!==1?' scale('+sc+')':''),class:cls||'st-ic sT','stroke-width':sw||1.4},parent);
}
/* A stroke that draws itself from start to end */
const drawn=(parent,d,cls,sw,b)=>node(parent,Object.assign({dash:0},b),'path',{d:d,fill:'none',class:cls,'stroke-width':sw,pathLength:1,'stroke-dasharray':'1 2','stroke-linecap':'round','stroke-linejoin':'round'});
/* A pill centred on 0,0; returns its width */
function pill(parent,s,o){
  const size=o.size||12,wt=o.wt||600,h=o.h||22,w=r2(Math.max(o.minW||0,measure(s,size,wt)+(o.pad||20)));
  el('rect',{x:-w/2,y:-h/2,width:w,height:h,rx:h/2,class:o.fill,'stroke-width':o.sw||null},parent);
  txt(parent,0,.5,s,size,wt,o.ink,null,o.max);
  return w;
}
/* An answer chip: icon and text on a soft background */
function chip(g,s,ic,w){
  el('rect',{x:r2(-w/2),y:-12,width:r2(w),height:24,rx:12,class:'fCs'},g);
  icon(g,ic,r2(-w/2+14),0,'st-ic sC');
  txt(g,r2(-w/2+24),.5,s,12.5,600,'fT','start');
}
const chipW=s=>measure(s,12.5,600)+36;
/* People: households a (Ana), b (Luis and Marta) and c (you); n is a grey candidate. a and b turn from grey to
   their colour when the group forms; you are always in colour. */
function face(g,who,init,colour){
  const ring=colour?'sS':'sL',sw=colour?2:1.2,ink=colour?'fInk':'fM',fill=colour?'f'+who.toUpperCase():'fS2';
  if(who==='b')[[-10,init[0]],[10,init[1]]].forEach(([dx,ch])=>{el('circle',{cx:dx,r:13,class:fill+' '+ring,'stroke-width':sw},g);txt(g,dx,.5,ch,12,700,ink);});
  else{el('circle',{r:who==='c'?18:who==='a'?16:14,class:fill+' '+ring,'stroke-width':sw},g);txt(g,0,.5,init,who==='c'?12:13,700,ink,null,who==='c'?30:null);}
}
function avatar(layer,who,init,b){
  const n=node(layer,b);
  if(who!=='c')face(el('g',null,n.el),who,init,false);
  if(who!=='n'){n.col=node(n.el,{o:who==='c'?1:0});face(n.col.el,who,init,true);}
  return n;
}
/* The scene, 340 by 250: a camera group that can zoom, and layers from back to front */
function build(mode){
  NODES=[];UPD=[];seed=7;UID++;
  const live=mode!=='let',svg=el('svg',{viewBox:'0 0 340 250',class:'st-svg','aria-hidden':'true',focusable:'false'});
  const S={live:live,svg:svg,defs:el('defs',null,svg),id:k=>'st'+UID+k,flash:[]};
  el('rect',{width:340,height:250,rx:14,class:'st-bg'},svg);
  S.cam=node(svg,{x:170,y:128});
  const inner=el('g',{transform:'translate(-170 -128)'},S.cam.el);
  S.L={};['links','brief','cards','plan','over','people','fly'].forEach(k=>S.L[k]=el('g',null,inner));
  const tg=el('g',{transform:'translate(8 8)'},svg),ts=t('stTag'),tw=r2(measure(ts,12,600)+20);
  el('rect',{width:tw,height:22,rx:11,class:'fS sL','stroke-width':1},tg);txt(tg,r2(tw/2),11.5,ts,12,600,'fM');
  const bn=t('lB').split(/\s+/);
  S.ana=avatar(S.L.people,'a',t('lA').charAt(0),{x:50,y:128,s:0});
  S.lm=avatar(S.L.people,'b',[bn[0].charAt(0),bn[bn.length-1].charAt(0)],{x:290,y:128,s:0});
  S.you=avatar(S.L.people,'c',t('lC'),{x:170,y:44,s:0});
  ch1(S);ch2(S);ch3(S);ch4(S);(live?finLive:finLet)(S);
  return svg;
}
const CHIPS=live=>({s:t(live?'stChipsLive':'stChipsLet'),ic:live?['pin','laptop','moon','pot']:['flat','sun','pin','cal']});
/* 1. You: what the home is for, then your answers. Your budget stays private. */
function ch1(S){
  const live=S.live,you=S.you,tx=live?-63:63;
  to(you,0,.5,{s:1.15},'back');
  const card=node(S.L.brief,{x:170,y:165,o:0}),c=card.el;
  to(card,.3,.6,{y:135,o:1},'out');
  el('rect',{x:-156,y:-91,width:312,height:182,rx:18,class:'fS sL','stroke-width':1.2},c);
  el('rect',{x:-126,y:-61,width:252,height:30,rx:15,class:'fS2'},c);
  txt(c,-63,-46,t('purposeLive'),12.5,600,'fM',null,112);
  txt(c,63,-46,t('purposeLet'),12.5,600,'fM',null,112);
  const cp=el('clipPath',{id:S.id('sw')},S.defs);el('rect',{x:-126,y:-61,width:252,height:30,rx:15},cp);
  const rip=node(el('g',{'clip-path':'url(#'+S.id('sw')+')'},c),{x:tx,y:-46,rad:0,o:.4},'circle',{class:'fC'});
  to(rip,1,.6,{rad:70,o:0},'out');
  const knob=node(c,{x:tx,y:-46,s:0});
  el('rect',{x:-62,y:-13,width:124,height:26,rx:13,class:'fC'},knob.el);
  txt(knob.el,0,.5,t(live?'purposeLive':'purposeLet'),12.5,700,'fInk',null,112);
  to(knob,1,.35,{s:1},'back');
  const tap=node(c,{x:40,y:24,o:0},'circle',{r:7,class:'fT'});
  to(tap,.45,.15,{o:.28});to(tap,.55,.4,{x:tx,y:-46});to(tap,.95,.08,{s:.7},'out');to(tap,1.03,.3,{s:1.6,o:0},'out');
  const ch=CHIPS(live);
  [0,1].forEach(row=>{
    const ws=[0,1].map(i=>chipW(ch.s[row*2+i])),tot=ws[0]+8+ws[1],k=Math.min(1,284/tot);
    const rg=el('g',{transform:'translate(0 '+(row?21:-11)+')'+(k<1?' scale('+r3(k)+')':'')},c);
    [0,1].forEach(i=>{const j=row*2+i,w=ws[i],n=node(rg,{x:r2(i?tot/2-w/2:-tot/2+w/2),s:0});
      chip(n.el,ch.s[j],ch.ic[j],w);to(n,2.3+.25*j,.4,{s:1},'back');});
  });
  const bs=t('stBudget'),bw=r2(measure(bs,12.5,600)+38),bud=node(c,{y:61,s:0}),lx=r2(-bw/2+15);
  el('rect',{x:-bw/2,y:-12,width:bw,height:24,rx:12,class:'fS sM','stroke-width':1.2,'stroke-dasharray':'4 3'},bud.el);
  icon(bud.el,'lock',lx,1,'st-ic sM');
  const shk=node(bud.el,{x:lx,y:-1.2},'path',{d:IC.shackle,class:'st-ic sM','stroke-width':1.4});
  txt(bud.el,r2(-bw/2+26),.5,bs,12.5,600,'fM','start');
  to(bud,3.5,.4,{s:1},'back');to(shk,4.15,.15,{y:1},'in');to(bud,4.3,.12,{s:1.08},'out');to(bud,4.42,.25,{s:1});
  to(card,5.6,.4,{y:60,s:.08,o:0},'in');
  to(you,5.85,.5,{y:128,s:1});
}
/* 2. Matching: who fits with you and why; a group forms only when everyone says yes */
function ch2(S){
  const L=S.L,you=S.you,ana=S.ana,lm=S.lm,who={a:ana,b:lm},link={};
  const spots={b:[290,128],n1:[230,204.2],n2:[110,204.2],a:[50,128],n3:[110,51.8],n4:[230,51.8]},grey={n1:'C',n2:'R',n3:'P',n4:'J'};
  ['n2','n1','b','n4','n3','a'].forEach((k,i)=>{
    const x=spots[k][0],y=spots[k][1],dx=x-170,dy=y-128,at=6.3+.09*i;
    link[k]=node(L.links,{x:170,y:128,r:r2(Math.atan2(dy,dx)*180/Math.PI),sx:0,o:0},'path',{d:'M0 0H1',class:'sL','stroke-width':1.6,'stroke-linecap':'butt'});
    to(link[k],at,.3,{sx:r2(Math.hypot(dx,dy)),o:1},'out');
    if(!who[k])who[k]=avatar(L.people,'n',grey[k],{x:x,y:y,s:0});
    to(who[k],at+.25,.35,{s:1},'back');
  });
  const others=['n1','n2','n3','n4'];
  others.forEach(k=>{to(who[k],7.1,.3,{o:.3});to(link[k],7.1,.3,{o:.3});});
  const badge=(av,v)=>{const b=node(av.el,{y:27,s:0});el('rect',{x:-22,y:-10,width:44,height:20,rx:10,class:'fGb'},b.el);
    count(txt(b.el,0,.5,t('stPct',0),11.5,700,'fG'),7.15,.7,v,n=>t('stPct',n));to(b,7.1,.35,{s:1},'back');return b;};
  const fades=[badge(ana,92),badge(lm,87),link.a,link.b];
  others.forEach(k=>{to(who[k],7.9,.35,{o:0,s:.6},'in');to(link[k],7.9,.3,{o:0});});
  to(ana,7.9,.6,{x:95});to(lm,7.9,.6,{x:245});
  to(link.a,7.9,.6,{sx:75});to(link.b,7.9,.6,{sx:75});
  // why: one of your answers that each of them shares
  const ch=CHIPS(S.live);
  [2,3].forEach((j,i)=>{const w=chipW(ch.s[j]),k=Math.min(1,150/w),n=node(L.fly,{x:170,y:128,s:.3,o:0});
    chip(el('g',k<1?{transform:'scale('+r3(k)+')'}:null,n.el),ch.s[j],ch.ic[j],w);
    to(n,8.35+.12*i,.55,{x:i?245:95,y:182,s:1,o:1},'out');fades.push(n);});
  // mutual interest: a line and a thumbs-up from each side meet in the middle
  [[95,9],[245,9.35]].forEach(([x,at])=>{
    const mid=(170+x)/2,half=Math.abs(170-x)/2;
    [[170,x<170?180:0],[x,x<170?0:180]].forEach(([x0,rot])=>{
      const h=node(L.links,{x:x0,y:128,r:rot,sx:0},'path',{d:'M0 0H1',class:'sC','stroke-width':2.4,'stroke-linecap':'butt'});
      to(h,at,.45,{sx:half});fades.push(h);
      const hh=node(L.fly,{x:x0,y:128,s:0},'path',{d:x0<mid?IC.thumb:IC.thumbL,class:'fC'});
      to(hh,at,.15,{s:.8},'out');to(hh,at,.45,{x:mid});to(hh,at+.45,.15,{o:0,s:.4},'in');
    });
    const big=node(L.fly,{x:mid,y:128,s:0},'path',{d:IC.thumb,class:'fC'});
    to(big,at+.45,.4,{s:1.5},'back');fades.push(big);
  });
  // everyone says yes
  const yes=t('v_yes'),bubbles=[[ana,10.1,-30],[lm,10.35,-28],[you,10.6,-32]].map(([av,at,y])=>{
    const b=node(av.el,{y:y,s:0}),w=r2(Math.max(34,measure(yes,12,700)+20));
    el('rect',{x:-w/2,y:-11,width:w,height:22,rx:11,class:'fGb'},b.el);
    el('path',{d:'M-4 10L0 15L4 10Z',class:'fGb'},b.el);
    txt(b.el,0,.5,yes,12,700,'fG');to(b,at,.35,{s:1},'back');return b;});
  to(ana.col,10.85,.5,{o:1});to(lm.col,10.85,.5,{o:1});
  fades.forEach(n=>to(n,10.9,.3,{o:0}));
  bubbles.forEach(b=>to(b,11.25,.2,{o:0}));
  to(S.cam,11,.7,{s:1.1});
  S.ring=drawn(L.links,'M170 100H252A28 28 0 0 1 280 128A28 28 0 0 1 252 156H88A28 28 0 0 1 60 128A28 28 0 0 1 88 100Z','sG',2);
  to(S.ring,11.35,.65,{dash:1});
  S.gpill=node(L.over,{x:170,y:158,s:0});pill(S.gpill.el,t('stGroup'),{size:12,wt:700,fill:'fG',ink:'fBg'});
  to(S.gpill,11.95,.4,{s:1},'back');
}
/* The shared home as a floor plan, in its own units (360 by 236) */
const ROOMS={bed:[[10,10,108,98],[118,10,124,98],[242,10,108,98]],bath:[10,108,76,60],kitchen:[10,168,76,58],living:[86,108,196,118],terrace:[282,108,68,118]};
const WALLS=['M118 10V108','M242 10V108','M10 108H60','M76 108H180','M200 108H300','M318 108H350','M86 108V126','M86 142V186','M86 202V226','M86 168H10','M282 108V150','M282 170V226'],
  DOOR='M156 226V198A28 28 0 0 1 184 226';
const rect=(parent,r,cls,a)=>el('rect',Object.assign({x:r[0],y:r[1],width:r[2],height:r[3],class:cls},a),parent);
function planNode(S){
  const p=node(S.L.plan,{x:175.8,y:92.8,s:.4176}),g=p.el;
  to(node(g,{o:0},'rect',{x:10,y:10,width:340,height:216,class:'fS'}),13.6,.5,{o:1});
  p.fills=el('g',null,g);
  const out=drawn(g,'M184 226H350V10H10V226H156','sT',3);to(out,13.6,.7,{dash:1});
  const inn=WALLS.map((d,i)=>{const w=drawn(g,d,'sM',1.4);to(w,13.9+.05*i,.3,{dash:1},'out');return w;});
  const door=drawn(g,DOOR,'sM',1.4);to(door,14.5,.35,{dash:1},'out');inn.push(door);
  UPD.push(()=>{const k=p.cur.s||1,wo=r2((1.1+1.5*k)/k);if(wo!==p.wo){p.wo=wo;out.el.setAttribute('stroke-width',wo);
    const wi=r2((.6+.8*k)/k);inn.forEach(w=>w.el.setAttribute('stroke-width',wi));}});
  return p;
}
/* 3. The brief: everyone's answers become one shared brief, with how to leave agreed from the start */
function ch3(S){
  const live=S.live,L=S.L,hs=[S.ana,S.lm,S.you],hx=[252,286,320];
  to(S.cam,13,.5,{s:1});to(S.ring,13,.4,{o:0});to(S.gpill,13,.3,{o:0});
  hs.forEach((n,i)=>to(n,13.05+.07*i,.55,{x:hx[i],y:26,s:.62}));
  const card=S.brief=node(L.brief,{x:-8,y:52,o:0}),c=card.el;to(card,13.45,.45,{x:12,o:1},'out');
  el('rect',{width:158,height:172,rx:14,class:'fS sL','stroke-width':1.2},c);
  txt(c,14,22,t('gs_brief'),13.5,700,'fT','start',130);
  el('path',{d:'M14 36H144',class:'sL','stroke-width':1},c);
  const P=S.plan=planNode(S),F=P.fills,fl=S.flash,lines=t(live?'stLinesLive':'stLinesLet');
  const by=live?['c','abc','a','b','abc']:['c','b','a','abc','abc'],src={a:252,b:286,c:320};
  const pop=(n,at,s)=>{to(n,at,.4,{s:s||1},'back');fl.push(n);return n;};
  const ico=(name,x,y,cls,sc)=>{const n=node(L.over,{x:x,y:y,s:0});icon(n.el,name,0,0,cls,sc,1.3);return n;};
  const show=(r,cls,at)=>{const n=node(F,{o:0},'rect',{x:r[0],y:r[1],width:r[2],height:r[3],class:cls});to(n,at,.4,{o:1});fl.push(n);};
  const door=at=>{const gl=node(L.over,{x:246.8,y:187.2,rad:0,o:.45},'circle',{class:'fG'});to(gl,at,.6,{rad:16,o:.12},'out');fl.push(gl);
    const d=node(F,{o:0},'path',{d:DOOR,fill:'none',class:'sG','stroke-width':5,'stroke-linecap':'round'});to(d,at,.3,{o:1});fl.push(d);};
  const fx=live?[
    at=>pop(ico('pin',322,86,'st-j fCs sC',1.6),at),
    at=>ROOMS.bed.forEach((r,i)=>show(r,'f'+'ABC'[i]+' st-tint',at+.1*i)),
    at=>{show(ROOMS.terrace,'pTer',at);const s=pop(ico('sun',307.8,162.5,'st-ic sW',1.5),at);to(s,at,2.6,{r:60},'lin');},
    at=>{const n=node(L.over,{x:326,y:200,s:0});el('circle',{r:10,class:'fS sL','stroke-width':1},n.el);icon(n.el,'train',0,0,'st-ic sT',1.05,1.3);pop(n,at);},
    door]:[
    at=>pop(ico('pin',322,86,'st-j fCs sC',1.6),at),
    at=>ROOMS.bed.forEach((r,i)=>show(r,'fW st-tint',at+.1*i)),
    at=>{const k=pop(ico('key',247,204,'st-ic sC',1.5),at);to(k,at,.5,{r:-25},'out');to(k,at+.5,.5,{r:0},'spring');},
    at=>{const n=pop(ico('coins',252.6,162.5,'st-j fWb sW',1.6),at);to(n,at+.4,.3,{sx:-1});to(n,at+.7,.3,{sx:1});},
    door];
  lines.forEach((s,i)=>{
    const y=108+23*i,ly=y-52,at=14.5+.55*i;
    by[i].split('').forEach((h,j)=>{const a0=at+.04*j,d=node(L.fly,{x:src[h],y:26,o:0},'circle',{r:3.5,class:'f'+h.toUpperCase()});
      set(d,a0,{o:1});to(d,a0,.4,{x:31});to(d,a0,.4,{y:y},'in');set(d,a0+.42,{o:0});});
    const ck=node(c,{x:19,y:ly,s:0});el('circle',{r:8,class:'fG'},ck.el);
    to(ck,at+.42,.3,{s:1},'back');to(drawn(ck.el,IC.check,'sBg',1.8),at+.55,.25,{dash:1},'out');
    const tl=node(c,{x:-6,o:0});txt(tl.el,44,ly,s,12.5,600,'fT','start',112);to(tl,at+.45,.35,{x:0,o:1},'out');
    fx[i](at+.45);
  });
}
function mini(g,big){
  el('rect',{x:-44,y:-52,width:88,height:56,rx:8,class:'fS2'},g);
  if(!big){rect(g,[-36,-47,72,38],'fS');rect(g,[-36,-47,36,17],'pBed');rect(g,[0,-47,36,17],'pBed');rect(g,[-36,-30,72,21],'pLive');
    el('path',{d:'M0-47V-30M-36-30H-26M-16-30H10M20-30H36',fill:'none',class:'sM','stroke-width':.8},g);
    el('rect',{x:-36,y:-47,width:72,height:38,fill:'none',class:'sT','stroke-width':1.2},g);return;}
  rect(g,[12,-48,28,40],'pGarden');el('rect',{x:18,y:-42,width:16,height:12,rx:2,class:'pPool'},g);
  rect(g,[-40,-48,52,40],'fS');[[-40,-48,17,16],[-23,-48,18,16],[-5,-48,17,16]].forEach(r=>rect(g,r,'pBed'));rect(g,[-40,-32,52,24],'pLive');
  el('path',{d:'M-23-48V-32M-5-48V-32M-40-32H-30M-22-32H12',fill:'none',class:'sM','stroke-width':.8},g);
  el('rect',{x:-40,y:-48,width:52,height:40,fill:'none',class:'sT','stroke-width':1.2},g);
}
/* 4. Homes: agency listings that fit the brief and the group's budget; hidden votes; then the pack */
function ch4(S){
  const live=S.live,L=S.L,P=S.plan;
  to(S.brief,18.2,.35,{x:-8,o:0},'in');
  S.flash.forEach(n=>to(n,18.2,.3,{o:0}));
  to(P,18.3,.55,{x:22,y:55.8,s:.2222});
  const fits=[96,71,88],cards=[0,1,2].map(i=>{
    const n=node(L.cards,{x:62+108*i,y:i?126:106,o:0});
    el('rect',{x:-48,y:-56,width:96,height:112,rx:12,class:'fS sL','stroke-width':1.2},n.el);
    if(i)mini(n.el,i===2);else el('rect',{x:-44,y:-52,width:88,height:56,rx:8,class:'fS2'},n.el);
    const b=node(n.el,{y:19,s:0});el('rect',{x:-24,y:-10,width:48,height:20,rx:10,class:'fGb'},b.el);
    count(txt(b.el,0,.5,t('stPct',0),11.5,700,'fG'),19.15+.1*i,.7,fits[i],v=>t('stPct',v));to(b,19.1+.1*i,.35,{s:1},'back');
    n.bars=node(n.el,{});rect(n.bars.el,[-36,35.5,72,4],'fL',{rx:2});rect(n.bars.el,[-36,43,46,4],'fL',{rx:2});
    return n;});
  to(cards[0],18.55,.3,{o:1});to(cards[1],18.75,.45,{y:106,o:1},'out');to(cards[2],18.9,.45,{y:106,o:1},'out');
  const note=(x,lines,at)=>{const n=node(L.over,{x:x,y:82,s:0}),w=r2(Math.min(92,Math.max(...lines.map(s=>measure(s,11.5,700)))+18));
    el('rect',{x:-w/2,y:-17,width:w,height:34,rx:10,class:'fWb sW','stroke-width':1},n.el);
    lines.forEach((s,i)=>txt(n.el,0,i?7.5:-7,s,11.5,700,'fW',null,w-12));to(n,at,.4,{s:1},'back');return n;};
  to(cards[1],19.95,.3,{o:.45});const n2=note(170,t('stOnly2'),20.05);
  // the group's budget: each household's private budget goes in, only the total shows
  const bud=node(L.over,{o:0}),bg=bud.el;to(bud,20.4,.3,{o:1});
  const lk=node(bg,{x:20,y:180});icon(lk.el,'lock',0,0,'st-ic sM',.9);icon(lk.el,'shackle',0,0,'st-ic sM',.9);
  txt(bg,30,179.5,t('stGroupBudget'),12,600,'fM','start',200);
  el('rect',{x:14,y:190,width:312,height:12,rx:6,class:'fS2 sL','stroke-width':1},bg);
  [['a',252],['b',286],['c',320]].forEach(([h,x],i)=>{const at=20.55+.15*i,c=node(L.fly,{x:x,y:26,o:0},'circle',{r:5,class:'f'+h.toUpperCase()+' sS','stroke-width':1.5});
    set(c,at,{o:1});to(c,at,.45,{x:20});to(c,at,.45,{y:180},'in');to(c,at+.45,.12,{o:0,s:.4});
    to(lk,at+.45,.06,{s:1.25},'out');to(lk,at+.51,.09,{s:1});});
  const cid=S.id('bud'),cp=el('clipPath',{id:cid},S.defs);el('rect',{x:14,y:190,width:312,height:12,rx:6},cp);
  to(node(el('g',{'clip-path':'url(#'+cid+')'},bg),{w:0},'rect',{x:14,y:190,height:12,class:'fM'}),21.35,.65,{w:243},'out');
  [62,170,278].forEach((x,i)=>{const m=el('g',{transform:'translate('+x+' 196)'},bg);
    el('circle',{r:5,class:'fS sM','stroke-width':1.5},m);
    const ok=node(m,{s:0},'circle',{r:5.5,class:(i<2?'fG':'fW')+' sS','stroke-width':1.5});
    to(ok,[21.4,21.54,22][i],.3,{s:1},'back');});
  to(cards[2],22.05,.3,{o:.45});const n3=note(278,t('stOver'),22.1);
  // everyone votes; no one sees the votes until all are in
  to(cards[0].bars,22.2,.25,{o:0});
  const toks=[['a',252,38],['b',286,62],['c',320,86]].map(([h,x0,x],i)=>{
    const at=22.3+.25*i,n=node(L.fly,{x:x0,y:26,o:0,s:.6}),q=node(n.el,{}),a=node(n.el,{o:0});
    el('circle',{r:9,class:'fS2 sM','stroke-width':1.2},q.el);txt(q.el,0,.5,'?',11.5,700,'fM');
    el('circle',{r:9,class:'f'+h.toUpperCase()},a.el);el('path',{d:IC.check,fill:'none',class:'sInk','stroke-width':1.8,'stroke-linecap':'round','stroke-linejoin':'round'},a.el);
    set(n,at,{o:1});to(n,at,.45,{x:x});to(n,at,.45,{y:147},'in');to(n,at,.45,{s:1},'out');
    to(n,23.45,.15,{sx:0},'in');set(q,23.6,{o:0});set(a,23.6,{o:1});to(n,23.6,.15,{sx:1},'out');
    return n;});
  const sw=r2(measure(t('stAgreed'),14,800)+26),stamp=node(L.over,{x:62,y:84,r:-8,s:1.8,o:0});
  el('rect',{x:-sw/2,y:-15,width:sw,height:30,rx:6,class:'fS sG','stroke-width':2},stamp.el);
  el('rect',{x:-sw/2+3.5,y:-11.5,width:sw-7,height:23,rx:3.5,fill:'none',class:'sG','stroke-width':.8},stamp.el);
  txt(stamp.el,0,.5,t('stAgreed'),14,800,'fG');
  set(stamp,23.95,{o:1});to(stamp,23.95,.35,{s:1},'back');
  for(let i=0;i<14;i++){const a=rnd()*Math.PI*2,d=26+rnd()*30,c=node(L.over,{x:62,y:84,o:0,r:r2(rnd()*180)},'rect',{x:-2,y:-1.2,width:4,height:2.4,rx:.6,class:'f'+'ABC'[i%3]});
    set(c,24,{o:1});to(c,24,.6,{x:r2(62+Math.cos(a)*d),y:r2(84+Math.sin(a)*d*.8)},'out');to(c,24,.7,{r:r2(rnd()*540-270)},'out');to(c,24.45,.35,{o:0});}
  to(bud,24.45,.3,{o:0});
  // the pack: what it might cost each of you, and questions for the lawyer
  const pk=node(L.over,{y:12,o:0}),pl=t(live?'stPackLive':'stPackLet');to(pk,24.6,.4,{y:0,o:1},'out');
  el('rect',{x:14,y:172,width:312,height:66,rx:12,class:'fS sL','stroke-width':1.2},pk.el);
  el('path',{d:'M30 208H310',class:'sL','stroke-width':1},pk.el);
  [['pie',190],['doc',222]].forEach(([ic,y],i)=>{icon(pk.el,ic,32,y,'st-ic sC',1.1);txt(pk.el,46,y,pl[i],12,600,'fT','start',248);
    const ck=node(pk.el,{x:308,y:y,s:0});el('circle',{r:7,class:'fG'},ck.el);
    el('path',{d:IC.check,transform:'scale(.8)',fill:'none',class:'sBg','stroke-width':2,'stroke-linecap':'round','stroke-linejoin':'round'},ck.el);
    to(ck,24.95+.25*i,.35,{s:1},'back');});
  cards.concat([n2,n3,stamp,pk],toks).forEach(n=>to(n,26.5,.3,{o:0}));
}
/* Live: each household in its own bedroom, the rest shared */
function finLive(S){
  const L=S.L,P=S.plan,F=P.fills,bx=[70.2,170,269.8];
  to(P,26.6,.65,{x:15.2,y:28,s:.86});
  [S.ana,S.lm,S.you].forEach((n,i)=>{const at=27.3+.15*i;
    to(n,at,.6,{x:bx[i]});to(n,at,.25,{y:8},'out');to(n,at+.25,.35,{y:62},'in');to(n,at,.6,{s:.85});
    to(n,at+.6,.08,{sx:1.2},'out');to(n,at+.68,.5,{sx:1},'spring');});
  ROOMS.bed.forEach((r,i)=>{const id=S.id('bed'+i),cp=el('clipPath',{id:id},S.defs);
    to(node(cp,{x:[64,180,296][i],y:39.5,rad:0},'circle'),27.85+.15*i,.6,{rad:90},'out');
    rect(F,r,'f'+'ABC'[i]+' st-tint',{'clip-path':'url(#'+id+')'});});
  const fid=S.id('floor'),fc=el('clipPath',{id:fid},S.defs),fl=el('g',{'clip-path':'url(#'+fid+')'},F);
  to(node(fc,{x:170,y:226,rad:0},'circle'),28.1,.9,{rad:280},'out');
  rect(fl,ROOMS.bath,'pBath');rect(fl,ROOMS.kitchen,'pLive');rect(fl,ROOMS.living,'pLive');rect(fl,ROOMS.terrace,'pTer');
  [t('lA'),t('lB'),t('lC')].forEach((s,i)=>{const n=node(L.over,{x:bx[i],y:6,o:0});
    txt(n.el,0,92,s,13,700,'fT',null,88);txt(n.el,0,106,t('lBed'),12,400,'fM',null,88);to(n,28+.15*i,.4,{y:0,o:1},'out');});
  [[t('lBath'),56.5,146,58],[t('lKitchen'),56.5,198,58],[t('lLiving'),173.4,172,150],[t('lTerrace'),287,172,52]].forEach(([s,x,y,m],i)=>{
    const n=node(L.over,{x:x,y:y,o:0});txt(n.el,0,0,s,12,600,'fM',null,m);to(n,28.6+.08*i,.4,{o:1});});
}
/* Let: the owners together, the whole home let, the rent going to the group */
function finLet(S){
  const L=S.L,P=S.plan,F=P.fills,ox=[248,282,316],box='M246 8H318A18 18 0 0 1 336 26A18 18 0 0 1 318 44H246A18 18 0 0 1 228 26A18 18 0 0 1 246 8Z';
  to(P,26.6,.65,{x:58.4,y:70,s:.62});
  [S.ana,S.lm,S.you].forEach((n,i)=>to(n,26.6+.05*i,.45,{x:ox[i],s:.7}));
  to(drawn(L.links,box,'sW',1.6),27,.5,{dash:1});
  const glow=node(L.links,{o:0},'path',{d:box,fill:'none',class:'sW','stroke-width':5});
  const own=node(L.over,{o:0});txt(own.el,282,60,t('kOwners'),12,700,'fM',null,104);to(own,27.2,.4,{o:1});
  const fid=S.id('let'),fc=el('clipPath',{id:fid},S.defs);
  to(node(fc,{x:170,y:226,rad:0},'circle'),27.5,.9,{rad:280},'out');
  rect(F,[10,10,340,216],'fW st-tint',{'clip-path':'url(#'+fid+')'});
  [[158,156],[187,156]].forEach(([x,y],i)=>{const at=28+.2*i,n=node(L.people,{x:163.8,y:262,o:0}),b=node(n.el,{});
    icon(b.el,'person',0,0,'st-j fS sT',1.7,1.2);
    set(n,at,{o:1});to(n,at,.5,{y:212},'lin');to(n,at+.5,.6,{x:x,y:y},'out');
    for(let k=0;k<6;k++){to(b,at+.18*k,.09,{y:-2},'out');to(b,at+.18*k+.09,.09,{y:0},'in');}});
  const tl=node(L.over,{x:172.5,y:181,o:0});txt(tl.el,0,0,t('stTenants'),12,700,'fT',null,90);to(tl,28.9,.4,{o:1});
  [0,1,2].forEach(i=>{const at=29+.25*i,c=node(L.fly,{x:172.5,y:144,s:0});icon(c.el,'coin',0,0,'st-j fWb sW',1.3,1.3);
    to(c,at,.2,{s:1},'back');to(c,at+.1,.45,{x:282},'in');to(c,at+.1,.45,{y:44},'out');to(c,at+.5,.1,{o:0,s:.5});
    set(glow,at+.55,{o:.5});to(glow,at+.57,.2,{o:0},'out');});
}
/* The figure: purpose switch, stage, controls and caption */
const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'})[c]);
const ico=(d,cls)=>'<svg class="st-ico '+(cls||'')+'" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="'+d+'"/></svg>';
const HOUSE='M3.5 11L12 4l8.5 7M6 9.5V20h12V9.5';
/* live: someone at home, never a heart (D-056); let: a key */
const I={live:ico(HOUSE+'M13.8 11.6a1.8 1.8 0 1 1-3.6 0a1.8 1.8 0 1 1 3.6 0zM8.4 20c.3-2.3 1.7-3.6 3.6-3.6s3.3 1.3 3.6 3.6'),
  let:ico(HOUSE+'M10.7 15.5a1.7 1.7 0 1 1-3.4 0a1.7 1.7 0 1 1 3.4 0zM10.7 15.5h5.8M15 15.5v1.7M16.5 15.5v1.3'),
  play:ico('M8 5.5v13l10.5-6.5z','st-fill st-i-play'),pause:ico('M8.5 5.5v13M15.5 5.5v13','st-i-pause'),replay:ico('M4.5 12a7.5 7.5 0 1 0 2.2-5.3M6.7 2.7v4h4','st-i-replay')};
const st={t:0,playing:false,mode:'live',started:false,done:false,userPaused:false,auto:false,inView:false};
let host=null,raf=0,last=0,io=null,beat=-1,fresh=false,rz=0;
function capFor(b,live){
  const s=t('lSteps'),T=(i,k)=>[s[i][0],t(k)];
  switch(b){
    case 'fork':return t('stFork');
    case 'c1':return [live?s[0][0]:t('stCh1Let'),t(live?'stC1Live':'stC1Let')];
    case 'c2a':return T(1,'stC2a');case 'c2b':return T(1,'stC2b');
    case 'c3a':return T(2,live?'stC3aLive':'stC3aLet');case 'c3b':return T(2,live?'stC3bLive':'stC3bLet');
    case 'c4a':return T(3,'stC4a');case 'c4b':return T(3,'stC4b');case 'c4c':return T(3,live?'stC4cLive':'stC4cLet');
    default:return t(live?'stFinLive':'stFinLet');
  }
}
const capHtml=c=>'<b>'+esc(c[0])+(/[.?!…]$/.test(c[0])?'':'.')+'</b> '+esc(c[1]);
const chTitle=i=>i===0&&st.mode==='let'?t('stCh1Let'):t('lSteps')[i][0];
function draw(){
  if(!host)return;
  frame(st.t);
  const live=st.mode!=='let',bs=beats(live);let b=0;while(b+1<bs.length&&st.t>=bs[b+1][0])b++;
  if(b!==beat){beat=b;const cap=host.querySelector('.st-cap');cap.innerHTML=capHtml(capFor(bs[b][1],live));
    if(!fresh&&!RM.matches&&cap.animate)cap.animate([{opacity:0,transform:'translateY(4px)'},{opacity:1,transform:'none'}],{duration:360,easing:'ease-out'});}
  fresh=false;
  host.querySelectorAll('.st-ch').forEach((c,i)=>{c.querySelector('b').style.transform='scaleX('+r3(Math.min(1,Math.max(0,(st.t-CH[i])/(CHEND[i]-CH[i]))))+')';
    if(st.t>=CH[i]&&(st.t<CHEND[i]||i===3))c.setAttribute('aria-current','step');else c.removeAttribute('aria-current');});
}
function sync(){
  if(!host)return;
  host.classList.toggle('is-playing',st.playing);host.classList.toggle('is-done',st.done);host.classList.toggle('is-rm',!!RM.matches);
  host.querySelector('.st-play').setAttribute('aria-label',t(st.playing?'stPause':st.done?'stReplay':'stPlay'));
  host.querySelector('.st-il').textContent=t(st.mode==='let'?'stInviteLive':'stInviteLet');
  host.querySelectorAll('.st-mode').forEach(m=>m.setAttribute('aria-pressed',String(m.dataset.stMode===st.mode)));
  host.querySelectorAll('.st-ch').forEach((c,i)=>c.setAttribute('aria-label',t('stChap',i+1,chTitle(i))));
}
function loop(now){
  raf=0;if(!st.playing||!host)return;
  st.t=Math.min(END,st.t+Math.min(.1,Math.max(0,(now-last)/1000)));last=now;draw();
  if(st.t>=END){st.playing=false;st.done=true;sync();return;}
  raf=requestAnimationFrame(loop);
}
function play(){
  if(!host)return;
  if(st.done||st.t>=END){st.t=0;st.done=false;}
  st.playing=st.started=true;st.userPaused=st.auto=false;last=performance.now();
  if(!raf)raf=requestAnimationFrame(loop);sync();
}
function pause(user){st.playing=false;if(user)st.userPaused=true;if(raf)cancelAnimationFrame(raf);raf=0;sync();}
function seek(T){st.t=Math.min(END,Math.max(0,+T||0));st.done=st.t>=END;st.started=true;draw();sync();}
function summary(){const live=st.mode!=='let';host.querySelector('.st-sum').textContent=t('stSumPre',t(live?'purposeLive':'purposeLet'))+' '+t(live?'stStillLive':'stStillLet').join(' ');}
function swap(fade){
  const stage=host.querySelector('.st-stage'),old=stage.querySelector('.st-svg:not(.st-leave)'),sv=build(st.mode);
  if(old){if(fade){old.classList.add('st-leave');sv.classList.add('st-enter');setTimeout(()=>old.remove(),450);}else old.remove();}
  stage.appendChild(sv);beat=-1;draw();summary();
}
/* Switch purpose: mid-story, carry on from the same moment; otherwise start the other story from the beginning */
function setMode(m,invite){
  m=m==='let'?'let':'live';if(!host||m===st.mode)return;
  st.mode=m;
  if(RM.matches)swap(false);
  else if(st.done||!st.started){st.t=0;st.done=false;swap(false);play();if(invite)host.querySelector('.st-play').focus();}
  else swap(true);
  sync();
}
function capHeight(){
  if(!host)return;const cap=host.querySelector('.st-cap'),w=cap.clientWidth;if(!w)return;
  const m=cap.cloneNode(false);m.classList.add('st-measure');m.style.width=w+'px';host.appendChild(m);
  let h=0;[true,false].forEach(live=>beats(live).forEach(b=>{m.innerHTML=capHtml(capFor(b[1],live));h=Math.max(h,m.offsetHeight);}));
  m.remove();cap.style.minHeight=h+'px';
}
function onClick(e){
  const b=e.target.closest('[data-st],[data-st-ch],[data-st-mode]');if(!b||!host||!host.contains(b))return;
  if(b.dataset.stMode)setMode(b.dataset.stMode);
  else if(b.dataset.stCh!=null){const i=+b.dataset.stCh;if(RM.matches){pause(true);seek(STILL[i]);}else{seek(CH[i]);play();}}
  else if(b.dataset.st==='play'){if(st.playing)pause(true);else play();}
  else if(b.dataset.st==='invite')setMode(st.mode==='let'?'live':'let',true);
}
function observe(){
  if(io)io.disconnect();io=null;
  if(!('IntersectionObserver' in window)){st.inView=true;if(!st.started&&!RM.matches)play();return;}
  io=new IntersectionObserver(es=>{const r=es[es.length-1].intersectionRatio;
    if(r>=.4){st.inView=true;if(!RM.matches&&!st.userPaused&&!st.done&&!st.playing&&(!st.started||st.auto)&&!document.hidden)play();}
    else if(r<.15){st.inView=false;if(st.playing){pause();st.auto=true;}}},{threshold:[0,.15,.4]});
  io.observe(host);
}
document.addEventListener('visibilitychange',()=>{if(!host)return;
  if(document.hidden){if(st.playing){pause();st.auto=true;}}else if(st.auto&&st.inView&&!st.userPaused)play();});
window.addEventListener('resize',()=>{clearTimeout(rz);rz=setTimeout(capHeight,150);});
/* Called after every render: builds the figure when the front page shows it, stops when it doesn't */
function mount(root){
  const fig=(root||document).querySelector('#story');
  if(!fig){if(raf)cancelAnimationFrame(raf);raf=0;st.playing=false;if(io)io.disconnect();io=null;host=null;return;}
  if(fig===host)return;
  host=fig;beat=-1;fresh=true;
  FAM=getComputedStyle(fig).fontFamily||FAM;
  if(RM.matches&&!st.started){st.t=END;st.done=st.started=true;}
  const mode=m=>'<button type="button" class="st-mode" data-st-mode="'+m+'" aria-pressed="false">'+I[m]+'<span>'+esc(t(m==='live'?'purposeLive':'purposeLet'))+'</span></button>';
  fig.innerHTML='<div class="st-modes" role="group" aria-label="'+esc(t('purposeH'))+'">'+mode('live')+mode('let')+'</div><div class="st-stage"></div>'
    +'<div class="st-ctl"><button type="button" class="st-play" data-st="play">'+I.play+I.pause+I.replay+'<span class="st-lbl">'+esc(t('stReplay'))+'</span></button>'
    +'<div class="st-chs" role="group" aria-label="'+esc(t('stChaps'))+'">'+[0,1,2,3].map(i=>'<button type="button" class="st-ch" data-st-ch="'+i+'"><span aria-hidden="true">'+(i+1)+'</span><i aria-hidden="true"><b></b></i></button>').join('')+'</div>'
    +'<button type="button" class="btn small st-inv" data-st="invite"><span class="st-il"></span> <span aria-hidden="true">→</span></button></div>'
    +'<figcaption><p class="st-cap" aria-hidden="true"></p><p class="sr st-sum"></p></figcaption>';
  fig.querySelector('.st-stage').appendChild(build(st.mode));
  fig.addEventListener('click',onClick);
  summary();draw();sync();capHeight();observe();
  if(st.playing){last=performance.now();if(!raf)raf=requestAnimationFrame(loop);}
}
window.Story={mount:mount,seek:seek,play:play,pause:()=>pause(true),setMode:setMode,state:()=>Object.assign({end:END,chapters:CH.slice()},st)};
})();
