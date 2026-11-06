// The app's price and territories (store/app-store/README.md): free, everywhere but the
// territories settings.json excludes, and new territories added automatically. Set once.
//
//   node tools/AppStoreRelease/availability.mjs            what is set
//   node tools/AppStoreRelease/availability.mjs --apply    set it

import { APP_APPLE_ID, AscError, get, getAll, post, settings } from './asc-api.mjs';

const app = { data: { type: 'apps', id: APP_APPLE_ID } };

async function show() {
  const schedule = await get(`/v1/apps/${APP_APPLE_ID}/appPriceSchedule?include=manualPrices,baseTerritory`).catch(error => {
    if (error instanceof AscError && error.status === 404) return null;
    throw error;
  });
  const prices = schedule?.included?.filter(item => item.type === 'appPrices') ?? [];
  console.log(`price schedule: ${schedule ? `${prices.length} manual price(s), base ${schedule.data.relationships?.baseTerritory?.data?.id}` : 'not set'}`);
  const availability = await get(`/v1/apps/${APP_APPLE_ID}/appAvailabilityV2`).catch(error => {
    if (error instanceof AscError && error.status === 404) return null;
    throw error;
  });
  if (!availability) {
    console.log('availability: not set');
    return;
  }
  const territories = await getAll(`/v2/appAvailabilities/${availability.data.id}/territoryAvailabilities?limit=200`);
  console.log(`availability: ${territories.filter(t => t.attributes.available).length} of ${territories.length} territories, new ones ${availability.data.attributes.availableInNewTerritories ? 'added' : 'not added'}`);
}

async function setFree() {
  const points = await getAll(`/v1/apps/${APP_APPLE_ID}/appPricePoints?filter[territory]=USA&limit=200`);
  const free = points.find(point => Number(point.attributes.customerPrice) === 0);
  if (!free) throw new Error('Apple listed no free price point.');
  await post('/v1/appPriceSchedules', {
    data: {
      type: 'appPriceSchedules',
      relationships: {
        app,
        baseTerritory: { data: { type: 'territories', id: 'USA' } },
        manualPrices: { data: [{ type: 'appPrices', id: '${free}' }] },
      },
    },
    included: [{
      type: 'appPrices',
      id: '${free}',
      attributes: { startDate: null, endDate: null },
      relationships: { appPricePoint: { data: { type: 'appPricePoints', id: free.id } } },
    }],
  });
}

async function setTerritories() {
  const territories = (await getAll('/v1/territories?limit=200')).filter(t => !settings.excludedTerritories.includes(t.id));
  await post('/v2/appAvailabilities', {
    data: {
      type: 'appAvailabilities',
      attributes: { availableInNewTerritories: true },
      relationships: {
        app,
        territoryAvailabilities: { data: territories.map(t => ({ type: 'territoryAvailabilities', id: `\${${t.id}}` })) },
      },
    },
    included: territories.map(t => ({
      type: 'territoryAvailabilities',
      id: `\${${t.id}}`,
      attributes: { available: true },
      relationships: { territory: { data: { type: 'territories', id: t.id } } },
    })),
  });
}

if (process.argv.includes('--apply')) {
  await setFree();
  await setTerritories();
}
await show();
