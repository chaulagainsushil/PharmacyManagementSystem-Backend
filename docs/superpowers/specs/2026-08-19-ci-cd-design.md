# CI/CD for pharmacy-api and pharmacy-ui

Date: 2026-08-19
Status: Approved for planning

## Context

`pharmacy-api` (.NET 10 solution, PostgreSQL) and `pharmacy-ui` (Next.js 16,
git submodule with its own Gitea remote) have no CI/CD today. Both repos live
on the same Gitea host as the `muna` project (`git.samyeak.com.np`), which
already has a working Gitea Actions CI/CD setup. This design ports muna's
patterns to both pharmacy repos, adapted to their stacks.

Key facts gathered during discovery:
- Git remotes for both repos: `git.samyeak.com.np:siddhasoft/<repo>.git` →
  Gitea Actions is the CI platform (not GitHub Actions).
- pharmacy-api: single test project (`PMS.Tests`) using EF Core InMemory —
  no real Postgres needed to run tests. No private NuGet feed (no
  `NuGet.config`), so no NuGet auth needed in CI, unlike muna.
- pharmacy-api has a dev-oriented `Dockerfile` + `docker-compose.yml`
  already, but no `docker-compose.prod.yml` and no registry-based image
  workflow. The dev compose has no bundled `db` service — Postgres is
  external via `DB_CONNECTION_STRING`.
- pharmacy-ui has no Dockerfile, no test framework (ESLint only), and a
  `.env.production` file, suggesting it may currently be built/run directly
  on a server rather than containerized. `NEXT_PUBLIC_API_URL` is a required
  build-time env var (validated in `next.config.ts`), so it must be baked in
  at Docker build time via build args, not at container runtime.
- Only a `main` branch exists today in pharmacy-api; muna's `dev` branch
  model is being adopted fresh, not migrated from an existing branch.

## Decisions (confirmed with user)

1. **Branch/release model**: adopt muna's `main` + `dev` model for both
   repos. `dev` pushes build a mutable `dev` channel image and auto-deploy
   to a test server. `v*` tags build a `release` channel image
   (`X.Y.Z` + `latest`); production deploy stays manual/out-of-CI, matching
   muna.
2. **Auto-deploy**: SSH-based, same shape as muna — CI SSHes into a test box
   that already has `docker-compose.prod.yml` + `.env` in a known directory
   and runs `docker compose pull && up -d` for the relevant service.
3. **UI containerization**: pharmacy-ui gets a new Dockerfile and is
   deployed the same way as the API (registry image + SSH compose deploy),
   for consistency with the API's already-Dockerized flow, even though this
   is a bigger lift than a non-Docker deploy would be.
4. **Notifications**: both CI and CD workflows in both repos get muna's
   ntfy notify step, gated behind an optional `NTFY_URL` variable (no-op
   until configured).

## Architecture

Both repos use Gitea Actions workflows under `.gitea/workflows/`. Because
pharmacy-ui is a separate Gitea repo (checked out here as a submodule), its
workflow files and Dockerfile are committed and pushed from within
`pharmacy-ui/` against its own remote — they do not live in the
pharmacy-api repo's git history.

### pharmacy-api (this repo)

- `.gitea/workflows/build-and-test.yml` — CI
- `.gitea/workflows/publish-docker.yml` — CD
- `docker-compose.prod.yml` — new, production compose for the `pms-api`
  service only (no bundled `db`, matching the existing dev compose's
  external-Postgres assumption)
- `docs/ci-cd.md` — new, config/secrets/variables guide and runbook
- `CLAUDE.md` — new, root-level project guide (none existed before)

### pharmacy-ui (submodule / separate repo)

- `Dockerfile` — new, multi-stage Next.js build
- `next.config.ts` — add `output: 'standalone'`
- `.gitea/workflows/build-and-test.yml` — CI
- `.gitea/workflows/publish-docker.yml` — CD
- `docker-compose.prod.yml` — new, production compose for the `pharmacy-ui`
  service

## Workflow details

### pharmacy-api CI (`build-and-test.yml`)

Trigger: `pull_request` and `push` on `[main, dev]`.

