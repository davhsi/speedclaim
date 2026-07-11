import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError, Subject } from 'rxjs';
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
    createBranch: ReturnType<typeof vi.fn>;
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
      updateAgentLicense: vi.fn(), updateBranch: vi.fn(), createBranch: vi.fn(),
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
      expect(c.agentProfiles()).toHaveLength(1);
      expect(c.branches()).toHaveLength(1);
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
      expect(fixture.componentInstance.pagedAgents()).toHaveLength(10);
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
    function validAddress() {
      return { line1: '12 MG Road', line2: '', city: 'Bengaluru', state: 'Karnataka', postalCode: '560001', country: 'India' };
    }

    function validForm() {
      return {
        firstName: 'Priya', lastName: 'Sharma', email: 'priya@example.com', phone: '9876543210',
        licenseNumber: 'LIC001', licenseExpiry: '2030-01-01',
        agencyName: 'Agency Co', aadhaarNumber: '123456789012', panNumber: 'ABCDE1234F',
        permanentAddress: validAddress(), currentAddress: validAddress(), sameAsPermanent: true,
      };
    }

    it('warns on other invalid fields (e.g. missing agency name)', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), agencyName: '' };
      c.registerAgent();
      expect(adminService.registerAgent).not.toHaveBeenCalled();
    });

    it('warns when the permanent address is incomplete — the server requires a real address, not a placeholder', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), permanentAddress: { ...validAddress(), city: '' } };
      c.registerAgent();
      expect(adminService.registerAgent).not.toHaveBeenCalled();
    });

    it('warns on an invalid permanent postal code', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), permanentAddress: { ...validAddress(), postalCode: '123' } };
      c.registerAgent();
      expect(adminService.registerAgent).not.toHaveBeenCalled();
    });

    it('does not require the current address fields when "same as permanent" is checked', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), currentAddress: { line1: '', line2: '', city: '', state: '', postalCode: '', country: 'India' }, sameAsPermanent: true };
      expect(c.registerFormInvalid()).toBe(false);
    });

    it('requires the current address fields when "same as permanent" is unchecked', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), currentAddress: { line1: '', line2: '', city: '', state: '', postalCode: '', country: 'India' }, sameAsPermanent: false };
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
      expect(toast.success).toHaveBeenCalledWith('Agent registered — they’ll get an email to set their password');
      expect(adminService.getAllUsers).toHaveBeenCalledTimes(2);
    });

    it('sends the permanent address as the current address when "same as permanent" is checked, rather than the placeholder address that used to be hardcoded', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), sameAsPermanent: true };
      adminService.registerAgent.mockReturnValue(of({ message: 'ok' }));

      c.registerAgent();

      expect(adminService.registerAgent).toHaveBeenCalledWith(expect.objectContaining({
        permanentAddress: validAddress(),
        currentAddress: validAddress(),
        isSameAsPermanent: true,
      }));
    });

    it('sends the distinct current address when "same as permanent" is unchecked', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const otherAddress = { line1: '9 Residency Rd', line2: '', city: 'Mumbai', state: 'Maharashtra', postalCode: '400001', country: 'India' };
      c.regForm = { ...validForm(), currentAddress: otherAddress, sameAsPermanent: false };
      adminService.registerAgent.mockReturnValue(of({ message: 'ok' }));

      c.registerAgent();

      expect(adminService.registerAgent).toHaveBeenCalledWith(expect.objectContaining({
        permanentAddress: validAddress(),
        currentAddress: otherAddress,
        isSameAsPermanent: false,
      }));
    });

    it('does not show its own toast on failure — the global error interceptor already surfaces the server message', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = validForm();
      adminService.registerAgent.mockReturnValue(throwError(() => ({ status: 500 })));
      c.registerAgent();
      expect(toast.error).not.toHaveBeenCalled();
    });

    it('trims padded values before submitting, so a value that only looks valid because inline validation trims does not get rejected by the server', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.regForm = { ...validForm(), aadhaarNumber: ' 123456789012', agencyName: 'Agency Co ', permanentAddress: { ...validAddress(), city: ' Bengaluru' } };
      adminService.registerAgent.mockReturnValue(of({ message: 'ok' }));

      c.registerAgent();

      expect(adminService.registerAgent).toHaveBeenCalledWith(expect.objectContaining({
        aadhaarNumber: '123456789012',
        agencyName: 'Agency Co',
        permanentAddress: validAddress(),
      }));
    });

    it('shows a submitting state while the request is in flight, blocks a second submit, and clears it on success', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.openRegisterModal();
      c.regForm = validForm();
      const request$ = new Subject<{ message: string }>();
      adminService.registerAgent.mockReturnValue(request$);

      c.registerAgent();
      expect(c.submitting()).toBe(true);

      // A second click while the request is still pending must not fire another request.
      c.registerAgent();
      expect(adminService.registerAgent).toHaveBeenCalledTimes(1);

      // Closing the modal (Cancel, the X button, or the backdrop all route through closeModal())
      // must not be possible while a request is in flight.
      c.closeModal();
      expect(c.activeModal()).toBe('register');

      request$.next({ message: 'ok' });
      request$.complete();

      expect(c.submitting()).toBe(false);
      expect(c.activeModal()).toBeNull();
    });

    it('clears the submitting state on failure so the form can be retried', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.openRegisterModal();
      c.regForm = validForm();
      const request$ = new Subject<{ message: string }>();
      adminService.registerAgent.mockReturnValue(request$);

      c.registerAgent();
      expect(c.submitting()).toBe(true);

      request$.error({ status: 500 });

      expect(c.submitting()).toBe(false);
      expect(c.activeModal()).toBe('register');
    });
  });

  describe('trimField', () => {
    it('strips leading/trailing whitespace in place', () => {
      const c = create().componentInstance;
      c.regForm.aadhaarNumber = ' 123456789012 ';
      c.trimField(c.regForm, 'aadhaarNumber');
      expect(c.regForm.aadhaarNumber).toBe('123456789012');
    });

    it('leaves an already-clean value untouched', () => {
      const c = create().componentInstance;
      c.regForm.firstName = 'Priya';
      c.trimField(c.regForm, 'firstName');
      expect(c.regForm.firstName).toBe('Priya');
    });
  });

  describe('inline field error getters', () => {
    it('are empty until a field has content', () => {
      const c = create().componentInstance;
      expect(c.emailError()).toBe('');
      expect(c.phoneError()).toBe('');
      expect(c.aadhaarError()).toBe('');
      expect(c.panError()).toBe('');
    });

    it('flag an invalid email', () => {
      const c = create().componentInstance;
      c.regForm.email = 'not-an-email';
      expect(c.emailError()).toBe('Enter a valid email address.');
      c.regForm.email = 'agent@example.com';
      expect(c.emailError()).toBe('');
    });

    it('flag a phone number that is not exactly 10 digits', () => {
      const c = create().componentInstance;
      c.regForm.phone = '12345';
      expect(c.phoneError()).toBe('Phone number must be exactly 10 digits.');
      c.regForm.phone = '9876543210';
      expect(c.phoneError()).toBe('');
    });

    it('flag an Aadhaar number that is not exactly 12 digits', () => {
      const c = create().componentInstance;
      c.regForm.aadhaarNumber = '123';
      expect(c.aadhaarError()).toBe('Aadhaar must be exactly 12 digits.');
      c.regForm.aadhaarNumber = '123456789012';
      expect(c.aadhaarError()).toBe('');
    });

    it('flag a malformed PAN', () => {
      const c = create().componentInstance;
      c.regForm.panNumber = 'ABC123';
      expect(c.panError()).toBe('PAN must be in the format ABCDE1234F.');
      c.regForm.panNumber = 'ABCDE1234F';
      expect(c.panError()).toBe('');
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

    it('assigns using the agent user id (the backend looks the agent up by UserId)', () => {
      const fixture = create([agentUser({ id: 'u1' })], [agentProfile({ userId: 'u1', agentId: 'ag-real' })]);
      const c = fixture.componentInstance;
      c.openAssignBranchModal(agentUser({ id: 'u1' }));
      c.selectedBranchId.set('b1');
      adminService.assignAgentToBranch.mockReturnValue(of({ message: 'ok' }));

      c.assignBranch();

      expect(adminService.assignAgentToBranch).toHaveBeenCalledWith('u1', 'b1');
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

  describe('openCreateBranchModal / submitCreateBranch', () => {
    it('resets the branch form and opens the createBranch modal', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.branchForm.name = 'leftover';
      c.openCreateBranchModal();
      expect(c.branchForm).toEqual({ name: '', city: '', state: '', address: '', phone: '', email: '' });
      expect(c.activeModal()).toBe('createBranch');
    });

    it('warns on an invalid branch form without calling the service', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.openCreateBranchModal();
      c.branchForm.phone = 'not-a-phone';
      c.submitCreateBranch();
      expect(adminService.createBranch).not.toHaveBeenCalled();
    });

    it('creates the branch and appends it to the local list', () => {
      const fixture = create([], [], [branch({ id: 'b1', name: 'Central' })]);
      const c = fixture.componentInstance;
      c.openCreateBranchModal();
      c.branchForm = { name: 'New Branch', city: 'Pune', state: 'Maharashtra', address: '2 Side St', phone: '9876543211', email: 'new@x.com' };
      const created = branch({ id: 'b2', name: 'New Branch', city: 'Pune' });
      adminService.createBranch.mockReturnValue(of(created));

      c.submitCreateBranch();

      expect(adminService.createBranch).toHaveBeenCalledWith({
        name: 'New Branch', city: 'Pune', state: 'Maharashtra', address: '2 Side St', phone: '9876543211', email: 'new@x.com',
      });
      expect(c.branches().map(b => b.id)).toEqual(['b1', 'b2']);
      expect(toast.success).toHaveBeenCalledWith('Branch created');
    });

    it('does not show its own toast on failure — the global error interceptor already surfaces the server message', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.openCreateBranchModal();
      c.branchForm = { name: 'New Branch', city: 'Pune', state: 'Maharashtra', address: '2 Side St', phone: '9876543211', email: 'new@x.com' };
      adminService.createBranch.mockReturnValue(throwError(() => ({ status: 500 })));
      c.submitCreateBranch();
      expect(toast.error).not.toHaveBeenCalled();
    });
  });

  describe('toggleAgentStatus', () => {
    it('deactivates an active agent using the agent user id', () => {
      const fixture = create([agentUser({ id: 'u1', isActive: true })], [agentProfile({ userId: 'u1', agentId: 'ag-real' })]);
      const c = fixture.componentInstance;
      adminService.toggleAgentStatus.mockReturnValue(of({ message: 'ok' }));

      c.toggleAgentStatus(agentUser({ id: 'u1', isActive: true }));

      expect(adminService.toggleAgentStatus).toHaveBeenCalledWith('u1', false);
      expect(c.agents().find(a => a.id === 'u1')?.isActive).toBe(false);
    });

    it('marks the row as toggling while in flight, blocks a duplicate click, and clears on success', () => {
      const fixture = create([agentUser({ id: 'u1', isActive: true })]);
      const c = fixture.componentInstance;
      const request$ = new Subject<{ message: string }>();
      adminService.toggleAgentStatus.mockReturnValue(request$);

      c.toggleAgentStatus(agentUser({ id: 'u1', isActive: true }));
      expect(c.isTogglingStatus('u1')).toBe(true);

      c.toggleAgentStatus(agentUser({ id: 'u1', isActive: true }));
      expect(adminService.toggleAgentStatus).toHaveBeenCalledTimes(1);

      request$.next({ message: 'ok' });
      request$.complete();

      expect(c.isTogglingStatus('u1')).toBe(false);
    });

    it('clears the toggling state on failure', () => {
      const fixture = create([agentUser({ id: 'u1', isActive: true })]);
      const c = fixture.componentInstance;
      const request$ = new Subject<{ message: string }>();
      adminService.toggleAgentStatus.mockReturnValue(request$);

      c.toggleAgentStatus(agentUser({ id: 'u1', isActive: true }));
      request$.error({ status: 500 });

      expect(c.isTogglingStatus('u1')).toBe(false);
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
