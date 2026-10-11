/** أنيميشن محادثة بوت تيليجرام عند ظهور القسم */
function initTelegramBotSection() {
  const section = document.getElementById('telegram-bot');
  const phone = document.getElementById('tg-phone');
  if (!section || !phone || section.dataset.tgInit === '1') return;
  section.dataset.tgInit = '1';

  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  const replay = () => {
    if (reduceMotion) {
      phone.classList.add('tg-chat-play');
      section.classList.add('tg-visible');
      return;
    }
    phone.classList.remove('tg-chat-play');
    void phone.offsetWidth;
    phone.classList.add('tg-chat-play');
  };

  const obs = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (!e.isIntersecting) return;
      section.classList.add('tg-visible');
      replay();
    });
  }, { threshold: 0.28 });

  obs.observe(section);
  document.addEventListener('i18n-ready', () => {
    if (section.classList.contains('tg-visible')) replay();
  });
}

window.initTelegramBotSection = initTelegramBotSection;
