#!/usr/bin/env node
'use strict';
// Run only against a GitHub Actions verified Unity WebGL artifact served on localhost.
// This is a real browser test; no synthetic canvas or mocked Unity runtime.
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.RUNNER_TEMP + '/nonation-playwright/node_modules/playwright');
const { criticalRuntimeMessages } = require('./webgl_runtime_guard');
const { requireKenneyCityReady, requireModernQuarterReady, requireCc0HeroReady } = require('./kenney_runtime_counts');

const target = process.env.NONATION_LOCAL_URL || 'http://127.0.0.1:8765/';
const meta = {
  sourceRun: process.env.SOURCE_RUN_ID || '',
  sourceSha: process.env.SOURCE_SHA || '',
  sourceArtifact: process.env.ARTIFACT_NAME || '',
  testedAt: new Date().toISOString(),
  browserVersion: '',
  ready: false,
  npcCount: null,
  canvas: false,
  fullscreenCanvas: false,
  moved: false,
  saved: false,
  restored: false,
  fatalErrors: [],
  kenneyArt: null,
  kenneyArtReloaded: null,
  modernFacades: null,
  modernFacadesReloaded: null,
  cc0HeroBuildings: null,
  cc0HeroBuildingsReloaded: null,
  premiumNature: null,
  premiumNatureReloaded: null,
  realHumanPilot: null,
  realHumanPilotReloaded: null,
  realCast: null,
  realCastReloaded: null,
  premiumLighting: false,
  heroQuarter: false,
  heroQuarterReloaded: false,
  stylizedStreet: false,
  stylizedStreetReloaded: false,
  showcaseScreenshot: false,
  waterfrontScreenshot: false,
  browserViewport: '1280x720',
};
const messages = [];
const allMessages = [];
const errors = [];

