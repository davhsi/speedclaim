# SpeedClaim frontend

The SpeedClaim customer and staff portal is an Angular 21.2 standalone-component application
styled with Tailwind CSS 4. It includes role-specific insurance workflows for customers, agents,
underwriters, claims, finance, and administration.

The customer portal includes the **Speedy** guided-support workspace. Speedy keeps conversation
history in the customer session, presents grounded brochure evidence in a side panel, offers
safe read/prepare assistance, and leaves every business-changing action to the normal portal
flow. The workspace rail includes conversation search, a single return-to-portal control, and a
compact account link.

## Local development

Run the .NET API at `http://localhost:5062` first, then:

```bash
npm ci
npm start
```

Open `http://localhost:4200`. The development proxy forwards only `/api`, `/uploads`, and
`/hubs` to the local API. Customer route names must not be added to the proxy.

## Verification

```bash
npm test -- --watch=false
npm run build -- --configuration development
npm run build -- --configuration production
```

Use an Angular build rather than TypeScript alone when checking changes: the Angular compiler
also validates templates.

## Configuration

Backend origins are compile-time environment configuration, not secrets. Development uses
relative API URLs through the proxy; production points at the deployed backend origin. Access
tokens remain memory-only, while refresh-token persistence follows the user’s remember-me
choice.

For the full platform setup, API configuration, and deployment information, see the repository
[README](../README.md) and [docs](../docs/).
