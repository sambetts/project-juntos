// Checks for the co-buying prototype. Run inside prototype/: node test_proto.js
// Exits with code 1 if any check fails. Screen checks run in both languages (English and Spanish).
'use strict';
const fs=require('fs'),path=require('path');
const html=fs.readFileSync(path.join(__dirname,'juntos-co-buying-prototype.html'),'utf8');
const script=html.split('<script>')[1].split('</script>')[0];
const logic=script.split('/* ===== LOGIC START ===== */')[1].split('/* ===== LOGIC END ===== */')[0];
const L=new Function(logic+'\nreturn {STR,setLang,PROFILES,PRESETS,WEIGHTS,listAnd,listOr,names,hhLabel,hard,score,explain,accepts,likesBack,mutual,othersAccept,groupStatus,bestGroups,topGroup,groupCheck,groupScore,houseBrief,roomPlan,placementTips,agreementTopics,groupMembers};')();
let passed=0;const failures=[];
function check(name,cond,detail){if(cond)passed++;else failures.push(name+(detail!==undefined&&detail!==null?' ['+detail+']':''));}
const clone=o=>JSON.parse(JSON.stringify(o));
const byId=id=>L.PROFILES.find(p=>p.id===id);

/* 1. Scoring (language-independent) */
const everyone=L.PROFILES.concat([L.PRESETS.family.user,L.PRESETS.room.user]);
let asym=0;everyone.forEach(a=>everyone.forEach(b=>{if(a!==b&&L.score(a,b).total!==L.score(b,a).total)asym++;}));
check('scores are symmetric',asym===0,asym);
check('weights add up to 1',Math.abs(Object.values(L.WEIGHTS).reduce((s,x)=>s+x,0)-1)<1e-9);
check('household type is not a score component',!('household' in L.WEIGHTS));
let hhDiff=0;L.PROFILES.forEach(p=>{const a=clone(p),b=clone(p);b.household=a.household==='single'?'couple':'single';L.PROFILES.forEach(q=>{if(q.id!==p.id&&L.score(a,q).total!==L.score(b,q).total)hhDiff++;});});
check('changing household type alone never changes a score',hhDiff===0,hhDiff);
let accDiff=0;L.PROFILES.forEach(p=>{const a=clone(p);delete a.needs.stepfree;delete a.needs.lift;const b=clone(a);b.needs.stepfree='must';b.needs.lift='must';L.PROFILES.forEach(q=>{if(q.id!==p.id&&L.score(a,q).total!==L.score(b,q).total)accDiff++;});});
check('accessibility needs never change a score',accDiff===0,accDiff);
const g1=clone(L.PRESETS.room.user);g1.needs={garden:'must'};
const gMust=clone(g1);gMust.id='m';const gNice=clone(g1);gNice.id='n';gNice.needs={garden:'nice'};const g1n=clone(g1);g1n.needs={garden:'nice'};
const mm=L.score(g1,gMust).parts.home,mn=L.score(g1,gNice).parts.home,nn=L.score(g1n,gNice).parts.home;
check('shared must-haves count more than must + nice, which count more than shared nice-to-haves',mm>mn&&mn>nn,[mm,mn,nn].join(' / '));
const noWish=clone(g1);noWish.needs={};
check('having no wish-list is neutral',L.score(noWish,gMust).parts.home===0.75);

