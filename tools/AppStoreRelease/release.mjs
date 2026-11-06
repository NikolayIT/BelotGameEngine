// Takes the build that .github/workflows/ios.yml uploaded through App Review
// (store/app-store/README.md). The App Store version is the one Belot.UI.csproj names.
//
//   node tools/AppStoreRelease/release.mjs            where that version stands
//   node tools/AppStoreRelease/release.mjs --create   create it: everybody gets it once Apple approves it
//   node tools/AppStoreRelease/release.mjs --submit   attach its newest build, once processed, and send it to App Review

import { APP_APPLE_ID, appVersion as version, get, getAll, patch, post } from './asc-api.mjs';

const args = process.argv.slice(2);

const allVersions = () => getAll(`/v1/apps/${APP_APPLE_ID}/appStoreVersions`);
const findRecord = async () => (await allVersions()).find(record => record.attributes.versionString === version);

async function newestBuild() {
  const query = `filter[app]=${APP_APPLE_ID}&filter[preReleaseVersion.version]=${version}&sort=-uploadedDate&limit=1`;
  return (await get(`/v1/builds?${query}`)).data[0] ?? null;
}

const phasedRelease = async record => (await get(`/v1/appStoreVersions/${record.id}/appStoreVersionPhasedRelease`)).data;

async function create() {
  if (await findRecord()) {
    const record = await findRecord();
    await patch(`/v1/appStoreVersions/${record.id}`, {
      data: { type: 'appStoreVersions', id: record.id, attributes: { releaseType: 'AFTER_APPROVAL' } },
    });
    return;
  }

  // A new app's first version already exists under the name App Store Connect gave it.
  const draft = (await allVersions()).find(record => record.attributes.appStoreState === 'PREPARE_FOR_SUBMISSION');
  if (draft) {
    await patch(`/v1/appStoreVersions/${draft.id}`, {
      data: { type: 'appStoreVersions', id: draft.id, attributes: { versionString: version, releaseType: 'AFTER_APPROVAL' } },
    });
    return;
  }

  await post('/v1/appStoreVersions', {
    data: {
      type: 'appStoreVersions',
      attributes: { platform: 'IOS', versionString: version, releaseType: 'AFTER_APPROVAL' },
      relationships: { app: { data: { type: 'apps', id: APP_APPLE_ID } } },
    },
  });
}

async function submit() {
  const record = await findRecord();
  if (!record) throw new Error(`${version} does not exist; run --create first`);

  // Nothing short of everyone at once: a phased release or a manual one has to be undone by hand.
  if (record.attributes.releaseType !== 'AFTER_APPROVAL') throw new Error(`release type is ${record.attributes.releaseType}`);
  if (await phasedRelease(record)) throw new Error('a phased release is set on this version');

  const build = await newestBuild();
  if (build?.attributes.processingState !== 'VALID') {
    throw new Error(`no processed build of ${version} yet (${build ? `build ${build.attributes.version} is ${build.attributes.processingState}` : 'none uploaded'})`);
  }
  await patch(`/v1/appStoreVersions/${record.id}/relationships/build`, { data: { type: 'builds', id: build.id } });

  // One draft submission per app.
  const [draft] = await getAll(`/v1/apps/${APP_APPLE_ID}/reviewSubmissions?filter[platform]=IOS&filter[state]=READY_FOR_REVIEW`);
  const submission = draft ?? (await post('/v1/reviewSubmissions', {
    data: {
      type: 'reviewSubmissions',
      attributes: { platform: 'IOS' },
      relationships: { app: { data: { type: 'apps', id: APP_APPLE_ID } } },
    },
  })).data;

  // A run that failed at the last step left the version in the draft, and Apple refuses it twice.
  const items = await getAll(`/v1/reviewSubmissions/${submission.id}/items?include=appStoreVersion`);
  if (!items.some(item => item.relationships?.appStoreVersion?.data?.id === record.id)) {
    await post('/v1/reviewSubmissionItems', {
      data: {
        type: 'reviewSubmissionItems',
        relationships: {
          reviewSubmission: { data: { type: 'reviewSubmissions', id: submission.id } },
          appStoreVersion: { data: { type: 'appStoreVersions', id: record.id } },
        },
      },
    });
  }
  await patch(`/v1/reviewSubmissions/${submission.id}`, {
    data: { type: 'reviewSubmissions', id: submission.id, attributes: { submitted: true } },
  });
}

if (args.includes('--create')) await create();
if (args.includes('--submit')) await submit();

const record = await findRecord();
const build = await newestBuild();
console.log(`version ${version}: ${record ? `${record.attributes.appStoreState}, ${record.attributes.releaseType}` : 'not created'}`);
if (record) {
  const attached = (await get(`/v1/appStoreVersions/${record.id}/build`)).data;
  console.log(`  phased release: ${(await phasedRelease(record)) ? 'SET' : 'none'}; attached build: ${attached?.attributes.version ?? 'none'}`);
}
console.log(`newest build: ${build ? `${build.attributes.version} (${build.attributes.processingState})` : 'none uploaded'}`);
