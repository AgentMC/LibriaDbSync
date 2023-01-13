let re = /\"([^\"]+?)\">[^<>]+?\[[^\]]+?HEVC\]/;

browser.menus.create({
    id: "copierMain",
    title: "Extract HEVC URLs!",
    contexts: ["message_list"],
    async onclick(info) {
        var urls = await Promise.all(
            info.selectedMessages.messages.map(
                async (msg) => (await browser.messages.getFull(msg.id)).parts[0].body.match(re)[1] ?? "Debug me."
            )
        );
        var copytext = urls.join("\r\n");
        await navigator.clipboard.writeText(copytext);
        for (var msg of info.selectedMessages.messages) { await browser.messages.update(msg.id, { 'read': true }); }
    },
});
