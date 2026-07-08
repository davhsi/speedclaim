import { Component, AfterViewInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { SafeHtmlPipe } from '../../shared/pipes/safe-html.pipe';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, SafeHtmlPipe],
  templateUrl: './landing.html',
})
export class LandingComponent implements AfterViewInit {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly router = inject(Router);

  readonly year = new Date().getFullYear();
  quoteType = signal<'Health' | 'Motor' | 'Life'>('Health');
  readonly quoteTypes = ['Health', 'Motor', 'Life'] as const;

  startQuote(): void {
    this.router.navigate(['/auth/register'], { queryParams: { type: this.quoteType() } });
  }

  readonly stats = [
    { value: '2.8', unit: ' days', label: 'Average claim settlement', footnote: 'Industry average: 23 days' },
    { value: '97.3%', unit: '', label: 'Claims paid on first submission', footnote: 'IRDAI verified · FY 2024–25' },
    { value: '4.8L+', unit: '', label: 'Families protected', footnote: 'Across 28 states' },
    { value: '₹847 Cr+', unit: '', label: 'Total claims settled', footnote: 'Since inception' },
  ];

  readonly steps = [
    {
      title: 'Register in under 2 minutes',
      callout: 'No branch. No agent. No commission.',
      desc: 'Create your account and complete digital KYC from your phone. Your policy certificate arrives before you finish your next cup of tea.',
      illustration: `<svg width="80" height="80" viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="40" cy="40" r="40" fill="#E6F4F8"/>
        <rect x="24" y="12" width="32" height="52" rx="7" fill="white" stroke="#0F6E8C" stroke-width="2"/>
        <rect x="33" y="14.5" width="14" height="4" rx="2" fill="#B3D9E6"/>
        <circle cx="40" cy="33" r="7.5" fill="#E6F4F8" stroke="#0F6E8C" stroke-width="1.5"/>
        <circle cx="40" cy="30.5" r="3" fill="#0F6E8C" opacity="0.45"/>
        <path d="M33.5 37.5 Q40 34 46.5 37.5" stroke="#0F6E8C" stroke-width="1.5" fill="none" stroke-linecap="round" opacity="0.45"/>
        <line x1="28" y1="45" x2="52" y2="45" stroke="#B3D9E6" stroke-width="1.2" stroke-dasharray="3 2"/>
        <line x1="28" y1="50" x2="52" y2="50" stroke="#B3D9E6" stroke-width="1.2" stroke-dasharray="3 2"/>
        <circle cx="55" cy="60" r="11" fill="#1F9D6B"/>
        <path d="M50 60 L54 64 L60 56" stroke="white" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>`,
    },
    {
      title: 'Get covered the same day',
      callout: 'Instant quote. Instant policy PDF.',
      desc: 'See the exact premium upfront — no hidden charges. Pick your plan and go live. The policy hits your inbox in minutes, not days.',
      illustration: `<svg width="80" height="80" viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="40" cy="40" r="40" fill="#FEF6E6"/>
        <rect x="18" y="10" width="40" height="52" rx="5" fill="white" stroke="#D9920A" stroke-width="2"/>
        <path d="M46 10 L58 22 L46 22 Z" fill="#FEF6E6"/>
        <path d="M46 10 L58 22 L46 22" stroke="#D9920A" stroke-width="1.5" fill="none" stroke-linejoin="round"/>
        <rect x="25" y="28" width="22" height="2.5" rx="1.25" fill="#FAD88A"/>
        <rect x="25" y="34" width="16" height="2.5" rx="1.25" fill="#FAD88A"/>
        <rect x="25" y="40" width="19" height="2.5" rx="1.25" fill="#FAD88A"/>
        <circle cx="52" cy="55" r="13" fill="#D9920A"/>
        <path d="M46.5 55 L51 59.5 L57.5 51" stroke="white" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>`,
    },
    {
      title: 'File a claim from anywhere',
      callout: 'Upload. Track. Get paid.',
      desc: "When something bad happens, you shouldn't have to fight for what you're owed. Upload documents from the hospital parking lot. We'll keep you updated at every step.",
      illustration: `<svg width="80" height="80" viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="40" cy="40" r="40" fill="#E8F7F1"/>
        <rect x="24" y="8" width="32" height="52" rx="7" fill="white" stroke="#1F9D6B" stroke-width="2"/>
        <rect x="33" y="10.5" width="14" height="4" rx="2" fill="#B2E4CE"/>
        <path d="M40 38 L40 22" stroke="#1F9D6B" stroke-width="2.2" stroke-linecap="round"/>
        <path d="M33 29 L40 22 L47 29" stroke="#1F9D6B" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
        <rect x="29" y="42" width="22" height="2.5" rx="1.25" fill="#B2E4CE"/>
        <rect x="29" y="47" width="15" height="2.5" rx="1.25" fill="#B2E4CE"/>
        <path d="M24 50 L56 50 L56 60 Q56 60 56 60 L24 60 Q24 60 24 60 Z" fill="#1F9D6B"/>
        <path d="M34 55 L38.5 59 L46 51" stroke="white" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
        <circle cx="34" cy="70" r="3.5" fill="#1F9D6B"/>
        <circle cx="40" cy="70" r="3.5" fill="#1F9D6B" opacity="0.45"/>
        <circle cx="46" cy="70" r="3.5" fill="#1F9D6B" opacity="0.2"/>
      </svg>`,
    },
  ];

