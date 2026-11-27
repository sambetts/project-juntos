// End-to-end check of the PoC in a real browser (headless Microsoft Edge or Chrome), with no npm packages:
// it drives the browser through the DevTools protocol using Node's built-in WebSocket.
// Usage: node app/tests/e2e/e2e.mjs <base-url> <invite-code> [screenshot-dir]
// Locally, start the app with Contact__PerHour=1000 and Auth__AttemptsPerMinute=1000: each run sends a contact message (the
// default allows 5 an hour) and several sign-in attempts (10 a minute), so back-to-back runs otherwise hit the limits.
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
  ok(await js(`[...document.querySelectorAll('#homes .home')].every(h=>{const a=h.querySelector('a.btn[rel~="external"]');return a?(a.target==='_blank'&&/noopener/.test(a.rel)&&/^https:/.test(a.href)&&/Ver el anuncio en /.test(a.textContent)):/inventada/.test(h.innerText);})`),
    tag + ': every home links to the agency advert in a new tab, or is marked made-up');
  // B-058: the group's combined budget, never anyone's own figure
  ok(await js(`/presupuesto conjunto: [0-9.]+ €/.test(document.querySelector('#homes .budgetline')?.innerText || '')`), tag + ': homes show the combined budget');
  await js(`document.getElementById('homes').scrollIntoView()`);
  await shot(tag + '-5b-homes-es');
  await js('window.scrollTo(0,0)');
}
// House voting (B-056): add a home to the shortlist, vote yes round by round; when everyone agrees, the pack shows.
async function voteCheck(tag) {
  await click('.subtabs [data-gsub="homes"]');
  if (!await until(`!!document.querySelector('#homes [data-action="sl-add"]')`, tag + ': homes offer "Add to shortlist"')) return;
  await click('#homes [data-action="sl-add"]');
  await until(`!!document.querySelector('#homes a[href$="-shortlist"]')`, tag + ': the home shows as on the shortlist');
  await click('.subtabs [data-gsub="shortlist"]');
  if (!await until(`/-shortlist$/.test(location.hash) && document.querySelectorAll('.cand').length===1`, tag + ': the shortlist is its own view, with the home')) return;
  ok(await js(`document.querySelectorAll('.cand .stages li').length===3`), tag + ': three voting rounds');
  for (let i = 0; i < 3; i++) {
    if (!await js(`!!document.querySelector('.cand [data-action="sl-vote"][data-v="yes"]')`)) break;
    await click('.cand [data-action="sl-vote"][data-v="yes"]');
    await sleep(600);
  }
  const agreed = await js(`!!document.querySelector('.cand.agreed')`);
  if (agreed) {
    ok(await js(`!!document.querySelector('.cand table.costs') && document.querySelectorAll('.cand .qs li').length===9`), tag + ': agreed: cost per person and lawyer questions');
    ok(await js(`(()=>{const d=document.querySelector('.cand pre.draft');const real=/Ver el anuncio/.test(document.querySelector('.cand').innerText);return real?!d&&/aún no están activados/.test(document.querySelector('.cand').innerText):!!d&&/^mailto:/.test(document.querySelector('.cand a[href^="mailto:"]').href);})()`), tag + ': an email draft for made-up homes only');
  } else {
    // A made-up member said maybe or no: their vote shows, with its reason, once everyone has voted.
    ok(await js(`!!document.querySelector('.cand .vchip.maybe, .cand .vchip.no')`), tag + ': not agreed, and the votes show why');
  }
  ok(!EN_ONLY.test(await text()), tag + ': no English on the Spanish shortlist');
  ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), tag + ': no sideways scrolling on the shortlist');
  await js(`document.getElementById('shortlist').scrollIntoView()`);
  await shot(tag + '-5e-shortlist-es');
  await js('window.scrollTo(0,0)');
}
async function size(w, h, mobile) { await send('Emulation.setDeviceMetricsOverride', { width: w, height: h, deviceScaleFactor: mobile ? 2 : 1, mobile }); }

