const CATEGORY_ICONS = {
  dashboard: '📊', 'master-data': '🗂️', sales: '🛒', purchases: '📥',
  installments: '📅', finance: '🏦', inventory: '📦', reports: '📈', admin: '⚙️'
};

function extractVideoId(url) {
  if (!url) return null;
  const m = url.match(/(?:v=|youtu\.be\/|embed\/)([A-Za-z0-9_-]{11})/);
  return m?.[1] ?? null;
}

function extractStartSeconds(url) {
  if (!url) return 0;
  const t = url.match(/[?&]t=(\d+)/);
  return t ? +t[1] : 0;
}

function thumbUrl(id) {
  return `https://img.youtube.com/vi/${id}/hqdefault.jpg`;
}

function embedUrl(id, start = 0) {
  const q = start ? `?start=${start}&autoplay=1` : '?autoplay=1';
  return `https://www.youtube-nocookie.com/embed/${id}${q}`;
}

function flattenVideos(manifest) {
  const list = [];
  for (const cat of manifest.categories ?? []) {
    for (const v of cat.videos ?? []) {
      const id = extractVideoId(v.youtubeUrl);
      list.push({
        categoryId: cat.id,
        categoryTitle: cat.title,
        title: v.title,
        description: v.description,
        youtubeUrl: v.youtubeUrl,
        videoId: id,
        start: extractStartSeconds(v.youtubeUrl)
      });
    }
  }
  return list;
}

const VideosUI = {
  all: [],
  filtered: [],
  activeCat: 'all',
  selected: null,

  init() {
    const manifest = window.HELP_VIDEOS;
    if (!manifest) return;
    this.all = flattenVideos(manifest);
    this.filtered = [...this.all];
    this.renderCategories();
    this.renderList();
    if (this.all.length) this.select(this.all[0], { scroll: false });

    const search = document.getElementById('video-search');
    search?.addEventListener('input', () => this.applyFilter(search.value.trim()));

    document.getElementById('video-cats')?.addEventListener('click', e => {
      const btn = e.target.closest('[data-cat]');
      if (!btn) return;
      this.activeCat = btn.dataset.cat;
      this.renderCategories();
      this.applyFilter(search?.value.trim() ?? '');
    });

    document.getElementById('video-list')?.addEventListener('click', e => {
      const card = e.target.closest('[data-video-idx]');
      if (!card) return;
      const idx = +card.dataset.videoIdx;
      if (this.filtered[idx]) this.select(this.filtered[idx], { scroll: true });
    });
  },

  catTitle(id, title) {
    const map = I18N.t('videos.categories');
    if (typeof map === 'object' && map[id]) return map[id];
    return title;
  },

  applyFilter(q) {
    const lower = q.toLowerCase();
    this.filtered = this.all.filter(v => {
      if (this.activeCat !== 'all' && v.categoryId !== this.activeCat) return false;
      if (!lower) return true;
      return v.title.toLowerCase().includes(lower) ||
        v.description.toLowerCase().includes(lower) ||
        v.categoryTitle.toLowerCase().includes(lower);
    });
    this.renderList();
    if (this.selected && !this.filtered.includes(this.selected)) {
      this.select(this.filtered[0] ?? null, { scroll: false });
    }
  },

  renderCategories() {
    const el = document.getElementById('video-cats');
    if (!el || !window.HELP_VIDEOS) return;
    const allLabel = I18N.t('videos.all');
    const cats = window.HELP_VIDEOS.categories ?? [];
    el.innerHTML = [
      `<button type="button" class="video-cat${this.activeCat === 'all' ? ' active' : ''}" data-cat="all">${allLabel} <em>${this.all.length}</em></button>`,
      ...cats.map(c => {
        const count = c.videos?.length ?? 0;
        const icon = CATEGORY_ICONS[c.id] ?? '▶';
        return `<button type="button" class="video-cat${this.activeCat === c.id ? ' active' : ''}" data-cat="${c.id}"><span>${icon}</span>${this.catTitle(c.id, c.title)} <em>${count}</em></button>`;
      })
    ].join('');
  },

  renderList() {
    const el = document.getElementById('video-list');
    const countEl = document.getElementById('video-count');
    if (!el) return;
    if (countEl) countEl.textContent = this.filtered.length;

    if (!this.filtered.length) {
      el.innerHTML = `<p class="video-empty">${I18N.t('videos.empty')}</p>`;
      return;
    }

    el.innerHTML = this.filtered.map((v, i) => {
      const active = v === this.selected ? ' active' : '';
      const thumb = v.videoId
        ? `<img src="${thumbUrl(v.videoId)}" alt="" loading="lazy"/>`
        : '<div class="video-no-thumb">▶</div>';
      return `
        <button type="button" class="video-card${active}" data-video-idx="${i}" style="--delay:${Math.min(i * 0.03, 0.5)}s">
          <div class="video-thumb">${thumb}<span class="play-ring"></span></div>
          <div class="video-card-body">
            <span class="video-cat-tag">${this.catTitle(v.categoryId, v.categoryTitle)}</span>
            <strong>${v.title}</strong>
            <p>${v.description}</p>
          </div>
        </button>`;
    }).join('');

    requestAnimationFrame(() => {
      el.querySelectorAll('.video-card').forEach(c => c.classList.add('visible'));
    });
  },

  select(video, { scroll = false } = {}) {
    this.selected = video;
    const wrap = document.getElementById('video-embed-wrap');
    const title = document.getElementById('video-active-title');
    const desc = document.getElementById('video-active-desc');
    const placeholder = document.getElementById('video-placeholder');

    document.querySelectorAll('.video-card').forEach(c => c.classList.remove('active'));

    if (!video) {
      if (wrap) wrap.innerHTML = '';
      if (placeholder) placeholder.style.display = 'flex';
      if (title) title.textContent = '';
      if (desc) desc.textContent = '';
      return;
    }

    if (title) title.textContent = video.title;
    if (desc) desc.textContent = video.description;

    if (!video.videoId) {
      if (wrap) wrap.innerHTML = '';
      if (placeholder) {
        placeholder.style.display = 'flex';
        placeholder.querySelector('p').textContent = I18N.t('videos.noLink');
      }
      return;
    }

    if (placeholder) placeholder.style.display = 'none';
    if (wrap) {
      wrap.innerHTML = `<iframe src="${embedUrl(video.videoId, video.start)}" title="${video.title}"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
        allowfullscreen loading="lazy"></iframe>`;
    }

    const idx = this.filtered.indexOf(video);
    const card = document.querySelector(`[data-video-idx="${idx}"]`);
    card?.classList.add('active');
    if (scroll) card?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
  }
};

let videosInited = false;

function initVideos() {
  if (videosInited || !window.HELP_VIDEOS) return;
  videosInited = true;
  VideosUI.init();
}

document.addEventListener('i18n-ready', () => {
  initVideos();
  if (videosInited) {
    VideosUI.renderCategories();
    VideosUI.renderList();
    if (VideosUI.selected) VideosUI.select(VideosUI.selected, { scroll: false });
  }
});