  readonly coverageTypes = [
    {
      title: 'Health Insurance',
      outcome: "Your mother checks in to the hospital at 2am. You don't touch your wallet at checkout. We settle directly.",
      detail: 'Family floater & individual · Cashless at 10,000+ hospitals · Pre & post-hospitalisation',
      icon: `<svg width="72" height="72" viewBox="0 0 72 72" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="72" height="72" rx="16" fill="#E8F7F1"/>
        <rect x="30" y="16" width="12" height="40" rx="5" fill="#1F9D6B"/>
        <rect x="16" y="30" width="40" height="12" rx="5" fill="#1F9D6B"/>
        <path d="M11 36 L18 36 L22 27 L27 46 L32 32 L36 41 L41 36 L61 36" stroke="#0A5469" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" fill="none" opacity="0.35"/>
      </svg>`,
    },
    {
      title: 'Motor Insurance',
      outcome: "Your car is damaged on a Tuesday. By Friday it's repaired. You didn't call a single helpline.",
      detail: 'Car & two-wheeler · Comprehensive & third-party · Surveyor within 2 hours',
      icon: `<svg width="72" height="72" viewBox="0 0 72 72" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="72" height="72" rx="16" fill="#E8F0FE"/>
        <rect x="10" y="36" width="52" height="16" rx="4" fill="#2D7FF9"/>
        <path d="M16 36 L24 22 L48 22 L56 36 Z" fill="#2D7FF9"/>
        <rect x="26" y="25" width="20" height="9" rx="2.5" fill="#E8F0FE" opacity="0.9"/>
        <circle cx="20" cy="52" r="7" fill="#1A2230"/>
        <circle cx="20" cy="52" r="4" fill="#E8F0FE"/>
        <circle cx="52" cy="52" r="7" fill="#1A2230"/>
        <circle cx="52" cy="52" r="4" fill="#E8F0FE"/>
        <line x1="4" y1="30" x2="11" y2="30" stroke="#2D7FF9" stroke-width="2.5" stroke-linecap="round" opacity="0.5"/>
        <line x1="2" y1="37" x2="8" y2="37" stroke="#2D7FF9" stroke-width="2.5" stroke-linecap="round" opacity="0.3"/>
        <line x1="3" y1="44" x2="8" y2="44" stroke="#2D7FF9" stroke-width="2" stroke-linecap="round" opacity="0.18"/>
      </svg>`,
    },
    {
      title: 'Life Insurance',
      outcome: "Whatever happens to you, your family's future is funded. No loopholes. No fine-print surprises.",
      detail: 'Term & whole life · Guaranteed payouts · Nominee protected · Tax benefits u/s 80C',
      icon: `<svg width="72" height="72" viewBox="0 0 72 72" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="72" height="72" rx="16" fill="#FEF6E6"/>
        <path d="M36 10 L60 20 L60 40 Q60 58 36 66 Q12 58 12 40 L12 20 Z" fill="#FEF0D0" stroke="#D9920A" stroke-width="2.2"/>
        <circle cx="27" cy="35" r="5" fill="#D9920A" opacity="0.75"/>
        <rect x="22" y="41" width="10" height="11" rx="5" fill="#D9920A" opacity="0.75"/>
        <circle cx="45" cy="35" r="5" fill="#D9920A" opacity="0.75"/>
        <rect x="40" y="41" width="10" height="11" rx="5" fill="#D9920A" opacity="0.75"/>
        <circle cx="36" cy="38" r="3.5" fill="#D9920A" opacity="0.5"/>
        <rect x="32.5" y="42.5" width="7" height="8" rx="3.5" fill="#D9920A" opacity="0.5"/>
      </svg>`,
    },
  ];