async function check() {
  const browser = await chromium.launch({
    headless: true,
    args: ['--no-sandbox', '--enable-webgl', '--ignore-gpu-blocklist',
      '--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader']
  });
  meta.browserVersion = browser.version();
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });

  page.on('console', msg => {
    const line = msg.text();
    messages.push(line);
    allMessages.push(line);
    if (line.includes('NONATION_') || msg.type() === 'error')
      console.log('WEBGL CONSOLE [' + msg.type() + '] ' + line.slice(0, 450));
    if (msg.type() === 'error') errors.push(line);
  });
  page.on('pageerror', err => {
    errors.push(err.message);
    console.error('WEBGL PAGE ERROR ' + err.message.slice(0, 500));
  });
  page.on('dialog', async dialog => {
    errors.push('Browser dialog: ' + dialog.message);
    await dialog.dismiss();
  });

  async function marker(word, timeoutMs) {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      // A missing glTF material generator or shader should fail immediately
      // with the original Unity error, not after a long marker timeout.
      const fatal = criticalRuntimeMessages([...errors, ...allMessages]);
      if (fatal.length)
        throw new Error('Fatal Unity browser error awaiting ' + word + ': ' +
          fatal.slice(0, 3).join(' | '));
      const visualFailure = allMessages.find(line =>
        line.includes('NONATION_HERO_QUARTER_ERROR') ||
        line.includes('NONATION_PREMIUM_NATURE_ERROR') ||
        line.includes('NONATION_MODULAR_CITY_ERROR'));
      if (visualFailure)
        throw new Error('Real 3D quarter construction failed: ' + visualFailure);
      const value = messages.find(msg => msg.includes(word));
      if (value) return value;
      await page.waitForTimeout(750);
    }
    throw new Error('No real Unity console marker ' + word +
      '. Recent messages: ' + messages.slice(-14).join(' | ').slice(0, 3000));
  }

  function verifiedRealCast() {
    // Only actual resident NPCs with Unity Avatar+SkinnedMeshRenderer
    // count. A static crowd prop, proxy, or source-only FBX never passes.
    const names = ['Female16', 'Male16'];
    const result = {};
    for (const name of names) {
      const line = messages.find(text =>
        text.includes('NONATION_REAL_HUMAN_CAST_OK asset=MIR/RocketboxCast/' + name));
      if (!line) return null;
      const match = /humanAvatar=(True|true)\s+skinnedVertices=(\d+)\s+bones=(\d+)/.exec(line);
      if (!match || Number(match[2]) < 1000 || Number(match[3]) < 40)
        throw new Error('Invalid real cast skeletal/animated Unity rig: ' + line);
      result[name] = { vertices: Number(match[2]), bones: Number(match[3]) };
    }
    return result;
  }

  function optionalRealHumanPilot() {
    const line = messages.find(msg => msg.includes('NONATION_REAL_HUMAN_PILOT_OK'));
    if (!line) return null; // Technical alpha still allows diagnostic NPCs.
    const match = /humanAvatar=(True|true)\s+skinnedVertices=(\d+)\s+bones=(\d+)\s+lodGroups=(\d+)/.exec(line);
    if (!match || Number(match[2]) < 1000 || Number(match[3]) < 40)
      throw new Error('Invalid real Humanoid pilot evidence: ' + line);
    return {
      asset: 'RocketboxPilot01', humanAvatar: true,
      skinnedVertices: Number(match[2]),
      bones: Number(match[3]),
      lodGroups: Number(match[4])
    };
  }

  try {
    const response = await page.goto(target, { waitUntil: 'domcontentloaded', timeout: 60000 });
    if (!response || response.status() !== 200)
      throw new Error('WebGL index did not return HTTP 200');

    const ready = await marker('NONATION_RUNTIME_READY', 180000);
    if (!ready.includes('scene=MIR_PlayableDistrict') ||
        !ready.includes('player=player-001'))
      throw new Error('Unexpected Unity scene/player in readiness signal: ' + ready);
    const match = /npcs=(\d+)/.exec(ready);
    if (!match || Number(match[1]) < 39)
      throw new Error('Expected at least 39 Unity NPCs: ' + ready);
    meta.ready = true;
    meta.npcCount = Number(match[1]);
    meta.realHumanPilot = optionalRealHumanPilot();
    meta.realCast = verifiedRealCast();
    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      const light = await marker('NONATION_PREMIUM_LIGHT_READY', 30000);
      if (!/msaa=4/.test(light) || !/shadowCascades=2/.test(light))
        throw new Error('Premium authored city lighting absent: ' + light);
      meta.premiumLighting = true;
    }
    if (meta.realHumanPilot)
      console.log('NONATION_REAL_HUMAN_CHROMIUM_VERIFIED phase=initial ' +
        JSON.stringify(meta.realHumanPilot));

    // The released city-art build must have instantiated real 3D GLB renderers;
    // static glTF file checks alone never establish actual Unity runtime usage.
    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      const modern = await marker('NONATION_MODULAR_CITY_READY', 30000);
      meta.modernFacades = requireModernQuarterReady(modern);
      const model = await marker('NONATION_QUATERNIUS_HERO_READY', 30000);
      meta.cc0HeroBuildings = requireCc0HeroReady(model);
      if (!modern.includes('cc0Hero=models'))
        throw new Error('Modular procedural facades still overlap real CC0 buildings');
      const line = await marker('NONATION_KENNEY_CITY_READY', 120000);
      meta.kenneyArt = requireKenneyCityReady(line, true);
      // Must be a real animated WebGL 3D scene using the original CC0
      // leaves/bark/flowers glTF and not default Unity spheres/capsules.
      const nature = await marker('NONATION_PREMIUM_NATURE_READY', 150000);
      if (!/\bmodels=6\b/.test(nature) || !/\btrees=7\b/.test(nature) ||
          !/\bbushes=12\b/.test(nature) || !/\bflowers=18\b/.test(nature) ||
          !/\bgrass=12\b/.test(nature) ||
          !/source=Quaternius-2025-CC0/.test(nature))
        throw new Error('Original authored CC0 tree and flower assets failed: ' + nature);
      meta.premiumNature = nature;
      console.log('NONATION_PREMIUM_NATURE_VERIFIED phase=initial');
      console.log('NONATION_MODERN_FACADES_VERIFIED phase=initial ' +
        JSON.stringify(meta.modernFacades) + ' kenney=' +
        JSON.stringify(meta.kenneyArt));
    }

    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      const detail = await marker('NONATION_HERO_QUARTER_READY', 30000);
      if (!/\bplaza=1\b/.test(detail) || !/\bwaterfront=1\b/.test(detail) ||
          !/\bstreets=1\b/.test(detail))
        throw new Error('Incomplete real hero quarter: ' + detail);
      meta.heroQuarter = true;
      console.log('NONATION_HERO_QUARTER_VERIFIED phase=initial');
      const details = await marker('NONATION_STYLIZED_STREET_READY', 30000);
      if (!/\bgardens=2\b/.test(details) || !/\bdeliveries=2\b/.test(details) || !/\bpark=1\b/.test(details))
        throw new Error('Stylized 3D diorama detail integration incomplete: ' + details);
      meta.stylizedStreet = true;
      console.log('NONATION_STYLIZED_STREET_VERIFIED phase=initial');
    }

    const canvas = page.locator('canvas').first();
    await canvas.waitFor({ state: 'visible', timeout: 15000 });
    const rect = await canvas.boundingBox();
    if (!rect || rect.width < 200 || rect.height < 200)
      throw new Error('Unity WebGL canvas is too small or not rendered');
    meta.canvas = true;
    // A passing Canvas element does not prove a useful game viewport.
    // Release template must fill the 1280x720 live Chromium screenshot.
    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      const viewport = page.viewportSize();
      if (!viewport || Math.abs(rect.width - viewport.width) > 4 ||
          Math.abs(rect.height - viewport.height) > 4)
        throw new Error('WebGL canvas does not fill browser viewport: ' +
          JSON.stringify({ rect, viewport }));
      if (await page.locator('#unity-footer').count() !== 0)
        throw new Error('Old Unity footer still present in released page');
      meta.fullscreenCanvas = true;
      console.log('NONATION_WEBGL_FULLSCREEN_CANVAS_VERIFIED ' +
        Math.round(rect.width) + 'x' + Math.round(rect.height));
    }

    await canvas.click({ position: { x: Math.floor(rect.width * 0.75),
                                     y: Math.floor(rect.height * 0.6) } });
    await page.keyboard.down('w');
    await page.waitForTimeout(1800);
    await page.keyboard.up('w');
    await marker('NONATION_PLAYER_MOVED', 15000);
    meta.moved = true;

    await marker('NONATION_SAVE_WRITTEN', 60000);
    meta.saved = true;
    await page.screenshot({ path: 'nonation-browser-proof.png', timeout: 20000 });

    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      // Capture the exact approved-angle live 3D composition, never an overlay.
      await page.keyboard.press('v');
      await marker('NONATION_SHOWCASE_CAMERA_READY active=1', 10000);
      await page.waitForTimeout(1500);
      await page.screenshot({ path: 'nonation-browser-showcase.png', timeout: 20000 });
      meta.showcaseScreenshot = true;
      await page.keyboard.press('v');
      await marker('NONATION_SHOWCASE_CAMERA_READY active=0', 10000);
      // Independent real 3D promenade view: capture sea, cycle lane, cafe
      // frontage and waterfront furniture without altering gameplay.
      await page.keyboard.press('b');
      await marker('NONATION_WATERFRONT_CAMERA_READY active=1', 10000);
      await page.waitForTimeout(1400);
      await page.screenshot({ path: 'nonation-browser-waterfront.png', timeout: 20000 });
      meta.waterfrontScreenshot = true;
      await page.keyboard.press('b');
      await marker('NONATION_WATERFRONT_CAMERA_READY active=0', 10000);
    }

    messages.length = 0;
    await page.reload({ waitUntil: 'domcontentloaded', timeout: 60000 });
    await marker('NONATION_SAVE_RESTORED', 90000);
    await marker('NONATION_RUNTIME_READY', 90000);
    meta.realHumanPilotReloaded = optionalRealHumanPilot();
    meta.realCastReloaded = verifiedRealCast();
    if (meta.realHumanPilotReloaded)
      console.log('NONATION_REAL_HUMAN_CHROMIUM_VERIFIED phase=reload ' +
        JSON.stringify(meta.realHumanPilotReloaded));
    // A successful restore is not enough: glTF meshes must also load again.
    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      const modern = await marker('NONATION_MODULAR_CITY_READY', 30000);
      meta.modernFacadesReloaded = requireModernQuarterReady(modern);
      const model = await marker('NONATION_QUATERNIUS_HERO_READY', 30000);
      meta.cc0HeroBuildingsReloaded = requireCc0HeroReady(model);
      if (!modern.includes('cc0Hero=models'))
        throw new Error('Restored CC0 FBX source is missing or overlapped');
      const line = await marker('NONATION_KENNEY_CITY_READY', 120000);
      meta.kenneyArtReloaded = requireKenneyCityReady(line, true);
      const nature = await marker('NONATION_PREMIUM_NATURE_READY', 150000);
      if (!/\bmodels=6\b/.test(nature) || !/\btrees=7\b/.test(nature) ||
          !/\bbushes=12\b/.test(nature) || !/\bflowers=18\b/.test(nature) ||
          !/\bgrass=12\b/.test(nature) ||
          !/source=Quaternius-2025-CC0/.test(nature))
        throw new Error('Original CC0 garden meshes disappeared on reload: ' + nature);
      meta.premiumNatureReloaded = nature;
      console.log('NONATION_PREMIUM_NATURE_VERIFIED phase=reload');
      console.log('NONATION_MODERN_FACADES_VERIFIED phase=reload ' +
        JSON.stringify(meta.modernFacadesReloaded) + ' kenney=' +
        JSON.stringify(meta.kenneyArtReloaded));
    }
    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      const detail = await marker('NONATION_HERO_QUARTER_READY', 30000);
      if (!/\bplaza=1\b/.test(detail) || !/\bwaterfront=1\b/.test(detail) ||
          !/\bstreets=1\b/.test(detail))
        throw new Error('Hero quarter not restored: ' + detail);
      meta.heroQuarterReloaded = true;
      console.log('NONATION_HERO_QUARTER_VERIFIED phase=reload');
      const details = await marker('NONATION_STYLIZED_STREET_READY', 30000);
      if (!/\bgardens=2\b/.test(details) || !/\bdeliveries=2\b/.test(details) || !/\bpark=1\b/.test(details))
        throw new Error('Stylized 3D diorama not restored after reload: ' + details);
      meta.stylizedStreetReloaded = true;
      console.log('NONATION_STYLIZED_STREET_VERIFIED phase=reload');
    }
    meta.restored = true;
    // Save both ACTUAL raw showcase/waterfront screenshots first, even if
    // the rig check fails; this creates honest visual-failure evidence.
    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      if (!meta.realHumanPilot || !meta.realHumanPilotReloaded ||
          !meta.realCast || !meta.realCastReloaded)
        throw new Error('VISUAL_GATE_BLOCKED: three distinct real skinned ' +
          'Humanoids with CC0 locomotion were NOT proven in Chromium');
      console.log('NONATION_PREMIUM_HUMANOIDS_VERIFIED uniqueAdults=3 ' +
        'pilot=MicrosoftMale12 cast=MicrosoftMale16,MicrosoftFemale16');
    }

    const fatal = criticalRuntimeMessages([...errors, ...allMessages]);
    if (fatal.length) throw new Error('Fatal Unity browser error: ' + fatal.slice(0, 3).join(' | '));
    if (allMessages.some(line => line.includes('NONATION_HERO_QUARTER_ERROR') ||
        line.includes('NONATION_MODULAR_CITY_ERROR')))
      throw new Error('City geometry fallback was activated during gameplay');
    console.log('PASS: real Unity WebGL runtime, 39+ NPC, canvas, W movement, save and reload.');
  } catch (error) {
    meta.fatalErrors = criticalRuntimeMessages([...errors, ...allMessages]).slice(-10).map(v => v.slice(0, 500));
    meta.failure = String(error && error.message || error).slice(0, 1600);
    console.error('WEBGL BROWSER SMOKE FAILURE:', meta.failure);
    try { await page.screenshot({ path: 'nonation-browser-failure.png', fullPage: true }); }
    catch (screenshotError) { console.error('Screenshot unavailable:', String(screenshotError).slice(0, 200)); }
    throw error;
  } finally {
    fs.writeFileSync('nonation-browser-proof.json', JSON.stringify(meta, null, 2));
    await browser.close();
  }
}
check().catch(err => { console.error(String(err).slice(0, 800)); process.exitCode = 1; });
