---
title: Changelog
---

# Changelog

Chaque changement publié sur `main` devient une nouvelle version. Cette page est **générée automatiquement** à
partir des releases GitHub à chaque mise à jour de la documentation.

## v0.3.5 { #v0-3-5 }

<span class="bk-date">19 octobre 2026</span> <span class="bk-latest">DERNIÈRE VERSION</span>

<div class="bk-downloads" markdown>

[:material-linux: Linux / Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.5/bedrokia){ .md-button }
[:material-microsoft-windows: Windows](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.5/bedrokia.exe){ .md-button }
[:material-egg: Egg Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.5/egg-bedrokia.json){ .md-button }

</div>

!!! danger "Sécurité"

    - players no longer share permissions through similar names; a broken permissions.json is kept ([`81c24e1`](https://github.com/Nexiumpvp2/bedrokia/commit/81c24e1c3546a239b4e1eeb642d12c9a65f2f255))

## v0.3.4 { #v0-3-4 }

<span class="bk-date">19 octobre 2026</span>

<div class="bk-downloads" markdown>

[:material-linux: Linux / Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.4/bedrokia){ .md-button }
[:material-microsoft-windows: Windows](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.4/bedrokia.exe){ .md-button }
[:material-egg: Egg Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.4/egg-bedrokia.json){ .md-button }

</div>

!!! success "Nouveautés"

    - granular permissions, groups with inheritance, chat prefixes and /perm command (#2) ([`4c282bd`](https://github.com/Nexiumpvp2/bedrokia/commit/4c282bd2c3fef6f8b5680181a07d4a56a6c86e26))

## v0.3.3 { #v0-3-3 }

<span class="bk-date">19 octobre 2026</span>

<div class="bk-downloads" markdown>

[:material-linux: Linux / Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.3/bedrokia){ .md-button }
[:material-microsoft-windows: Windows](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.3/bedrokia.exe){ .md-button }
[:material-egg: Egg Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.3/egg-bedrokia.json){ .md-button }

</div>

!!! bug "Corrections"

    - blocks placed twice on snow and tall grass, and blocks placed inside players ([`71cf0f8`](https://github.com/Nexiumpvp2/bedrokia/commit/71cf0f83154084602db69b55779b7da3a9208469))

## v0.3.2 { #v0-3-2 }

<span class="bk-date">19 octobre 2026</span>

<div class="bk-downloads" markdown>

[:material-linux: Linux / Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.2/bedrokia){ .md-button }
[:material-microsoft-windows: Windows](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.2/bedrokia.exe){ .md-button }
[:material-egg: Egg Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.2/egg-bedrokia.json){ .md-button }

</div>

??? bug "Corrections"

    - client crash on join with custom blocks and items (CustomContent) ([`377b9de`](https://github.com/Nexiumpvp2/bedrokia/commit/377b9de29b93a443dc0acf776ca012ee59b1d665))

## v0.3.1 { #v0-3-1 }

<span class="bk-date">19 octobre 2026</span>

<div class="bk-downloads" markdown>

[:material-linux: Linux / Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.1/bedrokia){ .md-button }
[:material-microsoft-windows: Windows](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.1/bedrokia.exe){ .md-button }
[:material-egg: Egg Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.1/egg-bedrokia.json){ .md-button }

</div>

??? success "Nouveautés"

    - Moteur de blocs et items personnalises, gestion reseau Dragonfly et plugin CustomContent (#1) ([`7d4691b`](https://github.com/Nexiumpvp2/bedrokia/commit/7d4691bd5e28c30c8728110fa2ac67158b80c86f))
    - Add documentation for custom blocks system in docs/custom_blocks.md ([`b1057ce`](https://github.com/Nexiumpvp2/bedrokia/commit/b1057cec2aa02d21218f5211bc07b397e554851a))
    - Add documentation for custom items system in docs/custom_items.md ([`17ff7af`](https://github.com/Nexiumpvp2/bedrokia/commit/17ff7aff0ce765fd537182e79c3c7c01d835fe31))
    - Add custom blocks and items system, Dragonfly 24-packet network handling, and CustomContent plugin ([`ffd8951`](https://github.com/Nexiumpvp2/bedrokia/commit/ffd8951f80290ea79700dc8e6c0953c676c941fc))

??? note "Autres changements"

    - A new release for every push to main (version = last tag + 1 patch), announced on Discord with its commits ([`f34b3ed`](https://github.com/Nexiumpvp2/bedrokia/commit/f34b3ede1be59efb00ce91c9d0a5076abc84ba5f))
    - a player can only emote as itself, once per second; restore MagicItems and Showcase examples ([`ad9ae90`](https://github.com/Nexiumpvp2/bedrokia/commit/ad9ae900c7b8475111e10bae26802767a7b51949))
    - Simpler Discord messages ([`ad7610c`](https://github.com/Nexiumpvp2/bedrokia/commit/ad7610c6cdb56fa0e9fcb723d8786a311ddbcfed))
    - Styled Discord announcements: pushes, releases with download links, CI failures ([`ada8ea2`](https://github.com/Nexiumpvp2/bedrokia/commit/ada8ea29a1945f2aabb307427425241b07cb880d))

## v0.3.0 { #v0-3-0 }

<span class="bk-date">18 octobre 2026</span>

<div class="bk-downloads" markdown>

[:material-linux: Linux / Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.0/bedrokia){ .md-button }
[:material-microsoft-windows: Windows](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.0/bedrokia.exe){ .md-button }
[:material-egg: Egg Pterodactyl](https://github.com/Nexiumpvp2/bedrokia/releases/download/v0.3.0/egg-bedrokia.json){ .md-button }

</div>

Première version publique.
