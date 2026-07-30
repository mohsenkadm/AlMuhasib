/** Systems Explorer — tab switching, panel render, accent themes */
const SYSTEM_ACCENTS = {
  accounting: '#1565C0',
  hotel: '#00897B',
  car: '#E65100',
  carTrade: '#558B2F',
  realEstate: '#6D4C41',
  mobile: '#0277BD'
};

let activeSystemId = 'accounting';

function getSystemTab(id) {
  return I18N.strings.systems?.tabs?.find(t => t.id === id);
}

function updateSystemAccent(id) {
  const root = document.getElementById('systems');
  const accent = SYSTEM_ACCENTS[id] ?? SYSTEM_ACCENTS.accounting;
  if (root) root.style.setProperty('--system-accent', accent);
}

function renderSystemTabs() {
  const container = document.getElementById('system-tabs');
  const tabs = I18N.strings.systems?.tabs;
  if (!container || !tabs?.length) return;

  container.innerHTML = tabs.map(t => `
    <button type="button" class="system-tab" role="tab" id="tab-${t.id}"
            data-system="${t.id}" aria-selected="false" aria-controls="system-panel">
      <span class="system-tab-label">${t.label}</span>
      ${t.badge ? `<span class="system-tab-badge">${t.badge}</span>` : ''}
    </button>`).join('');

  container.querySelectorAll('.system-tab').forEach(btn => {
    btn.addEventListener('click', () => switchSystem(btn.dataset.system));
  });
}

function renderSystemPanel(tab) {
  const panel = document.getElementById('system-panel');
  const img = document.getElementById('system-screenshot');
  const mock = document.getElementById('system-mock');
  const caption = document.getElementById('system-screenshot-caption');
  if (!panel || !tab) return;

  panel.classList.remove('system-panel-in');
  void panel.offsetWidth;
  panel.classList.add('system-panel-in');

  const highlights = (tab.highlights ?? []).map(h => `<li>${h}</li>`).join('');
  const restaurantBlock = tab.restaurant ? `
    <div class="system-restaurant-block">
      <h4>${tab.restaurant.title}</h4>
      <ul class="system-highlights system-highlights-compact">
        ${(tab.restaurant.highlights ?? []).map(h => `<li>${h}</li>`).join('')}
      </ul>
    </div>` : '';

  const modulesBlock = tab.modules?.length ? `
    <div class="system-modules-block">
      <h4>${I18N.t('systems.modulesTitle')}</h4>
      <div class="system-modules-grid">
        ${tab.modules.map(m => `
          <div class="system-module">
            <strong>${m.title}</strong>
            <ul>${(m.items ?? []).map(i => `<li>${i}</li>`).join('')}</ul>
          </div>`).join('')}
      </div>
    </div>` : '';

  panel.innerHTML = `
    <div class="system-panel-text">
      ${tab.badge ? `<span class="system-panel-badge">${tab.badge}</span>` : ''}
      <h3>${tab.tagline}</h3>
      <p class="system-panel-desc">${tab.desc}</p>
      <ul class="system-highlights">${highlights}</ul>
      ${modulesBlock}
      ${restaurantBlock}
      <div class="system-panel-cta">
        <a href="#download" class="btn btn-primary" data-download-link>${I18N.t('systems.cta_download')}</a>
        <a href="#" class="btn btn-whatsapp" data-whatsapp>${I18N.t('support.btnShort')}</a>
      </div>
    </div>`;

  if (img && tab.screenshot) {
    img.src = tab.screenshot;
    img.alt = tab.label;
    tryLoadSystemScreenshot('system-screenshot', 'system-mock');
  }
  if (caption) caption.textContent = tab.screenshotCaption ?? '';

  if (mock) {
    mock.dataset.system = tab.id;
    mock.hidden = false;
    mock.style.display = '';
  }
}

function renderSystemFeaturesGrid(tab) {
  const grid = document.getElementById('system-features-grid');
  if (!grid || !tab?.features) return;

  grid.innerHTML = tab.features.map((f, i) => `
    <article class="feature-card system-feature-card reveal visible" style="--delay:${i * 0.06}s" data-icon="${f.icon}">
      <div class="feature-icon-wrap">
        <div class="feature-icon-glow"></div>
        <div class="feature-icon">${featureIconHtml(f.icon)}</div>
      </div>
      <h3>${f.title}</h3>
      <p>${f.desc}</p>
    </article>`).join('');
}

function updateTabsUI(id) {
  document.querySelectorAll('.system-tab').forEach(btn => {
    const on = btn.dataset.system === id;
    btn.classList.toggle('active', on);
    btn.setAttribute('aria-selected', on ? 'true' : 'false');
  });
  const indicator = document.getElementById('system-tab-indicator');
  const active = document.getElementById(`tab-${id}`);
  if (indicator && active) {
    const wrap = document.getElementById('system-tabs');
    const isRtl = document.documentElement.dir === 'rtl';
    indicator.style.width = `${active.offsetWidth}px`;
    if (isRtl && wrap) {
      const right = wrap.offsetWidth - active.offsetLeft - active.offsetWidth;
      indicator.style.left = 'auto';
      indicator.style.right = `${right}px`;
      indicator.style.transform = 'none';
    } else {
      indicator.style.right = 'auto';
      indicator.style.left = `${active.offsetLeft}px`;
      indicator.style.transform = 'none';
    }
  }
}

function switchSystem(id) {
  const tab = getSystemTab(id);
  if (!tab) return;
  activeSystemId = id;
  updateSystemAccent(id);
  renderSystemPanel(tab);
  renderSystemFeaturesGrid(tab);
  updateTabsUI(id);
  if (typeof observeReveals === 'function') observeReveals();
  if (typeof applyDownloadLinks === 'function') {
    const dl = document.querySelector('[data-download-link]')?.href;
    if (dl) applyDownloadLinks(dl);
  }
  if (typeof initSupportLinks === 'function') initSupportLinks();
}

function initHeroRotate() {
  const el = document.getElementById('hero-rotate-words');
  const words = I18N.strings.hero?.rotateWords;
  if (!el || !words?.length) return;
  el.innerHTML = words.map((w, i) =>
    `<span class="hero-rotate-word" style="--i:${i}">${w}</span>`).join('');
}

function initSystems() {
  renderSystemTabs();
  switchSystem(activeSystemId);
  initHeroRotate();
  requestAnimationFrame(() => updateTabsUI(activeSystemId));

  window.addEventListener('resize', () => updateTabsUI(activeSystemId));
}

document.addEventListener('i18n-ready', () => {
  renderSystemTabs();
  switchSystem(activeSystemId);
  initHeroRotate();
});
