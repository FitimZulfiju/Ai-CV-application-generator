// Production service worker with asset caching
const cacheName = 'aicv-cache-v2'; // Incremented version to bust old cache
const assetsToCache = [
    '/',
    '/app.css',
    '/logo.png',
    '/favicon.png',
    '/_framework/blazor.web.js'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(cacheName).then(cache => cache.addAll(assetsToCache))
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => Promise.all(
            keys.map(key => {
                if (key !== cacheName) {
                    return caches.delete(key);
                }
            })
        ))
    );
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    // Always fetch manifest.json and service-worker from network
    if (url.pathname.endsWith('manifest.json') || url.pathname.endsWith('service-worker.js')) {
        event.respondWith(fetch(event.request));
        return;
    }

    event.respondWith(
        caches.match(event.request).then(response => {
            return response || fetch(event.request);
        })
    );
});
