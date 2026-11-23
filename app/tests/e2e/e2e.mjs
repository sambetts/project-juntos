// End-to-end check of the PoC in a real browser (headless Microsoft Edge or Chrome), with no npm packages:
// it drives the browser through the DevTools protocol using Node's built-in WebSocket.
// Usage: node app/tests/e2e/e2e.mjs <base-url> <invite-code> [screenshot-dir]
// Runs at phone size (390 x 844) and desktop size, in Spanish and English. Exit code 1 if any check fails.
// With local accounts (a developer's machine) it runs the whole journey: sign-up, onboarding wizard, matches, groups.
// With Entra External ID (the live site, D-033) it checks the sign-up checks and the hand-over to External ID,
// because signing up there needs a real inbox; the rest of the journey is covered by the local run.
import { spawn } from 'node:child_process';
import { mkdtempSync, writeFileSync, existsSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const [base, invite, shots] = process.argv.slice(2);
if (!base || !invite) { console.error('usage: node e2e.mjs <base-url> <invite-code> [screenshot-dir]'); process.exit(2); }
const browserPath = [
  process.env.BROWSER,
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  'C:/Program Files/Microsoft/Edge/Application/msedge.exe',
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  '/usr/bin/microsoft-edge', '/usr/bin/google-chrome', '/usr/bin/chromium',
].find(p => p && existsSync(p));
if (!browserPath) { console.error('No Edge or Chrome found; set BROWSER'); process.exit(2); }

let pass = 0, fail = 0;
const ok = (cond, label) => { if (cond) pass++; else { fail++; console.log('FAIL', label); } };
const sleep = ms => new Promise(r => setTimeout(r, ms));

const profile = mkdtempSync(join(tmpdir(), 'compicasa-e2e-'));
const port = 9400 + Math.floor(Math.random() * 400);
const proc = spawn(browserPath, ['--headless=new', `--remote-debugging-port=${port}`, `--user-data-dir=${profile}`, '--no-first-run', '--disable-extensions', 'about:blank'], { stdio: 'ignore' });

async function target() {
  for (let i = 0; i < 50; i++) {
    try { const list = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json(); const p = list.find(x => x.type === 'page'); if (p) return p.webSocketDebuggerUrl; } catch { }
    await sleep(200);
  }
  throw new Error('browser did not start');
}

const ws = new WebSocket(await target());
await new Promise(r => ws.addEventListener('open', r, { once: true }));
let seq = 0; const waiting = new Map(); const errors = [];
ws.addEventListener('message', ev => {
  const m = JSON.parse(ev.data);
  if (m.id && waiting.has(m.id)) { waiting.get(m.id)(m); waiting.delete(m.id); }
  if (m.method === 'Runtime.exceptionThrown') errors.push(m.params.exceptionDetails.text + ' ' + (m.params.exceptionDetails.exception?.description || ''));
  if (m.method === 'Log.entryAdded' && m.params.entry.level === 'error' && !/favicon|status of 40[013]/.test(m.params.entry.text + m.params.entry.url)) errors.push(m.params.entry.text);
});
const send = (method, params = {}) => new Promise(r => { const id = ++seq; waiting.set(id, r); ws.send(JSON.stringify({ id, method, params })); });
const js = async expr => { const r = await send('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true }); if (r.result?.exceptionDetails) throw new Error(expr + ': ' + r.result.exceptionDetails.text); return r.result?.result?.value; };
const until = async (expr, label, ms = 15000) => { const t0 = Date.now(); while (Date.now() - t0 < ms) { if (await js(expr)) return true; await sleep(150); } ok(false, 'timed out: ' + label); return false; };
const click = sel => js(`(()=>{const el=document.querySelector(${JSON.stringify(sel)});if(!el)return false;el.click();return true;})()`);
const text = () => js('document.body.innerText');
async function shot(name) {
  if (!shots) return;
  const r = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
  writeFileSync(join(shots, name + '.png'), Buffer.from(r.result.data, 'base64'));
}
// Homes for sale that fit the group (D-035): each names its agency and links to the agency's own advert.
async function homesCheck(tag) {
  // Homes have their own view on a group's page (B-051)
  await until(`!!document.querySelector('.subtabs [data-gsub="homes"]')`, tag + ': homes tab shows');
  await click('.subtabs [data-gsub="homes"]');
  await until(`/-homes$/.test(location.hash)`, tag + ': homes view has its own address');
  if (!await until(`document.querySelectorAll('#homes .home').length>0`, tag + ': homes that fit load')) return;
  ok(await js(`[...document.querySelectorAll('#homes .home')].every(h=>{const a=h.querySelector('a.btn');return a?(a.target==='_blank'&&/noopener/.test(a.rel)&&/^https:/.test(a.href)&&/Ver el anuncio en /.test(a.textContent)):/inventada/.test(h.innerText);})`),
    tag + ': every home links to the agency advert in a new tab, or is marked made-up');
  await js(`document.getElementById('homes').scrollIntoView()`);
  await shot(tag + '-5b-homes-es');
  await js('window.scrollTo(0,0)');
}
async function size(w, h, mobile) { await send('Emulation.setDeviceMetricsOverride', { width: w, height: h, deviceScaleFactor: mobile ? 2 : 1, mobile }); }

await send('Runtime.enable'); await send('Log.enable'); await send('Page.enable');

// Words that must not appear on a screen in the other language (a leak check, as in the prototype tests).
const EN_ONLY = /\b(Sign in|Your profile|Matches|Like|Pass|households|Proof of concept)\b/;
const ES_ONLY = /\b(Entrar|Tu perfil|Personas afines|Me interesa|Pasar|hogares|Prueba de concepto)\b/;

const user = 'e2e.' + Date.now().toString(36) + '@example.com';
const config = await (await fetch(base + '/api/config')).json();
const external = config.auth === 'external';
const fill = (id, v) => js(`(()=>{const el=document.getElementById(${JSON.stringify(id)});el.value=${JSON.stringify(v)};el.dispatchEvent(new Event('input',{bubbles:true}));return true;})()`);
const tick = id => js(`(()=>{const el=document.getElementById(${JSON.stringify(id)});el.checked=true;return true;})()`);
const authError = () => js(`document.getElementById('auth-error')?.textContent||''`);
async function submitAndExpect(re, label) {
  await js(`document.getElementById('auth-error').textContent=''`);
  await click('#auth-form button[type=submit]');
  await until(`document.getElementById('auth-error')?.textContent.length>0`, label);
  ok(re.test(await authError()), label + ' (in Spanish): ' + await authError());
}
for (const [w, h, mobile, tag] of [[390, 844, true, 'phone'], [1280, 900, false, 'desktop']]) {
  await size(w, h, mobile);
  await send('Network.clearBrowserCookies');
  await send('Page.navigate', { url: base + '/' });
  await until(`!!document.querySelector('[data-action="auth-mode"]')`, tag + ' sign-in screen');
  ok(await js('document.documentElement.lang') === 'es', tag + ': opens in Spanish');
  ok(!EN_ONLY.test(await text()), tag + ': no English on the Spanish sign-in screen');
  ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), tag + ': no sideways scrolling on sign-in');
  await shot(tag + '-1-signin-es');

  // Sign up (phone) or sign in (desktop, same account).
  const signup = tag === 'phone';
  if (signup) {
    await click('[data-action="auth-mode"][data-id="signup"]');
    await until(`!!document.querySelector('#a-terms')`, 'sign-up form');
    if (!external) {
      await fill('a-email', user); await fill('a-pass', 'e2e long password 2026'); await fill('a-name', 'Prueba');
      ok(await js(`+document.getElementById('pw-meter').getAttribute('data-level')`) >= 2, 'password meter rates a long phrase');
    }
    if (config.invite) {
      await fill('a-invite', 'wrong-code');
      await submitAndExpect(/invitación/, 'wrong invite shows an error');
      await fill('a-invite', invite);
    }
    await submitAndExpect(/18 años/, 'the 18+ box is required');
    await tick('a-adult');
    await submitAndExpect(/condiciones de uso/, 'the terms box is required');
    await tick('a-terms');
    if (external) ok(/Crear cuenta/.test(await js(`document.getElementById('ext-tip')?.textContent||''`)), 'sign-up explains External ID’s “Create account” page');
    await shot(tag + '-1b-signup-es');
    await click('#auth-form button[type=submit]');
  } else if (external) {
    await click('[data-action="ext-signin"]');
  } else {
    await fill('a-email', user); await fill('a-pass', 'e2e long password 2026');
    await click('#auth-form button[type=submit]');
  }

  if (external) {
    // The hand-over: the browser should now be at Entra External ID's sign-in page.
    await until(`/ciamlogin\.com$/.test(location.hostname)`, tag + ' hands over to Entra External ID', 30000);
    await shot(tag + '-2-external-id');
    continue;
  }

  if (signup) {
    // The onboarding wizard: eight steps, matches locked until the end.
    await until(`!!document.querySelector('.wizard')`, 'onboarding wizard starts');
    ok(await js(`!document.querySelector('#tabs [data-tab]')`), 'no tabs until onboarding is done');
    ok(/Paso 1 de 8/.test(await text()), 'wizard shows its progress');
    ok(await js(`document.getElementById('f-name').value`) === 'Prueba', 'wizard starts from the name given at sign-up');
    await shot(tag + '-2-wizard-1-es');
    await click('[data-action="wiz-next"]');
    await until(`/Paso 2 de 8/.test(document.body.innerText)`, 'wizard step 2');
    await click('[data-action="preset"][data-id="room"]');
    await until(`document.querySelector('#actionbar')?.innerText.includes('Guardado')`, 'example saved');
    ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), 'no sideways scrolling in the wizard');
    for (let n = 3; n <= 8; n++) {
      await click('[data-action="wiz-next"]');
      await until(`/Paso ${n} de 8/.test(document.body.innerText)`, 'wizard step ' + n);
    }
    await shot(tag + '-2-wizard-8-es');
    await click('[data-action="wiz-finish"]');
    await until(`!!document.querySelector('#tabs [data-tab]')`, tag + ' onboarding finished');
  } else {
    await until(`!!document.querySelector('#tabs [data-tab]')`, tag + ' signed in');
  }

  // Matches
  await click('[data-tab="matches"]');
  await until(`document.querySelectorAll('.pcard').length>0`, tag + ' matches load');
  const cards = await js(`document.querySelectorAll('.pcard').length`);
  ok(cards >= 10, `${tag}: at least 10 compatible people (${cards})`);
  ok(await js(`document.querySelectorAll('.demo').length`) > 0, tag + ': made-up people are labelled');
  ok(!EN_ONLY.test(await text()), tag + ': no English on the Spanish matches screen');
  ok(await js(`[...document.querySelectorAll('.avatar')].every(a=>a.style.background!=='')`), tag + ': avatar colours applied despite the strict CSP');
  ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), tag + ': no sideways scrolling on matches');
  // B-051: one navigation, no permanent bottom bar, and each view has an address
  ok(await js(`document.getElementById('actionbar').hidden`), tag + ': no bottom action bar on People without a group draft');
  ok(await js(`/^#people-/.test(location.hash)`), tag + ': People has an address');
  ok(await js(`document.querySelectorAll('.subtabs .stab').length===3`), tag + ': People has three views (to review, liked, matches)');
  await click('.pcard .more');
  await until(`!!document.querySelector('dialog.sheet[open]') && /^#person-/.test(location.hash)`, tag + ': More opens the person sheet with its own address');
  await click('[data-action="close-sheet"]');
  await until(`!document.querySelector('dialog.sheet[open]') && /^#people-/.test(location.hash)`, tag + ': closing the sheet goes back to People');
  await shot(tag + '-3-matches-es');

  if (signup) {
    // Like everyone, then propose the top suggested group.
    const likes = await js(`document.querySelectorAll('[data-action="like"]').length`);
    for (let i = 0; i < likes; i++) {
      await click('[data-action="like"]');
      await sleep(250);
    }
    // Suggested groups live under Groups (B-051)
    await click('[data-tab="groups"]');
    await until(`document.querySelectorAll('[data-action="propose"]').length>0`, 'a group is suggested');
    await click('.gcard [data-action="preview"]');
    await until(`!!document.querySelector('.kpis') && document.body.innerText.includes('Solo vista previa')`, 'preview shows the brief');
    await shot(tag + '-4-preview-es');
    await click('.notice [data-action="propose"]');
    await until(`document.body.innerText.includes('Formado')`, 'group formed');
    ok(await js(`document.querySelectorAll('.matrix, .kpis').length`) > 0, 'formed group shows how you fit');
    ok(await js(`/^#group-[0-9a-f-]{36}$/.test(location.hash)`), 'a group has its own address');
    await click('.subtabs [data-gsub="brief"]');
    await until(`!!document.querySelector('.headline') && /-brief$/.test(location.hash)`, 'the home brief is its own view');
    await js('history.back()');
    await until(`/^#group-[0-9a-f-]{36}$/.test(location.hash) && !document.querySelector('.headline')`, 'Back returns to the group overview');
    await shot(tag + '-5-group-es');
    await homesCheck(tag);
  } else {
    await click('[data-tab="groups"]');
    await until(`document.querySelectorAll('[data-action="open-group"]').length>0`, 'desktop sees the group from the phone');
    await click('[data-action="open-group"]');
    await until(`!!document.querySelector('.kpis')`, 'group opens');
    await shot(tag + '-5-group-es');
    await homesCheck(tag);
  }

  // Switch to English and check the same screen.
  await click('#lang-switch');
  await until(`document.documentElement.lang==='en'`, tag + ' switches to English');
  ok(!ES_ONLY.test(await text()), tag + ': no Spanish UI words on the English screen');
  await shot(tag + '-6-group-en');
  await click('#lang-switch');
}

