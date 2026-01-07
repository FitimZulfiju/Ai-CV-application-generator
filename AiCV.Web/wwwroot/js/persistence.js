export function saveItem(key, data) {
    try {
        localStorage.setItem(key, JSON.stringify(data));
        console.debug(`[Persistence] Saved '${key}'`);
    } catch (e) {
        console.warn(`[Persistence] Failed to save '${key}':`, e);
    }
}

export function loadItem(key) {
    try {
        const item = localStorage.getItem(key);
        return item ? JSON.parse(item) : null;
    } catch (e) {
        console.warn(`[Persistence] Failed to load '${key}':`, e);
        return null;
    }
}

export function clearItem(key) {
    try {
        localStorage.removeItem(key);
        console.debug(`[Persistence] Cleared '${key}'`);
    } catch (e) {
        console.warn(`[Persistence] Failed to clear '${key}':`, e);
    }
}