await send('Runtime.enable'); await send('Log.enable'); await send('Page.enable');
// The browser's languages decide the first language (Session 37): run as a Spanish browser, then check the rule.
const userAgent = (await send('Browser.getVersion')).result.userAgent;
const browserLangs = langs => send('Emulation.setUserAgentOverride', { userAgent, acceptLanguage: langs });
{
  for (const [langs, want] of [['en-GB,en', 'en'], ['fr-FR,fr', 'en'], ['ca-ES,ca', 'es'], ['en-US,es-ES', 'en'], ['es-ES,es', 'es']]) {
    await browserLangs(langs);
    await send('Page.navigate', { url: base + '/' });
    await until(`document.readyState==='complete'&&!!document.querySelector('#app .boot')===false`, 'page loads for ' + langs);
    await js(`localStorage.removeItem('compicasa-lang');1`);
    await send('Page.reload');
    await until(`document.readyState==='complete'&&!document.querySelector('#app .boot')`, 'page reloads for ' + langs);
    ok(await js('document.documentElement.lang') === want, `a browser set to ${langs} opens in ${want}`);
  }
}

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
// The front page's story (B-087, D-055): plays when in view, pauses, jumps to a chapter, switches purpose, offers the
// other story at the end, keeps its place across a language switch, and shows the finished home still with reduced motion.
{
  await size(390, 844, true);
  await send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'no-preference' }] });
  await send('Page.navigate', { url: base + '/' });
  await until(`!!document.querySelector('#story .st-svg')`, 'story renders on the front page');
  ok(/para vivir en ella o para alquilarla/.test(await js(`document.querySelector('.lhero .lead')?.textContent||''`)), 'front page: the opening line covers renting out');
  await js(`document.getElementById('story').scrollIntoView({block:'center'});1`);
  await until(`Story.state().playing&&Story.state().t>.3`, 'story plays when it comes into view');
  const playLabel = () => js(`document.querySelector('#story .st-play').getAttribute('aria-label')`);
  const before = await playLabel();
  await click('#story .st-play');
  ok(!(await js('Story.state().playing')) && (await playLabel()) !== before, 'story: the play button pauses and relabels');
  await click('#story [data-st-ch="2"]');
  ok(await js(`(s=>s.playing&&s.t>=13&&s.t<14)(Story.state())&&document.querySelector('#story [data-st-ch="2"]').getAttribute('aria-current')==='step'`), 'story: chapter 3 jumps there and plays');
  await click('#story [data-st-mode="let"]');
  ok(await js(`(s=>s.mode==='let'&&s.t>=13)(Story.state())&&document.querySelector('#story [data-st-mode="let"]').getAttribute('aria-pressed')==='true'`), 'story: the switch changes purpose and keeps its place');
  await js('Story.pause();Story.seek(Story.state().end);1');
  ok(await js(`(b=>!!b&&b.offsetParent!==null&&/vivir/.test(b.textContent))(document.querySelector('#story .st-inv'))`), 'story: the end offers the other purpose');
  await click('#story .st-inv');
  ok(await js(`(s=>s.mode==='live'&&s.playing&&s.t<1)(Story.state())&&document.activeElement===document.querySelector('#story .st-play')`), 'story: the offer plays the other story and moves focus to play');
  await js('Story.pause();Story.seek(20);1');
  await click('#menu-btn');
  await click('#menu [data-action="lang"][data-id="en"]');
  await until(`document.documentElement.lang==='en'&&!!document.querySelector('#story .st-svg')`, 'story: switch to English');
  ok(await js(`Math.abs(Story.state().t-20)<.01&&/Choose it together/.test(document.querySelector('#story .st-cap').textContent)`), 'story: a language switch keeps its place and translates the caption');
  ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), 'story: no sideways scrolling');
  if (await js(`document.getElementById('menu').hidden`)) await click('#menu-btn');
  await click('#menu [data-action="lang"][data-id="es"]');
  await until(`document.documentElement.lang==='es'`, 'story: back to Spanish');
  await send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }] });
  await send('Page.navigate', { url: 'about:blank' });
  await send('Page.navigate', { url: base + '/' });
  await until(`!!document.querySelector('#story .st-svg')`, 'story renders with reduced motion');
  await sleep(600);
  ok(await js(`(s=>s.t===s.end&&!s.playing)(Story.state())`), 'story: with reduced motion it shows the finished home, still');
  await send('Emulation.setEmulatedMedia', { features: [] });
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
    // The onboarding wizard: ten steps to live in the home (eight to rent it out, B-063), matches locked until the end.
    await until(`!!document.querySelector('.wizard')`, 'onboarding wizard starts');
    ok(await js(`!document.querySelector('#tabs [data-tab]')`), 'no tabs until onboarding is done');
    ok(/Paso 1 de 10/.test(await text()), 'wizard shows its progress');
    ok(await js(`document.getElementById('f-name').value`) === 'Prueba', 'wizard starts from the name given at sign-up');
    await shot(tag + '-2-wizard-1-es');
    await click('[data-action="wiz-next"]');
    await until(`/Paso 2 de 10/.test(document.body.innerText)`, 'wizard step 2');
    // B-063: what the home is for. To live in by default; renting it out shortens the wizard.
    ok(await js(`document.querySelector('input[data-field="purpose"][value="live"]').checked && /Para alquilar/.test(document.body.innerText)`), 'wizard asks what the home is for, to live in by default');
    await click('input[data-field="purpose"][value="let"]');
    await until(`/Paso 2 de 8/.test(document.body.innerText) && document.querySelector('#actionbar')?.innerText.includes('Guardado')`, 'renting it out skips the living-together steps');
    await click('input[data-field="purpose"][value="live"]');
    await until(`/Paso 2 de 10/.test(document.body.innerText) && document.querySelector('#actionbar')?.innerText.includes('Guardado')`, 'and living there brings them back');
    await click('[data-action="wiz-next"]');
    await until(`/Paso 3 de 10/.test(document.body.innerText)`, 'wizard step 3');
    await click('[data-action="preset"][data-id="room"]');
    await until(`document.querySelector('#actionbar')?.innerText.includes('Guardado')`, 'example saved');
    ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), 'no sideways scrolling in the wizard');
    for (let n = 4; n <= 10; n++) {
      await click('[data-action="wiz-next"]');
      await until(`/Paso ${n} de 10/.test(document.body.innerText)`, 'wizard step ' + n);
      if (n === 7) {
        // B-059: areas anywhere in Spain, one folding group per region; Madrid open to start with.
        ok(await js(`document.querySelectorAll('details.region').length === 19`), 'area picker lists the 19 regions');
        ok(await js(`document.querySelector('details.region[data-region="mad"]').open`), 'area picker opens Madrid');
        await js(`(() => { document.querySelector('details.region[data-region="and"]').open = true; document.querySelector('input[data-field="areas"][value="p41"]').click(); })()`);
        await until(`/6 sitios elegidos/.test(document.querySelector('#actionbar')?.innerText) && document.querySelector('#actionbar')?.innerText.includes('Guardado')`, 'a province outside Madrid can be chosen and saved, counted in the footer');
        ok(!/Has elegido:/.test(await text()), 'the wizard counts places in the footer rather than listing them above');
        await shot(tag + '-2-wizard-7-areas-es');
        ok(await js(`document.querySelector('details.region[data-region="and"]').open && /1/.test(document.querySelector('details.region[data-region="and"] summary').textContent)`), 'its region stays open with a count');
        await js(`document.querySelector('input[data-field="areas"][value="p41"]').click()`);
        await until(`!/Sevilla/.test(document.querySelector('.regions')?.previousElementSibling?.textContent || '')`, 'and taken off again');
        ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), 'no sideways scrolling in the area picker');
      }
      if (n === 8) {
        // B-058: the budget step needs an amount before it lets you on.
        await click('[data-action="wiz-next"]');
        await until(`/10\.000/.test(document.getElementById('wiz-error')?.textContent || '')`, 'wizard asks for a budget');
        ok(/Paso 8 de 10/.test(await text()), 'wizard stays on the budget step until it has one');
        await js(`(() => { const el = document.getElementById('f-maxBudget'); el.value = '200000'; el.dispatchEvent(new Event('change', { bubbles: true })); })()`);
        await until(`document.querySelector('#actionbar')?.innerText.includes('Guardado')`, 'budget saved');
      }
    }
    await shot(tag + '-2-wizard-10-es');
    await click('[data-action="wiz-finish"]');
    await until(`!!document.querySelector('#tabs [data-tab]')`, tag + ' onboarding finished');
  } else {
    await until(`!!document.querySelector('#tabs [data-tab]')`, tag + ' signed in');
  }

  // Home (Session 20): where you land, and where the logo takes you
  await until(`location.hash==='#home' && !!document.querySelector('.dash-steps')`, tag + ': lands on Home');
  ok(await js(`document.querySelectorAll('.dstep').length===4`), tag + ': Home shows the four steps');
  await until(`!!document.querySelector('.dash-score .ring') && !!document.querySelector('.dash-homes .home, .dash-homes p:not(.hint)')`, tag + ': Home shows the profile score and homes');
  ok(!(await js(`!!document.getElementById('badge')`)), tag + ': no "proof of concept" badge in the header');
  ok(!EN_ONLY.test(await text()), tag + ': no English on the Spanish Home page');
  ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), tag + ': no sideways scrolling on Home');
  await shot(tag + '-3-home-es');
  await send('Page.navigate', { url: base + '/#funnel' });
  await until(`/solo para quienes gestionan/.test(document.querySelector('#app')?.innerText||'')`, tag + ': the release funnel is refused to someone not listed (B-074)');
  await send('Page.navigate', { url: base + '/#houses' });
  ok(await until(`location.hash==='#houses' && document.querySelectorAll('.homes .home').length>0`, tag + ': the house browser lists homes (B-064)'), tag + ': house browser opens from #houses');
  ok(await js(`/viviendas/.test(document.querySelector('.sumline')?.innerText||'')`), tag + ': the house browser says how many homes');
  await shot(tag + '-9-houses');
  // B-084: the phone and desktop passes share one account, so save a home that isn't saved yet and count from what was saved before
  const savedBefore = await js(`window.__compicasa.S.br?.data?.savedCount||0`);
  const saveId = await js(`document.querySelector('[data-action="br-save"][aria-pressed="false"]')?.getAttribute('data-id')||''`);
  await js(`document.querySelector('[data-action="br-save"][data-id=${JSON.stringify(saveId)}]').click(),true`);
  ok(await until(`document.querySelector('[data-action="br-save"][data-id=${JSON.stringify(saveId)}]')?.getAttribute('aria-pressed')==='true'`, tag + ': saving a home marks it saved (B-064)'), tag + ': a home can be saved');
  await js(`(()=>{document.getElementById('br-more-f').open=true;const c=document.getElementById('br-saved');c.checked=true;c.dispatchEvent(new Event('change',{bubbles:true}));return true;})()`);
  ok(await until(`document.querySelectorAll('.homes .home').length===${savedBefore + 1} && !!document.getElementById('home-'+${JSON.stringify(saveId)})`, tag + ': only saved homes'), tag + ': the saved filter shows the saved homes, including the new one');
  await send('Page.navigate', { url: base + '/#inbox' });
  await until(`/solo para quienes gestionan/.test(document.querySelector('#app')?.innerText||'')`, tag + ': the contact inbox is refused to someone not listed (B-078)');
  await send('Page.navigate', { url: base + '/#contact' });
  await until(`!!document.querySelector('#contact-form') && !!document.querySelector('#ct-email').value`, tag + ': the contact form opens with your email filled in (B-078)');
  await js(`(()=>{const b=document.querySelector('#ct-body');b.value='Una prueba del formulario de contacto';document.querySelector('#contact-form button[type=submit]').click();return true;})()`);
  // The contact form allows Contact:PerHour messages an hour per address (5 live); start the local app with Contact__PerHour=1000 for repeated runs
  await until(`/Tu mensaje ha llegado/.test(document.querySelector('#app')?.innerText||'') || !!document.getElementById('contact-error')?.textContent`, tag + ': the contact form answers');
  const ctErr = await js(`document.getElementById('contact-error')?.textContent||''`);
  ok(!ctErr, tag + ': a contact message is sent and thanked (B-078)' + (ctErr ? ` — refused: "${ctErr}"; if it is the hourly limit, restart the local app with Contact__PerHour=1000` : ''));
  // Feedback (B-081): from the footer link while on the house browser; the screen it came from goes with it
  await send('Page.navigate', { url: base + '/#houses' });
  await until(`location.hash==='#houses' && !!document.querySelector('#footnote a[href="#feedback"]')`, tag + ': the footer links to feedback when signed in (B-081)');
  await js(`document.querySelector('#footnote a[href="#feedback"]').click(),true`);
  ok(await until(`!!document.querySelector('#feedback-form') && document.querySelectorAll('#feedback-form input[name=kind]').length===3`, tag + ': feedback form opens (B-081)'), tag + ': the feedback form has three kinds');
  ok(await js(`window.__compicasa.S.fbFrom==='#houses'`), tag + ': feedback remembers the screen it was opened from (B-081)');
  await js(`(()=>{const f=document.querySelector('#feedback-form');f.querySelector('input[name=kind][value=like]').click();f.querySelector('input[name=rating][value="4"]').click();f.querySelector('#fb-body').value='Una prueba de opinión';f.querySelector('button[type=submit]').click();return true;})()`);
  ok(await until(`/Tu opinión ha llegado/.test(document.querySelector('#app')?.innerText||'')`, tag + ': feedback is sent and thanked (B-081)'), tag + ': feedback is sent and thanked');
  await send('Page.navigate', { url: base + '/#help' });
  await until(`!!document.querySelector('.help')`, tag + ': guide opens signed in');
  await click('#brandlink');
  await until(`location.hash==='#home' && !!document.querySelector('.dash-steps') && !document.querySelector('.help')`, tag + ': the logo goes to Home');

  // Header menu (Session 21): profile, language, colour scheme, help; Profile left the tab bar
  ok(await js(`document.querySelectorAll('#tabs [data-tab]').length===5 && !!document.querySelector('#tabs [data-tab="home"]') && !!document.querySelector('#tabs [data-tab="browse"]') && !document.querySelector('#tabs [data-tab="you"]')`), tag + ': five tabs (Home, People, Homes, Groups, Messages), Profile in the menu');
  await click('#menu-btn');
  await until(`!document.getElementById('menu').hidden && document.getElementById('menu-btn').getAttribute('aria-expanded')==='true'`, tag + ': menu opens');
  ok(await js(`['#menu a[href="#profile"]','#menu [data-action="logout"]','#menu [data-action="lang"][data-id="en"]','#menu [data-action="theme"][data-id="dark"]','#menu a[href="#help"]'].every(q=>!!document.querySelector(q))`), tag + ': menu has profile, sign out, language, theme and the guide');
  ok(await js(`(()=>{const r=document.getElementById('menu').getBoundingClientRect();return r.left>=0&&r.right<=window.innerWidth+1;})()`), tag + ': menu fits on screen');
  await shot(tag + '-3b-menu-es');
  await click('#menu [data-action="theme"][data-id="dark"]');
  ok(await js(`document.documentElement.dataset.theme==='dark' && localStorage.getItem('compicasa-theme')==='dark' && getComputedStyle(document.body).backgroundColor==='rgb(14, 19, 27)'`), tag + ': dark theme applies and is remembered');
  await click('#menu [data-action="theme"][data-id="light"]');
  ok(await js(`document.documentElement.dataset.theme==='light' && getComputedStyle(document.body).backgroundColor==='rgb(245, 246, 250)'`), tag + ': light theme applies');
  await click('#menu [data-action="theme"][data-id="auto"]');
  ok(await js(`!document.documentElement.hasAttribute('data-theme') && localStorage.getItem('compicasa-theme')==='auto'`), tag + ': automatic theme follows the device');
  await js(`document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}))`);
  await until(`document.getElementById('menu').hidden && document.activeElement && document.activeElement.id==='menu-btn'`, tag + ': Escape closes the menu and returns focus');
  await click('#menu-btn');
  await click('#menu a[href="#profile"]');
  await until(`location.hash==='#profile' && document.getElementById('menu').hidden`, tag + ': Profile opens from the menu, which closes');
  await click('#brandlink');
  await until(`location.hash==='#home'`, tag + ': back Home');

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
  ok(/Buscas vivienda: para vivir/.test(await text()), tag + ': People says which purpose it shows (B-063)');
  await click('.pcard .more');
  await until(`!!document.querySelector('dialog.sheet[open]') && /^#person-/.test(location.hash)`, tag + ': More opens the person sheet with its own address');
  // B-085: the score and each of its parts explain what you have in common, on hover, tap or focus
  ok(await js(`(()=>{const b=[...document.querySelectorAll('dialog.sheet .bar[data-tip]')];return b.length>=3&&b.every(x=>x.getAttribute('data-tip').length>20)&&!!document.querySelector('dialog.sheet .ring[data-tip]')&&!!document.querySelector('.pcard .ring[data-tip]');})()`), tag + ': the score ring and each bar have an explanation (B-085)');
  ok(await js(`(()=>{const b=document.querySelector('dialog.sheet .bar[data-tip]'),tip=document.querySelector('.charttip');b.dispatchEvent(new PointerEvent('pointerdown',{bubbles:true,pointerType:'touch'}));return !tip.hidden&&!!tip.closest('dialog.sheet[open]')&&tip.textContent===b.getAttribute('data-tip');})()`), tag + ': tapping a bar shows its tooltip above the sheet (B-085)');
  await shot(tag + '-4c-score-tooltip-es');
  ok(await js(`(()=>{if(!document.hasFocus())return true;const b=document.querySelectorAll('dialog.sheet .bar[data-tip]')[1],tip=document.querySelector('.charttip');b.focus();return !tip.hidden&&tip.textContent===b.getAttribute('data-tip');})()`), tag + ': focusing a bar shows its tooltip (when the page has focus) (B-085)');
  await click('[data-action="close-sheet"]');
  await until(`!document.querySelector('dialog.sheet[open]') && /^#people-/.test(location.hash)`, tag + ': closing the sheet goes back to People');
  // B-061: a refresh can redraw the page just after the sheet closes. Force both timings: a redraw before the
  // dialog's "close" event, and a redraw while the Back it triggers is on its way. Either used to reopen the sheet.
  for (const [when, how] of [['before the close event', `b.click();window.__compicasa.render();`],
    ['while going back', `document.addEventListener('close',()=>window.__compicasa.render(),{once:true});b.click();`]]) {
    await click('.pcard .more');
    if (!await until(`!!document.querySelector('dialog.sheet[open]') && /^#person-/.test(location.hash)`, `${tag}: the sheet opens again (${when})`)) continue;
    await js(`(()=>{const b=document.querySelector('[data-action="close-sheet"]');${how}})()`);
    await until(`!document.querySelector('dialog.sheet[open]') && /^#people-/.test(location.hash)`, `${tag}: the sheet stays closed after a redraw ${when}`);
    await sleep(400);
    ok(await js(`!document.querySelector('dialog.sheet[open]') && /^#people-/.test(location.hash)`), `${tag}: still on People a moment later (${when})`);
  }
  // B-061: a redraw between a link changing the address and its hashchange must not write the old address back
  const peopleAt = await js('location.hash');
  await js(`(()=>{location.hash='#people-liked';window.__compicasa.render();})()`);
  await until(`location.hash==='#people-liked' && document.querySelector('.subtabs [aria-current="page"]')?.getAttribute('href')==='#people-liked'`, tag + ': a redraw before the hashchange keeps the new view');
  await js(`location.hash=${JSON.stringify(peopleAt)}`);
  await until(`document.querySelector('.subtabs [aria-current="page"]')?.getAttribute('href')===${JSON.stringify(peopleAt)}`, tag + ': back on the first People view');
  await shot(tag + '-3-matches-es');

  if (signup) {
    ok(await js(`!!document.querySelector('[data-action="like"] svg.thumb') && !document.body.innerText.includes('♥')`), 'likes show a thumbs-up, not a heart (D-056)');
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
    await voteCheck(tag);
    // Session 19 (B-036, B-037): the group's chat, Messages, the code of conduct, a scripted reply, the bell
    await click('.subtabs [data-gsub="chat"]');
    await until(`!!document.getElementById('chat-form') && /-chat$/.test(location.hash)`, 'the group chat is its own view');
    await click('[data-tab="messages"]');
    await until(`document.querySelectorAll('ul.convs li').length>0`, 'Messages lists conversations');
    ok(await js(`document.querySelectorAll('#tabs [data-tab]').length===5`), 'five destinations: Home, People, Homes, Groups and Messages (Profile is in the menu; Home a tab since D-054)');
    await click('ul.convs a[href^="#chat-"]');
    await until(`!!document.getElementById('chat-form') && /^#chat-/.test(location.hash)`, 'a conversation with a match opens');
    await fill('chat-input', 'Hola, ¿qué tal?');
    await click('#chat-form button[type=submit]');
    await until(`!!document.querySelector('#conduct-dlg[open]')`, 'the code of conduct comes before the first message');
    await shot(tag + '-5b-conduct-es');
    await click('#conduct-dlg [data-action="conduct-ok"]');
    await until(`document.querySelectorAll('#chat-msgs .msg').length>=2`, 'the message is sent and a made-up person replies');
    ok(await js(`!!document.querySelector('#chat-msgs .msg.mine')?.innerText.includes('Hola')`), 'your message shows as yours');
    ok(!EN_ONLY.test(await text()), 'no English in the Spanish conversation');
    ok(await js(`document.documentElement.scrollWidth <= window.innerWidth + 1`), 'no sideways scrolling in a conversation');
    await shot(tag + '-5c-chat-es');
    await click('#alerts-link');
    await until(`location.hash==='#alerts' && /Avisos/.test(document.querySelector('#app h2')?.innerText||'')`, 'the bell opens the notifications');
    await shot(tag + '-5d-alerts-es');
  } else {
    await click('[data-tab="groups"]');
    await until(`document.querySelectorAll('[data-action="open-group"]').length>0`, 'desktop sees the group from the phone');
    await click('[data-action="open-group"]');
    await until(`!!document.querySelector('.kpis')`, 'group opens');
    await shot(tag + '-5-group-es');
    await homesCheck(tag);
  }

  // Switch to English and check the same screen.
  await click('#menu-btn');
  await click('#menu [data-action="lang"][data-id="en"]');
  await until(`document.documentElement.lang==='en'`, tag + ' switches to English');
  ok(!ES_ONLY.test(await text()), tag + ': no Spanish UI words on the English screen');
  await shot(tag + '-6-group-en');
  await click('#menu [data-action="lang"][data-id="es"]');
  await until(`document.documentElement.lang==='es'`, tag + ' switches back to Spanish');
  await js(`document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}))`);
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
