import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SafeHtmlPipe } from '../../shared/pipes/safe-html.pipe';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, SafeHtmlPipe],
  templateUrl: './landing.html',
})
export class LandingComponent {
  readonly year = new Date().getFullYear();

  readonly stats = [
    { value: '4.8L+', label: 'Policies served' },
    { value: '₹847 Cr+', label: 'Claims settled' },
    { value: '2.8 days', label: 'Avg. settlement' },
    { value: '98.4%', label: 'Satisfaction rate' },
  ];

  readonly steps = [
    {
      number: '01',
      title: 'Create your account',
      desc: 'Register in under 2 minutes. Complete KYC digitally — no branch visits required.',
    },
    {
      number: '02',
      title: 'Choose your coverage',
      desc: 'Browse Health, Motor, and Life plans. Get a quote instantly and submit your proposal online.',
    },
    {
      number: '03',
      title: 'File & track claims',
      desc: 'Upload documents from your phone, track claim status in real time, and receive your payout directly.',
    },
  ];

  readonly coverageTypes = [
    {
      title: 'Health Insurance',
      desc: 'Individual and family floater plans with cashless hospitalisation at 10,000+ network hospitals. Covers pre and post-hospitalisation expenses.',
      features: ['Cashless hospitalisation', 'Family floater plans', 'No-claim bonus', 'Pre-existing conditions'],
      color: 'bg-[#E8F7F1] text-[#1F9D6B]',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>`,
    },
    {
      title: 'Motor Insurance',
      desc: 'Comprehensive coverage for cars and two-wheelers. Protection against accidents, theft, natural disasters, and third-party liability.',
      features: ['Accident & theft cover', 'Natural damage', 'Third-party liability', 'Quick surveyor dispatch'],
      color: 'bg-[#E8F0FE] text-[#2D7FF9]',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="1" y="3" width="15" height="13"/><polygon points="16 8 20 8 23 11 23 16 16 16 16 8"/><circle cx="5.5" cy="18.5" r="2.5"/><circle cx="18.5" cy="18.5" r="2.5"/></svg>`,
    },
    {
      title: 'Life Insurance',
      desc: 'Term life and whole life plans to protect your family\'s financial future. Transparent premiums with guaranteed payouts and maturity benefits.',
      features: ['Term & whole life', 'Maturity benefits', 'Nominee management', 'Tax benefits u/s 80C'],
      color: 'bg-[#FEF0EA] text-[#F2784B]',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>`,
    },
  ];

  readonly testimonials = [
    {
      name: 'Priya Mehta',
      role: 'Software Engineer, Infosys · Bengaluru',
      initials: 'PM',
      stars: 5,
      quote: 'My father needed emergency bypass surgery and I was terrified about the bills. SpeedClaim processed the cashless claim within 36 hours. The digital document upload from my phone was a lifesaver.',
      tag: 'Health claim · ₹3.2L settled',
    },
    {
      name: 'Karthik Raghunathan',
      role: 'Business Owner · Chennai',
      initials: 'KR',
      stars: 5,
      quote: 'Had a motor accident during the monsoon. Filed the claim right from the accident site on my phone. A surveyor was assigned within the hour. I genuinely didn\'t expect insurance to work this fast.',
      tag: 'Motor claim · 2-day settlement',
    },
    {
      name: 'Sunitha Rao',
      role: 'School Principal · Hyderabad',
      initials: 'SR',
      stars: 5,
      quote: 'Bought a family floater policy for all four of us in under 15 minutes. The premium calculator was transparent with no hidden charges, and the policy certificate arrived in my email immediately.',
      tag: 'Family floater · 4 members',
    },
    {
      name: 'Arjun Patel',
      role: 'Freelance Consultant · Mumbai',
      initials: 'AP',
      stars: 4,
      quote: 'The claims officer kept me updated at every single step — from submission to approval to payout. No more calling helplines and being kept on hold for 45 minutes. This is how insurance should work.',
      tag: 'Health claim · Fully digital',
    },
  ];
}
