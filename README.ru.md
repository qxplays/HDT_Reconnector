# HDT_Reconnector

[English](README.md)

Два плагина HDT для **полей сражений** в одном репозитории:

| Плагин | DLL | Назначение |
|--------|-----|------------|
| **BGMatchHelper** | `HDT_Reconnector.dll` | Кнопка reconnect для пропуска боя (нужен админ) |
| **BgPickAdvisor** | `HDT_BgPickAdvisor.dll` | Подсказки на выборе героя/аксессуара по мете с **BgMetaApi** |
| **BgMetaApi** | Docker / `dotnet run` | Сервер загрузки `heroes.json` / `trinkets.json` + админка |

---

## BG Match Helper (reconnect)

Кнопка **reconnect** для матчей **полей сражений** в [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker). Позволяет пропустить бой через краткий разрыв соединения с сервером.

### Требования

- Windows
- Hearthstone Deck Tracker (актуальная версия)
- **HDT запущен от имени администратора** (без этого reconnect не работает)

### Установка

1. Скачай `HDT_Reconnector.dll` из [последнего релиза](https://github.com/qxplays/HDT_Reconnector/releases).
2. Положи файл в папку плагинов HDT:  
   `%AppData%\HearthstoneDeckTracker\Plugins\`  
   (в HDT: **Options → Tracker → Plugins → Plugins folder**).
3. Перезапусти HDT **от администратора**.
4. Включи **BGMatchHelper** в **Options → Tracker → Plugins**.
5. В меню **Plugins** включи **BG Match Helper**.

### Использование

1. Зайди в матч **полей сражений**.
2. На оверлее появится кнопка **reconnect** (в районе правого нижнего угла).
3. Нажми её во время боя — игра отключится и переподключится (бой будет пропущен).
4. Если HDT запущен без прав администратора, на кнопке будет **(need admin rights)** — закрой HDT, запусти от администратора и попробуй снова.

Переместить кнопку: **Options → Overlay → General → Unlock Overlay**, затем перетащи.

---

## BG Pick Advisor (pick overlay)

Показывает тир и лучший вариант на выборе **героя** или **аксессуара**. Мета — JSON с **BgMetaApi** (`hero_dbf_id`, `tier_v2`, `group` и т.д.).

### Требования

- Windows
- Hearthstone Deck Tracker (актуальная версия)
- Интернет и доступ к **http://hsbg.qxplays.ru** (мета по умолчанию)

### Установка

1. Скачай `HDT_BgPickAdvisor.dll` из [последнего релиза](https://github.com/qxplays/HDT_Reconnector/releases).
2. Положи в `%AppData%\HearthstoneDeckTracker\Plugins\`.
3. Перезапусти HDT.
4. Включи **BgPickAdvisor** в **Options → Tracker → Plugins**.
5. В меню **Plugins** включи **BG Pick Advisor**.

### Использование

1. На экране выбора **героя** или **тринкета** вверху оверлея появится панель с тирами.
2. Лучший вариант — зелёная рамка и подпись **Best pick** (по рангу / среднему месту из меты).
3. Кнопка **Debug offers** — снимок офферов и счётчиков меты; **Reload meta** — повторная загрузка с API.

Переопределить API (редко): файл `meta-api.url` рядом с DLL или `BGMETA_API_URL`.

**Лог:** `%AppData%\HearthstoneDeckTracker\Plugins\BgPickAdvisor\bgpickadvisor.log` (кнопка **Log file** в Debug offers).

### BgMetaApi (свой сервер меты)

```bash
docker compose up -d --build
```

- Админка: http://localhost/Admin (логин/пароль из `.env`: `META_ADMIN_*`)
- Загрузка: `POST /api/upload/heroes`, `POST /api/upload/trinkets` (форма или curl + Basic auth)
- Скачивание для плагина: `GET /api/meta/heroes`, `GET /api/meta/trinkets`

Подробнее: [BgMetaApi/README.md](BgMetaApi/README.md)

### Плагин: мета с API

По умолчанию: `http://hsbg.qxplays.ru` → `GET /api/meta/heroes` и `/api/meta/trinkets`.

1. Другой сервер: `meta-api.url` (пример `releases/meta-api.url.example`) или `BGMETA_API_URL`
2. Обновление дампов на сервере: админка `http://hsbg.qxplays.ru/Admin` или `BgMetaDumper upload`
3. **Reload meta** в Debug offers после заливки новых JSON на сервер

> Сейчас API отдаёт JSON по **HTTP**. Если включишь HTTPS на nginx, поменяй URL в `meta-api.url` на `https://...`.

### Сборка обоих плагинов

```text
dotnet msbuild HDT_Reconnector.sln /p:Configuration=Release
```

Результат: `HDT_Reconnector\bin\Release\HDT_Reconnector.dll` и `HDT_BgPickAdvisor\bin\Release\HDT_BgPickAdvisor.dll`.

### Тесты меты (без игры)

```text
dotnet test HDT_BgPickAdvisor.Tests\HDT_BgPickAdvisor.Tests.csproj
```
