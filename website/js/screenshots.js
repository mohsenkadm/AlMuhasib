/** إظهار لقطات شاشة حقيقية إن وُجدت في assets/ — وإلا تبقى المعاينة المبنية بـ CSS */
function tryLoadScreenshot(imgId, mockId) {
  const img = document.getElementById(imgId);
  const mock = document.getElementById(mockId);
  if (!img) return;

  img.addEventListener('load', () => {
    img.hidden = false;
    if (mock) mock.style.display = 'none';
  });
  img.addEventListener('error', () => {
    img.remove();
  });

  // إعادة المحاولة — يضمن تشغيل onerror عند غياب الملف
  const src = img.getAttribute('src');
  if (src) {
    img.src = '';
    img.src = src;
  }
}

document.addEventListener('DOMContentLoaded', () => {
  tryLoadScreenshot('desktop-screenshot', 'desktop-mock');
  tryLoadScreenshot('mobile-screenshot', 'phone-mock');
});
