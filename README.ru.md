# HDT_Reconnector

[English](README.md)

Кнопка **reconnect** для матчей **полей сражений** в [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker). Позволяет пропустить бой через краткий разрыв соединения с сервером.

## Требования

- Windows
- Hearthstone Deck Tracker (актуальная версия)
- **HDT запущен от имени администратора** (без этого reconnect не работает)

## Установка

1. Скачай `HDT_Reconnector.dll` из [последнего релиза](https://github.com/qxplays/HDT_Reconnector/releases).
2. Положи файл в папку плагинов HDT:  
   `%AppData%\HearthstoneDeckTracker\Plugins\`  
   (в HDT: **Options → Tracker → Plugins → Plugins folder**).
3. Перезапусти HDT **от администратора**.
4. Включи **BGMatchHelper** в **Options → Tracker → Plugins**.
5. В меню **Plugins** включи **BG Match Helper**.

## Использование

1. Зайди в матч **полей сражений**.
2. На оверлее появится кнопка **reconnect** (в районе правого нижнего угла).
3. Нажми её во время боя — игра отключится и переподключится (бой будет пропущен).
4. Если HDT запущен без прав администратора, на кнопке будет **(need admin rights)** — закрой HDT, запусти от администратора и попробуй снова.

Переместить кнопку: **Options → Overlay → General → Unlock Overlay**, затем перетащи.
