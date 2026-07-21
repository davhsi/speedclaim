# Image Strategy for SpeedClaim Landing Page

> **Design reference.** This document proposes imagery for a landing-page redesign; it is not a
> current UI asset inventory or a requirement for the authenticated portal.

Research brief on how images work, why they beat text for emotional pull, and what we should actually do.

---

## 1. Why the Current Page Feels Like a Template

The pattern the user identified:

```
[ PILL BADGE — "IRDAI Registered · 97.3% Claim Ratio" ]
Big bold headline
Sub-headline in smaller grey text
Two pill-shaped CTA buttons
```

This is not bad design — it's *overused* design. It's the output of every Tailwind + shadcn AI generation in 2024–25. The brain pattern-matches it instantly as "generic SaaS landing" and skips reading. The trust signals (97.3%, IRDAI) become noise because they're in a pill badge — the exact same position where every site puts marketing copy.

---

## 2. How Images Process vs. Text

**The 60,000x stat** (MIT, 2014): The brain processes images 60,000× faster than text. More precisely: visual-to-emotional pathway (V1 → amygdala) fires in 13ms. Reading and semantic processing takes 250–400ms.

**Practical implication for insurance:**
- A photo of a woman looking relieved while reading her phone outside a hospital = emotional context in 13ms
- "97.3% claims paid on first submission" = 3 seconds to read + 2 seconds to believe it

**Dual coding theory (Paivio, 1971):** Information encoded both visually and verbally is remembered 65% better than text alone. This is why the same stat feels more credible next to a relevant image — the brain stores two memories, not one.

---

## 3. Image Types and Their Psychological Function

### A. Faces — The Most Powerful Element on Any Page

Eye-tracking studies (Nielsen Norman Group, 2010) consistently show:
- Faces are the **first fixation point** on any page — before headlines, before logos
- A face looking at a CTA button **redirects the user's gaze to the CTA** — this is called "directional cues"
- A genuine expression (not stock-photo forced smile) builds trust faster than any testimonial text

**What works for SpeedClaim:** A candid photo of someone checking their phone with visible relief. Not a model in a stock photo studio. The Sunitha / Priya testimonials in our copy need a face to land.

### B. The Product in Context (Interface Screenshots)

For digital products, showing the actual app interface:
- Reduces anxiety ("I can see what I'm signing up for")
- Makes abstract benefits concrete ("Upload documents" → you can *see* the upload screen)
- Our claim timeline card in the hero is text-based right now. A phone mockup showing the actual app UI would do more.

**What works for SpeedClaim:** A phone frame showing the claim status screen ("₹3,20,000 credited") at the exact moment of resolution. No feature tour. Just the one screen that represents the outcome.

### C. Outcome Illustrations (Abstract but Emotional)

When you can't use real photos (privacy, cost, authenticity concerns), hand-drawn or flat illustrations that represent outcomes — not features — work well.

**Good:** Illustration of a family sleeping peacefully, with a small insurance shield hovering in the corner (protection without them thinking about it)  
**Bad:** Illustration of a person uploading a document on a laptop (showing process, not outcome)

Key distinction: **outcome illustrations** vs. **process illustrations**. Insurance buyers don't want to see the claims process illustrated — they want to see their life after the claim is resolved.

### D. Proof Photography (Real, Messy, Human)

The highest-converting images in fintech/insurtech are ones that look like they could've been taken on an iPhone. Hospital corridor. Rain on a windshield. A stack of medical bills on a kitchen table. These images earn trust because they acknowledge the messy reality the user actually lives in.

**Pattern:**  
> Before-image (the bad moment) → claim card UI (the resolution) → feeling of relief

This is the SpeedClaim story in three beats.

---

## 4. Quirky Witty Copy + Images — How They Compound

Wit without an image is a headline that needs to be re-read. An image without wit is visually pleasant but forgettable. Together:

**The pattern:**
- Image establishes emotional context instantly (13ms)
- Witty/unexpected headline reframes it, triggers a micro-smile or "wait, what?" response
- That moment of surprise increases memory encoding (the "von Restorff effect" — distinctive items are remembered better)

### Examples from sites that get this right:

**Headspace** (meditation app):
> Image: peaceful sleeping person  
> Headline: "Some nights you lie awake. Others, you don't."

The image + the dry understatement creates a 2-beat joke. The user smiles. The smile creates a positive association with the brand before they've read a single feature.

**Monzo** (UK neobank):
> Image: coral card sitting on coffee mug  
> Headline: "A bank that lives on your phone and won't make you want to punch it."

The image is a physical product that looks unusually good. The headline is the kind of thing you'd say to a friend, not a bank. The specificity of "punch it" is what makes it land — it names the exact frustration without explaining it.

**What this means for SpeedClaim:**
Our headline "When the worst happens, you get money — not excuses." is good. But without an image that emotionally primes the reader, they arrive at "the worst happens" cold. If the image above it already conjures that feeling, the headline becomes a *confirmation* of what they're already feeling, not a description they have to imagine.

---

## 5. The Patterns That Are Dead (What to Avoid)

### 5a. The Gradient Hero with Floating Cards
```
[teal gradient background]
  [white card floating, rotated 3deg]  
  [another card floating, rotated -2deg]
```
Every fintech app built between 2021–2024. Our current page doesn't do this aggressively but the hero card has the same DNA.

