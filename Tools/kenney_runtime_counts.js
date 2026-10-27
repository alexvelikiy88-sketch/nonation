'use strict';
// Both active art sources are verified against real Unity console markers.
// Keep the original Kenney-only contract for the technical rollback, but a
// visual release must prove 12 modular facades + genuine Kenney tree/car art.
const EXPECTED = Object.freeze({ buildings: 12, trees: 13, vehicles: 3, props: 2 });
const MODULAR_KENNEY_EXPECTED = Object.freeze({
  buildings: 0, trees: 13, vehicles: 3, props: 2
});

function requireKenneyCityReady(line, modern = false) {
  if (typeof line !== 'string' || !line.includes('NONATION_KENNEY_CITY_READY'))
    throw new Error('Missing Kenney city readiness marker');
  const expected = modern ? MODULAR_KENNEY_EXPECTED : EXPECTED;
  const counts = {};
  for (const [name, value] of Object.entries(expected)) {
    const match = new RegExp('(?:^|\\s)' + name + '=(\\d+)(?=\\s|$)').exec(line);
    if (!match) throw new Error('Missing Kenney art counter ' + name + ': ' + line);
    counts[name] = Number(match[1]);
    if (counts[name] !== value)
      throw new Error('3D Kenney art integration incomplete: expected ' +
        name + '=' + value + ', got ' + counts[name]);
  }
  if (modern && !line.includes('source=modular-facades'))
    throw new Error('Kenney building art was not explicitly disabled');
  return counts;
}

function requireModernQuarterReady(line) {
  if (typeof line !== 'string' || !line.includes('NONATION_MODULAR_CITY_READY'))
    throw new Error('Missing live modern facade marker');
  const counts = {};
  for (const name of ['buildings', 'meshes', 'vertices']) {
    const match = new RegExp('(?:^|\\s)' + name + '=(\\d+)(?=\\s|$)').exec(line);
    if (!match) throw new Error('Missing modern facade counter ' + name);
    counts[name] = Number(match[1]);
  }
  if (counts.buildings !== 12 || counts.meshes < 5 || counts.vertices < 1000)
    throw new Error('Modern quarter geometry invalid: ' + JSON.stringify(counts));
  return counts;
}

function requireCc0HeroReady(line) {
  if (typeof line !== 'string' || !line.includes('NONATION_QUATERNIUS_HERO_READY'))
    throw new Error('Missing original CC0 FBX architecture readiness marker');
  const expected = Object.freeze({ models: 5, slots: 3, atlas: 4 });
  const actual = {};
  for (const [name, value] of Object.entries(expected)) {
    const match = new RegExp('(?:^|\\s)' + name + '=(\\d+)(?=\\s|$)').exec(line);
    if (!match || Number(match[1]) !== value)
      throw new Error('Author modeled CC0 architecture incomplete: ' + name +
        ' expected=' + value + ' source=' + line);
    actual[name] = Number(match[1]);
  }
  const verts = /(?:^|\s)vertices=(\d+)(?=\s|$)/.exec(line);
  if (!verts || Number(verts[1]) < 1200)
    throw new Error('CC0 original building meshes have too few vertices: ' + line);
  actual.vertices = Number(verts[1]);
  return actual;
}

module.exports = {
  requireKenneyCityReady, requireModernQuarterReady, requireCc0HeroReady,
  EXPECTED, MODULAR_KENNEY_EXPECTED
};
