/* Offline by design: classic scripts + relative PNG URLs, no fetch/CDN/server requirement. */
(() => {
  'use strict';
  const data = window.NIHONGO_ASSETS;
  if (!data || !Array.isArray(data.entries)) {
    document.getElementById('asset-grid').textContent = 'Gallery data is unavailable. Keep index.html, gallery-data.js and gallery.js together.';
    return;
  }
  const $ = (id) => document.getElementById(id);
  const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (c) => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const bytes = (value) => value == null ? 'Not generated' : value >= 1048576 ? `${(value / 1048576).toFixed(1)} MiB` : `${(value / 1024).toFixed(1)} KiB`;
  const pct = (value) => value == null ? '—' : `${(value * 100).toFixed(1)}%`;
  const entries = data.entries;
  const byId = new Map(entries.map((row) => [row.id, row]));
  const state = {view:'gallery',category:'All',search:'',missing:false,status:'all',page:1,shortlistOnly:false,
    shop:'Agriculture',shopPage:1,shopSelected:null,shopState:'available',failurePage:1,failureSearch:'',dialog:null};
  let saved = [];
  try { saved = JSON.parse(localStorage.getItem('nihongo-life-c0-shortlist') || '[]'); } catch (_) { /* file:// storage can be unavailable */ }
  const shortlist = new Set(Array.isArray(saved) ? saved.filter((id) => byId.has(id)) : []);
  let filtered = [], shopFiltered = [], failureFiltered = [], toastTimer;
  const pageSize = 40, shopPageSize = 12, failurePageSize = 18;
  const icons = {'All':'▦','Agriculture':'♧','Seeds and Crops':'❋','Farming Tools':'⌁','Animals and Animal Products':'◉','Fashion':'◇','Technology':'▣','General Inventory':'▧','Unclassified':'○'};

  function toast(message) {
    $('toast').textContent = message;
    $('toast').classList.add('visible');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => $('toast').classList.remove('visible'), 2800);
  }
  function available(row) { return Boolean(row.imageUrl && row.image.valid && !row.browserImageFailed); }
  function isCategory(row, category) { return category === 'All' || row.category === category || (category === 'Agriculture' && row.shopCategory === 'Agriculture'); }
  function imageHtml(row, attrs = '') { return available(row) ? `<img src="${esc(row.imageUrl)}" alt="${esc(row.name)}" decoding="async" ${attrs}>` : ''; }
  function statusLabel(row) { return row.status === 'Accepted candidate' ? 'Technical pass' : row.status === 'Needs visual review' ? 'Review flags' : row.status; }
  function shortlistButton(row) {
    return `<button class="shortlist-button ${shortlist.has(row.id) ? 'selected' : ''}" data-shortlist="${esc(row.id)}" aria-label="${shortlist.has(row.id) ? 'Remove from' : 'Add to'} shortlist: ${esc(row.name)}" aria-pressed="${shortlist.has(row.id)}">${shortlist.has(row.id) ? '♥' : '♡'}</button>`;
  }
  function updateShortlist(id) {
    if (shortlist.has(id)) shortlist.delete(id); else shortlist.add(id);
    try { localStorage.setItem('nihongo-life-c0-shortlist', JSON.stringify([...shortlist])); } catch (_) { /* session-only fallback */ }
    $('shortlist-count').textContent = String(shortlist.size);
    renderGallery();
    if (state.view === 'shop') renderShop();
    if (state.dialog) renderDialog();
    toast(shortlist.has(id) ? 'Added to your shortlist. Approval remains pending.' : 'Removed from your shortlist.');
  }

  function renderCollections() {
    $('collections').innerHTML = ['All', ...data.categories].map((category) => {
      const count = entries.filter((r) => (state.missing || available(r)) && isCategory(r, category)).length;
      const sub = ['Seeds and Crops','Farming Tools','Animals and Animal Products'].includes(category);
      return `<button class="collection-button ${sub ? 'sub' : ''} ${state.category === category ? 'active' : ''}" data-category="${esc(category)}" aria-pressed="${state.category === category}"><span aria-hidden="true">${icons[category]}</span>${category === 'All' ? 'All assets' : esc(category)}<span class="category-count">${count}</span></button>`;
    }).join('');
  }

  function renderGallery() {
    const query = state.search.trim().toLocaleLowerCase();
    filtered = entries.filter((r) => (state.missing || available(r)) && isCategory(r, state.category)
      && (state.status === 'all' || r.status === state.status) && (!state.shortlistOnly || shortlist.has(r.id))
      && (!query || [r.name,r.id,r.sourcePath,r.iconPath,r.category].join(' ').toLocaleLowerCase().includes(query)));
    const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
    state.page = Math.min(state.page, pages);
    const subset = filtered.slice((state.page - 1) * pageSize, state.page * pageSize);
    $('collection-title').textContent = state.shortlistOnly ? 'Your shortlist' : state.category === 'All' ? state.missing ? 'All catalog candidates' : 'All generated icons' : state.category;
    $('result-count').textContent = `${filtered.length} ${state.missing ? 'candidates' : 'icons'}${query ? ` matching “${state.search}”` : ''} · owner approval pending`;
    $('asset-grid').innerHTML = subset.map((r) => {
      const exists = available(r);
      const placeholder = `<span>No PNG available</span><small>${r.browserImageFailed ? 'File could not be loaded' : r.renderStatus === 'failed' ? 'Render failed' : 'Not generated'}</small>`;
      return `<article class="asset-card ${exists ? '' : 'missing-card'}">${shortlistButton(r)}${exists && r.duplicateIds?.length ? `<span class="duplicate-badge">IDENTICAL PIXELS</span>` : ''}<button class="card-open" data-inspect="${esc(r.id)}" aria-label="Inspect ${esc(r.name)} ${esc(r.assetType)} ${esc(r.id.slice(-8))}"><div class="card-image preview-surface checkered ${exists ? '' : 'missing-preview'}">${exists ? imageHtml(r, 'loading="lazy"') : placeholder}</div><div class="card-info"><span class="card-name">${esc(r.name.replaceAll('_',' '))}</span><span class="card-category">${esc(r.category)}</span><span class="card-foot"><span class="render-label"><span class="status-dot ${exists ? '' : 'failed-dot'}"></span>${exists ? `${r.image.width} × ${r.image.height}` : r.renderStatus === 'failed' ? 'Failed' : 'Not queued'}</span><span class="${r.qualityWarnings.length ? 'quality-flag' : ''}">${exists ? bytes(r.image.bytes) : r.assetType.toUpperCase()}</span></span></div></button></article>`;
    }).join('');
    $('empty-gallery').hidden = Boolean(filtered.length);
    $('asset-grid').hidden = !filtered.length;
    $('page-info').textContent = filtered.length ? `Showing ${(state.page-1)*pageSize+1}–${Math.min(state.page*pageSize,filtered.length)} of ${filtered.length}` : 'No matches';
    $('page-number').textContent = `${state.page} / ${pages}`;
    $('previous-page').disabled = state.page === 1;
    $('next-page').disabled = state.page === pages;
    renderCollections();
  }

  function showView(view) {
    state.view = view;
    for (const name of ['gallery','shop','quality']) $(`${name}-view`).hidden = name !== view;
    document.querySelectorAll('[data-view]').forEach((b) => { b.classList.toggle('active',b.dataset.view === view); b.setAttribute('aria-pressed',String(b.dataset.view === view)); });
    $('view-label').textContent = {gallery:'Asset library',shop:'Shop presentation study',quality:'Quality & recovery'}[view];
    if (view === 'shop') renderShop();
    if (view === 'quality') renderQuality();
    if (view === 'gallery') renderGallery();
    history.replaceState(null,'',`#${view}`);
    window.scrollTo({top:0,behavior:'instant'});
  }

  function selectShop(id) { state.shopSelected = id; renderShop(); }
  function renderShop() {
    const tabs = ['Fashion','Agriculture','Technology','General Inventory'];
    $('shop-tabs').innerHTML = tabs.map((tab) => `<button role="tab" aria-selected="${state.shop === tab}" class="shop-tab ${state.shop === tab ? 'active' : ''}" data-shop-tab="${esc(tab)}">${esc(tab)}</button>`).join('');
    shopFiltered = entries.filter((r) => available(r) && r.shopCategory === state.shop && (!$('shop-shortlist-only').checked || shortlist.has(r.id)));
    const pages = Math.max(1,Math.ceil(shopFiltered.length / shopPageSize));
    state.shopPage = Math.min(state.shopPage,pages);
    if (!shopFiltered.some((r) => r.id === state.shopSelected)) state.shopSelected = shopFiltered[0]?.id || null;
    const subset = shopFiltered.slice((state.shopPage-1)*shopPageSize,state.shopPage*shopPageSize);
    $('shop-count').textContent = `${shopFiltered.length} real icon candidates`;
    $('shop-grid').className = `shop-grid state-${state.shopState}`;
    $('shop-grid').innerHTML = subset.map((r) => `<button class="shop-card ${state.shopSelected === r.id ? 'active' : ''}" data-shop-item="${esc(r.id)}" aria-pressed="${state.shopSelected === r.id}"><span class="preview-surface checkered">${imageHtml(r,'loading="lazy"')}</span><span class="shop-card-text"><span class="jp-label ${r.japanese ? '' : 'unassigned'}">${esc(r.japanese || '日本語名未設定')}</span><span class="english-label">${esc(r.name.replaceAll('_',' '))}</span><span class="shop-price ${r.priceYen == null ? 'unknown-price' : ''}">${r.priceYen == null ? 'Price not assigned · 価格未設定' : `¥ ${r.priceYen.toLocaleString()}`}</span></span></button>`).join('');
    $('shop-empty').hidden = shopFiltered.length !== 0;
    $('shop-empty-copy').textContent = state.shop === 'Fashion' ? 'No valid fashion icons exist in this delivery. This tab is intentionally empty.' : 'No real icon candidates match this collection and shortlist filter.';
    $('shop-page-info').textContent = `${state.shopPage} / ${pages}`;
    $('shop-previous').disabled = state.shopPage === 1;
    $('shop-next').disabled = state.shopPage === pages;
    const row = byId.get(state.shopSelected);
    $('selected-shop-item').hidden = !row;
    if (!row) { $('selected-shop-item').innerHTML = ''; return; }
    $('selected-shop-item').innerHTML = `<div class="preview-surface checkered">${imageHtml(row)}</div><span class="eyebrow">${esc(row.shopCategory)} / SELECTED CANDIDATE</span><h2>${esc(row.name.replaceAll('_',' '))}</h2><span class="jp-label ${row.japanese ? '' : 'unassigned'}">${esc(row.japanese || '日本語名未設定')}</span><p>${row.japanese ? 'Japanese label and price copied from an existing linked runtime item. English shown here is the source model name.' : 'English is the source model name. Japanese item label and production price remain unassigned.'}</p><div class="selected-price"><span>Price label</span><strong>${row.priceYen == null ? '¥ —' : `¥ ${row.priceYen.toLocaleString()}`}</strong></div><label class="state-control">Presentation state only<select id="presentation-state"><option value="available" ${state.shopState === 'available' ? 'selected' : ''}>Available candidate</option><option value="owned" ${state.shopState === 'owned' ? 'selected' : ''}>Owned · mock display</option><option value="unavailable" ${state.shopState === 'unavailable' ? 'selected' : ''}>Unavailable · mock display</option></select></label><span class="presentation-state ${state.shopState}">${state.shopState === 'owned' ? 'Owned — presentation only' : state.shopState === 'unavailable' ? 'Unavailable — presentation only' : 'Candidate — approval pending'}</span><p>No purchase action is implemented.</p><button class="primary-button secondary" data-inspect="${esc(row.id)}" style="width:100%">Inspect original asset ↗</button>`;
  }

  function renderQuality() {
    const summary = data.summary;
    const stats = [[summary.statusCounts['Accepted candidate'] || 0,'Technical passes','Visual/material approval pending'],[summary.statusCounts['Needs visual review'] || 0,'Review flags','Measured PNG review triggers'],[summary.failedRenderJobs,'Failed renders','No substitute images supplied']];
    $('quality-stats').innerHTML = stats.map(([n,label,detail]) => `<div class="stat"><span class="stat-label">${label}</span><strong class="stat-number">${n}</strong><span class="stat-detail">${detail}</span></div>`).join('');
    const mipmaps = summary.importIssueCounts['Mipmaps enabled for UI thumbnail'] || 0;
    $('import-finding').textContent = `${mipmaps} generated icon imports currently enable mipmaps. The shared texture postprocessor sets mipmaps and Crunch on Default textures; the observed default platform format must be checked before assuming compression.`;
    $('contact-sheets').innerHTML = data.contactSheets.map((sheet,i) => `<a href="../Reports/${esc(sheet.path)}" target="_blank" rel="noopener">Sheet ${String(i+1).padStart(2,'0')} <span>↗</span></a>`).join('');
    const failures = entries.filter((r) => r.failure);
    const query = state.failureSearch.trim().toLocaleLowerCase();
    failureFiltered = failures.filter((r) => !query || `${r.name} ${r.sourcePath} ${r.failure.classification}`.toLocaleLowerCase().includes(query));
    const pages = Math.max(1,Math.ceil(failureFiltered.length / failurePageSize));
    state.failurePage = Math.min(state.failurePage,pages);
    $('failure-count').textContent = `${failureFiltered.length} of ${failures.length} failures`;
    $('failure-table').innerHTML = failureFiltered.slice((state.failurePage-1)*failurePageSize,state.failurePage*failurePageSize).map((r) => {
      const alts = r.failure.alternatives.filter((a) => a.iconExists);
      return `<tr><td><strong>${esc(r.name)}</strong> · ${r.assetType.toUpperCase()}<small>${esc(r.sourcePath)}</small></td><td>${esc(r.failure.classification)}<small>${esc(r.failure.evidence)}</small></td><td>${alts.length ? `${alts.length} available alternate icon${alts.length > 1 ? 's' : ''}<small>Same pack / basename; equivalence unverified</small>${alts.slice(0,2).map((a) => `<button class="alternative-link" data-inspect="${esc(a.id)}">Inspect ${esc(byId.get(a.id)?.assetType.toUpperCase() || 'alternate')} ↗</button>`).join('')}` : 'No safe alternate identified'}</td><td><button class="quiet-button" data-inspect="${esc(r.id)}">Details ↗</button></td></tr>`;
    }).join('');
    if (!failureFiltered.length) $('failure-table').innerHTML = '<tr><td colspan="4">No failures match this search.</td></tr>';
    $('failure-page-info').textContent = `${state.failurePage} / ${pages}`;
    $('failure-previous').disabled = state.failurePage === 1;
    $('failure-next').disabled = state.failurePage === pages;
  }

  function openDialog(id) {
    if (!byId.has(id)) return;
    state.dialog = id;
    renderDialog();
    if (!$('asset-dialog').open) $('asset-dialog').showModal();
  }
  function dialogSequence() { return filtered.some((r) => r.id === state.dialog) ? filtered : entries; }
  function renderDialog() {
    const r = byId.get(state.dialog);
    if (!r) return;
    const exists = available(r);
    const mappingPass = Object.values(r.mappingChecks).every(Boolean);
    const duplicateText = r.duplicateIds?.length ? `${r.duplicateIds.length} other candidate(s) have identical decoded pixels; review format redundancy.` : 'No identical-pixel counterpart in this delivery.';
    const visualNote = r.manualReview.status || 'Owner and material review pending';
    const failed = r.failure;
    $('dialog-content').innerHTML = `<div class="dialog-layout"><section class="dialog-visual"><div class="preview-surface checkered ${exists ? '' : 'missing-preview'}">${exists ? imageHtml(r) : `<span>No PNG available</span><small>${esc(r.renderDetail)}</small>`}</div><div class="surface-controls"><button class="surface-button ${document.body.dataset.surface === 'light' ? 'active' : ''}" data-surface="light">Light</button><button class="surface-button ${document.body.dataset.surface === 'dark' ? 'active' : ''}" data-surface="dark">Dark</button><label><input data-dialog-checker type="checkbox" ${document.body.dataset.checker === 'true' ? 'checked' : ''}> Checkerboard</label></div>${exists ? `<div class="size-samples">${[32,48,64].map((size) => `<span class="size-sample">${imageHtml(r,`style="width:${size}px;height:${size}px"`)}${size}px UI sample</span>`).join('')}</div>` : ''}<p class="review-note">${exists ? 'Compare color, silhouette and small-size readability. This PNG preview cannot establish source texture bindings or correct model orientation.' : 'A missing icon remains missing. Existing alternative models are separate candidates, never substituted into this record.'}</p></section><section class="dialog-details"><span class="eyebrow">${esc(r.category)} / ${r.assetType.toUpperCase()}</span><h2 id="dialog-title">${esc(r.name.replaceAll('_',' '))}</h2><div class="pill-row"><span class="pill">${esc(statusLabel(r))}</span><span class="pill warm">OWNER APPROVAL PENDING</span>${r.duplicateIds?.length ? '<span class="pill warm">IDENTICAL PIXELS</span>' : ''}</div><div class="metrics-grid"><span><strong>${exists ? `${r.image.width} × ${r.image.height}` : '—'}</strong><small>PNG resolution</small></span><span><strong>${exists ? bytes(r.image.bytes) : '—'}</strong><small>File size</small></span><span><strong>${r.renderStatus}</strong><small>Render status</small></span></div><div class="detail-group"><p class="eyebrow">SOURCE MODEL / ASSET</p><code class="path-value" id="source-path-value">${esc(r.sourcePath)}</code><button class="quiet-button" data-copy="source">Copy asset path</button><p>${mappingPass ? 'Source GUID, file hash and manifest records agree.' : exists ? 'Source mapping requires review; inspect automated checks below.' : 'Source record preserved; no generated icon mapping claimed.'}</p></div><div class="detail-group"><p class="eyebrow">ICON OUTPUT PATH</p><code class="path-value" id="icon-path-value">${esc(r.iconPath)}</code><button class="quiet-button" data-copy="icon">Copy icon path</button></div><div class="detail-group"><p class="eyebrow">QUALITY / MEASURABLE EVIDENCE</p><p>Technical checks: ${r.technicalScore == null ? 'not applicable' : `${r.technicalScore}/100`} · transparent canvas: ${pct(r.image.transparentFraction)}</p><p>Subject extent: ${pct(r.image.maxSubjectExtent)} · smallest margin: ${pct(r.image.minMargin)}</p>${Object.entries(r.automatedChecks).map(([check,pass]) => `<p>${pass ? '✓' : '×'} ${esc(check.replace(/([A-Z])/g,' $1'))}</p>`).join('')}${r.qualityWarnings.map((w) => `<p class="quality-flag">Review: ${esc(w)}</p>`).join('')}<p>${esc(duplicateText)}</p></div><div class="detail-group"><p class="eyebrow">HUMAN REVIEW & CATEGORY EVIDENCE</p><p>${esc(visualNote)}. Owner approval: pending.</p><p>${esc(r.categoryEvidence)}</p>${r.contactSheet ? `<a class="text-link" style="padding-left:0" href="${esc(r.contactSheet)}" target="_blank" rel="noopener">Contact sheet · cell ${r.contactSheetCell} ↗</a>` : ''}<p>${esc(r.materialReview)}</p><p>${esc(r.orientationReview)}</p></div>${failed ? `<div class="detail-group"><p class="eyebrow">RECOVERY</p><p><strong>${esc(failed.classification)}</strong></p><p>${esc(failed.evidence)}</p><p>${esc(failed.recovery)}</p>${failed.alternatives.filter((a) => a.iconExists).map((a) => `<button class="alternative-link" data-inspect="${esc(a.id)}">Inspect available ${esc(byId.get(a.id)?.assetType || 'model')} alternative ↗</button>`).join('')}</div>` : ''}<div class="detail-group"><p class="eyebrow">UNITY & SOURCE RIGHTS</p><p>${r.importSettings ? esc(r.importSettings.issues.join('; ') || 'Observed settings meet baseline') : 'No generated import settings'}</p><p>License: ${esc(r.provenance.license || 'Unverified')} · source: ${esc(r.provenance.source || 'Unverified')}</p><p>${esc(r.provenance.licenseEvidence || 'No verified local license evidence')}</p></div><button class="primary-button ${shortlist.has(r.id) ? 'secondary' : ''}" data-shortlist="${esc(r.id)}">${shortlist.has(r.id) ? 'Remove from shortlist' : 'Add to shortlist'} · not approval</button></section></div>`;
    const sequence = dialogSequence(), index = sequence.findIndex((row) => row.id === r.id);
    $('dialog-previous').disabled = index <= 0;
    $('dialog-next').disabled = index >= sequence.length - 1;
  }

  async function copyPath(kind) {
    const row = byId.get(state.dialog);
    if (!row) return;
    const value = kind === 'source' ? row.sourcePath : row.iconPath;
    let copied = false;
    try { if (navigator.clipboard?.writeText) { await navigator.clipboard.writeText(value); copied = true; } } catch (_) { /* file:// permissions vary */ }
    if (!copied) {
      const input = document.createElement('textarea'); input.value = value; input.setAttribute('readonly','');
      input.style.cssText = 'position:fixed;left:-9999px;top:0';
      $('asset-dialog').append(input); input.select();
      try { copied = document.execCommand('copy'); } catch (_) { copied = false; }
      input.remove();
    }
    if (!copied) {
      const range = document.createRange(); range.selectNodeContents($(kind === 'source' ? 'source-path-value' : 'icon-path-value'));
      const selection = window.getSelection(); selection.removeAllRanges(); selection.addRange(range);
      toast('Path selected. Press Ctrl+C or Command+C to copy.');
    } else toast(kind === 'source' ? 'Asset path copied.' : 'Icon path copied.');
  }
  function changeSurface(value) {
    document.body.dataset.surface = value;
    document.querySelectorAll('[data-surface]').forEach((b) => { if (b.tagName === 'BUTTON') b.classList.toggle('active',b.dataset.surface === value); });
  }
  function changeChecker(checked) {
    document.body.dataset.checker = String(checked);
    $('checker-toggle').checked = checked;
    document.querySelectorAll('[data-dialog-checker]').forEach((input) => input.checked = checked);
  }
  function turnPage(delta) { state.page += delta; renderGallery(); $('collection-title').scrollIntoView({block:'start'}); }

  document.addEventListener('click',(event) => {
    const b = event.target.closest('button');
    if (!b || b.disabled) return;
    if (b.dataset.view) showView(b.dataset.view);
    else if (b.dataset.category) { state.category=b.dataset.category; state.page=1; state.shortlistOnly=false; showView('gallery'); }
    else if (b.dataset.inspect) openDialog(b.dataset.inspect);
    else if (b.dataset.shortlist) updateShortlist(b.dataset.shortlist);
    else if (b.dataset.surface) changeSurface(b.dataset.surface);
    else if (b.dataset.shopTab) { state.shop=b.dataset.shopTab; state.shopPage=1; state.shopSelected=null; renderShop(); }
    else if (b.dataset.shopItem) selectShop(b.dataset.shopItem);
    else if (b.dataset.copy) copyPath(b.dataset.copy);
  });
  document.addEventListener('change',(event) => {
    if (event.target.id === 'presentation-state') { state.shopState=event.target.value; renderShop(); }
    if (event.target.hasAttribute('data-dialog-checker')) changeChecker(event.target.checked);
  });
  document.addEventListener('error',(event) => {
    if (!(event.target instanceof HTMLImageElement)) return;
    const src = event.target.getAttribute('src');
    const row = entries.find((r) => r.imageUrl === src);
    event.target.hidden = true;
    if (row && !row.browserImageFailed) {
      row.browserImageFailed = true;
      const warning = document.createElement('span'); warning.textContent='PNG unavailable at this path'; warning.className='quality-flag';
      event.target.parentElement.append(warning);
      toast('A PNG could not be loaded. Inspect its source path.');
    }
  },true);
  $('search').addEventListener('input',(e) => { state.search=e.target.value; state.page=1; renderGallery(); });
  $('include-missing').addEventListener('change',(e) => { state.missing=e.target.checked; state.page=1; renderGallery(); });
  $('status-filter').addEventListener('change',(e) => { state.status=e.target.value; if(['Failed','Not generated'].includes(state.status)) { state.missing=true; $('include-missing').checked=true; } state.page=1; renderGallery(); });
  $('checker-toggle').addEventListener('change',(e) => changeChecker(e.target.checked));
  $('previous-page').addEventListener('click',() => turnPage(-1));
  $('next-page').addEventListener('click',() => turnPage(1));
  $('shop-previous').addEventListener('click',() => { state.shopPage--; renderShop(); });
  $('shop-next').addEventListener('click',() => { state.shopPage++; renderShop(); });
  $('shop-shortlist-only').addEventListener('change',() => { state.shopPage=1; renderShop(); });
  $('failure-search').addEventListener('input',(e) => { state.failureSearch=e.target.value; state.failurePage=1; renderQuality(); });
  $('failure-previous').addEventListener('click',() => { state.failurePage--; renderQuality(); });
  $('failure-next').addEventListener('click',() => { state.failurePage++; renderQuality(); });
  $('reset-filters').addEventListener('click',() => { Object.assign(state,{category:'All',search:'',status:'all',page:1,shortlistOnly:false}); $('search').value=''; $('status-filter').value='all'; renderGallery(); });
  $('shortlist-filter').addEventListener('click',() => { state.shortlistOnly=!state.shortlistOnly; state.category='All'; state.page=1; showView('gallery'); });
  $('close-dialog').addEventListener('click',() => $('asset-dialog').close());
  $('asset-dialog').addEventListener('close',() => state.dialog=null);
  $('asset-dialog').addEventListener('click',(e) => { if(e.target === $('asset-dialog')) { const rect=e.target.getBoundingClientRect(); if (e.clientX<rect.left || e.clientX>rect.right || e.clientY<rect.top || e.clientY>rect.bottom) e.target.close(); } });
  $('dialog-previous').addEventListener('click',() => { const seq=dialogSequence(),i=seq.findIndex((r) => r.id === state.dialog); if(i>0) { state.dialog=seq[i-1].id; renderDialog(); } });
  $('dialog-next').addEventListener('click',() => { const seq=dialogSequence(),i=seq.findIndex((r) => r.id === state.dialog); if(i<seq.length-1) { state.dialog=seq[i+1].id; renderDialog(); } });
  $('export-shortlist').addEventListener('click',() => {
    const chosen = [...shortlist].map((id) => byId.get(id)).map((r) => ({id:r.id,sourcePath:r.sourcePath,iconPath:r.iconPath,status:r.status,ownerApproved:false}));
    const payload = {purpose:'Candidate shortlist only; no approval or runtime integration',entries:chosen};
    const url = URL.createObjectURL(new Blob([JSON.stringify(payload,null,2)],{type:'application/json'}));
    const link=document.createElement('a'); link.href=url; link.download='nihongo-life-c0-shortlist.json'; link.click();
    setTimeout(() => URL.revokeObjectURL(url),1000); toast(`Exported ${chosen.length} shortlisted candidates.`);
  });
  document.addEventListener('keydown',(e) => { if(e.key === '/' && !['INPUT','SELECT','TEXTAREA'].includes(e.target.tagName) && !$('asset-dialog').open) { e.preventDefault(); showView('gallery'); $('search').focus(); } });
  $('summary-stats').innerHTML = [[data.summary.validGeneratedPngs,'REAL GENERATED ICONS','Transparent PNGs, checked on disk'],[data.summary.failedRenderJobs,'RENDER FAILURES','Diagnosis & recovery links available'],[data.summary.notGeneratedCandidates,'NOT YET GENERATED','Unclassified candidates, no substitutes']].map(([n,label,detail]) => `<div class="stat"><span class="stat-label">${label}</span><strong class="stat-number">${n}</strong><span class="stat-detail">${detail}</span></div>`).join('');
  const heroes = ['Seeds and Crops','Farming Tools','Technology'].map((category) => entries.find((r) => available(r) && r.category === category));
  $('hero-icons').innerHTML = heroes.filter(Boolean).map((r) => imageHtml(r)).join('');
  $('shortlist-count').textContent = String(shortlist.size);
  renderGallery();
  const initialView = location.hash.slice(1);
  if (['shop','quality'].includes(initialView)) showView(initialView);
})();
