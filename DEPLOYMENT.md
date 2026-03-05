# Deployment Guide — Remote VM via Tailscale

Everything runs via Docker Compose: one command starts the whole stack (Postgres, API, frontend), and a systemd service ensures it restarts on boot.

## How it works

```
Browser → :3000 (Next.js) → rewrites /api/* → api:5000 (.NET API) → postgres:5432
```

The Next.js container proxies `/api/...` requests to the .NET API internally, so only port 3000 needs to be exposed.

---

## 1. Initial setup on the VM

SSH in via Tailscale, then confirm Docker is installed:

```bash
docker --version
docker compose version
```

If Docker Compose isn't available as a plugin, install it:

```bash
sudo apt-get install docker-compose-plugin
```

---

## 2. Create the .env file on the VM

The `.env` file holds secrets and is **never committed to git**.

```bash
cd ~/middagsklok   # or wherever the repo is cloned
cp .env.example .env
nano .env
```

Fill in the values:

```
POSTGRES_PASSWORD=something_strong
CLAUDE_API_KEY=sk-ant-api03-...
```

---

## 3. Migrate PostgreSQL data from local Docker to the VM

### Dump from local machine

```bash
# Find your local Postgres container
docker ps | grep postgres

# Dump the database
docker exec <container-name> pg_dump -U postgres middagsklok > middagsklok_backup.sql
```

### Copy to the VM

```bash
scp middagsklok_backup.sql user@<tailscale-hostname>:~/middagsklok_backup.sql
```

### Restore on the VM

First start only Postgres so you can restore into it:

```bash
cd ~/middagsklok
docker compose up -d postgres

# Wait for it to be healthy, then restore
cat ~/middagsklok_backup.sql | docker compose exec -T postgres psql -U postgres -d middagsklok
```

---

## 4. Start everything

```bash
cd ~/middagsklok
docker compose up -d --build
```

The app will be available at `http://<tailscale-hostname>:3000`.

To check logs:

```bash
docker compose logs -f          # all services
docker compose logs -f api      # just the API
docker compose logs -f frontend # just the frontend
```

---

## 5. Start on boot (systemd)

Create a systemd service that runs `docker compose up` on startup:

```bash
sudo tee /etc/systemd/system/middagsklok.service << EOF
[Unit]
Description=Middagsklok
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=$(pwd)
ExecStart=/usr/bin/docker compose up -d
ExecStop=/usr/bin/docker compose down

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable middagsklok
```

> Run this from inside `~/middagsklok` so that `$(pwd)` captures the correct path, or replace it manually.

---

## 6. Updating after a git push

### Option A: Manual

SSH in and run:

```bash
cd ~/middagsklok
git pull
docker compose up -d --build
```

### Option B: GitHub Actions (automatic on push to main)

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.VM_HOST }}
          username: ${{ secrets.VM_USER }}
          key: ${{ secrets.VM_SSH_KEY }}
          script: |
            cd ~/middagsklok
            git pull
            docker compose up -d --build
```

Add these secrets in GitHub (Settings → Secrets and variables → Actions):

| Secret | Value |
|--------|-------|
| `VM_HOST` | Tailscale hostname or IP |
| `VM_USER` | Your SSH username |
| `VM_SSH_KEY` | Private key contents (`cat ~/.ssh/id_ed25519`) |

---

## Useful commands

```bash
# Restart a single service after a config change
docker compose restart api

# Stop everything
docker compose down

# View database
docker compose exec postgres psql -U postgres -d middagsklok

# Force rebuild without cache
docker compose build --no-cache
docker compose up -d
```

---

## Security notes

- The `.env` file contains your API key and DB password — keep it on the server only
- Consider rotating your Claude API key if it has ever been shared or committed: https://console.anthropic.com
- The Postgres port is not exposed to the host (only reachable between containers), so no external access