/* 2. Consent and groups (language-independent) */
const summary=[];
['family','room'].forEach(k=>{
  const u=L.PRESETS[k].user;
  const ok=L.PROFILES.filter(p=>L.hard(u,p).ok);
  check(k+': has compatible people',ok.length>0,ok.length);
  check(k+': no group suggestions before any likes',L.bestGroups(u,{},[],3).length===0);
  const liked={};ok.forEach(p=>{liked[p.id]=true;});
  const mutuals=ok.filter(p=>L.likesBack(u,p));
  check(k+': some mutual matches after liking everyone compatible',mutuals.length>0,mutuals.length);
  let lb=0;ok.forEach(p=>{if(L.likesBack(u,p)!==(L.hard(p,u).ok&&L.score(p,u).total>=p.pickiness))lb++;});
  check(k+': each simulated reply follows that person\'s own pickiness',lb===0,lb);
  const sugg=L.bestGroups(u,liked,[],3);
  check(k+': at least one group suggestion',sugg.length>0);
  let bad=0;sugg.forEach(g=>{const others=g.ids.map(byId);others.forEach(p=>{if(!L.mutual(u,p,liked))bad++;});if(!L.othersAccept(others).ok)bad++;if(!L.groupStatus(u,g.ids,liked).ok)bad++;});
  check(k+': every suggestion is mutual matches who accept each other',bad===0,bad);
  const top=L.topGroup(sugg);
  check(k+': the best group is the highest-scoring one',!!top&&sugg.every(g=>g.min<top.min||(g.min===top.min&&g.avg<=top.avg)));
  check(k+': suggestions are listed best first',!!top&&top===sugg[0]);
  const half={};ok.slice(0,Math.ceil(ok.length/2)).forEach(p=>{half[p.id]=true;});
  let leak=0;L.bestGroups(u,half,[],3).forEach(g=>g.ids.forEach(id=>{if(!half[id])leak++;}));
  check(k+': suggestions only include people you liked',leak===0,leak);
  summary.push(k+': '+ok.length+' compatible, '+mutuals.length+' mutual after liking all; groups: '+sugg.map(g=>g.ids.join('+')+' '+g.min+'%/'+g.avg+'%').join(' | '));
});
check('a group listing someone twice is refused',!L.groupStatus(L.PRESETS.room.user,['diego','diego'],{diego:true}).ok);

/* 3. Home brief */
const hb=L.houseBrief([L.PRESETS.family.user,byId('marta')]);
check('size is a range around the estimate',hb.m2min<hb.m2&&hb.m2<hb.m2max,[hb.m2min,hb.m2,hb.m2max].join('/'));

