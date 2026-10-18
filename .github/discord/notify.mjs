// Sends a styled Discord embed for a push, a release or a CI failure.
// Usage: node notify.mjs <push|release|failure>   (env: DISCORD_WEBHOOK, GITHUB_* from Actions)
import { readFileSync } from "node:fs";

const webhook = process.env.DISCORD_WEBHOOK;
if (!webhook) {
  console.log("DISCORD_WEBHOOK is not set: nothing sent.");
  process.exit(0);
}

const repo = `${process.env.GITHUB_SERVER_URL ?? "https://github.com"}/${process.env.GITHUB_REPOSITORY ?? "Nexiumpvp2/bedrokia"}`;
const docs = "https://nexiumpvp2.github.io/bedrokia/";
const avatar = "https://github.com/Nexiumpvp2.png";
const footer = { text: "Bedrokia • codé par l'IA et un peu d'humain 🤖", icon_url: avatar };
const event = process.env.GITHUB_EVENT_PATH ? JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, "utf8")) : {};
const plural = (n, word) => `${n} ${word}${n > 1 ? "s" : ""}`;

const embeds = {
  push() {
    const commits = event.commits ?? [];
    if (commits.length === 0) return null; // branch deleted, tag only...
    return {
      author: { name: event.sender.login, icon_url: event.sender.avatar_url, url: event.sender.html_url },
      title: `🛠️ Mise à jour du code · ${plural(commits.length, "commit")}`,
      url: event.compare,
      description: commits.slice(-10)
        .map(c => `[\`${c.id.slice(0, 7)}\`](${c.url}) ${c.message.split("\n")[0].slice(0, 100)}`)
        .join("\n"),
      color: 0x5865f2,
      fields: [
        { name: "🌿 Branche", value: `\`${event.ref.replace("refs/heads/", "")}\``, inline: true },
        { name: "👤 Auteur", value: event.pusher.name, inline: true },
        { name: "📦 Dépôt", value: `[${event.repository.full_name}](${repo})`, inline: true },
        { name: "🔗 Liens", value: `[Code](${repo}) · [Documentation](${docs}) · [Releases](${repo}/releases)` },
      ],
      footer,
      timestamp: event.head_commit?.timestamp ?? new Date().toISOString(),
    };
  },

  release() {
    const tag = process.env.GITHUB_REF_NAME;
    const dl = `${repo}/releases/download/${tag}`;
    return {
      title: `🚀 Bedrokia ${tag} est disponible !`,
      url: `${repo}/releases/tag/${tag}`,
      description: "Nouvelle version du serveur **Minecraft Bedrock** en C#.\n\n"
        + "⚡ **Mise à jour = remplacer un seul fichier**, comme le `.phar` de PocketMine : arrête le serveur, "
        + "remplace `bedrokia`, relance. Mondes, joueurs et plugins sont gardés.",
      color: 0xffd700,
      thumbnail: { url: avatar },
      fields: [
        { name: "🐧 Linux / Pterodactyl", value: `[⬇️ bedrokia](${dl}/bedrokia)`, inline: true },
        { name: "🪟 Windows", value: `[⬇️ bedrokia.exe](${dl}/bedrokia.exe)`, inline: true },
        { name: "🦖 Egg Pterodactyl", value: `[⬇️ egg-bedrokia.json](${dl}/egg-bedrokia.json)`, inline: true },
        { name: "🧩 Plugins d'exemple", value: `[⬇️ example-plugins.zip](${dl}/example-plugins.zip)`, inline: true },
        { name: "📖 Documentation", value: `[Lire la doc](${docs})`, inline: true },
        { name: "📝 Nouveautés", value: `[CHANGELOG](${repo}/blob/main/CHANGELOG.md)`, inline: true },
      ],
      footer,
      timestamp: new Date().toISOString(),
    };
  },

  failure() {
    const run = `${repo}/actions/runs/${process.env.GITHUB_RUN_ID}`;
    const sha = process.env.GITHUB_SHA ?? "";
    return {
      title: "❌ La CI a échoué",
      url: run,
      description: "Les tests ou la compilation ne passent plus : aucune release n'a été publiée.",
      color: 0xed4245,
      fields: [
        { name: "🌿 Réf", value: `\`${process.env.GITHUB_REF_NAME}\``, inline: true },
        { name: "🔖 Commit", value: `[\`${sha.slice(0, 7)}\`](${repo}/commit/${sha})`, inline: true },
        { name: "👤 Par", value: process.env.GITHUB_ACTOR ?? "?", inline: true },
        { name: "🔎 Détails", value: `[Voir les logs](${run})` },
      ],
      footer,
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
console.log(`Sent the ${kind} embed.`);
