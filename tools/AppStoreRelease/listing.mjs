// Pushes the App Store listing kept in store/app-store/ to App Store Connect (store/app-store/README.md).
//
//   node tools/AppStoreRelease/listing.mjs            check every locale against Apple's rules
//   node tools/AppStoreRelease/listing.mjs --apply    write the app settings and every locale
//
// <locale>.json holds a locale's name, subtitle, keywords, promotional text and description, plus
// What's New from the second version on (the first has none); settings.json the app-wide answers.

import { readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { APP_APPLE_ID, editableRecords, get, getAll, listingDir, patch, post, settings } from './asc-api.mjs';

const LIMITS = { name: 30, subtitle: 30, keywords: 100, promotionalText: 170, description: 4000, whatsNew: 4000 };
const OPTIONAL = ['whatsNew'];
const apply = process.argv.includes('--apply');

const texts = Object.fromEntries(readdirSync(listingDir)
  .filter(file => file.endsWith('.json') && file !== 'settings.json')
  .map(file => [file.replace(/\.json$/, ''), JSON.parse(readFileSync(path.join(listingDir, file), 'utf8'))]));

/** Apple's limits and refusals, and what the other platform's listing must not bring along. */
function check(text) {
  const problems = [];
  for (const [field, limit] of Object.entries(LIMITS)) {
    const value = text[field];
    if (value === undefined && OPTIONAL.includes(field)) continue;
    if (typeof value !== 'string' || !value.trim()) { problems.push(`${field} is missing`); continue; }
    if ([...value].length > limit) problems.push(`${field} is ${[...value].length}/${limit} characters`);
    if (/\p{Extended_Pictographic}|️/u.test(value)) problems.push(`${field} has emoji`);
    if (value.includes('—')) problems.push(`${field} has an em dash`);
    if (/android|google|play store/i.test(value)) problems.push(`${field} names another platform`);
  }
  const words = text.keywords.split(',');
  if (words.some(word => word !== word.trim())) problems.push('keywords have spaces around commas');
  if (new Set(words.map(word => word.toLowerCase())).size !== words.length) problems.push('keywords repeat a word');
  const named = `${text.name} ${text.subtitle}`.toLowerCase();
  const inName = words.filter(word => !word.includes(' ') && named.includes(word.toLowerCase()));
  if (inName.length) problems.push(`keywords repeat the name or subtitle: ${inName.join(', ')}`);
  return problems;
}

let failed = false;
for (const [locale, text] of Object.entries(texts)) {
  const problems = check(text);
  console.log(`${locale}: ${problems.length ? problems.join('; ') : `ok (keywords ${[...text.keywords].length}/100, description ${[...text.description].length}/4000)`}`);
  failed ||= problems.length > 0;
}
if (failed) process.exit(1);
if (!apply) process.exit(0);
if (!APP_APPLE_ID) throw new Error('Set appleId in store/app-store/settings.json first.');

const { info, version } = await editableRecords();
const step = async (label, action) => {
  try {
    await action();
    console.log(`ok   ${label}`);
  } catch (error) {
    failed = true;
    console.log(`FAIL ${label}: ${error.message}`);
  }
};

await step('content rights', () => patch(`/v1/apps/${APP_APPLE_ID}`, {
  data: { type: 'apps', id: APP_APPLE_ID, attributes: { contentRightsDeclaration: settings.contentRightsDeclaration } },
}));
await step('categories', () => patch(`/v1/appInfos/${info.id}`, {
  data: {
    type: 'appInfos',
    id: info.id,
    relationships: Object.fromEntries(Object.entries(settings.categories)
      .map(([relation, id]) => [relation, { data: { type: 'appCategories', id } }])),
  },
}));
await step('age rating', async () => {
  const declaration = await get(`/v1/appInfos/${info.id}/ageRatingDeclaration`);
  await patch(`/v1/ageRatingDeclarations/${declaration.data.id}`, {
    data: { type: 'ageRatingDeclarations', id: declaration.data.id, attributes: settings.ageRating },
  });
});
await step('copyright', () => patch(`/v1/appStoreVersions/${version.id}`, {
  data: { type: 'appStoreVersions', id: version.id, attributes: { copyright: settings.copyright } },
}));

// The review contact is the team's own (the contact on its other app); it is never in this repository.
await step('review notes and contact', async () => {
  const contactApp = process.env.ASC_CONTACT_APP ?? '6823036828';
  const review = await get(`/v1/appStoreVersions/${version.id}/appStoreReviewDetail`).catch(error => {
    if (error.status === 404) return null;
    throw error;
  });
  if (review?.data) {
    await patch(`/v1/appStoreReviewDetails/${review.data.id}`, {
      data: { type: 'appStoreReviewDetails', id: review.data.id, attributes: { notes: settings.reviewNotes, demoAccountRequired: false } },
    });
    return;
  }
  const [other] = await getAll(`/v1/apps/${contactApp}/appStoreVersions?limit=1`);
  const contact = (await get(`/v1/appStoreVersions/${other.id}/appStoreReviewDetail`)).data.attributes;
  await post('/v1/appStoreReviewDetails', {
    data: {
      type: 'appStoreReviewDetails',
      attributes: {
        contactFirstName: contact.contactFirstName,
        contactLastName: contact.contactLastName,
        contactPhone: contact.contactPhone,
        contactEmail: contact.contactEmail,
        notes: settings.reviewNotes,
        demoAccountRequired: false,
      },
      relationships: { appStoreVersion: { data: { type: 'appStoreVersions', id: version.id } } },
    },
  });
});

const infoLocalizations = await getAll(`/v1/appInfos/${info.id}/appInfoLocalizations`);
for (const [locale, text] of Object.entries(texts)) {
  await step(locale, async () => {
    const infoAttributes = { name: text.name, subtitle: text.subtitle, privacyPolicyUrl: settings.privacyPolicyUrl };
    const existingInfo = infoLocalizations.find(l => l.attributes.locale === locale);
    if (existingInfo) {
      await patch(`/v1/appInfoLocalizations/${existingInfo.id}`, { data: { type: 'appInfoLocalizations', id: existingInfo.id, attributes: infoAttributes } });
    } else {
      await post('/v1/appInfoLocalizations', {
        data: { type: 'appInfoLocalizations', attributes: { locale, ...infoAttributes }, relationships: { appInfo: { data: { type: 'appInfos', id: info.id } } } },
      });
    }

    const versionAttributes = {
      description: text.description,
      keywords: text.keywords,
      promotionalText: text.promotionalText,
      supportUrl: settings.supportUrl,
      marketingUrl: settings.marketingUrl,
      ...(text.whatsNew ? { whatsNew: text.whatsNew } : {}),
    };

    // Adding a language to the app info creates its version localization as well, so it is
    // looked up only now.
    const versionLocalizations = await getAll(`/v1/appStoreVersions/${version.id}/appStoreVersionLocalizations`);
    const existingVersion = versionLocalizations.find(l => l.attributes.locale === locale);
    if (existingVersion) {
      await patch(`/v1/appStoreVersionLocalizations/${existingVersion.id}`, { data: { type: 'appStoreVersionLocalizations', id: existingVersion.id, attributes: versionAttributes } });
    } else {
      await post('/v1/appStoreVersionLocalizations', {
        data: { type: 'appStoreVersionLocalizations', attributes: { locale, ...versionAttributes }, relationships: { appStoreVersion: { data: { type: 'appStoreVersions', id: version.id } } } },
      });
    }
  });
}
process.exit(failed ? 1 : 0);
