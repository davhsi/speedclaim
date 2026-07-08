import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { UserDto, BranchDto, AgentProfileDto } from '../../../core/models/api.models';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-admin-agents',
  standalone: true,
  imports: [FormsModule, StatCardComponent, StatusBadgeComponent, PaginationComponent],
  templateUrl: './admin-agents.html',
})
export class AdminAgentsComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly toastService = inject(ToastService);

  activeTab = signal<'agents' | 'branches'>('agents');
  agents = signal<UserDto[]>([]);
  agentProfiles = signal<AgentProfileDto[]>([]);
  branches = signal<BranchDto[]>([]);
  loading = signal(true);
  searchQuery = signal('');

  activeModal = signal<'register' | 'assignBranch' | 'updateLicense' | 'editBranch' | 'createBranch' | null>(null);
  selectedAgent = signal<UserDto | null>(null);
  selectedBranch = signal<BranchDto | null>(null);
  selectedBranchId = signal<string | null>(null);

  regForm = { firstName: '', lastName: '', email: '', phone: '', licenseNumber: '', licenseExpiry: '', agencyName: '', aadhaarNumber: '', panNumber: '' };
  licForm = { licenseNumber: '', licenseExpiry: '' };
  branchForm = { name: '', city: '', state: '', address: '', phone: '', email: '' };

  currentPage = signal(1);
  readonly pageSize = 10;

  filteredAgents = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const list = this.agents();
    if (!q) return list;
    return list.filter(a => a.fullName.toLowerCase().includes(q) || a.email.toLowerCase().includes(q));
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredAgents().length / this.pageSize)));
  pagedAgents = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredAgents().slice(start, start + this.pageSize);
  });

  onSearch(val: string): void { this.searchQuery.set(val); this.currentPage.set(1); }
  onPageChange(page: number): void { this.currentPage.set(page); }

  totalAgents = computed(() => this.agents().length);
  activeAgents = computed(() => this.agents().filter(a => a.isActive).length);
  branchCount = computed(() => this.branches().length);
  expiringLicenses = computed(() => {
    const now = new Date();
    const threshold = new Date(now.getFullYear(), now.getMonth() + 3, now.getDate());
    return this.agentProfiles().filter(ap => {
      if (!ap.licenseExpiry) return false;
      const exp = new Date(ap.licenseExpiry);
      return exp <= threshold && exp >= now;
    }).length;
  });

  iconBriefcase = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 7V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v2"/></svg>';
  iconCheck = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>';
  iconBuilding = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="4" y="2" width="16" height="20" rx="2"/><path d="M9 22v-4h6v4"/><line x1="8" y1="6" x2="8" y2="6.01"/><line x1="12" y1="6" x2="12" y2="6.01"/><line x1="16" y1="6" x2="16" y2="6.01"/><line x1="8" y1="10" x2="8" y2="10.01"/><line x1="12" y1="10" x2="12" y2="10.01"/><line x1="16" y1="10" x2="16" y2="10.01"/><line x1="8" y1="14" x2="8" y2="14.01"/><line x1="12" y1="14" x2="12" y2="14.01"/><line x1="16" y1="14" x2="16" y2="14.01"/></svg>';
  iconAlert = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>';

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.adminService.getAllUsers(1, 200).subscribe({
      next: res => {
        this.agents.set(res.data.filter(u => u.role === 'Agent'));
        this.loading.set(false);
      },
    });
    this.adminService.getAgentProfiles().subscribe({
      next: profiles => this.agentProfiles.set(profiles),
      error: () => {},
    });
    this.adminService.getBranches().subscribe({
      next: branches => this.branches.set(branches),
    });
  }

  initials(name: string): string {
    const parts = name.trim().split(/\s+/);
    return (parts[0][0] + (parts[1]?.[0] ?? '')).toUpperCase();
  }

  avatarBg(name: string): string {
    const palettes = ['#E6F4F8', '#E8F7F1', '#FEF0EA', '#EEF4FF', '#FEF6E6', '#F3E8FF'];
    const h = name.codePointAt(0)! + (name.codePointAt(1) || 0);
    return palettes[h % palettes.length];
  }

  avatarFg(name: string): string {
    const palettes = ['#0F6E8C', '#1F9D6B', '#D45E2F', '#2D7FF9', '#D9920A', '#7C3AED'];
    const h = name.codePointAt(0)! + (name.codePointAt(1) || 0);
    return palettes[h % palettes.length];
  }

  getAgentProfile(userId: string): AgentProfileDto | undefined {
    return this.agentProfiles().find(ap => ap.userId === userId);
  }

  getAgentBranch(userId: string): string {
    const ap = this.getAgentProfile(userId);
    return ap?.branchName ?? '—';
  }

  licBadge(expiryStr: string): { label: string; bg: string; fg: string; bdr: string } {
    const exp = new Date(expiryStr);
    const now = new Date();
    const daysLeft = Math.floor((exp.getTime() - now.getTime()) / 86400000);
    if (daysLeft < 0) return { label: 'Expired', bg: '#FBE9E9', fg: '#D14343', bdr: '#F5B4B4' };
    if (daysLeft < 90) return { label: 'Expiring', bg: '#FEF6E6', fg: '#D9920A', bdr: '#FAD88A' };
    return { label: 'Valid', bg: '#E8F7F1', fg: '#1F9D6B', bdr: '#B2E4CE' };
  }

  openRegisterModal(): void {
    this.regForm = { firstName: '', lastName: '', email: '', phone: '', licenseNumber: '', licenseExpiry: '', agencyName: '', aadhaarNumber: '', panNumber: '' };
    this.activeModal.set('register');
  }

  openAssignBranchModal(agent: UserDto): void {
    this.selectedAgent.set(agent);
    const profile = this.getAgentProfile(agent.id);
    this.selectedBranchId.set(profile?.branchId ?? null);
    this.activeModal.set('assignBranch');
  }

  openUpdateLicenseModal(agent: UserDto): void {
    this.selectedAgent.set(agent);
    const profile = this.getAgentProfile(agent.id);
    this.licForm = { licenseNumber: profile?.licenseNumber ?? '', licenseExpiry: profile?.licenseExpiry ?? '' };
    this.activeModal.set('updateLicense');
  }

  openEditBranchModal(br: BranchDto): void {
    this.selectedBranch.set(br);
    this.branchForm = { name: br.name, city: br.city, state: br.state, address: br.address, phone: br.phone, email: br.email };
    this.activeModal.set('editBranch');
  }

  openCreateBranchModal(): void {
    this.branchForm = { name: '', city: '', state: '', address: '', phone: '', email: '' };
    this.activeModal.set('createBranch');
  }

  closeModal(): void {
    this.activeModal.set(null);
  }

  private isValidEmail(v: string): boolean {
    return /^[^\s@]{1,64}@[^\s@]{1,255}\.[^\s@]{1,24}$/.test(v.trim());
  }

  private isValidPhone(v: string): boolean {
    return /^\d{10}$/.test(v.trim());
  }

  private isValidAadhaar(v: string): boolean {
    return /^\d{12}$/.test(v.trim());
  }

  private isValidPan(v: string): boolean {
    return /^[A-Z]{5}\d{4}[A-Z]$/.test(v.trim().toUpperCase());
  }

  private isValidDate(v: string): boolean {
    return !!v && !Number.isNaN(new Date(v).getTime());
  }

  emailError(): string {
    const v = this.regForm.email.trim();
    if (!v) return '';
    return this.isValidEmail(v) ? '' : 'Enter a valid email address.';
  }

  phoneError(): string {
    const v = this.regForm.phone.trim();
    if (!v) return '';
    return this.isValidPhone(v) ? '' : 'Phone number must be exactly 10 digits.';
  }

  aadhaarError(): string {
    const v = this.regForm.aadhaarNumber.trim();
    if (!v) return '';
    return this.isValidAadhaar(v) ? '' : 'Aadhaar must be exactly 12 digits.';
  }

  panError(): string {
    const v = this.regForm.panNumber.trim();
    if (!v) return '';
    return this.isValidPan(v) ? '' : 'PAN must be in the format ABCDE1234F.';
  }

  registerFormInvalid(): boolean {
    const f = this.regForm;
    return !f.firstName.trim() || !f.lastName.trim() || !this.isValidEmail(f.email)
      || !this.isValidPhone(f.phone)
      || !f.licenseNumber.trim() || !this.isValidDate(f.licenseExpiry)
      || !f.agencyName.trim() || !this.isValidAadhaar(f.aadhaarNumber) || !this.isValidPan(f.panNumber);
  }

  licenseFormInvalid(): boolean {
    return !this.licForm.licenseNumber.trim() || !this.isValidDate(this.licForm.licenseExpiry);
  }

  branchFormInvalid(): boolean {
    const f = this.branchForm;
    return !f.name.trim() || !f.city.trim() || !f.state.trim() || !f.address.trim()
      || !this.isValidPhone(f.phone) || !this.isValidEmail(f.email);
  }

  toggleAgentStatus(agent: UserDto): void {
    const next = !agent.isActive;
    this.adminService.toggleAgentStatus(agent.id, next).subscribe({
      next: () => {
        this.agents.update(list => list.map(a => a.id === agent.id ? { ...a, isActive: next } : a));
        this.toastService.success(agent.fullName + (next ? ' activated' : ' deactivated'));
      },
      error: () => this.toastService.error('Failed to update status'),
    });
  }

  registerAgent(): void {
    const f = this.regForm;
    if (this.registerFormInvalid()) {
      this.toastService.warning('Please fill in all fields with valid values (email, phone, Aadhaar, PAN, and license expiry) before registering the agent.');
      return;
    }
    this.adminService.registerAgent({
      email: f.email,
      salutation: 'Mr',
      firstName: f.firstName,
      lastName: f.lastName,
      phone: f.phone,
      licenseNumber: f.licenseNumber,
      licenseExpiry: f.licenseExpiry,
      agencyName: f.agencyName,
      aadhaarNumber: f.aadhaarNumber,
      panNumber: f.panNumber,
      maritalStatus: 'Single',
      permanentAddress: { line1: 'Admin onboarding desk', city: 'Bengaluru', state: 'Karnataka', postalCode: '560001', country: 'India' },
      currentAddress: { line1: 'Admin onboarding desk', city: 'Bengaluru', state: 'Karnataka', postalCode: '560001', country: 'India' },
      isSameAsPermanent: true,
    }).subscribe({
      next: () => {
        this.toastService.success('Agent registered — they’ll get an email to set their password');
        this.closeModal();
        this.loadData();
      },
      error: () => this.toastService.error('Failed to register agent'),
    });
  }

  assignBranch(): void {
    const agent = this.selectedAgent();
    const branchId = this.selectedBranchId();
    if (!agent) return;
    if (!branchId) {
      this.toastService.warning('Please select a branch to assign.');
      return;
    }
    this.adminService.assignAgentToBranch(agent.id, branchId).subscribe({
      next: () => {
        this.toastService.success('Branch assigned');
        this.closeModal();
        this.loadData();
      },
      error: () => this.toastService.error('Failed to assign branch'),
    });
  }

  updateLicense(): void {
    const agent = this.selectedAgent();
    if (!agent) return;
    if (this.licenseFormInvalid()) {
      this.toastService.warning('Please enter a license number and a valid expiry date.');
      return;
    }
    this.adminService.updateAgentLicense(agent.id, {
      licenseNumber: this.licForm.licenseNumber,
      licenseExpiry: this.licForm.licenseExpiry,
    }).subscribe({
      next: () => {
        this.toastService.success('License updated');
        this.closeModal();
        this.loadData();
      },
      error: () => this.toastService.error('Failed to update license'),
    });
  }

  saveBranch(): void {
    const br = this.selectedBranch();
    if (!br) return;
    if (this.branchFormInvalid()) {
      this.toastService.warning('Please fill in all branch fields with valid values (name, city, state, address, phone, and email).');
      return;
    }
    this.adminService.updateBranch(br.id.toString(), {
      name: this.branchForm.name,
      city: this.branchForm.city,
      state: this.branchForm.state,
      address: this.branchForm.address,
      phone: this.branchForm.phone,
      email: this.branchForm.email,
    }).subscribe({
      next: updated => {
        this.branches.update(list => list.map(b => b.id === br.id ? updated : b));
        this.toastService.success('Branch updated');
        this.closeModal();
      },
      error: () => this.toastService.error('Failed to update branch'),
    });
  }

  submitCreateBranch(): void {
    if (this.branchFormInvalid()) {
      this.toastService.warning('Please fill in all branch fields with valid values (name, city, state, address, phone, and email).');
      return;
    }
    this.adminService.createBranch({
      name: this.branchForm.name,
      city: this.branchForm.city,
      state: this.branchForm.state,
      address: this.branchForm.address,
      phone: this.branchForm.phone,
      email: this.branchForm.email,
    }).subscribe({
      next: created => {
        this.branches.update(list => [...list, created]);
        this.toastService.success('Branch created');
        this.closeModal();
      },
      error: () => this.toastService.error('Failed to create branch'),
    });
  }
}
