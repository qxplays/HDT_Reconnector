# HDT_Reconnector

[Русская версия](README.ru.md)

Reconnect button for **Battlegrounds** matches in [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker). Use it to skip combat by briefly dropping the game connection.

## Requirements

- Windows
- Hearthstone Deck Tracker (recent version)
- **Run HDT as administrator** (required for reconnect to work)

## Install

1. Download `HDT_Reconnector.dll` from the [latest release](https://github.com/qxplays/HDT_Reconnector/releases).
2. Copy it to your HDT plugins folder:  
   `%AppData%\HearthstoneDeckTracker\Plugins\`  
   (in HDT: **Options → Tracker → Plugins → Plugins folder**).
3. Restart HDT **as administrator**.
4. Enable **BGMatchHelper** under **Options → Tracker → Plugins**.
5. In the **Plugins** menu, turn on **BG Match Helper**.

## Use

1. Start a **Battlegrounds** match.
2. A **reconnect** button appears on the overlay (bottom-right area).
3. Click it during combat to disconnect and let the client reconnect (skips the fight).
4. If HDT was not started as admin, the button shows **(need admin rights)** — close HDT, run it as administrator, and try again.

To move the button: **Options → Overlay → General → Unlock Overlay**, then drag it.
