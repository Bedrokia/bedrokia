---
title: Accueil
hide:
  - navigation
  - toc
---

<div class="bk-hero" markdown>

![Bedrokia](assets/logo.svg)

# Bedrokia

<p class="bk-tagline">Serveur <strong>Minecraft: Bedrock Edition</strong> en C# / .NET 10 — Bedrock 26.50</p>

[:material-download: Télécharger](https://github.com/Nexiumpvp2/bedrokia/releases/latest){ .md-button .md-button--primary }
[:material-rocket-launch: Installer](demarrer/installation.md){ .md-button }
[:material-puzzle: Créer un plugin](plugins/index.md){ .md-button }

</div>

!!! warning "Projet codé avec l'IA, et un peu d'humain"
    Presque tout le code a été écrit par une IA (Claude, d'Anthropic, via Claude Code), guidée et testée en jeu par
    un humain. Le but : tester ce que l'IA sait faire sur un vrai logiciel, et montrer qu'elle peut aussi le
    **maintenir**. [En savoir plus](projet/ia.md)

<div class="grid cards" markdown>

-   :material-lightning-bolt:{ .lg .middle } **Un seul fichier**

    ---

    Pas de Java, pas de .NET à installer. Mettre à jour = remplacer `bedrokia` par la nouvelle version.

    [:octicons-arrow-right-24: Installation](demarrer/installation.md)

-   :material-language-csharp:{ .lg .middle } **Plugins en C# sans compilation**

    ---

    Un dossier avec `plugin.yml`, `src/` et `config.yml` : le serveur compile tout seul au démarrage.

    [:octicons-arrow-right-24: Créer un plugin](plugins/index.md)

-   :material-cube-outline:{ .lg .middle } **Blocs et objets personnalisés**

    ---

    Des épées, armures, minerais et lampes à toi, déclarés depuis un plugin.

    [:octicons-arrow-right-24: Objets](plugins/objets.md) · [Blocs](plugins/blocs.md)

-   :material-server:{ .lg .middle } **Prêt pour l'hébergement**

    ---

    Egg Pterodactyl, service systemd, Docker, `server.properties` comme PocketMine.

    [:octicons-arrow-right-24: Pterodactyl](demarrer/pterodactyl.md)

-   :material-earth:{ .lg .middle } **Gameplay vanilla**

    ---

    Biomes, mobs, artisanat, enchantements, coffres, fours, eau et lave, TNT…

    [:octicons-arrow-right-24: Feuille de route](projet/roadmap.md)

-   :material-history:{ .lg .middle } **Toujours à jour**

    ---

    Une nouvelle version à chaque changement, avec son changelog et ses téléchargements.

    [:octicons-arrow-right-24: Changelog](projet/changelog.md)

</div>
