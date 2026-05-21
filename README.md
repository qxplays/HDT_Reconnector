# HDT_Reconnector

[Русская версия](README.ru.md)

Two HDT plugins in one repository for **Battlegrounds**:

| Plugin | DLL | Purpose |
|--------|-----|---------|
| **BGMatchHelper** | `HDT_Reconnector.dll` | Reconnect button to skip combat (needs admin) |
| **BgPickAdvisor** | `HDT_BgPickAdvisor.dll` | Hero/trinket pick hints from BgMetaApi JSON meta |

---

## BG Match Helper (reconnect)

Reconnect button for **Battlegrounds** matches in [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker). Use it to skip combat by briefly dropping the game connection.

### Requirements

- Windows
- Hearthstone Deck Tracker (recent version)
- **Run HDT as administrator** (required for reconnect to work)

### Install

1. Download `HDT_Reconnector.dll` from the [latest release](https://github.com/qxplays/HDT_Reconnector/releases).
2. Copy it to your HDT plugins folder:  
   `%AppData%\HearthstoneDeckTracker\Plugins\`  
   (in HDT: **Options → Tracker → Plugins → Plugins folder**).
3. Restart HDT **as administrator**.
4. Enable **BGMatchHelper** under **Options → Tracker → Plugins**.
5. In the **Plugins** menu, turn on **BG Match Helper**.

### Use

1. Start a **Battlegrounds** match.
2. A **reconnect** button appears on the overlay (bottom-right area).
3. Click it during combat to disconnect and let the client reconnect (skips the fight).
4. If HDT was not started as admin, the button shows **(need admin rights)** — close HDT, run it as administrator, and try again.

To move the button: **Options → Overlay → General → Unlock Overlay**, then drag it.

---

## BG Pick Advisor (free pick overlay)

Shows tier badges and highlights the best hero or trinket during Battlegrounds picks. Meta comes from **BgMetaApi** JSON (`hero_dbf_id`, `tier_v2`, `group`, etc.).

### Requirements

- Windows
- Hearthstone Deck Tracker (recent version)
- Internet access to the meta API (default: `http://hsbg.qxplays.ru`)

### Install

1. Download `HDT_BgPickAdvisor.dll` from the [latest release](https://github.com/qxplays/HDT_Reconnector/releases).
2. Copy it to `%AppData%\HearthstoneDeckTracker\Plugins\`.
3. Restart HDT.
4. Enable **BgPickAdvisor** under **Options → Tracker → Plugins**.
5. In the **Plugins** menu, turn on **BG Pick Advisor**.

### Use

1. During **hero** or **trinket** selection in Battlegrounds, a pick panel appears at the top of the overlay.
2. The best option has a green border and **Best pick** label (by meta rank / average placement).
3. **Debug offers** — offer snapshot; **Reload meta** — refresh from API.

Override API URL via `meta-api.url` or `BGMETA_API_URL`. Log: `%AppData%\...\BgPickAdvisor\bgpickadvisor.log`.

### Build both plugins

```text
dotnet msbuild HDT_Reconnector.sln /p:Configuration=Release
```

Output: `HDT_Reconnector\bin\Release\HDT_Reconnector.dll` and `HDT_BgPickAdvisor\bin\Release\HDT_BgPickAdvisor.dll`.

### Tests (no game required)

Unit tests (JSON fixtures):

```text
dotnet test HDT_BgPickAdvisor.Tests\HDT_BgPickAdvisor.Tests.csproj --filter "Category!=Integration"
```

Live API integration (default `http://hsbg.qxplays.ru`):

```text
dotnet test HDT_BgPickAdvisor.Tests\HDT_BgPickAdvisor.Tests.csproj --filter "Category=Integration"
```

Override URL: `BGMETA_API_URL` · Skip network: `BGPICKADVISOR_SKIP_LIVE_API=1`
