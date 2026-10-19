---
title: The project and AI
---

# The project and AI

## A project coded by AI

Bedrokia is written **almost entirely by an AI**: Claude (Anthropic), used with Claude Code. A human sets the
direction ("add the mobs", "the bed is invisible, fix it"), plays on the server, reports what is wrong, and decides.
The AI reads the reference projects' code and writes the code, the tests and this documentation.

**Warning:** code written by an AI can have bugs nobody has seen yet. The automated tests catch some of them, playing
catches the rest. Test before using it in production.

## Why

1. **Test AI** on a real, hard project: a binary protocol with no official documentation, a real-time game engine,
   procedural generation, a plugin system compiled on the fly.
2. **Have the AI maintain the project**: fix reported bugs, follow new Minecraft versions, keep the docs up to date —
   with human developers reviewing.
3. **Show that AI is the future of software development**: one person with a clear idea and an AI can build a full
   piece of software, much faster than before.

## Contributing

Every maintainer is welcome, human or AI. 😎

- **A bug?** Open an *issue*: what you did, what happened, the Bedrokia and Minecraft versions, and the relevant part
  of `server.log`.
- **A fix?** A *pull request*, with a test if possible. `dotnet test Bedrokia.slnx` must pass.
- **With an AI?** Sure. Give it the repository and the issue, and ask it to follow the rules below.

### Code rules

- Only copy code under a compatible licence (MIT, Apache, BSD) and credit it: Dragonfly, gophertunnel, Glowstone,
  minecraft-data, Cuberite... **Never** PocketMine-MP, PowerNukkitX (LGPL) or the official server (proprietary).
- Match the surrounding style: modern C#, clear names, comments that explain *why*.
- The Bedrokia banner at the top of every `.cs` file.
- One change = passing tests.

### Writing the documentation

The docs live in `docs/` (French) and `docs-en/` (English), in Markdown with the
[Material for MkDocs](https://squidfunk.github.io/mkdocs-material/) theme. A page added in one language should
ideally be added in the other one too, at the same place.

- **Add a page**: create a `.md` file in the right folder (`getting-started/`, `plugins/`, `project/`). It shows up in
  the menu by itself; to choose its position, add it to the folder's `.nav.yml`.
- **Publishing**: the site is rebuilt and published automatically after every push to `main`, with the changelog
  generated from the releases.
- **Preview on your PC**:

```bash
pip install "mkdocs-material==9.*" "mkdocs-awesome-nav==3.*"
```

```bash
mkdocs serve -f mkdocs.en.yml
```

### Commit messages

Start with the kind of change so the changelog files it in the right place:
`feat:` (new feature), `fix:` (bug fix), `security:`, `perf:`, `docs:`, or nothing (other changes).
