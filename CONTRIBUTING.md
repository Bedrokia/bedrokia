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

- Ne copier que du code sous licence compatible (MIT, Apache, BSD) et le citer : Dragonfly, gophertunnel, Glowstone,
  minecraft-data, Cuberite... **Jamais** PocketMine-MP, PowerNukkitX (LGPL) ni le serveur officiel (propriétaire).
- Garder le style du code autour : C# moderne, noms clairs, commentaires qui expliquent le *pourquoi*.
- La bannière Bedrokia en tête de chaque fichier `.cs`.
- Un changement = des tests qui passent.
