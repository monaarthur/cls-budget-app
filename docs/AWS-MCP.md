# AWS MCP setup (Cursor)

This doc explains **uvx**, why the **AWS MCP** plugin in Cursor needs it, and which project files relate to AWS (vs. local Cursor tooling).

---

## What is `uv`?

[**uv**](https://github.com/astral-sh/uv) is a fast Python package and project manager from Astral (same team behind Ruff). On Windows it installs a small CLI named `uv`.

You do **not** use uv directly for CLS Budget app development (.NET + Next.js). It is only required because **Cursor’s AWS MCP plugin** uses it to launch the AWS MCP server.

---

## What is `uvx`?

**`uvx`** is a companion command that ships with `uv`. Think of it like:

| Tool | Analogy | What it does |
|------|---------|----------------|
| `npx` | Node.js | Run an npm package without installing it globally |
| **`uvx`** | Python | Run a Python tool from PyPI in an isolated environment |

The AWS MCP plugin runs something equivalent to:

```text
uvx awslabs.aws-api-mcp-server@latest
```

That downloads and starts the official [AWS Labs MCP server](https://github.com/awslabs/mcp), which exposes tools (S3, CloudFront, IAM, etc.) to the AI in Cursor.

---

## Why AWS MCP needs `uvx`

Cursor’s **aws-mcp** plugin (`plugin-aws-core-aws-mcp`) does not embed the server. It **spawns** the server as a child process using `uvx`.

If `uvx` is missing from your PATH, the MCP log shows:

```text
'uvx' is not recognized as an internal or external command
Connection failed: MCP error -32000: Connection closed
```

That is a **local tooling** error, not an AWS permission error.

---

## Install `uv` / `uvx` on Windows

**Recommended (what we used):**

```powershell
winget install --id astral-sh.uv -e
```

**Verify (open a new terminal after install):**

```powershell
uv --version
uvx --version
```

Then **fully quit and restart Cursor** so it picks up the updated PATH.

---

## AWS credentials (second requirement)

`uvx` only starts the MCP server. The server uses your **local AWS credential chain** (same as the AWS CLI).

Configure credentials:

```powershell
aws configure
```

Or SSO:

```powershell
aws sso login --profile your-profile-name
```

**Verify:**

```powershell
aws sts get-caller-identity
```

If this fails with `InvalidClientTokenId`, fix credentials before expecting MCP to work.

---

## What is *not* in this Git repo

These live on your machine / in Cursor, not in `cls-budget-app`:

| Item | Location |
|------|----------|
| AWS MCP plugin | Cursor → Settings → MCP (`aws-mcp`) |
| `uv` / `uvx` binaries | Installed via winget (user PATH) |
| AWS access keys / SSO session | `~/.aws/credentials` or SSO cache |
| Cursor MCP config (optional) | `%USERPROFILE%\.cursor\mcp.json` |

No `mcp.json` or `uvx` references were added to this repository for AWS MCP.

---

## AWS-related files *in* this repository

These are for **deploying the frontend to S3 via GitHub Actions**, not for Cursor MCP:

| File | Purpose |
|------|---------|
| [`.github/workflows/AWS-DeployReactAppS3.yml`](../.github/workflows/AWS-DeployReactAppS3.yml) | Build static Next.js export (`out/`) and `aws s3 sync` to `cls-budget-app-prod` |
| [`.github/workflows/migrate-database.yml`](../.github/workflows/migrate-database.yml) | Supabase EF migrations (not AWS) |
| [`.github/workflows/deploy-api.yml`](../.github/workflows/deploy-api.yml) | Azure API deploy (not AWS) |

**Removed:** `.github/workflows/DeployFrontEnd` (broken duplicate workflow; wrong paths and S3 URL).

**Frontend changes for S3 static hosting** (same deploy effort, not MCP):

- `frontend/cls-budget-web/next.config.ts` — static export when `NEXT_OUTPUT=export`
- `frontend/cls-budget-web/app/budgets/detail/page.tsx` — budget detail via query string (static-hosting friendly)
- Auth/client updates to call the Render API directly in production

GitHub secret required for S3 workflow: `NEXT_PUBLIC_API_BASE_URL`  
Optional: `CLOUDFRONT_DISTRIBUTION_ID`

---

## Troubleshooting checklist

| Symptom | Fix |
|---------|-----|
| `'uvx' is not recognized` | Install uv via winget; restart Cursor |
| MCP connects but AWS calls fail | Run `aws configure` or `aws sso login` |
| S3 deploy workflow fails | See [DEPLOYMENT.md](DEPLOYMENT.md); check GitHub Actions logs |
| MCP green but I want S3 deploy help | Paste Actions log errors; MCP can inspect buckets after credentials work |

---

## Summary

| Question | Answer |
|----------|--------|
| What is `uvx`? | Python tool runner (like `npx` for Node) |
| Why for AWS MCP? | Cursor’s plugin uses it to launch `awslabs.aws-api-mcp-server` |
| Repo file for `uvx`? | **None** — install locally with winget |
| Repo files for AWS deploy? | `.github/workflows/AWS-DeployReactAppS3.yml` (+ frontend static-export changes) |
