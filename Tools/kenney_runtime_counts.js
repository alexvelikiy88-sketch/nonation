'use strict';
// Verify counts reported by successfully instantiated real Kenney glTF meshes.
// Both first load and subsequent page reload must satisfy the same contract.
const EXPECTED = Object.freeze({ buildings: 12, trees: 13, vehicles: 3, props: 2 });

function requireKenneyCityReady(line) {
  if (typeof line !== 'string' || !line.includes('NONATION_KENNEY_CITY_READY'))
    throw new Error('Missing Kenney city readiness marker');
  const counts = {};
  for (const [name, expected] of Object.entries(EXPECTED)) {
    const match = new RegExp('(?:^|\\s)' + name + '=(\\d+)(?=\\s|$)').exec(line);
    if (!match) throw new Error('Missing Kenney art counter ' + name + ': ' + line);
    counts[name] = Number(match[1]);
    if (counts[name] !== expected)
      throw new Error('3D city art integration incomplete: expected ' +
        name + '=' + expected + ', got ' + counts[name]);
  }
  return counts;
}

module.exports = { requireKenneyCityReady, EXPECTED };
