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

    const mobilePoints = document.getElementById('mobile-points');
    if (mobilePoints && this.strings.mobile?.points) {
      mobilePoints.innerHTML = this.strings.mobile.points.map(p => `<li>${p}</li>`).join('');
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
          <summary>${item.q}</summary>
          <p>${item.a}</p>
        </details>`).join('');
    }
  }
};
