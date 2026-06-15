This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Environment

Copy `.env.example` to `.env.local` and adjust as needed:

```bash
cp .env.example .env.local
```

## Authentication

Login is optional in local development. To enable or disable auth on the frontend and backend (including JWT settings and dev auto-login), see **[docs/AUTH.md](../../docs/AUTH.md)** at the repo root.

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy the Next.js frontend is [Vercel](https://vercel.com):

1. Import the GitHub repo `monaarthur/cls-budget-app`.
2. Set **Root Directory** to `frontend/cls-budget-web`.
3. Add environment variables from `.env.example` (use your production API URL).

See **[docs/DEPLOYMENT.md](../../docs/DEPLOYMENT.md)** for the full multi-platform guide (Vercel + Azure + Supabase).
