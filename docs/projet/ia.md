---
title: Le projet et l'IA
---

# Le projet et l'IA

## Un projet codé par l'IA

Bedrokia est écrit **presque entièrement par une IA** : Claude (Anthropic), utilisé avec Claude Code. Un humain
donne la direction (« fais les mobs », « le lit est invisible, corrige »), joue sur le serveur, signale ce qui ne va
pas, et décide. L'IA lit le code des projets de référence, écrit le code, les tests et cette documentation.

**Attention :** du code écrit par une IA peut avoir des bugs que personne n'a encore vus. Les tests automatiques en
attrapent une partie, les essais en jeu le reste. Testez avant de l'utiliser en production.

## Pourquoi

1. **Tester l'IA** sur un vrai projet difficile : un protocole binaire non documenté officiellement, un moteur de jeu
   temps réel, de la génération procédurale, un système de plugins avec compilation à chaud.
2. **Faire maintenir le projet par l'IA** : corriger les bugs remontés, suivre les nouvelles versions de Minecraft,
   garder la doc à jour — avec des développeurs humains qui relisent.
3. **Montrer que l'IA est l'avenir du développement** : une personne avec une idée claire et une IA peuvent construire
   un logiciel complet, bien plus vite qu'avant.

## Contribuer

Tous les mainteneurs sont les bienvenus, humains comme IA. 😎

- **Un bug ?** Ouvrir une *issue* : ce que vous avez fait, ce qui s'est passé, la version de Bedrokia et de Minecraft,
  et le passage de `server.log`.
- **Une correction ?** Une *pull request* avec un test si possible. `dotnet test Bedrokia.slnx` doit passer.
- **Avec une IA ?** Bien sûr. Donnez-lui le dépôt, l'issue, et demandez-lui de suivre les règles ci-dessous.

### Règles du code

- Ne copier que du code sous licence compatible (MIT, Apache, BSD) et le citer dans `THIRD-PARTY-NOTICES.md`.
  **Jamais** de code sous licence LGPL ou GPL, ni du serveur officiel (propriétaire).
- Garder le style du code autour : C# moderne, noms clairs, commentaires qui expliquent le *pourquoi*.
- La bannière Bedrokia en tête de chaque fichier `.cs`.
- Un changement = des tests qui passent.

### Écrire la documentation

La doc est dans `docs/` (français) et `docs-en/` (anglais), en Markdown avec le thème
[Material for MkDocs](https://squidfunk.github.io/mkdocs-material/). Une page ajoutée dans une langue doit idéalement
l'être aussi dans l'autre, avec le même emplacement.

- **Ajouter une page** : créer un fichier `.md` dans le bon dossier (`demarrer/`, `plugins/`, `projet/`). Il apparaît
  tout seul dans le menu ; pour choisir sa place, l'ajouter dans le `.nav.yml` du dossier.
- **Publication** : le site est reconstruit et publié automatiquement après chaque push sur `main`, avec le
  changelog généré à partir des releases.
- **Aperçu sur son PC** :

```bash
pip install "mkdocs-material==9.*" "mkdocs-awesome-nav==3.*"
```

```bash
mkdocs serve
```

### Messages de commit

Commencer par le type de changement, pour que le changelog les range au bon endroit :
`feat:` (nouveauté), `fix:` (correction), `security:` (sécurité), `perf:`, `docs:`, ou rien (autres changements).
