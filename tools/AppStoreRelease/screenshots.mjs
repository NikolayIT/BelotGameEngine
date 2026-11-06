// Uploads the iPhone screenshots to App Store Connect, replacing each locale's set
// (store/app-store/README.md).
//
//   node tools/AppStoreRelease/screenshots.mjs <shots folder> [--locales en-US,en-GB]
//
// <shots folder> is the workflow's app-store-screenshots artifact: one folder per app language
// with the 1320x2868 PNGs. settings.json maps each App Store locale to its folder and names the
// files, in the order the store shows them.

import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { del, editableRecords, getAll, patch, post, settings } from './asc-api.mjs';

// App Store Connect's slot for the 6.9" iPhones; one set covers every iPhone.
const DISPLAY_TYPE = 'APP_IPHONE_67';

const args = process.argv.slice(2);
const localeArg = args.includes('--locales') ? args[args.indexOf('--locales') + 1].split(',') : null;
const shotsDir = args.find((arg, i) => !arg.startsWith('--') && args[i - 1] !== '--locales');
if (!shotsDir) throw new Error('Usage: screenshots.mjs <shots folder> [--locales en-US,en-GB]');

const locales = localeArg ?? Object.keys(settings.screenshotFolders);
const { version } = await editableRecords();
const localizations = await getAll(`/v1/appStoreVersions/${version.id}/appStoreVersionLocalizations`);

async function uploadSet(locale, folder) {
  const localization = localizations.find(l => l.attributes.locale === locale);
  if (!localization) throw new Error('no such localization; run listing.mjs --apply first');

  const sets = await getAll(`/v1/appStoreVersionLocalizations/${localization.id}/appScreenshotSets`);
  let set = sets.find(s => s.attributes.screenshotDisplayType === DISPLAY_TYPE);
  if (set) {
    for (const shot of await getAll(`/v1/appScreenshotSets/${set.id}/appScreenshots`)) await del(`/v1/appScreenshots/${shot.id}`);
  } else {
    set = (await post('/v1/appScreenshotSets', {
      data: {
        type: 'appScreenshotSets',
        attributes: { screenshotDisplayType: DISPLAY_TYPE },
        relationships: { appStoreVersionLocalization: { data: { type: 'appStoreVersionLocalizations', id: localization.id } } },
      },
    })).data;
  }

  // Reserve, upload the parts Apple asks for, then commit with the checksum.
  const ids = [];
  for (const name of settings.screenshots) {
    const bytes = readFileSync(path.join(folder, name));
    const reservation = (await post('/v1/appScreenshots', {
      data: {
        type: 'appScreenshots',
        attributes: { fileName: name, fileSize: bytes.length },
        relationships: { appScreenshotSet: { data: { type: 'appScreenshotSets', id: set.id } } },
      },
    })).data;
    for (const operation of reservation.attributes.uploadOperations) {
      const headers = Object.fromEntries((operation.requestHeaders ?? []).map(h => [h.name, h.value]));
      const part = bytes.subarray(operation.offset, operation.offset + operation.length);
      const response = await fetch(operation.url, { method: operation.method, headers, body: part });
      if (!response.ok) throw new Error(`uploading ${name} failed with ${response.status}`);
    }
    await patch(`/v1/appScreenshots/${reservation.id}`, {
      data: { type: 'appScreenshots', id: reservation.id, attributes: { uploaded: true, sourceFileChecksum: createHash('md5').update(bytes).digest('hex') } },
    });
    ids.push(reservation.id);
  }
  await patch(`/v1/appScreenshotSets/${set.id}/relationships/appScreenshots`, { data: ids.map(id => ({ type: 'appScreenshots', id })) });

  // Apple processes every image after its upload.
  for (let attempt = 0; attempt < 75; attempt++) {
    const shots = await getAll(`/v1/appScreenshotSets/${set.id}/appScreenshots`);
    const states = shots.map(shot => shot.attributes.assetDeliveryState?.state);
    if (states.includes('FAILED')) throw new Error(`Apple could not process ${shots.filter(s => s.attributes.assetDeliveryState?.state === 'FAILED').map(s => s.attributes.fileName).join(', ')}`);
    if (shots.length === ids.length && states.every(state => state === 'COMPLETE')) return shots.length;
    await new Promise(resolve => setTimeout(resolve, 4000));
  }
  throw new Error('still processing; read assetDeliveryState before retrying, a retry starts the set over');
}

let failed = false;
for (const locale of locales) {
  try {
    const count = await uploadSet(locale, path.join(shotsDir, settings.screenshotFolders[locale]));
    console.log(`ok   ${locale}: ${count} screenshots`);
  } catch (error) {
    failed = true;
    console.log(`FAIL ${locale}: ${error.message}`);
  }
}
process.exit(failed ? 1 : 0);
