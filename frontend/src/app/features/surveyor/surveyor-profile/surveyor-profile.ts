import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { SurveyorProfileDto, SurveyorService } from '../services/surveyor.service';
import { ClaimDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-surveyor-profile',
  standalone: true,
  templateUrl: './surveyor-profile.html',
})
export class SurveyorProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private surveyorService = inject(SurveyorService);

  claims = signal<ClaimDto[]>([]);
  profile = signal<SurveyorProfileDto | null>(null);

  fullName = computed(() => {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : 'Surveyor';
  });

  initials = computed(() => {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  });

  email = computed(() => this.profile()?.email ?? this.authService.currentUser()?.email ?? '');
  phone = computed(() => this.profile()?.phone ?? this.authService.currentUser()?.phone ?? '');
  licenseNo = computed(() => this.profile()?.licenseNumber ?? 'Not assigned');
  specialization = computed(() => this.profile()?.specialization ?? 'Assigned Region');
  isActive = computed(() => this.profile()?.isActive ?? true);

  totalClaims = computed(() => this.claims().length);
  submittedCount = computed(() =>
    this.claims().filter(c =>
      c.status === 'Settled' || c.status === 'Approved' || c.status === 'PayoutProcessed'
    ).length
  );
  overdueCount = computed(() => {
    const now = new Date();
    return this.claims().filter(c => {
      if (c.status === 'Settled' || c.status === 'Approved' || c.status === 'PayoutProcessed') return false;
      const intimated = new Date(c.intimationDate);
      return Math.floor((now.getTime() - intimated.getTime()) / 86400000) > 7;
    }).length;
  });

  ngOnInit(): void {
    this.surveyorService.getProfile().subscribe({
      next: profile => this.profile.set(profile),
      error: () => {},
    });
    this.surveyorService.getAssignedClaims().subscribe({
      next: claims => this.claims.set(claims),
      error: () => {},
    });
  }

  signOut(): void {
    this.authService.logout();
  }
}
