'use strict';
// Shared failure classifier for the real Unity WebGL Chromium artifact smoke.
// Console warnings can contain Unity-native component stripping failures, so
// inspecting only Playwright pageerror/error events is insufficient.
const FATAL_RUNTIME = /\b(?:RuntimeError|out of memory|WebGL context lost|wasm trap|uncaught(?: exception)?|Missing runtime shader)\b|abort\(/i;
const COLLIDER_FAILURE = /(?:cannot|can't|can not|failed|missing|stripp?ed|doesn.t exist|not found|could not|unable to).{0,180}\b(?:BoxCollider|CapsuleCollider|SphereCollider)\b|\b(?:BoxCollider|CapsuleCollider|SphereCollider)\b.{0,180}(?:doesn.t exist|missing|stripp?ed|failed|not found|not allowed)/i;
function criticalRuntimeMessages(lines) {
  return [...new Set(lines.map(String).filter(line =>
    FATAL_RUNTIME.test(line) || COLLIDER_FAILURE.test(line)))];
}
module.exports = { criticalRuntimeMessages };
