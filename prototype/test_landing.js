// Checks for the landing-page draft (B-007). Run inside prototype/: node test_landing.js
// Exits with code 1 if any check fails. Screen checks run in Spanish and in English.
'use strict';
const fs=require('fs'),path=require('path');
const html=fs.readFileSync(path.join(__dirname,'compicasa-landing-draft.html'),'utf8');
const script=html.split('<script>')[1].split('</script>')[0];
const logic=script.split('/* ===== LOGIC START ===== */')[1].split('/* ===== LOGIC END ===== */')[0];
const L=new Function(logic+'\nreturn {BRAND,STR,setLang,t,validEmail,checkForm};')();
let passed=0;const failures=[];
function check(name,cond,detail){if(cond)passed++;else failures.push(name+(detail!==undefined&&detail!==null?' ['+detail+']':''));}

/* 1. Sends and stores nothing; safe to open anywhere */
check('safety: no external scripts, styles, images or fonts',!/(src|href)\s*=\s*["']?\s*(https?:)?\/\//i.test(html)&&!/@import|url\(/i.test(html));
check('safety: no inline event handlers',!/<[^>]+\son[a-z]+\s*=/i.test(html));
check('safety: no javascript: links',!/javascript:/i.test(html));
const sinks=script.match(/fetch\s*\(|XMLHttpRequest|sendBeacon|WebSocket|EventSource|localStorage|sessionStorage|indexedDB|document\.cookie|console\.|navigator\.|\.submit\s*\(\s*\)|window\.open|location\s*=|location\.(href|assign|replace)/g);
check('safety: the script has no way to send, store or log anything',!sinks,sinks&&sinks.join(','));
const formTag=(script.match(/<form[^>]*>/)||[''])[0];
check('safety: the form has no action or method',!!formTag&&!/action=|method=/.test(formTag),formTag);
check('safety: no field has a name, so even without JavaScript nothing can be submitted',!/<(input|select|textarea|button)[^>]*\sname=/.test(html)&&/<input [^>]*id="f-email"/.test(script));
check('safety: every submit is intercepted',/addEventListener\('submit'[\s\S]{0,40}preventDefault\(\)/.test(script));
check('draft: search engines are asked not to index it',/<meta name="robots" content="noindex, nofollow">/.test(html));

/* 2. The provisional name lives in one place (D-007) */
check('name: set once in BRAND',(script.match(/Compicasa/g)||[]).length===1&&L.BRAND==='Compicasa');
check('name: elsewhere only in the static title',(html.match(/Compicasa/g)||[]).length===2&&/<title>Compicasa /.test(html));

/* 3. The two languages */
const ke=Object.keys(L.STR.en).sort(),ks=Object.keys(L.STR.es).sort();
check('copy: English and Spanish have the same strings',JSON.stringify(ke)===JSON.stringify(ks),ke.filter(k=>ks.indexOf(k)<0).concat(ks.filter(k=>ke.indexOf(k)<0)).join(','));
check('copy: no empty strings',ke.every(k=>L.STR.en[k]&&L.STR.es[k]));
const used=[...new Set((script.match(/\bt\('([A-Za-z0-9]+)'\)/g)||[]).map(m=>m.slice(3,-2)))];
const dyn=['s1t','s2t','s3t','s4t','s1d','s2d','s3d','s4d','not1','not2','not3','not4','q1','q2','q3','q4','q5','q6','a1','a2','a3','a4','a5','a6','areaMadrid','areaRegion','areaBcn','areaVlc','areaOther','when1','when2','when3','when4'];
const unknown=used.concat(dyn).filter(k=>!(k in L.STR.es));
check('copy: every string the page asks for exists',!unknown.length,unknown.join(','));
const unused=ks.filter(k=>used.indexOf(k)<0&&dyn.indexOf(k)<0);
check('copy: no unused strings',!unused.length,unused.join(','));
const es=Object.values(L.STR.es).join(' \n '),en=Object.values(L.STR.en).join(' \n ');
check('copy (es): addresses the reader as tú or vosotros, never usted',!/\busted(es)?\b/i.test(es));
check('copy (es): "y" becomes "e" before an i sound',!/\sy\s+h?[iíIÍ]/.test(es));
check('copy (es): "o" becomes "u" before an o sound',!/\so\s+h?[oóOÓ]/.test(es));
check('copy (es): Spanish quotation and question marks are opened',(es.match(/\?/g)||[]).length===(es.match(/¿/g)||[]).length&&(es.match(/!/g)||[]).length===(es.match(/¡/g)||[]).length);
/* Honest claims only (Brand & Marketing rules): no promises about money, mortgages, savings or legal outcomes */
const promise=/garantiz|guarante|ahorr|savings?\b|save money|sin riesgo|risk[- ]free|seguro que|te conseguimos|we('ll| will) get you|asesoramos|we advise|mejor precio|best price|aprobad|approved/i;
check('claims: no promises about money, savings, mortgages or legal outcomes',!promise.test(es)&&!promise.test(en),(es+en).match(promise)&&(es+en).match(promise)[0]);
check('claims: says plainly it gives no legal, tax or financial advice',/asesoramiento legal, fiscal ni financiero/.test(es)&&/legal, tax or financial advice/.test(en));
/* Data minimisation (D-009; B-013 still to come): email, area, timing and consent only */
check('data: the form asks only for email, area, timing and consent',(script.match(/<input /g)||[]).length===2&&(script.match(/<select /g)||[]).length===2);
check('data: no questions on money, household or protected characteristics',!/income|salary|ingres|sueldo|ahorros|nacionalidad|nationality|género|gender|religi[oó]n\b.*\?|edad\?|age\?/i.test(Object.keys(L.STR.es).filter(k=>/Label$/.test(k)).map(k=>L.STR.es[k]+L.STR.en[k]).join(' ')));

/* 4. Email check */
check('email: plain addresses pass',['ana@example.es','a.b+c@mail.example.com',' ana@example.es '].every(L.validEmail));
check('email: broken addresses fail',['','ana','ana@','@example.es','ana@example','ana @example.es','ana@.es'].every(x=>!L.validEmail(x)));
check('form: reports which fields fail',JSON.stringify(L.checkForm('',false))==='["email","consent"]'&&JSON.stringify(L.checkForm('ana@example.es',true))==='[]');

/* 5. Colour contrast (WCAG 2.2 AA: 4.5:1 for normal text) */
function lum(h){h=h.replace('#','');const c=[0,2,4].map(i=>parseInt(h.substr(i,2),16)/255).map(v=>v<=0.03928?v/12.92:Math.pow((v+0.055)/1.055,2.4));return 0.2126*c[0]+0.7152*c[1]+0.0722*c[2];}
function ratio(a,b){const x=lum(a),y=lum(b);return (Math.max(x,y)+0.05)/(Math.min(x,y)+0.05);}
const css=html.split('<style>')[1].split('</style>')[0];
const vars=blk=>{const o={};(blk.match(/--[\w-]+:#[0-9a-fA-F]{6}/g)||[]).forEach(m=>{const kv=m.split(':');o[kv[0].slice(2)]=kv[1];});return o;};
const light=vars(css.split('@media (prefers-color-scheme: dark)')[0]);
const dark=Object.assign({},light,vars(css.split('@media (prefers-color-scheme: dark)')[1].split('*{')[0]));
const pairs=[['text','bg'],['text','surface'],['text','surface-2'],['muted','bg'],['muted','surface'],['muted','surface-2'],['brand-ink','brand'],['brand','brand-soft'],['brand','surface'],['teal','teal-soft'],['teal','surface'],['teal','bg'],['good','good-bg'],['warn','warn-bg'],['warn','surface'],['bg','text']];
[['light',light],['dark',dark]].forEach(([n,v])=>pairs.forEach(([a,b])=>{const r=ratio(v[a],v[b]);check(n+' contrast: '+a+' on '+b,r>=4.5,r.toFixed(2));}));

/* 6. Screens, in a minimal fake page, in each language */
class El{constructor(id){this.id=id;this.innerHTML='';this.textContent='';this.attrs={};this.focused=0;}setAttribute(k,v){this.attrs[k]=v;}focus(){this.focused++;lastFocus=this.id;}}
let lastFocus=null;const els={};
const getEl=id=>els[id]||(els[id]=new El(id));
const root=new El('html');
global.window={};
global.document={getElementById:getEl,addEventListener(){},documentElement:root,title:''};
global.location={hash:''};
const tags=['div','span','section','ul','ol','li','details','summary','button','label','p','select','option','form','header','main','footer','h1','h2','h3','a'];
const LEAK={
  es:/(^|[^\wÀ-ÿ])(the|you|your|and|with|of|we|home|homes|buy|join|draft|email|bedrooms?|bathrooms?|households?|must-have|nice to have|lawyer)(?=$|[^\wÀ-ÿ])/i,
  en:/(^|[^\wÀ-ÿ])(de|que|con|para|los|las|una|tu|vivienda|casa|hogares?|dormitorios?|baños?|imprescindible|deseable|personas?|borrador|abogado)(?=$|[^\wÀ-ÿ])/i
};
function text(s){return s.replace(/<[^>]*>/g,' ').replace(/&#39;/g,"'").replace(/&amp;/g,'&').replace(/\s+/g,' ');}
function smoke(lang,name){
  const s=els.app.innerHTML;
  const bad=tags.filter(t=>(s.match(new RegExp('<'+t+'[\\s>]','g'))||[]).length!==(s.match(new RegExp('</'+t+'>','g'))||[]).length);
  check(lang+' '+name+': tags balanced',!bad.length,bad.join(','));
  const m=s.match(/.{0,40}(undefined|NaN|\[object|null).{0,30}/);
  check(lang+' '+name+': no broken values',!m,m&&m[0]);
  const raw=s.match(/.{0,30}\{brand\}.{0,10}/);
  check(lang+' '+name+': the name is filled in everywhere',!raw,raw&&raw[0]);
  const tx=text(s).replace(/Español|English/g,''),lk=tx.match(new RegExp('.{0,30}'+LEAK[lang].source+'.{0,30}','i'));
  check(lang+' '+name+': no words from the other language',!lk,lk&&lk[0]);
}
function run(hash,fn){global.location.hash=hash;for(const k in els)delete els[k];new Function(script)();fn(global.window.__landing);}
const W={
  es:{title:/^Compicasa – encuentra con quién comprar tu vivienda \(borrador\)$/,other:'en',draft:/no guarda ni envía nada/,done:/no hemos guardado ni enviado/,errE:/correo electrónico válido/,errC:/Marca la casilla/,skip:'Ir al formulario'},
  en:{title:/^Compicasa – find who to buy your home with \(draft\)$/,other:'es',draft:/saves and sends nothing/,done:/haven(&#39;|')t saved or sent/,errE:/valid email/,errC:/Tick the box/,skip:'Skip to the form'}
};
['es','en'].forEach(lang=>{
  const w=W[lang];
  run('#lang='+lang,J=>{
    const h=els.app.innerHTML;
    check(lang+': the link hook sets the language',J.lang===lang,J.lang);
    check(lang+': the page language attribute matches',root.attrs.lang===lang,root.attrs.lang);
    check(lang+': the title is translated',w.title.test(global.document.title),global.document.title);
    check(lang+': the skip link is translated',els.skip&&els.skip.textContent===w.skip);
    check(lang+': the switch offers the other language, marked with its own lang',new RegExp('id="lang-switch" data-id="'+w.other+'" lang="'+w.other+'"').test(h));
    check(lang+': the draft banner says nothing is saved or sent',w.draft.test(h));
    check(lang+': one main heading',(h.match(/<h1[\s>]/g)||[]).length===1);
    check(lang+': every field has a label',['f-email','f-area','f-when','f-consent'].every(id=>new RegExp('<label for="'+id+'"').test(h)&&new RegExp('id="'+id+'"').test(h)));
    check(lang+': the form result is announced',/id="status" role="status"/.test(h));
    check(lang+': the calls to action point at real sections',/href="#join"/.test(h)&&/id="join"/.test(h)&&/href="#how"/.test(h)&&/id="how"/.test(h));
    smoke(lang,'page');

    J.submit('ana',true);
    check(lang+': a bad email is refused with a message',!J.state.sent&&w.errE.test(els.app.innerHTML)&&/id="f-email"[^>]*aria-invalid="true"/.test(els.app.innerHTML));
    check(lang+': focus moves to the field to fix',lastFocus==='f-email',lastFocus);
    smoke(lang,'email error');
    J.submit('ana@example.es',false);
    check(lang+': consent is required',!J.state.sent&&w.errC.test(els.app.innerHTML)&&lastFocus==='f-consent');
    smoke(lang,'consent error');
    J.submit('ana@example.es',true);
    const d=els.app.innerHTML;
    check(lang+': a valid sign-up shows the draft thank-you',J.state.sent&&w.done.test(d)&&!w.errE.test(d));
    check(lang+': the thank-you gets focus',lastFocus==='status',lastFocus);
    check(lang+': the email is kept nowhere: not in the state, not on the page',!/ana@example/.test(JSON.stringify(J.state))&&!/ana@example/.test(d));
    smoke(lang,'after sign-up');

    J.setLang(w.other);
    check(lang+': switching language redraws the page in the other language',root.attrs.lang===w.other&&W[w.other].done.test(els.app.innerHTML));
    check(lang+': focus stays on the switch',lastFocus==='lang-switch',lastFocus);
    smoke(w.other,'after switching');
  });
});
run('',J=>{check('page: opens in Spanish by default',J.lang==='es'&&root.attrs.lang==='es',J.lang);});
run('#lang=fr',J=>{check('page: an unknown language falls back to Spanish',J.lang==='es',J.lang);});

if(failures.length){console.log(failures.length+' check(s) FAILED:\n- '+failures.join('\n- '));process.exit(1);}
console.log('All '+passed+' checks passed.');
