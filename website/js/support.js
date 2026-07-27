/** دعم واتساب — يقرأ من help-videos-data.js (نفس ملف النظام الأوفلاين) */
function getSupportConfig() {
  const m = window.HELP_VIDEOS ?? {};
  const phone = m.supportWhatsApp ?? '07505496065';
  const digits = phone.replace(/\D/g, '').replace(/^0/, '964');
  const waDigits = digits.startsWith('964') ? digits : `964${digits}`;
  return {
    phone,
    waDigits,
    messageAr: m.supportMessage ?? 'السلام عليكم، أحتاج مساعدة في نظام قيد.',
    messageEn: 'Hello, I need help with the Qayd business system.'
  };
}

function buildWhatsAppUrl(lang) {
  const s = getSupportConfig();
  const text = encodeURIComponent(lang === 'en' ? s.messageEn : s.messageAr);
  return `https://wa.me/${s.waDigits}?text=${text}`;
}

function initSupportLinks() {
  const lang = I18N?.lang ?? 'ar';
  const s = getSupportConfig();
  const url = buildWhatsAppUrl(lang);
  document.querySelectorAll('[data-whatsapp]').forEach(el => {
    el.href = url;
    el.target = '_blank';
    el.rel = 'noopener noreferrer';
  });
  const phoneEl = document.getElementById('support-phone');
  if (phoneEl) phoneEl.textContent = s.phone;
}

document.addEventListener('DOMContentLoaded', initSupportLinks);
document.addEventListener('i18n-ready', initSupportLinks);
