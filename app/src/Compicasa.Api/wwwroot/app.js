/* Compicasa PoC front end (B-028, D-028): one installable web app for phones and desktops.
   Views follow prototype/compicasa-prototype.html; data, matching and consent come from the C# API.
   Wording comes only from i18n.js (Spanish first, English supported: D-024). */
'use strict';
(function(){
window.addEventListener('error',e=>{document.documentElement.setAttribute('data-error',String(e.message));});

/* ---------- labels (ported from the prototype) ---------- */
const PET_WORDS_=PET_WORDS;
function hh(id){return HOUSEHOLDS.find(h=>h.id===id)||HOUSEHOLDS[0];}
function inter(a,b){return a.filter(x=>b.indexOf(x)>=0);}
function conj(word,next){if(LANG!=='es')return word;const s=String(next).toLowerCase();if(word==='y'&&/^h?[ií](?![aeiouáéó])/.test(s))return 'e';if(word==='o'&&/^h?[oó]/.test(s))return 'u';return word;}
function listJoin(items,word){items=items.filter(Boolean);if(items.length<=1)return items.join('');const last=items[items.length-1];return items.slice(0,-1).join(', ')+' '+conj(word,last)+' '+last;}
function listAnd(items){return listJoin(items,t('and'));}
function listOr(items){return listJoin(items,t('or'));}
function cap(s){return s?s.charAt(0).toUpperCase()+s.slice(1):s;}
function areaLabel(id){const a=AREAS.find(x=>x.id===id);return a?tx(a.label):id;}
function typeLabel(id){const x=TYPES.find(y=>y.id===id);return x?tx(x.label):id;}
function featLabel(id){const f=FEATURES.find(x=>x.id===id);return f?tx(f.label):id;}
function horizonLabel(id){const h=HORIZONS.find(x=>x.id===id);return h?tx(h.label):id;}
function petText(pets){return listAnd(pets.map(p=>tx(PET_WORDS_[p])||p));}
function homeType(types){return types.length===1?typeLabel(types[0]).toLowerCase():(types.length?t('typeBoth'):t('typeNone'));}
/* B-063: live in it or rent it out */
function isLet(p){return !!p&&p.purpose==='let';}
function purposeLabel(p){return t(p==='let'?'purposeLet':'purposeLive');}
/* Score parts in a fixed order; a letting score has no "how you live" (B-063) */
function partKeys(parts){return Object.keys(WEIGHTS).filter(k=>parts&&parts[k]!=null);}
function hhLabel(p){
  if(p.household==='family')return t('hhFamily',p.adults,t('kids',p.kids));
  if(p.household==='singleParent')return t('hhSingleParent',t('kids',p.kids));
  return tx(hh(p.household).label);
}

/* ---------- people and messages from the API ---------- */
const PEOPLE={};
function remember(map){if(map)Object.keys(map).forEach(k=>{PEOPLE[k]=map[k];});}
function rememberLite(map){if(map)Object.keys(map).forEach(k=>{PEOPLE[k]=Object.assign({},map[k],PEOPLE[k]||{});});}
function isMe(id){return S.me&&id===S.me.id;}
function personName(id){return PEOPLE[id]?PEOPLE[id].name:'?';}
function nm(id){return isMe(id)?t('you'):personName(id);}
function dn(id){return isMe(id)?t('youName'):personName(id);}
/* "Laura and you" / "Laura y tú": the viewer goes last */
function names(ids){return listAnd(ids.filter(id=>!isMe(id)).concat(ids.filter(isMe)).map(nm));}
function initials(n){if(n===STR.en.youName||n===STR.es.youName)return n;const parts=n.split(' & ');return parts.length>1?parts.map(s=>s.trim().charAt(0)).join(''):n.charAt(0);}
/* How to put each message's arguments into words; anything not listed passes through */
const ARGS={notOpen:['hh'],petsNo:['pets'],noArea:['areas'],onlyType:['types'],has:['pets'],sameHorizon:['h'],diffHorizon:['h','h'],
  bothWant:['f'],kidsInHome:['kids'],maxHH:['n','ids'],pairReason:['name','msg'],clash:['ids'],notMutual:['name'],othersReject:['ids'],
  wfhWho:['ids'],dogWho:['ids'],petWho:['ids'],smokerWho:['ids'],keepTheirs:['name'],clusters:['ids'],owlsLarks:['ids','ids'],ensuite:['ids']};
function arg(kind,v){
  switch(kind){
    case 'hh':return tx(hh(v).plural);
    case 'pets':return petText(v);
    case 'areas':return listOr(v.map(areaLabel));
    case 'types':return listOr(v.map(x=>typeLabel(x).toLowerCase()));
    case 'h':return horizonLabel(v);
    case 'f':return featLabel(v);
    case 'kids':return t('kids',v);
    case 'ids':return names(v);
    case 'name':return nm(v);
    case 'msg':return M(v);
    default:return v;
  }
}
function M(m){
  if(!m)return '';
  const kinds=ARGS[m.key]||[];
  const args=(m.args||[]).map((v,i)=>arg(kinds[i],v));
  const out=t.apply(null,[m.key].concat(args));
  return typeof out==='string'?out:String(out);
}

/* ---------- state ---------- */
const S={me:null,config:null,view:null,wiz:0,authError:'',tab:'you',authMode:'signin',profile:null,matches:null,groups:null,home:null,draft:[],save:'',busy:false,
  /* Session 19: unread counts, the inbox, conversations, the open conversation and blocked people */
  counts:{alerts:0,messages:0,invites:0},inbox:null,chats:null,chat:null,chatKey:null,blocks:null};
const app=document.getElementById('app');
const tabsEl=document.getElementById('tabs');
const barEl=document.querySelector('#actionbar .wrap');
const toastEl=document.querySelector('#toast div');
const LANG_KEY='compicasa-lang';

function esc(s){return String(s==null?'':s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'})[c]);}

/* ---------- API ---------- */
async function api(method,path,body){
  let r;
  try{r=await fetch(path,{method:method,credentials:'same-origin',headers:{'Content-Type':'application/json','X-Compicasa':'1'},body:body===undefined?undefined:JSON.stringify(body)});}
  catch(e){throw {code:'network'};}
  if(r.status===204)return null;
  let data=null;try{data=await r.json();}catch(e){}
  if(!r.ok){
    const code=(data&&data.error)||(r.status===429?'rateLimited':(r.status===401?'unauthorised':(r.status>=500?'server':'generic')));
    if(r.status===401&&S.me){S.me=null;render();}
    throw {status:r.status,code:code,issues:data&&data.issues};
  }
  return data;
}
function errText(e){
  if(e&&e.issues&&e.issues.length)return M(e.issues[0]);
  const k='err_'+(e&&e.code);
  return STR.es[k]!==undefined?t(k):t('err_generic');
}

/* ---------- small view helpers ---------- */
function loc(opts){return opts.map(o=>({id:o.id,label:tx(o.label),hint:tx(o.hint)}));}
function avatar(id,size,color,name){
  const p=PEOPLE[id]||{},c=color||p.color||'#495057',n=name||dn(id);
  return `<span class="avatar" data-bg="${esc(c)}" data-sz="${size}" aria-hidden="true">${esc(initials(n))}</span>`;
}
function bar(label,v){const pct=Math.round(v*100);return `<div class="bar"><span>${esc(label)}</span><div class="track"><div class="fill" data-w="${pct}"></div></div><span>${pct}%</span></div>`;}
function kp(v,k){return `<div class="kpi"><div class="v">${v}</div><div class="k">${k}</div></div>`;}
function radioGroup(field,opts,val,label){
  return `<div class="field"><span class="label">${esc(label)}</span><div class="seg" role="radiogroup" aria-label="${esc(label)}">${opts.map(o=>`<label class="opt"><input type="radio" name="f-${field}" data-field="${field}" value="${esc(o.id)}"${String(o.id)===String(val)?' checked':''}>${esc(o.label)}</label>`).join('')}</div></div>`;
}
function checkGroup(field,label,opts,vals){
  return `<div class="field"><span class="label">${esc(label)}</span><div class="seg">${opts.map(o=>`<label class="opt"><input type="checkbox" data-field="${field}" data-multi="1" value="${esc(o.id)}"${vals.indexOf(o.id)>=0?' checked':''}>${esc(o.label)}${o.hint?` <span class="hint">${esc(o.hint)}</span>`:''}</label>`).join('')}</div></div>`;
}
/* Areas across Spain (B-059): one folding group per region, open where you have chosen something (Madrid if nothing yet)
   or where you opened it. The checkboxes are the same multi field as any other, so saving is unchanged. */
const OPEN_REGIONS=new Set();
document.addEventListener('toggle',e=>{const d=e.target;if(d&&d.getAttribute&&d.getAttribute('data-region')){if(d.open)OPEN_REGIONS.add(d.getAttribute('data-region'));else OPEN_REGIONS.delete(d.getAttribute('data-region'));}},true);
function areaPicker(vals){
  const chosen=AREAS.filter(a=>vals.indexOf(a.id)>=0);
  const sorted=REGIONS.slice(0,1).concat(REGIONS.slice(1).sort((a,b)=>tx(a.label).localeCompare(tx(b.label),LANG)));
  const groups=sorted.map(r=>{
    const n=r.areas.filter(a=>vals.indexOf(a.id)>=0).length,open=n>0||OPEN_REGIONS.has(r.id)||(!vals.length&&r.id==='mad');
    return `<details class="region" data-region="${r.id}"${open?' open':''}><summary>${esc(tx(r.label))}${areaCount(n)}</summary><div class="seg">${r.areas.map(a=>`<label class="opt"><input type="checkbox" data-field="areas" data-multi="1" value="${esc(a.id)}"${vals.indexOf(a.id)>=0?' checked':''}>${esc(tx(a.label))}${a.hint?` <span class="hint">${esc(tx(a.hint))}</span>`:''}</label>`).join('')}</div></details>`;}).join('');
  return `<div class="field"><span class="label">${esc(t('areasLabel'))}</span><p class="hint" id="areas-chosen" aria-live="polite">${esc(areasChosenText(chosen))}</p><div class="regions">${groups}</div></div>`;
}
function areaCount(n){return n?` <span class="pill">${n}</span>`:'';}
function areasChosenText(chosen){return chosen.length?t('areasChosen',listAnd(chosen.map(a=>tx(a.label)))):t('areasHint');}
/* Ticking a box saves without redrawing (focus stays put), so the summary and counts are updated in place */
function refreshAreaPicker(vals){
  const h=document.getElementById('areas-chosen');if(!h)return;
  h.textContent=areasChosenText(AREAS.filter(a=>vals.indexOf(a.id)>=0));
  document.querySelectorAll('details.region').forEach(d=>{const r=REGIONS.find(x=>x.id===d.getAttribute('data-region'));if(!r)return;
    const n=r.areas.filter(a=>vals.indexOf(a.id)>=0).length,sum=d.querySelector('summary'),old=sum.querySelector('.pill');
    if(old)old.remove();if(n)sum.insertAdjacentHTML('beforeend',areaCount(n));});
}
function checkField(field,label,val){return `<div class="field"><label class="opt"><input type="checkbox" data-field="${field}" data-bool="1"${val?' checked':''}>${esc(label)}</label></div>`;}
function selectField(field,label,opts,val){
  const o2=opts.map(o=>typeof o==='object'?o:{id:o,label:String(o)});
  return `<div class="field"><label for="f-${field}">${esc(label)}</label><select id="f-${field}" data-field="${field}">${o2.map(o=>`<option value="${esc(o.id)}"${String(o.id)===String(val)?' selected':''}>${esc(o.label)}</option>`).join('')}</select></div>`;
}
function slider(s,val){return `<div class="slider"><label for="f-${s.id}">${esc(tx(s.label))}</label><input id="f-${s.id}" type="range" min="1" max="5" step="1" value="${val}" data-field="${s.id}"><div class="ends"><span>${esc(tx(s.lo))}</span><span>${esc(tx(s.hi))}</span></div></div>`;}
function featBtn(f,level){
  const lv=level||'none',txt=lv==='must'?t('lvMust'):(lv==='nice'?t('lvNice'):t('lvNone'));
  return `<button type="button" class="fbtn" data-action="feat" data-id="${f.id}" data-level="${lv}" aria-label="${esc(tx(f.label))}: ${txt}">${esc(tx(f.label))}<small>${txt}</small></button>`;
}
/* Inline styles are blocked by the page's security policy, so colours and sizes are set from script */
const HEX=/^#[0-9a-fA-F]{3,8}$/;
function applyStyles(root){
  root.querySelectorAll('[data-bg]').forEach(el=>{const c=el.getAttribute('data-bg');if(HEX.test(c))el.style.background=c;});
  root.querySelectorAll('[data-sz]').forEach(el=>{const s=+el.getAttribute('data-sz');el.style.inlineSize=s+'px';el.style.blockSize=s+'px';el.style.fontSize=Math.round(s*0.36)+'px';});
  root.querySelectorAll('[data-w]').forEach(el=>{el.style.inlineSize=(+el.getAttribute('data-w'))+'%';});
  root.querySelectorAll('[data-p]').forEach(el=>{el.style.setProperty('--p',String(+el.getAttribute('data-p')));});
}

/* ---------- presets (from the prototype) ---------- */
const PRESETS={
  family:{label:{en:'Family wanting half a house',es:'Familia que busca media casa'},v:{purpose:'live',household:'family',adults:2,kids:2,bedrooms:3,privateBath:true,cars:1,pets:[],acceptsPets:true,smoking:'no',acceptsSmoking:'outside',
    sleep:2,tidy:4,social:2,guests:2,meals:2,wfh:true,horizon:'10+',areas:['north','city'],types:['house'],
    needs:{parking:'must',garden:'must',schools:'must',quiet:'nice',pool:'nice',storage:'nice'},open:['family','singleParent'],maxHouseholds:2}},
  room:{label:{en:'Just a room, open to anyone',es:'Solo una habitación, con quien sea'},v:{purpose:'live',household:'single',adults:1,kids:0,bedrooms:1,privateBath:false,cars:0,pets:[],acceptsPets:true,smoking:'no',acceptsSmoking:'outside',
    sleep:3,tidy:3,social:3,guests:3,meals:3,wfh:false,horizon:'5-9',areas:['centre','city','north','south','east'],types:['flat','house'],
    needs:{transport:'must'},open:['single','couple','family','singleParent'],maxHouseholds:4}}
};
const NUM_FIELDS=['bedrooms','kids','cars','maxHouseholds','sleep','tidy','social','guests','meals'];

/* ---------- views ---------- */
function consentFields(){
  const inv=S.config&&S.config.invite;
  const terms=`${esc(t('termsPre'))}<a href="/#terms" target="_blank" rel="noopener">${esc(t('termsLink'))}</a>${esc(t('termsMid'))}<a href="/#privacy" target="_blank" rel="noopener">${esc(t('privacyLink'))}</a>`;
  return `${inv?`<div class="field"><label for="a-invite">${esc(t('invite'))}</label><input id="a-invite" name="inviteCode" autocomplete="off" autocapitalize="none" spellcheck="false" required><p class="hint">${esc(t('inviteHint'))}</p></div>`:''}
<div class="field consent"><label class="check"><input type="checkbox" id="a-adult" name="adult" required><span>${esc(t('adultLabel'))}</span></label>
<label class="check"><input type="checkbox" id="a-terms" name="terms" required><span>${terms}</span></label></div>`;
}
function passwordField(up){
  return `<div class="field"><label for="a-pass">${esc(t('password'))}</label><div class="pw"><input id="a-pass" name="password" type="password" autocomplete="${up?'new-password':'current-password'}" minlength="${up?10:1}" required><button type="button" class="btn small ghost" data-action="pw-toggle" aria-controls="a-pass" aria-pressed="false" aria-label="${esc(t('showPwAria'))}">${esc(t('showPw'))}</button></div>
${up?`<div class="meter" id="pw-meter" data-level="0" aria-hidden="true"><span></span></div><p class="hint" id="pw-label" aria-live="polite">${esc(t('passwordHint'))}</p>`:''}</div>`;
}
function viewAuth(){
  const up=S.authMode==='signup',ext=S.config&&S.config.auth==='external';
  let body;
  if(ext&&!up)body=`<p>${esc(t('extSignInLead'))}</p><p class="error" id="auth-error" role="alert">${esc(S.authError)}</p><button type="button" class="btn primary wide" data-action="ext-signin">${esc(t('extSignInBtn'))}</button>`;
  else if(ext)body=`<form id="auth-form" novalidate><p>${esc(t('extSignUpLead'))}</p>${consentFields()}<p class="notice card" id="ext-tip"><b>${esc(t('extSignUpTip'))}</b></p><p class="error" id="auth-error" role="alert">${esc(S.authError)}</p><button type="submit" class="btn primary"${S.busy?' disabled':''}>${esc(t('extContinue'))}</button></form>`;
  else body=`<form id="auth-form" novalidate>
<div class="field"><label for="a-email">${esc(t('email'))}</label><input id="a-email" name="email" type="email" inputmode="email" autocomplete="email" autocapitalize="none" spellcheck="false" required>${up?`<p class="hint">${esc(t('emailHint'))}</p>`:''}</div>
${passwordField(up)}
${up?`<div class="field"><label for="a-name">${esc(t('displayName'))}</label><input id="a-name" name="name" maxlength="40" autocomplete="nickname" required><p class="hint">${esc(t('displayNameHint'))}</p></div>${consentFields()}`:''}
<p class="error" id="auth-error" role="alert">${esc(S.authError)}</p>
<button type="submit" class="btn primary"${S.busy?' disabled':''}>${esc(up?t('signUp'):t('signIn'))}</button>
</form>`;
  return `<section class="card auth">
<h2>${esc(t('authH'))}</h2>
<p class="lead">${esc(t('authLead'))}</p>
<div class="seg mb" role="tablist">
<button type="button" class="tab" role="tab" data-action="auth-mode" data-id="signin" aria-selected="${!up}">${esc(t('signIn'))}</button>
<button type="button" class="tab" role="tab" data-action="auth-mode" data-id="signup" aria-selected="${up}">${esc(t('signUp'))}</button>
</div>
${body}</section>`;
}
/* A rough guide, not a rule: the server only insists on 10 characters */
function pwLevel(v){
  if(v.length<10)return 0;
  const kinds=[/[a-z]/,/[A-Z]/,/[0-9]/,/[^A-Za-z0-9]/].filter(r=>r.test(v)).length;
  let n=1;if(v.length>=14)n++;if(kinds>=3||v.length>=20)n++;return n;
}
function updateMeter(v){
  const m=document.getElementById('pw-meter'),l=document.getElementById('pw-label');if(!m||!l)return;
  const lv=pwLevel(v);m.setAttribute('data-level',v?String(lv):'0');
  l.textContent=v?t('pwMeter',[t('pwShort'),t('pwWeak'),t('pwFair'),t('pwStrong')][lv]):t('passwordHint');
}

/* The in-app guide (#help, B-050): plain sections from i18n, open signed in or not */
function viewHelp(){
  return `<p><button type="button" class="btn small ghost" data-action="legal-back">← ${esc(t('legalBack'))}</button></p>
<section class="card legal help"><h2>${esc(t('helpH'))}</h2><p class="lead">${esc(t('helpLead'))}</p>
${t('helpSecs').map((x,i)=>`<h3>${esc(x.h)}</h3>${i===0?'<ol>':'<ul>'}${x.items.map(y=>`<li>${esc(y)}</li>`).join('')}${i===0?'</ol>':'</ul>'}`).join('')}</section>`;
}
/* Where homes come from (#sources): which agencies the crawl reads, when, and what we keep (D-035). Open signed in or not. */
function viewSources(){
  if(!S.sources){S.sources={loading:true};api('GET','/api/homes/sources').then(d=>{S.sources={data:d};if(S.view==='sources')rerender();},()=>{S.sources={error:true};if(S.view==='sources')rerender();});}
  const r=S.sources;
  const when=iso=>{try{return new Date(iso).toLocaleString(locale(),{day:'numeric',month:'short',year:'numeric',hour:'2-digit',minute:'2-digit',timeZone:'Europe/Madrid'});}catch(e){return '';}};
  const list=r.loading?`<p class="hint">${esc(t('srcLoading'))}</p>`:(r.error||!r.data.sources.length?`<p class="hint">${esc(t(r.error?'srcError':'srcNone'))}</p>`:
    `<ul class="sources">${r.data.sources.map(x=>{const host=x.site.replace(/^https?:\/\/(www\.)?/,'').replace(/\/$/,'');
      return `<li><b>${esc(x.name)}</b> · <a href="${esc(x.site)}" target="_blank" rel="noopener">${esc(host)}</a><br><span class="hint">${esc(t('srcHomes',num(x.homes)))} · ${esc(x.lastChecked?t('srcLast',when(x.lastChecked)):t('srcNotYet'))}</span></li>`;}).join('')}</ul>`);
  const portals=r.data&&r.data.portals?`<p class="hint">${esc(t('srcPortals'))} ${r.data.portals.map(esc).join(', ')}.</p>`:'';
  return `<p><button type="button" class="btn small ghost" data-action="legal-back">← ${esc(t('legalBack'))}</button></p>
<section class="card legal help"><h2>${esc(t('srcH'))}</h2><p class="lead">${esc(t('srcLead'))}</p>
<h3>${esc(t('srcNowH'))}</h3>${list}<p class="hint">${esc(t('srcMore'))}</p>
${t('srcSecs').map(x=>`<h3>${esc(x.h)}</h3><ul>${x.items.map(y=>`<li>${esc(y)}</li>`).join('')}</ul>${x.portals?portals:''}${x.bot?`<pre>User-agent: CompicasaBot
Disallow: /</pre><p class="hint"><a href="/bot" target="_blank" rel="noopener">${esc(t('srcBotLink'))}</a></p>`:''}`).join('')}</section>`;
}
/* The release funnel (#funnel, B-074): only for the admins the server lists; anyone else sees a short note. */
function viewFunnel(){
  if(!S.funnel){S.funnel={loading:true};api('GET','/api/admin/funnel').then(d=>{S.funnel=d.allowed?{data:d.funnel}:{denied:true};if(S.view==='funnel')rerender();},()=>{S.funnel={denied:true};if(S.view==='funnel')rerender();});}
  const r=S.funnel,back=`<p><button type="button" class="btn small ghost" data-action="legal-back">← ${esc(t('legalBack'))}</button></p>`;
  if(r.loading||r.denied)return back+`<section class="card legal"><h2>${esc(t('fnH'))}</h2><p class="hint">${esc(t(r.loading?'fnLoading':'fnDenied'))}</p></section>`;
  const d=r.data,rows=t('fnRows');
  const pct=(n,of)=>of?` <span class="hint">(${num(Math.round(100*n/of))} %)</span>`:'';
  const base={livingIn:'onboarded',letting:'onboarded',liked:'onboarded',matched:'onboarded',matched14:'cohort14',groupsShortlisting:'groupsFormed',groupsAgreedHome:'groupsFormed',onboarded:'signedUp'};
  const table=Object.keys(rows).map(k=>`<tr><th scope="row">${esc(rows[k])}</th>${[d.all,d.madrid].map(c=>`<td>${num(c[k])}${base[k]?pct(c[k],c[base[k]]):''}</td>`).join('')}</tr>`).join('');
  const weeks=d.weeks.map(w=>`<tr><th scope="row">${esc(w.week)}</th><td>${num(w.signedUp)}</td><td>${num(w.onboarded)}</td><td>${num(w.matches)}</td></tr>`).join('');
  return back+`<section class="card legal"><h2>${esc(t('fnH'))}</h2><p class="lead">${esc(t('fnLead'))}</p>
<table class="mt"><thead><tr><th></th><th scope="col">${esc(t('fnAll'))}</th><th scope="col">${esc(t('fnMadrid'))}</th></tr></thead><tbody>${table}</tbody></table>
<h3>${esc(t('fnWeeksH'))}</h3><table><thead><tr><th scope="col">${esc(t('fnWeek'))}</th><th scope="col">${esc(rows.signedUp)}</th><th scope="col">${esc(rows.onboarded)}</th><th scope="col">${esc(rows.matched)}</th></tr></thead><tbody>${weeks}</tbody></table>
<p class="hint">${esc(t('fnAt',new Date(d.at).toLocaleString(locale(),{timeZone:'Europe/Madrid'})))}</p></section>`;
}
function viewLegal(kind){
  const body=t(kind+'Body');
  return `<p><button type="button" class="btn small ghost" data-action="legal-back">← ${esc(t('legalBack'))}</button></p>
<section class="card legal"><h2>${esc(t(kind+'H'))}</h2>
<p class="notice card">${esc(t('legalDraft'))}</p>
<ol class="mt">${body.map(x=>`<li>${esc(x)}</li>`).join('')}</ol></section>`;
}

const SECTIONS={
  purpose:u=>`<div class="field"><span class="label">${esc(t('purposeQ'))}</span><div class="seg purpose" role="radiogroup" aria-label="${esc(t('purposeQ'))}">${['live','let'].map(id=>`<label class="opt"><input type="radio" name="f-purpose" data-field="purpose" value="${id}"${u.purpose===id?' checked':''}>${esc(purposeLabel(id))} <span class="hint">${esc(t(id==='let'?'purposeLetHint':'purposeLiveHint'))}</span></label>`).join('')}</div></div>
<p class="hint">${esc(t('purposeNote'))}</p>`,
  letting:u=>`${selectField('bedrooms',t('letBedsLabel'),[1,2,3,4],u.bedrooms)}
${selectField('maxHouseholds',t('letMaxLabel'),[{id:2,label:t('maxHH2')},{id:3,label:'3'},{id:4,label:t('maxHH4')}],u.maxHouseholds)}
<p class="hint">${esc(t('letSkipped'))}</p>`,
  name:u=>{const bio=u.bio?(u.bio.es||''):'';return `<div class="field"><label for="f-name">${esc(t('nameLabel'))}</label><input id="f-name" data-field="name" maxlength="40" autocomplete="nickname" value="${esc(u.name)}"></div>
<div class="field"><label for="f-bio">${esc(t('bioLabel'))}</label><textarea id="f-bio" data-field="bio" maxlength="400" rows="3">${esc(bio)}</textarea><p class="hint">${esc(t('bioHint'))}</p></div>`;},
  household:u=>{const hasKids=u.household==='family'||u.household==='singleParent';return `${radioGroup('household',loc(HOUSEHOLDS),u.household,t('yourHousehold'))}
${hasKids?selectField('kids',t('kidsLabel'),[1,2,3,4,5],u.kids):''}`;},
  space:u=>`${selectField('bedrooms',t('bedroomsLabel'),[1,2,3,4],u.bedrooms)}
${checkField('privateBath',t('ownBath'),u.privateBath)}
${selectField('cars',t('parkingLabel'),[0,1,2],u.cars)}
${checkGroup('pets',t('petsLabel'),[{id:'dog',label:t('dog')},{id:'cat',label:t('cat')}],u.pets)}`,
  live:u=>`${SLIDERS.map(x=>slider(x,u[x.id])).join('')}
${checkField('wfh',t('wfhLabel'),u.wfh)}
${selectField('smoking',t('smokeQ'),[{id:'no',label:t('smokeNo')},{id:'outside',label:t('smokeOutOpt')},{id:'inside',label:t('smokeInOpt')}],u.smoking)}`,
  share:u=>`<p class="hint">${t('noHouseholdFilter')}</p>
${selectField('maxHouseholds',t('maxHHLabel'),[{id:2,label:t('maxHH2')},{id:3,label:'3'},{id:4,label:t('maxHH4')}],u.maxHouseholds)}
${checkField('acceptsPets',t('okPets'),u.acceptsPets)}
${selectField('acceptsSmoking',t('smokersQ'),[{id:'none',label:t('smokersNone')},{id:'outside',label:t('smokersOut')},{id:'any',label:t('smokersAny')}],u.acceptsSmoking)}`,
  where:u=>`${areaPicker(u.areas)}
${checkGroup('types',t('typesLabel'),loc(TYPES),u.types)}
${radioGroup('horizon',loc(HORIZONS),u.horizon,isLet(u)?t('letHorizonLabel'):t('horizonLabel'))}`,
  budget:u=>`<div class="field"><label for="f-maxBudget">${esc(t('budgetLabel'))}</label><input id="f-maxBudget" type="number" inputmode="numeric" min="10000" max="5000000" step="5000" data-field="maxBudget" value="${u.maxBudget==null?'':u.maxBudget}" placeholder="150000"><p class="hint">${esc(t('budgetHint'))}</p></div>`,
  needs:u=>`<p class="hint">${t('needsHint')}</p>
<div class="feat">${FEATURES.map(f=>featBtn(f,u.needs[f.id])).join('')}</div>`
};
function presetButtons(){return Object.keys(PRESETS).map(k=>`<button type="button" class="btn small" data-action="preset" data-id="${k}">${esc(tx(PRESETS[k].label))}</button>`).join('');}

/* One line per section: what you answered, so the profile reads as a summary until you open a section (B-051) */
const SUMMARY={
  purpose:u=>purposeLabel(u.purpose),
  letting:u=>[t('letBedsN',u.bedrooms),t('sumMaxHH',u.maxHouseholds)].join(' · '),
  name:u=>[u.name,u.bio?t('sumBio'):''].filter(Boolean).join(' · '),
  household:u=>cap(hhLabel(u)),
  space:u=>[t('bedroomsN',u.bedrooms),u.privateBath?t('ownBathFact'):t('sharesBath'),u.cars?t('parkingN',u.cars):t('sumNoParking'),u.pets.length?cap(petText(u.pets)):''].filter(Boolean).join(' · '),
  live:u=>[u.wfh?t('wfhFact'):'',t('sumSmoke_'+u.smoking),t('sumSliders')].filter(Boolean).join(' · '),
  share:u=>[t('sumMaxHH',u.maxHouseholds),u.acceptsPets?t('okPets'):''].filter(Boolean).join(' · '),
  where:u=>[cap(listOr(u.areas.map(areaLabel))),homeType(u.types),t(isLet(u)?'keeping':'staying',horizonLabel(u.horizon))].filter(Boolean).join(' · '),
  budget:u=>u.maxBudget==null?t('sumNoBudget'):t('sumBudget',euros(u.maxBudget)),
  needs:u=>{const k=Object.keys(u.needs),m=k.filter(x=>u.needs[x]==='must'),n=k.filter(x=>u.needs[x]==='nice');return [m.length?t('sumMust',listAnd(m.map(x=>featLabel(x).toLowerCase()))):'',n.length?t('sumNice',n.length):''].filter(Boolean).join(' · ')||t('sumNoNeeds');}
};
function viewYou(){
  const u=S.profile;if(!u)return `<p class="lead">${esc(t('loading'))}</p>`;
  if(!S.openSecs)S.openSecs={};
  const sec=(h,id)=>`<details class="psec" data-sec="${id}"${S.openSecs[id]?' open':''}><summary><span class="psec-h">${h}</span><span class="psec-s">${esc(SUMMARY[id](u))}</span><span class="psec-e" aria-hidden="true">${esc(t('editSec'))}</span></summary><div class="psec-b">${SECTIONS[id](u)}</div></details>`;
  const m=S.matches,save=S.save?` · <span role="status">${esc(t(S.save))}</span>`:'';
  return `<header class="phead2"><div><h2>${t('profileH')}</h2><p class="hint">${m?esc(t('profCount',m.compatible.length,m.hidden.length)):''}${save}</p></div><a class="btn small" href="#people-review">${esc(t('seePeople'))}</a></header>
<div class="psecs">
${sec(esc(t('nameLabel')),'name')}
${sec(esc(t('purposeH')),'purpose')}
${sec(t('whoBuying'),'household')}
${isLet(u)?sec(esc(t('letH')),'letting'):`${sec(t('privateSpace'),'space')}
${sec(t('howLive'),'live')}
${sec(t('shareWith'),'share')}`}
${sec(isLet(u)?esc(t('letWhereH')):t('whereHow'),'where')}
${sec(esc(t('budgetH')),'budget')}
${sec(t('needsH'),'needs')}
</div>
<details class="quietbox mt"><summary>${String(t('startFrom')).replace(/:\s*$/,'')}</summary><p class="hint">${esc(t('presetWarn'))}</p><div class="presets">${presetButtons()}</div></details>
<section class="account mt"><h3>${esc(t('account'))}</h3>
<p class="hint">${esc(t('signedInAs',S.me.email||''))}</p>
<div class="presets"><button type="button" class="btn small" data-action="logout">${esc(t('signOut'))}</button><button type="button" class="btn small ghost danger" data-action="delete-account">${esc(t('deleteAccount'))}</button></div>
<h4 class="mt">${esc(t('blockedH'))}</h4>
${!S.blocks?`<p class="hint">${esc(t('loading'))}</p>`:(S.blocks.length?`<ul class="clean">${S.blocks.map(b=>`<li>${avatar(b.id,28,b.color,b.name)}<div><b>${esc(b.name)}</b></div><button type="button" class="btn small ghost" data-action="unblock" data-id="${esc(b.id)}">${esc(t('unblock'))}</button></li>`).join('')}</ul>`:`<p class="hint">${esc(t('blockedNone'))}</p>`)}
</section>`;
}

/* ---------- Home (Session 20): the logo's page. Where you are, your profile score, homes that might interest you ---------- */
/* How much the profile tells others: optional answers that the wizard doesn't insist on. It never changes a match score. */
function profileScore(u){
  const bio=u.bio&&u.bio.es?u.bio.es.trim():'',live=SLIDERS.some(x=>u[x.id]!==3)||u.wfh;
  const needs=Object.keys(u.needs||{}),must=needs.some(k=>u.needs[k]==='must'),nice=needs.some(k=>u.needs[k]==='nice');
  const items=[
    {k:'name',w:10,ok:!!(u.name&&u.name.trim()),sec:'name'},
    {k:bio&&bio.length<40?'bioShort':'bio',w:25,ok:bio.length>=40,part:bio?10:0,sec:'name'},
    {k:'areas',w:10,ok:u.areas.length>0,sec:'where'},
    {k:'types',w:10,ok:u.types.length>0,sec:'where'},
    {k:'must',w:20,ok:must,sec:'needs'},
    {k:'nice',w:10,ok:nice,sec:'needs'},
    {k:live?'liveOk':'live',w:15,ok:live,sec:'live'}
  ].filter(x=>!(isLet(u)&&x.sec==='live'));
  const total=items.reduce((s,x)=>s+x.w,0);
  return {score:Math.round(100*items.reduce((s,x)=>s+(x.ok?x.w:(x.part||0)),0)/total),items:items};
}
function myGroups(){
  const gs=S.groups||[];
  return {invites:gs.filter(g=>g.myStatus==='pending'),formed:gs.find(g=>g.state==='formed'&&g.myStatus==='accepted'),
    open:gs.filter(g=>g.myStatus==='accepted'&&g.state!=='formed'&&g.state!=='broken')};
}
function viewDash(){
  const u=S.profile;if(!u)return `<p class="lead">${esc(t('loading'))}</p>`;
  const ps=profileScore(u),c=peopleCounts(),g=myGroups(),loading=!S.matches||!S.groups;
  /* The four steps; "now" is the step the one next step belongs to */
  const done=[ps.score>=60,c.matches>0,!!g.formed,false];
  /* One next step, in journey order: answer an invitation, look at homes, form a group, then the profile, then review people */
  let next=null;
  if(!loading){
    if(g.invites.length)next=[t('dn_invite'),t('dn_inviteBtn'),'#group-'+g.invites[0].id,2];
    else if(g.formed)next=[t('dn_home'),t('dn_homeBtn'),groupHash(g.formed,'homes'),3];
    else if(c.matches&&!g.open.length)next=[t('dn_group',c.matches),t('dn_groupBtn'),'#groups',2];
    else if(ps.score<60)next=[t('dn_profile'),t('dn_profileBtn'),'#profile',0];
    else if(c.review)next=[t('dn_review',c.review),t('dn_reviewBtn'),'#people-review',1];
    else next=[t('dn_wait'),'','',g.open.length?2:1];
  }
  const now=next?next[3]:-1;
  const grpLine=g.formed?t('dsGroupFormed'):(g.invites.length?t('dsGroupInvite',g.invites.length):(g.open.length?t('dsGroupWaiting'):t('dsGroupNone')));
  const steps=[['profile','#profile',t('dsScore',ps.score)],['people','#people-'+peopleSub(),loading?t('loading'):t('dsPeople',c.review,c.matches)],
    ['group','#groups',loading?t('loading'):grpLine],['home',g.formed?groupHash(g.formed,'homes'):'#groups',g.formed?t('dsHomeNow'):t('dsHomeLater')]];
  const stepper=`<ol class="dash-steps">${steps.map((s,i)=>`<li><a class="dstep${done[i]?' done':''}${i===now?' now':''}" href="${s[1]}"${i===now?' aria-current="step"':''}><span class="dn" aria-hidden="true">${done[i]?'✓':i+1}</span><b>${esc(t('ds_'+s[0]))}</b><span class="hint">${esc(s[2])}</span></a></li>`).join('')}</ol>`;
  const nextBox=next?`<div class="nextstep"><span>${esc(next[0])}</span>${next[1]?`<a class="btn small primary" href="${next[2]}">${esc(next[1])}</a>`:''}</div>`:'';
  const cls=ps.score>=80?'hi':(ps.score>=60?'mid':'lo'),missing=ps.items.filter(x=>!x.ok);
  const scoreCard=`<section class="card dash-score"><div class="scorehead"><div class="ring ${cls}" data-p="${ps.score}" role="img" aria-label="${esc(t('scoreAria',ps.score))}"><b>${ps.score}%</b></div><div><h3>${esc(t('scoreH'))}</h3><p class="hint">${esc(t('scoreLead'))}</p></div></div>
<ul class="clean checks">${ps.items.map(x=>`<li class="${x.ok?'ok':'miss'}"><span class="ck" aria-hidden="true">${x.ok?'✓':'+'}</span><div>${esc(t('sc_'+x.k))}</div>${x.ok?'':`<a class="quiet" href="#profile" data-action="open-sec" data-id="${x.sec}">${esc(t('scoreAdd'))}</a>`}</li>`).join('')}</ul>
${missing.length?'':`<p class="hint">${esc(t('scoreAll'))}</p>`}</section>`;
  const r=S.dashHomes;
  let homes;
  if(!r||r.loading)homes=`<div class="skeleton"></div>`;
  else if(r.error)homes=`<p class="error">${esc(r.error)}</p>`;
  else{
    const d=r.data,list=d.homes.slice(0,3).map(x=>Object.assign({},x,{pm2:x.price&&x.m2?x.price/x.m2:null})),med=median(list.map(x=>x.pm2));
    const who=d.with?t('dashHomesWith',d.with.name,d.with.mutual):t('dashHomesAlone');
    homes=`<p class="hint">${esc(who)}</p>${d.budgetSet===false?`<p class="budgetline">${esc(t('dashBudget'))} <a class="quiet" href="#profile" data-action="open-sec" data-id="budget">${esc(t('hbSetMine'))}</a></p>`:''}${list.length?`<ul class="homes">${list.map(x=>homeItem(x,med)).join('')}</ul><p class="hint mt">${esc(t('dashHomesNote'))}</p>`:`<p>${esc(t('dashHomesNone'))}</p>`}
${g.formed?`<p class="mt"><a class="btn small" href="${groupHash(g.formed,'homes')}">${esc(t('dashHomesGroup'))}</a></p>`:''}`;
  }
  return `<header class="phead2"><div><h2>${esc(t('dashH',u.name||''))}</h2><p class="hint">${esc(t('dashLead'))}</p></div></header>
${nextBox}
<section class="mb"><h3 class="sr">${esc(t('dashStepsH'))}</h3>${stepper}</section>
<div class="dash-grid">${scoreCard}<section class="card dash-homes"><h3>${esc(t('dashHomesH'))}</h3>${homes}</section></div>`;
}
async function loadDashHomes(){
  if(S.dashHomes)return;S.dashHomes={loading:true};
  try{S.dashHomes={data:await api('GET','/api/me/homes')};}catch(e){S.dashHomes={error:errText(e)};}
  if(S.tab==='home')rerender();
}

/* ---------- onboarding wizard: the same sections, one per step; matches unlock at the end ---------- */
/* B-063: steps marked only:'live' or only:'let' belong to one path; letting skips the living-together questions */
const WIZ=[
  {id:'name',h:()=>t('wizNameH'),lead:()=>t('wizNameLead'),check:u=>u.name&&u.name.trim()?'':t('wizNeedName')},
  {id:'purpose',h:()=>t('purposeH'),lead:()=>t('wizPurposeLead')},
  {id:'household',h:()=>t('whoBuying'),lead:u=>isLet(u)?t('wizLetHouseholdLead'):t('wizHouseholdLead'),examples:u=>!isLet(u)},
  {id:'space',only:'live',h:()=>t('privateSpace'),lead:()=>t('wizSpaceLead')},
  {id:'live',only:'live',h:()=>t('howLive'),lead:()=>t('wizLiveLead')},
  {id:'share',only:'live',h:()=>t('shareWith'),lead:()=>t('wizShareLead')},
  {id:'letting',only:'let',h:()=>t('letH'),lead:()=>t('wizLetLead')},
  {id:'where',h:u=>isLet(u)?t('letWhereH'):t('whereHow'),lead:u=>isLet(u)?t('wizLetWhereLead'):t('wizWhereLead'),check:u=>u.areas.length&&u.types.length?'':t('err_profileIncomplete')},
  {id:'budget',h:()=>t('budgetH'),lead:()=>t('wizBudgetLead'),check:u=>u.maxBudget>=10000&&u.maxBudget<=5000000?'':t('err_badBudget')},
  {id:'needs',h:()=>t('needsH'),lead:()=>t('wizNeedsLead')},
  {id:'done',h:()=>t('wizDoneH'),lead:()=>t('wizDoneLead')}
];
function wizSteps(){const p=S.profile&&S.profile.purpose==='let'?'let':'live';return WIZ.filter(s=>!s.only||s.only===p);}
function viewWizard(){
  const u=S.profile;if(!u)return `<p class="lead">${esc(t('loading'))}</p>`;
  const steps=wizSteps(),st=steps[S.wiz],n=S.wiz+1,pct=Math.round(n/steps.length*100);
  let body;
  if(st.id==='done'){
    const facts=isLet(u)?[purposeLabel('let'),hhLabel(u),t('letBedsN',u.bedrooms),listOr(u.areas.map(areaLabel)),homeType(u.types),t('keeping',horizonLabel(u.horizon))]
      :[purposeLabel('live'),hhLabel(u),t('bedroomsN',u.bedrooms),listOr(u.areas.map(areaLabel)),homeType(u.types),t('staying',horizonLabel(u.horizon))];
    body=`<section class="card"><h3>${esc(t('wizSummary'))}</h3><p><b>${esc(u.name)}</b></p><div class="facts">${facts.map(f=>`<span class="fact">${esc(cap(f))}</span>`).join('')}</div></section>`;
  }else body=`${st.examples&&st.examples(u)?`<div class="card presets mb"><span>${esc(t('wizExamples'))}</span>${presetButtons()}</div>`:''}<section class="card">${SECTIONS[st.id](u)}</section>`;
  return `<div class="wizard">
<p class="hint" id="wiz-step">${esc(t('wizStep',n,steps.length))}</p>
<div class="progress" role="progressbar" aria-labelledby="wiz-step" aria-valuemin="1" aria-valuemax="${steps.length}" aria-valuenow="${n}"><span data-w="${pct}"></span></div>
<h2>${esc(st.h(u))}</h2>
<p class="lead">${esc(st.lead(u))}</p>
${body}
<p class="error" id="wiz-error" role="alert"></p>
</div>`;
}

/* The buttons for one person, on the card and in the sheet */
function cardActions(x){
  const p=x.person,st=x.state,inDraft=S.draft.indexOf(p.id)>=0,sim=p.isDemo?` <span class="hint">${t('simReply')}</span>`:'';
  if(inDraft)return `<span class="status ok">${t('inDraft')}</span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="remove" data-id="${esc(p.id)}">${t('remove')}</button>`;
  if(st==='passed')return `<span class="status wait">${t('passedS')}</span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="undo" data-id="${esc(p.id)}">${t('undo')}</button>`;
  if(st==='none')return `<button type="button" class="btn small" data-action="pass" data-id="${esc(p.id)}">${t('pass')}</button><span class="spacer"></span><button type="button" class="btn small primary" data-action="like" data-id="${esc(p.id)}">${t('like')}</button>`;
  if(st==='mutual')return `<span class="status ok">${t('mutualS')}${sim}</span><span class="spacer"></span><a class="btn small" href="#chat-${esc(p.id)}">${esc(t('msgBtn'))}</a><button type="button" class="btn small primary" data-action="add" data-id="${esc(p.id)}">${t('addGroup')}</button>`;
  if(st==='waiting')return `<span class="status wait">${t('waitingS')}</span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="undo" data-id="${esc(p.id)}">${t('undo')}</button>`;
  return `<span class="status wait">${t('likedNo')}${sim}</span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="undo" data-id="${esc(p.id)}">${t('undo')}</button>`;
}
/* Compact by default (B-051): who, the score, two lines of bio, two reasons you click and one thing to talk about.
   Everything else is in the sheet ("More"), so cards in a row stay the same height. */
function personCard(x){
  const p=x.person,s=x.score.total,cls=s>=80?'hi':(s>=65?'mid':'lo'),st=x.state;
  const bio=p.bio?tx(p.bio):'';
  const chip=(c,good)=>`<span class="chip ${good?'good':'warn'}">${good?'✓':'⚑'} ${esc(M(c))}</span>`;
  const top=(x.likedMe?`<span class="chip liked">${esc(t('likedYou'))}</span>`:'')+x.clicks.slice(0,2).map(c=>chip(c,true)).join('')+x.talks.slice(0,1).map(c=>chip(c,false)).join('');
  return `<article class="card pcard${st==='passed'?' passed':''}">
<div class="phead">${avatar(p.id,46,p.color,p.name)}<div class="pwho"><div class="pname">${esc(p.name)}${p.isDemo?` <span class="demo">${esc(t('demoTag'))}</span>`:''}</div><div class="psub">${esc(hhLabel(p))}</div></div><div class="ring ${cls}" data-p="${s}" role="img" aria-label="${esc(t('ringAria',s))}"><b>${s}%</b></div></div>
${bio?`<p class="bio clamp">${esc(bio)}</p>`:''}
${top?`<div class="chips">${top}</div>`:''}
<a class="quiet more" href="#person-${esc(p.id)}">${esc(t('moreAbout'))}</a>
<div class="pactions">${cardActions(x)}</div>
</article>`;
}

function groupCard(g,label){
  const ids=[S.me.id].concat(g.ids);
  const line=t('homeLine',homeType(g.types),g.beds);
  return `<article class="card gcard"><div class="hint"><b>${esc(label)}</b></div>
<div class="stack">${ids.map(id=>avatar(id,38)).join('')}</div>
<div><b>${esc(ids.map(dn).join(' + '))}</b></div>
<div><span class="gscore">${g.min}%</span> <span class="hint">${esc(t('weakestAvg',g.avg))}</span></div>
<div class="hint">${esc(t('needsIn',line,listOr(g.areas.map(areaLabel))))}</div>
<div class="pactions"><button type="button" class="btn small" data-action="preview" data-id="${esc(g.ids.join(','))}">${t('previewBtn')}</button><span class="spacer"></span><button type="button" class="btn small primary" data-action="propose" data-id="${esc(g.ids.join(','))}">${t('propose')}</button></div></article>`;
}

/* People (Session 18, B-051): one job per view. To review → Liked (waiting) → Matches; Passed from a link.
   A like is silent, so someone who said no simply leaves your Liked list: no "said no" list. */
function psOf(x){const s=x.state;return s==='none'?'review':(s==='mutual'?'matches':(s==='passed'?'passed':(s==='waiting'?'liked':'gone')));}
function peopleCounts(){const c={review:0,liked:0,matches:0,passed:0,gone:0};if(S.matches)S.matches.compatible.forEach(x=>c[psOf(x)]++);return c;}
function peopleSub(){if(S.ps)return S.ps;const c=peopleCounts();return c.review||!c.matches?'review':'matches';}
/* Sub-views are links: each has an address, so Back works (B-051). items: [key,label,count,disabledReason,href] */
function subTabs(items,cur,attr,label){
  return `<nav class="subtabs" aria-label="${esc(label)}">${items.map(x=>x[3]?`<span class="stab off" aria-disabled="true">${esc(x[1])} <small>${esc(x[3])}</small></span>`:`<a class="stab" href="${x[4]}" ${attr}="${x[0]}"${x[0]===cur?' aria-current="page"':''}>${esc(x[1])}${x[2]!=null?` <span class="n">${x[2]}</span>`:''}</a>`).join('')}</nav>`;
}
function viewMatches(){
  const m=S.matches;if(!m)return `<p class="lead">${esc(t('loading'))}</p>`;
  const all=m.compatible,c=peopleCounts(),ps=peopleSub(),med=median(all.map(x=>x.score.total));
  const likedN=all.filter(x=>x.likedMe).length;
  const summary=[t('sumCompat',all.length),med!=null?t('sumMedian',Math.round(med)):'',ps==='review'&&likedN?t('likedYouN',likedN):''].filter(Boolean).join(' · ');
  const purpose=S.profile?`<p class="purposeline"><span class="pill">${esc(t('peoplePurpose',purposeLabel(S.profile.purpose)))}</span> <a class="quiet" href="#profile" data-action="open-sec" data-id="purpose">${esc(t('purposeChange'))}</a></p>`:'';
  const head=`<header class="phead2"><div><h2>${t('peopleH')}</h2><p class="hint">${esc(summary)} · <a href="#help" class="quiet">${esc(t('howScore'))}</a></p>${purpose}</div></header>
${subTabs([['review',t('ps_review'),c.review],['liked',t('ps_liked'),c.liked],['matches',t('ps_matches'),c.matches]].map(x=>x.concat(['','#people-'+x[0]])),ps,'data-ps',t('peopleH'))}`;
  /* the one next step on this page: a group, once you have a mutual match */
  const next=ps==='matches'&&c.matches?`<div class="nextstep"><span>${esc(m.suggestions.length?t('nextGroups',m.suggestions.length):t('nextAddToGroup'))}</span>${m.suggestions.length?`<a class="btn small primary" href="#groups">${esc(t('nextGroupsBtn'))}</a>`:''}</div>`:'';
  const passedTitle=ps==='passed'?`<p class="crumb"><a class="linkbtn" href="#people-review">← ${esc(t('backToReview'))}</a></p><h3>${esc(t('passedH',c.passed))}</h3>`:'';
  const tail=ps==='review'?[c.passed?`<a class="quiet" href="#people-passed">${esc(t('seePassed',c.passed))}</a>`:'',m.hidden.length?`<details class="quietbox"><summary>${esc(t('hiddenN',m.hidden.length))} <span class="hint">${t('seeWhy')}</span></summary><ul class="clean mt">${m.hidden.map(h=>`<li>${avatar(h.person.id,32,h.person.color,h.person.name)}<div><b>${esc(h.person.name)}</b> <span class="hint">· ${esc(hhLabel(h.person))}</span><div class="who">${esc(h.reasons.map(M).join(' · '))}</div></div></li>`).join('')}</ul></details>`:''].filter(Boolean):[];
  return `${head}${next}${passedTitle}
${c[ps]>3?matchesToolbar():''}
<div id="mx-results">${matchesResults()}</div>${tail.length?`<div class="tail mt">${tail.join('')}</div>`:''}${personSheet()}`;
}

function memberStatus(st){return st&&st!=='preview'?t('ms_'+st):'';}

function viewGroups(){
  if(S.home)return viewHome(S.home);
  const gs=S.groups;if(!gs)return `<p class="lead">${esc(t('loading'))}</p>`;
  const m=S.matches,have=gs.filter(g=>g.state!=='broken').map(g=>g.members.map(x=>x.id).filter(id=>!isMe(id)).sort().join(','));
  const sugg=(m?m.suggestions:[]).filter(g=>have.indexOf(g.ids.slice().sort().join(','))<0);
  /* Invitations waiting for your answer first, then groups that could work, then your groups (B-051) */
  const card=g=>{
    const ids=g.members.map(x=>x.id),invite=g.myStatus==='pending';
    const waiting=g.members.filter(x=>!isMe(x.id)&&x.status==='pending').map(x=>x.id);
    const by=g.life&&g.life.invitedBy||g.createdBy;
    const line=invite?t('invitedBy',dn(by))+(g.life&&g.life.expiresAt?' · '+t('answerBy',day(g.life.expiresAt)):''):(g.state==='formed'?t('gFormedLine'):(waiting.length?t('gWaitingFor',names(waiting)):(g.state==='broken'?t('gBrokenLine'):t('st_'+g.state))));
    return `<article class="card gcard${invite?' invite':''}"><div class="grow-row"><div class="stack">${ids.map(id=>avatar(id,38)).join('')}</div><span class="pill ${g.state==='formed'?'ok':(invite?'act':'wait')}">${esc(invite?t('pillInvite'):t('pill_'+g.state))}</span></div>
<div><b>${esc(ids.map(dn).join(' + '))}</b></div>
<div class="who">${esc(line)}</div>
<div class="pactions">${invite?`<button type="button" class="btn small" data-action="respond" data-id="${g.id}" data-accept="0">${t('decline')}</button><a class="btn small ghost" href="#group-${g.id}" data-action="open-group" data-id="${g.id}">${t('openGroup')}</a><span class="spacer"></span><button type="button" class="btn small primary" data-action="respond" data-id="${g.id}" data-accept="1">${t('accept')}</button>`:`<span class="spacer"></span><a class="btn small primary" href="#group-${g.id}" data-action="open-group" data-id="${g.id}">${t('openGroup')}</a>`}</div></article>`;
  };
  const inv=gs.filter(g=>g.myStatus==='pending'),mine=gs.filter(g=>g.myStatus!=='pending');
  const label=(g,i)=>(i===0?t('topSugg'):'')+(g.size===2?t('pairOf'):t('groupOf',g.size));
  const suggBlock=sugg.length?`<section class="mb"><h3>${t('groupsH')}</h3><p class="hint">${esc(t('groupsHint'))}</p><div class="grid groups">${sugg.map((g,i)=>groupCard(g,label(g,i))).join('')}</div><p class="hint mt">${esc(t('ownGroupHint'))} <a class="quiet" href="#people-matches">${esc(t('ownGroupLink'))}</a></p></section>`:'';
  const empty=!gs.length&&!sugg.length?`<div class="card empty"><p class="lead">${esc(m&&m.mutualCount?t('noGroupYet'):t('noGroups'))}</p><a class="btn primary" href="#people-review">${t('goMatches')}</a></div>`:'';
  return `<header class="phead2"><div><h2>${t('groupsPageH')}</h2><p class="hint">${esc(t('groupsListLead'))}</p></div></header>
${inv.length?`<section class="mb"><h3>${esc(t('invitesH',inv.length))}</h3><div class="grid groups">${inv.map(card).join('')}</div></section>`:''}
${suggBlock}
${mine.length?`<section><h3>${esc(t('yourGroupsH'))}</h3><div class="grid groups">${mine.map(card).join('')}</div></section>`:''}${empty}`;
}

/* A person's details open in a sheet (a dialog; full screen on phones) with its own address, #person-<id> */
function personSheet(){
  const x=S.person&&S.matches?S.matches.compatible.find(y=>y.person.id===S.person):null;if(!x)return '';
  const p=x.person,s=x.score.total,cls=s>=80?'hi':(s>=65?'mid':'lo');
  const facts=(isLet(p)?[purposeLabel('let'),t('letBedsN',p.bedrooms),t('keeping',horizonLabel(p.horizon))]
    :[t('bedroomsN',p.bedrooms),p.privateBath?t('ownBathFact'):t('sharesBath'),p.cars?t('parkingN',p.cars):'',p.wfh?t('wfhFact'):'',t('staying',horizonLabel(p.horizon))]).filter(Boolean);
  const tg=x.together,line=t('homeLine',homeType(tg.types),tg.beds),rest=t('bathsN',tg.baths)+(tg.cars?' · '+t('parkingN',tg.cars):'');
  const bio=p.bio?tx(p.bio):'';
  return `<dialog class="sheet" id="person-sheet" aria-labelledby="sheet-h"><div class="sheet-in">
<div class="phead">${avatar(p.id,52,p.color,p.name)}<div class="pwho"><h2 id="sheet-h" class="pname">${esc(p.name)}${p.isDemo?` <span class="demo">${esc(t('demoTag'))}</span>`:''}</h2><div class="psub">${esc(hhLabel(p))}</div></div><div class="ring ${cls}" data-p="${s}" role="img" aria-label="${esc(t('ringAria',s))}"><b>${s}%</b></div><button type="button" class="btn small ghost xbtn" data-action="close-sheet" aria-label="${esc(t('close'))}">✕</button></div>
${bio?`<p class="bio">${esc(bio)}</p>`:''}
<div class="facts">${facts.map(f=>`<span class="fact">${esc(f)}</span>`).join('')}</div>
${x.clicks.length?`<h4>${esc(t('clickH'))}</h4><div class="chips">${x.clicks.map(c=>`<span class="chip good">✓ ${esc(M(c))}</span>`).join('')}</div>`:''}
${x.talks.length?`<h4>${esc(t('talkH'))}</h4><div class="chips">${x.talks.map(c=>`<span class="chip warn">⚑ ${esc(M(c))}</span>`).join('')}</div>`:''}
<div class="together mt">${isLet(p)?esc(t('letTogether',line)):t('together',esc(line),esc(rest))}</div>
<h4>${esc(t('why',s))}</h4><div class="bars">${partKeys(x.score.parts).map(k=>bar(tx(PART_LABELS[k]),x.score.parts[k])).join('')}</div><p class="hint">${esc(t('demoScoreHint'))}</p>
${x.likedMe?`<p><span class="chip liked">${esc(t('likedYou'))}</span></p>`:''}
<p class="safety"><button type="button" class="linkbtn" data-action="report" data-id="${esc(p.id)}">${esc(t('report'))}</button> · <button type="button" class="linkbtn" data-action="block" data-id="${esc(p.id)}">${esc(t('block'))}</button></p>
<div class="pactions sheet-act">${cardActions(x)}</div>
</div></dialog>`;
}
function openSheet(){const d=document.getElementById('person-sheet');if(d&&!d.open){try{d.showModal();}catch(e){d.setAttribute('open','');}d.dataset.shown='1';const b=d.querySelector('.xbtn');if(b)b.focus();}}
/* The sheet was closed (✕, Escape or the backdrop): leave its address. Back if the app opened it, so Back doesn't reopen it */
function sheetClosed(){
  const d=document.getElementById('person-sheet');if(d)delete d.dataset.shown;
  if(!S.person)return;
  S.person=null;
  if(S.sheetFromApp){S.sheetFromApp=false;S.goingBack=true;history.back();}
  else{try{history.replaceState(null,'','#people-'+peopleSub());}catch(err){}render();}
}

function matrix(ids,pairs){
  const s={};pairs.forEach(p=>{s[p.a+'|'+p.b]=p.score;s[p.b+'|'+p.a]=p.score;});
  const cell=v=>`<td class="${v>=80?'c-hi':(v>=65?'c-mid':'c-lo')}">${v}%</td>`;
  return `<div class="scroll"><table class="matrix"><thead><tr><th></th>${ids.map(id=>`<th>${esc(dn(id))}</th>`).join('')}</tr></thead><tbody>${ids.map((a,i)=>`<tr><th>${esc(dn(a))}</th>${ids.map((b,j)=>i===j?'<td class="c-self">–</td>':cell(s[a+'|'+b])).join('')}</tr>`).join('')}</tbody></table></div>`;
}

function viewHome(h){
  const isPreview=h.preview,ids=[S.me.id].concat(h.members.map(m=>m.id).filter(id=>!isMe(id)));
  const mine=h.members.find(m=>isMe(m.id)),myStatus=mine?mine.status:'preview';
  const statusOf=id=>{const m=h.members.find(x=>x.id===id);return m?m.status:'';};
  const life=h.life||{free:0,canInvite:false,canWithdraw:[],pendingUntil:{},gone:[]};
  const until=id=>statusOf(id)==='pending'&&life.pendingUntil[id]?' · '+t('untilDate',day(life.pendingUntil[id])):'';
  const wd=id=>life.canWithdraw.indexOf(id)>=0?`<button type="button" class="linkbtn" data-action="withdraw" data-gid="${h.id}" data-id="${esc(id)}">${esc(t('withdraw'))}</button>`:'';
  const goneLine=!isPreview&&life.gone.length?`<p class="hint gone">${esc(life.gone.map(x=>t('gone_'+x.status,dn(x.id))).join(' · '))}</p>`:'';
  const chips=`<div class="card members">${ids.map(id=>`<span class="member">${avatar(id,32)}<span><b>${esc(dn(id))}</b><br><span class="hint">${esc(hhLabel(PEOPLE[id]))}${!isPreview&&!isMe(id)?' · '+esc(memberStatus(statusOf(id)))+esc(until(id)):''}</span>${wd(id)}</span></span>`).join('')}${goneLine}</div>`;
  const back=`<p class="crumb"><a class="linkbtn" href="#groups">← ${t('allGroups')}</a></p>`;
  const stateLine=isPreview?`<span class="pill wait">${esc(t('pillPreview'))}</span>`:`<span class="pill ${h.state==='formed'?'ok':(myStatus==='pending'?'act':'wait')}">${esc(myStatus==='pending'?t('pillInvite'):t('pill_'+h.state))}</span>`;
  const respond=!isPreview&&myStatus==='pending'?`<div class="card notice mt">${esc(t('invitedBy',dn(life.invitedBy||h.createdBy)))}${life.expiresAt?' · '+esc(t('answerBy',day(life.expiresAt))):''}<div class="row"><button type="button" class="btn small" data-action="respond" data-id="${h.id}" data-accept="0">${t('decline')}</button><button type="button" class="btn small primary" data-action="respond" data-id="${h.id}" data-accept="1">${t('accept')}</button></div></div>`:'';
  if(!h.check.ok)return `${back}<h2>${t('homeH')}</h2>${stateLine}
<p class="lead">${esc(t('attentionLead'))}</p>
${chips}${respond}
<div class="card warnbox mt"><b>${esc(t('doesntWork'))}</b> ${esc(h.check.issues.map(M).join(' · '))}</div>`;
  const gs=h.score,hb=h.brief;
  const musts=hb.feats.filter(x=>x.level==='must'),nices=hb.feats.filter(x=>x.level==='nice');
  const featLi=x=>`<li><span class="tag ${x.level}">${x.level==='must'?t('tagMust'):t('tagNice')}</span><div>${esc(featLabel(x.id))}<div class="who">${esc(x.level==='must'?t('mustFor',names(x.must),x.nice.length?names(x.nice):''):t('niceFor',names(x.nice)))}</div></div></li>`;
  const where=hb.areas.length?listOr(hb.areas.map(areaLabel)):t('noAreaAll');
  const line=t('homeLine',homeType(hb.types),hb.beds);
  const letting=h.purpose==='let';
  const headline=letting?t('letHeadline',line):t('headline',line,t('bathsN',hb.baths),hb.cars?t('parkingN',hb.cars):'');
  const banner=isPreview?`<div class="card notice mt"><b>${t('previewOnly')}</b> ${esc(t('previewNote'))}<div class="row"><button type="button" class="btn small primary" data-action="propose" data-id="${esc(ids.filter(id=>!isMe(id)).join(','))}">${t('propose')}</button><button type="button" class="btn small ghost" data-action="close-preview">${t('closePreview')}</button></div></div>`:'';
  const canSize=!isPreview&&myStatus==='accepted';
  const sizeEdit=canSize?`<div class="field mt"><span class="label" id="size-label">${esc(t('sizeLabel'))}</span><div class="range-edit" role="group" aria-labelledby="size-label"><label>${t('from')} <input type="number" min="20" max="900" step="5" data-size="min" value="${h.size.min}"></label><label>${t('to')} <input type="number" min="20" max="900" step="5" data-size="max" value="${h.size.max}"></label></div></div>`:'';
  const lead=isPreview?t('leadPreview'):(h.state==='formed'?t('leadGroup'):t('leadPending'));
  const w=gs.weakest;
  const leave=!isPreview&&myStatus!=='pending'?`<p class="mt"><button type="button" class="btn small ghost danger" data-action="leave" data-id="${h.id}">${t('leave')}</button></p>`:'';
  /* Three views (B-051): Overview (where things stand, and how you fit), Home brief (the home, the rooms, what to agree), Homes */
  const hr=S.homes&&S.homes.gid===h.id&&S.homes.data?S.homes.data:null;
  const sl=S.sl&&S.sl.gid===h.id&&S.sl.data?S.sl.data:null;
  const sub=GSUBS.indexOf(S.gsub)>=0&&!(isPreview&&(S.gsub==='homes'||S.gsub==='shortlist'||S.gsub==='chat'))?S.gsub:'overview';
  const subs=[['overview',t('gs_overview'),null,'',groupHash(h,'overview')],['brief',t('gs_brief'),null,'',groupHash(h,'brief')],
    ['homes',t('gs_homes'),hr?hr.homes.length:null,isPreview?t('gs_homesLater'):'',isPreview?'':groupHash(h,'homes')],
    ['shortlist',t('gs_shortlist'),sl?sl.items.length:null,isPreview?t('gs_homesLater'):'',isPreview?'':groupHash(h,'shortlist')],
    ['chat',t('gs_chat'),null,isPreview?t('gs_homesLater'):'',isPreview?'':groupHash(h,'chat')]];
  const fitCard=`<section class="card"><h3>${t('fitH')}</h3>
<div class="kpis">${kp(gs.min+'%',(ids.length>2?t('weakestK'):t('compatK'))+esc(dn(w.a))+' + '+esc(dn(w.b)))}${ids.length>2?kp(gs.avg+'%',esc(t('avgOf',gs.pairs.length))):''}</div>
${ids.length>2?matrix(ids,gs.pairs):''}
<details class="quietbox mt"><summary>${ids.length>2?t('weakDetail'):t('breakdown')}</summary>
<div class="bars">${partKeys(gs.weakestParts).map(k=>bar(tx(PART_LABELS[k]),gs.weakestParts[k])).join('')}</div>
<p class="hint mt">${esc(t('demoScores'))}</p></details>
</section>`;
  const briefView=`<div class="grid cols-2">
<section class="card"><h3>${letting?esc(t('letH')):t('homeNeedH')}</h3>
<p class="headline">${esc(cap(headline))}</p>
<div class="kpis">${letting?`${kp(hb.beds,t('kBeds'))}${kp(h.size.min+'–'+h.size.max+' m²',t('kArea'))}${kp(ids.length,t('kOwners'))}`:`${kp(hb.beds,t('kBeds'))}${kp(hb.baths,t('kBaths'))}${kp(hb.cars||t('kNone'),t('kParking'))}${kp(h.size.min+'–'+h.size.max+' m²',t('kArea'))}${kp(hb.adults+(hb.kids?' + '+hb.kids:''),hb.kids?t('kAdultsKids'):t('kAdults'))}`}</div>
${sizeEdit}
<p class="hint">${esc(t('areaHint'))}</p>
<p class="mt"><b>${t('whereB')}</b> ${esc(where)}</p>
${musts.length?`<h4>${t('mustsH')}</h4><ul class="clean">${musts.map(featLi).join('')}</ul>`:''}
${nices.length?`<h4>${t('nicesH')}</h4><ul class="clean">${nices.map(featLi).join('')}</ul>`:''}
${hb.extras.length?`<h4>${t('worthH')}</h4><ul class="clean">${hb.extras.map(x=>`<li><span class="tag tip">${t('tipTag')}</span><div>${esc(M(x.label))}<div class="who">${esc(M(x.why))}</div></div></li>`).join('')}</ul>`:''}
</section>
<div class="stackcol">
${letting?`<section class="card"><h3>${esc(t('letCardH'))}</h3><p>${esc(t('letCardLead'))}</p><p class="hint">${esc(t('letCardLaw'))}</p></section>`:`<section class="card"><h3>${t('roomsH')}</h3>
<div class="split" aria-hidden="true">${h.rooms.map(r=>`<span data-w="${r.pct}" data-bg="${esc((PEOPLE[r.id]||{}).color||'#495057')}"></span>`).join('')}</div>
<ul class="clean">${h.rooms.map(r=>`<li>${avatar(r.id,28)}<div><b>${esc(dn(r.id))}</b>: ${esc(t('roomsOf',r.rooms,hb.beds))}${r.bath?esc(t('plusOwnBath')):''}${h.cars[r.id]?`<div class="who">${esc(t('parkingSpaces',h.cars[r.id]))}</div>`:''}</div></li>`).join('')}</ul>
<h4>${t('placementH')}</h4>
<ul class="clean">${h.tips.map(x=>`<li><span class="tag tip">${t('tipTag')}</span><div>${esc(M(x))}</div></li>`).join('')}</ul>
<p class="hint mt">${esc(t('illustrative'))}</p>
</section>`}
<section class="card"><h3>${t('agreeH')}</h3>
<p class="hint">${esc(t('agreeHint'))}</p>
<ul class="clean mt">${h.topics.map(x=>{const a=t(x.key);return `<li><span class="tag tip">${esc(a[0])}</span><div>${esc(a[1])}<div class="who">${esc(x.why?M(x.why):a[2])}</div></div></li>`;}).join('')}</ul>
</section></div></div>`;
  /* Overview: one next step, who's in, how you fit, and links to the brief and the homes */
  const waiting=h.members.filter(m=>!isMe(m.id)&&m.status==='pending').map(m=>m.id);
  let next='';
  if(isPreview)next=banner;
  else if(myStatus==='pending')next='';
  const agreedH=sl&&sl.items.find(x=>x.stage===3&&!x.gone),myTurn=sl?sl.items.filter(x=>slMyTurn(sl,x)).length:0;
  if(!isPreview&&myStatus!=='pending'&&agreedH)next=`<div class="nextstep"><span>${esc(t('nextAgreed',euros(agreedH.home.price)))}</span><a class="btn small primary" href="${groupHash(h,'shortlist')}">${esc(t('nextAgreedBtn'))}</a></div>`;
  else if(!isPreview&&myStatus!=='pending'&&myTurn)next=`<div class="nextstep"><span>${esc(t('nextVote',myTurn))}</span><a class="btn small primary" href="${groupHash(h,'shortlist')}">${esc(t('nextVoteBtn'))}</a></div>`;
  else if(h.state==='formed')next=`<div class="nextstep"><span>${esc(hr&&hr.homes.length?t('nextHomes',hr.homes.length):t('nextFormed'))}</span>${hr&&hr.homes.length?`<a class="btn small primary" href="${groupHash(h,'homes')}">${esc(t('nextHomesBtn'))}</a>`:`<a class="btn small" href="${groupHash(h,'brief')}">${esc(t('nextBriefBtn'))}</a>`}</div>`;
  else if(waiting.length)next=`<div class="nextstep"><span>${esc(t('gWaitingFor',names(waiting)))}</span><a class="btn small" href="${groupHash(h,'brief')}">${esc(t('nextBriefBtn'))}</a></div>`;
  else if(h.state==='broken')next=`<div class="nextstep"><span>${esc(life.canInvite?t('nextBroken'):t('gBrokenLine'))}</span></div>`;
  const medP=hr?median(hr.homes.map(x=>x.price)):null;
  const otile=(k,v,label,sub2)=>`<a class="otile" href="${groupHash(h,k)}"><span class="tk">${esc(label)}</span><span class="tv">${v}</span><span class="ts">${esc(sub2)}</span><span class="go" aria-hidden="true">→</span></a>`;
  const overview=`${next}<div class="grid cols-2">${fitCard}<div class="stackcol">${chips}${isPreview?'':freePlaces(h,life,ids)}
${otile('brief',esc(cap(line)),letting?t('oBriefLet'):t('oBrief'),[letting?purposeLabel('let'):t('bathsN',hb.baths),h.size.min+'–'+h.size.max+' m²',where].join(' · '))}
${isPreview?'':otile('homes',hr?num(hr.homes.length):'…',t('oHomes'),hr?(hr.homes.length?t('oHomesSub',kEur(medP)):t('oHomesNone')):t('loading'))}
${isPreview?'':otile('shortlist',sl?num(sl.items.length):'…',t('oShortlist'),sl?(sl.items.length?t('oShortlistSub',sl.items.filter(x=>x.stage===3).length,myTurn):t('oShortlistNone')):t('loading'))}
</div></div>${leave}`;
  const body={overview:overview,brief:briefView,homes:isPreview?'':homesSection(h.id),shortlist:isPreview?'':shortlistSection(h),chat:isPreview?'':chatPanel({kind:'group',id:h.id})}[sub];
  return `${back}<header class="ghead"><div class="stack">${ids.map(id=>avatar(id,36)).join('')}</div><div class="gwho"><h2>${esc(ids.map(dn).join(' + '))}</h2><p class="hint">${stateLine} ${esc(lead)}</p></div></header>
${respond}
${subTabs(subs,sub,'data-gsub',t('gsAria'))}
<div class="gbody">${body}</div>
<p class="hint mt">${esc(t('leadScope'))}</p>`;
}
/* Free places (B-038): someone declined, left, didn't answer in time or was withdrawn. A member who has said yes
   can invite one of their own mutual matches; the server checks they fit the group. */
function freePlaces(h,life,ids){
  if(!life.free)return '';
  let body;
  if(!life.canInvite)body=`<p class="hint">${esc(t('freeAfterYes'))}</p>`;
  else if(!S.matches)body=`<p class="hint">${esc(t('loading'))}</p>`;
  else{
    const final=life.gone.filter(x=>x.status==='declined'||x.status==='left').map(x=>x.id);
    const can=S.matches.compatible.filter(x=>x.state==='mutual'&&ids.indexOf(x.person.id)<0&&final.indexOf(x.person.id)<0);
    body=can.length?`<ul class="clean pick">${can.slice(0,8).map(x=>`<li>${avatar(x.person.id,28,x.person.color,x.person.name)}<div><b>${esc(x.person.name)}</b>${x.person.isDemo?` <span class="demo">${esc(t('demoTag'))}</span>`:''} <span class="hint">· ${x.score.total}%</span></div><button type="button" class="btn small" data-action="invite-member" data-gid="${h.id}" data-id="${esc(x.person.id)}">${esc(t('invite'))}</button></li>`).join('')}</ul>`:`<p class="hint">${esc(t('freeNone'))}</p>`;
  }
  return `<section class="card freeplace"><h3>${esc(t('freeH',life.free))}</h3><p class="hint">${esc(t('freeLead'))}</p>${body}</section>`;
}

/* ---------- Session 19: notifications, messages, block and report (B-036, B-037) ---------- */
function day(iso){try{return new Date(iso).toLocaleDateString(locale(),{day:'numeric',month:'long'});}catch(e){return '';}}
function when(iso){
  const d=new Date(iso),s=(Date.now()-d.getTime())/1000;
  if(s<60)return t('justNow');if(s<3600)return t('minAgo',Math.floor(s/60));if(s<86400)return t('hAgo',Math.floor(s/3600));
  return d.toLocaleDateString(locale(),{day:'numeric',month:'short'});
}
function noteLink(n){
  if(n.kind==='like')return '#person-'+n.actor;
  if(n.kind==='match')return '#chat-'+n.actor;
  if(n.group&&n.groupOpen&&SL_NOTES.indexOf(n.kind)>=0)return '#group-'+n.group+'-shortlist';
  return n.group&&n.groupOpen?'#group-'+n.group:'';
}
function viewAlerts(){
  const head=`<header class="phead2"><div><h2>${esc(t('alertsH'))}</h2><p class="hint">${esc(t('alertsLead'))}</p></div></header>`;
  const ib=S.inbox;if(!ib)return head+`<p class="lead">${esc(t('loading'))}</p>`;
  if(!ib.items.length)return head+`<div class="card empty"><p class="lead">${esc(t('alertsNone'))}</p></div>`;
  return head+`<ul class="alerts">${ib.items.map(n=>{
    const who=n.actor?personName(n.actor):'',txt=t('n_'+n.kind,who),href=noteLink(n);
    const inner=`${n.actor?avatar(n.actor,36):`<span class="avatar bellav" aria-hidden="true">${ICONS.bell}</span>`}<span class="atext">${esc(txt)}<time datetime="${esc(n.at)}">${esc(when(n.at))}</time></span>`;
    return `<li class="${n.read?'':'unread'}">${href?`<a href="${href}">${inner}</a>`:`<div>${inner}</div>`}</li>`;}).join('')}</ul>`;
}
function convTitle(x){return x.kind==='person'?personName(x.id):names(x.members);}
function msgText(m){return m.script?t('scr_'+m.script):m.body;}
function viewMessages(){
  if(S.chatKey)return viewChat();
  const head=`<header class="phead2"><div><h2>${esc(t('chatsH'))}</h2><p class="hint">${esc(t('chatsLead'))}</p></div></header>`;
  const cs=S.chats;if(!cs)return head+`<p class="lead">${esc(t('loading'))}</p>`;
  if(!cs.items.length)return head+`<div class="card empty"><p class="lead">${esc(t('chatsNone'))}</p><a class="btn primary" href="#people-review">${t('goMatches')}</a></div>`;
  return head+`<ul class="convs">${cs.items.map(x=>{
    const href=x.kind==='person'?'#chat-'+x.id:'#group-'+x.id+'-chat',others=x.kind==='person'?[x.id]:x.members.filter(id=>!isMe(id));
    const last=x.last?(isMe(x.last.from)?t('youPrefix'):(x.kind==='group'?personName(x.last.from)+': ':''))+msgText(x.last):t('sayHello');
    return `<li${x.unread?' class="unread"':''}><a href="${href}"><span class="stack">${others.slice(0,3).map(id=>avatar(id,36)).join('')}</span><span class="ctext"><b>${esc(convTitle(x))}</b>${x.kind==='group'?` <span class="pill">${esc(t('pill_'+x.state))}</span>`:''}<span class="clast">${esc(last)}</span></span>${x.last?`<time datetime="${esc(x.last.at)}">${esc(when(x.last.at))}</time>`:''}${x.unread?`<span class="count" aria-label="${x.unread}">${x.unread}</span>`:''}</a></li>`;}).join('')}</ul>`;
}
function viewChat(){
  const k=S.chatKey,back=`<p class="crumb"><a class="linkbtn" href="#messages">← ${esc(t('allChats'))}</a></p>`;
  const ch=S.chat&&S.chat.kind===k.kind&&S.chat.id===k.id?S.chat:null;
  const head=ch&&!ch.error?`<header class="ghead">${avatar(k.id,40)}<div class="gwho"><h2>${esc(personName(k.id))}</h2></div><div class="gacts"><a class="btn small ghost" href="#person-${esc(k.id)}">${esc(t('seeProfile'))}</a><button type="button" class="btn small ghost" data-action="report" data-id="${esc(k.id)}">${esc(t('report'))}</button><button type="button" class="btn small ghost danger" data-action="block" data-id="${esc(k.id)}">${esc(t('block'))}</button></div></header>`:'';
  return back+head+chatPanel(k);
}
function chatPanel(k){
  const ch=S.chat&&S.chat.kind===k.kind&&S.chat.id===k.id?S.chat:null;
  if(!ch)return `<p class="lead">${esc(t('loading'))}</p>`;
  if(ch.error)return `<div class="card empty"><p class="lead">${esc(t('chatGone'))}</p><a class="btn" href="#messages">${esc(t('allChats'))}</a></div>`;
  return `<section class="chat card"><div class="msgs" id="chat-msgs">${chatMsgs(ch)}</div>
<form id="chat-form" class="composer"><label class="sr" for="chat-input">${esc(t('chatLabel'))}</label><textarea id="chat-input" rows="2" maxlength="1000" placeholder="${esc(t('chatPh'))}"></textarea><button type="submit" class="btn primary">${esc(t('send'))}</button></form>
<p class="hint">${esc(t('chatSafety'))} <a class="quiet" href="#help">${esc(t('conductLink'))}</a></p></section>`;
}
function chatMsgs(ch){
  if(!ch.messages.length)return `<p class="hint center">${esc(ch.kind==='person'?t('chatEmptyP',personName(ch.id)):t('chatEmptyG'))}</p>`;
  return ch.messages.map(m=>{const mine=isMe(m.from);return `<div class="msg${mine?' mine':''}">${mine?'':avatar(m.from,28)}<div class="bubble">${ch.kind==='group'&&!mine?`<b class="from">${esc(personName(m.from))}</b>`:''}<p>${esc(msgText(m))}</p><time datetime="${esc(m.at)}">${esc(when(m.at))}</time></div></div>`;}).join('');
}
function scrollChat(){const el=document.getElementById('chat-msgs');if(el)el.scrollTop=el.scrollHeight;}
/* Which conversation the current view shows, if any */
function wantedChat(){
  if(!inApp())return null;
  if(S.tab==='messages'&&S.chatKey)return S.chatKey;
  if(S.tab==='groups'&&S.home&&!S.home.preview&&S.gsub==='chat')return {kind:'group',id:S.home.id};
  return null;
}
async function loadChat(k,quiet){
  const key=k.kind+':'+k.id;if(!quiet){if(S.chatLoading===key)return;S.chatLoading=key;}
  let d;
  try{d=await api('GET','/api/chats/'+k.kind+'/'+encodeURIComponent(k.id));rememberLite(d.people);}
  catch(e){d={kind:k.kind,id:k.id,error:true,messages:[]};}
  if(!quiet)S.chatLoading=null;
  const w=wantedChat();if(!w||w.kind!==k.kind||w.id!==k.id)return;
  const before=S.chat&&S.chat.kind===k.kind&&S.chat.id===k.id?S.chat.messages.length:-1;
  S.chat=d;
  if(quiet&&before===d.messages.length)return;
  const el=document.getElementById('chat-msgs');
  if(el&&!d.error){el.innerHTML=chatMsgs(d);applyStyles(el);scrollChat();}else rerender();
  loadCounts();
}
async function sendChat(){
  const inp=document.getElementById('chat-input'),k=wantedChat();if(!inp||!k)return;
  const body=inp.value.trim();if(!body)return;
  if(!S.me.conduct){openConduct();return;}
  const btn=document.querySelector('#chat-form button[type=submit]');if(btn)btn.disabled=true;
  try{const d=await api('POST','/api/chats/'+k.kind+'/'+encodeURIComponent(k.id),{body:body});rememberLite(d.people);S.chat=d;inp.value='';
    const el=document.getElementById('chat-msgs');if(el){el.innerHTML=chatMsgs(d);applyStyles(el);scrollChat();}}
  catch(e){if(e.code==='conductNeeded'){S.me.conduct=false;openConduct();}else toast(errText(e));}
  finally{if(btn)btn.disabled=false;inp.focus();}
}
/* Dialogs live outside #app, so a re-render doesn't remove them */
function showDialog(id,html){
  const old=document.getElementById(id);if(old)old.remove();
  const d=document.createElement('dialog');d.className='sheet';d.id=id;d.innerHTML=`<div class="sheet-in">${html}</div>`;
  document.body.appendChild(d);applyStyles(d);
  d.addEventListener('close',()=>d.remove());
  try{d.showModal();}catch(e){d.setAttribute('open','');}
  const f=d.querySelector('button,input,textarea');if(f)f.focus();
  return d;
}
function closeDialog(el){const d=el.closest('dialog');if(d){if(d.open)d.close();else d.remove();}}
function openConduct(){
  showDialog('conduct-dlg',`<h2>${esc(t('conductH'))}</h2><p>${esc(t('conductLead'))}</p><ul>${t('conductItems').map(x=>`<li>${esc(x)}</li>`).join('')}</ul>
<div class="row end"><button type="button" class="btn ghost" data-action="dlg-close">${esc(t('cancel'))}</button><button type="button" class="btn primary" data-action="conduct-ok">${esc(t('conductOk'))}</button></div>`);
}
function openReport(id){
  const reasons=['fake','harassment','scam','unsafe','other'];
  showDialog('report-dlg',`<form id="report-form" data-id="${esc(id)}"><h2>${esc(t('reportH',personName(id)))}</h2><p class="hint">${esc(t('reportLead'))}</p>
<fieldset class="field"><legend class="label">${esc(t('reportReason'))}</legend>${reasons.map(r=>`<label class="check"><input type="radio" name="reason" value="${r}" required><span>${esc(t('rr_'+r))}</span></label>`).join('')}</fieldset>
<div class="field"><label for="report-note">${esc(t('reportNote'))}</label><textarea id="report-note" name="note" rows="3" maxlength="1000"></textarea></div>
<label class="check"><input type="checkbox" name="block" checked><span>${esc(t('reportBlock'))}</span></label>
<p class="hint">${esc(t('reportPoC'))}</p>
<div class="row end"><button type="button" class="btn ghost" data-action="dlg-close">${esc(t('cancel'))}</button><button type="submit" class="btn primary">${esc(t('reportSend'))}</button></div></form>`);
}
/* After blocking, the person is gone from every list: reload, and leave their conversation */
async function afterBlock(){
  S.chats=null;S.chat=null;S.blocks=null;S.person=null;
  if(S.tab==='messages'&&S.chatKey){S.chatKey=null;location.hash='#messages';}
  if(S.home&&!S.home.preview){S.home=null;if(S.tab==='groups')location.hash='#groups';}
  await refresh();
}
async function blockPerson(id){
  const n=personName(id);if(!window.confirm(t('blockConfirm',n)))return;
  try{await api('PUT','/api/blocks/'+encodeURIComponent(id));toast(t('tBlocked',n));await afterBlock();}catch(e){toast(errText(e));}
}
async function loadCounts(){
  if(!inApp())return;
  try{const c=await api('GET','/api/inbox/count');const changed=JSON.stringify(c)!==JSON.stringify(S.counts);S.counts=c;if(changed){renderTabs();renderChrome();}}catch(e){}
}
async function loadInbox(){
  if(S.inboxLoading)return;S.inboxLoading=true;
  try{const d=await api('GET','/api/inbox');rememberLite(d.people);S.inbox=d;
    if(d.items.some(n=>!n.read)){await api('POST','/api/inbox/read');S.counts.alerts=0;}}
  catch(e){S.inbox={items:[]};toast(errText(e));}
  finally{S.inboxLoading=false;}
  if(S.tab==='alerts')rerender();
}
async function loadChats(){
  if(S.chatsLoading)return;S.chatsLoading=true;
  try{const d=await api('GET','/api/chats');rememberLite(d.people);S.chats=d;}catch(e){S.chats={items:[]};toast(errText(e));}
  finally{S.chatsLoading=false;}
  if(S.tab==='messages')rerender();
}
async function loadBlocks(){
  if(S.blocksLoading)return;S.blocksLoading=true;
  try{S.blocks=await api('GET','/api/blocks');}catch(e){S.blocks=[];}
  finally{S.blocksLoading=false;}
  if(S.tab==='you')rerender();
}
/* Load what the current view needs and hasn't got yet */
function ensureData(){
  if(!inApp())return;
  if(S.tab==='home'&&!S.dashHomes)loadDashHomes();
  if(S.tab==='alerts'&&!S.inbox)loadInbox();
  if(S.tab==='messages'&&!S.chatKey&&!S.chats)loadChats();
  if(S.tab==='you'&&!S.blocks)loadBlocks();
  const w=wantedChat();if(w&&!(S.chat&&S.chat.kind===w.kind&&S.chat.id===w.id))loadChat(w);
  if(S.tab==='groups'&&S.home&&!S.home.preview&&!S.matches)loadMatches().then(rerender).catch(()=>{});
}
/* Every 10 s while the page is visible: the open conversation; every 30 s: the unread counts */
let pollTick=0;
setInterval(()=>{
  if(document.visibilityState!=='visible'||!inApp())return;
  pollTick++;
  const w=wantedChat();if(w&&S.chat&&!S.chat.error)loadChat(w,true);
  if(pollTick%3===0)loadCounts();
},10000);

/* ---------- numbers, stat tiles and charts (Session 16 design review) ----------
   Charts are plain SVG drawn at the container's real width, so text stays readable on phones.
   Colours come from CSS classes (the page's security policy blocks inline styles). */
function locale(){return LANG==='es'?'es-ES':'en-GB';}
function num(n,d){return n==null||isNaN(n)?'–':Number(n).toLocaleString(locale(),{maximumFractionDigits:d||0});}
function euros(n){return n==null?'':num(n)+' €';}
function kEur(n){if(n==null)return '–';return n>=1e6?t('mEur',num(n/1e6,2)):t('kEur',num(Math.round(n/1000)));}
function median(a){const v=a.filter(x=>x!=null&&!isNaN(x)).sort((x,y)=>x-y);if(!v.length)return null;const m=v.length>>1;return v.length%2?v[m]:(v[m-1]+v[m])/2;}
function niceStep(range,count){const raw=Math.max(range,1)/Math.max(1,count),p=Math.pow(10,Math.floor(Math.log10(raw))),f=raw/p;return (f<=1?1:f<=2?2:f<=2.5?2.5:f<=5?5:10)*p;}
function ticks(lo,hi,count){const s=niceStep(hi-lo,count),a=[];for(let v=Math.ceil(lo/s)*s;v<=hi+1e-9;v+=s)a.push(v);return a;}
function tile(v,k,sub){return `<div class="tile"><div class="tv">${v}</div><div class="tk">${esc(k)}</div>${sub?`<div class="ts">${sub}</div>`:''}</div>`;}
function colPath(x,y,w,h){const r=Math.max(0,Math.min(4,w/2,h));return `M${x},${y+h}V${y+r}Q${x},${y} ${x+r},${y}H${x+w-r}Q${x+w},${y} ${x+w},${y+r}V${y+h}Z`;}
function hash(s){let h=2166136261;for(const c of String(s)){h^=c.charCodeAt(0);h=Math.imul(h,16777619);}return h>>>0;}
/* Chart builders, keyed by the data-chart attribute of their container; filled after each render */
const CHARTS={};
function chartBox(key,build,label){CHARTS[key]=build;return `<div class="chart" data-chart="${key}" role="img" aria-label="${esc(label)}"></div>`;}
function drawCharts(root){(root||document).querySelectorAll('[data-chart]').forEach(el=>{const b=CHARTS[el.getAttribute('data-chart')];const w=Math.floor(el.clientWidth);if(b&&w>0)el.innerHTML=b(w);});}
let resizeTimer=0;window.addEventListener('resize',()=>{clearTimeout(resizeTimer);resizeTimer=setTimeout(()=>drawCharts(),150);});
function legend(items){return `<div class="legend">${items.map(i=>`<span><i class="sw ${i[0]}"></i>${esc(i[1])}</span>`).join('')}</div>`;}
/* A histogram of everything (context) with the filtered part on top (the accent). bins:[{lo,hi,all,sel,tip}] */
function histogram(w,bins,o){
  const H=168,L=30,R=6,T=10,B=24,n=bins.length,max=Math.max(1,...bins.map(b=>b.all));
  const yt=ticks(0,max,3).filter(v=>Number.isInteger(v)),ymax=Math.max(max,yt[yt.length-1]||1);
  const pw=w-L-R,ph=H-T-B,bw=pw/n,cw=Math.max(2,Math.min(24,bw-2)),y=v=>T+ph-ph*v/ymax;
  let s=yt.map(v=>`<line class="gl" x1="${L}" x2="${w-R}" y1="${y(v)}" y2="${y(v)}"/><text class="ax" x="${L-6}" y="${y(v)+4}" text-anchor="end">${num(v)}</text>`).join('');
  const stride=Math.max(1,Math.ceil(64/bw));
  bins.forEach((b,i)=>{
    const x=L+i*bw+(bw-cw)/2;
    if(b.all)s+=`<path class="m-ctx" d="${colPath(x,y(b.all),cw,ph-(y(b.all)-T))}"/>`;
    if(b.sel)s+=`<path class="m-acc" d="${colPath(x,y(b.sel),cw,ph-(y(b.sel)-T))}"/>`;
    if(i%stride===0){const lx=L+i*bw,lab=o.fmt(b.lo),half=3.2*lab.length;s+=`<text class="ax" x="${Math.max(half,Math.min(w-half,lx))}" y="${H-6}" text-anchor="middle">${esc(lab)}</text>`;}
    s+=`<rect class="hit" x="${L+i*bw}" y="${T}" width="${bw}" height="${ph}" data-tip="${esc(b.tip)}"${o.action?` data-action="${o.action}" data-lo="${b.lo}" data-hi="${b.hi}"`:''}/>`;
  });
  s+=`<line class="base" x1="${L}" x2="${w-R}" y1="${T+ph}" y2="${T+ph}"/>`;
  return `<svg width="${w}" height="${H}" viewBox="0 0 ${w} ${H}" aria-hidden="true">${s}</svg>`;
}

/* ---------- homes explorer: filters, live stats and charts over the homes that fit ---------- */
const EN_ORDER='ABCDEFG';
const HSORTS={
  fit:(a,b)=>(a.isDemo-b.isDemo)||(b.score-a.score)||((a.price||9e9)-(b.price||9e9)),
  priceUp:(a,b)=>(a.price||9e9)-(b.price||9e9),
  priceDown:(a,b)=>(b.price||0)-(a.price||0),
  pm2:(a,b)=>(a.pm2||9e9)-(b.pm2||9e9),
  size:(a,b)=>(b.m2||0)-(a.m2||0)
};
function hfFor(gid){if(!S.hf||S.hf.gid!==gid)S.hf={gid:gid,pmin:null,pmax:null,beds:0,m2:0,types:[],areas:[],energy:'',must:false,real:false,sort:'fit',show:12};return S.hf;}
function hfActive(f){return (f.pmin!=null)+(f.pmax!=null)+!!f.beds+!!f.m2+!!f.types.length+!!f.areas.length+!!f.energy+f.must+f.real;}
function hfApply(list,f){
  return list.filter(x=>(f.pmin==null||(x.price!=null&&x.price>=f.pmin))&&(f.pmax==null||(x.price!=null&&x.price<=f.pmax))
    &&(!f.beds||(x.beds||0)>=f.beds)&&(!f.m2||(x.m2||0)>=f.m2)
    &&(!f.types.length||f.types.indexOf(x.type)>=0)&&(!f.areas.length||f.areas.indexOf(x.area)>=0)
    &&(!f.energy||(x.energy&&EN_ORDER.indexOf(x.energy)>=0&&EN_ORDER.indexOf(x.energy)<=EN_ORDER.indexOf(f.energy)))
    &&(!f.must||!x.misses.some(m=>m.startsWith('f:')))&&(!f.real||!x.isDemo));
}
function homesAll(){const r=S.homes;if(!r||!r.data)return [];if(!r.list)r.list=r.data.homes.map(x=>Object.assign({},x,{pm2:x.price&&x.m2?x.price/x.m2:null}));return r.list;}
function priceBounds(list){const p=list.map(x=>x.price).filter(v=>v!=null);if(!p.length)return [0,0];return [Math.floor(Math.min(...p)/10000)*10000,Math.ceil(Math.max(...p)/10000)*10000];}
function pressBtn(k,v,on,label){return `<button type="button" class="chipbtn" data-action="hf" data-k="${k}" data-v="${esc(v)}" aria-pressed="${on}">${esc(label)}</button>`;}
function homesSection(gid){
  const r=S.homes&&S.homes.gid===gid?S.homes:null;
  let body;
  if(!r||r.loading)body=`<div class="skeleton"></div><div class="skeleton short"></div>`;
  else if(r.error)body=`<p class="error">${esc(r.error)}</p>`;
  else if(!r.data.homes.length)body=`<div class="card empty left"><p><b>${esc(t('homesNoneH'))}</b></p><ul class="tips">${t('homesNoneTips').map(x=>`<li>${esc(x)}</li>`).join('')}</ul>${S.home?`<a class="btn" href="${groupHash(S.home,'brief')}">${esc(t('homesNoneBtn'))}</a>`:''}</div>`;
  else body=homesExplorer(gid);
  const count=r&&r.data?`<p class="hint">${esc(t('homesCount',r.data.considered,r.data.real>0))}</p>`:'';
  const letNote=S.home&&S.home.purpose==='let'?`<p class="hint">${esc(t('letHomesNote'))}</p>`:'';
  return `<section class="explorer" id="homes">${count}${letNote}${r&&r.data?budgetLine(r.data):''}${body}<p class="hint mt">${esc(t('homesLead'))} ${esc(t('homesNote'))} <a class="quiet" href="#sources">${esc(t('srcLinkHomes'))}</a></p></section>`;
}
/* The group's combined maximum budget (B-058, D-044): the total, never anyone's own figure, and who hasn't set one */
function budgetLine(d){
  const unset=d.budgetUnset||[];
  if(unset.length)return `<p class="budgetline">${esc(t('hbUnset',names(unset)))}${unset.some(isMe)?` <a class="quiet" href="#profile" data-action="open-sec" data-id="budget">${esc(t('hbSetMine'))}</a>`:''}</p>`;
  if(d.budget==null)return '';
  return `<p class="budgetline"><b>${esc(t('hbBudget',euros(d.budget)))}</b>${d.overBudget?' '+esc(t('hbOver',d.overBudget)):''}</p>`;
}
function homesExplorer(gid){
  const f=hfFor(gid),all=homesAll(),[plo,phi]=priceBounds(all);
  const types=TYPES.filter(x=>all.some(h=>h.type===x.id)),areas=AREAS.filter(a=>all.some(h=>h.area===a.id));
  const bedsMin=Math.min(...all.map(h=>h.beds||0)),bedsMax=Math.max(...all.map(h=>h.beds||0));
  const m2Max=Math.ceil(Math.max(0,...all.map(h=>h.m2||0))/10)*10,real=all.some(h=>!h.isDemo),n=hfActive(f);
  const bedOpts=[];for(let b=bedsMin+1;b<=Math.min(bedsMax,bedsMin+3);b++)bedOpts.push(b);
  const pmin=f.pmin==null?plo:f.pmin,pmax=f.pmax==null?phi:f.pmax;
  const mode=S.hview==='market'?'market':'list';
  const bar=`<div class="hxbar"><div class="seg2" role="group" aria-label="${esc(t('hxView'))}"><button type="button" class="chipbtn" data-action="hview" data-v="list" aria-pressed="${mode==='list'}">${esc(t('hxList'))}</button><button type="button" class="chipbtn" data-action="hview" data-v="market" aria-pressed="${mode==='market'}">${esc(t('hxMarket'))}</button></div><span class="spacer"></span><label class="sr" for="hx-sort">${esc(t('hxSort'))}</label><select id="hx-sort" data-hf="sort" class="inline">${Object.keys(HSORTS).map(k=>`<option value="${k}"${f.sort===k?' selected':''}>${esc(t('sort_'+k))}</option>`).join('')}</select></div>`;
  const controls=`<details class="filters" id="hx-filters"${S.hfOpen?' open':''}><summary><b>${esc(t('hxFilters'))}</b>${n?` <span class="count">${n}</span>`:''}<span class="hint fsum">${esc(t('hxFiltersHint'))}</span></summary>
<div class="fgrid">
<div class="fcell wide"><span class="label">${esc(t('hxPrice'))} <output id="hx-price-out" class="hint">${esc(euros(pmin))} – ${esc(euros(pmax))}</output></span>
<div class="dual"><div class="dtrack"><i id="hx-dfill"></i></div><input type="range" data-hf="pmin" min="${plo}" max="${phi}" step="5000" value="${pmin}" aria-label="${esc(t('hxPrice')+': '+t('hxFrom'))}"><input type="range" data-hf="pmax" min="${plo}" max="${phi}" step="5000" value="${pmax}" aria-label="${esc(t('hxPrice')+': '+t('hxTo'))}"></div></div>
<div class="fcell"><span class="label">${esc(t('hxSize'))} <output id="hx-m2-out" class="hint">${f.m2?f.m2+' m²':esc(t('hxAny'))}</output></span><input type="range" data-hf="m2" min="0" max="${m2Max}" step="5" value="${f.m2}" aria-label="${esc(t('hxSize'))}"></div>
<div class="fcell"><span class="label">${esc(t('hxBeds'))}</span><div class="seg">${pressBtn('beds',0,!f.beds,t('hxAny'))}${bedOpts.map(b=>pressBtn('beds',b,f.beds===b,b+'+')).join('')}</div></div>
${types.length>1?`<div class="fcell"><span class="label">${esc(t('hxType'))}</span><div class="seg">${types.map(x=>pressBtn('types',x.id,f.types.indexOf(x.id)>=0,tx(x.label))).join('')}</div></div>`:''}
${areas.length>1?`<div class="fcell"><span class="label">${esc(t('hxArea'))}</span><div class="seg">${areas.map(a=>pressBtn('areas',a.id,f.areas.indexOf(a.id)>=0,tx(a.label))).join('')}</div></div>`:''}
<div class="fcell"><span class="label">${esc(t('hxEnergy'))}</span><div class="seg">${pressBtn('energy','',!f.energy,t('hxAny'))}${['C','E'].map(e=>pressBtn('energy',e,f.energy===e,'A–'+e)).join('')}</div></div>
<div class="fcell"><span class="label">${esc(t('hxMore'))}</span><div class="seg">${pressBtn('must',1,f.must,t('hxMust'))}${real?pressBtn('real',1,f.real,t('hxReal')):''}</div></div>
</div>
<div class="frow"><span class="hint" id="hx-count" aria-live="polite"></span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="hf-reset"${n?'':' disabled'}>${esc(t('hxReset'))}</button></div>
</details>`;
  return `${bar}${controls}<div id="hx-results"></div>`;
}
function homesResults(){
  const f=S.hf,all=homesAll(),sel=hfApply(all,f).sort(HSORTS[f.sort]||HSORTS.fit);
  const medP=median(sel.map(x=>x.price)),medPm2=median(sel.map(x=>x.pm2)),medM2=median(sel.map(x=>x.m2)),allPm2=median(all.map(x=>x.pm2));
  const best=sel.length?Math.max(...sel.map(x=>x.score)):null;
  const market=S.hview==='market';
  const sumline=`<p class="sumline" aria-live="polite"><b>${esc(t('hxSumN',sel.length,all.length))}</b>${sel.length?` · ${esc(t('hxSumMed',kEur(medP)))}${medPm2?' · '+esc(num(Math.round(medPm2))+' €/m²'):''}`:''}</p>`;
  const tiles=!market?sumline:`<div class="tiles">${tile(num(sel.length),t('stHomes'),esc(t('stOfTotal',all.length)))}${tile(kEur(medP),t('stMedPrice'),'')}${tile(medPm2?num(Math.round(medPm2))+' €/m²':'–',t('stMedPm2'),medPm2&&allPm2&&f!==null&&hfActive(f)?esc(t('vsAll',Math.round((medPm2/allPm2-1)*100))):'')}${tile(medM2?num(medM2)+' m²':'–',t('stMedSize'),'')}${tile(best!=null?best+' %':'–',t('stBestFit'),'')}</div>`;
  if(!sel.length)return `${tiles}<div class="card empty mt"><p>${esc(t('hxNone'))}</p><button type="button" class="btn" data-action="hf-reset">${esc(t('hxReset'))}</button></div>`;
  /* price histogram: all homes that fit (context) and the filtered ones (accent) */
  const [plo,phi]=priceBounds(all),step=niceStep(phi-plo,10),start=Math.floor(plo/step)*step,bins=[];
  for(let lo=start;lo<phi||bins.length===0;lo+=step){const hi=lo+step,inb=x=>x.price!=null&&x.price>=lo&&(x.price<hi||(hi>=phi&&x.price<=hi));const a=all.filter(inb).length,s=sel.filter(inb).length;bins.push({lo:lo,hi:hi,all:a,sel:s,tip:t('chBinTip',kEur(lo),kEur(hi),s,a)});}
  /* price against size, with the median price per m² as a reference line */
  const pts=all.filter(x=>x.price&&x.m2),selIds={};sel.forEach(x=>{selIds[x.id]=1;});
  /* median price per m² by area */
  const byArea=AREAS.map(a=>{const v=sel.filter(x=>x.area===a.id);return {a:a,n:v.length,m:median(v.map(x=>x.pm2))};}).filter(x=>x.n);
  const aMax=Math.max(1,...byArea.map(x=>x.m||0));
  const areaBars=byArea.length?`<div class="hbars">${byArea.map(x=>`<div class="hbar" data-tip="${esc(t('chAreaTip',tx(x.a.label),num(Math.round(x.m)),x.n))}"><span class="hl">${esc(tx(x.a.label))}</span><span class="track"><i data-w="${Math.round(100*(x.m||0)/aMax)}"></i></span><b>${num(Math.round(x.m))} €/m²</b></div>`).join('')}</div>`:'';
  const charts=`<div class="charts">
<figure class="chartcard"><figcaption><b>${esc(t('chPriceH'))}</b>${legend([['ctx',t('chAll')],['acc',t('chSel')]])}</figcaption>${chartBox('hx-price',w=>histogram(w,bins,{fmt:kEur,action:'bin'}),t('chPriceH'))}<p class="hint">${esc(t('chClickBin'))}</p></figure>
<figure class="chartcard"><figcaption><b>${esc(t('chScatterH'))}</b>${legend([['ctx',t('chAll')],['acc',t('chSel')],['ref',t('chRef',num(Math.round(medPm2||allPm2||0)))]])}</figcaption>${chartBox('hx-scatter',w=>scatter(w,pts,selIds,medPm2||allPm2),t('chScatterH'))}<p class="hint">${esc(t('chScatterNote'))}</p></figure>
${areaBars?`<figure class="chartcard"><figcaption><b>${esc(t('chAreaH'))}</b></figcaption>${areaBars}</figure>`:''}
</div>`;
  if(market)return `${tiles}${charts}<p class="center"><button type="button" class="btn" data-action="hview" data-v="list">${esc(t('hxSeeList',sel.length))}</button></p>`;
  const shown=sel.slice(0,f.show),more=sel.length-shown.length;
  return `${tiles}<ul class="homes">${shown.map(x=>homeItem(x,medPm2,true)).join('')}</ul>${more>0?`<p class="center mt"><button type="button" class="btn" data-action="more-homes">${esc(t('moreHomes',Math.min(12,more),more))}</button></p>`:''}`;
}
function scatter(w,pts,selIds,refPm2){
  if(!pts.length)return '';
  const xs=pts.map(p=>p.m2),ys=pts.map(p=>p.price);
  /* axes run from one round tick below the data to one above */
  const span=(lo,hi,n)=>{const st=niceStep(hi-lo,n),a=Math.floor(lo/st)*st,b=Math.ceil(hi/st)*st,v=[];for(let x=a;x<=b+1e-9;x+=st)v.push(x);return v;};
  const xt=span(Math.min(...xs),Math.max(...xs),w<480?3:5),yt=span(Math.min(...ys),Math.max(...ys),4);
  const H=w<480?220:250,L=12+6.4*Math.max(...yt.map(v=>kEur(v).length)),R=14,T=12,B=30;
  const x0=xt[0],x1=xt[xt.length-1],y0=yt[0],y1=yt[yt.length-1];
  const X=v=>L+(w-L-R)*(v-x0)/(x1-x0||1),Y=v=>T+(H-T-B)*(1-(v-y0)/(y1-y0||1));
  let s=yt.map(v=>`<line class="gl" x1="${L}" x2="${w-R}" y1="${Y(v)}" y2="${Y(v)}"/><text class="ax" x="${L-6}" y="${Y(v)+4}" text-anchor="end">${esc(kEur(v))}</text>`).join('');
  s+=xt.map(v=>{const lab=num(v)+' m²',half=3.2*lab.length;return `<text class="ax" x="${Math.max(L,Math.min(w-half,X(v)))}" y="${H-10}" text-anchor="middle">${lab}</text>`;}).join('');
  s+=`<line class="base" x1="${L}" x2="${w-R}" y1="${H-B}" y2="${H-B}"/>`;
  if(refPm2){
    /* clip the reference line to the plot */
    const a=Math.max(x0,y0/refPm2),b=Math.min(x1,y1/refPm2);
    if(b>a)s+=`<line class="ref" x1="${X(a)}" y1="${Y(a*refPm2)}" x2="${X(b)}" y2="${Y(b*refPm2)}"/>`;
  }
  const dot=(p,on)=>`<circle class="${on?'d-acc':'d-ctx'}" cx="${X(p.m2).toFixed(1)}" cy="${Y(p.price).toFixed(1)}" r="${on?5:4}"/><circle class="hit" cx="${X(p.m2).toFixed(1)}" cy="${Y(p.price).toFixed(1)}" r="10" data-tip="${esc(t('chDotTip',euros(p.price),p.m2,p.beds,p.district||p.municipality||''))}"${on?` data-action="dot" data-id="${esc(p.id)}"`:''}/>`;
  s+=pts.filter(p=>!selIds[p.id]).map(p=>dot(p,false)).join('')+pts.filter(p=>selIds[p.id]).map(p=>dot(p,true)).join('');
  return `<svg width="${w}" height="${H}" viewBox="0 0 ${w} ${H}" aria-hidden="true">${s}</svg>`;
}
/* An illustrative floor-plan sketch drawn from the advert's facts (no photos: D-035). Not the real plan. */
function planSvg(x){
  const W=240,H=110,p=5,r=hash(x.id),beds=Math.max(1,Math.min(6,x.beds||1)),baths=Math.max(1,Math.min(3,x.baths||1)),fe=x.features||[];
  const out=(x.type==='house'&&(fe.indexOf('garden')>=0||fe.indexOf('pool')>=0))||fe.indexOf('terrace')>=0?50:0;
  const iw=W-2*p-out,ih=H-2*p,top=Math.round(ih*(0.46+(r%9)/100)),bw=iw/beds,baw=Math.min(iw*0.2,36);
  let s=`<rect class="pl-wall" x="${p}" y="${p}" width="${iw}" height="${ih}" rx="4"/>`;
  for(let i=0;i<beds;i++)s+=`<rect class="pl-bed" x="${(p+i*bw+2).toFixed(1)}" y="${p+2}" width="${(bw-4).toFixed(1)}" height="${top-4}" rx="2.5"/>`;
  for(let j=0;j<baths;j++)s+=`<rect class="pl-bath" x="${(p+j*baw+2).toFixed(1)}" y="${p+top+2}" width="${(baw-4).toFixed(1)}" height="${ih-top-4}" rx="2.5"/>`;
  const lx=p+baths*baw+2;s+=`<rect class="pl-live" x="${lx.toFixed(1)}" y="${p+top+2}" width="${(p+iw-lx-2).toFixed(1)}" height="${ih-top-4}" rx="2.5"/>`;
  if(out){
    const ox=W-p-out+4;
    if(fe.indexOf('garden')>=0)s+=`<rect class="pl-garden" x="${ox}" y="${p}" width="${out-4}" height="${ih}" rx="4"/>`;
    else s+=`<rect class="pl-terrace" x="${ox}" y="${p}" width="${out-4}" height="${ih}" rx="4"/>`;
    if(fe.indexOf('pool')>=0)s+=`<rect class="pl-pool" x="${ox+8}" y="${p+ih*0.55}" width="${out-20}" height="${ih*0.32}" rx="4"/>`;
  }
  return `<svg class="plan" viewBox="0 0 ${W} ${H}" role="img" aria-label="${esc(t('planAria',beds,baths))}">${s}</svg>`;
}
function homeItem(x,medPm2,onGroup){
  const where=[x.district,x.municipality].filter(Boolean).filter((v,i,a)=>a.indexOf(v)===i).join(', ');
  const facts=[typeLabel(x.type),x.beds!=null?t('homeBeds',x.beds):'',x.baths!=null?t('homeBaths',x.baths):'',x.m2?x.m2+' m²':''].filter(Boolean).join(' · ');
  /* misses first (they matter most), then confirmed must-haves, then the general fits; three at most */
  const tagList=x.misses.map(f=>`<span class="tag must">${esc(f.startsWith('f:')?t('missF',featLabel(f.slice(2))):t('miss_'+f))}</span>`)
    .concat(x.fits.filter(f=>f.startsWith('f:')).map(f=>`<span class="tag nice">${esc(featLabel(f.slice(2)))}</span>`))
    .concat(x.fits.filter(f=>!f.startsWith('f:')).map(f=>`<span class="tag nice">${esc(t('fit_'+f))}</span>`));
  const allTags=x.misses.map(f=>f.startsWith('f:')?t('missF',featLabel(f.slice(2))):t('miss_'+f)).concat(x.fits.map(f=>f.startsWith('f:')?featLabel(f.slice(2)):t('fit_'+f)));
  const tags=tagList.slice(0,3).join('')+(tagList.length>3?`<span class="tag tip" title="${esc(allTags.slice(3).join(', '))}">${esc(t('plusN',tagList.length-3))}</span>`:'');
  const check=x.check.length?`<div class="who">${esc(t('checkH'))}: ${esc(listAnd(x.check.map(c=>c.startsWith('f:')?featLabel(c.slice(2)).toLowerCase():t('check_'+c))))}</div>`:'';
  const link=x.url&&!x.isDemo?`<a class="btn small primary" href="${esc(x.url)}" target="_blank" rel="noopener noreferrer external">${esc(t('seeAdvert',x.agency))} ↗</a>`:`<span class="hint">${esc(t('demoHome'))}</span>`;
  const d=x.pm2&&medPm2?Math.round((x.pm2/medPm2-1)*100):null;
  const delta=d==null?'':`<span class="delta ${d<=-5?'lo':(d>=5?'hi':'')}">${esc(t('vsMedian',d))}</span>`;
  const cls=x.score>=85?'hi':(x.score>=70?'mid':'lo');
  const energy=x.energy&&EN_ORDER.indexOf(x.energy)>=0?`<span class="energy e-${x.energy}" title="${esc(t('energy',x.energy))}" aria-label="${esc(t('energy',x.energy))}">${esc(x.energy)}</span>`:'';
  return `<li class="home" id="home-${esc(x.id)}"><div class="home-vis">${planSvg(x)}${energy}${x.isDemo?`<span class="demo">${esc(t('demoHomeTag'))}</span>`:''}</div>
<div class="home-top"><b class="price">${esc(euros(x.price))}</b><div class="ring sm ${cls}" data-p="${x.score}" role="img" aria-label="${esc(t('homeFit',x.score))}"><b>${x.score}</b></div></div>
<div class="pm2">${x.pm2?esc(num(Math.round(x.pm2))+' €/m²'):''} ${delta}</div>
<div>${esc(facts)}</div><div class="who">${esc(where)} · ${esc(x.isDemo?t('demoAgency'):x.agency)}</div><div class="home-tags">${tags}</div>${check}<div class="row">${link}${onGroup?slButton(x.id):''}</div></li>`;
}
/* ---------- house voting (B-056): a shortlist, three yes rounds, then the "agreed" pack ----------
   Votes on a stage stay hidden until everyone still in has voted; a price change or a newcomer means voting again.
   The pack is an estimate and a list of questions, never advice (AGENTS.md section 4). Emails only for made-up
   homes, from the person's own email app: contacting real agencies needs Sam (F-024). */
const SL_NOTES=['shortlisted','agreeView','agreeOffer','agreed'];
/* Assumptions for the cost estimate, shown with it (product/2026-11-23-house-voting.md):
   deposit 20% (banks usually lend up to 80%), the region's general ITP scale for a resale home at the whole home's price
   (itpTax and REGIONS in i18n.js, B-059, B-060), other costs about 1.5%. A home we can't place uses the highest tax any
   region charges at that price, so the estimate isn't too low. */
const COSTS={deposit:0.20,other:0.015};
function itpFor(home){const r=home&&areaRegion(home.area),v=itpTax(home&&home.price,r);return {rate:v.rate,region:r?tx(r.label):null};}
const COPY={};
async function loadShortlist(gid){
  if(!S.sl||S.sl.gid!==gid)S.sl={gid:gid,loading:true};
  try{const d=await api('GET','/api/groups/'+gid+'/shortlist');if(S.sl&&S.sl.gid===gid)S.sl={gid:gid,data:d};}
  catch(e){S.sl={gid:gid,error:errText(e)};}
  if(S.home&&S.home.id===gid)rerender();
}
function slData(){return S.home&&S.sl&&S.sl.gid===S.home.id?S.sl.data:null;}
function slMine(x){const v=x.votes.find(v=>isMe(v.id));return v&&v.voted?v:null;}
function slMyTurn(sl,x){return sl.canVote&&!x.gone&&x.stage<3&&!slMine(x);}
function slButton(homeId){
  const sl=slData(),h=S.home;if(!sl||!h||h.preview||!sl.canVote)return '';
  const on=sl.items.some(x=>x.home&&x.home.id===homeId);
  return on?`<a class="btn small ghost" href="${groupHash(h,'shortlist')}">✓ ${esc(t('slOn'))}</a>`
    :`<button type="button" class="btn small" data-action="sl-add" data-id="${esc(homeId)}"${sl.items.length>=sl.max?' disabled':''}>+ ${esc(t('slAdd'))}</button>`;
}
function shortlistSection(h){
  const r=S.sl&&S.sl.gid===h.id?S.sl:null;
  const how=`<details class="quietbox"><summary>${esc(t('slHowH'))}</summary><ul class="tips">${t('slHow').map(x=>`<li>${esc(x)}</li>`).join('')}</ul></details>`;
  let body;
  if(!r||r.loading)body=`<div class="skeleton"></div>`;
  else if(r.error)body=`<p class="error">${esc(r.error)}</p>`;
  else if(!r.data.items.length)body=`<div class="card empty left"><p><b>${esc(t('slNoneH'))}</b></p><p>${esc(t('slNone'))}</p><a class="btn primary" href="${groupHash(h,'homes')}">${esc(t('slGoHomes'))}</a></div>`;
  else body=`${r.data.canVote?'':`<p class="hint">${esc(t('slCantVote'))}</p>`}<ul class="cands">${r.data.items.map(x=>candItem(h,r.data,x)).join('')}</ul>`;
  return `<section id="shortlist"><p class="lead">${esc(t('slLead'))}</p>${how}${body}</section>`;
}
function stageBar(x){
  return `<ol class="stages" aria-label="${esc(t('slStagesAria'))}">${['view','offer','buy'].map((k,i)=>{
    const st=x.stage>i?'done':(x.stage===i?'now':'later');
    return `<li class="${st}"${st==='now'?' aria-current="step"':''}><span class="dot" aria-hidden="true">${st==='done'?'✓':i+1}</span>${esc(t('st_'+k))}</li>`;}).join('')}</ol>`;
}
function voteChip(v){
  if(!v.voted)return `<span class="vchip wait">${esc(t('vWaiting'))}</span>`;
  if(!v.choice)return `<span class="vchip in">${esc(t('vVoted'))}</span>`;
  return `<span class="vchip ${v.choice}">${esc(t('v_'+v.choice))}${v.reason?' · '+esc(t('r_'+v.reason)):''}</span>`;
}
function candItem(h,sl,x){
  const o=x.home;
  if(!o)return `<li class="card cand"><p>${esc(t('slMissing'))}</p>${x.canRemove?`<button type="button" class="linkbtn" data-action="sl-remove" data-id="${x.id}">${esc(t('slRemove'))}</button>`:''}</li>`;
  const where=[o.district,o.municipality].filter(Boolean).filter((v,i,a)=>a.indexOf(v)===i).join(', ');
  const facts=[typeLabel(o.type),o.beds!=null?t('homeBeds',o.beds):'',o.baths!=null?t('homeBaths',o.baths):'',o.m2?o.m2+' m²':''].filter(Boolean).join(' · ');
  const cls=o.score>=85?'hi':(o.score>=70?'mid':'lo');
  const link=o.url&&!o.isDemo?`<a class="linkbtn" href="${esc(o.url)}" target="_blank" rel="noopener noreferrer external">${esc(t('seeAdvert',o.agency))} ↗</a>`:`<span>${esc(t('demoHome'))}</span>`;
  const notes=(x.gone?`<p class="card warnbox">${esc(t('slGone'))}</p>`:'')+(x.repriced?`<p class="card notice">${esc(t('slRepriced',euros(x.repriced.from),euros(x.repriced.to)))}</p>`:'');
  const mine=slMine(x),pick=S.slPick&&S.slPick.cid===x.id?S.slPick.choice:null;
  let act='';
  if(x.stage<3&&!x.gone&&sl.canVote){
    const btn=c=>`<button type="button" class="chipbtn v-${c}" data-action="sl-vote" data-id="${x.id}" data-v="${c}" aria-pressed="${(pick||(mine&&mine.choice))===c}">${esc(t('v_'+c))}</button>`;
    const reasons=pick?`<div class="reasons" role="group" aria-label="${esc(t('slWhy'))}"><span class="hint">${esc(t('slWhy'))}</span>${sl.reasons.map(r=>`<button type="button" class="chipbtn" data-action="sl-reason" data-id="${x.id}" data-c="${pick}" data-v="${r}">${esc(t('r_'+r))}</button>`).join('')}<button type="button" class="linkbtn" data-action="sl-reason" data-id="${x.id}" data-c="${pick}" data-v="">${esc(t('slSkipWhy'))}</button></div>`:'';
    act=`<div class="vote"><b>${esc(t('q_'+x.stageName))}</b><div class="seg">${['yes','maybe','no'].map(btn).join('')}</div>${reasons}</div>`;
  }
  const people=`<ul class="voters">${x.votes.map(v=>`<li>${avatar(v.id,26)}<span>${esc(dn(v.id))}</span>${voteChip(v)}</li>`).join('')}</ul>${x.stage<3&&!x.revealed?`<p class="hint">${esc(t('slHidden'))}</p>`:''}`;
  const extra=x.stage===3?agreedPack(h,sl,x):(x.stage>=1&&!x.gone?emailCard(sl,x,'view'):'');
  const meta=`${esc(t('slAddedBy',dn(x.addedBy)))}${x.canRemove?` · <button type="button" class="linkbtn" data-action="sl-remove" data-id="${x.id}">${esc(t('slRemove'))}</button>`:''}`;
  return `<li class="card cand${x.stage===3?' agreed':''}" id="cand-${x.id}">
<div class="cand-head"><div><b class="price">${esc(euros(o.price))}</b> ${x.stage===3?`<span class="pill ok">${esc(t('slAgreedPill'))}</span>`:''}<div>${esc(facts)}</div><div class="who">${esc(where)} · ${esc(o.isDemo?t('demoAgency'):o.agency)}</div></div><div class="ring sm ${cls}" data-p="${o.score}" role="img" aria-label="${esc(t('homeFit',o.score))}"><b>${o.score}</b></div></div>
${stageBar(x)}${notes}${people}${act}${extra}
<p class="hint mt">${link} · ${meta}</p></li>`;
}
/* Shares for the estimate: equal, or by the rooms each household would have (the brief's room plan) */
function slShares(h,ids){
  if(S.slSplit==='rooms'&&h.rooms&&h.rooms.length){const tot=h.rooms.filter(r=>ids.indexOf(r.id)>=0).reduce((a,r)=>a+r.pct,0)||1;
    return ids.map(id=>{const r=h.rooms.find(z=>z.id===id);return {id:id,share:r?r.pct/tot:0};});}
  return ids.map(id=>({id:id,share:1/ids.length}));
}
function agreedPack(h,sl,x){
  const o=x.home,P=o.price,ids=sl.voters;
  let cost='';
  if(P){
    const tax=itpFor(o),sh=slShares(h,ids),up=COSTS.deposit+tax.rate/100+COSTS.other,loan=1-COSTS.deposit;
    const row=s=>`<tr><th scope="row">${esc(dn(s.id))}</th><td>${num(Math.round(s.share*100))} %</td><td class="c-price">${esc(euros(Math.round(P*s.share)))}</td><td>${esc(euros(Math.round(P*up*s.share)))}</td><td>${esc(euros(Math.round(P*loan*s.share)))}</td></tr>`;
    cost=`<section class="pack"><h4>${esc(t('pkCostH'))}</h4>
${h.rooms&&h.rooms.length?`<div class="seg2" role="group" aria-label="${esc(t('pkSplit'))}"><button type="button" class="chipbtn" data-action="sl-split" data-v="equal" aria-pressed="${S.slSplit!=='rooms'}">${esc(t('pkEqual'))}</button><button type="button" class="chipbtn" data-action="sl-split" data-v="rooms" aria-pressed="${S.slSplit==='rooms'}">${esc(t('pkRooms'))}</button></div>`:''}
<div class="split" aria-hidden="true">${sh.map(s=>`<span data-w="${(s.share*100).toFixed(1)}" data-bg="${esc((PEOPLE[s.id]||{}).color||'#495057')}"></span>`).join('')}</div>
<div class="tablewrap"><table class="costs"><thead><tr><th scope="col">${esc(t('pkWho'))}</th><th scope="col">${esc(t('pkShare'))}</th><th scope="col" class="c-price">${esc(t('pkPrice'))}</th><th scope="col">${esc(t('pkUpfront'))}</th><th scope="col">${esc(t('pkLoan'))}</th></tr></thead>
<tbody>${sh.map(row).join('')}</tbody><tfoot><tr><th scope="row">${esc(t('pkTotal'))}</th><td>100 %</td><td class="c-price">${esc(euros(P))}</td><td>${esc(euros(Math.round(P*up)))}</td><td>${esc(euros(Math.round(P*loan)))}</td></tr></tfoot></table></div>
<p class="hint">${esc(t('pkCostNote',num(tax.rate,2),tax.region))}</p></section>`;
  }
  const qs=lawyerQuestions(h,ids,P?slShares(h,ids):null);
  const key='q'+x.id;COPY[key]=t('pkQH')+'\n\n'+qs.map((q,i)=>(i+1)+'. '+q).join('\n');
  const questions=`<section class="pack"><h4>${esc(t('pkQH'))}</h4><p class="hint">${esc(t('pkQLead'))}</p><ol class="qs">${qs.map(q=>`<li>${esc(q)}</li>`).join('')}</ol>
<div class="row"><button type="button" class="btn small" data-action="sl-copy" data-id="${key}">${esc(t('pkCopy'))}</button><a class="btn small ghost" href="${groupHash(h,'brief')}">${esc(t('pkBrief'))}</a></div></section>`;
  return `<div class="packs"><p class="nextstep"><span>${esc(t('pkLead'))}</span></p>${cost}${questions}${emailCard(sl,x,'offer')}</div>`;
}
function lawyerQuestions(h,ids,shares){
  const split=shares?listAnd(shares.map(s=>dn(s.id)+' '+num(Math.round(s.share*100))+' %')):'';
  const rooms=h.rooms&&h.rooms.length?listAnd(h.rooms.filter(r=>ids.indexOf(r.id)>=0).map(r=>dn(r.id)+' '+r.rooms)):'';
  if(h.purpose==='let')return [t('lq_shares',ids.length,split),t('lq_money'),t('lq_mortgage'),t('lq_costs'),t('lq_letKind'),t('lq_letRent'),t('lq_letManage'),t('lq_letTax'),t('lq_decide'),t('lq_leave'),t('lq_life'),t('lq_end')];
  return [t('lq_shares',ids.length,split),t('lq_money'),t('lq_mortgage'),t('lq_costs'),t('lq_rooms',rooms),t('lq_decide'),t('lq_leave'),t('lq_life'),t('lq_end')];
}
function emailCard(sl,x,kind){
  const o=x.home,title=t(kind==='view'?'emViewH':'emOfferH');
  if(!o.isDemo)return `<section class="pack"><h4>${esc(title)}</h4><p class="hint">${esc(t('emRealOff'))}</p></section>`;
  const facts=[typeLabel(o.type).toLowerCase(),o.beds!=null?t('homeBeds',o.beds):'',o.m2?o.m2+' m²':''].filter(Boolean).join(', ');
  const where=[o.district,o.municipality].filter(Boolean).filter((v,i,a)=>a.indexOf(v)===i).join(', ');
  const subject=t(kind==='view'?'emViewSubj':'emOfferSubj',facts,where);
  const body=t(kind==='view'?'emViewBody':'emOfferBody',sl.voters.length,facts,where,euros(o.price),o.id,listAnd(sl.voters.map(personName)));
  const key='e'+kind+x.id;COPY[key]=subject+'\n\n'+body;
  return `<section class="pack"><h4>${esc(title)}</h4><p class="hint">${esc(t('emLead'))}</p>
<pre class="draft">${esc(subject)}

${esc(body)}</pre>
<div class="row"><a class="btn small primary" href="${esc('mailto:?subject='+encodeURIComponent(subject)+'&body='+encodeURIComponent(body))}">${esc(t('emOpen'))}</a><button type="button" class="btn small" data-action="sl-copy" data-id="${key}">${esc(t('pkCopy'))}</button></div></section>`;
}
async function slCall(method,path,body,msg){
  const gid=S.home&&S.home.id;if(!gid)return;
  try{const d=await api(method,'/api/groups/'+gid+'/shortlist'+path,body);S.sl={gid:gid,data:d};S.slPick=null;rerender();if(msg)toast(msg);}
  catch(e){toast(errText(e));rerender();}
}
async function slVote(cid,choice,reason){
  const gid=S.home&&S.home.id;if(!gid)return;
  const before=(slData()||{items:[]}).items.find(x=>x.id===cid),st=before?before.stage:0;
  try{const d=await api('PUT','/api/groups/'+gid+'/shortlist/'+cid+'/vote',{choice:choice,reason:reason||null});S.sl={gid:gid,data:d};S.slPick=null;rerender();
    const after=d.items.find(x=>x.id===cid);
    toast(after&&after.stage>st?(after.stage===3?t('tAgreed'):t('tStageOn',t('st_'+after.stageName))):t('tVoted'));}
  catch(e){toast(errText(e));rerender();}
}
function updateHomes(){
  const el=document.getElementById('hx-results');if(!el||!S.hf)return;
  el.innerHTML=homesResults();applyStyles(el);drawCharts(el);homesControlsSync();
  const all=homesAll(),n=hfApply(all,S.hf).length,c=document.getElementById('hx-count');if(c)c.textContent=t('hxShown',n,all.length);
}
function homesControlsSync(){
  const f=S.hf,[plo,phi]=priceBounds(homesAll());
  const po=document.getElementById('hx-price-out');if(po)po.textContent=euros(f.pmin==null?plo:f.pmin)+' – '+euros(f.pmax==null?phi:f.pmax);
  const mo=document.getElementById('hx-m2-out');if(mo)mo.textContent=f.m2?f.m2+' m²':t('hxAny');
  const fill=document.getElementById('hx-dfill'),span=(phi-plo)||1;
  if(fill){const a=((f.pmin==null?plo:f.pmin)-plo)/span*100,b=((f.pmax==null?phi:f.pmax)-plo)/span*100;fill.style.insetInlineStart=a+'%';fill.style.inlineSize=Math.max(0,b-a)+'%';}
}
/* Re-render the homes section in place, keeping focus on the control that changed */
function rerenderHomes(focusSel){
  const sec=document.getElementById('homes');if(!sec||!S.home)return;
  const d=document.getElementById('hx-filters');if(d)S.hfOpen=d.open;
  const tmp=document.createElement('div');tmp.innerHTML=homesSection(S.home.id);sec.replaceWith(tmp.firstElementChild);
  updateHomes();
  if(focusSel){const el=document.querySelector(focusSel);if(el)el.focus();}
}

/* ---------- matches explorer: affinity distribution, status and score filters ---------- */
function mfState(){if(!S.mf)S.mf={min:0,sort:'score',q:'',chart:false,show:12};return S.mf;}
function mfApply(list,f){const q=f.q.trim().toLowerCase();const r=list.filter(x=>x.score.total>=f.min&&(!q||x.person.name.toLowerCase().indexOf(q)>=0));
  if(f.sort==='name')r.sort((a,b)=>a.person.name.localeCompare(b.person.name,locale()));return r;}
function matchesResults(){
  const m=S.matches,f=mfState(),ps=peopleSub(),all=m.compatible.filter(x=>psOf(x)===ps),sel=mfApply(all,f),filtered=f.min||f.q.trim();
  /* people who liked you come first in To review (stable, so the order by affinity holds within each part) */
  if(ps==='review'&&f.sort==='score')sel.sort((a,b)=>(b.likedMe?1:0)-(a.likedMe?1:0));
  let chart='';
  if(f.chart&&all.length){
    const lo=Math.min(95,Math.floor(Math.min(...all.map(x=>x.score.total),100)/5)*5),bins=[];
    for(let b=lo;b<=95;b+=5){const inb=x=>x.score.total>=b&&(x.score.total<b+5||(b===95&&x.score.total<=100));const a=all.filter(inb).length,s=sel.filter(inb).length;bins.push({lo:b,hi:b+5,all:a,sel:s,tip:t('chScoreTip',b,Math.min(100,b+4+(b===95?1:0)),s,a)});}
    chart=`<figure class="chartcard mb"><figcaption><b>${esc(t('chScoreH'))}</b>${legend([['ctx',t('chScoreAll')],['acc',t('chSel')]])}</figcaption>${chartBox('mx-score',w=>histogram(w,bins,{fmt:v=>v+'%',action:'score-bin'}),t('chScoreH'))}<p class="hint">${esc(t('chClickScore'))}</p></figure>`;
  }
  /* twelve at a time, like the homes */
  const shown=sel.slice(0,f.show),more=sel.length-shown.length;
  const grid=sel.length?`<div class="grid people">${shown.map(personCard).join('')}</div>${more>0?`<p class="center mt"><button type="button" class="btn" data-action="more-people">${esc(t('moreHomes',Math.min(12,more),more))}</button></p>`:''}`:`<div class="card empty"><p>${esc(all.length?t('mfNone'):t('psEmpty_'+ps))}</p>${!all.length&&ps!=='review'&&S.matches.compatible.some(x=>x.state==='none')?`<a class="btn" href="#people-review">${esc(t('psGoReview'))}</a>`:''}</div>`;
  return `${chart}${filtered?`<p class="hint" aria-live="polite">${esc(t('mfShown',sel.length,all.length))}</p>`:''}${grid}`;
}
function updateMatches(){const el=document.getElementById('mx-results');if(!el)return;el.innerHTML=matchesResults();applyStyles(el);drawCharts(el);}
/* One quiet row: search, minimum affinity, sort, and the chart on request */
function matchesToolbar(){
  const f=mfState();
  return `<div class="toolbar">
<div class="fcell grow"><label class="sr" for="mf-q">${esc(t('mfSearch'))}</label><input id="mf-q" type="search" data-mf="q" value="${esc(f.q)}" autocomplete="off" placeholder="${esc(t('mfSearch'))}"></div>
<div class="fcell"><label class="label" for="mf-min">${esc(t('mfMinL'))} <output id="mf-min-out" class="hint">${f.min?f.min+' %':esc(t('hxAny'))}</output></label><input id="mf-min" type="range" data-mf="min" min="0" max="100" step="5" value="${f.min}"></div>
<div class="fcell"><label class="sr" for="mf-sort">${esc(t('hxSort'))}</label><select id="mf-sort" data-mf="sort"><option value="score"${f.sort==='score'?' selected':''}>${esc(t('msort_score'))}</option><option value="name"${f.sort==='name'?' selected':''}>${esc(t('msort_name'))}</option></select></div>
<button type="button" class="chipbtn" data-action="mf-chart" aria-pressed="${!!f.chart}">${esc(t('showChart'))}</button>
</div>`;
}

/* One tooltip for every chart mark: follows the pointer, shows the mark's data-tip */
const tipEl=document.createElement('div');tipEl.className='charttip';tipEl.setAttribute('role','tooltip');tipEl.hidden=true;document.body.appendChild(tipEl);
document.addEventListener('pointermove',e=>{
  const el=e.target.closest&&e.target.closest('[data-tip]');
  if(!el){if(!tipEl.hidden)tipEl.hidden=true;return;}
  tipEl.textContent=el.getAttribute('data-tip');tipEl.hidden=false;
  const r=tipEl.getBoundingClientRect(),x=Math.min(window.innerWidth-r.width-8,Math.max(8,e.clientX-r.width/2)),y=e.clientY-r.height-14<8?e.clientY+18:e.clientY-r.height-14;
  tipEl.style.left=x+'px';tipEl.style.top=y+'px';
});
document.addEventListener('pointerleave',()=>{tipEl.hidden=true;});
async function loadHomes(gid){
  loadShortlist(gid);
  S.homes={gid:gid,loading:true};
  try{const d=await api('GET','/api/groups/'+gid+'/homes');if(S.homes&&S.homes.gid===gid){S.homes={gid:gid,data:d};}}
  catch(e){S.homes={gid:gid,error:errText(e)};}
  if(S.home&&S.home.id===gid)rerender();
}

/* ---------- render ---------- */
function renderChrome(){
  document.documentElement.setAttribute('lang',LANG);
  document.title=t('title');
  const set=(id,txt)=>{const el=document.getElementById(id);if(el)el.textContent=txt;};
  set('brandname',BRAND);
  set('tagline',t('tagline').replace(/ \((working name|nombre provisional)\)/,''));
  const bl=document.getElementById('brandlink');if(bl)bl.setAttribute('aria-label',BRAND+' · '+t('homeLink'));
  const foot=document.getElementById('footnote');
  if(foot)foot.innerHTML=`<p>${esc(t('footnote'))}</p><p class="links"><a href="#help">${esc(t('helpLink'))}</a> · <a href="#sources">${esc(t('srcLink'))}</a> · <a href="#terms">${esc(t('footTerms'))}</a> · <a href="#privacy">${esc(t('footPrivacy'))}</a></p>`;
  const nav=document.getElementById('steps');if(nav){nav.setAttribute('aria-label',t('navAria'));nav.hidden=!inApp();}
  const bell=document.getElementById('alerts-link');
  if(bell){const n=S.counts.alerts||0;bell.hidden=!inApp();bell.innerHTML=ICONS.bell+(n?`<span class="count" aria-hidden="true">${n}</span>`:'');bell.setAttribute('aria-label',t('alertsAria',n));if(S.tab==='alerts')bell.setAttribute('aria-current','page');else bell.removeAttribute('aria-current');}
  renderMenu();
  document.body.classList.toggle('has-nav',inApp());
}
/* The tabs show once someone is signed in and onboarded, and not on a legal page */
function inApp(){return !!(S.me&&S.me.onboarded&&!S.view);}
function renderTabs(){
  app.setAttribute('role','main');
  if(!inApp()){tabsEl.innerHTML='';return;}
  /* Badges count what is waiting for you: people not answered yet, invitations not answered yet */
  /* Only invitations get a badge: they wait for your answer (B-051 review) */
  const invites=S.groups?S.groups.filter(g=>g.myStatus==='pending').length:(S.counts.invites||0),unread=S.counts.messages||0;
  const tabs=[['matches','#people-'+peopleSub(),t('navPeople'),'','',ICONS.people],['groups','#groups',t('navGroups'),invites?String(invites):'',t('srInvites'),ICONS.groups],
    ['messages','#messages',t('navMessages'),unread?String(unread):'',t('srUnread'),ICONS.chat]];
  tabsEl.innerHTML=tabs.map(x=>`<a class="tab" href="${x[1]}" id="tab-${x[0]}" data-tab="${x[0]}"${S.tab===x[0]?' aria-current="page"':''}>${x[5]}<span>${esc(x[2])}</span>${x[3]?`<span class="count"><span aria-hidden="true">${x[3]}</span><span class="sr">: ${x[3]} ${esc(x[4])}</span></span>`:''}</a>`).join('');
}
/* Line icons for the tabs (stroke only, coloured by CSS) */
const ICONS={
  people:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><circle cx="9" cy="8" r="3.2"/><path d="M3 19c.6-3.2 3-5 6-5s5.4 1.8 6 5"/><circle cx="17" cy="9" r="2.6"/><path d="M16.5 14.2c2.4.2 4 1.8 4.5 4.3"/></svg>',
  groups:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M3.5 11 12 4l8.5 7"/><path d="M5.5 9.5V20h13V9.5"/><path d="M9.5 20v-5h2v5M12.5 20v-5h2v5"/></svg>',
  profile:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="8.5" r="3.5"/><path d="M5 20c.8-3.6 3.6-5.6 7-5.6s6.2 2 7 5.6"/></svg>',
  chat:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M4.5 6.5A2.5 2.5 0 0 1 7 4h10a2.5 2.5 0 0 1 2.5 2.5v7A2.5 2.5 0 0 1 17 16h-6l-4.5 4v-4H7a2.5 2.5 0 0 1-2.5-2.5z"/></svg>',
  help:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="8.5"/><path d="M9.6 9.4a2.5 2.5 0 0 1 4.8.9c0 1.7-2.4 2.2-2.4 3.7"/><path d="M12 17h.01"/></svg>',
  doc:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3.5h7l4 4V20a.5.5 0 0 1-.5.5h-10.5A.5.5 0 0 1 6.5 20V4a.5.5 0 0 1 .5-.5z"/><path d="M14 3.5V8h4M9.5 12.5h5M9.5 16h5"/></svg>',
  out:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M14 4.5h4a1.5 1.5 0 0 1 1.5 1.5v12a1.5 1.5 0 0 1-1.5 1.5h-4"/><path d="M10 8l-4 4 4 4M6 12h9"/></svg>',
  auto:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="8"/><path d="M12 4a8 8 0 0 1 0 16z" fill="currentColor"/></svg>',
  sun:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5.3 5.3l1.4 1.4M17.3 17.3l1.4 1.4M5.3 18.7l1.4-1.4M17.3 6.7l1.4-1.4"/></svg>',
  moon:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M19.5 14.5A8 8 0 0 1 9.5 4.5a8 8 0 1 0 10 10z"/></svg>',
  bell:'<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M6 16.5V11a6 6 0 0 1 12 0v5.5l1.5 1.5h-15z"/><path d="M10 20.5a2 2 0 0 0 4 0"/></svg>'
};
/* ---------- header menu (Session 21): profile, language, colour scheme, help ----------
   To add a setting or a place, add an item or a section in menuSections(); renderMenu() draws it.
   Item kinds: {href,label,icon} a link · {action,id,label,icon} a button · {choice:[{action,id,label,icon,on}],label} a segmented choice */
const THEME_KEY='compicasa-theme';
let THEME=(()=>{try{const v=localStorage.getItem(THEME_KEY);return v==='light'||v==='dark'?v:'auto';}catch(e){return 'auto';}})();
function applyTheme(){
  const d=document.documentElement;
  if(THEME==='auto')d.removeAttribute('data-theme');else d.setAttribute('data-theme',THEME);
  const m=document.querySelector('meta[name="color-scheme"]');if(m)m.setAttribute('content',THEME==='auto'?'light dark':THEME);
}
function setTheme(v){THEME=v==='light'||v==='dark'?v:'auto';try{localStorage.setItem(THEME_KEY,THEME);}catch(e){}applyTheme();rerender();}
function menuSections(){
  const secs=[];
  if(S.me){
    const items=[];
    if(S.me.onboarded)items.push({href:'#profile',label:t('navProfile'),icon:ICONS.profile,current:!S.view&&S.tab==='you'});
    items.push({action:'logout',label:t('signOut'),icon:ICONS.out});
    secs.push({id:'account',who:(S.profile&&S.profile.name)||S.me.name||'',items:items});
  }
  secs.push({id:'lang',label:t('menuLang'),items:[{choice:[{action:'lang',id:'es',label:'Español',lang:'es',on:LANG==='es'},{action:'lang',id:'en',label:'English',lang:'en',on:LANG==='en'}],label:t('menuLang')}]});
  secs.push({id:'theme',label:t('menuTheme'),items:[{choice:[['auto',ICONS.auto,t('themeAuto')],['light',ICONS.sun,t('themeLight')],['dark',ICONS.moon,t('themeDark')]].map(x=>({action:'theme',id:x[0],icon:x[1],label:x[2],on:THEME===x[0]})),label:t('menuTheme')}]});
  secs.push({id:'more',label:t('menuMore'),items:[{href:'#help',label:t('helpLink'),icon:ICONS.help,current:S.view==='help'},{href:'#sources',label:t('srcLink'),icon:ICONS.groups,current:S.view==='sources'},{href:'#terms',label:t('footTerms'),icon:ICONS.doc,current:S.view==='terms'},{href:'#privacy',label:t('footPrivacy'),icon:ICONS.doc,current:S.view==='privacy'}]});
  return secs;
}
function menuItem(it){
  if(it.choice)return `<div class="mseg" role="group" aria-label="${esc(it.label)}">${it.choice.map(o=>`<button type="button" class="mopt" data-action="${o.action}" data-id="${o.id}" aria-pressed="${o.on?'true':'false'}"${o.lang?` lang="${o.lang}"`:''}>${o.icon||''}<span>${esc(o.label)}</span></button>`).join('')}</div>`;
  if(it.href)return `<a class="mitem" href="${it.href}"${it.current?' aria-current="page"':''}>${it.icon||''}<span>${esc(it.label)}</span></a>`;
  return `<button type="button" class="mitem" data-action="${it.action}"${it.id?` data-id="${it.id}"`:''}>${it.icon||''}<span>${esc(it.label)}</span></button>`;
}
function renderMenu(){
  const btn=document.getElementById('menu-btn'),menu=document.getElementById('menu');if(!btn||!menu)return;
  btn.setAttribute('aria-label',t('menuAria'));btn.setAttribute('aria-expanded',S.menuOpen?'true':'false');
  menu.setAttribute('aria-label',t('menuAria'));
  menu.hidden=!S.menuOpen;
  if(!S.menuOpen){menu.innerHTML='';return;}
  /* Keep focus on the same control when a choice redraws the menu */
  const a=document.activeElement,keep=a&&menu.contains(a)?[a.getAttribute('data-action'),a.getAttribute('data-id'),a.getAttribute('href')]:null;
  menu.innerHTML=menuSections().map(sec=>`<div class="msec" data-sec="${sec.id}">${sec.who!==undefined?`<p class="mwho"><strong>${esc(sec.who)}</strong>${esc(t('menuSignedIn'))}</p>`:''}${sec.label?`<p class="mhead">${esc(sec.label)}</p>`:''}${sec.items.map(menuItem).join('')}</div>`).join('');
  if(keep){const el=[...menu.querySelectorAll('[data-action],[href]')].find(x=>x.getAttribute('data-action')===keep[0]&&x.getAttribute('data-id')===keep[1]&&x.getAttribute('href')===keep[2]);if(el)el.focus();}
}
function openMenu(){S.menuOpen=true;renderMenu();const f=document.querySelector('#menu .mitem, #menu .mopt');if(f)f.focus();}
function closeMenu(focusBtn){if(!S.menuOpen)return;S.menuOpen=false;renderMenu();if(focusBtn){const b=document.getElementById('menu-btn');if(b)b.focus();}}
document.addEventListener('click',e=>{
  if(!S.menuOpen)return;
  if(!e.target.closest('.menuwrap')){closeMenu(false);return;}
  if(e.target.closest('#menu a, #menu [data-action="logout"]'))closeMenu(false);
});
document.addEventListener('keydown',e=>{if(e.key==='Escape'&&S.menuOpen){e.preventDefault();closeMenu(true);}});
/* The bottom bar only appears when it carries a real action: the wizard's Back and Next, or a group you're building */
function renderBar(){
  const bar=document.getElementById('actionbar');
  let h='';
  if(S.me&&!S.view&&!S.me.onboarded){
    const last=S.wiz===wizSteps().length-1,save=S.save?`<span class="hint" role="status">${esc(t(S.save))}</span>`:'';
    h=`<button type="button" class="btn" data-action="wiz-back"${S.wiz===0?' disabled':''}>${esc(t('wizBack'))}</button>${save}<span class="spacer"></span><button type="button" class="btn primary" data-action="${last?'wiz-finish':'wiz-next'}">${esc(last?t('wizFinish'):t('wizNext'))}</button>`;
  }else if(inApp()&&S.tab==='matches'&&S.draft.length){
    h=`<div class="stat draft-who">${S.draft.map(id=>avatar(id,28)).join('')}<span>${esc(t('draftBar',1+S.draft.length))}</span></div><span class="spacer"></span><button type="button" class="btn small ghost" data-action="clear-draft">${t('clearDraft')}</button><button type="button" class="btn small" data-action="preview" data-id="${esc(S.draft.join(','))}">${t('previewDraft')}</button><button type="button" class="btn small primary" data-action="propose" data-id="${esc(S.draft.join(','))}">${t('propose')}</button>`;
  }
  bar.hidden=!h;barEl.innerHTML=h;if(h)applyStyles(barEl);
  document.body.classList.toggle('has-bar',!!h);
}
function render(){
  /* B-061: a dialog's "close" event comes a moment after close(). If a render lands in between (a refresh finishing),
     the closed sheet would be replaced by a new open one and the close lost; so settle a closed sheet first */
  const sh=document.getElementById('person-sheet');
  if(sh&&sh.dataset.shown&&!sh.open&&S.person){sheetClosed();if(!S.goingBack)return;}
  renderChrome();
  renderTabs();
  const views={home:viewDash,you:viewYou,matches:viewMatches,groups:viewGroups,messages:viewMessages,alerts:viewAlerts};
  app.innerHTML=S.view?(S.view==='help'?viewHelp():S.view==='sources'?viewSources():S.view==='funnel'?viewFunnel():viewLegal(S.view)):(!S.me?viewAuth():(!S.me.onboarded?viewWizard():(views[S.tab]||viewGroups)()));
  applyStyles(app);
  if(document.getElementById('hx-results'))updateHomes();
  drawCharts(app);
  renderBar();
  syncHash();
  if(S.person)openSheet();
  scrollChat();
  ensureData();
}

/* ---------- addresses (Session 18, B-051): every view has one, so Back and shared links work ----------
   #people-review|-liked|-matches|-passed · #person-<id> (a sheet over People) · #groups ·
   #group-<id>[-brief|-homes] · #preview[-brief] · #profile · #home (the logo's page, Session 20).
   #help, #sources, #terms, #privacy, #lang= and #authError= are read by readHash as before. */
const GSUBS=['overview','brief','homes','shortlist','chat'];
function groupHash(h,sub){return (h.preview?'#preview':'#group-'+h.id)+(sub&&sub!=='overview'?'-'+sub:'');}
function routeHash(){
  if(!inApp())return '';
  if(S.tab==='home')return '#home';
  if(S.tab==='you')return '#profile';
  if(S.tab==='alerts')return '#alerts';
  if(S.tab==='messages')return S.chatKey?'#chat-'+S.chatKey.id:'#messages';
  if(S.tab==='matches')return S.person?'#person-'+S.person:'#people-'+peopleSub();
  if(S.home)return groupHash(S.home,S.gsub);
  return '#groups';
}
function parseRoute(h){
  h=(h||'').replace(/^#/,'');
  let m;
  if(h==='home')return {tab:'home'};
  if(h==='profile')return {tab:'you'};
  if((m=/^people(?:-(review|liked|matches|passed))?$/.exec(h)))return {tab:'matches',ps:m[1]||null};
  if((m=/^person-([A-Za-z0-9_.-]{1,64})$/.exec(h)))return {tab:'matches',person:m[1]};
  if(h==='groups')return {tab:'groups'};
  if(h==='alerts')return {tab:'alerts'};
  if(h==='messages')return {tab:'messages'};
  if((m=/^chat-([A-Za-z0-9_.-]{1,64})$/.exec(h)))return {tab:'messages',chat:m[1]};
  if((m=/^preview(?:-(brief))?$/.exec(h)))return {tab:'groups',preview:true,gsub:m[1]||'overview'};
  if((m=/^group-([0-9a-f-]{36})(?:-(brief|homes|shortlist|chat))?$/.exec(h)))return {tab:'groups',gid:m[1],gsub:m[2]||'overview'};
  return null;
}
function syncHash(){
  /* while the sheet's history.back() is on its way, adding an address would make Back land on the sheet again (B-061) */
  if(S.goingBack)return;
  const h=routeHash();if(!h)return;
  if(location.hash===h){S.atHash=h;return;}
  /* the address moved to another of our views (a link, Back) and its hashchange hasn't run yet: a render now
     (a refresh finishing) must not write the old view's address over it; the hashchange handler will show it */
  if(S.atHash&&location.hash!==S.atHash&&parseRoute(location.hash))return;
  /* the first address replaces "#lang=en" and the like; later ones add a history entry */
  if(parseRoute(location.hash))location.hash=h;else{try{history.replaceState(null,'',h);}catch(e){}}
  S.atHash=h;
}
/* Apply an address; true if it was one of ours */
function applyRoute(h){
  const r=parseRoute(h);if(!r||!inApp())return false;
  if(r.preview&&!(S.home&&S.home.preview))return applyRoute('#groups');
  const before={tab:S.tab,gid:S.home&&S.home.id,preview:S.home&&S.home.preview,person:S.person};
  S.tab=r.tab;
  S.person=r.person||null;
  if(r.tab==='messages'){const k=r.chat?{kind:'person',id:r.chat}:null;if(!k||!S.chatKey||S.chatKey.id!==k.id)S.chat=null;S.chatKey=k;if(!k)S.chats=null;}
  if(r.tab==='alerts')S.inbox=null;
  if(r.tab==='matches'&&r.ps){if(r.ps!==S.ps&&S.mf)S.mf.show=12;S.ps=r.ps;}
  if(r.tab!=='groups'||(!r.gid&&!r.preview))S.home=null;
  if(r.gid){S.gsub=r.gsub;if(!S.home||S.home.id!==r.gid){openGroup(r.gid);return true;}}
  if(r.preview)S.gsub=r.gsub;
  render();
  /* a sub-view of the same page, or the person sheet: keep the place; otherwise start at the top */
  const samePage=before.tab===S.tab&&(S.tab!=='groups'||(S.home&&(before.gid===S.home.id||(before.preview&&S.home.preview))));
  if(S.person){S.sheetFromApp=before.tab==='matches';openSheet();}
  else if(samePage&&!before.person){const cur=app.querySelector('.subtabs [aria-current="page"]');if(cur){cur.focus({preventScroll:true});const nav=cur.closest('.subtabs');if(nav.getBoundingClientRect().top<0)nav.scrollIntoView({block:'start'});}}
  else if(!before.person){window.scrollTo(0,0);focusHeading();}
  if(!samePage&&(r.tab==='matches'||r.tab==='groups'||r.tab==='home')&&!r.gid)refresh();
  return true;
}
function rerender(){const y=window.scrollY||0;render();window.scrollTo(0,y);}
function focusHeading(){const h=document.querySelector('#app h2');if(h){h.setAttribute('tabindex','-1');h.focus({preventScroll:true});}}

let toastTimer=0;
function toast(msg){toastEl.textContent=msg;toastEl.classList.add('show');clearTimeout(toastTimer);toastTimer=setTimeout(()=>toastEl.classList.remove('show'),3200);}

/* ---------- loading ---------- */
async function loadMatches(){const m=await api('GET','/api/matches');remember(m.people);S.matches=m;}
async function loadGroups(){const gs=await api('GET','/api/groups');gs.forEach(g=>remember(g.people));S.groups=gs;}
async function refresh(){
  try{await Promise.all([loadMatches(),loadGroups()]);}catch(e){toast(errText(e));}
  rerender();
  loadCounts();
}
async function go(tab,sub){
  S.tab=tab;S.home=null;if(tab==='matches'&&sub)S.ps=sub;
  render();window.scrollTo(0,0);focusHeading();
  if(tab==='matches'||tab==='groups')await refresh();
}
/* After signing in or up: load the profile, then either the wizard or the app */
async function enter(me){
  S.me=me;S.authError='';
  PEOPLE[S.me.id]={name:S.me.name,color:'#52607a'};
  try{S.profile=await api('GET','/api/profile');}catch(e){toast(errText(e));}
  if(S.me.onboarded){S.tab=S.tab||'home';
    /* Opening the app at an address: applyRoute sees the same tab as before and skips loading, so always load here (found in Session 19) */
    if(S.startRoute&&applyRoute(S.startRoute)){S.startRoute=null;refresh();return;}render();focusHeading();refresh();}
  else{S.wiz=0;render();focusHeading();}
}
async function start(){
  try{S.config=await api('GET','/api/config');}catch(e){S.config={auth:'local',invite:true,terms:''};}
  readHash();
  if(parseRoute(location.hash))S.startRoute=location.hash;
  let me=null;try{me=await api('GET','/api/me');}catch(e){}
  if(me){S.tab='home';await enter(me);}else render();
}
/* #terms, #privacy, #help, #sources, #authError=code (back from External ID), #lang=en */
function readHash(){
  const h=new URLSearchParams((location.hash||'').replace(/^#/,''));
  const prev=S.view;
  S.view=h.has('terms')?'terms':(h.has('privacy')?'privacy':(h.has('help')?'help':(h.has('sources')?'sources':(h.has('funnel')?'funnel':null))));
  if(S.view==='funnel'&&prev!=='funnel')S.funnel=null; // fresh counts each visit
  const err=h.get('authError');
  if(err){S.authError=t('err_'+err)!==undefined&&STR.es['err_'+err]!==undefined?t('err_'+err):t('err_generic');if(err==='noAccount')S.authMode='signup';
    try{history.replaceState(null,'',location.pathname);}catch(e){}}
  return prev!==S.view;
}

/* ---------- profile saving (debounced) ---------- */
let saveTimer=0,saveQueued=false,saving=null;
function profileChanged(){
  S.save='saving';renderBar();
  clearTimeout(saveTimer);saveQueued=true;
  saveTimer=setTimeout(saveNow,600);
}
function saveNow(){
  saveQueued=false;
  saving=(async()=>{
    try{
      const body=Object.assign({},S.profile,{bio:S.profile.bio&&S.profile.bio.es?S.profile.bio:null});
      S.profile=await api('PUT','/api/profile',body);
      PEOPLE[S.me.id]=Object.assign({},PEOPLE[S.me.id],{name:S.profile.name,household:S.profile.household,adults:S.profile.adults,kids:S.profile.kids});
      S.save='saved';S.matches=null;S.dashHomes=null;
      if(S.me.onboarded)await loadMatches();
      return true;
    }catch(e){S.save='saveFailed';toast(errText(e));return false;}
    finally{renderTabs();renderBar();}
  })();
  return saving;
}
/* Save anything pending now and wait for it; false if it failed */
async function flushSave(){
  if(saveQueued){clearTimeout(saveTimer);return saveNow();}
  return saving?saving:true;
}
async function wizMove(step){
  const err=document.getElementById('wiz-error');
  const active=document.activeElement;if(active&&active.matches&&active.matches('input,textarea,select'))active.blur();
  const steps=wizSteps();
  if(step>0){const msg=steps[S.wiz].check?steps[S.wiz].check(S.profile):'';if(msg){if(err)err.textContent=msg;return;}}
  if(!(await flushSave()))return;
  S.wiz=Math.max(0,Math.min(steps.length-1,S.wiz+step));
  render();window.scrollTo(0,0);focusHeading();
}
async function wizFinish(){
  if(!(await flushSave()))return;
  try{S.me=await api('POST','/api/me/onboarded');S.tab='home';render();window.scrollTo(0,0);focusHeading();refresh();}
  catch(e){const err=document.getElementById('wiz-error');if(err)err.textContent=errText(e);}
}

/* ---------- actions ---------- */
async function react(id,kind){
  try{
    const r=await api('PUT','/api/reactions/'+encodeURIComponent(id),{kind:kind});
    if(kind==='like'){const n=personName(id);toast(r.mutual?(r.isDemo?t('tMutual',n):t('tMutualReal',n)):(r.isDemo?t('tNoMatch',n):t('tWaiting',n)));}
    if(kind!=='like')S.draft=S.draft.filter(x=>x!==id);
    S.dashHomes=null;
    await loadMatches();rerender();
  }catch(e){toast(errText(e));rerender();}
}
async function addToDraft(id){
  if(S.draft.indexOf(id)>=0){toast(t('tAlready',personName(id)));return;}
  try{await api('POST','/api/preview',{ids:S.draft.concat([id])});S.draft.push(id);rerender();toast(t('tAdded',personName(id)));}
  catch(e){toast(t('tCantAdd',personName(id),errText(e)));}
}
async function preview(ids){
  try{const h=await api('POST','/api/preview',{ids:ids});remember(h.people);S.home=h;S.gsub='overview';S.tab='groups';render();window.scrollTo(0,0);focusHeading();toast(t('tPreview'));}
  catch(e){toast(t('tCantForm',errText(e)));}
}
async function propose(ids){
  try{const h=await api('POST','/api/groups',{ids:ids});remember(h.people);S.home=h;S.gsub='overview';S.draft=[];S.tab='groups';await loadGroups();render();window.scrollTo(0,0);focusHeading();toast(t('tProposed'));if(h.id&&h.check&&h.check.ok)loadHomes(h.id);}
  catch(e){toast(t('tCantForm',errText(e)));}
}
async function openGroup(id,sub){
  if(sub)S.gsub=sub;
  try{const h=await api('GET','/api/groups/'+id);remember(h.people);S.home=h;S.tab='groups';render();window.scrollTo(0,0);focusHeading();if(h.check&&h.check.ok)loadHomes(id);}
  catch(e){toast(errText(e));}
}
async function respond(id,accept){
  try{const h=await api('POST','/api/groups/'+id+'/respond',{accept:accept});remember(h.people);await loadGroups();
    /* After declining, the group is no longer yours to see */
    if(accept){S.home=h;render();if(h.check&&h.check.ok)loadHomes(id);}else{S.home=null;if(location.hash!=='#groups')location.hash='#groups';else render();}
    toast(accept?t('tAccepted'):t('tDeclined'));loadCounts();}
  catch(e){toast(errText(e));if(e.code==='expired'||e.code==='withdrawn'){S.home=null;await loadGroups();if(location.hash!=='#groups')location.hash='#groups';else render();}}
}
async function groupAction(gid,path,id,msg){
  try{const h=await api('POST','/api/groups/'+gid+'/'+path,{id:id});remember(h.people);S.home=h;await loadGroups();rerender();toast(t(msg,personName(id)));}
  catch(e){toast(errText(e));}
}
async function leave(id){
  try{await api('POST','/api/groups/'+id+'/leave');S.home=null;await loadGroups();render();toast(t('tLeft'));}
  catch(e){toast(errText(e));}
}
function cycleFeature(btn,id){
  const order=['none','nice','must'],cur=S.profile.needs[id]||'none',nx=order[(order.indexOf(cur)+1)%3];
  if(nx==='none')delete S.profile.needs[id];else S.profile.needs[id]=nx;
  btn.outerHTML=featBtn(FEATURES.find(f=>f.id===id),S.profile.needs[id]);
  const again=app.querySelector('[data-action="feat"][data-id="'+id+'"]');if(again)again.focus();
  profileChanged();
}
function switchLang(l){setLang(l);try{localStorage.setItem(LANG_KEY,LANG);}catch(e){}rerender();toast(t('tLang'));}

/* ---------- events ---------- */
app.addEventListener('submit',async e=>{
  if(e.target.id==='chat-form'){e.preventDefault();sendChat();return;}
  if(e.target.id!=='auth-form')return;
  e.preventDefault();
  const f=e.target,data={},errEl=document.getElementById('auth-error'),btn=f.querySelector('button[type=submit]');
  Array.prototype.forEach.call(f.querySelectorAll('input[name]'),i=>{data[i.name]=i.type==='checkbox'?i.checked:i.value;});
  const up=S.authMode==='signup',ext=S.config&&S.config.auth==='external';
  if(up){data.terms=data.terms?S.config.terms:null;data.adult=!!data.adult;}
  S.busy=true;btn.disabled=true;errEl.textContent='';
  try{
    if(ext){
      const r=await api('POST','/api/auth/start',{inviteCode:data.inviteCode||'',adult:data.adult,terms:data.terms});
      location.href='/auth/external?ticket='+encodeURIComponent(r.ticket)+'&lang='+LANG;
      return;
    }
    const me=await api('POST',up?'/api/auth/signup':'/api/auth/login',data);
    S.busy=false;S.tab=up?'you':'home';
    await enter(me);
  }catch(err){S.busy=false;btn.disabled=false;errEl.textContent=errText(err);}
});
app.addEventListener('change',e=>{
  const el=e.target;
  if(el.getAttribute('data-hf')==='sort'&&S.hf){S.hf.sort=el.value;S.hf.show=12;updateHomes();return;}
  if(el.getAttribute('data-mf')==='sort'){mfState().sort=el.value;updateMatches();return;}
  if(el.hasAttribute('data-size')){
    const vals={};app.querySelectorAll('input[data-size]').forEach(x=>{vals[x.getAttribute('data-size')]=parseInt(x.value,10);});
    if(isNaN(vals.min)||isNaN(vals.max)||!S.home)return;
    api('PUT','/api/groups/'+S.home.id+'/size',{min:vals.min,max:vals.max}).then(r=>{S.home.size=r;toast(t('sizeSaved'));}).catch(err=>toast(errText(err)));
    return;
  }
  const f=el.getAttribute('data-field');if(!f||!S.profile)return;
  const u=S.profile;
  if(f==='name'){u.name=el.value.trim();}
  else if(f==='bio'){const v=el.value.trim();u.bio=v?{es:v,en:v}:null;}
  else if(f==='maxBudget'){const v=parseInt(String(el.value).replace(/[^0-9]/g,''),10);if(!isNaN(v)&&(v<10000||v>5000000)){toast(t('err_badBudget'));return;}u.maxBudget=isNaN(v)?null:v;}
  else if(el.hasAttribute('data-multi')){u[f]=Array.prototype.map.call(app.querySelectorAll('input[data-field="'+f+'"]:checked'),x=>x.value);if(f==='areas')refreshAreaPicker(u[f]);}
  else if(el.hasAttribute('data-bool'))u[f]=el.checked;
  else if(NUM_FIELDS.indexOf(f)>=0)u[f]=parseInt(el.value,10);
  else u[f]=el.value;
  if(f==='household'){
    if(u.household==='family'){u.adults=2;if(u.kids<1)u.kids=1;}
    else if(u.household==='singleParent'){u.adults=1;if(u.kids<1)u.kids=1;}
    else if(u.household==='couple'){u.adults=2;u.kids=0;}
    else{u.adults=1;u.kids=0;}
    rerender();
  }
  if(f==='purpose')rerender();
  profileChanged();
});
app.addEventListener('input',e=>{const el=e.target;
  if(el.hasAttribute('data-hf')&&S.hf&&el.type==='range'){
    const k=el.getAttribute('data-hf'),v=parseInt(el.value,10),f=S.hf,[plo,phi]=priceBounds(homesAll());
    if(k==='m2')f.m2=v;
    else{f[k]=v;if(f.pmin!=null&&f.pmax!=null&&f.pmin>f.pmax){if(k==='pmin')f.pmax=f.pmin;else f.pmin=f.pmax;const o=app.querySelector('input[data-hf="'+(k==='pmin'?'pmax':'pmin')+'"]');if(o)o.value=f[k==='pmin'?'pmax':'pmin'];}
      if(f.pmin!=null&&f.pmin<=plo)f.pmin=null;if(f.pmax!=null&&f.pmax>=phi)f.pmax=null;}
    f.show=12;homesControlsSync();updateHomes();return;}
  if(el.hasAttribute('data-mf')){const f=mfState(),k=el.getAttribute('data-mf');f.show=12;if(k==='q')f.q=el.value;else if(k==='min'){f.min=parseInt(el.value,10);const o=document.getElementById('mf-min-out');if(o)o.textContent=f.min?f.min+' %':t('hxAny');}updateMatches();return;}if(el.id==='a-pass'&&S.authMode==='signup'){updateMeter(el.value);return;}if(el.type==='range'&&S.profile){S.profile[el.getAttribute('data-field')]=parseInt(el.value,10);profileChanged();}});
document.addEventListener('click',async e=>{
  /* Tabs and sub-views are links: the address changes and the hashchange handler shows the view */
  const el=e.target.closest('[data-action]');if(!el||el.disabled)return;
  const id=el.getAttribute('data-id');
  const ids=()=>(id||'').split(',').filter(Boolean);
  switch(el.getAttribute('data-action')){
    case 'lang':switchLang(id);break;
    case 'menu':S.menuOpen?closeMenu(false):openMenu();break;
    case 'theme':setTheme(id);break;
    case 'auth-mode':S.authMode=id;S.authError='';render();break;
    case 'ext-signin':location.href='/auth/external?lang='+LANG;break;
    case 'pw-toggle':{const inp=document.getElementById('a-pass'),show=inp.type==='password';inp.type=show?'text':'password';el.setAttribute('aria-pressed',String(show));el.textContent=show?t('hidePw'):t('showPw');inp.focus();break;}
    case 'wiz-next':wizMove(1);break;
    case 'wiz-back':wizMove(-1);break;
    case 'wiz-finish':wizFinish();break;
    case 'legal-back':S.view=null;try{history.replaceState(null,'',location.pathname);}catch(err){}render();window.scrollTo(0,0);focusHeading();break;
    case 'goto':go(id,el.getAttribute('data-sub'));break;
    case 'open-sec':S.openSecs=Object.assign({},S.openSecs,{[id]:true});break;
    case 'preset':Object.assign(S.profile,JSON.parse(JSON.stringify(PRESETS[id].v)));rerender();profileChanged();toast(t('tExample',tx(PRESETS[id].label)));break;
    case 'feat':cycleFeature(el,id);break;
    case 'like':el.disabled=true;react(id,'like');break;
    case 'pass':el.disabled=true;react(id,'pass');break;
    case 'undo':el.disabled=true;react(id,'none');break;
    case 'add':addToDraft(id);break;
    case 'remove':S.draft=S.draft.filter(x=>x!==id);rerender();toast(t('tRemoved'));break;
    case 'clear-draft':S.draft=[];rerender();break;
    case 'preview':preview(ids());break;
    case 'propose':propose(ids());break;
    case 'close-preview':go('groups');break;
    case 'close-sheet':{const d=document.getElementById('person-sheet');if(d&&d.open)d.close();break;}
    case 'all-groups':S.home=null;go('groups');break;
    case 'respond':respond(id,el.getAttribute('data-accept')==='1');break;
    case 'leave':leave(id);break;
    case 'invite-member':el.disabled=true;groupAction(el.getAttribute('data-gid'),'invite',id,'tInvited');break;
    case 'withdraw':el.disabled=true;groupAction(el.getAttribute('data-gid'),'withdraw',id,'tWithdrawn');break;
    case 'report':openReport(id);break;
    case 'block':blockPerson(id);break;
    case 'unblock':{const b=(S.blocks||[]).find(x=>x.id===id);try{await api('DELETE','/api/blocks/'+encodeURIComponent(id));toast(t('tUnblocked',b?b.name:''));S.blocks=null;S.matches=null;rerender();}catch(err){toast(errText(err));}break;}
    case 'dlg-close':closeDialog(el);break;
    case 'conduct-ok':try{S.me=await api('POST','/api/me/conduct');closeDialog(el);sendChat();}catch(err){toast(errText(err));}break;
    case 'hf':{const f=S.hf;if(!f)break;const k=el.getAttribute('data-k'),v=el.getAttribute('data-v');
      if(k==='types'||k==='areas'){const i=f[k].indexOf(v);if(i>=0)f[k].splice(i,1);else f[k].push(v);}
      else if(k==='beds')f.beds=parseInt(v,10)||0;else if(k==='energy')f.energy=v;else f[k]=!f[k];
      f.show=12;rerenderHomes('[data-action="hf"][data-k="'+k+'"][data-v="'+v+'"]');break;}
    case 'hf-reset':if(S.home){S.hf=null;hfFor(S.home.id);rerenderHomes('#hx-filters summary');}break;
    case 'hview':S.hview=el.getAttribute('data-v');rerenderHomes('[data-action="hview"][data-v="'+S.hview+'"]');break;
    case 'sl-add':el.disabled=true;slCall('POST','',{homeId:id},t('tShortlisted'));break;
    case 'sl-remove':slCall('DELETE','/'+id,undefined,t('tSlRemoved'));break;
    case 'sl-vote':{const v=el.getAttribute('data-v'),cid=+id;if(v==='yes')slVote(cid,'yes');else{S.slPick={cid:cid,choice:v};rerender();const f=app.querySelector('#cand-'+cid+' .reasons .chipbtn');if(f)f.focus();}break;}
    case 'sl-reason':slVote(+id,el.getAttribute('data-c'),el.getAttribute('data-v'));break;
    case 'sl-split':S.slSplit=el.getAttribute('data-v');rerender();break;
    case 'sl-copy':try{await navigator.clipboard.writeText(COPY[id]||'');toast(t('tCopied'));}catch(err){toast(t('tCopyFailed'));}break;
    case 'more-homes':if(S.hf){S.hf.show+=12;updateHomes();}break;
    case 'bin':if(S.hf){S.hf.pmin=+el.getAttribute('data-lo');S.hf.pmax=+el.getAttribute('data-hi');const[plo,phi]=priceBounds(homesAll());if(S.hf.pmin<=plo)S.hf.pmin=null;if(S.hf.pmax>=phi)S.hf.pmax=null;S.hf.show=12;rerenderHomes();}break;
    case 'dot':{const f=S.hf;if(!f)break;if(S.hview==='market'){S.hview='list';rerenderHomes();}const list=hfApply(homesAll(),f).sort(HSORTS[f.sort]||HSORTS.fit),i=list.findIndex(x=>x.id===id);if(i>=f.show){f.show=i+1;updateHomes();}
      const card=document.getElementById('home-'+id);if(card){card.scrollIntoView({behavior:'smooth',block:'center'});card.classList.add('flash');setTimeout(()=>card.classList.remove('flash'),1600);}break;}
    case 'more-people':{mfState().show+=12;updateMatches();break;}
    case 'mf-chart':{const f=mfState();f.chart=!f.chart;el.setAttribute('aria-pressed',String(f.chart));updateMatches();break;}
    case 'score-bin':{const f=mfState();f.min=+el.getAttribute('data-lo');const r=document.getElementById('mf-min');if(r)r.value=f.min;const o=document.getElementById('mf-min-out');if(o)o.textContent=f.min?f.min+' %':t('hxAny');updateMatches();break;}
    case 'logout':{let r=null;try{r=await api('POST','/api/auth/logout');}catch(err){}if(r&&r.next){location.href=r.next;break;}S.me=null;S.profile=null;S.matches=null;S.groups=null;S.home=null;S.draft=[];S.inbox=null;S.chats=null;S.chat=null;S.chatKey=null;S.blocks=null;S.dashHomes=null;S.sl=null;S.counts={alerts:0,messages:0,invites:0};S.authMode='signin';render();break;}
    case 'delete-account':if(window.confirm(t('deleteConfirm'))){try{await api('DELETE','/api/me');S.me=null;S.matches=null;S.groups=null;S.home=null;S.draft=[];render();}catch(err){toast(errText(err));}}break;
  }
});
/* ---------- start: Spanish unless the viewer chose English; #lang= overrides both ---------- */
let saved=null;try{saved=localStorage.getItem(LANG_KEY);}catch(e){}
setLang(saved==='en'?'en':'es');
try{const h=new URLSearchParams((location.hash||'').replace(/^#/,''));const lg=h.get('lang');if(lg==='en'||lg==='es')setLang(lg);}catch(e){}
document.documentElement.setAttribute('lang',LANG); /* so the start-up screen speaks the chosen language before the first render */
applyTheme();
document.addEventListener('close',e=>{
  if(e.target.id==='person-sheet')sheetClosed();
},true);
app.addEventListener('click',e=>{if(e.target.id==='person-sheet')e.target.close();});
/* The report dialog sits outside #app */
document.addEventListener('submit',async e=>{
  if(e.target.id!=='report-form')return;
  e.preventDefault();
  const f=e.target,id=f.getAttribute('data-id'),reason=(f.querySelector('input[name=reason]:checked')||{}).value;
  if(!reason){toast(t('err_badReason'));return;}
  const block=f.querySelector('input[name=block]').checked;
  try{await api('POST','/api/reports',{about:id,reason:reason,note:f.querySelector('textarea').value,block:block,thread:S.chatKey&&S.chatKey.id===id?'p:'+[S.me.id,id].sort().join('|'):null});
    closeDialog(f);toast(t('tReported'));if(block)await afterBlock();}
  catch(err){toast(errText(err));}
});
/* Enter sends; Shift+Enter starts a new line */
app.addEventListener('keydown',e=>{if(e.target.id==='chat-input'&&e.key==='Enter'&&!e.shiftKey&&!e.isComposing){e.preventDefault();sendChat();}});
window.addEventListener('popstate',()=>{S.goingBack=false;});
window.addEventListener('hashchange',()=>{S.goingBack=false;S.atHash=location.hash;if(S.menuOpen){S.menuOpen=false;renderMenu();}
  /* the logo (#home) also closes the guide and the legal pages */
  if(readHash()){if(!applyRoute(location.hash)){render();window.scrollTo(0,0);focusHeading();}return;}
  if(location.hash!==routeHash())applyRoute(location.hash);
});
window.__compicasa={S:S,M:M,setLang:setLang,render:render,api:api};
window.addEventListener('scroll',()=>{tipEl.hidden=true;},{passive:true});
if('serviceWorker' in navigator)navigator.serviceWorker.register('/sw.js').catch(()=>{});
start();
})();
