import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from '../profile/services/profile.service';
import { FamilyMemberDto, AddFamilyMemberRequest, UpdateFamilyMemberRequest } from '../../../core/models/api.models';
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
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);
  private readonly toast = inject(ToastService);

  members = signal<FamilyMemberDto[]>([]);
  showForm = signal(false);
  deleteTarget = signal<string | null>(null);
  editTarget = signal<FamilyMemberDto | null>(null);
  addingMember = signal(false);
  savingEdit = signal(false);
  deleting = signal(false);

  memberForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    dateOfBirth: ['', Validators.required],
    relationship: ['Spouse', Validators.required],
    gender: ['Male', Validators.required],
    salutation: ['Mr'],
  });

  editForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    dateOfBirth: ['', Validators.required],
    relationship: ['Spouse', Validators.required],
    gender: ['Male', Validators.required],
    salutation: ['Mr'],
  });

  ngOnInit(): void {
    this.profileService.getFamilyMembers().subscribe(m => this.members.set(m));
  }

  toggleAddForm(): void {
    if (this.addingMember()) return;
    this.showForm.update(v => !v);
  }

  addMember(): void {
    if (this.addingMember() || this.memberForm.invalid) return;
    const v = this.memberForm.getRawValue();
    this.addingMember.set(true);
    this.profileService.addFamilyMember({ ...v, isDependent: true } as AddFamilyMemberRequest).subscribe({
      next: member => {
        this.addingMember.set(false);
        this.members.update(m => [...m, member]);
        this.toast.success('Family member added');
        this.showForm.set(false);
        this.memberForm.reset({ relationship: 'Spouse', gender: 'Male', salutation: 'Mr' });
      },
      error: () => {
        this.addingMember.set(false);
        this.toast.error('Failed to add member');
      },
    });
  }

  startEdit(m: FamilyMemberDto): void {
    this.editTarget.set(m);
    this.editForm.patchValue({
      firstName: m.firstName, lastName: m.lastName,
      dateOfBirth: m.dateOfBirth, relationship: m.relationship,
      gender: m.gender, salutation: m.salutation,
    });
  }

  cancelEdit(): void {
    if (this.savingEdit()) return;
    this.editTarget.set(null);
  }

  saveEdit(): void {
    const target = this.editTarget();
    if (!target || this.editForm.invalid || this.savingEdit()) return;
    const v = this.editForm.getRawValue();
    this.savingEdit.set(true);
    this.profileService.updateFamilyMember(target.id, { ...v, isDependent: true } as UpdateFamilyMemberRequest).subscribe({
      next: () => {
        this.savingEdit.set(false);
        this.members.update(list => list.map(m => m.id === target.id
          ? { ...m, ...v, fullName: `${v.firstName} ${v.lastName}` } as typeof m
          : m,
        ));
        this.toast.success('Member updated');
        this.editTarget.set(null);
      },
      error: () => {
        this.savingEdit.set(false);
        this.toast.error('Update failed');
      },
    });
  }

  confirmDelete(): void {
    const id = this.deleteTarget();
    if (!id || this.deleting()) return;
    this.deleting.set(true);
    this.profileService.deleteFamilyMember(id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.members.update(m => m.filter(x => x.id !== id));
        this.toast.success('Member removed');
        this.deleteTarget.set(null);
      },
      error: () => {
        this.deleting.set(false);
        this.toast.error('Delete failed');
        this.deleteTarget.set(null);
      },
    });
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
