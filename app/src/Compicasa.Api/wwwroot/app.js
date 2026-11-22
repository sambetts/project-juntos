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
function hhLabel(p){
  if(p.household==='family')return t('hhFamily',p.adults,t('kids',p.kids));
  if(p.household==='singleParent')return t('hhSingleParent',t('kids',p.kids));
  return tx(hh(p.household).label);
}

/* ---------- people and messages from the API ---------- */
const PEOPLE={};
function remember(map){if(map)Object.keys(map).forEach(k=>{PEOPLE[k]=map[k];});}
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
const S={me:null,config:null,view:null,wiz:0,authError:'',tab:'you',authMode:'signin',profile:null,matches:null,groups:null,home:null,draft:[],save:'',busy:false};
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
  family:{label:{en:'Family wanting half a house',es:'Familia que busca media casa'},v:{household:'family',adults:2,kids:2,bedrooms:3,privateBath:true,cars:1,pets:[],acceptsPets:true,smoking:'no',acceptsSmoking:'outside',
    sleep:2,tidy:4,social:2,guests:2,meals:2,wfh:true,horizon:'10+',areas:['north','city'],types:['house'],
    needs:{parking:'must',garden:'must',schools:'must',quiet:'nice',pool:'nice',storage:'nice'},open:['family','singleParent'],maxHouseholds:2}},
  room:{label:{en:'Just a room, open to anyone',es:'Solo una habitación, con quien sea'},v:{household:'single',adults:1,kids:0,bedrooms:1,privateBath:false,cars:0,pets:[],acceptsPets:true,smoking:'no',acceptsSmoking:'outside',
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
  else if(ext)body=`<form id="auth-form" novalidate><p>${esc(t('extSignUpLead'))}</p>${consentFields()}<p class="error" id="auth-error" role="alert">${esc(S.authError)}</p><button type="submit" class="btn primary"${S.busy?' disabled':''}>${esc(t('extContinue'))}</button></form>`;
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

function viewLegal(kind){
  const body=t(kind+'Body');
  return `<p><button type="button" class="btn small ghost" data-action="legal-back">← ${esc(t('legalBack'))}</button></p>
<section class="card legal"><h2>${esc(t(kind+'H'))}</h2>
<p class="notice card">${esc(t('legalDraft'))}</p>
<ol class="mt">${body.map(x=>`<li>${esc(x)}</li>`).join('')}</ol></section>`;
}

const SECTIONS={
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
  share:u=>`${checkGroup('open',t('happyShare'),loc(HOUSEHOLDS),u.open)}
<p class="hint">${t('demoHousehold')}</p>
${selectField('maxHouseholds',t('maxHHLabel'),[{id:2,label:t('maxHH2')},{id:3,label:'3'},{id:4,label:t('maxHH4')}],u.maxHouseholds)}
${checkField('acceptsPets',t('okPets'),u.acceptsPets)}
${selectField('acceptsSmoking',t('smokersQ'),[{id:'none',label:t('smokersNone')},{id:'outside',label:t('smokersOut')},{id:'any',label:t('smokersAny')}],u.acceptsSmoking)}`,
  where:u=>`${checkGroup('areas',t('areasLabel'),loc(AREAS),u.areas)}
${checkGroup('types',t('typesLabel'),loc(TYPES),u.types)}
${radioGroup('horizon',loc(HORIZONS),u.horizon,t('horizonLabel'))}`,
  needs:u=>`<p class="hint">${t('needsHint')}</p>
<div class="feat">${FEATURES.map(f=>featBtn(f,u.needs[f.id])).join('')}</div>`
};
function presetButtons(){return Object.keys(PRESETS).map(k=>`<button type="button" class="btn small" data-action="preset" data-id="${k}">${esc(tx(PRESETS[k].label))}</button>`).join('');}

function viewYou(){
  const u=S.profile;if(!u)return `<p class="lead">${esc(t('loading'))}</p>`;
  const card=(h,id)=>`<section class="card"><h3>${h}</h3>${SECTIONS[id](u)}</section>`;
  return `<h2>${t('profileH')}</h2>
<p class="lead">${t('profileLead')}</p>
<div class="card presets"><b>${t('startFrom')}</b>${presetButtons()}</div>
<div class="grid cols-2 mt">
${card(esc(t('nameLabel')),'name')}
${card(t('whoBuying'),'household')}
${card(t('privateSpace'),'space')}
${card(t('howLive'),'live')}
${card(t('shareWith'),'share')}
${card(t('whereHow'),'where')}
${card(t('needsH'),'needs')}
<section class="card"><h3>${esc(t('account'))}</h3>
<p>${esc(t('signedInAs',S.me.email||''))}</p>
<div class="presets"><button type="button" class="btn" data-action="logout">${esc(t('signOut'))}</button><button type="button" class="btn ghost danger" data-action="delete-account">${esc(t('deleteAccount'))}</button></div>
</section>
</div>`;
}

/* ---------- onboarding wizard: the same sections, one per step; matches unlock at the end ---------- */
const WIZ=[
  {id:'name',h:()=>t('wizNameH'),lead:()=>t('wizNameLead'),check:u=>u.name&&u.name.trim()?'':t('wizNeedName')},
  {id:'household',h:()=>t('whoBuying'),lead:()=>t('wizHouseholdLead'),examples:true},
  {id:'space',h:()=>t('privateSpace'),lead:()=>t('wizSpaceLead')},
  {id:'live',h:()=>t('howLive'),lead:()=>t('wizLiveLead')},
  {id:'share',h:()=>t('shareWith'),lead:()=>t('wizShareLead')},
  {id:'where',h:()=>t('whereHow'),lead:()=>t('wizWhereLead'),check:u=>u.areas.length&&u.types.length?'':t('err_profileIncomplete')},
  {id:'needs',h:()=>t('needsH'),lead:()=>t('wizNeedsLead')},
  {id:'done',h:()=>t('wizDoneH'),lead:()=>t('wizDoneLead')}
];
function viewWizard(){
  const u=S.profile;if(!u)return `<p class="lead">${esc(t('loading'))}</p>`;
  const st=WIZ[S.wiz],n=S.wiz+1,pct=Math.round(n/WIZ.length*100);
  let body;
  if(st.id==='done'){
    const facts=[hhLabel(u),t('bedroomsN',u.bedrooms),listOr(u.areas.map(areaLabel)),homeType(u.types),t('staying',horizonLabel(u.horizon))];
    body=`<section class="card"><h3>${esc(t('wizSummary'))}</h3><p><b>${esc(u.name)}</b></p><div class="facts">${facts.map(f=>`<span class="fact">${esc(cap(f))}</span>`).join('')}</div></section>`;
  }else body=`${st.examples?`<div class="card presets mb"><span>${esc(t('wizExamples'))}</span>${presetButtons()}</div>`:''}<section class="card">${SECTIONS[st.id](u)}</section>`;
  return `<div class="wizard">
<p class="hint" id="wiz-step">${esc(t('wizStep',n,WIZ.length))}</p>
<div class="progress" role="progressbar" aria-labelledby="wiz-step" aria-valuemin="1" aria-valuemax="${WIZ.length}" aria-valuenow="${n}"><span data-w="${pct}"></span></div>
<h2>${esc(st.h())}</h2>
<p class="lead">${esc(st.lead())}</p>
${body}
<p class="error" id="wiz-error" role="alert"></p>
</div>`;
}

function personCard(x){
  const p=x.person,s=x.score.total,cls=s>=80?'hi':(s>=65?'mid':'lo'),st=x.state,inDraft=S.draft.indexOf(p.id)>=0;
  const facts=[t('bedroomsN',p.bedrooms),p.privateBath?t('ownBathFact'):t('sharesBath'),p.cars?t('parkingN',p.cars):'',p.wfh?t('wfhFact'):'',t('staying',horizonLabel(p.horizon))].filter(Boolean);
  const sim=p.isDemo?` <span class="hint">${t('simReply')}</span>`:'';
  let act;
  if(inDraft)act=`<span class="status ok">${t('inDraft')}</span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="remove" data-id="${esc(p.id)}">${t('remove')}</button>`;
  else if(st==='passed')act=`<span class="status wait">${t('passedS')}</span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="undo" data-id="${esc(p.id)}">${t('undo')}</button>`;
  else if(st==='none')act=`<button type="button" class="btn small" data-action="pass" data-id="${esc(p.id)}">${t('pass')}</button><span class="spacer"></span><button type="button" class="btn small primary" data-action="like" data-id="${esc(p.id)}">${t('like')}</button>`;
  else if(st==='mutual')act=`<span class="status ok">${t('mutualS')}${sim}</span><span class="spacer"></span><button type="button" class="btn small primary" data-action="add" data-id="${esc(p.id)}">${t('addGroup')}</button>`;
  else if(st==='waiting')act=`<span class="status wait">${t('waitingS')}</span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="undo" data-id="${esc(p.id)}">${t('undo')}</button>`;
  else act=`<span class="status wait">${t('likedNo')}${sim}</span><span class="spacer"></span><button type="button" class="btn small ghost" data-action="undo" data-id="${esc(p.id)}">${t('undo')}</button>`;
  const tg=x.together,line=t('homeLine',homeType(tg.types),tg.beds),rest=t('bathsN',tg.baths)+(tg.cars?' · '+t('parkingN',tg.cars):'');
  const bio=p.bio?tx(p.bio):'';
  return `<article class="card pcard${st==='passed'?' passed':''}">
<div class="phead">${avatar(p.id,46,p.color,p.name)}<div><div class="pname">${esc(p.name)}${p.isDemo?` <span class="demo">${esc(t('demoTag'))}</span>`:''}</div><div class="psub">${esc(hhLabel(p))}</div></div><div class="ring ${cls}" data-p="${s}" role="img" aria-label="${esc(t('ringAria',s))}"><b>${s}%</b></div></div>
${bio?`<p class="bio">${esc(bio)}</p>`:''}
<div class="facts">${facts.map(f=>`<span class="fact">${esc(f)}</span>`).join('')}</div>
<div class="chips">${x.clicks.map(c=>`<span class="chip good">✓ ${esc(M(c))}</span>`).join('')}${x.talks.map(c=>`<span class="chip warn">⚑ ${esc(M(c))}</span>`).join('')}</div>
<div class="together">${t('together',esc(line),esc(rest))}</div>
<details class="why"><summary>${esc(t('why',s))}</summary><div class="bars">${Object.keys(WEIGHTS).map(k=>bar(tx(PART_LABELS[k]),x.score.parts[k])).join('')}</div><p class="hint">${esc(t('demoScoreHint'))}</p></details>
<div class="pactions">${act}</div>
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

function viewMatches(){
  const m=S.matches;if(!m)return `<p class="lead">${esc(t('loading'))}</p>`;
  const label=(g,i)=>(i===0?t('topSugg'):'')+(g.size===2?t('pairOf'):t('groupOf',g.size));
  const groupsBlock=m.suggestions.length?`<p class="hint">${esc(t('groupsHint'))}</p><div class="grid groups">${m.suggestions.map((g,i)=>groupCard(g,label(g,i))).join('')}</div>`
    :`<div class="card hint">${esc(m.mutualCount?t('noGroupYet'):t('groupsAppear'))}</div>`;
  return `<h2>${t('matchesH')}</h2>
<p class="lead">${esc(t('matchesLead2',m.compatible.length))}</p>
<details class="card mb"><summary><b>${t('howScore')}</b></summary><p class="hint">${esc(t('howScoreText'))}</p></details>
<section class="mb"><h3>${t('groupsH')}</h3>${groupsBlock}</section>
<h3>${t('everyoneH')}</h3>
<div class="grid people">${m.compatible.length?m.compatible.map(personCard).join(''):`<div class="card empty">${esc(t('nobody'))}</div>`}</div>
${m.hidden.length?`<details class="card mt"><summary><b>${esc(t('hiddenN',m.hidden.length))}</b> <span class="hint">${t('seeWhy')}</span></summary><ul class="clean mt">${m.hidden.map(h=>`<li>${avatar(h.person.id,32,h.person.color,h.person.name)}<div><b>${esc(h.person.name)}</b> <span class="hint">· ${esc(hhLabel(h.person))}</span><div class="who">${esc(h.reasons.map(M).join(' · '))}</div></div></li>`).join('')}</ul></details>`:''}`;
}

function memberStatus(st){return st&&st!=='preview'?t('ms_'+st):'';}

function viewGroups(){
  if(S.home)return viewHome(S.home);
  const gs=S.groups;if(!gs)return `<p class="lead">${esc(t('loading'))}</p>`;
  if(!gs.length)return `<div class="card empty"><h2>${t('groupsListH')}</h2><p class="lead">${esc(t('noGroups'))}</p><button type="button" class="btn primary" data-action="goto" data-id="matches">${t('goMatches')}</button></div>`;
  return `<h2>${t('groupsListH')}</h2>
<div class="grid groups mt">${gs.map(g=>{
    const ids=g.members.map(m=>m.id);
    const invite=g.myStatus==='pending';
    return `<article class="card gcard"><div class="hint"><b>${esc(t('st_'+g.state))}</b></div>
<div class="stack">${ids.map(id=>avatar(id,38)).join('')}</div>
<div><b>${esc(ids.map(dn).join(' + '))}</b></div>
<ul class="clean">${g.members.filter(m=>!isMe(m.id)).map(m=>`<li class="who">${esc(dn(m.id))}: ${esc(memberStatus(m.status))}</li>`).join('')}</ul>
${invite?`<div class="notice card">${esc(t('invitedBy',dn(g.createdBy)))}</div>`:''}
<div class="pactions">${invite?`<button type="button" class="btn small" data-action="respond" data-id="${g.id}" data-accept="0">${t('decline')}</button><span class="spacer"></span><button type="button" class="btn small primary" data-action="respond" data-id="${g.id}" data-accept="1">${t('accept')}</button>`:`<span class="spacer"></span><button type="button" class="btn small primary" data-action="open-group" data-id="${g.id}">${t('openGroup')}</button>`}</div></article>`;
  }).join('')}</div>`;
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
  const chips=`<div class="card members">${ids.map(id=>`<span class="member">${avatar(id,32)}<span><b>${esc(dn(id))}</b><br><span class="hint">${esc(hhLabel(PEOPLE[id]))}${!isPreview&&!isMe(id)?' · '+esc(memberStatus(statusOf(id))):''}</span></span></span>`).join('')}</div>`;
  const back=isPreview?'':`<p><button type="button" class="btn small ghost" data-action="all-groups">← ${t('allGroups')}</button></p>`;
  const stateLine=isPreview?'':`<p class="status ${h.state==='formed'?'ok':'wait'}">${esc(t('st_'+h.state))}</p>`;
  const respond=!isPreview&&myStatus==='pending'?`<div class="card notice mt">${esc(t('invitedBy',dn(h.createdBy)))}<div class="row"><button type="button" class="btn small" data-action="respond" data-id="${h.id}" data-accept="0">${t('decline')}</button><button type="button" class="btn small primary" data-action="respond" data-id="${h.id}" data-accept="1">${t('accept')}</button></div></div>`:'';
  if(!h.check.ok)return `${back}<h2>${t('homeH')}</h2>${stateLine}
<p class="lead">${esc(t('attentionLead'))}</p>
${chips}${respond}
<div class="card warnbox mt"><b>${esc(t('doesntWork'))}</b> ${esc(h.check.issues.map(M).join(' · '))}</div>`;
  const gs=h.score,hb=h.brief;
  const musts=hb.feats.filter(x=>x.level==='must'),nices=hb.feats.filter(x=>x.level==='nice');
  const featLi=x=>`<li><span class="tag ${x.level}">${x.level==='must'?t('tagMust'):t('tagNice')}</span><div>${esc(featLabel(x.id))}<div class="who">${esc(x.level==='must'?t('mustFor',names(x.must),x.nice.length?names(x.nice):''):t('niceFor',names(x.nice)))}</div></div></li>`;
  const where=hb.areas.length?listOr(hb.areas.map(areaLabel)):t('noAreaAll');
  const line=t('homeLine',homeType(hb.types),hb.beds);
  const headline=t('headline',line,t('bathsN',hb.baths),hb.cars?t('parkingN',hb.cars):'');
  const banner=isPreview?`<div class="card notice mt"><b>${t('previewOnly')}</b> ${esc(t('previewNote'))}<div class="row"><button type="button" class="btn small primary" data-action="propose" data-id="${esc(ids.filter(id=>!isMe(id)).join(','))}">${t('propose')}</button><button type="button" class="btn small ghost" data-action="close-preview">${t('closePreview')}</button></div></div>`:'';
  const canSize=!isPreview&&myStatus==='accepted';
  const sizeEdit=canSize?`<div class="field mt"><span class="label" id="size-label">${esc(t('sizeLabel'))}</span><div class="range-edit" role="group" aria-labelledby="size-label"><label>${t('from')} <input type="number" min="20" max="900" step="5" data-size="min" value="${h.size.min}"></label><label>${t('to')} <input type="number" min="20" max="900" step="5" data-size="max" value="${h.size.max}"></label></div></div>`:'';
  const lead=isPreview?t('leadPreview'):(h.state==='formed'?t('leadGroup'):t('leadPending'));
  const w=gs.weakest;
  const leave=!isPreview&&myStatus!=='pending'?`<p class="mt"><button type="button" class="btn small ghost danger" data-action="leave" data-id="${h.id}">${t('leave')}</button></p>`:'';
  return `${back}<h2>${t('homeH')}</h2>${stateLine}
<p class="lead">${esc(lead)} ${esc(t('leadScope'))}</p>
${chips}${banner}${respond}
<div class="grid cols-2 mt">
<section class="card"><h3>${t('fitH')}</h3>
<div class="kpis">${kp(gs.min+'%',(ids.length>2?t('weakestK'):t('compatK'))+esc(dn(w.a))+' + '+esc(dn(w.b)))}${ids.length>2?kp(gs.avg+'%',esc(t('avgOf',gs.pairs.length))):''}</div>
${ids.length>2?matrix(ids,gs.pairs):''}
<h4>${ids.length>2?t('weakDetail'):t('breakdown')}</h4>
<div class="bars">${Object.keys(WEIGHTS).map(k=>bar(tx(PART_LABELS[k]),gs.weakestParts[k])).join('')}</div>
<p class="hint mt">${esc(t('demoScores'))}</p>
</section>
<section class="card"><h3>${t('homeNeedH')}</h3>
<p class="headline">${esc(cap(headline))}</p>
<div class="kpis">${kp(hb.beds,t('kBeds'))}${kp(hb.baths,t('kBaths'))}${kp(hb.cars||t('kNone'),t('kParking'))}${kp(h.size.min+'–'+h.size.max+' m²',t('kArea'))}${kp(hb.adults+(hb.kids?' + '+hb.kids:''),hb.kids?t('kAdultsKids'):t('kAdults'))}</div>
${sizeEdit}
<p class="hint">${esc(t('areaHint'))}</p>
<p class="mt"><b>${t('whereB')}</b> ${esc(where)}</p>
${musts.length?`<h4>${t('mustsH')}</h4><ul class="clean">${musts.map(featLi).join('')}</ul>`:''}
${nices.length?`<h4>${t('nicesH')}</h4><ul class="clean">${nices.map(featLi).join('')}</ul>`:''}
${hb.extras.length?`<h4>${t('worthH')}</h4><ul class="clean">${hb.extras.map(x=>`<li><span class="tag tip">${t('tipTag')}</span><div>${esc(M(x.label))}<div class="who">${esc(M(x.why))}</div></div></li>`).join('')}</ul>`:''}
</section>
<section class="card"><h3>${t('roomsH')}</h3>
<div class="split" aria-hidden="true">${h.rooms.map(r=>`<span data-w="${r.pct}" data-bg="${esc((PEOPLE[r.id]||{}).color||'#495057')}"></span>`).join('')}</div>
<ul class="clean">${h.rooms.map(r=>`<li>${avatar(r.id,28)}<div><b>${esc(dn(r.id))}</b>: ${esc(t('roomsOf',r.rooms,hb.beds))}${r.bath?esc(t('plusOwnBath')):''}${h.cars[r.id]?`<div class="who">${esc(t('parkingSpaces',h.cars[r.id]))}</div>`:''}</div></li>`).join('')}</ul>
<h4>${t('placementH')}</h4>
<ul class="clean">${h.tips.map(x=>`<li><span class="tag tip">${t('tipTag')}</span><div>${esc(M(x))}</div></li>`).join('')}</ul>
<p class="hint mt">${esc(t('illustrative'))}</p>
</section>
<section class="card"><h3>${t('agreeH')}</h3>
<p class="hint">${esc(t('agreeHint'))}</p>
<ul class="clean mt">${h.topics.map(x=>{const a=t(x.key);return `<li><span class="tag tip">${esc(a[0])}</span><div>${esc(a[1])}<div class="who">${esc(x.why?M(x.why):a[2])}</div></div></li>`;}).join('')}</ul>
</section>
</div>${leave}`;
}

/* ---------- render ---------- */
function renderChrome(){
  document.documentElement.setAttribute('lang',LANG);
  document.title=t('title');
  const set=(id,txt)=>{const el=document.getElementById(id);if(el)el.textContent=txt;};
  set('brandname',BRAND);set('brandmark',BRAND.charAt(0));
  set('tagline',t('tagline').replace(/ \((working name|nombre provisional)\)/,''));set('badge',t('badge'));
  const foot=document.getElementById('footnote');
  if(foot)foot.innerHTML=`<p>${esc(t('footnote'))}</p><p class="links"><a href="#terms">${esc(t('footTerms'))}</a> · <a href="#privacy">${esc(t('footPrivacy'))}</a></p>`;
  const nav=document.getElementById('steps');if(nav){nav.setAttribute('aria-label',t('steps'));nav.hidden=!inApp();}
  const sw=document.getElementById('lang-switch');
  if(sw){sw.textContent=t('langOther');sw.setAttribute('lang',t('langOtherCode'));sw.setAttribute('data-id',t('langOtherCode'));sw.setAttribute('aria-label',t('langOtherAria'));}
}
/* The tabs show once someone is signed in and onboarded, and not on a legal page */
function inApp(){return !!(S.me&&S.me.onboarded&&!S.view);}
function renderTabs(){
  if(!inApp()){tabsEl.innerHTML='';app.setAttribute('role','main');app.removeAttribute('aria-labelledby');return;}
  app.setAttribute('role','tabpanel');
  const n=S.matches?S.matches.compatible.length:0,g=S.groups?S.groups.length:0;
  const tabs=[['you',t('tabYou'),'',''],['matches',t('tabMatches'),n?String(n):'',t('srCompatible')],['groups',t('tabHome'),g?String(g):'',t('srInGroup')]];
  tabsEl.innerHTML=tabs.map(x=>{const sel=S.tab===x[0];return `<button type="button" class="tab" role="tab" id="tab-${x[0]}" aria-controls="app" aria-selected="${sel}" tabindex="${sel?0:-1}" data-tab="${x[0]}">${x[1]}${x[2]?`<span class="count"><span aria-hidden="true">${x[2]}</span><span class="sr">: ${x[2]} ${x[3]}</span></span>`:''}</button>`;}).join('');
  app.setAttribute('aria-labelledby','tab-'+S.tab);
}
function renderBar(){
  const bar=document.getElementById('actionbar');
  if(!S.me||S.view){bar.hidden=true;return;}
  bar.hidden=false;
  let h;
  if(!S.me.onboarded){
    const last=S.wiz===WIZ.length-1,save=S.save?`<span class="hint" role="status">${esc(t(S.save))}</span>`:'';
    barEl.innerHTML=`<button type="button" class="btn" data-action="wiz-back"${S.wiz===0?' disabled':''}>${esc(t('wizBack'))}</button>${save}<span class="spacer"></span><button type="button" class="btn primary" data-action="${last?'wiz-finish':'wiz-next'}">${esc(last?t('wizFinish'):t('wizNext'))}</button>`;
    return;
  }
  const m=S.matches;
  if(S.tab==='you'){
    const save=S.save?`<span class="hint" role="status">${esc(t(S.save))}</span>`:'';
    h=`<div class="stat">${m?t('barYou',m.compatible.length,m.hidden.length):''} ${save}</div><span class="spacer"></span><button type="button" class="btn primary" data-action="goto" data-id="matches">${t('seeMatches')}</button>`;
  }else if(S.tab==='matches'){
    if(S.draft.length)h=`<div class="stat">${esc(t('draftBar',1+S.draft.length))}</div><span class="spacer"></span><button type="button" class="btn ghost" data-action="clear-draft">${t('clearDraft')}</button><button type="button" class="btn" data-action="preview" data-id="${esc(S.draft.join(','))}">${t('previewDraft')}</button><button type="button" class="btn primary" data-action="propose" data-id="${esc(S.draft.join(','))}">${t('propose')}</button>`;
    else h=`<div class="stat">${m?t('barGroup',1,m.likedCount):''}</div><span class="spacer"></span><button type="button" class="btn primary" data-action="goto" data-id="groups">${t('seeHome')}</button>`;
  }else h=`<button type="button" class="btn" data-action="goto" data-id="you">${t('editProfile')}</button><span class="spacer"></span><button type="button" class="btn primary" data-action="goto" data-id="matches">${t('backMatches')}</button>`;
  barEl.innerHTML=h;
}
function render(){
  renderChrome();
  renderTabs();
  app.innerHTML=S.view?viewLegal(S.view):(!S.me?viewAuth():(!S.me.onboarded?viewWizard():(S.tab==='you'?viewYou():(S.tab==='matches'?viewMatches():viewGroups()))));
  applyStyles(app);
  renderBar();
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
}
async function go(tab){
  S.tab=tab;if(tab!=='groups')S.home=null;
  render();window.scrollTo(0,0);focusHeading();
  if(tab==='matches'||tab==='groups')await refresh();
}
/* After signing in or up: load the profile, then either the wizard or the app */
async function enter(me){
  S.me=me;S.authError='';
  PEOPLE[S.me.id]={name:S.me.name,color:'#1b2230'};
  try{S.profile=await api('GET','/api/profile');}catch(e){toast(errText(e));}
  if(S.me.onboarded){S.tab=S.tab||'matches';render();focusHeading();refresh();}
  else{S.wiz=0;render();focusHeading();}
}
async function start(){
  try{S.config=await api('GET','/api/config');}catch(e){S.config={auth:'local',invite:true,terms:''};}
  readHash();
  let me=null;try{me=await api('GET','/api/me');}catch(e){}
  if(me){S.tab='matches';await enter(me);}else render();
}
/* #terms, #privacy, #authError=code (back from External ID), #lang=en */
function readHash(){
  const h=new URLSearchParams((location.hash||'').replace(/^#/,''));
  const prev=S.view;
  S.view=h.has('terms')?'terms':(h.has('privacy')?'privacy':null);
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
      S.save='saved';S.matches=null;
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
  if(step>0){const msg=WIZ[S.wiz].check?WIZ[S.wiz].check(S.profile):'';if(msg){if(err)err.textContent=msg;return;}}
  if(!(await flushSave()))return;
  S.wiz=Math.max(0,Math.min(WIZ.length-1,S.wiz+step));
  render();window.scrollTo(0,0);focusHeading();
}
async function wizFinish(){
  if(!(await flushSave()))return;
  try{S.me=await api('POST','/api/me/onboarded');S.tab='matches';render();window.scrollTo(0,0);focusHeading();refresh();}
  catch(e){const err=document.getElementById('wiz-error');if(err)err.textContent=errText(e);}
}

/* ---------- actions ---------- */
async function react(id,kind){
  try{
    const r=await api('PUT','/api/reactions/'+encodeURIComponent(id),{kind:kind});
    if(kind==='like'){const n=personName(id);toast(r.mutual?(r.isDemo?t('tMutual',n):t('tMutualReal',n)):(r.isDemo?t('tNoMatch',n):t('tWaiting',n)));}
    if(kind!=='like')S.draft=S.draft.filter(x=>x!==id);
    await loadMatches();rerender();
  }catch(e){toast(errText(e));}
}
async function addToDraft(id){
  if(S.draft.indexOf(id)>=0){toast(t('tAlready',personName(id)));return;}
  try{await api('POST','/api/preview',{ids:S.draft.concat([id])});S.draft.push(id);rerender();toast(t('tAdded',personName(id)));}
  catch(e){toast(t('tCantAdd',personName(id),errText(e)));}
}
async function preview(ids){
  try{const h=await api('POST','/api/preview',{ids:ids});remember(h.people);S.home=h;S.tab='groups';render();window.scrollTo(0,0);focusHeading();toast(t('tPreview'));}
  catch(e){toast(t('tCantForm',errText(e)));}
}
async function propose(ids){
  try{const h=await api('POST','/api/groups',{ids:ids});remember(h.people);S.home=h;S.draft=[];S.tab='groups';await loadGroups();render();window.scrollTo(0,0);focusHeading();toast(t('tProposed'));}
  catch(e){toast(t('tCantForm',errText(e)));}
}
async function openGroup(id){
  try{const h=await api('GET','/api/groups/'+id);remember(h.people);S.home=h;render();window.scrollTo(0,0);focusHeading();}
  catch(e){toast(errText(e));}
}
async function respond(id,accept){
  try{const h=await api('POST','/api/groups/'+id+'/respond',{accept:accept});remember(h.people);S.home=h;await loadGroups();render();toast(accept?t('tAccepted'):t('tDeclined'));}
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
    S.busy=false;S.tab=up?'you':'matches';
    await enter(me);
  }catch(err){S.busy=false;btn.disabled=false;errEl.textContent=errText(err);}
});
app.addEventListener('change',e=>{
  const el=e.target;
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
  else if(el.hasAttribute('data-multi'))u[f]=Array.prototype.map.call(app.querySelectorAll('input[data-field="'+f+'"]:checked'),x=>x.value);
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
  profileChanged();
});
app.addEventListener('input',e=>{const el=e.target;if(el.id==='a-pass'&&S.authMode==='signup'){updateMeter(el.value);return;}if(el.type==='range'&&S.profile){S.profile[el.getAttribute('data-field')]=parseInt(el.value,10);profileChanged();}});
document.addEventListener('click',async e=>{
  const el=e.target.closest('[data-action],[data-tab]');if(!el||el.disabled)return;
  if(el.hasAttribute('data-tab')){go(el.getAttribute('data-tab'));return;}
  const id=el.getAttribute('data-id');
  const ids=()=>(id||'').split(',').filter(Boolean);
  switch(el.getAttribute('data-action')){
    case 'lang':switchLang(id);break;
    case 'auth-mode':S.authMode=id;S.authError='';render();break;
    case 'ext-signin':location.href='/auth/external?lang='+LANG;break;
    case 'pw-toggle':{const inp=document.getElementById('a-pass'),show=inp.type==='password';inp.type=show?'text':'password';el.setAttribute('aria-pressed',String(show));el.textContent=show?t('hidePw'):t('showPw');inp.focus();break;}
    case 'wiz-next':wizMove(1);break;
    case 'wiz-back':wizMove(-1);break;
    case 'wiz-finish':wizFinish();break;
    case 'legal-back':S.view=null;try{history.replaceState(null,'',location.pathname);}catch(err){}render();window.scrollTo(0,0);focusHeading();break;
    case 'goto':go(id);break;
    case 'preset':Object.assign(S.profile,JSON.parse(JSON.stringify(PRESETS[id].v)));rerender();profileChanged();toast(t('tExample',tx(PRESETS[id].label)));break;
    case 'feat':cycleFeature(el,id);break;
    case 'like':react(id,'like');break;
    case 'pass':react(id,'pass');break;
    case 'undo':react(id,'none');break;
    case 'add':addToDraft(id);break;
    case 'remove':S.draft=S.draft.filter(x=>x!==id);rerender();toast(t('tRemoved'));break;
    case 'clear-draft':S.draft=[];rerender();break;
    case 'preview':preview(ids());break;
    case 'propose':propose(ids());break;
    case 'close-preview':S.home=null;go('matches');break;
    case 'open-group':openGroup(id);break;
    case 'all-groups':S.home=null;go('groups');break;
    case 'respond':respond(id,el.getAttribute('data-accept')==='1');break;
    case 'leave':leave(id);break;
    case 'logout':{let r=null;try{r=await api('POST','/api/auth/logout');}catch(err){}if(r&&r.next){location.href=r.next;break;}S.me=null;S.profile=null;S.matches=null;S.groups=null;S.home=null;S.draft=[];S.authMode='signin';render();break;}
    case 'delete-account':if(window.confirm(t('deleteConfirm'))){try{await api('DELETE','/api/me');S.me=null;S.matches=null;S.groups=null;S.home=null;S.draft=[];render();}catch(err){toast(errText(err));}}break;
  }
});
/* Arrow keys move between tabs, as in the prototype */
tabsEl.addEventListener('keydown',e=>{
  const keys=['you','matches','groups'],i=keys.indexOf(S.tab);
  let n=-1;if(e.key==='ArrowRight')n=(i+1)%3;else if(e.key==='ArrowLeft')n=(i+2)%3;else if(e.key==='Home')n=0;else if(e.key==='End')n=2;
  if(n>=0){e.preventDefault();go(keys[n]).then(()=>{const b=document.getElementById('tab-'+keys[n]);if(b)b.focus();});}
});

/* ---------- start: Spanish unless the viewer chose English; #lang= overrides both ---------- */
let saved=null;try{saved=localStorage.getItem(LANG_KEY);}catch(e){}
setLang(saved==='en'?'en':'es');
try{const h=new URLSearchParams((location.hash||'').replace(/^#/,''));const lg=h.get('lang');if(lg==='en'||lg==='es')setLang(lg);}catch(e){}
window.addEventListener('hashchange',()=>{if(readHash()){render();window.scrollTo(0,0);focusHeading();}});
window.__compicasa={S:S,M:M,setLang:setLang,render:render,api:api};
if('serviceWorker' in navigator)navigator.serviceWorker.register('/sw.js').catch(()=>{});
start();
})();
