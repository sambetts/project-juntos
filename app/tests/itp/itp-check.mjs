// Checks the agreed-home estimate's purchase tax (ITP) against each region's law (B-060).
// The expected euros are worked out by hand from the laws' own tables (research/2026-11-23-itp-scales.md),
// not from the code. Run from the repository root: node app/tests/itp/itp-check.mjs
import fs from 'node:fs';
import vm from 'node:vm';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
const src = fs.readFileSync(path.join(root, 'app/src/Compicasa.Api/wwwroot/i18n.js'), 'utf8');
const ctx = vm.createContext({});
const { itpTax, REGIONS } = vm.runInContext(src + '\n;({itpTax, REGIONS})', ctx);
const reg = id => REGIONS.find(r => r.id === id);

let pass = 0, fail = 0;
const ok = (cond, name) => { if (cond) pass++; else { fail++; console.log('FAIL ' + name); } };
const tax = (id, price, want) => {
  const got = itpTax(price, id ? reg(id) : null).tax;
  ok(Math.abs(got - want) < 0.01, `${id || 'unknown'} ${price}: want ${want}, got ${got}`);
};

// Data: every region has a rate; each scale starts at that rate, rises, and ends open
for (const r of REGIONS) {
  ok(typeof r.itp === 'number' && r.itp > 0, r.id + ': has a rate');
  if (!r.scale) continue;
  const b = r.scale.bands;
  ok(b[0][1] === r.itp, r.id + ': scale starts at the first-band rate');
  ok(b[b.length - 1][0] === null, r.id + ': last band is open');
  ok(b.every((x, i) => i === 0 || (x[0] === null || x[0] > b[i - 1][0]) && x[1] > b[i - 1][1]), r.id + ': bands rise');
  ok(r.scale.by === 'part' || r.scale.by === 'whole', r.id + ': says how the scale applies');
}
ok(REGIONS.filter(r => r.scale).map(r => r.id).sort().join() === 'ara,ast,bal,cat,cyl,ext,val', 'the seven regions with a scale');

// Flat rates
tax('mad', 300000, 18000);
tax('gal', 1000000, 80000);
tax('mur', 200000, 15500);
// Aragón, DL 1/2005 art. 121-1: each slice at its rate (the law's cuota column: 32,000; 36,250; 40,750; 64,500)
tax('ara', 400000, 32000);
tax('ara', 420000, 33700);
tax('ara', 500000, 40750);
tax('ara', 800000, 69500);
// Asturias, DL 2/2014 art. 26: the whole value at the rate of its band
tax('ast', 300000, 24000);
tax('ast', 300001, 27000.09);
tax('ast', 600000, 60000);
// Illes Balears, DL 1/2014 art. 10: the average rate, rounded to two decimals (up only when the third is above 5)
tax('bal', 500000, 41000);   // 8.20%
tax('bal', 450000, 36495);   // 36,500 / 450,000 = 8.111% -> 8.11%
tax('bal', 470000, 38305);   // 38,300 / 470,000 = 8.1489% -> 8.15%
tax('bal', 700000, 59990);   // 60,000 / 700,000 = 8.5714% -> 8.57%
tax('bal', 2500000, 275000); // 210,000 + 65,000 = 11.00%
// Castilla y León, DL 1/2013 art. 25.1: 8% to 250,000, 10% on the rest
tax('cyl', 250000, 20000);
tax('cyl', 300000, 25000);
// Cataluña, DL 1/2024 art. 641-1.1 (cuota column: 60,000; 93,000; 165,000)
tax('cat', 600000, 60000);
tax('cat', 1000000, 105000);
tax('cat', 2000000, 230000);
// Comunitat Valenciana, Ley 13/1997 art. 13.Uno: 11% on the whole value above 1 million
tax('val', 1000000, 90000);
tax('val', 1000001, 110000.11);
// Extremadura, DL 1/2018 art. 36: each slice at its rate
tax('ext', 360000, 28800);
tax('ext', 500000, 42800);
tax('ext', 700000, 63800);
// A home we can't place: the most any region charges at that price
tax(null, 300000, 30000);    // Cataluña 10%
tax(null, 2000000, 230000);  // Cataluña's scale
// The average rate shown to people matches the tax
ok(Math.abs(itpTax(420000, reg('ara')).rate - 33700 / 420000 * 100) < 1e-9, 'ara 420000: average rate');
ok(itpTax(0, reg('cat')).tax === 0 && itpTax(0, reg('cat')).rate === 10, 'no price: no tax, first-band rate');

console.log(`${pass} passed, ${fail} failed`);
process.exit(fail ? 1 : 0);
