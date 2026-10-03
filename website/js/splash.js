/** Splash screen — intro overlay with the Qayd logo (CSS-driven, JS only orchestrates timing) */
(function () {
  const root = document.documentElement;
  const splash = document.getElementById('splash');
  if (!splash || !root.classList.contains('splash-active')) return;

  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const DURATION = reduced ? 500 : 2600;   // visible time before exit
  const EXIT = reduced ? 150 : 700;        // exit transition length
  let finished = false;

  function finish() {
    if (finished) return;
    finished = true;
    splash.classList.add('splash-exit');
    root.classList.remove('splash-active');   // resumes hero / map intro animations
    root.classList.add('splash-done');
    window.setTimeout(() => {
      splash.classList.add('splash-hidden');
      splash.remove();
    }, EXIT + 50);
  }

  // Allow skipping with click / tap / key
  splash.addEventListener('click', finish, { once: true });
  window.addEventListener('keydown', finish, { once: true });

  // Start when the logo image is ready (or right away if cached / failed)
  const img = splash.querySelector('.splash-logo');
  const start = () => {
    splash.classList.add('splash-run');
    window.setTimeout(finish, DURATION);
  };
  if (img && !img.complete) {
    img.addEventListener('load', start, { once: true });
    img.addEventListener('error', start, { once: true });
    window.setTimeout(start, 1200); // never block on a slow image
  } else {
    start();
  }

  // Safety net: never leave the splash stuck
  window.setTimeout(finish, 6000);
})();
