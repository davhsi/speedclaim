import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AdminUsersComponent } from './admin-users';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { AuthUserDto, UserDto, SessionDto } from '../../../core/models/api.models';

describe('AdminUsersComponent', () => {
  let adminService: {
    getAllUsers: ReturnType<typeof vi.fn>;
    getAllSessions: ReturnType<typeof vi.fn>;
    changeUserRole: ReturnType<typeof vi.fn>;
    toggleUserStatus: ReturnType<typeof vi.fn>;
    resetPassword: ReturnType<typeof vi.fn>;
    inviteUser: ReturnType<typeof vi.fn>;
  };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };
  let authService: { currentUser: ReturnType<typeof vi.fn> };

  function user(overrides: Partial<UserDto> = {}): UserDto {
    return {
      id: 'u1', email: 'jane@example.com', salutation: 'Ms', firstName: 'Jane', lastName: 'Doe',
      fullName: 'Jane Doe', phone: '9876543210', role: 'Customer', maritalStatus: 'Single',
      isEmailVerified: true, isActive: true, createdAt: '2024-01-01', kycApproved: false,
      ...overrides,
    };
  }

  function session(overrides: Partial<SessionDto> = {}): SessionDto {
    return { id: 's1', userId: 'u1', userEmail: 'jane@example.com', ipAddress: '1.1.1.1', userAgent: 'test', expiresAt: '2030-01-01', isRevoked: false, createdAt: '2024-01-01', ...overrides };
  }

  function create(users: UserDto[] = [user()], sessions: SessionDto[] = []) {
    adminService.getAllUsers.mockReturnValue(of({ data: users, pageNumber: 1, pageSize: 200, totalRecords: users.length, totalPages: 1 }));
    adminService.getAllSessions.mockReturnValue(of(sessions));
    const fixture = TestBed.createComponent(AdminUsersComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    adminService = {
      getAllUsers: vi.fn(), getAllSessions: vi.fn(), changeUserRole: vi.fn(),
      toggleUserStatus: vi.fn(), resetPassword: vi.fn(), inviteUser: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };
    authService = { currentUser: vi.fn(() => ({ id: 'admin1' } as AuthUserDto)) };

    TestBed.configureTestingModule({
      imports: [AdminUsersComponent],
      providers: [
        { provide: AdminService, useValue: adminService },
        { provide: ToastService, useValue: toast },
        { provide: AuthService, useValue: authService },
      ],
    });
  });

  describe('ngOnInit / loadData', () => {
    it('loads users and sessions and clears the loading flag', () => {
      const fixture = create([user({ id: 'u1' }), user({ id: 'u2' })], [session()]);
      const c = fixture.componentInstance;
      expect(c.allUsers().length).toBe(2);
      expect(c.sessions().length).toBe(1);
      expect(c.loading()).toBe(false);
    });

    it('clears the loading flag even if the users request fails', () => {
      adminService.getAllUsers.mockReturnValue(throwError(() => ({ status: 500 })));
      adminService.getAllSessions.mockReturnValue(of([]));
      const fixture = TestBed.createComponent(AdminUsersComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('filteredUsers', () => {
    it('filters by name or email substring, case-insensitively', () => {
      const fixture = create([user({ id: 'u1', fullName: 'Jane Doe', email: 'jane@x.com' }), user({ id: 'u2', fullName: 'Bob Smith', email: 'bob@x.com' })]);
      fixture.componentInstance.searchQuery.set('JANE');
      expect(fixture.componentInstance.filteredUsers().map(u => u.id)).toEqual(['u1']);
    });

    it('filters by role', () => {
      const fixture = create([user({ id: 'u1', role: 'Admin' }), user({ id: 'u2', role: 'Customer' })]);
      fixture.componentInstance.roleFilter.set('Admin');
      expect(fixture.componentInstance.filteredUsers().map(u => u.id)).toEqual(['u1']);
    });

    it('filters by Active/Inactive status', () => {
      const fixture = create([user({ id: 'u1', isActive: true }), user({ id: 'u2', isActive: false })]);
      fixture.componentInstance.statusFilter.set('Inactive');
      expect(fixture.componentInstance.filteredUsers().map(u => u.id)).toEqual(['u2']);
    });
  });

  describe('pagination', () => {
    it('paginates using pageSize=5 and computes totalPages', () => {
      const users = Array.from({ length: 12 }, (_, i) => user({ id: `u${i}` }));
      const fixture = create(users);
      expect(fixture.componentInstance.paginatedUsers().length).toBe(5);
      expect(fixture.componentInstance.totalPages()).toBe(3);
    });

    it('prevPage/nextPage clamp within [1, totalPages]', () => {
      const users = Array.from({ length: 12 }, (_, i) => user({ id: `u${i}` }));
      const fixture = create(users);
      const c = fixture.componentInstance;
      c.prevPage();
      expect(c.currentPage()).toBe(1);
      c.currentPage.set(3);
      c.nextPage();
      expect(c.currentPage()).toBe(3);
    });
  });

  describe('self-protection rules', () => {
    it('isSelf is true only for the logged-in admin', () => {
      const fixture = create([user({ id: 'admin1' })]);
      expect(fixture.componentInstance.isSelf(user({ id: 'admin1' }))).toBe(true);
      expect(fixture.componentInstance.isSelf(user({ id: 'other' }))).toBe(false);
    });

    it('blocks changing your own Admin role and warns instead of opening the modal', () => {
      const fixture = create([user({ id: 'admin1', role: 'Admin' })]);
      fixture.componentInstance.openChangeRoleModal(user({ id: 'admin1', role: 'Admin' }));
      expect(fixture.componentInstance.activeModal()).toBeNull();
      expect(toast.warning).toHaveBeenCalledWith('You cannot remove your own admin role.');
    });

    it('allows opening the change-role modal for another user', () => {
      const fixture = create();
      fixture.componentInstance.openChangeRoleModal(user({ id: 'u2', role: 'Agent' }));
      expect(fixture.componentInstance.activeModal()).toBe('changeRole');
      expect(fixture.componentInstance.selectedRole()).toBe('Agent');
    });

    it('blocks toggling your own status', () => {
      const fixture = create();
      fixture.componentInstance.openToggleStatusModal(user({ id: 'admin1' }));
      expect(fixture.componentInstance.activeModal()).toBeNull();
      expect(toast.warning).toHaveBeenCalledWith('You cannot deactivate your own account.');
    });
  });

  describe('saveRole', () => {
    it('updates the role locally, toasts success, and closes the modal', () => {
      const fixture = create([user({ id: 'u2', role: 'Customer' })]);
      const c = fixture.componentInstance;
      c.openChangeRoleModal(user({ id: 'u2', role: 'Customer' }));
      c.selectedRole.set('Agent');
      adminService.changeUserRole.mockReturnValue(of({ message: 'ok' }));

      c.saveRole();

      expect(adminService.changeUserRole).toHaveBeenCalledWith('u2', 'Agent');
      expect(c.allUsers().find(u => u.id === 'u2')?.role).toBe('Agent');
      expect(toast.success).toHaveBeenCalled();
      expect(c.activeModal()).toBeNull();
    });

    it('shows an error toast when the update fails', () => {
      const fixture = create([user({ id: 'u2' })]);
      const c = fixture.componentInstance;
      c.openChangeRoleModal(user({ id: 'u2' }));
      adminService.changeUserRole.mockReturnValue(throwError(() => ({ status: 500 })));

      c.saveRole();

      expect(toast.error).toHaveBeenCalledWith('Failed to update role');
    });
  });

  describe('confirmToggleStatus', () => {
    it('deactivates an active user with a warning toast', () => {
      const fixture = create([user({ id: 'u2', isActive: true, fullName: 'Bob Smith' })]);
      const c = fixture.componentInstance;
      c.openToggleStatusModal(user({ id: 'u2', isActive: true, fullName: 'Bob Smith' }));
      adminService.toggleUserStatus.mockReturnValue(of({ message: 'ok' }));

      c.confirmToggleStatus();

      expect(adminService.toggleUserStatus).toHaveBeenCalledWith('u2', false);
      expect(c.allUsers().find(u => u.id === 'u2')?.isActive).toBe(false);
      expect(toast.warning).toHaveBeenCalledWith('Bob Smith deactivated');
    });

    it('activates an inactive user with a success toast', () => {
      const fixture = create([user({ id: 'u2', isActive: false, fullName: 'Bob Smith' })]);
      const c = fixture.componentInstance;
      c.openToggleStatusModal(user({ id: 'u2', isActive: false, fullName: 'Bob Smith' }));
      adminService.toggleUserStatus.mockReturnValue(of({ message: 'ok' }));

      c.confirmToggleStatus();

      expect(adminService.toggleUserStatus).toHaveBeenCalledWith('u2', true);
      expect(toast.success).toHaveBeenCalledWith('Bob Smith activated');
    });
  });

  describe('confirmResetPw', () => {
    it('warns and does not submit a password shorter than 8 characters', () => {
      const fixture = create([user({ id: 'u2' })]);
      const c = fixture.componentInstance;
      c.openResetPwModal(user({ id: 'u2' }));
      c.resetPwForm.newPassword = 'short';

      c.confirmResetPw();

      expect(adminService.resetPassword).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalledWith('Password must be at least 8 characters.');
    });

    it('resets the password and marks resetPwSent on success', () => {
      const fixture = create([user({ id: 'u2', email: 'bob@x.com' })]);
      const c = fixture.componentInstance;
      c.openResetPwModal(user({ id: 'u2', email: 'bob@x.com' }));
      c.resetPwForm.newPassword = 'LongEnough1!';
      adminService.resetPassword.mockReturnValue(of({ message: 'ok' }));

      c.confirmResetPw();

      expect(adminService.resetPassword).toHaveBeenCalledWith('u2', { newPassword: 'LongEnough1!' });
      expect(c.resetPwSent()).toBe(true);
      expect(toast.success).toHaveBeenCalledWith('Password reset for bob@x.com');
    });
  });

  describe('submitInvite', () => {
    it('warns when name or email is missing', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.openInviteModal();
      c.submitInvite();
      expect(adminService.inviteUser).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalled();
    });

    it('warns when the full name has no last name', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.openInviteModal();
      c.inviteForm = { name: 'Priya', email: 'priya@example.com', role: 'Surveyor' };
      c.submitInvite();
      expect(adminService.inviteUser).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalledWith(expect.stringContaining('first and last name'));
    });

    it('warns on an invalid email', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.inviteForm = { name: 'Priya Sharma', email: 'not-an-email', role: 'Surveyor' };
      c.submitInvite();
      expect(adminService.inviteUser).not.toHaveBeenCalled();
    });

    it('invites successfully and reloads data', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.inviteForm = { name: 'Priya Sharma', email: 'priya@example.com', role: 'Surveyor' };
      adminService.inviteUser.mockReturnValue(of({ message: 'ok' }));

      c.submitInvite();

      expect(adminService.inviteUser).toHaveBeenCalledWith({ firstName: 'Priya', lastName: 'Sharma', email: 'priya@example.com', role: 'Surveyor' });
      expect(c.inviteSuccess()).toBe(true);
      expect(adminService.getAllUsers).toHaveBeenCalledTimes(2); // init + reload
    });

    it('sets inviteError from the server response on failure', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.inviteForm = { name: 'Priya Sharma', email: 'priya@example.com', role: 'Surveyor' };
      adminService.inviteUser.mockReturnValue(throwError(() => ({ error: { detail: 'Email already registered' } })));

      c.submitInvite();

      expect(c.inviteError()).toBe('Email already registered');
    });
  });

  describe('bulk deactivate', () => {
    it('toggles individual selection, ignoring ineligible (self or inactive) users', () => {
      const fixture = create([user({ id: 'admin1' }), user({ id: 'u2', isActive: false }), user({ id: 'u3', isActive: true })]);
      const c = fixture.componentInstance;
      c.toggleSelectUser(user({ id: 'admin1' })); // self — ignored
      c.toggleSelectUser(user({ id: 'u2', isActive: false })); // inactive — ignored
      c.toggleSelectUser(user({ id: 'u3', isActive: true })); // eligible
      expect(c.selectedIds()).toEqual(new Set(['u3']));
    });

    it('toggleSelectAll selects all eligible users on the current page, then deselects on second call', () => {
      const fixture = create([user({ id: 'u1', isActive: true }), user({ id: 'u2', isActive: true })]);
      const c = fixture.componentInstance;
      c.toggleSelectAll();
      expect(c.selectedIds()).toEqual(new Set(['u1', 'u2']));
      c.toggleSelectAll();
      expect(c.selectedIds().size).toBe(0);
    });

    it('bulkDeactivate deactivates all eligible selected users and reports success', () => {
      const fixture = create([user({ id: 'u1', isActive: true }), user({ id: 'u2', isActive: true })]);
      const c = fixture.componentInstance;
      adminService.toggleUserStatus.mockReturnValue(of({ message: 'ok' }));
      c.toggleSelectAll();

      c.bulkDeactivate();

      expect(adminService.toggleUserStatus).toHaveBeenCalledTimes(2);
      expect(c.allUsers().every(u => !u.isActive)).toBe(true);
      expect(toast.warning).toHaveBeenCalledWith('2 users deactivated');
      expect(c.selectedIds().size).toBe(0);
      expect(c.showBulkConfirm()).toBe(false);
    });

    it('reports partial failure when some deactivations fail', () => {
      const fixture = create([user({ id: 'u1', isActive: true }), user({ id: 'u2', isActive: true })]);
      const c = fixture.componentInstance;
      adminService.toggleUserStatus.mockImplementation((id: string) =>
        id === 'u1' ? of({ message: 'ok' }) : throwError(() => ({ status: 500 })),
      );
      c.toggleSelectAll();

      c.bulkDeactivate();

      expect(toast.warning).toHaveBeenCalledWith('1 user deactivated');
      expect(toast.error).toHaveBeenCalledWith('1 user could not be deactivated');
    });
  });

  describe('initials', () => {
    it('builds initials from the first letters of the first two name parts', () => {
      const fixture = create();
      expect(fixture.componentInstance.initials('Jane Doe')).toBe('JD');
    });

    it('handles a single-word name', () => {
      const fixture = create();
      expect(fixture.componentInstance.initials('Cher')).toBe('C');
    });
  });
});
