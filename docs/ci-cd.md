# CI/CD

Both `pharmacy-api` (this repo) and `pharmacy-ui` (submodule, separate Gitea
repo) use Gitea Actions. Each has two workflows under `.gitea/workflows/`:

- `build-and-test.yml` — runs on every PR and push to `main`/`dev`. Builds,
  lints/tests, and posts a summary. Never pushes an image or deploys.
- `publish-docker.yml` — runs on push to `dev` (mutable "dev" channel) and
  on tags matching `v*` (immutable "release" channel). Builds and pushes a
  Docker image to the Gitea container registry, then — for the `dev`
  channel only — deploys it to a test server over SSH.

Production deploys are **manual**, not automated: after pushing a `vX.Y.Z`
tag and letting CI publish the image, deploy it yourself on the prod box
with `docker compose -f docker-compose.prod.yml pull && docker compose -f
docker-compose.prod.yml up -d`.

## Required secrets (set per repo, in each repo's Gitea Settings → Actions → Secrets)

| Secret | Used by | Purpose |
|---|---|---|
| `REGISTRY_USER` | both `publish-docker.yml` | Login to `git.samyeak.com.np` container registry |
| `REGISTRY_TOKEN` | both `publish-docker.yml` | Login to `git.samyeak.com.np` container registry |
| `TEST_SSH_HOST` | both `publish-docker.yml` | Test box address for the `deploy-test` job |
| `TEST_SSH_USER` | both `publish-docker.yml` | SSH user on the test box |
| `TEST_SSH_KEY` | both `publish-docker.yml` | Private key for the SSH user above |
| `NTFY_TOKEN` | both workflows, both repos | Optional — bearer token if your ntfy topic requires auth |

`TEST_SSH_HOST`/`TEST_SSH_USER`/`TEST_SSH_KEY` are the same test box for
both repos — set the same values in both the pharmacy-api and pharmacy-ui
Gitea repos.

## Required variables (Gitea Settings → Actions → Variables)

| Variable | Repo | Purpose |
|---|---|---|
| `TEST_DEPLOY_ENABLED` | both | Set to `true` to turn on the `deploy-test` job. Left unset/false, publish workflows build+push images but skip deploy. |
| `TEST_DEPLOY_DIR` | pharmacy-api | Path on the test box holding `docker-compose.prod.yml` + `.env` for the API |
| `TEST_UI_DEPLOY_DIR` | pharmacy-ui | Path on the test box holding `docker-compose.prod.yml` for the UI |
| `NTFY_URL` | both, both repos | Optional — ntfy topic URL. Left unset, the notify steps no-op. |
| `DEV_API_URL` | pharmacy-ui | `NEXT_PUBLIC_API_URL` baked into the `dev`-channel UI image |
| `PROD_API_URL` | pharmacy-ui | `NEXT_PUBLIC_API_URL` baked into `release`-channel UI images |

`DEV_API_URL`/`PROD_API_URL` are required for `pharmacy-ui`'s
`publish-docker.yml` to succeed — the workflow fails fast with a clear error
if the one it needs for the current channel is unset, since a missing
`NEXT_PUBLIC_API_URL` would otherwise bake `undefined` into the built
frontend.

## One-time test server setup

On the test box:

```bash
mkdir -p /opt/pharmacy-api /opt/pharmacy-ui   # or wherever you want them
# pharmacy-api:
cp docker-compose.prod.yml /opt/pharmacy-api/
cp .env.example /opt/pharmacy-api/.env        # then edit with real test values
# pharmacy-ui:
cp docker-compose.prod.yml /opt/pharmacy-ui/
```

Then set `TEST_DEPLOY_DIR=/opt/pharmacy-api` and
`TEST_UI_DEPLOY_DIR=/opt/pharmacy-ui` as Gitea repo variables.

Generate a dedicated SSH keypair for CI to use, add the public key to
`~/.ssh/authorized_keys` for the deploy user on the test box, and store the
private key as the `TEST_SSH_KEY` secret in both repos:

```bash
ssh-keygen -t ed25519 -f ./ci-deploy-key -C "gitea-ci-deploy" -N ""
```

Finally, log in to the Gitea container registry once from the test box (or
confirm `docker compose pull` can reach it) if the registry requires auth
for pulls as well as pushes.

## Cutting a release

```bash
git tag v1.2.3
git push origin v1.2.3
```

This builds and pushes `pms-api:1.2.3` + `pms-api:latest` (or
`pharmacy-ui:1.2.3` + `:latest` in the UI repo). It does **not** deploy —
do that manually on the prod box once you're ready.
