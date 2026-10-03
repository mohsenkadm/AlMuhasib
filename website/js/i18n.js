const I18N = {
  lang: localStorage.getItem('almuhasib-lang') || (document.documentElement.lang === 'en' ? 'en' : 'ar'),
  strings: {},

  async load(lang) {
    this.lang = lang;
    localStorage.setItem('almuhasib-lang', lang);

    // مضمّن — المصدر الرئيسي (يعمل مع file:// و http)
    if (window.LOCALES?.[lang]) {
      this.strings = window.LOCALES[lang];
    }

    // اختياري: locales/*.json للت override عند النشر — لا يستبدل المضمّن إن وُجد systems
    if ((window.location.protocol === 'http:' || window.location.protocol === 'https:')
        && !window.LOCALES?.[lang]?.systems) {
      try {
        const url = new URL(`locales/${lang}.json`, window.location.href);
        const res = await fetch(url);
        if (res.ok) this.strings = await res.json();
      } catch { /* استخدم المضمّن */ }
    }

    if (!this.strings || !this.strings.meta) {
      this.strings = window.LOCALES?.ar ?? {};
    }

    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    document.title = this.strings.meta?.title ?? 'قيد';
    const meta = document.querySelector('meta[name="description"]');
    if (meta && this.strings.meta?.description) meta.content = this.strings.meta.description;

    this.apply();

    document.querySelectorAll('[data-lang-btn]').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.langBtn === lang);
    });
  },

  t(path) {
    return path.split('.').reduce((o, k) => o?.[k], this.strings) ?? path;
  },

  apply() {
    document.querySelectorAll('[data-i18n]').forEach(el => {
      const val = this.t(el.dataset.i18n);
      if (typeof val === 'string') el.textContent = val;
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
      const val = this.t(el.dataset.i18nPlaceholder);
      if (typeof val === 'string') el.placeholder = val;
    });
    this.renderLists();
    document.dispatchEvent(new CustomEvent('i18n-ready'));
  },

  renderLists() {
    const features = document.getElementById('features-grid');
    if (features && this.strings.features?.items) {
      features.innerHTML = this.strings.features.items.map((f, i) => `
        <article class="feature-card reveal" style="--delay:${i * 0.06}s" data-icon="${f.icon}">
          <div class="feature-icon-wrap">
            <div class="feature-icon-glow"></div>
            <div class="feature-icon">${featureIconHtml(f.icon)}</div>
          </div>
          <h3>${f.title}</h3>
          <p>${f.desc}</p>
        </article>`).join('');
    }

    const whatsNew = document.getElementById('whats-new-grid');
    if (whatsNew && this.strings.whatsNew?.items) {
      const badge = this.strings.whatsNew.newLabel || 'New';
      whatsNew.innerHTML = this.strings.whatsNew.items.map((f, i) => `
        <article class="whats-new-card reveal" style="--delay:${i * 0.07}s" data-icon="${f.icon}">
          <span class="whats-new-badge">${badge}</span>
          <div class="feature-icon-wrap">
            <div class="feature-icon-glow"></div>
            <div class="feature-icon">${featureIconHtml(f.icon)}</div>
          </div>
          <h3>${f.title}</h3>
          <p>${f.desc}</p>
        </article>`).join('');
    }

    const steps = document.getElementById('how-steps');
    if (steps && this.strings.how?.steps) {
      steps.innerHTML = this.strings.how.steps.map((s, i) => `
        <div class="step-card reveal" style="--delay:${i * 0.1}s">
          <span class="step-num">${s.num}</span>
          <h3>${s.title}</h3>
          <p>${s.desc}</p>
        </div>`).join('');
    }

    const cloudPoints = document.getElementById('cloud-points');
    if (cloudPoints && this.strings.cloud?.points) {
      cloudPoints.innerHTML = this.strings.cloud.points.map(p => `<li>${p}</li>`).join('');
    }

    const networkPoints = document.getElementById('network-points');
    if (networkPoints && this.strings.network?.points) {
      networkPoints.innerHTML = this.strings.network.points.map(p => `<li>${p}</li>`).join('');
    }

    const branchesPoints = document.getElementById('branches-points');
    if (branchesPoints && this.strings.branches?.points) {
      branchesPoints.innerHTML = this.strings.branches.points.map(p => `<li>${p}</li>`).join('');
    }

    const freeApps = document.getElementById('free-apps-grid');
    if (freeApps && this.strings.freeApps?.apps) {
      const googleLabel = this.strings.freeApps.storeGoogle || 'Google Play';
      const appleLabel = this.strings.freeApps.storeApple || 'App Store';
      const soonLabel = this.strings.freeApps.storeSoon || 'Coming soon on Google Play';
      const googleSvg = `<svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true"><path fill="currentColor" d="M3.6 2.2l10.3 10.3L3.6 22.8A1.6 1.6 0 0 1 3 21.5V3.7c0-.6.2-1.1.6-1.5zm12.1 11.2l2.7 2.7-9.1 5.2 6.4-7.9zm3.6-2.1c.5.3.8.8.8 1.4s-.3 1.1-.8 1.4l-2.4 1.4-3-3 3-3 2.4 1.8zM7.3 2.1l9.1 5.2-2.7 2.7-6.4-7.9z"/></svg>`;
      const appleSvg = `<svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true"><path fill="currentColor" d="M16.4 12.7c0-2.3 1.9-3.4 2-3.5-1.1-1.6-2.8-1.8-3.4-1.8-1.4-.2-2.8.9-3.5.9s-1.8-.8-3-.8c-1.5 0-3 .9-3.8 2.3-1.6 2.8-.4 7 1.2 9.3.8 1.1 1.7 2.3 3 2.3s1.7-.8 3.1-.8 1.9.8 3.1.8 2.1-1.1 2.9-2.2c.9-1.3 1.3-2.6 1.3-2.6s-2.5-1-2.5-3.9zm-2.4-7c.7-.9 1.2-2.1 1.1-3.3-1 .1-2.3.7-3 1.6-.7.8-1.3 2.1-1.1 3.3 1.2.1 2.3-.6 3-1.6z"/></svg>`;
      freeApps.innerHTML = this.strings.freeApps.apps.map((app, i) => {
        const play = (app.playStore || '').trim();
        const apple = (app.appStore || '').trim();
        const googleBtn = play
          ? `<a class="store-btn store-google" href="${play}" target="_blank" rel="noopener noreferrer">${googleSvg}<span>${googleLabel}</span></a>`
          : `<span class="store-btn store-soon" aria-disabled="true">${googleSvg}<span>${soonLabel}</span></span>`;
        const appleBtn = apple
          ? `<a class="store-btn store-apple" href="${apple}" target="_blank" rel="noopener noreferrer">${appleSvg}<span>${appleLabel}</span></a>`
          : '';
        return `
        <article class="free-app-card reveal accent-${app.accent || 'default'}" style="--delay:${i * 0.1}s">
          <div class="free-app-glow" aria-hidden="true"></div>
          <div class="free-app-top">
            <div class="free-app-icon-wrap">
              <img src="${app.icon}" alt="${app.name}" class="free-app-icon" width="96" height="96" loading="lazy"/>
              <span class="free-app-icon-shine" aria-hidden="true"></span>
            </div>
            <div class="free-app-meta">
              <span class="free-app-pill">${this.strings.freeApps.badge || ''}</span>
              <h3>${app.name}</h3>
              <p class="free-app-tagline">${app.tagline}</p>
            </div>
          </div>
          <p class="free-app-desc">${app.desc}</p>
          <ul class="free-app-features">
            ${(app.features || []).map(f => `<li>${f}</li>`).join('')}
          </ul>
          <div class="free-app-stores">${googleBtn}${appleBtn}</div>
        </article>`;
      }).join('');
    }

    const mobilePoints = document.getElementById('mobile-points');
    if (mobilePoints && this.strings.mobile?.points) {
      mobilePoints.innerHTML = this.strings.mobile.points.map(p => `<li>${p}</li>`).join('');
    }

    const mobileStores = document.getElementById('mobile-stores');
    if (mobileStores && this.strings.mobile?.playStore) {
      const googleLabel = this.strings.mobile.storeGoogle || 'Google Play';
      const appleLabel = this.strings.mobile.storeApple || 'App Store';
      mobileStores.innerHTML = `
        <a class="store-btn store-google" href="${this.strings.mobile.playStore}" target="_blank" rel="noopener noreferrer">
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true"><path fill="currentColor" d="M3.6 2.2l10.3 10.3L3.6 22.8A1.6 1.6 0 0 1 3 21.5V3.7c0-.6.2-1.1.6-1.5zm12.1 11.2l2.7 2.7-9.1 5.2 6.4-7.9zm3.6-2.1c.5.3.8.8.8 1.4s-.3 1.1-.8 1.4l-2.4 1.4-3-3 3-3 2.4 1.8zM7.3 2.1l9.1 5.2-2.7 2.7-6.4-7.9z"/></svg>
          <span>${googleLabel}</span>
        </a>
        <a class="store-btn store-apple" href="${this.strings.mobile.appStore}" target="_blank" rel="noopener noreferrer">
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true"><path fill="currentColor" d="M16.4 12.7c0-2.3 1.9-3.4 2-3.5-1.1-1.6-2.8-1.8-3.4-1.8-1.4-.2-2.8.9-3.5.9s-1.8-.8-3-.8c-1.5 0-3 .9-3.8 2.3-1.6 2.8-.4 7 1.2 9.3.8 1.1 1.7 2.3 3 2.3s1.7-.8 3.1-.8 1.9.8 3.1.8 2.1-1.1 2.9-2.2c.9-1.3 1.3-2.6 1.3-2.6s-2.5-1-2.5-3.9zm-2.4-7c.7-.9 1.2-2.1 1.1-3.3-1 .1-2.3.7-3 1.6-.7.8-1.3 2.1-1.1 3.3 1.2.1 2.3-.6 3-1.6z"/></svg>
          <span>${appleLabel}</span>
        </a>`;
    }

    const reports = document.getElementById('reports-groups');
    if (reports && this.strings.reports?.groups) {
      reports.innerHTML = this.strings.reports.groups.map(g => `
        <div class="reports-group reveal">
          <h3 class="reports-group-label">${g.label}</h3>
          <div class="reports-list">${g.items.map(r => `<span class="report-chip">${r}</span>`).join('')}</div>
        </div>`).join('');
    } else {
      const reportsLegacy = document.getElementById('reports-list');
      if (reportsLegacy && this.strings.reports?.items) {
        reportsLegacy.innerHTML = this.strings.reports.items.map(r => `<span class="report-chip">${r}</span>`).join('');
      }
    }

    const mobileProfiles = document.getElementById('mobile-profiles');
    if (mobileProfiles && this.strings.mobile?.profiles) {
      const p = this.strings.mobile.profiles;
      mobileProfiles.innerHTML = Object.values(p).map(label =>
        `<span class="mobile-profile-chip">${label}</span>`).join('');
    }

    const faq = document.getElementById('faq-list');
    if (faq && this.strings.faq?.items) {
      faq.innerHTML = this.strings.faq.items.map((item, i) => `
        <details class="faq-item reveal" style="--delay:${i * 0.05}s">
          <summary><span>${item.q}</span><span class="faq-toggle" aria-hidden="true"></span></summary>
          <div class="faq-answer"><p>${item.a}</p></div>
        </details>`).join('');
    }
  }
};
