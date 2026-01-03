// Production service worker with asset caching
const cacheName = 'webcv-cache-v1';
const assetsToCache = [
    './',
    './index.html',
    './app.css',
    './manifest.json',
    './icon-192.png',
    './icon-512.png',
    './_framework/blazor.web.js'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(cacheName).then(cache => cache.addAll(assetsToCache))
    );
});

self.addEventListener('fetch', event => {
    event.respondWith(
        caches.match(event.request).then(response => {
            return response || fetch(event.request);
        })
    );
});
