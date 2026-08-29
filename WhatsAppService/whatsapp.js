const fs = require('fs');
const path = require('path');
const qrcode = require('qrcode');
const { Client, LocalAuth } = require('whatsapp-web.js');
const { normalizePakistaniPhone } = require('./phone');

let state = { status: 'starting', qr: null };
let client = null;

function getState() {
    return state;
}

function readConfiguredBrowser() {
    const configPath = path.join(__dirname, '..', 'WhatsAppConfig.env');
    if (!fs.existsSync(configPath)) return null;
    const match = fs.readFileSync(configPath, 'utf8').match(/^WHATSAPP_BROWSER_PATH=(.+)$/m);
    const value = match ? match[1].trim() : '';
    return value && fs.existsSync(value) ? value : null;
}

function findSystemBrowser() {
    const configured = readConfiguredBrowser();
    if (configured) return configured;

    const localApp = process.env.LOCALAPPDATA || '';
    const pf       = process.env['ProgramFiles'] || 'C:\\Program Files';
    const pf86     = process.env['ProgramFiles(x86)'] || 'C:\\Program Files (x86)';

    const candidates = [
        path.join(pf,   'Google\\Chrome\\Application\\chrome.exe'),
        path.join(pf86, 'Google\\Chrome\\Application\\chrome.exe'),
        path.join(localApp, 'Google\\Chrome\\Application\\chrome.exe'),
        path.join(pf86, 'Microsoft\\Edge\\Application\\msedge.exe'),
        path.join(pf,   'Microsoft\\Edge\\Application\\msedge.exe'),
        path.join(pf,   'BraveSoftware\\Brave-Browser\\Application\\brave.exe')
    ];
    return candidates.find((p) => fs.existsSync(p)) || null;
}

function start() {
    const puppeteerOptions = { args: ['--no-sandbox', '--disable-setuid-sandbox'] };
    const browser = findSystemBrowser();
    if (browser) {
        puppeteerOptions.executablePath = browser;
        console.log(`Using system browser: ${browser}`);
    } else {
        console.log('No system browser found, falling back to bundled Chromium');
    }

    client = new Client({
        authStrategy: new LocalAuth({ dataPath: path.join(__dirname, '.wwebjs_auth') }),
        puppeteer: puppeteerOptions
    });

    client.on('qr', async (qr) => {
        try {
            state = { status: 'qr', qr: await qrcode.toDataURL(qr) };
        } catch (err) {
            console.error('Failed to render QR code', err);
        }
    });

    client.on('ready', () => {
        state = { status: 'ready', qr: null };
    });

    client.on('disconnected', (reason) => {
        console.error('WhatsApp disconnected:', reason);
        state = { status: 'disconnected', qr: null };
    });

    client.on('auth_failure', (msg) => {
        console.error('WhatsApp auth failure:', msg);
        state = { status: 'disconnected', qr: null };
    });

    client.initialize().catch((err) => {
        console.error('Failed to initialize WhatsApp client', err);
        const missingBrowser = /Could not find (Chrome|browser)|Failed to launch/i.test(err.message || '');
        state = {
            status: 'error',
            qr: null,
            error: missingBrowser
                ? 'Chrome ya Edge browser system par nahi mila. Google Chrome install karein, ya WhatsAppConfig.env mein WHATSAPP_BROWSER_PATH set karein.'
                : (err.message || 'WhatsApp start nahi ho saka')
        };
    });
}

async function sendMessage(phone, message) {
    if (!client || state.status !== 'ready') {
        return { success: false, error: `WhatsApp not ready (status: ${state.status})` };
    }

    const normalized = normalizePakistaniPhone(phone);
    if (normalized.error) return { success: false, error: normalized.error };

    try {
        const isRegistered = await client.isRegisteredUser(normalized.chatId);
        if (!isRegistered) return { success: false, error: `Number not on WhatsApp: ${phone}` };

        await client.sendMessage(normalized.chatId, message);
        return { success: true };
    } catch (err) {
        return { success: false, error: err.message || String(err) };
    }
}

module.exports = { start, getState, sendMessage };
