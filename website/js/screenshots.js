/** Screenshot loading — real images from assets/ with CSS mock fallback */
function tryLoadScreenshot(imgId, mockId, fallbacks = []) {
  const img = document.getElementById(imgId);
  const mock = document.getElementById(mockId);
  if (!img) return;

  let fallbackIdx = 0;

  img.addEventListener('load', () => {
    img.hidden = false;
    if (mock) mock.style.display = 'none';
  });

  img.addEventListener('error', () => {
    if (fallbackIdx < fallbacks.length) {
      img.src = fallbacks[fallbackIdx++];
      return;
    }
    img.hidden = true;
    if (mock) mock.style.display = '';
  });

  const src = img.getAttribute('src');
  if (src) {
    img.src = '';
    img.src = src;
  }
}

function tryLoadSystemScreenshot(imgId, mockId) {
  const img = document.getElementById(imgId);
  const mock = document.getElementById(mockId);
  if (!img) return;

  const onLoad = () => {
    img.hidden = false;
    if (mock) mock.style.display = 'none';
  };
  const onError = () => {
    img.hidden = true;
    if (mock) mock.style.display = '';
  };

  img.removeEventListener('load', onLoad);
  img.removeEventListener('error', onError);
  img.addEventListener('load', onLoad);
  img.addEventListener('error', onError);

  const src = img.getAttribute('src');
  if (src) {
    img.hidden = true;
    img.src = '';
    img.src = src;
  }
}

window.tryLoadSystemScreenshot = tryLoadSystemScreenshot;

document.addEventListener('DOMContentLoaded', () => {
  tryLoadScreenshot('desktop-screenshot', 'desktop-mock', ['assets/desktop-dashboard.png']);
  tryLoadScreenshot('mobile-screenshot', 'phone-mock');
  tryLoadScreenshot('system-screenshot', 'system-mock', ['assets/desktop-dashboard.png']);
});
