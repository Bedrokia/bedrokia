// Changelog from the commits between releases, sorted by kind (security, fixes, features...).
//   node changelog.mjs page [fr|en]    -> changelog page of the docs (every release, Material for MkDocs)
//   node changelog.mjs release <tag>   -> notes of a new release (commits since the latest release up to GITHUB_SHA)
// Env: GITHUB_TOKEN, GITHUB_REPOSITORY (owner/name), GITHUB_SHA for "release".

const repo = process.env.GITHUB_REPOSITORY ?? "Nexiumpvp2/bedrokia";
const site = `https://github.com/${repo}`;
const headers = { Accept: "application/vnd.github+json", "User-Agent": "bedrokia-changelog" };
if (process.env.GITHUB_TOKEN)
  headers.Authorization = `Bearer ${process.env.GITHUB_TOKEN}`;

async function api(path) {
  const response = await fetch(`https://api.github.com/repos/${repo}${path}`, { headers });
  if (!response.ok)
    throw new Error(`GitHub API ${path}: ${response.status} ${await response.text()}`);
  return response.json();
}

// Kinds, in display order. A commit goes to the first that matches its subject ("fix: ...", "security: ...").
const kinds = [
  { key: "security", title: "Sécurité", titleEn: "Security", emoji: "🔒", box: "danger", test: /^(security|sécurité|securite)\b|faille|vulnerab/i },
  { key: "fix", title: "Corrections", titleEn: "Fixes", emoji: "🐛", box: "bug", test: /^(fix|bugfix|hotfix|corrig|correction)/i },
  { key: "feat", title: "Nouveautés", titleEn: "New features", emoji: "✨", box: "success", test: /^(feat|add|ajout|new|nouveau)/i },
  { key: "perf", title: "Performances", titleEn: "Performance", emoji: "⚡", box: "tip", test: /^perf\b/i },
  { key: "docs", title: "Documentation", titleEn: "Documentation", emoji: "📖", box: "info", test: /^docs?\b/i },
  { key: "other", title: "Autres changements", titleEn: "Other changes", emoji: "🔧", box: "note", test: /./ },
];

// Subject of a commit; a merged pull request shows its title instead of "Merge pull request #N from ...".
function describe(commit) {
  const lines = commit.commit.message.split("\n").map(l => l.trim()).filter(Boolean);
  const merge = lines[0].match(/^Merge pull request #(\d+)/);
  const raw = merge && lines[1] ? `${lines[1]} (#${merge[1]})` : lines[0];
  // The documentation does not name the reference projects (they are credited in THIRD-PARTY-NOTICES.md).
  const subject = raw.replace(/\b(Dragonfly|PocketMine(-MP)?)\b\s*/gi, "").replace(/\s{2,}/g, " ").trim();
  return { subject, sha: commit.sha, url: commit.html_url, author: commit.author?.login ?? commit.commit.author.name };
}

function group(commits) {
  const groups = new Map(kinds.map(k => [k.key, []]));
  const described = commits.map(describe);
  // A merged pull request is listed once, as "title (#N)", not also as its own commits with the same title.
  const merged = new Set(described.map(c => c.subject.match(/^(.*) \(#\d+\)$/)?.[1]).filter(Boolean));
  for (const c of described) {
    if (/^Merge (branch|remote)/.test(c.subject) || merged.has(c.subject))
      continue;
    const kind = kinds.find(k => k.test.test(c.subject));
    groups.get(kind.key).push({ ...c, subject: c.subject.replace(/^[a-zA-Z]+(\([^)]*\))?!?:\s*/, "") });
  }
  return kinds.filter(k => groups.get(k.key).length > 0).map(k => ({ ...k, items: groups.get(k.key) }));
}

const line = c => `- ${c.subject} ([\`${c.sha.slice(0, 7)}\`](${c.url}))`;
const date = iso => new Date(iso).toLocaleDateString(en ? "en-GB" : "fr-FR", { day: "numeric", month: "long", year: "numeric" });
const downloads = tag => [
  ["Linux / Pterodactyl", "bedrokia", ":material-linux:"],
  ["Windows", "bedrokia.exe", ":material-microsoft-windows:"],
  ["Egg Pterodactyl", "egg-bedrokia.json", ":material-egg:"],
].map(([label, file, icon]) => ({ label, icon, url: `${site}/releases/download/${tag}/${file}` }));

async function commitsBetween(from, to) {
  if (!from)
    return [];
  return (await api(`/compare/${from}...${to}`)).commits.reverse(); // newest first
}

async function page() {
  const releases = (await api("/releases?per_page=100")).filter(r => !r.draft);
  const out = [
    "---",
    "title: Changelog",
    "---",
    "",
    "# Changelog",
    "",
    ...(en
      ? ["Every change pushed to `main` becomes a new version. This page is **generated automatically** from the",
         "GitHub releases each time the documentation is updated."]
      : ["Chaque changement publié sur `main` devient une nouvelle version. Cette page est **générée automatiquement** à",
         "partir des releases GitHub à chaque mise à jour de la documentation."]),
    "",
  ];
  for (const [i, release] of releases.entries()) {
    const groups = group(await commitsBetween(releases[i + 1]?.tag_name, release.tag_name));
    const open = i < 3 ? "!!!" : "???"; // the latest versions open, the older ones folded
    out.push(`## ${release.tag_name} { #${release.tag_name.replace(/\./g, "-")} }`, "");
    out.push(`<span class="bk-date">${date(release.published_at)}</span>${i === 0 ? ` <span class="bk-latest">${en ? "LATEST" : "DERNIÈRE VERSION"}</span>` : ""}`, "");
    out.push('<div class="bk-downloads" markdown>', "");
    out.push(downloads(release.tag_name).map(d => `[${d.icon} ${d.label}](${d.url}){ .md-button }`).join("\n"), "", "</div>", "");
    if (groups.length === 0)
      out.push(i === releases.length - 1 ? (en ? "First public release." : "Première version publique.") : (en ? "No code changes." : "Aucun changement de code."), "");
    for (const g of groups) {
      out.push(`${open} ${g.box} "${en ? g.titleEn : g.title}"`, "");
      out.push(...g.items.map(c => "    " + line(c)), "");
    }
  }
  return out.join("\n");
}

async function release(tag) {
  const latest = await api("/releases/latest").catch(() => null);
  const groups = group(await commitsBetween(latest?.tag_name, process.env.GITHUB_SHA ?? "main"));
  const out = [];
  for (const g of groups)
    out.push(`### ${g.emoji} ${g.title}`, "", ...g.items.map(line), "");
  if (groups.length === 0)
    out.push("Aucun changement de code.", "");
  out.push("### ⬇️ Télécharger", "", "| Fichier | Pour |", "|---|---|",
    ...downloads(tag).map(d => `| [\`${d.url.split("/").pop()}\`](${d.url}) | ${d.label} |`), "");
  out.push("Mettre à jour : arrêter le serveur, remplacer le fichier, relancer. Mondes, joueurs et plugins sont gardés.", "");
  if (latest)
    out.push(`**Tous les changements** : [${latest.tag_name}...${tag}](${site}/compare/${latest.tag_name}...${tag})`);
  return out.join("\n");
}

const [mode, arg] = process.argv.slice(2);
const en = mode === "page" && arg === "en";
process.stdout.write(mode === "release" ? await release(arg) : await page());
