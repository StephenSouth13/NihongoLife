// Browser release check of the WebGL build (Core/WebQaBridge.cs, only with ?qa=1): plays, walks into every area through the game's
// own doors, and per area reads missing scripts, broken materials, frame rate, memory, area packs held and Unity errors; one
// screenshot per area. At most two packs stay in memory (the town and the area the player is in).
// Usage (serve Build/Release/WebGL on 127.0.0.1:8767 first):
//   node <browser-automation skill>/browser.mjs "http://127.0.0.1:8767/?qa=1" --script Tools/release/web_qa_tour.mjs --timeout 900000
// Screenshots go to NL_WEBQA_DIR (default D:/VTC_Academy/NihongoLife/_work/webqa).
const OUT = process.env.NL_WEBQA_DIR || 'D:/VTC_Academy/NihongoLife/_work/webqa';
const CITY = '90_TestSandbox';
// scene | spawn, in the order a player could walk them (zone to zone through SceneFlowController.TransferZone).
const AREAS = [
  ['30_SushiRestaurant', 'sushi_entrance'], ['35_HibariClinic', 'clinic_entrance'], ['40_ HIBARICLASS', 'school_entrance'],
  ['45_HomeBedroom', 'home_bedroom'], ['50_GameCenter', 'game_center_entrance'], ['55_MusicShop', 'music_shop_entrance'],
  ['56_FashionStore', 'fashion_store_entrance'], ['20_StationDistrict', 'station_entrance'], ['60_MidoriIsland', 'midori_station'],
  ['65_PhuQuocIsland', 'phuquoc_ganh_ngon'],
];

export default async function run(page) {
  // patchright runs page.evaluate in an isolated world that cannot see window.unityInstance; waitForFunction runs in the page's own world,
  // so every call into the page goes through it (the function returns a truthy value: true for an action, the data for a read).
  const main = async (fn, arg, timeout = 240000) => (await page.waitForFunction(fn, arg, { timeout })).jsonValue();
  const ready = () => main(() => !!(window.unityInstance && window.__nlQa)); // the bridge logs "[WebQa] log capture is on" once the player runs
  const send = async (method, arg = '') => {
    await ready();
    await main(([m, a]) => { window.unityInstance.SendMessage('NLWebQa', m, a); return true; }, [method, arg]);
  };
  const report = async () => {
    await main(() => { window.__nlQa.report = null; return true; });
    await send('Report');
    return JSON.parse(await main(() => window.__nlQa.report, undefined, 15000));
  };
  const reach = async scene => {
    for (let t = 0; t < 60; t++) {
      await page.waitForTimeout(2000);
      const r = await report();
      if (r.scene === scene && !r.loading && r.player) return r;
    }
    throw new Error('did not reach ' + scene);
  };

  await ready();
  await page.waitForTimeout(4000);
  await send('Play');
  const results = [];
  const visit = async (scene, go) => {
    const started = Date.now();
    await go();
    await reach(scene);
    await page.waitForTimeout(4000); // settle: streamed content, first frames
    const r = await report();
    await page.screenshot({ path: `${OUT}/qa_${scene.replace(/\s+/g, '')}.png` });
    results.push({ scene, seconds: Math.round((Date.now() - started) / 1000), missing: r.missing, broken: r.broken, fps: r.fps, allocMB: r.allocMB, packs: r.packs, error: r.error, firstMissing: r.firstMissing, firstBroken: r.firstBroken });
  };
  await visit(CITY, async () => {});
  for (const [scene, spawn] of AREAS) await visit(scene, () => send('Go', `${scene}|${spawn}`));
  // A left area gives its pack back (SceneContentLoader.ReleaseArea): going back downloads it again from the cache.
  await visit(AREAS[0][0], () => send('Go', `${AREAS[0][0]}|${AREAS[0][1]}`));
  await visit(CITY, () => send('Leave', `${CITY}|city_fashion_return`));

  const logs = await main(() => window.__nlQa.logs || []);
  // LogType: 0 Error, 1 Assert, 2 Warning, 4 Exception.
  const problems = logs.filter(([type]) => type === 0 || type === 1 || type === 4).map(([, m]) => m.split('\n')[0]);
  const warnings = [...new Set(logs.filter(([type]) => type === 2).map(([, m]) => m.split('\n')[0]))];
  return {
    areas: results.length,
    clean: results.every(r => r.missing === 0 && r.broken === 0 && !r.error && r.packs <= 2) && problems.length === 0,
    results,
    errors: [...new Set(problems)],
    warnings: warnings.slice(0, 20),
  };
}