/* 4. Colour contrast (WCAG 2.2 AA: 4.5:1 for normal text) */
function lum(h){h=h.replace('#','');const c=[0,2,4].map(i=>parseInt(h.substr(i,2),16)/255).map(v=>v<=0.03928?v/12.92:Math.pow((v+0.055)/1.055,2.4));return 0.2126*c[0]+0.7152*c[1]+0.0722*c[2];}
function ratio(a,b){const x=lum(a),y=lum(b);return (Math.max(x,y)+0.05)/(Math.min(x,y)+0.05);}
const css=html.split('<style>')[1].split('</style>')[0];
const vars=blk=>{const o={};(blk.match(/--[\w-]+:#[0-9a-fA-F]{6}/g)||[]).forEach(m=>{const kv=m.split(':');o[kv[0].slice(2)]=kv[1];});return o;};
const light=vars(css.split('@media (prefers-color-scheme: dark)')[0]);
const dark=Object.assign({},light,vars(css.split('@media (prefers-color-scheme: dark)')[1].split('*{')[0]));
const pairs=[['text','bg'],['text','surface'],['text','surface-2'],['muted','bg'],['muted','surface'],['muted','surface-2'],['brand-ink','brand'],['brand','brand-soft'],['brand','surface'],['teal','teal-soft'],['teal','surface'],['good','good-bg'],['warn','warn-bg'],['warn','surface'],['bg','text']];
[['light',light],['dark',dark]].forEach(([n,v])=>pairs.forEach(([a,b])=>{const r=ratio(v[a],v[b]);check(n+' contrast: '+a+' on '+b,r>=4.5,r.toFixed(2));}));
[...new Set((logic.match(/color:'#[0-9a-fA-F]{6}'/g)||[]).map(m=>m.slice(7,14)))].forEach(c=>{const r=ratio('#ffffff',c);check('avatar initials readable on '+c,r>=4.5,r.toFixed(2));});

/* 5. The two languages */
const keysEn=Object.keys(L.STR.en).sort(),keysEs=Object.keys(L.STR.es).sort();
check('copy: English and Spanish have the same strings',JSON.stringify(keysEn)===JSON.stringify(keysEs),keysEn.filter(k=>keysEs.indexOf(k)<0).concat(keysEs.filter(k=>keysEn.indexOf(k)<0)).join(','));
let missing=[];
['AREAS','TYPES','HOUSEHOLDS','FEATURES','HORIZONS','SLIDERS'].forEach(n=>{const arr=new Function(logic+'\nreturn '+n+';')();arr.forEach(o=>['label','plural','lo','hi'].forEach(f=>{if(o[f]&&typeof o[f]==='object'&&(!o[f].en||!o[f].es))missing.push(n+'.'+o.id+'.'+f);}));});
L.PROFILES.forEach(p=>{if(!p.bio.en||!p.bio.es)missing.push('bio.'+p.id);});
check('copy: every label and bio exists in both languages',!missing.length,missing.join(','));
L.setLang('es');
check('copy (es): "y" becomes "e" before an i sound',L.listAnd(['Laura','Irene'])==='Laura e Irene'&&L.listAnd(['Laura','Diego'])==='Laura y Diego');
check('copy (es): "o" becomes "u" before an o sound',L.listOr(['Madrid centro','oeste'])==='Madrid centro u oeste'&&L.listOr(['piso','casa'])==='piso o casa');
check('copy (es): you go last in a list of names',L.names([L.PRESETS.family.user,byId('laura')])==='Laura y tú');
check('copy (es): household labels count children',L.hhLabel(byId('laura'))==='Familia monoparental · 1 hijo'&&L.hhLabel(byId('marta'))==='Familia · 2 adultos + 2 hijos');
L.setLang('en');
check('copy (en): plain "and" and "or"',L.listAnd(['Laura','Irene'])==='Laura and Irene'&&L.listOr(['piso','oeste'])==='piso or oeste');
check('copy (en): you go last in a list of names',L.names([L.PRESETS.family.user,byId('laura')])==='Laura and you');
check('copy (en): household labels count children',L.hhLabel(byId('laura'))==='Single parent + 1 kid'&&L.hhLabel(byId('marta'))==='Family · 2 adults + 2 kids');
L.setLang('es');
check('copy: no "añadirle" (leísmo with women)',!/añadirle/.test(html));

/* 6. Screens, in a minimal fake page, in each language */
class El{constructor(n){this.n=n;this.innerHTML='';this.textContent='';this.attrs={};this.classList={add(){},remove(){}};}addEventListener(){}querySelectorAll(){return [];}setAttribute(k,v){this.attrs[k]=v;}}
const els={app:new El('app'),tabs:new El('tabs'),bar:new El('bar'),toast:new El('toast'),root:new El('root'),tagline:new El('tagline'),badge:new El('badge'),footnote:new El('footnote'),steps:new El('steps'),sw:new El('lang-switch')};
const byElId={app:els.app,tabs:els.tabs,tagline:els.tagline,badge:els.badge,footnote:els.footnote,steps:els.steps,'lang-switch':els.sw};
global.window={addEventListener(){},scrollTo(){},scrollY:0};
global.document={getElementById:id=>byElId[id]||null,querySelector:s=>s==='#actionbar .wrap'?els.bar:(s==='#toast div'?els.toast:null),addEventListener(){},documentElement:els.root,title:''};
global.localStorage={getItem(){return null;},setItem(){}};
global.location={hash:''};
global.setTimeout=()=>0;global.clearTimeout=()=>{};
const tags=['div','span','section','ul','li','details','summary','button','label','article','table','tr','td','th','p','select','b','h2','h3','h4'];
/* Words that give away the other language. Proper names (Las Rozas, La Paz) and the brand are capitalised, so only lower-case words are listed. */
const LEAK={
  es:/(^|[^\wÀ-ÿ])(the|you|your|and|with|of|bedrooms?|bathrooms?|households?|must-have|nice to have|like|pass|remove|undo|group|home)(?=$|[^\wÀ-ÿ])/,
  en:/(^|[^\wÀ-ÿ])(de|que|con|para|los|una|tu|vuestra|hogar|hogares|dormitorios?|baños?|imprescindible|deseable|personas?)(?=$|[^\wÀ-ÿ])/
};
function screenText(){return [els.app.innerHTML,els.tabs.innerHTML,els.bar.innerHTML,els.tagline.textContent,els.badge.textContent,els.footnote.textContent].join(' ').replace(/<[^>]*>/g,' ').replace(/\s+/g,' ');}
function smoke(lang,name,s){
  const bad=tags.filter(t=>(s.match(new RegExp('<'+t+'[\\s>]','g'))||[]).length!==(s.match(new RegExp('</'+t+'>','g'))||[]).length);
  check(lang+' '+name+': tags balanced',!bad.length,bad.join(','));
  const m=s.match(/.{0,50}(undefined|NaN|\[object|null%).{0,30}/);
  check(lang+' '+name+': no broken values',!m,m&&m[0]);
  const txt=screenText(),lk=txt.match(new RegExp('.{0,30}'+LEAK[lang].source+'.{0,30}',lang==='es'?'i':''));
  check(lang+' '+name+': no words from the other language',!lk,lk&&lk[0]);
}
function run(hash,steps){global.location.hash=hash;new Function(script)();steps(global.window.__juntos);}
const W={
  en:{simulated:/simulated/,preview:/Preview only/,already:/already in your group/,area:/Useful area, rough/,illustrative:/Illustrative bedroom allocation only/,notAgreement:/not the agreement itself/,broken:/doesn(&#39;|')t work as it stands/,attention:/needs attention/,privacy:[/You(&#39;|')re looking for|They(&#39;|')re looking for/,/They can(&#39;|')t live with pets/,/They(&#39;|')d rather/],switchTo:'es',title:/find the people/},
  es:{simulated:/se simulan/,preview:/Solo vista previa/,already:/ya está en tu grupo/,area:/Superficie útil, aprox\./,illustrative:/Reparto de dormitorios solo orientativo/,notAgreement:/no el acuerdo en sí/,broken:/no funciona tal como está/,attention:/necesita atención/,privacy:[/No busca compartir/,/No puede vivir con mascotas/,/Prefiere que no fume nadie/],switchTo:'en',title:/encuentra con quién/}
};
['en','es'].forEach(lang=>{
  const w=W[lang];
  run('#lang='+lang+'&preset=family',J=>{
    check(lang+': the link hook sets the language',J.lang===lang,J.lang);
    check(lang+': the page language attribute matches',els.root.attrs.lang===lang,els.root.attrs.lang);
    check(lang+': the title is translated',w.title.test(global.document.title),global.document.title);
    check(lang+': the switch offers the other language, marked with its own lang',els.sw.attrs['data-id']===w.switchTo&&els.sw.attrs.lang===w.switchTo,JSON.stringify(els.sw.attrs));
    ['you','matches','home'].forEach(t=>{J.go(t);smoke(lang,'family '+t,els.app.innerHTML);});
    J.go('matches');
    const m0=els.app.innerHTML;
    check(lang+' family: nothing to propose before any likes',!/data-action="propose"/.test(m0));
    check(lang+' family: matches page says replies are simulated',w.simulated.test(m0));
    check(lang+' family: other people\'s rejection reasons stay private',!w.privacy.some(r=>r.test(m0)));
    const ok=J.lists().ok.map(o=>o.p);
    ok.forEach(p=>J.like(p.id));
    J.go('matches');smoke(lang,'family matches after likes',els.app.innerHTML);
    check(lang+' family: group suggestions appear once there are mutual matches',/data-action="propose"/.test(els.app.innerHTML));
    const top=L.topGroup(L.bestGroups(J.state.user,J.state.likes,[],3));
    J.previewGroup(top.ids);
    check(lang+' family: preview leaves your group empty',J.state.group.length===0,J.state.group.join(','));
    check(lang+' family: preview is labelled as a preview',w.preview.test(els.app.innerHTML));
    smoke(lang,'family preview',els.app.innerHTML);
    J.proposeGroup(top.ids);
    check(lang+' family: proposing forms the group',JSON.stringify(J.state.group)===JSON.stringify(top.ids),J.state.group.join(','));
    check(lang+' family: the preview closes after proposing',J.preview===null);
    J.addToGroup(top.ids[0]);
    check(lang+' family: adding someone twice is refused',J.state.group.length===top.ids.length&&w.already.test(els.toast.textContent),els.toast.textContent);
    const notMutual=ok.find(p=>!L.likesBack(J.state.user,p));
    if(notMutual){const n=J.state.group.length;J.addToGroup(notMutual.id);check(lang+' family: someone who is not a mutual match cannot be added',J.state.group.length===n,els.toast.textContent);}
    J.go('home');const h=els.app.innerHTML;smoke(lang,'family home',h);
    check(lang+' family: the brief shows a useful-area range',w.area.test(h)&&/m²/.test(h));
    check(lang+' family: the target size is editable',/data-size="min"/.test(h)&&/data-size="max"/.test(h));
    check(lang+' family: the room split is labelled illustrative',w.illustrative.test(h));
    check(lang+' family: agreement prompts say they are not the agreement',w.notAgreement.test(h));
    check(lang+' tabs: ARIA roles, one tab in the tab order',/role="tab"/.test(els.tabs.innerHTML)&&/aria-controls="app"/.test(els.tabs.innerHTML)&&(els.tabs.innerHTML.match(/tabindex="0"/g)||[]).length===1);
    check(lang+' tabs: the panel is labelled by the active tab',els.app.attrs['aria-labelledby']==='tab-home',els.app.attrs['aria-labelledby']);
    const before=JSON.stringify(J.state.group);
    J.setLang(w.switchTo);
    check(lang+': switching language keeps the group and the tab',JSON.stringify(J.state.group)===before&&J.state.tab==='home');
    check(lang+': switching language redraws the page in the other language',els.root.attrs.lang===w.switchTo&&W[w.switchTo].illustrative.test(els.app.innerHTML));
    smoke(w.switchTo,'home after switching',els.app.innerHTML);
  });
  run('#lang='+lang+'&preset=room&like=all&group=best&tab=home',J=>{
    check(lang+' room: link hooks form a consented group',J.state.group.length>0&&L.groupStatus(J.state.user,J.state.group,J.state.likes).ok,J.state.group.join(','));
    smoke(lang,'room home',els.app.innerHTML);
    check(lang+' room: a valid group gets a brief',/class="headline"/.test(els.app.innerHTML));
    const nm=J.lists().ok.map(o=>o.p).find(p=>!L.likesBack(J.state.user,p));
    check(lang+' room: there is someone who does not like you back (test data)',!!nm);
    if(nm){const n=J.state.group.length;J.addToGroup(nm.id);check(lang+' room: someone who is not a mutual match cannot be added',J.state.group.length===n,els.toast.textContent);}
    J.state.user.open=[];J.render();
    const h=els.app.innerHTML;smoke(lang,'room home after edit',h);
    check(lang+' room: the brief is hidden once a profile edit breaks the group',!/class="headline"/.test(h)&&w.broken.test(h));
    J.go('you');
    check(lang+' room: the action bar flags the broken group',w.attention.test(els.bar.innerHTML));
  });
});
check('page: the main area is a tab panel',/id="app" role="tabpanel"/.test(html));
check('page: no "waiting for them" copy',!/waiting for them|esperando su respuesta/.test(html));
check('page: a profile saved by the older English-only version is not read',!/juntos-prototype-v2/.test(html));

console.log(summary.join('\n'));
if(failures.length){console.log('\n'+failures.length+' check(s) FAILED:\n- '+failures.join('\n- '));process.exit(1);}
console.log('\nAll '+passed+' checks passed.');
