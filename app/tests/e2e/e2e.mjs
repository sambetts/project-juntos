// End-to-end check of the PoC in a real browser (headless Microsoft Edge or Chrome), with no npm packages:
// it drives the browser through the DevTools protocol using Node's built-in WebSocket.
// Usage: node app/tests/e2e/e2e.mjs <base-url> <invite-code> [screenshot-dir]
// Runs at phone size (390 x 844) and desktop size, in Spanish and English. Exit code 1 if any check fails.
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
  if (m.method === 'Log.entryAdded' && m.params.entry.level === 'error' && !/favicon|status of 40[13]/.test(m.params.entry.text + m.params.entry.url)) errors.push(m.params.entry.text);
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
async function size(w, h, mobile) { await send('Emulation.setDeviceMetricsOverride', { width: w, height: h, deviceScaleFactor: mobile ? 2 : 1, mobile }); }

await send('Runtime.enable'); await send('Log.enable'); await send('Page.enable');

// Words that must not appear on a screen in the other language (a leak check, as in the prototype tests).
const EN_ONLY = /\b(Sign in|Your profile|Matches|Like|Pass|households|Proof of concept)\b/;
const ES_ONLY = /\b(Entrar|Tu perfil|Personas afines|Me interesa|Pasar|hogares|Prueba de concepto)\b/;

const user = 'e2e' + Date.now().toString(36);
for (const [w, h, mobile, tag] of [[390, 844, true, 'phone'], [1280, 900, false, 'desktop']]) {
  await size(w, h, mobile);
  await send('Network.clearBrowserCookies');
  await send('Page.navigate', { url: base + '/' });
  await until(`!!document.querySelector('#auth-form')`, tag + ' sign-in form');
  ok(await js('document.documentElement.lang') === 'es', tag + ': opens in Spanish');
  ok(!EN_ONLY.test(await text()), tag + ': no English on the Spanish sign-in screen');
  ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), tag + ': no sideways scrolling on sign-in');
  await shot(tag + '-1-signin-es');

  // Sign up (phone) or sign in (desktop, same account).
  const signup = tag === 'phone';
  if (signup) { await click('[data-action="auth-mode"][data-id="signup"]'); await until(`!!document.querySelector('#a-invite')`, 'sign-up form'); }
  const fill = (id, v) => js(`(()=>{const el=document.getElementById(${JSON.stringify(id)});el.value=${JSON.stringify(v)};return true;})()`);
  await fill('a-user', user); await fill('a-pass', 'e2e-long-password');
  if (signup) {
    await fill('a-name', 'Prueba'); await fill('a-invite', 'wrong-code');
    await click('#auth-form button[type=submit]');
    await until(`document.getElementById('auth-error')?.textContent.length>0`, 'wrong invite shows an error');
    ok(/invitación/.test(await js(`document.getElementById('auth-error').textContent`)), 'wrong invite error is in Spanish');
    await fill('a-invite', invite);
  }
  await click('#auth-form button[type=submit]');
  await until(`!!document.querySelector('#tabs [data-tab]')`, tag + ' signed in');

  if (signup) {
    // Profile: load the room example; it saves itself.
    await until(`!!document.querySelector('[data-action="preset"][data-id="room"]')`, 'profile view');
    await click('[data-action="preset"][data-id="room"]');
    await until(`document.querySelector('#actionbar')?.innerText.includes('Guardado')`, 'profile saved');
    await shot(tag + '-2-profile-es');
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
  await shot(tag + '-3-matches-es');

  if (signup) {
    // Like everyone, then propose the top suggested group.
    const likes = await js(`document.querySelectorAll('[data-action="like"]').length`);
    for (let i = 0; i < likes; i++) {
      await click('[data-action="like"]');
      await sleep(250);
    }
    await until(`document.querySelectorAll('[data-action="propose"]').length>0`, 'a group is suggested');
    await click('.gcard [data-action="preview"]');
    await until(`!!document.querySelector('.kpis') && document.body.innerText.includes('Solo vista previa')`, 'preview shows the brief');
    await shot(tag + '-4-preview-es');
    await click('.notice [data-action="propose"]');
    await until(`document.body.innerText.includes('Formado')`, 'group formed');
    ok(await js(`document.querySelectorAll('.matrix, .kpis').length`) > 0, 'formed group shows the brief');
    await shot(tag + '-5-group-es');
  } else {
    await click('[data-tab="groups"]');
    await until(`document.querySelectorAll('[data-action="open-group"]').length>0`, 'desktop sees the group from the phone');
    await click('[data-action="open-group"]');
    await until(`!!document.querySelector('.kpis')`, 'group opens');
    await shot(tag + '-5-group-es');
  }

  // Switch to English and check the same screen.
  await click('#lang-switch');
  await until(`document.documentElement.lang==='en'`, tag + ' switches to English');
  ok(!ES_ONLY.test(await text()), tag + ': no Spanish UI words on the English screen');
  await shot(tag + '-6-group-en');
  await click('#lang-switch');
}

// Install basics for phones: manifest, icons and service worker.
const man = await (await fetch(base + '/manifest.webmanifest')).json();
ok(man.display === 'standalone' && man.icons.some(i => i.sizes === '512x512'), 'installable manifest');
ok((await fetch(base + '/icons/apple-touch-icon.png')).ok, 'iOS icon served');
ok(await js(`navigator.serviceWorker.getRegistration().then(r=>!!r)`), 'service worker registered');

// Tidy up: delete the test account through the API, as a user would.
await js(`window.__compicasa.api('DELETE','/api/me').then(()=>true)`);

ok(errors.length === 0, 'no script errors: ' + errors.join(' | '));
console.log(`e2e: ${pass} passed, ${fail} failed`);
ws.close(); proc.kill();
try { rmSync(profile, { recursive: true, force: true }); } catch { }
process.exit(fail ? 1 : 0);
