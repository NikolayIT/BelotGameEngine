// Belot release operations through the official Google Play Developer API.
// Credentials stay outside the repository. Every command opens its own edit.
import assert from 'node:assert/strict';
import { createHash, createSign } from 'node:crypto';
import { readFile, readdir, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const [command, ...args] = process.argv.slice(2);
const options = new Map();
for (let i = 0; i < args.length; i += 2) {
  assert(args[i].startsWith('--') && args[i + 1], 'Use --name value arguments.');
  options.set(args[i].slice(2), args[i + 1]);
}
assert(['inspect', 'listings', 'verify', 'upload', 'complete'].includes(command), 'Command: inspect | listings | verify | upload | complete');
assert(options.has('key'), '--key must point to an existing service-account JSON file.');
const packageName = 'com.nksolutions.belot';
const api = 'https://androidpublisher.googleapis.com/androidpublisher/v3';
const media = 'https://androidpublisher.googleapis.com/upload/androidpublisher/v3';
const root = `${api}/applications/${packageName}/edits`;
const locales = ['bg', 'en-US'];
const receiptDir = path.resolve(repo, options.get('receipts') ?? 'artifacts/play-release-1.0');
await mkdir(receiptDir, { recursive: true });
const key = JSON.parse(await readFile(options.get('key'), 'utf8'));
const now = Math.floor(Date.now() / 1000);
const b64 = value => Buffer.from(JSON.stringify(value)).toString('base64url');
const jwt = `${b64({ alg: 'RS256', typ: 'JWT' })}.${b64({ iss: key.client_email, scope: 'https://www.googleapis.com/auth/androidpublisher', aud: 'https://oauth2.googleapis.com/token', iat: now, exp: now + 3600 })}`;
const signature = createSign('RSA-SHA256').update(jwt).sign(key.private_key).toString('base64url');
const auth = await fetch('https://oauth2.googleapis.com/token', { method: 'POST', body: new URLSearchParams({ grant_type: 'urn:ietf:params:oauth:grant-type:jwt-bearer', assertion: `${jwt}.${signature}` }) });
const token = await auth.json();
assert(auth.ok && token.access_token, `OAuth token exchange failed (${auth.status}).`);
async function request(method, url, body, type) {
  const response = await fetch(url, { method, headers: { Authorization: `Bearer ${token.access_token}`, ...(body === undefined ? {} : { 'Content-Type': type ?? 'application/json' }) }, body: body === undefined ? undefined : type ? body : JSON.stringify(body) });
  const text = await response.text();
  const result = text ? JSON.parse(text) : {};
  if (!response.ok) throw new Error(`${method} ${url.replace(api, '')}: ${response.status} ${result.error?.message ?? 'request failed'}`);
  return result;
}
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
async function listing(locale) {
  const folder = path.join(repo, 'store/google-play/listings', locale);
  const fields = [['title', 'title', 30], ['shortDescription', 'short-description', 80], ['fullDescription', 'full-description', 4000]];
  const result = { language: locale };
  for (const [name, file, limit] of fields) {
    const value = (await readFile(path.join(folder, `${file}.txt`), 'utf8')).replace(/^\uFEFF/, '').trim();
    assert(value.length > 0 && [...value].length <= limit, `${locale} ${name} exceeds its limit.`);
    result[name] = value;
  }
  return result;
}
async function images(locale) {
  const folder = path.join(repo, 'store/google-play/screenshots', locale);
  const screenshots = (await readdir(folder)).filter(name => /^0[1-8]-.*\.png$/.test(name)).sort();
  assert.equal(screenshots.length, 8, `${locale}: eight screenshots required.`);
  return { icon: [path.join(repo, 'store/google-play/graphics/icon.png')], featureGraphic: [path.join(repo, `store/google-play/graphics/feature-${locale}.png`)], phoneScreenshots: screenshots.map(file => path.join(folder, file)) };
}
// Read and validate all listing assets before opening a mutable edit.
const assets = [];
for (const locale of locales) {
  const groups = {};
  for (const [type, files] of Object.entries(await images(locale))) groups[type] = await Promise.all(files.map(async file => ({ file: path.relative(repo, file), bytes: await readFile(file) })));
  assets.push({ locale, listing: await listing(locale), groups });
}
const edit = await request('POST', root, {});
const editRoot = `${root}/${edit.id}`;
let committed = false;
const receipt = { command, packageName, atUtc: new Date().toISOString(), editId: edit.id };
async function verifyAssets() {
  const verified = [];
  for (const asset of assets) {
    const remoteListing = await request('GET', `${editRoot}/listings/${asset.locale}`);
    for (const [name, value] of Object.entries(asset.listing)) assert.equal(remoteListing[name], value, `${asset.locale} ${name}`);
    for (const [type, files] of Object.entries(asset.groups)) {
      const remote = (await request('GET', `${editRoot}/listings/${asset.locale}/${type}`)).images ?? [];
      assert.equal(remote.length, files.length, `${asset.locale} ${type} count`);
      files.forEach((file, i) => {
        assert.equal(remote[i].sha256, sha(file.bytes), `${asset.locale} ${type} image ${i + 1}`);
        verified.push({ locale: asset.locale, type, file: file.file, sha256: remote[i].sha256, id: remote[i].id });
      });
    }
  }
  return verified;
}
try {
  receipt.before = { details: await request('GET', `${editRoot}/details`), tracks: await request('GET', `${editRoot}/tracks`), bundles: await request('GET', `${editRoot}/bundles`), listings: await request('GET', `${editRoot}/listings`) };
  if (command === 'listings') {
    for (const asset of assets) {
      await request('PUT', `${editRoot}/listings/${asset.locale}`, asset.listing);
      for (const [type, files] of Object.entries(asset.groups)) {
        const imageRoot = `${editRoot}/listings/${asset.locale}/${type}`;
        const existing = (await request('GET', imageRoot)).images ?? [];
        if (existing.length === files.length && files.every((file, i) => sha(file.bytes) === existing[i].sha256)) continue;
        if (existing.length) await request('DELETE', imageRoot);
        for (const file of files) await request('POST', `${media}/applications/${packageName}/edits/${edit.id}/listings/${asset.locale}/${type}?uploadType=media`, file.bytes, 'image/png');
      }
      console.log(`Uploaded ${asset.locale}: text, icon, feature graphic, eight screenshots.`);
    }
    receipt.verified = await verifyAssets();
  } else if (command === 'verify') {
    receipt.verified = await verifyAssets();
  } else if (command === 'upload') {
    assert(options.has('bundle'), '--bundle is required.');
    const current = receipt.before.tracks.tracks?.find(track => track.track === 'production');
    assert.equal(current?.releases?.length ?? 0, 0, 'Refusing to replace an existing production release.');
    const bytes = await readFile(options.get('bundle'));
    receipt.upload = await request('POST', `${media}/applications/${packageName}/edits/${edit.id}/bundles?uploadType=media`, bytes, 'application/octet-stream');
    assert.equal(receipt.upload.versionCode, 1, 'This release must be version code 1.');
    assert.equal(receipt.upload.sha256, sha(bytes), 'Uploaded AAB hash mismatch.');
    receipt.bundleSha256 = sha(bytes);
    const notes = await Promise.all(locales.map(async language => ({ language, text: (await readFile(path.join(repo, 'store/google-play/listings', language, 'release-notes.txt'), 'utf8')).replace(/^\uFEFF/, '').trim() })));
    assert(notes.every(note => [...note.text].length <= 500), 'Release notes exceed 500 characters.');
    receipt.track = await request('PUT', `${editRoot}/tracks/production`, { track: 'production', releases: [{ name: '1.0', versionCodes: ['1'], status: 'draft', releaseNotes: notes }] });
  } else if (command === 'complete') {
    const track = await request('GET', `${editRoot}/tracks/production`);
    assert.equal(track.releases?.length, 1, 'Expected one prepared production release.');
    assert.deepEqual(track.releases[0].versionCodes, ['1'], 'Unexpected production version.');
    track.releases[0].status = 'completed';
    delete track.releases[0].userFraction;
    receipt.track = await request('PUT', `${editRoot}/tracks/production`, track);
  }
  if (['listings', 'upload', 'complete'].includes(command)) {
    await request('POST', `${editRoot}:validate`);
    // This application's Play API requires automatic review submission and
    // explicitly rejects changesNotSentForReview. Never cancel another review.
    receipt.commit = await request('POST', `${editRoot}:commit?changesInReviewBehavior=ERROR_IF_IN_REVIEW`);
    committed = true;
  }
  await writeFile(path.join(receiptDir, `${command}-${Date.now()}.json`), JSON.stringify(receipt, null, 2));
  console.log(JSON.stringify({ command, committed, verifiedImages: receipt.verified?.length, bundle: receipt.upload, track: receipt.track, details: command === 'inspect' ? receipt.before : undefined }, null, 2));
} catch (error) {
  receipt.error = String(error);
  await writeFile(path.join(receiptDir, `${command}-failed-${Date.now()}.json`), JSON.stringify(receipt, null, 2));
  throw error;
} finally {
  if (!committed) await request('DELETE', editRoot);
}
