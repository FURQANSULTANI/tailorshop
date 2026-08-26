const path = require('path');
const qrcode = require('qrcode');
const { Client, LocalAuth } = require('whatsapp-web.js');
const { normalizePakistaniPhone } = require('./phone');

let state = { status: 'starting', qr: null };
let client = null;

function getState() {
    return state;
}

function start() {
    client = new Client({
        authStrategy: new LocalAuth({ dataPath: path.join(__dirname, '.wwebjs_auth') }),
        puppeteer: { args: ['--no-sandbox', '--disable-setuid-sandbox'] }
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
        state = { status: 'disconnected', qr: null };
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
