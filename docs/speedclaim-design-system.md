# SpeedClaim Design System

SpeedClaim should feel like a premium insurance operations product: quiet, dense, trustworthy, and fast to scan. The logo sets the palette: deep ink, white, and gold. Use color semantically and sparingly.

## Palette

| Token | Hex | Use |
| --- | --- | --- |
| Primary / brand ink | `#091520` | Main buttons, active identities, headers, avatar fills |
| Primary hover | `#111E2B` | Hover state for primary actions |
| Gold accent | `#F5A623` | Primary highlights, selected nav rails, important CTAs |
| Gold surface | `#FFF4DE` | Soft selected backgrounds and warning surfaces |
| Surface | `#F6F7F9` | App background |
| Surface alt | `#ECEFF3` | Skeletons, table headers, subtle hover states |
| Ink text | `#111827` | Headings and dense values |
| Body text | `#374151` | Main body copy |
| Muted text | `#6B7280` | Captions, metadata, helper text |
| Line | `#E5E7EB` | Borders and dividers |
| Info teal | `#0F6E8C` | Informational badges only |
| Success green | `#1F9D6B` | Completed, approved, paid |
| Danger red | `#D14343` | Destructive actions and errors |

## Rules

- Use `primary` for brand and main action surfaces, not teal.
- Use `accent/gold` for highlights and selected navigation, not as a page-wide background.
- Use teal only for informational states. It should not compete with the logo.
- Keep portal shells consistent: dark sidebar, white topbar, light gray page surface, white cards.
- Cards use `rounded-card`, `border-line`, and `shadow-sm`. Avoid nested cards.
- Dense operational screens should prefer small headers, tables, filters, and compact controls over marketing-style hero layouts.
- Buttons should use:
  - Primary action: `bg-primary text-white hover:bg-primary-hover`
  - Secondary action: `bg-white border border-line text-body hover:bg-surface`
  - Destructive action: `text-danger` or `bg-danger text-white`
- Status colors:
  - Approved/Paid/Active: success
  - Pending/Review: warning
  - Rejected/Failed/Expired: danger
  - Informational/Assigned: info
