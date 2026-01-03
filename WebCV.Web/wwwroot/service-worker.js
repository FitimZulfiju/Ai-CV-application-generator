// In development, always fetch from the network and do not cache assets.
// This allows see changes immediately without needing to skip waiting in DevTools.
self.addEventListener('install', event => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));

self.addEventListener('fetch', event => {
    // Basic service worker for development: no caching
    return fetch(event.request);
});
