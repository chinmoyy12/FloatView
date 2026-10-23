// FloatView Companion Background Service Worker - Bridge to Desktop App

const HTTP_BRIDGE_URL = 'http://127.0.0.1:48123/api/media/event';
let nativePort = null;
let useNativeMessaging = true;

function getBrowserName() {
    const ua = navigator.userAgent;
    if (ua.includes('Edg/')) return 'Microsoft Edge';
    if (ua.includes('Brave/')) return 'Brave Browser';
    if (ua.includes('Chrome/')) return 'Google Chrome';
    return 'Chromium Browser';
}

function initNativePort() {
    if (nativePort || !useNativeMessaging) return;
    try {
        nativePort = chrome.runtime.connectNative('com.floatview.bridge');
        nativePort.onMessage.addListener((response) => {
            if (response && response.command === 'PAUSE' && response.tabId) {
                chrome.tabs.sendMessage(response.tabId, { action: 'FLOATVIEW_PAUSE' });
            }
        });
        nativePort.onDisconnect.addListener(() => {
            nativePort = null;
            // Fall back to HTTP bridge if native messaging host is not registered
            useNativeMessaging = false;
        });
    } catch (e) {
        useNativeMessaging = false;
        nativePort = null;
    }
}

// Forward to Desktop FloatView
async function forwardToFloatView(payload, tabId) {
    payload.tabId = tabId;
    payload.browserName = getBrowserName();

    // Strategy 1: Native Messaging (if connected)
    if (nativePort) {
        try {
            nativePort.postMessage(payload);
            return;
        } catch (e) {
            nativePort = null;
        }
    }

    // Strategy 2: HTTP Loopback Bridge (zero configuration)
    try {
        const response = await fetch(HTTP_BRIDGE_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            const data = await response.json();
            if (data && data.command === 'PAUSE' && data.tabId) {
                chrome.tabs.sendMessage(data.tabId, { action: 'FLOATVIEW_PAUSE' });
            }
        }
    } catch (e) {
        // App might not be running yet or port closed
    }
}

// Listen for content script events
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message && message.action === 'FLOATVIEW_MEDIA_EVENT' && message.payload) {
        const tabId = sender.tab ? sender.tab.id : -1;
        forwardToFloatView(message.payload, tabId);
        sendResponse({ received: true });
    }
    return true;
});

// Handle tab closed
chrome.tabs.onRemoved.addListener((tabId) => {
    forwardToFloatView({ type: 'MEDIA_ENDED', url: '', title: '' }, tabId);
});

// Try connecting to native host on launch
try {
    initNativePort();
} catch (e) {}
