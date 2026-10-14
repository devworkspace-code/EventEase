window.eventEaseStorage = {
    get: (key, isSession) => {
        const storage = isSession ? window.sessionStorage : window.localStorage;
        return storage.getItem(key);
    },
    set: (key, value, isSession) => {
        const storage = isSession ? window.sessionStorage : window.localStorage;
        storage.setItem(key, JSON.stringify(value));
    }
};