// The draft legal pages open from the footer, signed in or not.
await send('Page.navigate', { url: base + '/#privacy' });
await until(`!!document.querySelector('.legal')`, 'privacy notice opens');
ok(/Borrador/.test(await text()) && /aepd\.es/.test(await text()), 'privacy notice is a marked draft, in Spanish');
await click('[data-action="legal-back"]');
await until(`!document.querySelector('.legal')`, 'back from the privacy notice');

// The guide opens from the header, signed in or not (B-050).
await send('Page.navigate', { url: base + '/#help' });
await until(`!!document.querySelector('.help')`, 'guide opens');
ok(/Cómo funciona/.test(await text()) && /Me interesa/.test(await text()) && /Rechazar/.test(await text()), 'guide explains likes and invitations, in Spanish');
await click('[data-action="legal-back"]');
await until(`!document.querySelector('.help')`, 'back from the guide');

// Install basics for phones: manifest, icons and service worker.
const man = await (await fetch(base + '/manifest.webmanifest')).json();
ok(man.display === 'standalone' && man.icons.some(i => i.sizes === '512x512'), 'installable manifest');
ok((await fetch(base + '/icons/apple-touch-icon.png')).ok, 'iOS icon served');
ok(await js(`navigator.serviceWorker.getRegistration().then(r=>!!r)`), 'service worker registered');

// Tidy up: delete the test account through the API, as a user would.
if (!external) await js(`window.__compicasa.api('DELETE','/api/me').then(()=>true)`);

ok(errors.length === 0, 'no script errors: ' + errors.join(' | '));
console.log(`e2e: ${pass} passed, ${fail} failed`);
ws.close(); proc.kill();
try { rmSync(profile, { recursive: true, force: true }); } catch { }
process.exit(fail ? 1 : 0);
