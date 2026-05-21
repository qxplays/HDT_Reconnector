# Загрузка меты в BgMetaApi

Плагин больше не качает HSReplay. Нужны файлы в формате API:

- `heroes.json` — массив с `hero_dbf_id`, `tier_v2`, `avg_final_placement`, …
- `trinkets.json` — массив с `trinket_dbf_id`, `group` (`lesser` / `greater`), …

## Вариант 1: админка

1. `docker compose up -d --build`
2. http://localhost:5080/Admin — загрузить оба файла

## Вариант 2: BgMetaDumper

```text
dotnet build tools\BgMetaDumper\BgMetaDumper.csproj -c Release
tools\BgMetaDumper\bin\Release\net472\BgMetaDumper.exe upload C:\path\to\json
```

Переменные: `BGMETA_API_URL`, `META_ADMIN_USERNAME`, `META_ADMIN_PASSWORD`.

## Плагин

При старте качает `GET http://hsbg.qxplays.ru/api/meta/heroes` и `.../trinkets`.  
Другой URL — только через `meta-api.url` или `BGMETA_API_URL`.
