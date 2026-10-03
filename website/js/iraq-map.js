/** Interactive Iraq map — Hero visual (geo-projected SVG + light canvas particles) */
(function () {
  const VIEW_W = 400;
  const VIEW_H = 440;
  const PAD = 26;

  /**
   * Iraq international border — [lon, lat] clockwise from the Syria/Turkey tripoint.
   * Simplified from real geography (Turkey → Iran → Gulf → Kuwait → Saudi → Jordan → Syria).
   */
  const IRAQ_BORDER = [
    // Turkey (west → east)
    [42.36, 37.11], [42.62, 37.26], [42.88, 37.35], [43.18, 37.37], [43.52, 37.27],
    [43.86, 37.22], [44.16, 37.30], [44.45, 37.22], [44.77, 37.15],
    // Iran (north → south)
    [44.90, 36.95], [45.02, 36.62], [45.36, 36.42], [45.30, 36.10], [45.12, 35.95],
    [45.42, 35.85], [45.76, 35.76], [46.12, 35.62], [46.32, 35.22], [46.16, 35.00],
    [45.90, 34.70], [45.56, 34.56], [45.46, 34.12], [45.66, 33.86], [46.06, 33.46],
    [46.16, 33.10], [46.52, 32.95], [47.02, 32.46], [47.46, 32.30], [47.70, 31.76],
    [47.86, 31.36], [47.66, 31.02], [47.96, 30.62], [48.16, 30.36], [48.40, 30.10],
    [48.56, 29.95],
    // Gulf coast & Kuwait
    [48.30, 29.86], [47.96, 29.96], [47.70, 30.06], [47.36, 30.08], [47.12, 30.02],
    [46.56, 29.10],
    // Saudi Arabia (east → west)
    [44.72, 29.20], [43.30, 30.16], [42.10, 30.92], [41.07, 31.42], [40.10, 31.86],
    [39.20, 32.15],
    // Jordan (south → north)
    [39.06, 32.52], [38.90, 32.96], [38.80, 33.38],
    // Syria (south → north)
    [39.60, 33.76], [40.40, 34.10], [41.00, 34.42], [41.20, 34.82], [41.26, 35.32],
    [41.30, 35.92], [41.36, 36.36], [41.82, 36.60], [42.06, 36.86]
  ];

  // Major cities — [lon, lat]; ordering matters (first ones stay visible on small screens)
  const CITIES = [
    { id: 'baghdad', lon: 44.37, lat: 33.31, r: 5.4, stats: { ar: 'المركز', en: 'Hub' } },
    { id: 'basra', lon: 47.78, lat: 30.51, r: 4.4, stats: { ar: 'الجنوب', en: 'South' } },
    { id: 'erbil', lon: 44.01, lat: 36.19, r: 4.2, stats: { ar: 'الشمال', en: 'North' } },
    { id: 'mosul', lon: 43.13, lat: 36.34, r: 4.2, stats: { ar: 'نينوى', en: 'Nineveh' } },
    { id: 'najaf', lon: 44.33, lat: 32.00, r: 3.8, stats: { ar: 'النجف', en: 'Najaf' } },
    { id: 'kirkuk', lon: 44.39, lat: 35.47, r: 3.8, stats: { ar: 'كركوك', en: 'Kirkuk' } },
    { id: 'sulaymaniyah', lon: 45.44, lat: 35.56, r: 3.8, stats: { ar: 'السليمانية', en: 'Sulaymaniyah' } },
    { id: 'karbala', lon: 44.02, lat: 32.61, r: 3.6, stats: { ar: 'كربلاء', en: 'Karbala' } },
    { id: 'anbar', lon: 43.30, lat: 33.42, r: 3.8, stats: { ar: 'الأنبار', en: 'Anbar' } },
    { id: 'nasiriyah', lon: 46.26, lat: 31.05, r: 3.5, stats: { ar: 'ذي قار', en: 'Dhi Qar' } },
    { id: 'amarah', lon: 47.14, lat: 31.84, r: 3.4, stats: { ar: 'ميسان', en: 'Maysan' } },
    { id: 'kut', lon: 45.82, lat: 32.50, r: 3.3, stats: { ar: 'واسط', en: 'Wasit' } },
    { id: 'diyala', lon: 44.65, lat: 33.75, r: 3.3, stats: { ar: 'ديالى', en: 'Diyala' } },
    { id: 'hillah', lon: 44.42, lat: 32.47, r: 3.3, stats: { ar: 'بابل', en: 'Babil' } },
    { id: 'tikrit', lon: 43.68, lat: 34.60, r: 3.3, stats: { ar: 'صلاح الدين', en: 'Salah ad-Din' } },
    { id: 'duhok', lon: 42.99, lat: 36.87, r: 3.2, stats: { ar: 'دهوك', en: 'Duhok' } }
  ];

  const LINK_PAIRS = [
    ['baghdad', 'basra'],
    ['baghdad', 'erbil'],
    ['baghdad', 'mosul'],
    ['baghdad', 'najaf'],
    ['baghdad', 'kirkuk'],
    ['erbil', 'sulaymaniyah'],
    ['baghdad', 'anbar'],
    ['najaf', 'karbala'],
    ['erbil', 'duhok'],
    ['mosul', 'kirkuk'],
    ['basra', 'nasiriyah'],
    ['basra', 'amarah'],
    ['baghdad', 'kut'],
    ['baghdad', 'diyala'],
    ['kirkuk', 'tikrit']
  ];

  const CITY_NAMES = {
    baghdad: { ar: 'بغداد', en: 'Baghdad' },
    basra: { ar: 'البصرة', en: 'Basra' },
    erbil: { ar: 'أربيل', en: 'Erbil' },
    mosul: { ar: 'الموصل', en: 'Mosul' },
    kirkuk: { ar: 'كركوك', en: 'Kirkuk' },
    sulaymaniyah: { ar: 'السليمانية', en: 'Sulaymaniyah' },
    najaf: { ar: 'النجف', en: 'Najaf' },
    karbala: { ar: 'كربلاء', en: 'Karbala' },
    anbar: { ar: 'الأنبار', en: 'Anbar' },
    nasiriyah: { ar: 'الناصرية', en: 'Nasiriyah' },
    amarah: { ar: 'العمارة', en: 'Amarah' },
    kut: { ar: 'الكوت', en: 'Kut' },
    diyala: { ar: 'ديالى', en: 'Diyala' },
    hillah: { ar: 'الحلة', en: 'Hillah' },
    tikrit: { ar: 'تكريت', en: 'Tikrit' },
    duhok: { ar: 'دهوك', en: 'Duhok' }
  };

  let reducedMotion = false;
  let rafId = 0;
  let packets = [];

  /* ── Projection (equirectangular, lat-corrected) ─────────────────── */
  function buildProjection() {
    const lons = IRAQ_BORDER.map(p => p[0]);
    const lats = IRAQ_BORDER.map(p => p[1]);
    const minLon = Math.min(...lons), maxLon = Math.max(...lons);
    const minLat = Math.min(...lats), maxLat = Math.max(...lats);
    const midLat = (minLat + maxLat) / 2;
    const kx = Math.cos(midLat * Math.PI / 180);

    const spanX = (maxLon - minLon) * kx;
    const spanY = maxLat - minLat;
    const scale = Math.min((VIEW_W - PAD * 2) / spanX, (VIEW_H - PAD * 2) / spanY);
    const usedW = spanX * scale;
    const usedH = spanY * scale;
    const offX = (VIEW_W - usedW) / 2;
    const offY = (VIEW_H - usedH) / 2;

    return (lon, lat) => ({
      x: offX + (lon - minLon) * kx * scale,
      y: offY + (maxLat - lat) * scale
    });
  }

  /* Closed Catmull-Rom → cubic Bézier for a soft, premium outline */
  function smoothClosedPath(points, tension = 0.42) {
    const n = points.length;
    if (n < 3) return '';
    const p = i => points[(i + n) % n];
    let d = `M${p(0).x.toFixed(2)} ${p(0).y.toFixed(2)}`;
    for (let i = 0; i < n; i++) {
      const p0 = p(i - 1), p1 = p(i), p2 = p(i + 1), p3 = p(i + 2);
      const c1x = p1.x + (p2.x - p0.x) / 6 * tension;
      const c1y = p1.y + (p2.y - p0.y) / 6 * tension;
      const c2x = p2.x - (p3.x - p1.x) / 6 * tension;
      const c2y = p2.y - (p3.y - p1.y) / 6 * tension;
      d += ` C${c1x.toFixed(2)} ${c1y.toFixed(2)} ${c2x.toFixed(2)} ${c2y.toFixed(2)} ${p2.x.toFixed(2)} ${p2.y.toFixed(2)}`;
    }
    return d + ' Z';
  }

  function tCity(id) {
    const lang = document.documentElement.lang === 'en' ? 'en' : 'ar';
    return window.I18N?.strings?.hero?.mapCities?.[id] || CITY_NAMES[id]?.[lang] || id;
  }

  function cityById(id) {
    return CITIES.find(c => c.id === id);
  }

  function prefersReduced() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  /* ── Build SVG ────────────────────────────────────────────────────── */
  function buildMap(stage) {
    const svgNS = 'http://www.w3.org/2000/svg';
    const project = buildProjection();

    CITIES.forEach(c => {
      const pt = project(c.lon, c.lat);
      c.x = pt.x;
      c.y = pt.y;
    });
    const outline = smoothClosedPath(IRAQ_BORDER.map(([lon, lat]) => project(lon, lat)));

    const svg = document.createElementNS(svgNS, 'svg');
    svg.setAttribute('viewBox', `0 0 ${VIEW_W} ${VIEW_H}`);
    svg.setAttribute('class', 'iraq-map-svg');
    svg.setAttribute('role', 'img');
    svg.setAttribute('aria-label', 'Iraq map');

    svg.innerHTML = `
      <defs>
        <linearGradient id="iraqFillGrad" x1="15%" y1="0%" x2="85%" y2="100%">
          <stop offset="0%" stop-color="rgba(21,101,192,0.34)"/>
          <stop offset="55%" stop-color="rgba(13,71,161,0.22)"/>
          <stop offset="100%" stop-color="rgba(0,172,193,0.16)"/>
        </linearGradient>
        <linearGradient id="iraqStrokeGrad" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stop-color="#90caf9"/>
          <stop offset="50%" stop-color="#4fc3f7"/>
          <stop offset="100%" stop-color="#00acc1"/>
        </linearGradient>
        <pattern id="iraqDots" width="9" height="9" patternUnits="userSpaceOnUse">
          <circle cx="1.2" cy="1.2" r="0.9" fill="rgba(144,202,249,0.22)"/>
        </pattern>
        <clipPath id="iraqClip"><path d="${outline}"/></clipPath>
        <filter id="iraqSoftGlow" x="-30%" y="-30%" width="160%" height="160%">
          <feGaussianBlur stdDeviation="2.6" result="b"/>
          <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
        </filter>
        <radialGradient id="iraqCoreGlow" cx="50%" cy="50%" r="55%">
          <stop offset="0%" stop-color="rgba(0,172,193,0.20)"/>
          <stop offset="100%" stop-color="rgba(0,172,193,0)"/>
        </radialGradient>
      </defs>
      <ellipse class="iraq-map-aura" cx="${VIEW_W / 2}" cy="${VIEW_H / 2}" rx="${VIEW_W * 0.46}" ry="${VIEW_H * 0.44}" fill="url(#iraqCoreGlow)"/>
      <g class="iraq-map-fill">
        <path d="${outline}" fill="url(#iraqFillGrad)"/>
        <rect x="0" y="0" width="${VIEW_W}" height="${VIEW_H}" fill="url(#iraqDots)" clip-path="url(#iraqClip)"/>
      </g>
      <path class="iraq-map-stroke-glow" d="${outline}" fill="none" stroke="rgba(79,195,247,0.35)"
            stroke-width="6" stroke-linejoin="round" stroke-linecap="round" filter="url(#iraqSoftGlow)"/>
      <path class="iraq-map-stroke" d="${outline}" fill="none" stroke="url(#iraqStrokeGrad)"
            stroke-width="2" stroke-linejoin="round" stroke-linecap="round"/>
      <g class="iraq-map-links" aria-hidden="true"></g>
      <g class="iraq-map-packets" aria-hidden="true"></g>
      <g class="iraq-map-cities"></g>
    `;

    const linksG = svg.querySelector('.iraq-map-links');
    const citiesG = svg.querySelector('.iraq-map-cities');
    const packetsG = svg.querySelector('.iraq-map-packets');

    LINK_PAIRS.forEach(([a, b], i) => {
      const ca = cityById(a);
      const cb = cityById(b);
      if (!ca || !cb) return;
      const line = document.createElementNS(svgNS, 'line');
      line.setAttribute('x1', ca.x.toFixed(2));
      line.setAttribute('y1', ca.y.toFixed(2));
      line.setAttribute('x2', cb.x.toFixed(2));
      line.setAttribute('y2', cb.y.toFixed(2));
      line.setAttribute('class', 'iraq-link');
      line.style.setProperty('--i', String(i));
      linksG.appendChild(line);
    });

    CITIES.forEach((c, i) => {
      const g = document.createElementNS(svgNS, 'g');
      g.setAttribute('class', 'iraq-city' + (c.id === 'baghdad' ? ' iraq-city-hub' : ''));
      g.setAttribute('data-city', c.id);
      g.style.setProperty('--i', String(i));
      g.setAttribute('tabindex', '0');
      g.setAttribute('role', 'button');

      const pulse = document.createElementNS(svgNS, 'circle');
      pulse.setAttribute('cx', c.x.toFixed(2));
      pulse.setAttribute('cy', c.y.toFixed(2));
      pulse.setAttribute('r', (c.r + 6).toFixed(2));
      pulse.setAttribute('class', 'iraq-city-pulse');

      const dot = document.createElementNS(svgNS, 'circle');
      dot.setAttribute('cx', c.x.toFixed(2));
      dot.setAttribute('cy', c.y.toFixed(2));
      dot.setAttribute('r', c.r.toFixed(2));
      dot.setAttribute('class', 'iraq-city-dot');

      const hit = document.createElementNS(svgNS, 'circle');
      hit.setAttribute('cx', c.x.toFixed(2));
      hit.setAttribute('cy', c.y.toFixed(2));
      hit.setAttribute('r', '14');
      hit.setAttribute('class', 'iraq-city-hit');

      g.append(pulse, dot, hit);
      citiesG.appendChild(g);
    });

    // Data packets along the main links
    LINK_PAIRS.slice(0, 7).forEach(([a, b], i) => {
      const ca = cityById(a);
      const cb = cityById(b);
      if (!ca || !cb) return;
      const pkt = document.createElementNS(svgNS, 'circle');
      pkt.setAttribute('r', '2.1');
      pkt.setAttribute('class', 'iraq-packet');
      pkt.style.setProperty('--i', String(i));
      packetsG.appendChild(pkt);
      packets.push({ el: pkt, a: ca, b: cb, t: i * 0.14, speed: 0.07 + (i % 3) * 0.018, dir: i % 2 ? -1 : 1 });
    });

    stage.appendChild(svg);

    // Exact stroke length for the draw-on animation
    const strokeEl = svg.querySelector('.iraq-map-stroke');
    const glowEl = svg.querySelector('.iraq-map-stroke-glow');
    try {
      const len = Math.ceil(strokeEl.getTotalLength()) + 2;
      [strokeEl, glowEl].forEach(el => {
        el.style.strokeDasharray = `${len}`;
        el.style.strokeDashoffset = `${len}`;
      });
    } catch { /* keep CSS fallback */ }

    return { svg };
  }

  /* ── Tooltip ──────────────────────────────────────────────────────── */
  function bindTooltip(stage, svg) {
    const tip = document.createElement('div');
    tip.className = 'iraq-map-tooltip';
    tip.setAttribute('aria-hidden', 'true');
    stage.appendChild(tip);

    const show = (cityEl, city) => {
      const lang = document.documentElement.lang === 'en' ? 'en' : 'ar';
      tip.innerHTML = `<strong>${tCity(city.id)}</strong><span>${city.stats[lang]}</span>`;
      tip.classList.add('visible');
      tip.setAttribute('aria-hidden', 'false');
      cityEl.classList.add('is-active');

      const rect = stage.getBoundingClientRect();
      const ctm = svg.getScreenCTM();
      if (!ctm) return;
      const pt = svg.createSVGPoint();
      pt.x = city.x;
      pt.y = city.y;
      const screen = pt.matrixTransform(ctm);
      tip.style.left = `${screen.x - rect.left}px`;
      tip.style.top = `${screen.y - rect.top - 16}px`;
    };

    const hide = () => {
      tip.classList.remove('visible');
      tip.setAttribute('aria-hidden', 'true');
      svg.querySelectorAll('.iraq-city.is-active').forEach(el => el.classList.remove('is-active'));
    };

    svg.querySelectorAll('.iraq-city').forEach(el => {
      const city = cityById(el.dataset.city);
      if (!city) return;
      el.setAttribute('aria-label', tCity(city.id));
      el.addEventListener('pointerenter', () => show(el, city));
      el.addEventListener('pointerleave', hide);
      el.addEventListener('focus', () => show(el, city));
      el.addEventListener('blur', hide);
    });

    document.addEventListener('i18n-ready', () => {
      svg.querySelectorAll('.iraq-city').forEach(el => {
        const city = cityById(el.dataset.city);
        if (city) el.setAttribute('aria-label', tCity(city.id));
      });
      hide();
    });
  }

  /* ── Background particles (canvas, paused off-screen) ─────────────── */
  function initParticles(canvas, stage) {
    if (!canvas || reducedMotion) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let w = 0, h = 0, dpr = 1;
    let dots = [];
    let running = true;

    const resize = () => {
      const rect = stage.getBoundingClientRect();
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      w = Math.max(1, Math.floor(rect.width));
      h = Math.max(1, Math.floor(rect.height));
      canvas.width = Math.floor(w * dpr);
      canvas.height = Math.floor(h * dpr);
      canvas.style.width = `${w}px`;
      canvas.style.height = `${h}px`;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

      const count = w < 480 ? 14 : 30;
      dots = Array.from({ length: count }, () => ({
        x: Math.random() * w,
        y: Math.random() * h,
        vx: (Math.random() - 0.5) * 0.16,
        vy: (Math.random() - 0.5) * 0.16,
        r: 0.8 + Math.random() * 1.3
      }));
    };

    const draw = () => {
      if (!running) return;
      ctx.clearRect(0, 0, w, h);
      for (let i = 0; i < dots.length; i++) {
        const a = dots[i];
        a.x += a.vx;
        a.y += a.vy;
        if (a.x < 0 || a.x > w) a.vx *= -1;
        if (a.y < 0 || a.y > h) a.vy *= -1;
        ctx.beginPath();
        ctx.fillStyle = 'rgba(144,202,249,0.42)';
        ctx.arc(a.x, a.y, a.r, 0, Math.PI * 2);
        ctx.fill();
        for (let j = i + 1; j < dots.length; j++) {
          const b = dots[j];
          const dx = a.x - b.x;
          const dy = a.y - b.y;
          const dist = Math.hypot(dx, dy);
          if (dist < 90) {
            ctx.strokeStyle = `rgba(0,172,193,${0.11 * (1 - dist / 90)})`;
            ctx.lineWidth = 0.7;
            ctx.beginPath();
            ctx.moveTo(a.x, a.y);
            ctx.lineTo(b.x, b.y);
            ctx.stroke();
          }
        }
      }
      rafId = requestAnimationFrame(draw);
    };

    resize();
    window.addEventListener('resize', resize, { passive: true });
    const io = new IntersectionObserver(([e]) => {
      running = e.isIntersecting && !reducedMotion;
      if (running) {
        cancelAnimationFrame(rafId);
        rafId = requestAnimationFrame(draw);
      }
    }, { threshold: 0.05 });
    io.observe(stage);
    if (!reducedMotion) rafId = requestAnimationFrame(draw);
  }

  /* ── Data packets ─────────────────────────────────────────────────── */
  function animatePackets(start) {
    if (reducedMotion || !packets.length) return;
    const tick = (now) => {
      const t = (now - start) / 1000;
      packets.forEach(p => {
        let u = (t * p.speed + p.t) % 1;
        if (p.dir < 0) u = 1 - u;
        const x = p.a.x + (p.b.x - p.a.x) * u;
        const y = p.a.y + (p.b.y - p.a.y) * u;
        p.el.setAttribute('cx', x.toFixed(2));
        p.el.setAttribute('cy', y.toFixed(2));
      });
      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }

  function runIntro() {
    const root = document.documentElement;
    root.classList.add('hero-intro');
    if (reducedMotion) {
      root.classList.add('hero-intro-done');
      return;
    }
    window.setTimeout(() => root.classList.add('hero-intro-done'), 2800);
  }

  function initIraqMap() {
    const stage = document.getElementById('iraq-map-stage');
    if (!stage) return;

    reducedMotion = prefersReduced();
    window.matchMedia('(prefers-reduced-motion: reduce)').addEventListener('change', e => {
      reducedMotion = e.matches;
    });

    const canvas = document.getElementById('iraq-map-particles');
    const { svg } = buildMap(stage);
    bindTooltip(stage, svg);
    initParticles(canvas, stage);
    animatePackets(performance.now());
    runIntro();
  }

  window.initIraqMap = initIraqMap;
})();