Single job `test` (no Postgres service needed):
1. Checkout
2. Setup .NET 10 (`actions/setup-dotnet@v4`)
3. Cache `~/.nuget/packages` keyed on `Directory.Packages.props`-equivalent
   files (this repo has no central package management file, so key on
   `**/*.csproj` hashes instead)
4. `dotnet restore PMS.sln` (no NuGet auth needed — no private feed)
5. `dotnet build PMS.sln -c Release --no-restore`
6. `dotnet test PMS.Tests/PMS.Tests.csproj -c Release --no-build --logger
   "trx;LogFileName=PMS.Tests.trx" --results-directory TestResults`, with
   `continue-on-error: true` so the summary step always runs
7. Parse the `.trx` into a `$GITHUB_STEP_SUMMARY` table (same grep-based
   parser muna uses, simplified for a single project) and fail the job if
   tests failed
8. `notify` job (`needs: test`, `if: always()`) pings ntfy with pass/fail,
   skipping silently if `vars.NTFY_URL` is unset

### pharmacy-api CD (`publish-docker.yml`)

Trigger: `push` to `dev`, and tags matching `v*`.

`publish` job:
- Resolve channel/version identically to muna: tag → `channel=release`,
  `version=<tag without v>`, tags `pms-api:<version>` + `pms-api:latest`;
  `dev` push → `channel=dev`, tags `pms-api:dev` + `pms-api:dev-<short-sha>`
- `docker/setup-buildx-action`, `docker/login-action` against
  `git.samyeak.com.np` using `secrets.REGISTRY_USER` /
  `secrets.REGISTRY_TOKEN`
- `docker/build-push-action`, context `.`, file `Dockerfile`, platform
  `linux/amd64`, push `true`, tags from the resolve step. No build-args
  needed (no NuGet auth, no build-time env baking for the API)
- Step summary with channel/tags/digest
- ntfy notify (always)

`deploy-test` job (`needs: publish`, `if: needs.publish.outputs.channel ==
'dev' && vars.TEST_DEPLOY_ENABLED == 'true'`):
- SSH in using `secrets.TEST_SSH_HOST`, `secrets.TEST_SSH_USER`,
  `secrets.TEST_SSH_KEY`, run in `vars.TEST_DEPLOY_DIR`:
  `docker compose -f docker-compose.prod.yml pull pms-api && docker compose
  -f docker-compose.prod.yml up -d pms-api && docker image prune -f`
- No-ops (exit 0) if SSH secrets aren't set, same as muna

### pharmacy-api `docker-compose.prod.yml`

One `pms-api` service:
- `image: ${REGISTRY:-git.samyeak.com.np}/${NAMESPACE:-siddhasoft}/${IMAGE_NAME:-pms-api}:${IMAGE_TAG:-latest}`
- `env_file: .env` (same variable names as current `.env.example`:
  `DB_CONNECTION_STRING`, `JWT_SECRET`, `JWT_EXPIRY_MINUTES`,
  `AllowedOrigins`, `ASPNETCORE_ENVIRONMENT`)
- Port bound to `127.0.0.1:${API_PORT:-8080}:8080` (loopback only, assumes
  a reverse proxy in front — matches muna's rationale of not exposing the
  API directly)
- Healthcheck reusing the existing curl-based check from the dev compose
- `restart: unless-stopped`, json-file logging with rotation

### pharmacy-ui Dockerfile (new)

Multi-stage build:
- Stage 1 (`node:22-alpine`, `deps`): `npm ci`
- Stage 2 (`node:22-alpine`, `builder`): copy deps + source; accept
  `ARG NEXT_PUBLIC_API_URL`, `ARG NEXT_PUBLIC_APP_NAME`,
  `ARG NEXT_PUBLIC_APP_VERSION`, export as `ENV` before `npm run build` (so
  Next.js inlines them into the client bundle)
- Stage 3 (runtime, `node:22-alpine`): non-root user, copy
  `.next/standalone`, `.next/static`, `public/` from the builder,
  `EXPOSE 3000`, `CMD ["node", "server.js"]`

Requires adding `output: 'standalone'` to `pharmacy-ui/next.config.ts`.

