import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AdminAgentsComponent } from './admin-agents';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { UserDto, BranchDto, AgentProfileDto } from '../../../core/models/api.models';

describe('AdminAgentsComponent', () => {
  let adminService: {
    getAllUsers: ReturnType<typeof vi.fn>;
    getAgentProfiles: ReturnType<typeof vi.fn>;
    getBranches: ReturnType<typeof vi.fn>;
    toggleAgentStatus: ReturnType<typeof vi.fn>;
    registerAgent: ReturnType<typeof vi.fn>;
    assignAgentToBranch: ReturnType<typeof vi.fn>;
    updateAgentLicense: ReturnType<typeof vi.fn>;
    updateBranch: ReturnType<typeof vi.fn>;
  };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };

  function agentUser(overrides: Partial<UserDto> = {}): UserDto {
    return {
      id: 'u1', email: 'agent@example.com', salutation: 'Mr', firstName: 'Raj', lastName: 'Kumar',
      fullName: 'Raj Kumar', phone: '9876543210', role: 'Agent', maritalStatus: 'Single',
      isEmailVerified: true, isActive: true, createdAt: '2024-01-01', kycApproved: false,
      ...overrides,
    };
  }

  function branch(overrides: Partial<BranchDto> = {}): BranchDto {
    return { id: 'b1', name: 'Central', city: 'Mumbai', state: 'Maharashtra', address: '1 Main St', phone: '9876543210', email: 'central@x.com', isActive: true, ...overrides };
  }

  function agentProfile(overrides: Partial<AgentProfileDto> = {}): AgentProfileDto {
    return {
      agentId: 'ag1', userId: 'u1', email: 'agent@example.com',
      fullName: 'Raj Kumar', agentCode: 'AGT001', agentType: 'Individual',
      licenseNumber: 'LIC001', licenseExpiry: '2030-01-01', commissionRate: 5, isActive: true,
      ...overrides,
    };
  }

  function create(users: UserDto[] = [agentUser()], profiles: AgentProfileDto[] = [], branches: BranchDto[] = []) {
    adminService.getAllUsers.mockReturnValue(of({ data: users, pageNumber: 1, pageSize: 200, totalRecords: users.length, totalPages: 1 }));
    adminService.getAgentProfiles.mockReturnValue(of(profiles));
    adminService.getBranches.mockReturnValue(of(branches));
    const fixture = TestBed.createComponent(AdminAgentsComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    adminService = {
      getAllUsers: vi.fn(), getAgentProfiles: vi.fn(), getBranches: vi.fn(),
      toggleAgentStatus: vi.fn(), registerAgent: vi.fn(), assignAgentToBranch: vi.fn(),
      updateAgentLicense: vi.fn(), updateBranch: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AdminAgentsComponent],
      providers: [
        { provide: AdminService, useValue: adminService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('loadData', () => {
    it('keeps only users with role Agent, and loads profiles + branches', () => {
      const fixture = create([agentUser({ id: 'u1' }), { ...agentUser({ id: 'u2' }), role: 'Customer' }], [agentProfile()], [branch()]);
      const c = fixture.componentInstance;
      expect(c.agents().map(a => a.id)).toEqual(['u1']);
      expect(c.agentProfiles().length).toBe(1);
      expect(c.branches().length).toBe(1);
      expect(c.loading()).toBe(false);
    });
  });

  describe('filteredAgents / pagination', () => {
    it('filters by name or email', () => {
      const fixture = create([agentUser({ id: 'u1', fullName: 'Raj Kumar', email: 'raj@x.com' }), agentUser({ id: 'u2', fullName: 'Priya Shah', email: 'priya@x.com' })]);
      fixture.componentInstance.onSearch('priya');
      expect(fixture.componentInstance.filteredAgents().map(a => a.id)).toEqual(['u2']);
    });

    it('resets to page 1 on search', () => {
      const fixture = create();
      fixture.componentInstance.currentPage.set(3);
      fixture.componentInstance.onSearch('x');
      expect(fixture.componentInstance.currentPage()).toBe(1);
    });

    it('paginates with pageSize=10', () => {
      const agents = Array.from({ length: 15 }, (_, i) => agentUser({ id: `u${i}` }));
      const fixture = create(agents);
      expect(fixture.componentInstance.pagedAgents().length).toBe(10);
      expect(fixture.componentInstance.totalPages()).toBe(2);
    });
  });

  describe('expiringLicenses', () => {
    beforeEach(() => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date('2026-01-01'));
    });
    afterEach(() => vi.useRealTimers());

    it('counts licenses expiring within 3 months (and not already expired)', () => {
      const fixture = create([agentUser()], [
        agentProfile({ userId: 'u1', licenseExpiry: '2026-02-01' }), // within 3 months
        agentProfile({ userId: 'u2', licenseExpiry: '2027-01-01' }), // far out
        agentProfile({ userId: 'u3', licenseExpiry: '2025-01-01' }), // already expired
      ]);
      expect(fixture.componentInstance.expiringLicenses()).toBe(1);
    });
  });

  describe('modal seeding', () => {
    it('openAssignBranchModal seeds the currently assigned branch id', () => {
      const fixture = create([agentUser({ id: 'u1' })], [agentProfile({ userId: 'u1', branchId: 'b1' })]);
      fixture.componentInstance.openAssignBranchModal(agentUser({ id: 'u1' }));
      expect(fixture.componentInstance.selectedBranchId()).toBe('b1');
      expect(fixture.componentInstance.activeModal()).toBe('assignBranch');
    });

    it('openUpdateLicenseModal seeds the license form from the existing profile', () => {
      const fixture = create([agentUser({ id: 'u1' })], [agentProfile({ userId: 'u1', licenseNumber: 'LIC999', licenseExpiry: '2031-05-01' })]);
      fixture.componentInstance.openUpdateLicenseModal(agentUser({ id: 'u1' }));
      expect(fixture.componentInstance.licForm).toEqual({ licenseNumber: 'LIC999', licenseExpiry: '2031-05-01' });
    });

    it('openEditBranchModal seeds the branch form', () => {
      const fixture = create();
      fixture.componentInstance.openEditBranchModal(branch({ name: 'North Branch' }));
      expect(fixture.componentInstance.branchForm.name).toBe('North Branch');
      expect(fixture.componentInstance.activeModal()).toBe('editBranch');
    });
  });

  describe('registerAgent', () => {
    function validForm() {
      return {
        firstName: 'Priya', lastName: 'Sharma', email: 'priya@example.com', phone: '9876543210',
        password: 'StrongP@ss1', licenseNumber: 'LIC001', licenseExpiry: '2030-01-01',
        agencyName: 'Agency Co', aadhaarNumber: '123456789012', panNumber: 'ABCDE1234F',
      };
    }

    it('warns on a weak password without submitting', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), password: 'weak' };
      c.registerAgent();
      expect(adminService.registerAgent).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalledWith(expect.stringContaining('Password must be'));
    });

    it('warns on other invalid fields (e.g. missing agency name)', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), agencyName: '' };
      c.registerAgent();
      expect(adminService.registerAgent).not.toHaveBeenCalled();
    });

    it('registers successfully and reloads the agent list', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = validForm();
      adminService.registerAgent.mockReturnValue(of({ message: 'ok' }));

      c.registerAgent();

      expect(adminService.registerAgent).toHaveBeenCalled();
      expect(toast.success).toHaveBeenCalledWith('Agent registered successfully');
      expect(adminService.getAllUsers).toHaveBeenCalledTimes(2);
    });

    it('shows an error toast on failure', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = validForm();
      adminService.registerAgent.mockReturnValue(throwError(() => ({ status: 500 })));
      c.registerAgent();
      expect(toast.error).toHaveBeenCalledWith('Failed to register agent');
    });
  });

  describe('assignBranch', () => {
    it('warns when no branch is selected', () => {
      const fixture = create([agentUser({ id: 'u1' })]);
      const c = fixture.componentInstance;
      c.openAssignBranchModal(agentUser({ id: 'u1' }));
      c.selectedBranchId.set(null);
      c.assignBranch();
      expect(adminService.assignAgentToBranch).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalledWith('Please select a branch to assign.');
    });

    it('assigns using the profile agentId when available, falling back to the user id', () => {
      const fixture = create([agentUser({ id: 'u1' })], [agentProfile({ userId: 'u1', agentId: 'ag-real' })]);
      const c = fixture.componentInstance;
      c.openAssignBranchModal(agentUser({ id: 'u1' }));
      c.selectedBranchId.set('b1');
      adminService.assignAgentToBranch.mockReturnValue(of({ message: 'ok' }));

      c.assignBranch();

      expect(adminService.assignAgentToBranch).toHaveBeenCalledWith('ag-real', 'b1');
      expect(toast.success).toHaveBeenCalledWith('Branch assigned');
    });
  });

  describe('updateLicense', () => {
    it('warns on an invalid license form', () => {
      const fixture = create([agentUser({ id: 'u1' })]);
      const c = fixture.componentInstance;
      c.openUpdateLicenseModal(agentUser({ id: 'u1' }));
      c.licForm = { licenseNumber: '', licenseExpiry: '' };
      c.updateLicense();
      expect(adminService.updateAgentLicense).not.toHaveBeenCalled();
    });

    it('updates the license successfully', () => {
      const fixture = create([agentUser({ id: 'u1' })]);
      const c = fixture.componentInstance;
      c.openUpdateLicenseModal(agentUser({ id: 'u1' }));
      c.licForm = { licenseNumber: 'LIC123', licenseExpiry: '2031-01-01' };
      adminService.updateAgentLicense.mockReturnValue(of({ message: 'ok' }));

      c.updateLicense();

      expect(adminService.updateAgentLicense).toHaveBeenCalledWith('u1', { licenseNumber: 'LIC123', licenseExpiry: '2031-01-01' });
      expect(toast.success).toHaveBeenCalledWith('License updated');
    });
  });

  describe('saveBranch', () => {
    it('warns on an invalid branch form', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.openEditBranchModal(branch());
      c.branchForm.phone = 'not-a-phone';
      c.saveBranch();
      expect(adminService.updateBranch).not.toHaveBeenCalled();
    });

    it('saves the branch and updates it in the local list', () => {
      const fixture = create([], [], [branch({ id: 'b1', name: 'Old Name' })]);
      const c = fixture.componentInstance;
      c.openEditBranchModal(branch({ id: 'b1', name: 'Old Name' }));
      c.branchForm.name = 'New Name';
      const updated = branch({ id: 'b1', name: 'New Name' });
      adminService.updateBranch.mockReturnValue(of(updated));

      c.saveBranch();

      expect(c.branches().find(b => b.id === 'b1')?.name).toBe('New Name');
      expect(toast.success).toHaveBeenCalledWith('Branch updated');
    });
  });

  describe('toggleAgentStatus', () => {
    it('deactivates an active agent using the resolved agentId', () => {
      const fixture = create([agentUser({ id: 'u1', isActive: true })], [agentProfile({ userId: 'u1', agentId: 'ag-real' })]);
      const c = fixture.componentInstance;
      adminService.toggleAgentStatus.mockReturnValue(of({ message: 'ok' }));

      c.toggleAgentStatus(agentUser({ id: 'u1', isActive: true }));

      expect(adminService.toggleAgentStatus).toHaveBeenCalledWith('ag-real', false);
      expect(c.agents().find(a => a.id === 'u1')?.isActive).toBe(false);
    });
  });

  describe('licBadge', () => {
    beforeEach(() => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date('2026-01-01'));
    });
    afterEach(() => vi.useRealTimers());

    it('labels a past date as Expired', () => {
      const fixture = create();
      expect(fixture.componentInstance.licBadge('2025-01-01').label).toBe('Expired');
    });

    it('labels a date under 90 days out as Expiring', () => {
      const fixture = create();
      expect(fixture.componentInstance.licBadge('2026-02-01').label).toBe('Expiring');
    });

    it('labels a far-future date as Valid', () => {
      const fixture = create();
      expect(fixture.componentInstance.licBadge('2030-01-01').label).toBe('Valid');
    });
  });
});
