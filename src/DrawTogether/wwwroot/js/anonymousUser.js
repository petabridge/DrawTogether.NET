// Manages anonymous user ID storage in localStorage

window.anonymousUserStorage = {
    // Key used to store the anonymous user ID
    storageKey: 'drawtogether_anonymous_user_id',

    // Gets the stored anonymous user ID, or creates a new one if none exists
    getOrCreateUserId: function() {
        let userId = localStorage.getItem(this.storageKey);

        if (!userId) {
            // Generate a new anonymous user ID
            userId = 'Anonymous-' + this.generateGuid();
            localStorage.setItem(this.storageKey, userId);
        }

        return userId;
    },

    // Generates a GUID (v4)
    generateGuid: function() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    },

    // Clears the stored anonymous user ID (useful for testing or user logout)
    clearUserId: function() {
        localStorage.removeItem(this.storageKey);
    }
};
