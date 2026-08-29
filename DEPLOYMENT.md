Deployment notes

- On Render set up a Web Service using the existing `Dockerfile`.
- In the service environment, add these secrets (Mark as secret):
  - `JWT_KEY` — a long random secret used to sign JWTs.
  - `INVITE_CODE` — a secret code shared with allowed registrants.

Important: do NOT commit real secret values to the repo. Use the provider's secret store.

Example (Render): Service → Environment → Add Secret → `JWT_KEY`, `INVITE_CODE`.