  readonly testimonials = [
    {
      name: 'Priya Mehta',
      context: 'Mother of 1 · Bengaluru',
      avatarUrl: '/images/testimonials/priya.png',
      stars: 5,
      quote: "Baba had emergency bypass surgery on a Saturday night. I was in the hospital parking lot, absolutely terrified about the bill. Filed the claim from my phone right there. By Tuesday morning, ₹3.2 lakhs was approved. I actually cried.",
      tag: 'Health · ₹3.2L settled in 38 hrs',
    },
    {
      name: 'Karthik R.',
      context: 'Small business owner · Chennai',
      avatarUrl: '/images/testimonials/karthik.png',
      stars: 5,
      quote: "Monsoon, heavy rain, lost control on the ECR. Car dented badly. Filed the claim from the car itself. A surveyor called me within 90 minutes. I genuinely wasn't expecting that from an insurance company.",
      tag: 'Motor · Repaired in 4 days',
    },
    {
      name: 'Sunitha Rao',
      context: 'School principal · Hyderabad',
      avatarUrl: '/images/testimonials/sunitha.png',
      stars: 4,
      quote: "Had to reupload my Aadhaar once — the first scan was blurry, entirely my fault. After that the family floater was active same day. When my son had appendicitis last year, we just showed the card. That was it.",
      tag: 'Family floater · 4 members covered',
    },
    {
      name: 'Arjun Patel',
      context: 'Freelancer · Mumbai',
      avatarUrl: '/images/testimonials/arjun.png',
      stars: 5,
      quote: "Two other companies rejected my claims — one for a 'pre-existing condition' they defined after the fact. SpeedClaim paid in full in 2.1 days. I've told every single person I know.",
      tag: 'Health · ₹1.8L settled',
    },
  ];

  readonly claimTimeline = [
    { time: 'Sat 11:42pm', event: 'Claim filed from hospital parking lot' },
    { time: 'Sun 6:18am', event: 'Claims officer reviewed documents' },
    { time: 'Mon 9:45am', event: 'Amount approved · ₹3,20,000' },
    { time: 'Tue 11:23am', event: '₹3,20,000 credited to savings account', final: true },
  ];

  readonly contactChannels = [
    {
      title: 'Customer care',
      value: '1800-123-4567',
      hint: 'Toll-free · Mon–Sat, 9:00 AM – 8:00 PM IST',
      href: 'tel:+911800123456',
      icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
        <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92Z"/>
      </svg>`,
    },
    {
      title: 'Email support',
      value: 'support@speedclaim.in',
      hint: 'We reply within 24 hours',
      href: 'mailto:support@speedclaim.in',
      icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
        <rect x="2" y="4" width="20" height="16" rx="2"/>
        <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>
      </svg>`,
    },
    {
      title: 'Registered office',
      value: 'Prestige Tech Park, 4th Floor, Bengaluru, Karnataka 560103',
      hint: 'IRDAI Registration No. 1234567890',
      href: null,
      icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
        <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/>
        <circle cx="12" cy="10" r="3"/>
      </svg>`,
    },
    {
      title: 'Grievance redressal officer',
      value: 'Ananya Krishnan',
      hint: 'grievance.officer@speedclaim.in · For complaints unresolved by regular support, per IRDAI guidelines',
      href: 'mailto:grievance.officer@speedclaim.in',
      icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z"/>
      </svg>`,
    },
  ];

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
          }
        });
      },
      { threshold: 0.12, rootMargin: '0px 0px -48px 0px' }
    );

    document.querySelectorAll('.sc-reveal').forEach(el => observer.observe(el));
  }
}
