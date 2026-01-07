// Development Service Worker - v2 (Trigger Update)
// In development, always fetch from the network and do not cache assets.
self.addEventListener('install', event => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));

self.addEventListener('fetch', event => {
    return fetch(event.request);
});
