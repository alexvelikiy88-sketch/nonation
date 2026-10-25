#!/usr/bin/env node
'use strict';
// Run only against a GitHub Actions verified Unity WebGL artifact served on localhost.
// This is a real browser test; no synthetic canvas or mocked Unity runtime.
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.RUNNER_TEMP + '/nonation-playwright/node_modules/playwright');
const { criticalRuntimeMessages } = require('./webgl_runtime_guard');
const { requireKenneyCityReady } = require('./kenney_runtime_counts');

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
  moved: false,
  saved: false,
  restored: false,
  fatalErrors: [],
  kenneyArt: null,
  kenneyArtReloaded: null,
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
      const value = messages.find(msg => msg.includes(word));
      if (value) return value;
      await page.waitForTimeout(750);
    }
    throw new Error('No real Unity console marker ' + word +
      '. Recent messages: ' + messages.slice(-14).join(' | ').slice(0, 3000));
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

    // The released city-art build must have instantiated real 3D GLB renderers;
    // static glTF file checks alone never establish actual Unity runtime usage.
    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      const line = await marker('NONATION_KENNEY_CITY_READY', 120000);
      meta.kenneyArt = requireKenneyCityReady(line);
      console.log('KENNEY_RUNTIME_3D_VERIFIED phase=initial ' +
        JSON.stringify(meta.kenneyArt));
    }

    const canvas = page.locator('canvas').first();
    await canvas.waitFor({ state: 'visible', timeout: 15000 });
    const rect = await canvas.boundingBox();
    if (!rect || rect.width < 200 || rect.height < 200)
      throw new Error('Unity WebGL canvas is too small or not rendered');
    meta.canvas = true;

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

    messages.length = 0;
    await page.reload({ waitUntil: 'domcontentloaded', timeout: 60000 });
    await marker('NONATION_SAVE_RESTORED', 90000);
    await marker('NONATION_RUNTIME_READY', 90000);
    // A successful restore is not enough: glTF meshes must also load again.
    if (process.env.NONATION_REQUIRE_KENNEY_ART === '1') {
      const line = await marker('NONATION_KENNEY_CITY_READY', 120000);
      meta.kenneyArtReloaded = requireKenneyCityReady(line);
      console.log('KENNEY_RUNTIME_3D_VERIFIED phase=reload ' +
        JSON.stringify(meta.kenneyArtReloaded));
    }
    meta.restored = true;

    const fatal = criticalRuntimeMessages([...errors, ...allMessages]);
    if (fatal.length) throw new Error('Fatal Unity browser error: ' + fatal.slice(0, 3).join(' | '));
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
