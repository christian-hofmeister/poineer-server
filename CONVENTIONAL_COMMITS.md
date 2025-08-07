# 📝 Conventional Commit Cheat Sheet

## 🔧 Format

```
<type>(<scope>): <kurze beschreibung>
```

- **type** – Was ist passiert?
- **scope** *(optional)* – Wo genau?
- **beschreibung** – In **max. 50 Zeichen**, im Präsens, **keine Punktuation**

---

## 🎯 Beispiele

| Typ        | Beispiel                                                                 |
|------------|--------------------------------------------------------------------------|
| `feat`     | `feat(api): add region download endpoint`                                |
| `fix`      | `fix(sqlite): correct POI table creation`                                |
| `docs`     | `docs(readme): update setup instructions for devs`                       |
| `style`    | `style(server): reformat using .editorconfig`                            |
| `refactor` | `refactor(controller): simplify POI filtering logic`                     |
| `test`     | `test(api): add integration test for /health`                            |
| `chore`    | `chore(deps): bump Microsoft.Data.Sqlite to 9.0.8`                       |
| `build`    | `build(jenkins): add dotnet outdated check`                              |
| `ci`       | `ci(github): enable semantic-release`                                    |
| `perf`     | `perf(sqlite): improve POI insert performance`                           |

---

## 🔍 Best Practices

✅ Nutze Imperativform: `add`, `fix`, `remove`, `bump`, …  
✅ Scope ist optional, aber hilfreich  
✅ Keine Punkte oder Emojis im Titel  
✅ Kurze Commit-Nachricht, optional längerer Body  

---

## 📚 Optionaler Body (für komplexe Änderungen)

Nach einer Leerzeile:

```
fix(region): correct tile rendering for zoom level 14

The renderer failed when too many POIs overlapped. We now use a clustering strategy.
```

---

## 📦 Release Automation / Changelog Tools erkennen automatisch:

| Typ    | Wirkung       |
|--------|----------------|
| `feat` | ➕ Neue Version (`minor`) |
| `fix`  | 🐛 Bugfix (`patch`)        |
| `BREAKING CHANGE:` im Body | 🔥 Major-Version |