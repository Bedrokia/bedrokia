// Sends a Discord message for a push, a release or a CI failure.
// Usage: node notify.mjs <push|release|failure>   (env: DISCORD_WEBHOOK, GITHUB_* from Actions)
import { readFileSync } from "node:fs";

const webhook = process.env.DISCORD_WEBHOOK;
if (!webhook) {
  console.log("DISCORD_WEBHOOK is not set: nothing sent.");
  process.exit(0);
}

const repo = `${process.env.GITHUB_SERVER_URL ?? "https://github.com"}/${process.env.GITHUB_REPOSITORY ?? "Nexiumpvp2/bedrokia"}`;
const avatar = "https://github.com/Nexiumpvp2.png";
const event = process.env.GITHUB_EVENT_PATH ? JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, "utf8")) : {};

const embeds = {
  push() {
    const commits = event.commits ?? [];
    if (commits.length === 0) return null; // branch deleted, tag only...
    return {
      author: { name: event.sender.login, icon_url: event.sender.avatar_url, url: event.sender.html_url },
      title: commits.length > 1 ? `${commits.length} nouveaux commits` : "1 nouveau commit",
      url: event.compare,
      description: commits.slice(-10)
        .map(c => `[\`${c.id.slice(0, 7)}\`](${c.url}) ${c.message.split("\n")[0].slice(0, 100)}`)
        .join("\n"),
      color: 0x5865f2,
      timestamp: event.head_commit?.timestamp ?? new Date().toISOString(),
    };
  },

  release() {
    const tag = process.env.GITHUB_REF_NAME;
    const dl = `${repo}/releases/download/${tag}`;
    return {
      title: `Bedrokia ${tag}`,
      url: `${repo}/releases/tag/${tag}`,
      description: "Nouvelle version disponible.\n\n"
        + `[Linux](${dl}/bedrokia) · [Windows](${dl}/bedrokia.exe) · [Egg Pterodactyl](${dl}/egg-bedrokia.json) · `
        + `[Nouveautés](${repo}/blob/main/CHANGELOG.md)`,
      color: 0x57f287,
      timestamp: new Date().toISOString(),
    };
  },

  failure() {
    const run = `${repo}/actions/runs/${process.env.GITHUB_RUN_ID}`;
    return {
      title: "La CI a échoué",
      url: run,
      description: `\`${process.env.GITHUB_REF_NAME}\` · [voir les logs](${run})`,
      color: 0xed4245,
      timestamp: new Date().toISOString(),
    };
  },
};

const kind = process.argv[2];
const embed = embeds[kind]?.();
if (!embed) {
  console.log(`Nothing to send for '${kind}'.`);
  process.exit(0);
}
const response = await fetch(webhook, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ username: "Bedrokia", avatar_url: avatar, embeds: [embed] }),
});
if (!response.ok) {
  console.error(`Discord answered ${response.status}: ${await response.text()}`);
  process.exit(1);
}
console.log(`Sent the ${kind} message.`);
