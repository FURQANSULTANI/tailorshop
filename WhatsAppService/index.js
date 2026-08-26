const fs = require('fs');
const path = require('path');
const express = require('express');
const whatsapp = require('./whatsapp');

process.on('unhandledRejection', (err) => console.error('Unhandled rejection:', err));
process.on('uncaughtException', (err) => console.error('Uncaught exception:', err));

function readPort() {
    const configPath = path.join(__dirname, '..', 'WhatsAppConfig.env');
    if (fs.existsSync(configPath)) {
        const match = fs.readFileSync(configPath, 'utf8').match(/^WHATSAPP_PORT=(\d+)/m);
        if (match) return parseInt(match[1], 10);
    }
    return 4001;
}

const app = express();
app.use(express.json());

app.get('/health', (req, res) => {
    res.json(whatsapp.getState());
});

app.post('/send', async (req, res) => {
    const { phone, message } = req.body || {};
    if (!phone || !message) {
        res.status(400).json({ success: false, error: 'phone and message are required' });
        return;
    }
    const result = await whatsapp.sendMessage(phone, message);
    res.json(result);
});

const port = readPort();
app.listen(port, '127.0.0.1', () => {
    console.log(`WhatsApp service listening on 127.0.0.1:${port}`);
    whatsapp.start();
});
