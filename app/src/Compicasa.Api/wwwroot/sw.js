/* Service worker: caches the app shell so the app opens fast and installs on phones.
   It never caches /api: people, likes and groups always come fresh from the server. Bump VERSION on each release. */
'use strict';
const VERSION = 'compicasa-shell-2026-11-23-session26b';
const SHELL = ['/', '/index.html', '/app.css', '/theme.js', '/i18n.js', '/app.js', '/manifest.webmanifest', '/icons/icon.svg', '/icons/icon-192.png'];

self.addEventListener('install', e => {
  e.waitUntil(caches.open(VERSION).then(c => c.addAll(SHELL)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', e => {
  e.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(k => k !== VERSION).map(k => caches.delete(k)))).then(() => self.clients.claim()));
});

self.addEventListener('fetch', e => {
  const url = new URL(e.request.url);
  if (e.request.method !== 'GET' || url.origin !== location.origin || url.pathname.startsWith('/api') || url.pathname.startsWith('/healthz')) return;
  // Network first, so a new release shows at once; the cached shell is the offline fallback.
  e.respondWith(
    fetch(e.request).then(r => {
      if (r.ok) { const copy = r.clone(); caches.open(VERSION).then(c => c.put(e.request, copy)); }
      return r;
    }).catch(() => caches.match(e.request).then(r => r || caches.match('/index.html')))
  );
});