### pharmacy-ui CI (`build-and-test.yml`)

Trigger: `pull_request` and `push` on `[main, dev]`.

Single job:
1. Checkout
2. Setup Node 22 (`actions/setup-node@v4`), cache npm via `actions/cache@v4`
   keyed on `package-lock.json`
3. `npm ci`
4. `npm run lint`
5. `npx tsc --noEmit`
6. `npm run build`, with placeholder values for the required build-time env
   vars (e.g. `NEXT_PUBLIC_API_URL=http://localhost:5259`) purely to
   validate the build compiles — real values are injected at Docker build
   time in CD, not here
7. `notify` job pings ntfy, same gating as the API

### pharmacy-ui CD (`publish-docker.yml`)

Trigger: `push` to `dev`, and tags matching `v*`.

`publish` job: same channel/version resolution as the API
(`pharmacy-ui:dev`/`dev-<sha>` vs `pharmacy-ui:<version>`/`latest`). Build
args:
- `NEXT_PUBLIC_API_URL`: `vars.DEV_API_URL` for `dev` channel,
  `vars.PROD_API_URL` for `release` channel
- `NEXT_PUBLIC_APP_NAME` / `NEXT_PUBLIC_APP_VERSION`: static/derived
  (app name constant, version from the resolved tag for release builds)

Push to `git.samyeak.com.np/siddhasoft/pharmacy-ui`. Summary + ntfy notify.

`deploy-test` job: same SSH deploy pattern and secrets as the API
(`TEST_SSH_HOST`/`TEST_SSH_USER`/`TEST_SSH_KEY`, same test box), but its own
`vars.TEST_UI_DEPLOY_DIR` (separate directory holding pharmacy-ui's
`docker-compose.prod.yml` + `.env`), still gated on
`vars.TEST_DEPLOY_ENABLED`.

### pharmacy-ui `docker-compose.prod.yml`

One `pharmacy-ui` service pulling
`${REGISTRY:-git.samyeak.com.np}/${NAMESPACE:-siddhasoft}/${IMAGE_NAME:-pharmacy-ui}:${IMAGE_TAG:-latest}`,
port bound to `127.0.0.1:${UI_PORT:-3000}:3000`, `restart: unless-stopped`.

## Config guide document (`docs/ci-cd.md`, in pharmacy-api)

Contents:
- Architecture summary (two-workflow-per-repo pattern, dev/release channels)
- Required Gitea **secrets** (repo-level, set per repo since api/ui are
  separate Gitea repos): `REGISTRY_USER`, `REGISTRY_TOKEN`,
  `TEST_SSH_HOST`, `TEST_SSH_USER`, `TEST_SSH_KEY`, `NTFY_TOKEN` (optional)
- Required **variables**: `TEST_DEPLOY_ENABLED`, `TEST_DEPLOY_DIR`
  (api repo), `TEST_UI_DEPLOY_DIR` (ui repo), `NTFY_URL` (optional),
  `DEV_API_URL`, `PROD_API_URL` (ui repo only)
- One-time server setup: create deploy directories on the test box, place
  `docker-compose.prod.yml` + `.env` in each, generate/install an SSH
  keypair for the CI deploy user, `docker login` once if needed
- Release runbook: `git tag vX.Y.Z && git push --tags` to build a release
  image; production deploy is manual (`docker compose pull && up -d` on the
  prod box), out of CI scope for now

## CLAUDE.md

- `pharmacy-ui/CLAUDE.md` already exists (`@AGENTS.md` include) — untouched.
- New `CLAUDE.md` at the pharmacy-api repo root: solution layout overview
  (`PMS`, `PMS.Application`, `PMS.Repository`, `PMS.Models`, `PMS.Common`,
  `PMS.Tests`), local build/test/run commands, and a pointer to
  `docs/ci-cd.md` for the pipeline.

## Out of scope

- Production auto-deploy (stays manual, per decision #1).
- Migrating pharmacy-ui off its current (presumed) non-Docker deploy until
  the new Docker-based flow is verified.
- Adding a UI test framework — CI only lints/typechecks/builds since no
  tests exist today.
