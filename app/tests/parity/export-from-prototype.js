// Exports the made-up profiles and a parity fixture from the prototype's logic block,
// so the C# engine (app/src/Compicasa.Core) can be checked against the original rules.
// Run from the repository root: node app/tests/parity/export-from-prototype.js
'use strict';
const fs = require('fs'), path = require('path');
const root = path.resolve(__dirname, '../../..');
const html = fs.readFileSync(path.join(root, 'prototype/compicasa-prototype.html'), 'utf8');
const logic = html.split('/* ===== LOGIC START ===== */')[1].split('/* ===== LOGIC END ===== */')[0];
const api = new Function(logic + '\nreturn {PROFILES,PRESETS,hard,score,accepts,groupCheck,bestGroups,houseBrief,roomPlan,placementTips,agreementTopics,explain,groupScore};')();

const strip = p => { const o = JSON.parse(JSON.stringify(p)); if (!o.bio) delete o.bio; return o; };
const seeds = api.PROFILES.map(strip);
fs.writeFileSync(path.join(root, 'app/src/Compicasa.Core/seed-profiles.json'), JSON.stringify(seeds, null, 1) + '\n');

const people = api.PROFILES.concat(Object.keys(api.PRESETS).map(k => Object.assign(strip(api.PRESETS[k].user), { id: 'you-' + k })));
const pairs = [];
for (const a of people) for (const b of people) {
  if (a === b) continue;
  const s = api.score(a, b), h = api.hard(a, b), ex = api.explain(a, b);
  pairs.push({ a: a.id, b: b.id, ok: h.ok, mine: h.mine.length, theirs: h.theirs.length, both: h.both.length,
    total: s.total, parts: s.parts, accepts: api.accepts(a, b), clicks: ex.clicks.length, talks: ex.talks.length });
}
// Groups: each preset user, liking everyone; suggestions and the brief of the top one.
const groups = Object.keys(api.PRESETS).map(k => {
  const u = Object.assign(strip(api.PRESETS[k].user), { id: 'you' });
  const liked = {}; api.PROFILES.forEach(p => { liked[p.id] = true; });
  const sugg = api.bestGroups(u, liked, [], 3);
  const briefs = sugg.map(g => {
    const all = [u].concat(g.ids.map(id => api.PROFILES.find(p => p.id === id)));
    const hb = api.houseBrief(all);
    return { ids: g.ids, beds: hb.beds, baths: hb.baths, cars: hb.cars, m2: hb.m2, m2min: hb.m2min, m2max: hb.m2max,
      areas: hb.areas, types: hb.types, feats: hb.feats.map(f => f.f.id + ':' + f.level), extras: hb.extras.length,
      rooms: api.roomPlan(all).map(r => r.pct), tips: api.placementTips(all).length, topics: api.agreementTopics(all).length,
      check: api.groupCheck(all).ok };
  });
  return { preset: k, user: u, suggestions: sugg, briefs };
});
// Whole-group checks on a few awkward groups of made-up people.
const sets = [['marta', 'laura'], ['diego', 'raul', 'tom'], ['nuria', 'lucia', 'ana'], ['elena', 'marta', 'carmen'], ['karim', 'irene', 'tom', 'diego']];
const checks = sets.map(ids => { const all = ids.map(id => api.PROFILES.find(p => p.id === id)); const c = api.groupCheck(all); return { ids, ok: c.ok, issues: c.issues.length, min: api.groupScore(all).min, avg: api.groupScore(all).avg }; });
fs.writeFileSync(path.join(__dirname, 'prototype-parity.json'), JSON.stringify({ pairs, groups, checks }, null, 0) + '\n');
console.log('seeds', seeds.length, 'pairs', pairs.length, 'groups', groups.map(g => g.suggestions.length), 'checks', checks.length);
