# HDT_Reconnector

Плагин для [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker): кнопка **reconnect** в матче **полей сражений** (Battlegrounds) для пропуска боя через кратковременный разрыв TCP-соединения.

## Требования

- Windows
- Hearthstone Deck Tracker (актуальная версия)
- **HDT запущен от имени администратора** (нужно для `SetTcpEntry`)

## Установка

1. Скачай `HDT_Reconnector.dll` из [последнего релиза](https://github.com/qxplays/HDT_Reconnector/releases).
2. Положи файл в папку плагинов HDT:  
   `%AppData%\HearthstoneDeckTracker\Plugins\`  
   (в HDT: **Options → Tracker → Plugins → Plugins folder**).
3. Перезапусти HDT **от администратора**.
4. **Options → Tracker → Plugins** → включи **BGMatchHelper**.
5. Меню **Plugins** → включи **BG Match Helper**.

В матче BG в правом нижнем углу оверлея (со смещением влево и вверх) появится кнопка **reconnect**.

Переместить кнопку: **Options → Overlay → General → Unlock Overlay**.

## Сборка из исходников

```powershell
dotnet build HDT_Reconnector.sln -c Release
```

Нужна установленная HDT в `%LOCALAPPDATA%\HearthstoneDeckTracker`, либо:

```powershell
dotnet build /p:HdtExe="C:\path\to\HearthstoneDeckTracker.exe"
```

Готовый DLL: `HDT_Reconnector\bin\Release\HDT_Reconnector.dll`

## Примечание

Имя плагина в HDT — **BGMatchHelper** (не «Reconnector»): официальный HDT блокирует плагины с именами вроде Reconnector и статический импорт `iphlpapi`.

## Лицензия

MIT
