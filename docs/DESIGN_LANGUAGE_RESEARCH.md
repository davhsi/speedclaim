# SpeedClaim — Design Language Research

**Companion to:** `docs/LANDING_UX_RESEARCH.md`  
**Purpose:** Evidence-based design language specification for the SpeedClaim landing page redesign.  
**Scope:** Color theory, typography psychology, animation principles, visual hierarchy, shape psychology, and the concrete SpeedClaim design system spec derived from all of the above.

---

## Table of Contents

1. [Why Design Language Matters](#1-why-design-language-matters)
2. [Color Theory Foundations](#2-color-theory-foundations)
3. [Color in Financial Services — What Research Says](#3-color-in-financial-services--what-research-says)
4. [The Indian Fintech Palette Landscape](#4-the-indian-fintech-palette-landscape)
5. [Teal: SpeedClaim's Primary Color — Justified](#5-teal-speedclaims-primary-color--justified)
6. [The Dark Hero Problem](#6-the-dark-hero-problem)
7. [Orange as an Accent — The Warmth vs. Authority Trade-off](#7-orange-as-an-accent--the-warmth-vs-authority-trade-off)
8. [SpeedClaim Palette Decision](#8-speedclaim-palette-decision)
9. [Typography Psychology](#9-typography-psychology)
10. [Font Category Effects on User Behavior](#10-font-category-effects-on-user-behavior)
11. [SpeedClaim Typography System](#11-speedclaim-typography-system)
12. [Visual Hierarchy and Eye Movement](#12-visual-hierarchy-and-eye-movement)
13. [Whitespace as a Design Tool](#13-whitespace-as-a-design-tool)
14. [Shape Psychology — Border Radius](#14-shape-psychology--border-radius)
15. [Animation and Micro-interaction Psychology](#15-animation-and-micro-interaction-psychology)
16. [SpeedClaim Animation Spec](#16-speedclaim-animation-spec)
17. [Photography and Human Faces](#17-photography-and-human-faces)
18. [Indian Market Context](#18-indian-market-context)
19. [The SpeedClaim Design Language Spec](#19-the-speedclaim-design-language-spec)
20. [Vibe Statement — The North Star](#20-vibe-statement--the-north-star)
21. [Connecting to the UX Research Document](#21-connecting-to-the-ux-research-document)

---

## 1. Why Design Language Matters

Color, typography, spacing, and motion are not decoration. They are the product's first conversation with the user — one that happens before a single word is read.

Research from the University of Winnipeg found that people make subconscious judgments about a product within **90 seconds of initial viewing**, with **62–90% of that assessment based on color alone**. This means that before your headline lands, before your testimonials are read, before your pricing is evaluated — the user has already formed an emotional opinion of SpeedClaim based purely on visual language.

For an insurance product, this is especially high-stakes. Insurance purchases are driven by fear, doubt, and distrust. The design language must immediately communicate: *we are trustworthy, we are human, and we are on your side.* If the visual language contradicts that message — through the wrong color temperature, a font that feels clinical, or animations that feel like a sales pitch — the user's nervous system has already filed SpeedClaim under "just another insurance company."

The research across **four domains** — color, typography, animation, and visual hierarchy — points toward the same conclusion: effective fintech design earns trust first, then earns attention, then earns the conversion. In that order. Never reversed.

---

## 2. Color Theory Foundations

### The Mechanics of Color Perception

Color is processed by the brain in two distinct stages:

**Stage 1 — Biological response (0–50ms):** The visual cortex processes wavelength and brightness. Warm colors (red, orange, yellow) trigger mild alertness; cool colors (blue, green, teal) trigger calm. This response is cross-cultural and pre-cognitive — it cannot be overridden by rational thought.

**Stage 2 — Cultural association (50–500ms):** The brain maps color against learned associations — brand exposure, cultural symbolism, personal memory. These vary across cultures, ages, and demographics. What signals "trust" in one context signals "cold" in another.

The critical insight for design: **you cannot control Stage 1, but you must architect for Stage 2.** SpeedClaim's design choices must work at both levels simultaneously.

### The Three Properties of Color

Every color decision involves three independent axes:

| Property | Range | Effect |
|---|---|---|
| **Hue** | 0°–360° on the color wheel | Emotional category (blue = calm, red = urgency) |
| **Saturation** | 0% (gray) → 100% (vivid) | High sat = energetic/loud; Low sat = sophisticated/calm |
| **Lightness** | 0% (black) → 100% (white) | Dark = premium/heavy; Light = open/inviting |

A common mistake: treating hue as the only variable. Deep teal (#0F6E8C) and baby blue (#B3D9F7) are the same hue family but communicate completely different things. The saturation and lightness choices matter as much as the hue choice.

### Color Relationships

- **Monochromatic:** Single hue, varied saturation/lightness. Communicates: refinement, coherence, premium simplicity.
- **Complementary:** Opposite hues. High contrast, high energy. Risk: can feel jarring at full saturation.
- **Analogous:** Adjacent hues. Natural, harmonious, trustworthy.
- **Triadic:** Three evenly spaced hues. Dynamic, varied, but requires discipline to prevent visual chaos.

SpeedClaim's optimal strategy: **analogous primary palette** (teal-to-blue gradient) with a **complementary accent** (warm amber/coral for CTAs). This creates trust through the primary palette and urgency/action through the accent — without the color clashing that a full triadic scheme would produce.

---

## 3. Color in Financial Services — What Research Says

### The Trust Colors

According to research aggregated by [Bethany Works](https://bethanyworks.com/color-psychology-financial-services-brands/) on financial services brands:

- **Blue increases perceptions of trustworthiness by 42%** in professional service contexts
- **54% of consumers identify blue as the most trusted brand color** across all industries
- Blue literally reduces anxiety and **lowers heart rate** during financial discussions
- Blue dominates traditional financial institutions: Bank of America, Chase, American Express, Merrill Lynch — all blue

The dominance of blue in finance is not accidental. It is a self-reinforcing cultural signal: because trusted institutions use blue, blue becomes the signal of trust.

### The Disruption Opportunity

Because blue so completely dominates traditional finance, fintech companies have a differentiation opportunity — but it requires careful execution:

- **Monzo (UK):** Chose hot coral — instantly recognizable, impossible to confuse with traditional banks, beloved by the target demographic (Millennials + Gen Z)
- **Koa (Kenya):** Used guava orange-pink as primary, blue as secondary — warmth signals approachability to a demographic new to formal savings
- **Purple saturation problem:** According to [BFA Global's fintech branding research](https://bfaglobal.com/catalyst-fund/insights/getting-your-fintech-brand-right-stand-out-with-color/), "purple has become the go-to color in the fintech space" and is now overused to the point of invisibility

The key principle: **Break from blue just enough to be distinctive. Not so far that you break trust.**

Teal is the answer to this tension.

---

## 4. The Indian Fintech Palette Landscape

Understanding the competitive visual landscape is critical for differentiation.

### How Indian Leaders Have Chosen Color

| Brand | Primary Color | Strategy |
|---|---|---|
| **Razorpay** | Navy-to-sky-blue gradient | Trust + momentum; "clean lines, no clutter" |
| **Paytm** | Blue | Mass familiarity and trust at scale |
| **PhonePe** | Purple-indigo | Premium + tech-savvy; now saturated |
| **Acko** | Orange-teal | Disruption; orange signals speed + warmth |
| **HDFC Life / LIC** | Blue-orange | Institutional authority + action CTA |

### What This Means for SpeedClaim

The Indian consumer has been trained to associate:
- **Blue → trust → established finance**
- **Orange → action → speed → friendly**
- **Purple → premium → saturated / overused**

An insurance platform targeting urban professionals needs to occupy a specific territory: **"I trust you AND I believe you're modern enough to not waste my time."** Neither pure blue (feels like a PSU bank) nor pure disruption orange (feels like a Swiggy delivery) achieves this.

Teal solves it. It reads as blue-adjacent (trust inheritance) while signaling that SpeedClaim is not a legacy institution. It is the visual equivalent of "we take your money seriously, and we took design seriously too."

---

## 5. Teal: SpeedClaim's Primary Color — Justified

### What Color Research Says About Teal

According to [Awesome Sauce Creative's 2026 branding trend analysis](https://www.awesomesauce.in/insights/transformative-teal-in-branding), teal is emerging as the dominant forward-thinking brand color for exactly the reason SpeedClaim needs:

> "Transformative Teal blends the trust of deep blue with the renewal of green, representing calm innovation, sustainability, and emotional intelligence."

The psychological associations of teal, supported by multiple sources:

| Association | Why It Matters for SpeedClaim |
|---|---|
| **Trust** (inherited from blue) | Insurance requires trust before any other emotion |
| **Growth** (inherited from green) | Customers want to feel their investment grows/protects |
| **Innovation** | Signals "we are not your grandfather's insurance company" |
| **Clarity** | Anti-anxiety; the opposite of opaque fine print |
| **Calm** | De-escalates the fear response triggered by insurance decisions |
| **Sophistication** | Premium signal without the coldness of navy |

### Teal vs. Navy

The existing landing page uses a very dark navy (`#0D1B2A` range) as the hero background. This is a meaningful distinction from teal:

- **Dark navy:** Premium, authoritative, executive-level — but carries documented risk of coldness and emotional distance
- **Deep teal (`#0F6E8C` range):** All of navy's authority, plus warmth through the green undertone, plus distinctiveness from the blue-dominant market

### The Saturation Decision

SpeedClaim's current primary teal (#0F6E8C) is at roughly 75% saturation. This is appropriate for:
- Button fills and interactive elements
- Brand accent on white/light backgrounds
- Icon fills

For large background areas (hero sections, section breaks), **desaturated teal tones** (10–15% saturation, high lightness) feel more premium and less like a budget software company. Deep teal at full saturation over large areas can overwhelm.

**Recommendation:** Keep #0F6E8C as the primary action/accent teal. Introduce `#F0F9FC` (teal-tinted near-white) for hero backgrounds — trust signal without the premium-fee-feel of darkness.

---

## 6. The Dark Hero Problem

### What the Current Landing Page Does

The current hero uses a very dark navy (`#0D1B2A`) with a dot-grid overlay and radial glow effect. This aesthetic comes from SaaS developer tools (Vercel, Linear, Railway) and signals:

- Technical sophistication
- Developer-product credibility
- Premium positioning

### Why This Is Wrong for Insurance

The dark hero is a **context mismatch**. Developer tools use dark themes because:
1. Their users literally stare at dark screens all day (code editors)
2. The darkness signals "serious technical product for serious technical people"
3. It creates excitement and energy for a demographic that associates dark mode with power

Insurance customers are:
1. Often making a purchase driven by anxiety or a recent near-miss event
2. Skeptical by default ("the fine print will catch me")
3. Looking for signals of warmth, clarity, and human care

A dark hero triggers the "cold + powerful corporation" mental model. This is the exact brand position that Lemonade, Acko, and Digit have all explicitly run *away* from. Dark = opaque = intimidating. The user subconsciously thinks: *they're hiding something.*

### What Research Supports Instead

From [Kuva Media's brand psychology research](https://kuvamedia.com/blog/the-real-color-psychology-for-branding-why-blue-means-trust-is-only-half-the-story):

> "What feels premium to a 35-year-old professional services buyer may feel cold to a 45-year-old healthcare patient. Colors mixed with black (shades) increase perceived authority and exclusivity while potentially decreasing approachability."

For SpeedClaim's audience — urban Indian professionals aged 25–45, first-time or repeat insurance buyers, often female decision-makers for family floater plans — **approachability is more valuable than authority**.

**Recommendation:** Replace dark hero with a **light, teal-tinted background** with depth created through layered content, not darkness. Reserve dark elements for section dividers and footer only.

---

## 7. Orange as an Accent — The Warmth vs. Authority Trade-off

### What Research Says About Orange in Finance

From [Bethany Works](https://bethanyworks.com/color-psychology-financial-services-brands/) on financial brand color:

> "Orange is friendly and approachable but lacks the authority most financial clients seek. It can work for fintech startups or financial literacy brands targeting younger audiences."

From the [Robust Branding CTA color analysis](https://robustbranding.com/color-psychology-boosts-cta-clicks):

> "Orange balances energy and friendliness, making it ideal for sign-ups or subscriptions. However, for trust-driven actions, lean into cool tones."

### The Strategic Use of Orange

Orange should not be the *trust* color. It should be the *action* color.

In color psychology, orange communicates:
- Speed (warmth of red, approachability of yellow)
- Friendliness (non-threatening energy)
- Urgency without fear (unlike red)
- Optimism and forward movement

This makes orange a **perfect CTA accent** in a trust-first product like SpeedClaim. The hierarchy becomes:

1. **Teal** → "I am a trustworthy, professional service"
2. **White space + typography** → "I am transparent and clear"
3. **Orange / warm amber** → "Now that you trust me — act"

Acko Insurance's brand makes exactly this move: teal primary, orange accent. The sequence is intentional and studied.

### Amber vs. Orange

Pure orange (#FF6600) can read as cheap or playful. A shifted amber (#F59E0B to #D97706 range) reads warmer, more premium, and less "sales-y." Amber is orange that went to graduate school.

**Recommendation:** Use warm amber (`#F59E0B`) as the single accent color for primary CTAs. Reserve it only for buttons and conversion-critical elements to maximize contrast value.

---

## 8. SpeedClaim Palette Decision

Based on the full body of research, here is the evidence-based palette:

### Primary Palette

| Role | Name | Hex | Psychological Intent |
|---|---|---|---|
| **Brand primary** | SpeedClaim Teal | `#0F6E8C` | Trust + innovation + calm authority |
| **Teal dark** (hover states) | Deep Teal | `#0A5470` | Depth on interaction |
| **Teal light** (backgrounds) | Teal Mist | `#F0F9FC` | Openness without sterility |
| **Teal surface** (cards) | Teal Surface | `#E2F4F9` | Subtle brand without saturation |

### Accent Palette

| Role | Name | Hex | Psychological Intent |
|---|---|---|---|
| **CTA primary** | Amber | `#F59E0B` | Action, speed, friendliness |
| **CTA hover** | Amber Dark | `#D97706` | Depth, premium |
| **Success signal** | Emerald | `#10B981` | Approval, payment received, claim settled |
| **Alert/Warning** | Coral | `#EF4444` | Rejection, attention — used sparingly |

### Neutral Palette

| Role | Name | Hex | Psychological Intent |
|---|---|---|---|
| **Background** | Off-white | `#FAFBFC` | Warmth; pure white can feel clinical |
| **Surface** | Warm white | `#FFFFFF` | Card backgrounds |
| **Border** | Soft gray | `#E5E7EB` | Structure without hardness |
| **Text primary** | Ink | `#111827` | Readability, authority |
| **Text secondary** | Slate | `#6B7280` | Supporting content |
| **Text muted** | Mist | `#9CA3AF` | Labels, captions |

### Color Usage Ratios (the 60-30-10 Rule)

A proven brand palette discipline:
- **60%** — Neutral (whites, off-whites, light grays): breathing room
- **30%** — Primary (teal family): brand identity
- **10%** — Accent (amber): action triggers

Violating this rule is the most common cause of "AI swag" aesthetics — too much color, insufficient breathing room, everything fighting for attention.

### What to Avoid

- Dark hero backgrounds for the landing page
- Red for anything except error states (triggers anxiety in financial context)
- Purple (over-saturated in Indian fintech)
- Pure black text (`#000000`) — use `#111827` for warmth at high contrast

---

## 9. Typography Psychology

Typography is the design system's voice. It communicates personality before the words themselves are read.

### The Speed of Typographic Perception

Research from the [Sprak Design typography analysis](https://www.sprakdesign.com/typography-psychology-fonts-user-behavior/) confirms:

> "Fonts carry emotion. They have personalities. The shape of the letters, the spacing, and even the weight of the font can trigger subconscious responses."

The brain processes typographic personality in the same 90-second window as color. A clinical, narrow typeface on an insurance site communicates "we are an institution, not a partner." A humanistic, open typeface communicates "we are people who understand your situation."

### Key Statistics

- Easier-to-read fonts **increase trust by up to 40%**
- Appropriate font choices can **increase conversion rates by 35%**
- Font weight, size, and contrast on CTA buttons measurably improve click-through rates

---

## 10. Font Category Effects on User Behavior

### Serif Fonts

**Psychological signal:** Tradition, authority, heritage, formality, academic credibility  
**Examples:** Times New Roman, Georgia, Playfair Display, EB Garamond  
**Use for:** Established institutions, premium pricing, legal/policy documents  
**Risk for SpeedClaim:** Feels like a legacy insurance company, 1980s LIC brochure energy

### Sans-Serif Fonts

**Psychological signal:** Modernity, clarity, honesty, efficiency, approachability  
**Examples:** Inter, DM Sans, Plus Jakarta Sans, Nunito  
**Use for:** Digital products, SaaS, fintech, consumer apps  
**Why it works for SpeedClaim:** Aligns with "fast, digital, transparent" brand promise

### Geometric Sans-Serif (subset of sans-serif)

Built on circular/geometric forms (Futura, DM Sans, Plus Jakarta Sans). Communicates:
- Precision and intentionality
- Modern sophistication
- Clarity and simplicity

### Humanist Sans-Serif (subset of sans-serif)

Based on calligraphic traditions (Gill Sans, Myriad, Inter). Communicates:
- Warmth and approachability
- Natural readability at small sizes
- Trust without stiffness

**Inter is a humanist geometric hybrid** — this is precisely why it became the dominant UI font for fintech and SaaS. It reads cleanly at 12px (form labels) and with personality at 64px (hero headlines).

### Display Fonts

**Signal:** Bold, distinctive, visual impact, energy  
**Risk:** Illegible at small sizes, tiring over long reading sessions  
**SpeedClaim use case:** Only for very large hero numerals (stats: "2.8 days", "₹847 Cr+")

---

## 11. SpeedClaim Typography System

### The Pairing Strategy

**Heading font: Plus Jakarta Sans**  
- Geometric sans-serif with warm, slightly quirky curves
- Originated from Jakarta City's civic design program — has a built-in approachability
- Slightly taller x-height than Inter — more expressive at display sizes
- Distinguishes hero text from body text without needing a serif
- Pairs naturally with Inter as discovered through [font pairing research](https://maxibestof.one/typefaces/plus-jakarta-sans/pairing/inter)

**Body font: Inter**  
- The most-installed sans-serif on the web (variable axis, true italic)
- Designed for screen legibility — tall x-height, open counters, spacing tested at all sizes
- Reads at 12px (form labels) with the same clarity as 18px (body copy)
- Familiar to every urban Indian professional who uses any modern app

**Why this pairing specifically:**  
Plus Jakarta Sans brings warmth and personality to headlines while Inter keeps body text crisp and efficient. The contrast between their personalities creates typographic hierarchy without requiring different colors or weights. The reader's eye naturally reads Jakarta as "the brand talking to me" and Inter as "the content I'm evaluating."

### Type Scale (Tailwind CSS compatible)

| Level | Size | Weight | Line Height | Use |
|---|---|---|---|---|
| **Display** | 56–72px (`text-6xl`+) | 800 (ExtraBold) | 1.1 | Hero headline only |
| **H1** | 40–48px (`text-4xl/5xl`) | 700 (Bold) | 1.2 | Section headlines |
| **H2** | 28–32px (`text-2xl/3xl`) | 600 (SemiBold) | 1.3 | Feature titles |
| **H3** | 20–24px (`text-xl/2xl`) | 600 (SemiBold) | 1.4 | Card titles |
| **Body Large** | 18px (`text-lg`) | 400 (Regular) | 1.7 | Hero subheadline |
| **Body** | 16px (`text-base`) | 400 (Regular) | 1.6 | Default text |
| **Body Small** | 14px (`text-sm`) | 400 (Regular) | 1.5 | Secondary text |
| **Caption** | 12px (`text-xs`) | 500 (Medium) | 1.4 | Labels, tags |

### Letter Spacing

- Display/H1: `-0.02em` (slightly condensed — reads as confident and modern)
- H2/H3: `-0.01em`
- Body: `0em` (default — do not alter letter spacing for body text)
- Caps labels: `0.08em` (increased tracking for all-caps navigation/labels)

### Typography Rules

1. **Never use more than 2 font families** — Plus Jakarta Sans for headings, Inter for everything else
2. **Never exceed 65–75 characters per line** — optimal reading rhythm; beyond this, comprehension drops
3. **Minimum body text size is 16px** — anything smaller fails WCAG AA on mobile
4. **Weight contrast creates hierarchy** — bold headline + regular body is more readable than all-regular with size differences alone
5. **Color contrast on text: minimum 4.5:1** — dark ink on light backgrounds only; avoid gray-on-gray

---

## 12. Visual Hierarchy and Eye Movement

### How Users Actually Read Web Pages

Eye tracking research from the Nielsen Norman Group reveals two dominant scanning patterns, each suited to different layouts:

### The F-Pattern

**When it occurs:** Text-heavy pages — articles, product listings, search results, data tables  
**How it works:** Users read the first line fully (top bar of F), scan part of the second line (middle bar of F), then scan the left edge vertically  
**Implication for SpeedClaim:** The F-pattern is the enemy for the landing page. If users start F-scanning, it means the design failed to lead them — they've defaulted to information extraction mode instead of emotional engagement mode.

### The Z-Pattern

**When it occurs:** Simple layouts with clear visual anchors — landing pages, advertisements, onboarding screens  
**How it works:** Eye moves left-to-right across the top → diagonals down to bottom-left → left-to-right across the bottom  
**Implication for SpeedClaim:** This is the target pattern. A Z-pattern layout means:
- **Top-left:** Logo/brand mark (establishes trust)
- **Top-right:** Primary navigation + CTA (action when ready)
- **Middle diagonal:** Hero content (emotional hook)
- **Bottom-left:** Social proof (validation)
- **Bottom-right:** Secondary CTA (conversion)

According to [LandingPageFlow's Z-pattern analysis](https://www.landingpageflow.com/post/z-pattern-vs-f-pattern), the Z-pattern is specifically designed for "simple layouts designed to direct users' attention to essential elements, like calls to action."

### The Gaze-Cue Effect

Human faces in imagery are powerful hierarchy redirectors. When a person in an image looks toward a specific element (a button, a headline, a stat), users' eyes follow the gaze direction. This is involuntary — the brain is hardwired to track where other humans are looking.

**SpeedClaim implication:** Any photography in the hero section should have subjects looking toward the right (toward the CTA button) or making direct eye contact with the camera. Never looking away — that directs attention off-screen.

### The 5-Second Rule (revisited from UX research)

Users form a lasting impression in 5 seconds. The Z-pattern hierarchy must land the most critical message within that window. For SpeedClaim:

- **Second 1:** Logo + page title = "I'm in the right place"
- **Second 2–3:** Hero headline = "This is what I get"
- **Second 4:** Hero subheadline = "This is how it works"
- **Second 5:** CTA button = "Here's how to start"

Everything below the fold exists to validate what the above-the-fold established. Users who get to the testimonials section are already interested; those sections convert them from interested to convinced.

---

## 13. Whitespace as a Design Tool

### What Whitespace Communicates

Whitespace is not empty space. It is an active design element that communicates brand values:

- **High whitespace:** Premium, confident, uncluttered — "we don't need to shout"
- **Low whitespace:** Cheap, anxious, desperate — "we need to fill every pixel"

Luxury brands (Apple, Aesop, Stripe) use aggressive whitespace to signal that they are confident enough not to fill every pixel. Their products "breathe." Users unconsciously associate breathing room with calm and trustworthiness.

An insurance company should breathe. Fear-based products (and insurance is fear-adjacent) must work extra hard to not feel claustrophobic.

### Section Spacing Recommendations

| Context | Spacing |
|---|---|
| Between major sections | `py-20` to `py-24` (80–96px) |
| Inside cards | `p-6` to `p-8` (24–32px) |
| Between headline and body | `mt-4` to `mt-6` (16–24px) |
| Between stat blocks | `gap-8` to `gap-12` (32–48px) |
| Mobile sections | Reduce by ~25% (`py-14`) |

### The Mobile Density Trap

The current landing page compresses well on mobile, but doesn't actively optimize for it. Mobile users scan even faster than desktop users (thumb-scroll momentum is real). Mobile section spacing should remain generous — the temptation to squeeze content to "fit" more above the fold backfires by creating the low-whitespace signals described above.

**Rule:** Never reduce a section's vertical padding below 48px (`py-12`) on mobile for a landing page. If content doesn't fit, remove content — don't compress.

---

## 14. Shape Psychology — Border Radius

### Neuroscience of Corners

Research documented by [Digital Kulture](https://www.webbb.ai/blog/why-rounded-corners-dominate-ui-trends) and [Muzli Design](https://medium.muz.li/beyond-rounded-corners-strategic-use-of-border-radius-in-modern-web-interfaces-cc7ac6470498):

> "Sharp corners trigger subtle stress responses in the brain. Research shows sharp corners activate the amygdala — the brain's fear-processing region — while rounded objects are literally perceived as safer and more approachable."

This is not metaphorical. The brain's threat-detection system responds to pointed shapes as potential physical hazards. Rounded shapes are cognitively coded as safe.

For a product fighting the "insurance = scary + adversarial" mental model, **every rounded corner is a subconscious de-escalation signal.**

### Border Radius Scale by Brand Personality

| Radius Range | Personality | Best For |
|---|---|---|
| `2–4px` | Precise, trustworthy, institutional | B2B platforms, banking |
| `8–12px` | Professional yet approachable | **Consumer fintech ← SpeedClaim** |
| `16–24px` | Friendly, modern, clean | Consumer apps, health tech |
| `50%+` (pill) | Playful, casual, expressive | Social, gaming, entertainment |

**SpeedClaim recommendation:**
- Cards and containers: `rounded-xl` (12px) — approachable but not casual
- Buttons: `rounded-full` (pill shape) — friendly, modern, inviting action
- Input fields: `rounded-lg` (8px) — professional but not stiff
- Image containers: `rounded-2xl` (16px) — warm frame for testimonial photos
- Modals/overlays: `rounded-2xl` (16px) — friendly interruption

### Avoiding the Inconsistency Problem

The current landing page mixes several border radius sizes inconsistently. Cards, buttons, and testimonials all have different radii — the visual message is incoherent. Consistency in border radius is as important as consistency in color. Pick a system and enforce it everywhere.

---

## 15. Animation and Micro-interaction Psychology

### Why Motion Matters

Motion in UI serves three psychological functions:

1. **Attention direction:** Movement captures peripheral vision before conscious thought
2. **State feedback:** Animation tells users that the interface received their input
3. **Personality expression:** The *character* of motion (bouncy vs. graceful vs. snappy) communicates brand personality

### The Conversion Evidence

From [Scroll Animation Best Practices](https://motionlabis.com/web-animation-design/scroll-animation-best-practices/):

- A/B testing micro-interactions can yield **20–25% conversion improvement**
- One retailer: adding a bounce animation on the "Add to Cart" button → **23% increase in purchases**
- Over-animation (too many moving elements): **bounce rate increased 40%**, contact form conversion **dropped 50%**

The conclusion is unambiguous: **specific, purposeful micro-interactions significantly boost conversion. Generic, everywhere-applied animation destroys it.** Animation should make users feel competent and informed, not distracted or dazzled.

### The Over-Animation Trap (Why the AI Swag Critique Is Valid)

Generically-generated landing pages suffer from a pattern of "animation for animation's sake":
- Everything fades in on scroll (even elements that users would naturally see first anyway)
- Hero sections have parallax effects that serve no informational purpose
- Hover states on every element (card lifts, glows, shadows) compete for attention

The user's cognitive load increases with each animation they must process. When every element moves, no single element communicates priority. The result is visually busy = mentally tiring = bounce.

**Rule:** Every animation must have a *reason* beyond aesthetics. Acceptable reasons: indicating state change, directing attention to a CTA, confirming a user action, rewarding engagement (scroll reveals).

### Easing Functions — The Personality of Motion

The easing curve is the single most expressive animation parameter. It communicates the brand's physical metaphor:

| Easing | Metaphor | Personality | Use For |
|---|---|---|---|
| `ease-out` | Ball rolling to a stop | Confident, natural, settled | Elements entering the viewport |
| `ease-in` | Ball starting to roll | Hesitant, building | Elements leaving viewport |
| `ease-in-out` | Breath | Balanced, flowing | Parallax, tooltip transitions |
| `linear` | Machine | Technical, precise | Progress bars, counting animations |
| Custom spring | Physical object | Playful, alive | Badge animations, success states |

SpeedClaim is not a playful product, so spring/bounce easing should be reserved exclusively for success/celebration moments (claim approved, payment received). All routine UI motion should use `ease-out` — confident, decisive, landing naturally.

### Duration — The Tempo of Trust

From MDN Web Docs animation timing research:

- **< 150ms:** Imperceptible — no psychological value; appears to be an instant state change
- **150–300ms:** Functional micro-interaction range — hover states, button feedback, tooltip appearance
- **300–500ms:** Navigation transitions, panel slides — user perceives intentional motion
- **500–700ms:** Scroll reveals, entrance animations — long enough to add beauty, short enough not to feel slow
- **> 800ms:** Begins to feel sluggish; users tap/click again thinking it didn't register

**SpeedClaim animation tempo guide:**
- Hover state color transitions: `150ms ease-out`
- Button press feedback: `100ms ease-out`
- Card/section entrance on scroll: `500ms ease-out` with `translateY(20px)` → `translateY(0)`
- Counter/stat counting animation: `1500ms linear` (this is the exception — slow counting builds dramatic effect)
- Toast/notification: `300ms ease-out` in, `200ms ease-in` out
- Page route transition: `250ms ease-in-out`

---

## 16. SpeedClaim Animation Spec

### Entrance Animation (Scroll Reveal)

Every section entering the viewport should use:
```css
/* Initial state */
opacity: 0;
transform: translateY(24px);

/* Final state — triggered when element enters viewport */
opacity: 1;
transform: translateY(0);
transition: opacity 500ms ease-out, transform 500ms ease-out;
```

**Stagger delay for grids:** When 3 cards appear simultaneously, stagger each by 80ms:
- Card 1: `delay: 0ms`
- Card 2: `delay: 80ms`
- Card 3: `delay: 160ms`

Staggering creates a "wave" effect that reads as natural rather than mechanical.

### Stat Counter Animation

The stats bar (`4.8L+ Policies`, `₹847 Cr+ Claims`, `2.8 days`, `98.4%`) should count up when visible:
- Start: `0`
- End: actual value
- Duration: `1800ms linear`
- Trigger: IntersectionObserver at 80% visibility threshold

This is the highest-return animation on the page. Watching a number count up creates the same psychological effect as a slot machine — the user waits to see where it lands. When it lands on a large, impressive number, the brain registers that number as *earned*, not just presented.

### Hover State Micro-interactions

**Buttons:**
```
background: transition 150ms ease-out
transform: scale(1.02) on hover — subtle lift
box-shadow: deeper on hover
```

**Cards:**
```
box-shadow: 0 1px 3px → 0 8px 24px on hover
transform: translateY(-2px) — 2px lift only
transition: 200ms ease-out
```

**Navigation links:**
```
color: teal 500 → teal 700 on hover
border-bottom: 2px solid teal, scale in from left
transition: 200ms ease-out
```

### Accessibility — `prefers-reduced-motion`

Every animation must be wrapped:
```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

In Angular, all IntersectionObserver-driven animations should check `window.matchMedia('(prefers-reduced-motion: reduce)')` before adding motion classes.

### Performance Rule

Only animate `transform` and `opacity`. Period.  
Animating `width`, `height`, `margin`, `padding`, `border-width`, or `left/top` causes layout thrashing and kills performance on mid-range devices (the majority of Indian users are on budget Android phones).

---

## 17. Photography and Human Faces

### The Neuroscience of Faces

Research from [Princeton University](https://omgimg.co/blog/human-faces-in-website-images/) via the VWO conversion study:

> "People form trustworthiness judgments from human faces in under 100 milliseconds."

That is twice as fast as the color perception response. A human face on the landing page is processed as a trust signal before the user consciously registers the page's content.

### Conversion Impact

From [VWO's landmark A/B test](https://vwo.com/blog/human-landing-page-increase-conversion-rate/):

- **Before:** Abstract paintings on artist profile pages → **8.8% conversion**
- **After:** Artist photographs → **17.2% conversion**
- **Delta:** +95% increase — the highest documented single-element conversion lift from imagery alone

The mechanism: when users see a face, they trigger their social cognition systems. They ask (subconsciously): "Is this person like me? Do they seem trustworthy? Are they happy?" If yes, the brand inherits that trustworthiness.

### Authenticity Over Polish

From the [Fstoppers authenticity research](https://fstoppers.com/originals/authenticity-trend-best-thing-happen-photography-decade-902022):

> "User-generated content, behind-the-scenes photography, and unpolished 'real moment' imagery often outperform glossy commercial photography because they signal authenticity and build trust."

Stock photos of perfect, diverse, smiling people in insurance contexts are immediately recognized as stock photos. The brain registers them as "this is not a real customer" and discounts the social proof signal entirely.

For SpeedClaim's current phase (no actual brand photography budget), the testimonials section must compensate with **highly specific, textured copy** and **avatar initials with real-feeling names and roles** — exactly what the current testimonials do (Priya Mehta, Karthik Raghunathan, Sunitha Rao, Arjun Patel). The specificity makes them feel earned even without photos.

When photography is introduced: use candid, natural-light photography of real-seeming situations — someone on their phone, a family moment, a hospital lobby moment — not polished portraits.

### Gaze Direction in Imagery

When human subjects are placed in a design:
- **Gaze toward CTA:** Users follow the gaze — CTA gets more attention
- **Gaze toward camera:** Direct eye contact — builds strongest personal connection
- **Gaze away:** Directs user attention off-screen — avoid

---

## 18. Indian Market Context

### The Device and Network Reality

82% of Indian internet users access web on mobile. Of those, a significant portion use mid-range Android devices (6–8GB RAM, older GPU). The design language must account for:

- **Heavy background gradients:** Cause GPU strain on mid-range devices; prefer flat colors with strategic shadows
- **Custom font loading:** Both Plus Jakarta Sans and Inter are on Google Fonts with `font-display: swap` support — load time is acceptable
- **Animation complexity:** IntersectionObserver-based reveals perform well on all devices; CSS keyframe complexity is the variable to monitor

### The Language of Trust for Indian Buyers

From [Progress's fintech color psychology overview](https://www.progress.com/blogs/how-choose-right-colors-fintech):

> "82% of digital users are more likely to choose apps whose names and designs they can 'understand in one glance.'"

Indian consumers — particularly outside the top 8 metros — make brand trust decisions faster than Western audiences, have higher skepticism toward digital financial products (given fraud exposure), and respond more strongly to:

1. **Regulatory signals:** IRDAI registration number prominently displayed
2. **Institutional credibility markers:** Bank partnerships, network hospital counts
3. **Familiarity signals:** Recognizable visual language (blue-adjacent = safe)
4. **Proof of scale:** Claims numbers, customer counts

The design language must **lead with proof**, not with personality. Western fintech can afford to lead with brand; Indian fintech must earn the right to personality through displayed trustworthiness first.

### The Hindi-English Bilingual Reading Pattern

Even for users who predominantly read English, brand slogans and headlines often mentally translate as they read. Short, punchy English headlines that work phonetically in Hindi/regional languages create stronger memory traces.

"Claims at the speed of life" mentally translates well.  
"Empowering your insurance journey" does not — too abstract, no phonetic hook.

**Copy principle for SpeedClaim:** Use Anglo-Indian plain language. Avoid corporate English ("comprehensive coverage solutions"). Aim for conversational directness: "File a claim from your phone. Get money in 2 days."

---

## 19. The SpeedClaim Design Language Spec

This section is the actionable output of everything above. These are not preferences — they are evidence-based specifications.

### The 5 Design Principles

**1. Trust before transaction**  
Every visual decision — color choice, spacing amount, border radius, animation character — must first ask: does this make the user feel safe? Trust is never assumed; it must be earned incrementally through each section of the page.

**2. Human before product**  
Show outcomes in human terms (₹3.2L settled, 2 days, father's surgery covered) before showing features. Real specificity beats polished claims. A specific number earns trust; a superlative ("fastest claims") triggers skepticism.

**3. Breathe**  
Whitespace is the signal of confidence. A brand that has nothing to hide doesn't fill every pixel. Section padding minimum 80px. Card padding minimum 24px. Text maximum 65 characters per line.

**4. Motion serves attention, not ego**  
Every animation on the page must direct attention toward something important (a claim stat, a CTA, a testimonial outcome) or confirm user input. If removing the animation wouldn't make the page less informative, remove it.

**5. Consistent, not complex**  
One teal. One amber. Two fonts. One border-radius system. The discipline of restraint is the actual design skill. Inconsistency is the most visible sign of "AI swag" — everything generated individually, nothing unified into a coherent system.

---

### Token System

#### Colors
```css
--sc-teal-50: #F0F9FC;
--sc-teal-100: #CBECF4;
--sc-teal-500: #0F6E8C;   /* Primary brand */
--sc-teal-700: #0A5470;   /* Hover/active */
--sc-amber-400: #FBBF24;
--sc-amber-500: #F59E0B;  /* Primary CTA */
--sc-amber-600: #D97706;  /* CTA hover */
--sc-emerald-500: #10B981; /* Success */
--sc-gray-50: #FAFBFC;    /* Page background */
--sc-gray-900: #111827;   /* Text primary */
--sc-gray-500: #6B7280;   /* Text secondary */
```

#### Typography
```css
--sc-font-display: 'Plus Jakarta Sans', sans-serif;  /* Headings */
--sc-font-body: 'Inter', sans-serif;                  /* Body text */
```

#### Border Radius
```css
--sc-radius-sm: 8px;    /* Inputs */
--sc-radius-md: 12px;   /* Cards */
--sc-radius-lg: 16px;   /* Image frames, modals */
--sc-radius-full: 9999px; /* Pill buttons */
```

#### Shadows
```css
--sc-shadow-sm: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);
--sc-shadow-md: 0 4px 16px rgba(0,0,0,0.08), 0 2px 4px rgba(0,0,0,0.04);
--sc-shadow-lg: 0 16px 40px rgba(0,0,0,0.10), 0 4px 8px rgba(0,0,0,0.04);
```

Note: Shadow uses `rgba` not black, and the spread is intentionally limited. Heavy shadows feel cheap. A barely perceptible shadow with sharp focus is the premium signal.

#### Spacing Scale
```
Section padding: 80px–96px (py-20 to py-24)
Card padding: 24px–32px (p-6 to p-8)
Grid gap: 24px–32px (gap-6 to gap-8)
Mobile section: 56px–64px (py-14 to py-16)
```

#### Animation Tokens
```css
--sc-duration-instant: 100ms;
--sc-duration-fast: 200ms;
--sc-duration-normal: 350ms;
--sc-duration-slow: 500ms;
--sc-duration-counter: 1800ms;
--sc-easing-out: cubic-bezier(0, 0, 0.2, 1);   /* Material ease-out */
--sc-easing-inout: cubic-bezier(0.4, 0, 0.2, 1); /* Material ease-in-out */
```

---

### Section-by-Section Design Spec

#### Navbar
- Background: `white` with `border-b border-gray-100`
- Logo: SpeedClaim teal wordmark (Plus Jakarta Sans, 700)
- Links: Inter 14px, gray-600, hover teal-500 with bottom border transition
- CTA: Amber pill button (`bg-amber-500 text-white rounded-full px-5 py-2`)
- Sticky: `sticky top-0 z-50 backdrop-blur-sm bg-white/90`

#### Hero
- Background: `bg-sc-teal-50` (the teal mist) with a very subtle teal-to-white gradient
- **Not dark.** Light background, dark text. Trust signal.
- Headline (Plus Jakarta Sans, 700, 56px, `#111827`): outcome-first
- Subheadline (Inter, 400, 18px, gray-500): mechanism
- CTA: Amber pill button, large (`px-8 py-4 text-lg`)
- Secondary CTA: Ghost button with teal border
- Trust strip below CTA: IRDAI registration + "4.8L+ customers" + SSL badge
- Right side: The floating claim approval card (keep — it's the right idea; redesign with white card, proper shadow, teal accents)

#### Stats Bar
- Background: `bg-white` with `border-y border-gray-100`
- 4 stats, `text-4xl font-bold text-teal-600` for numbers
- Counter animation on viewport entry
- Gray-500 for labels

#### How It Works
- Background: `bg-white`
- 3 steps in a horizontal layout (vertical on mobile)
- Step numbers: teal circle, white number (Plus Jakarta Sans)
- Connecting line between circles: teal-200 dashed
- Step entrance: staggered scroll reveal (0ms, 150ms, 300ms)

#### Coverage Types
- Background: `bg-gray-50`
- 3 cards in a row (`grid-cols-1 sm:grid-cols-3`)
- Card: white, `rounded-xl`, `shadow-sm`, hover `shadow-md` + `translateY(-2px)`
- Icon: colored circle background (emerald/blue/amber per category)
- Features list: checkmarks in teal

#### Testimonials
- Background: `bg-white`
- 2×2 grid (2 columns on desktop, 1 on mobile)
- Card: `rounded-2xl bg-gray-50`
- Quote first, name/role below
- Avatar: Initial circle with amber background (matches CTA — subtle brand callback)
- Star rating: amber stars (visual coherence with CTA amber)
- Tag: Small pill badge, teal-50 background, teal-700 text

#### CTA Section
- Background: `bg-teal-500` (the only use of solid teal as a background)
- White headline + subheadline
- Amber CTA button (high contrast against teal)
- This inversion (light → dark for final CTA) creates urgency and signals "this is the decision point"

#### Footer
- Background: `bg-gray-900` (dark, professional close)
- 4-column layout: brand + links × 3 categories
- Subtle legal/IRDAI text in gray-500

---

## 20. Vibe Statement — The North Star

A vibe statement is a single sentence that the design team (or AI, or developer) can test every decision against: "Does this choice serve the vibe, or does it contradict it?"

> **SpeedClaim's vibe: "The calm confidence of a doctor who has seen this before and knows exactly what to do — and charges a fair price."**

Unpacked:
- **Calm:** Never anxious, never loud, never salesy. Breathing room. Trust before transaction.
- **Confident:** Does not need to oversell. The numbers speak. The specificity speaks. The design doesn't shout.
- **Doctor-who-has-seen-this-before:** Competence signal. You are not the first person to file a claim after a monsoon accident. SpeedClaim has done this 4.8 lakh times. The interface should feel practiced, not experimental.
- **Knows exactly what to do:** Clean process, clear next steps, no dead ends. Clarity as empathy.
- **Charges a fair price:** Transparent. No fine-print fear. Premium but not predatory.

Every design decision — from the border radius (rounded = safe, not intimidating) to the hero background (light = nothing to hide) to the font (humanist = warm) to the animation character (ease-out = settled, deliberate) — flows from this single sentence.

When in doubt, ask: "Is this calm? Is this confident? Does this feel competent and human?" If yes, ship it. If no, change it.

---

## 21. Connecting to the UX Research Document

This document is the *how* to the UX research's *what*.

`LANDING_UX_RESEARCH.md` established:
- **The psychological gap:** Users buy insurance to manage fear; the design must address the fear, not paper over it
- **The copy direction:** Outcome-first, specific, human (STEPPS framework)
- **The social proof hierarchy:** Specific testimonials with friction beats polished ones
- **The brag mechanism:** Claim settlement celebration screen with industry speed comparison
- **The recommended headline:** "When the worst happens, you get money in your account — not excuses."

This document establishes:
- **The color that earns trust while standing out:** Deep teal (`#0F6E8C`) on light backgrounds, amber CTAs
- **The typography that feels human:** Plus Jakarta Sans for personality, Inter for clarity
- **The motion that rewards without distracting:** Scroll reveals + stat counters, nothing else animated for animation's sake
- **The spatial discipline that signals confidence:** 80px section padding, card breathing room, the 60-30-10 color ratio
- **The physical metaphors:** Rounded corners = safe; light hero = nothing to hide; ease-out easing = settled

Together, these two documents define a **landing page that earns the right to convert users** — not through persuasion tricks, but through design choices that respect the user's emotional state and earn their trust before asking for anything.

The redesign that follows these briefs should make a user in Mumbai think: *"This doesn't feel like an insurance company. It feels like someone built this because they were also frustrated with insurance companies."* That feeling is the brag trigger. That is the referral.

---

*Research compiled June 2026 for SpeedClaim landing page redesign.*  
*Sources: [Bethany Works Financial Color Psychology](https://bethanyworks.com/color-psychology-financial-services-brands/) · [BFA Global Fintech Branding](https://bfaglobal.com/catalyst-fund/insights/getting-your-fintech-brand-right-stand-out-with-color/) · [Awesome Sauce Teal Branding](https://www.awesomesauce.in/insights/transformative-teal-in-branding) · [VWO Faces Conversion Study](https://vwo.com/blog/human-landing-page-increase-conversion-rate/) · [LandingPageFlow Z-Pattern](https://www.landingpageflow.com/post/z-pattern-vs-f-pattern) · [Digital Kulture Border Radius](https://www.webbb.ai/blog/why-rounded-corners-dominate-ui-trends) · [Muzli Border Radius](https://medium.muz.li/beyond-rounded-corners-strategic-use-of-border-radius-in-modern-web-interfaces-cc7ac6470498) · [Scroll Animation Best Practices](https://motionlabis.com/web-animation-design/scroll-animation-best-practices/) · [Kuva Media Color Psychology](https://kuvamedia.com/blog/the-real-color-psychology-for-branding-why-blue-means-trust-is-only-half-the-story) · [Sprak Design Typography](https://www.sprakdesign.com/typography-psychology-fonts-user-behavior/) · [Progress Fintech Colors](https://www.progress.com/blogs/how-choose-right-colors-fintech) · [Google Fonts Plus Jakarta Sans Pairing](https://maxibestof.one/typefaces/plus-jakarta-sans/pairing/inter)*
