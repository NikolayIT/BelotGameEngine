// Sets up App Store signing for .github/workflows/ios.yml through the App Store Connect API.
//
//   node tools/AppStoreRelease/setup-signing.mjs [--no-secrets]
//
// Safe to run again at any time, and needed once a year, when the certificate or profile expires:
//   1. registers the bundle ID com.nksolutions.belot if it is missing;
//   2. uses the team's Apple Distribution certificate in the keys folder (asc-api.mjs), which
//      the team's other app issued; Apple allows only a few per team, so this never makes one;
//   3. recreates the "Belot App Store" provisioning profile for that certificate, under artifacts/;
//   4. creates the GitHub environment app-store (master only, with the owner as its required
//      reviewer, because this repository is public and has other writers) and sets its secrets.

import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { BUNDLE_ID, credentials, del, get, keysDir, post, repoRoot } from './asc-api.mjs';

const BUNDLE_NAME = 'Belot';
const PROFILE_NAME = 'Belot App Store';
const GITHUB_REPO = 'NikolayIT/BelotGameEngine';
const ENVIRONMENT = 'app-store';
const REVIEWER = 'NikolayIT';
const RENEW_BEFORE_MS = 30 * 24 * 60 * 60 * 1000;

const outDir = path.join(repoRoot, 'artifacts', 'app-store-signing');
const files = {
  certificateState: path.join(keysDir, 'signing.json'),
  p12: path.join(keysDir, 'distribution.p12'),
  p12Password: path.join(keysDir, 'distribution-p12-password.txt'),
  profile: path.join(outDir, 'belot-app-store.mobileprovision'),
};

async function ensureBundleId() {
  const found = await get(`/v1/bundleIds?filter[identifier]=${BUNDLE_ID}&limit=200`);
  const existing = found.data.find(bundle => bundle.attributes.identifier === BUNDLE_ID);
  if (existing) {
    console.log(`Bundle ID ${BUNDLE_ID} is registered (${existing.id}).`);
    return existing;
  }
  const created = await post('/v1/bundleIds', {
    data: { type: 'bundleIds', attributes: { identifier: BUNDLE_ID, name: BUNDLE_NAME, platform: 'IOS' } },
  });
  console.log(`Registered bundle ID ${BUNDLE_ID} (${created.data.id}).`);
  return created.data;
}

async function teamCertificate() {
  for (const file of Object.values(files).slice(0, 3)) {
    if (!existsSync(file)) throw new Error(`Missing ${file}: run the Gods & Magic setup-signing.mjs first, it issues the team's certificate.`);
  }
  const { certificateId } = JSON.parse(readFileSync(files.certificateState, 'utf8'));
  const certificate = (await get(`/v1/certificates/${certificateId}`)).data;
  const expires = Date.parse(certificate.attributes.expirationDate);
  if (expires - Date.now() < RENEW_BEFORE_MS) {
    throw new Error(`The certificate expires on ${certificate.attributes.expirationDate}: renew it with the Gods & Magic setup-signing.mjs first.`);
  }
  console.log(`Using certificate "${certificate.attributes.name}" (expires ${certificate.attributes.expirationDate}).`);
  return certificate;
}

async function recreateProfile(bundle, certificate) {
  const found = await get(`/v1/profiles?filter[name]=${encodeURIComponent(PROFILE_NAME)}&limit=200`);
  for (const old of found.data.filter(profile => profile.attributes.name === PROFILE_NAME)) {
    await del(`/v1/profiles/${old.id}`);
  }
  const created = await post('/v1/profiles', {
    data: {
      type: 'profiles',
      attributes: { name: PROFILE_NAME, profileType: 'IOS_APP_STORE' },
      relationships: {
        bundleId: { data: { type: 'bundleIds', id: bundle.id } },
        certificates: { data: [{ type: 'certificates', id: certificate.id }] },
      },
    },
  });
  mkdirSync(outDir, { recursive: true });
  writeFileSync(files.profile, Buffer.from(created.data.attributes.profileContent, 'base64'));
  console.log(`Created profile "${PROFILE_NAME}" (expires ${created.data.attributes.expirationDate}).`);
}

function gh(args, input) {
  const result = spawnSync('gh', args, { input, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] });
  if (result.status !== 0) throw new Error(`gh ${args.slice(0, 3).join(' ')} failed: ${result.stderr}`);
  return result.stdout;
}

function ensureEnvironment() {
  const reviewerId = Number(gh(['api', `users/${REVIEWER}`, '--jq', '.id']).trim());
  gh(['api', '-X', 'PUT', `repos/${GITHUB_REPO}/environments/${ENVIRONMENT}`, '--input', '-'], JSON.stringify({
    reviewers: [{ type: 'User', id: reviewerId }],
    deployment_branch_policy: { protected_branches: false, custom_branch_policies: true },
  }));
  const policies = JSON.parse(gh(['api', `repos/${GITHUB_REPO}/environments/${ENVIRONMENT}/deployment-branch-policies`]));
  if (!policies.branch_policies.some(policy => policy.name === 'master')) {
    gh(['api', '-X', 'POST', `repos/${GITHUB_REPO}/environments/${ENVIRONMENT}/deployment-branch-policies`, '-f', 'name=master', '-f', 'type=branch']);
  }
  console.log(`Environment ${ENVIRONMENT}: master only, reviewer ${REVIEWER}.`);
}

function setSecret(name, value) {
  gh(['secret', 'set', name, '--env', ENVIRONMENT, '--repo', GITHUB_REPO], value);
  console.log(`Set secret ${name}.`);
}

const bundle = await ensureBundleId();
const certificate = await teamCertificate();
await recreateProfile(bundle, certificate);

if (!process.argv.includes('--no-secrets')) {
  ensureEnvironment();
  setSecret('IOS_DIST_CERT_P12_BASE64', readFileSync(files.p12).toString('base64'));
  setSecret('IOS_DIST_CERT_PASSWORD', readFileSync(files.p12Password, 'utf8').trim());
  setSecret('IOS_PROFILE_BASE64', readFileSync(files.profile).toString('base64'));
  setSecret('ASC_KEY_ID', credentials.keyId);
  setSecret('ASC_ISSUER_ID', credentials.issuerId);
  setSecret('ASC_KEY_P8_BASE64', Buffer.from(credentials.pem).toString('base64'));
}
console.log('Done.');
