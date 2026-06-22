import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { SurveyorService } from '../services/surveyor.service';
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

  fullName = computed(() => {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : 'Surveyor';
  });

  initials = computed(() => {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  });

  email = computed(() => this.authService.currentUser()?.email ?? '');
  phone = computed(() => this.authService.currentUser()?.phone ?? '');
  licenseNo = computed(() => 'IRDA/SB/2019/0000');
  zone = computed(() => 'Zone: Assigned Region');

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
    this.surveyorService.getAssignedClaims().subscribe({
      next: claims => this.claims.set(claims),
      error: () => {},
    });
  }

  signOut(): void {
    this.authService.logout();
  }
}
