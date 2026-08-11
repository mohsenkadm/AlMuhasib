const VERSION_URL = 'https://raw.githubusercontent.com/mohsenkadm/AlMuhasib/master/version.json';
const GITHUB_REPO = 'https://github.com/mohsenkadm/AlMuhasib';
const FALLBACK_DOWNLOAD = 'https://github.com/mohsenkadm/AlMuhasib/releases/download/v1.14.10/Qayd-Setup-1.14.10.exe';

let revealObserver = null;

function formatBytes(bytes) {
  if (!bytes) return '—';
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function applyDownloadLinks(url) {
  document.querySelectorAll('[data-download-link], #dl-btn').forEach(a => {
    if (a) a.href = url;
  });
}
window.applyDownloadLinks = applyDownloadLinks;

async function loadVersion() {
  const els = {
    version: document.getElementById('dl-version'),
    size: document.getElementById('dl-size'),
    date: document.getElementById('dl-date'),
    notes: document.getElementById('dl-notes')
  };
  try {
    const res = await fetch(VERSION_URL);
    if (!res.ok) throw new Error('version fetch failed');
    const data = await res.json();
    if (els.version) els.version.textContent = data.version ?? '—';
    if (els.size) els.size.textContent = formatBytes(data.sizeBytes);
    if (els.date) els.date.textContent = data.releaseDate ?? '—';
    if (els.notes && data.releaseNotes) els.notes.textContent = data.releaseNotes;
    // زر التنزيل يشير للمثبت EXE؛ ZIP يبقى لـ downloadUrl (التحديث التلقائي داخل التطبيق)
    applyDownloadLinks(data.installerUrl || data.downloadUrl || FALLBACK_DOWNLOAD);
  } catch {
    if (els.version) els.version.textContent = '1.14.10';
    applyDownloadLinks(FALLBACK_DOWNLOAD);
  }
}

function initScrollTop() {
  if ('scrollRestoration' in history) history.scrollRestoration = 'manual';
  if (!location.hash) {
    window.scrollTo(0, 0);
  }
}

function initNav() {
  const header = document.querySelector('.site-header');
  const toggle = document.querySelector('.nav-toggle');
  const links = document.querySelector('.nav-links');
  window.addEventListener('scroll', () => header?.classList.toggle('scrolled', window.scrollY > 24));
  toggle?.addEventListener('click', () => {
    const open = links?.classList.toggle('open');
    toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
  });
  document.querySelectorAll('.nav-links a').forEach(a => {
    a.addEventListener('click', () => {
      links?.classList.remove('open');
      toggle?.setAttribute('aria-expanded', 'false');
    });
  });
  document.addEventListener('click', e => {
    if (!links?.classList.contains('open')) return;
    if (links.contains(e.target) || toggle?.contains(e.target)) return;
    links.classList.remove('open');
    toggle?.setAttribute('aria-expanded', 'false');
  });
}

function observeReveals() {
  if (!revealObserver) {
    revealObserver = new IntersectionObserver(entries => {
      entries.forEach(e => { if (e.isIntersecting) e.target.classList.add('visible'); });
    }, { threshold: 0.08, rootMargin: '0px 0px -30px 0px' });
  }
  document.querySelectorAll('.reveal:not(.visible)').forEach(el => revealObserver.observe(el));
  document.querySelectorAll('.hero .reveal, .site-header').forEach(el => el.classList.add('visible'));
}
window.observeReveals = observeReveals;

function initCounters() {
  const run = el => {
    const target = +el.dataset.count;
    const duration = 1400;
    const start = performance.now();
    const tick = now => {
      const p = Math.min((now - start) / duration, 1);
      el.textContent = Math.floor(target * (1 - Math.pow(1 - p, 3)));
      if (p < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  };
  const obs = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (e.isIntersecting) { run(e.target); obs.unobserve(e.target); }
    });
  });
  document.querySelectorAll('[data-count]').forEach(c => obs.observe(c));
}

function initLang() {
  document.querySelectorAll('[data-lang-btn]').forEach(btn => {
    btn.addEventListener('click', () => I18N.load(btn.dataset.langBtn));
  });
}

async function boot() {
  document.documentElement.classList.add('js-ready');
  initScrollTop();
  try {
    await I18N.load(I18N.lang);
  } catch (e) {
    console.error('i18n load failed', e);
    if (window.LOCALES?.ar) {
      I18N.strings = window.LOCALES.ar;
      I18N.apply();
    }
  }
  initNav();
  observeReveals();
  initCounters();
  initLang();
  initVideos?.();
  initSystems?.();
  loadVersion();
  document.querySelectorAll('[data-github]').forEach(a => a.href = GITHUB_REPO);
}

document.addEventListener('DOMContentLoaded', boot);
document.addEventListener('i18n-ready', observeReveals);
