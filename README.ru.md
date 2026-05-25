# HDT_Reconnector

[English](README.md)

Два плагина HDT для **полей сражений** в одном репозитории:

| Плагин | Релиз | Назначение |
|--------|-------|------------|
| **BGMatchHelper** | `HDT_Reconnector.zip` | Кнопка reconnect для пропуска боя (нужен админ) |
| **BgPickAdvisor** | `HDT_BgPickAdvisor.zip` | Подсказки на выборе героя/аксессуара по мете с **BgMetaApi** |
| **BgMetaApi** | Docker / `dotnet run` | Сервер загрузки `heroes.json` / `trinkets.json` + админка |

> В релизах — **ZIP-архивы** (браузеры ругаются на голые `.dll`). Распакуй и скопируй DLL в папку плагинов HDT.

**BgReplay** — в разработке, в релизы пока не входит (сборка из исходников).

---

## BG Match Helper (reconnect)

Кнопка **reconnect** для матчей **полей сражений** в [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker). Позволяет пропустить бой через краткий разрыв соединения с сервером.

### Требования

- Windows
- Hearthstone Deck Tracker (актуальная версия)
- **HDT запущен от имени администратора** (без этого reconnect не работает)

### Установка

1. Скачай `HDT_Reconnector.zip` из [релиза](https://github.com/qxplays/HDT_Reconnector/releases) или [прямой ссылки](https://github.com/qxplays/HDT_Reconnector/raw/main/releases/HDT_Reconnector.zip) и распакуй.
2. Положи `HDT_Reconnector.dll` в папку плагинов HDT:  
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

1. Скачай `HDT_BgPickAdvisor.zip` из [релиза](https://github.com/qxplays/HDT_Reconnector/releases) или [прямой ссылки](https://github.com/qxplays/HDT_Reconnector/raw/main/releases/HDT_BgPickAdvisor.zip) и распакуй.
2. Положи `HDT_BgPickAdvisor.dll` в `%AppData%\HearthstoneDeckTracker\Plugins\`.
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

### BgReplay (в разработке, не в релизе)

Парсер **не использует** `Core.Game` HDT — читает логи Blizzard (`Power.log` и др.). Собирается из репозитория, в GitHub Releases пока не выкладывается.

**Где кнопка:** не в клиенте HS, а в **HDT**:
1. **Options → Tracker → Plugins** — включи **BgReplay** (галочка Enable).
2. Меню HDT **Plugins → BG Log Replay** (вкл. оверлей).
3. На **оверлее HDT** слева сверху (под меню) кнопка **BG Replay** — видна в меню BG и в матче.
4. На карточке плагина кнопка **Open replay**.

Установка: `HDT_BgReplay.dll` + `BgPowerLog.dll` в папку плагинов.

В окне: укажи **Logs folder** (по умолчанию `C:\Program Files (x86)\Hearthstone\Logs`) → **Apply folder** → **Refresh logs** → выбери `Power.log` или **`Power_old.log`** в подпапке `Hearthstone_2026_…` → **Parse full file** → **Match** → слайдер.

Два места логов:
- `%LocalAppData%\Blizzard\Hearthstone\Logs\` — текущая сессия
- `C:\Program Files (x86)\Hearthstone\Logs\Hearthstone_YYYY_MM_DD_…\` — архив по запускам игры

### Тесты (без игры)

Юнит-тесты (фикстуры JSON):

```text
dotnet test HDT_BgPickAdvisor.Tests\HDT_BgPickAdvisor.Tests.csproj --filter "Category!=Integration"
```

Интеграция с живым API (`http://hsbg.qxplays.ru` по умолчанию):

```text
dotnet test HDT_BgPickAdvisor.Tests\HDT_BgPickAdvisor.Tests.csproj --filter "Category=Integration"
```

Другой сервер: `set BGMETA_API_URL=http://127.0.0.1:5080`  
Без сети / в CI: `set BGPICKADVISOR_SKIP_LIVE_API=1`
