function normalizePakistaniPhone(raw) {
    const digits = String(raw || '').replace(/\D/g, '');

    let national = digits;
    if (national.startsWith('0092')) national = national.slice(4);
    else if (national.startsWith('92')) national = national.slice(2);
    else if (national.startsWith('0')) national = national.slice(1);

    if (national.length !== 10 || !national.startsWith('3')) {
        return { error: `Invalid Pakistani mobile number: ${raw}` };
    }

    return { chatId: `92${national}@c.us` };
}

module.exports = { normalizePakistaniPhone };
