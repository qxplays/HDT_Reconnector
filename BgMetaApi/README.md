# BgMetaApi

HTTP service for **HDT BgPickAdvisor** meta dumps (`heroes.json`, `trinkets.json`).

## Run locally

```bash
cd BgMetaApi
dotnet run
```

- Admin UI: http://localhost:5080/Admin (default `admin` / `admin`)
- Public JSON: `GET /api/meta/heroes`, `GET /api/meta/trinkets`

## Docker

From repo root:

```bash
docker compose up -d --build
```

Copy `.env.example` to `.env` and set `META_ADMIN_PASSWORD`.

| Variable | Default | Description |
|----------|---------|-------------|
| `META_ADMIN_USERNAME` | `admin` | Admin + Basic auth user |
| `META_ADMIN_PASSWORD` | `admin` | Admin + Basic auth password |
| `META_DATA_DIR` | `/data` | Storage for JSON files |

## Upload API (authorized)

Cookie (after admin login) or HTTP Basic (`admin` / password):

```bash
curl -u admin:admin -F "file=@heroes.json" http://localhost:5080/api/upload/heroes
curl -u admin:admin -F "file=@trinkets.json" http://localhost:5080/api/upload/trinkets
```

Raw JSON body also works: `Content-Type: application/json`.

## HDT plugin

The plugin loads meta only from the API. Default: `http://hsbg.qxplays.ru`.

Override with `meta-api.url` next to `HDT_BgPickAdvisor.dll` or `BGMETA_API_URL`.
