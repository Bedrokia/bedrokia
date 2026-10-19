---
title: Home
hide:
  - navigation
  - toc
---

<div class="bk-hero" markdown>

![Bedrokia](assets/logo.svg)

# Bedrokia

<p class="bk-tagline">A <strong>Minecraft: Bedrock Edition</strong> server in C# / .NET 10 — Bedrock 26.50</p>

[:material-download: Download](https://github.com/Nexiumpvp2/bedrokia/releases/latest){ .md-button .md-button--primary }
[:material-rocket-launch: Install](getting-started/installation.md){ .md-button }
[:material-puzzle: Create a plugin](plugins/index.md){ .md-button }

</div>

!!! warning "Coded with AI, and a bit of human"
    Almost all the code was written by an AI (Claude, by Anthropic, through Claude Code), guided and tested in game by
    a human. The goal: test what AI can do on real software, and show it can also **maintain** it.
    [Learn more](project/ai.md)

<div class="grid cards" markdown>

-   :material-lightning-bolt:{ .lg .middle } **A single file**

    ---

    No Java, no .NET to install. Updating = replacing `bedrokia` with the new version.

    [:octicons-arrow-right-24: Installation](getting-started/installation.md)

-   :material-language-csharp:{ .lg .middle } **C# plugins, no build step**

    ---

    A folder with `plugin.yml`, `src/` and `config.yml`: the server compiles it by itself at startup.

    [:octicons-arrow-right-24: Create a plugin](plugins/index.md)

-   :material-cube-outline:{ .lg .middle } **Custom blocks and items**

    ---

    Your own swords, armour, ores and lamps, declared from a plugin.

    [:octicons-arrow-right-24: Items](plugins/items.md) · [Blocks](plugins/blocks.md)

-   :material-server:{ .lg .middle } **Ready for hosting**

    ---

    Pterodactyl egg, systemd service, Docker, a simple commented `server.properties`.

    [:octicons-arrow-right-24: Pterodactyl](getting-started/pterodactyl.md)

-   :material-earth:{ .lg .middle } **Vanilla gameplay**

    ---

    Biomes, mobs, crafting, enchanting, chests, furnaces, water and lava, TNT…

    [:octicons-arrow-right-24: Roadmap](project/roadmap.md)

-   :material-history:{ .lg .middle } **Always up to date**

    ---

    A new version with every change, with its changelog and downloads.

    [:octicons-arrow-right-24: Changelog](project/changelog.md)

</div>