### 5b. Illustration with No Context
Stick-figure-style people holding giant phones or coins, in teal and coral colors. These illustrations say "startup" but nothing about the actual problem or outcome. Users scroll past.

### 5c. The Team Photo Grid
A 3×2 grid of headshots from people who "care deeply about your claim." Nobody believes this.

### 5d. Meaningless Abstract 3D
Glass morphism orbs, abstract 3D blobs. These were a design trend. They're now a sign the site was generated.

---

## 6. What SpeedClaim Should Actually Do

### Priority 1 — One hero image that does the work

Replace the hero card mockup with a **real-feeling photograph** or a **phone frame mockup showing the actual app UI**.

Two options:

**Option A (stock photo, high bar):**  
Find an Unsplash/Pexels photo that captures "relief in a hard moment." Not a smiling person in an office. Something like: a woman sitting in a hospital waiting room, hands around a paper cup of chai, staring at her phone. Something that doesn't look like stock.

- Unsplash search: `hospital waiting room`, `relief phone`, `notification phone night`  
- Pexels search: `hospital india phone relief`
- Key filter: no obvious stock-photo staging (clean white backgrounds, unnatural smiles)

**Option B (app mockup, safer):**  
A phone frame (CSS/SVG frame we can build) showing the SpeedClaim claim status screen: claim number, "₹3,20,000 Approved", green checkmark, "Credited to HDFC ••••4821." This is what the user *actually* wants to see they'll get. It proves the product works.

### Priority 2 — Faces on testimonials

Right now testimonials use initials (PM, KR) in colored circles. These read as fake. Options:

- **Avataaars / DiceBear** — algorithmic avatar generators. Consistent art style, not stock photos, but more human than colored initials. Free, can generate in SVG.
- **Generated.photos** — AI-generated faces (ethically sourced, license allows commercial use). Looks real, is not a real person.
- Leave as initials but add a real-feeling location photo as a background to the testimonial card instead.

### Priority 3 — One illustration in the "How it works" section

Replace the numbered circles with a single illustration that shows the journey. Not three separate images — one continuous illustration, like a map or timeline with character states.

Source: **unDraw.co** — MIT-licensed SVG illustrations, can change brand color via URL param. Search: `insurance`, `contract`, `approved`, `notification`.

### Priority 4 — The "Peak Moment" section (currently pure text)

This is the section: "Baba had surgery on Saturday." — this is our strongest copy. It needs:
- A phone notification mockup (we can build this in pure HTML/CSS/SVG)
- Or a blurred photo of a hospital scene as a section background, with the text overlaid on a semi-transparent white card

---

## 7. SVG Illustrations Claude Can Write

Things I can write directly as inline SVG in the Angular templates, no image files needed:

1. **Phone frame mockup** with our claim UI rendered inside it (the ₹3,20,000 credited screen)
2. **Notification card** that looks like a real iOS push notification ("SpeedClaim: ₹3,20,000 credited")
3. **Simple human figures** in a flat illustration style (family, hospital scene)
4. **Shield / protection icons** that are more illustrative than iconographic
5. **Timeline illustration** for the "How it works" section with small character states

These would be hand-coded SVG — fully scalable, no external requests, matches our exact brand colors.

---

## 8. Free Image Sources — Specifically for Insurance/Fintech

### Stock Photos (Free, No Attribution Required)
| Source | Best search terms for us |
|---|---|
| unsplash.com | `hospital india`, `phone notification night`, `family home india`, `car accident road` |
| pexels.com | `insurance claim`, `hospital waiting`, `family protection` |
| pixabay.com | `mobile banking india`, `family safety`, `medical bills` |

**Image quality filter:** Always use landscape images over portrait for hero sections. Avoid any image where someone is looking directly at the camera smiling — looks staged.

### SVG Illustrations (Free, Customizable)
| Source | What it has |
|---|---|
| undraw.co | Insurance, contract, notification, mobile app, family — all in our brand color |
| storyset.com | More detailed, animated-compatible illustrations |
| humaaans.com | Mix-and-match character system — makes unique people scenes |
| icons8.com/illustrations | Flat style, good for fintech |

### Algorithmic Avatars (for Testimonials)
| Source | How to use |
|---|---|
| avataaars.io | Generate + export SVG per testimonial name |
| dicebear.com | URL-based SVG generation, can seed by name for consistency |

---

## 9. Implementation Path

**Step 1 — Hero:** Replace or supplement the existing hero card with a phone mockup SVG showing the claim resolved. I can build this.

**Step 2 — Testimonials:** Switch from colored initials to Avataaars SVGs. I can generate these inline.

**Step 3 — One stock photo:** Find one strong hospital/relief photo from Unsplash for either the hero background or the "Peak Moment" section. User selects it; I integrate it.

**Step 4 — unDraw illustrations:** Drop 2–3 unDraw SVGs into the "How it works" and coverage sections to break up the text density.

**Step 5 — Wit audit:** Once images are in place, revisit the copy in each section. Some lines will need to shorten or sharpen because the image already handles the emotional setup.

---

## 10. One-Line Summary

> Images give the landing page a heartbeat. The copy gives it a voice. Right now we have voice and no heartbeat — the reader has to imagine the emotion from text alone, which is slower, and which they won't do.
