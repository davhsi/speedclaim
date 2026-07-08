import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { UserDto, SessionDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [FormsModule, StatCardComponent, StatusBadgeComponent, DateFormatPipe],
  templateUrl: './admin-users.html',
})
export class AdminUsersComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly toastService = inject(ToastService);
  private readonly authService = inject(AuthService);

  allUsers = signal<UserDto[]>([]);
  sessions = signal<SessionDto[]>([]);
  loading = signal(true);
  searchQuery = signal('');
  roleFilter = signal('');
  statusFilter = signal('');
  currentPage = signal(1);
  pageSize = 5;

  activeModal = signal<'changeRole' | 'sessions' | 'resetPw' | 'invite' | 'toggleStatus' | null>(null);
  selectedUser = signal<UserDto | null>(null);
  selectedRole = signal('');
  resetPwSent = signal(false);
  resetPwForm = { newPassword: '' };
  showPassword = signal(false);
  actionInFlight = signal(false);
  inviteForm = { name: '', email: '', role: 'Surveyor' };
  inviteSuccess = signal(false);
  inviteError = signal<string | null>(null);

  selectedIds = signal<Set<string>>(new Set());
  showBulkConfirm = signal(false);
  bulkDeactivating = signal(false);
  selectedCount = computed(() => this.selectedIds().size);
  allPageSelected = computed(() => {
    const eligible = this.paginatedUsers().filter(u => this.canToggleStatus(u) && u.isActive);
    return eligible.length > 0 && eligible.every(u => this.selectedIds().has(u.id));
  });

  filteredUsers = computed(() => {
    let list = this.allUsers();
    const q = this.searchQuery().toLowerCase();
    if (q) list = list.filter(u => u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q));
    const role = this.roleFilter();
    if (role) list = list.filter(u => u.role === role);
    const status = this.statusFilter();
    if (status === 'Active') list = list.filter(u => u.isActive);
    if (status === 'Inactive') list = list.filter(u => !u.isActive);
    return list;
  });

  paginatedUsers = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredUsers().slice(start, start + this.pageSize);
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredUsers().length / this.pageSize)));
  totalUsers = computed(() => this.allUsers().length);
  activeUsers = computed(() => this.allUsers().filter(u => u.isActive).length);
  inactiveUsers = computed(() => this.allUsers().filter(u => !u.isActive).length);
  activeSessions = computed(() => this.sessions().filter(s => !s.isRevoked).length);

  iconUsers = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>';
  iconCheck = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>';
  iconUserX = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="8.5" cy="7" r="4"/><line x1="18" y1="8" x2="23" y2="13"/><line x1="23" y1="8" x2="18" y2="13"/></svg>';
  iconActivity = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>';

  roleOptions = [
    { role: 'Admin', label: 'Admin', desc: 'Full system access', bg: '#EEF4FF', fg: '#2D7FF9', dot: '#2D7FF9' },
    { role: 'ClaimsOfficer', label: 'Claims Officer', desc: 'Manage claims and grievances', bg: '#E6F4F8', fg: '#0F6E8C', dot: '#0F6E8C' },
    { role: 'Underwriter', label: 'Underwriter', desc: 'Review proposals and KYC', bg: '#FEF6E6', fg: '#D9920A', dot: '#D9920A' },
    { role: 'FinanceOfficer', label: 'Finance Officer', desc: 'Payments, payouts, commissions', bg: '#E8F7F1', fg: '#1F9D6B', dot: '#1F9D6B' },
    { role: 'Surveyor', label: 'Surveyor', desc: 'Handle assigned claims & reports', bg: '#FEF0EA', fg: '#D45E2F', dot: '#F2784B' },
    { role: 'Agent', label: 'Agent', desc: 'Submit proposals for customers', bg: '#F3E8FF', fg: '#7C3AED', dot: '#7C3AED' },
    { role: 'Customer', label: 'Customer', desc: 'Customer self-service portal', bg: '#F0F1F3', fg: '#6B7685', dot: '#9CA3AF' },
  ];

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.adminService.getAllUsers(1, 200).subscribe({
      next: res => { this.allUsers.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
    this.adminService.getAllSessions().subscribe({
      next: sessions => this.sessions.set(sessions),
    });
  }

  initials(name: string): string {
    const parts = name.trim().split(/\s+/);
    return (parts[0][0] + (parts[1]?.[0] ?? '')).toUpperCase();
  }

  avatarBg(name: string): string {
    const p = ['#E6F4F8', '#E8F7F1', '#FEF0EA', '#EEF4FF', '#FEF6E6', '#F3E8FF'];
    return p[(name.codePointAt(0)! + (name.codePointAt(1) || 0)) % p.length];
  }

  avatarFg(name: string): string {
    const p = ['#0F6E8C', '#1F9D6B', '#D45E2F', '#2D7FF9', '#D9920A', '#7C3AED'];
    return p[(name.codePointAt(0)! + (name.codePointAt(1) || 0)) % p.length];
  }

  roleBadge(role: string): { bg: string; fg: string; bdr: string; dot: string } {
    const m: Record<string, { bg: string; fg: string; bdr: string; dot: string }> = {
      Admin: { bg: '#EEF4FF', fg: '#2D7FF9', bdr: '#B0CCFC', dot: '#2D7FF9' },
      ClaimsOfficer: { bg: '#E6F4F8', fg: '#0F6E8C', bdr: '#B3D9E6', dot: '#0F6E8C' },
      Surveyor: { bg: '#FEF0EA', fg: '#D45E2F', bdr: '#F9C3A8', dot: '#F2784B' },
      Underwriter: { bg: '#FEF6E6', fg: '#D9920A', bdr: '#FAD88A', dot: '#D9920A' },
      FinanceOfficer: { bg: '#E8F7F1', fg: '#1F9D6B', bdr: '#B2E4CE', dot: '#1F9D6B' },
      Agent: { bg: '#F3E8FF', fg: '#7C3AED', bdr: '#DDD6FE', dot: '#7C3AED' },
      Customer: { bg: '#F0F1F3', fg: '#6B7685', bdr: '#D1D5DB', dot: '#9CA3AF' },
    };
    return m[role] ?? m['Customer'];
  }

  formatRole(role: string): string {
    const m: Record<string, string> = {
      ClaimsOfficer: 'Claims Officer', FinanceOfficer: 'Finance Officer',
    };
    return m[role] ?? role;
  }

  userSessionCount(userId: string): number {
    return this.sessions().filter(s => s.userId === userId && !s.isRevoked).length;
  }

  userSessionsForModal(): SessionDto[] {
    const u = this.selectedUser();
    return u ? this.sessions().filter(s => s.userId === u.id) : [];
  }

  isSelf(user: UserDto): boolean {
    return user.id === this.authService.currentUser()?.id;
  }

  canChangeRole(user: UserDto): boolean {
    return !(this.isSelf(user) && user.role === 'Admin');
  }

  canToggleStatus(user: UserDto): boolean {
    return !this.isSelf(user);
  }

  openChangeRoleModal(user: UserDto): void {
    if (!this.canChangeRole(user)) {
      this.toastService.warning('You cannot remove your own admin role.');
      return;
    }
    this.selectedUser.set(user);
    this.selectedRole.set(user.role);
    this.activeModal.set('changeRole');
  }

  openSessionsModal(user: UserDto): void {
    this.selectedUser.set(user);
    this.activeModal.set('sessions');
  }

  openResetPwModal(user: UserDto): void {
    this.selectedUser.set(user);
    this.resetPwSent.set(false);
    this.resetPwForm = { newPassword: '' };
    this.showPassword.set(false);
    this.activeModal.set('resetPw');
  }

  openInviteModal(): void {
    this.inviteForm = { name: '', email: '', role: 'Surveyor' };
    this.inviteSuccess.set(false);
    this.inviteError.set(null);
    this.activeModal.set('invite');
  }

  closeModal(): void {
    if (this.actionInFlight()) return;
    this.activeModal.set(null);
  }

  saveRole(): void {
    const u = this.selectedUser();
    if (!u || this.actionInFlight()) return;
    if (!this.canChangeRole(u)) {
      this.toastService.warning('You cannot remove your own admin role.');
      return;
    }
    this.actionInFlight.set(true);
    this.adminService.changeUserRole(u.id, this.selectedRole()).subscribe({
      next: () => {
        this.allUsers.update(list => list.map(usr => usr.id === u.id ? { ...usr, role: this.selectedRole() as any } : usr));
        this.toastService.success('Role updated for ' + u.fullName);
        this.actionInFlight.set(false);
        this.closeModal();
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toastService.error('Failed to update role');
      },
    });
  }

  openToggleStatusModal(user: UserDto): void {
    if (!this.canToggleStatus(user)) {
      this.toastService.warning('You cannot deactivate your own account.');
      return;
    }
    this.selectedUser.set(user);
    this.activeModal.set('toggleStatus');
  }

  confirmToggleStatus(): void {
    const user = this.selectedUser();
    if (!user || this.actionInFlight()) return;
    if (!this.canToggleStatus(user)) {
      this.toastService.warning('You cannot deactivate your own account.');
      return;
    }
    const next = !user.isActive;
    this.actionInFlight.set(true);
    this.adminService.toggleUserStatus(user.id, next).subscribe({
      next: () => {
        this.allUsers.update(list => list.map(u => u.id === user.id ? { ...u, isActive: next } : u));
        if (next) this.toastService.success(user.fullName + ' activated');
        else this.toastService.warning(user.fullName + ' deactivated');
        this.actionInFlight.set(false);
        this.closeModal();
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toastService.error('Failed to update status');
      },
    });
  }

  confirmResetPw(): void {
    const u = this.selectedUser();
    if (!u || this.actionInFlight()) return;
    const pw = this.resetPwForm.newPassword.trim();
    if (!pw || pw.length < 8) {
      this.toastService.warning('Password must be at least 8 characters.');
      return;
    }
    this.actionInFlight.set(true);
    this.adminService.resetPassword(u.id, { newPassword: pw }).subscribe({
      next: () => {
        this.resetPwSent.set(true);
        this.toastService.success('Password reset for ' + u.email);
        this.actionInFlight.set(false);
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toastService.error('Failed to reset password');
      },
    });
  }

  submitInvite(): void {
    const name = this.inviteForm.name.trim();
    const email = this.inviteForm.email.trim();
    if (!name || !email) {
      this.toastService.warning('Please enter a name and email.');
      return;
    }
    if (!/^[^\s@]{1,64}@[^\s@]{1,255}\.[^\s@]{1,24}$/.test(email)) {
      this.toastService.warning('Please enter a valid email address.');
      return;
    }
    const parts = name.trim().split(/\s+/);
    const firstName = parts[0];
    const lastName = parts.length > 1 ? parts.slice(1).join(' ') : '';
    if (!lastName) {
      this.toastService.warning('Please enter the full name — first and last name are required (e.g., "Rajesh Kumar").');
      return;
    }

    this.actionInFlight.set(true);
    this.inviteError.set(null);
    this.adminService.inviteUser({ firstName, lastName, email, role: this.inviteForm.role }).subscribe({
      next: () => {
        this.inviteSuccess.set(true);
        this.actionInFlight.set(false);
        this.loadData();
      },
      error: (err) => {
        this.inviteError.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to send invite. The email may already be registered.');
        this.actionInFlight.set(false);
      },
    });
  }

  prevPage(): void { this.currentPage.update(p => Math.max(1, p - 1)); }
  nextPage(): void { this.currentPage.update(p => Math.min(this.totalPages(), p + 1)); }
  min(a: number, b: number): number { return Math.min(a, b); }

  isSelected(id: string): boolean { return this.selectedIds().has(id); }

  toggleSelectUser(user: UserDto): void {
    if (!this.canToggleStatus(user) || !user.isActive) return;
    this.selectedIds.update(prev => {
      const next = new Set(prev);
      if (next.has(user.id)) next.delete(user.id); else next.add(user.id);
      return next;
    });
  }

  toggleSelectAll(): void {
    const eligible = this.paginatedUsers().filter(u => this.canToggleStatus(u) && u.isActive);
    const allSel = eligible.every(u => this.selectedIds().has(u.id));
    this.selectedIds.update(prev => {
      const next = new Set(prev);
      if (allSel) eligible.forEach(u => next.delete(u.id));
      else eligible.forEach(u => next.add(u.id));
      return next;
    });
  }

  clearSelection(): void { this.selectedIds.set(new Set()); }

  bulkDeactivate(): void {
    if (this.bulkDeactivating()) return;
    const ids = this.selectedIds();
    const users = this.allUsers().filter(u => ids.has(u.id) && u.isActive && this.canToggleStatus(u));
    if (!users.length) { this.clearSelection(); this.showBulkConfirm.set(false); return; }
    this.bulkDeactivating.set(true);
    let done = 0, failed = 0;
    for (const u of users) {
      this.adminService.toggleUserStatus(u.id, false).subscribe({
        next: () => {
          this.allUsers.update(list => list.map(usr => usr.id === u.id ? { ...usr, isActive: false } : usr));
          if (++done + failed === users.length) this.finishBulk(done, failed);
        },
        error: () => { if (done + ++failed === users.length) this.finishBulk(done, failed); },
      });
    }
  }

  private finishBulk(done: number, failed: number): void {
    this.bulkDeactivating.set(false);
    this.showBulkConfirm.set(false);
    this.clearSelection();
    if (done > 0) this.toastService.warning(`${done} user${done > 1 ? 's' : ''} deactivated`);
    if (failed > 0) this.toastService.error(`${failed} user${failed > 1 ? 's' : ''} could not be deactivated`);
  }
}
