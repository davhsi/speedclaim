import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from '../profile/services/profile.service';
import { FamilyMemberDto, AddFamilyMemberRequest } from '../../../core/models/api.models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-family',
  standalone: true,
  imports: [ReactiveFormsModule, ConfirmDialogComponent, DateFormatPipe],
  templateUrl: './family.html',
})
export class FamilyComponent implements OnInit {
  private fb = inject(FormBuilder);
  private profileService = inject(ProfileService);
  private toast = inject(ToastService);

  members = signal<FamilyMemberDto[]>([]);
  showForm = signal(false);
  deleteTarget = signal<string | null>(null);

  memberForm = this.fb.group({
    name: ['', Validators.required],
    dateOfBirth: ['', Validators.required],
    relationship: ['Spouse', Validators.required],
    gender: ['Male', Validators.required],
    salutationTitle: ['Mr'],
  });

  ngOnInit(): void {
    this.profileService.getFamilyMembers().subscribe(m => this.members.set(m));
  }

  addMember(): void {
    this.profileService.addFamilyMember(this.memberForm.getRawValue() as AddFamilyMemberRequest).subscribe({
      next: member => {
        this.members.update(m => [...m, member]);
        this.toast.success('Family member added');
        this.showForm.set(false);
        this.memberForm.reset({ relationship: 'Spouse', gender: 'Male', salutationTitle: 'Mr' });
      },
      error: () => this.toast.error('Failed to add member'),
    });
  }

  confirmDelete(): void {
    const id = this.deleteTarget();
    if (!id) return;
    this.profileService.deleteFamilyMember(id).subscribe({
      next: () => {
        this.members.update(m => m.filter(x => x.id !== id));
        this.toast.success('Member removed');
      },
      error: () => this.toast.error('Delete failed'),
    });
    this.deleteTarget.set(null);
  }

  badgeColor(rel: string): { bg: string; fg: string; border: string } {
    switch (rel) {
      case 'Spouse': return { bg: 'var(--color-primary-light)', fg: 'var(--color-primary)', border: 'var(--color-primary-muted)' };
      case 'Son': case 'Daughter': return { bg: 'var(--color-info-bg)', fg: 'var(--color-info)', border: 'var(--color-info-border)' };
      case 'Father': case 'Mother': return { bg: 'var(--color-warning-bg)', fg: 'var(--color-warning)', border: 'var(--color-warning-border)' };
      default: return { bg: 'var(--color-surface-alt)', fg: 'var(--color-muted)', border: 'var(--color-line)' };
    }
  }
}
