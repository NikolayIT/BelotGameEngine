// A small App Store Connect API client for the scripts in this folder.
//
// This repository is public, so the team's key stays outside it: AuthKey_<KEY_ID>.p8 and asc.json
// ({ "keyId", "issuerId" }) in the folder BELOT_APPLE_KEYS names, by default the Gods & Magic
// repository's ignored keys/apple beside this one. The key needs the Admin or App Manager role.

import { createPrivateKey, sign } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
export const keysDir = process.env.BELOT_APPLE_KEYS ?? path.resolve(repoRoot, '..', 'GodsAndMagic', 'keys', 'apple');
export const BUNDLE_ID = 'com.nksolutions.belot';

export const listingDir = path.join(repoRoot, 'store', 'app-store');
export const settings = JSON.parse(readFileSync(path.join(listingDir, 'settings.json'), 'utf8'));

/** Belot's Apple ID in App Store Connect (App Information). */
export const APP_APPLE_ID = settings.appleId;

/** The version the app's project names; the App Store version must equal the build's. */
export const appVersion = /<ApplicationDisplayVersion>([^<]+)</.exec(
  readFileSync(path.join(repoRoot, 'src', 'UI', 'Belot.UI', 'Belot.UI.csproj'), 'utf8'))[1];

function loadCredentials() {
  const configPath = path.join(keysDir, 'asc.json');
  const config = existsSync(configPath) ? JSON.parse(readFileSync(configPath, 'utf8')) : {};
  const keyId = process.env.ASC_KEY_ID ?? config.keyId;
  const issuerId = process.env.ASC_ISSUER_ID ?? config.issuerId;
  if (!keyId || !issuerId) throw new Error(`No App Store Connect key: ${configPath} needs keyId and issuerId.`);
  return { keyId, issuerId, pem: readFileSync(path.join(keysDir, `AuthKey_${keyId}.p8`), 'utf8') };
}

export const credentials = loadCredentials();
const privateKey = createPrivateKey(credentials.pem);
const base64Url = value => Buffer.from(value).toString('base64url');
let cached = { token: null, expiresAt: 0 };

function token() {
  const now = Math.floor(Date.now() / 1000);
  if (cached.token && cached.expiresAt - now > 60) return cached.token;
  const expiresAt = now + 900;
  const header = base64Url(JSON.stringify({ alg: 'ES256', kid: credentials.keyId, typ: 'JWT' }));
  const claims = base64Url(JSON.stringify({ iss: credentials.issuerId, iat: now, exp: expiresAt, aud: 'appstoreconnect-v1' }));
  const signature = sign('sha256', Buffer.from(`${header}.${claims}`), { key: privateKey, dsaEncoding: 'ieee-p1363' });
  cached = { token: `${header}.${claims}.${signature.toString('base64url')}`, expiresAt };
  return cached.token;
}

export class AscError extends Error {
  constructor(method, apiPath, status, body) {
    const details = (body?.errors ?? []).map(e => `${e.code ?? ''} ${e.detail ?? e.title ?? ''}`.trim()).join('; ');
    super(`${method} ${apiPath} -> ${status} ${details}`.trim());
    this.status = status;
    this.body = body;
  }
}

export async function api(method, apiPath, body) {
  const url = apiPath.startsWith('http') ? apiPath : `https://api.appstoreconnect.apple.com${apiPath}`;
  const send = () => fetch(url, {
    method,
    headers: { Authorization: `Bearer ${token()}`, ...(body ? { 'Content-Type': 'application/json' } : {}) },
    body: body ? JSON.stringify(body) : undefined,
  });

  // A read may be repeated after a dropped connection; a write may have gone through.
  let response;
  for (let attempt = 1; !response; attempt++) {
    try {
      response = await send();
    } catch (error) {
      if (method !== 'GET' || attempt === 4) throw error;
      await new Promise(resolve => setTimeout(resolve, 2000 * attempt));
    }
  }
  if (response.status === 204) return null;
  const text = await response.text();
  const json = text ? JSON.parse(text) : null;
  if (!response.ok) throw new AscError(method, apiPath, response.status, json);
  return json;
}

export const get = apiPath => api('GET', apiPath);
export const post = (apiPath, body) => api('POST', apiPath, body);
export const patch = (apiPath, body) => api('PATCH', apiPath, body);
export const del = apiPath => api('DELETE', apiPath);

/** Every page of a list endpoint. */
export async function getAll(apiPath) {
  const items = [];
  let next = apiPath;
  while (next) {
    const page = await get(next);
    items.push(...(page.data ?? []));
    next = page.links?.next ?? null;
  }
  return items;
}

/**
 * The App Store version still being edited and its App Info record. A live app has two of
 * those, the one on sale and the editable one, and Apple refuses edits to the first.
 */
export async function editableRecords() {
  const editable = ['PREPARE_FOR_SUBMISSION', 'READY_FOR_REVIEW', 'DEVELOPER_REJECTED', 'REJECTED', 'METADATA_REJECTED', 'INVALID_BINARY'];
  const versions = await getAll(`/v1/apps/${APP_APPLE_ID}/appStoreVersions`);
  const version = versions.find(v => editable.includes(v.attributes.appStoreState));
  if (!version) throw new Error('No App Store version is open for editing; create the next version first.');
  const infos = await getAll(`/v1/apps/${APP_APPLE_ID}/appInfos`);
  const info = infos.find(i => editable.includes(i.attributes.state));
  if (!info) throw new Error('No App Info record is open for editing.');
  return { info, version };
}
