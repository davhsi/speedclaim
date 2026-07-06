import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { AdminSystemComponent } from './admin-system';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuditLogDto, EmailTemplateDto, NotificationDto, SystemConfigDto } from '../../../core/models/api.models';

describe('AdminSystemComponent', () => {
  let adminService: {
    getSystemConfigs: ReturnType<typeof vi.fn>;
    updateSystemConfig: ReturnType<typeof vi.fn>;
    getAuditLogs: ReturnType<typeof vi.fn>;
    getNotificationLogs: ReturnType<typeof vi.fn>;
    getEmailTemplates: ReturnType<typeof vi.fn>;
    saveEmailTemplate: ReturnType<typeof vi.fn>;
  };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  function config(overrides: Partial<SystemConfigDto> = {}): SystemConfigDto {
    return { configKey: 'MaxLoginAttempts', configValue: '5', ...overrides };
  }

  function auditPage(overrides: Partial<{ data: AuditLogDto[]; totalRecords: number; totalPages: number }> = {}) {
    return {
      data: overrides.data ?? [{ id: 'a1', entityType: 'User', entityId: 'u1', action: 'UserRoleChanged', createdAt: '2026-01-01' } as AuditLogDto],
      pageNumber: 1,
      pageSize: 25,
      totalRecords: overrides.totalRecords ?? 1,
      totalPages: overrides.totalPages ?? 1,
    };
  }

  function create() {
    adminService.getSystemConfigs.mockReturnValue(of([config()]));
    const fixture = TestBed.createComponent(AdminSystemComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    adminService = {
      getSystemConfigs: vi.fn(),
      updateSystemConfig: vi.fn(),
      getAuditLogs: vi.fn(),
      getNotificationLogs: vi.fn(),
      getEmailTemplates: vi.fn(),
      saveEmailTemplate: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AdminSystemComponent],
      providers: [
        { provide: AdminService, useValue: adminService },
        { provide: ToastService, useValue: toast },
        { provide: DomSanitizer, useValue: { bypassSecurityTrustHtml: (v: string) => v } },
      ],
    });
  });

  it('ngOnInit loads system configs', () => {
    const fixture = create();
    expect(fixture.componentInstance.configs()).toEqual([config()]);
  });

  describe('onTabChange (lazy tab loading)', () => {
    it('loads audit logs the first time the audit tab is opened', () => {
      const fixture = create();
      adminService.getAuditLogs.mockReturnValue(of(auditPage()));

      fixture.componentInstance.onTabChange('audit');

      expect(fixture.componentInstance.activeTab()).toBe('audit');
      expect(adminService.getAuditLogs).toHaveBeenCalledTimes(1);
      expect(fixture.componentInstance.auditLogs()).toEqual(auditPage().data);
    });

    it('does not refetch audit logs on a second visit to the tab', () => {
      const fixture = create();
      adminService.getAuditLogs.mockReturnValue(of(auditPage()));
      fixture.componentInstance.onTabChange('audit');
      fixture.componentInstance.onTabChange('configs');
      fixture.componentInstance.onTabChange('audit');
      expect(adminService.getAuditLogs).toHaveBeenCalledTimes(1);
    });

    it('loads notification logs the first time that tab is opened', () => {
      const fixture = create();
      const logs = [{ id: 'n1' } as NotificationDto];
      adminService.getNotificationLogs.mockReturnValue(of(logs));
      fixture.componentInstance.onTabChange('notifications');
      expect(fixture.componentInstance.notifLogs()).toEqual(logs);
    });

    it('loads email templates the first time that tab is opened', () => {
      const fixture = create();
      const templates = [{ id: 't1', templateKey: 'KycApproved' } as EmailTemplateDto];
      adminService.getEmailTemplates.mockReturnValue(of(templates));
      fixture.componentInstance.onTabChange('templates');
      expect(fixture.componentInstance.templates()).toEqual(templates);
    });
  });

  describe('audit log filtering and pagination', () => {
    it('applyAuditFilter resets to page 1, collapses any expanded row, and reloads with the current filters', () => {
      const fixture = create();
      adminService.getAuditLogs.mockReturnValue(of(auditPage()));
      fixture.componentInstance.auditPage.set(3);
      fixture.componentInstance.expandedLogId.set('a1');
      fixture.componentInstance.auditSearch.set('login');

      fixture.componentInstance.applyAuditFilter();

      expect(fixture.componentInstance.auditPage()).toBe(1);
      expect(fixture.componentInstance.expandedLogId()).toBeNull();
      expect(adminService.getAuditLogs).toHaveBeenCalledWith(1, 25, 'login', undefined, undefined);
    });

    it('clearAuditFilter resets all filters and reloads', () => {
      const fixture = create();
      adminService.getAuditLogs.mockReturnValue(of(auditPage()));
      fixture.componentInstance.auditSearch.set('login');
      fixture.componentInstance.auditFrom.set('2026-01-01');
      fixture.componentInstance.auditTo.set('2026-02-01');

      fixture.componentInstance.clearAuditFilter();

      expect(fixture.componentInstance.auditSearch()).toBe('');
      expect(fixture.componentInstance.auditFrom()).toBe('');
      expect(fixture.componentInstance.auditTo()).toBe('');
      expect(adminService.getAuditLogs).toHaveBeenCalledWith(1, 25, undefined, undefined, undefined);
    });

    it('auditPageChange ignores a delta that would go out of bounds', () => {
      const fixture = create();
      adminService.getAuditLogs.mockReturnValue(of(auditPage({ totalPages: 1 })));
      fixture.componentInstance.onTabChange('audit');
      adminService.getAuditLogs.mockClear();

      fixture.componentInstance.auditPageChange(1);

      expect(adminService.getAuditLogs).not.toHaveBeenCalled();
      expect(fixture.componentInstance.auditPage()).toBe(1);
    });

    it('auditPageChange moves to a valid page and reloads', () => {
      const fixture = create();
      adminService.getAuditLogs.mockReturnValue(of(auditPage({ totalPages: 3 })));
      fixture.componentInstance.onTabChange('audit');
      adminService.getAuditLogs.mockClear();

      fixture.componentInstance.auditPageChange(1);

      expect(fixture.componentInstance.auditPage()).toBe(2);
      expect(adminService.getAuditLogs).toHaveBeenCalledTimes(1);
    });

    it('auditGoToPage ignores an out-of-range page', () => {
      const fixture = create();
      adminService.getAuditLogs.mockReturnValue(of(auditPage({ totalPages: 2 })));
      fixture.componentInstance.onTabChange('audit');
      adminService.getAuditLogs.mockClear();

      fixture.componentInstance.auditGoToPage(5);

      expect(adminService.getAuditLogs).not.toHaveBeenCalled();
    });

    it('toggleLogExpand expands then collapses the same row', () => {
      const fixture = create();
      fixture.componentInstance.toggleLogExpand('a1');
      expect(fixture.componentInstance.expandedLogId()).toBe('a1');
      fixture.componentInstance.toggleLogExpand('a1');
      expect(fixture.componentInstance.expandedLogId()).toBeNull();
    });
  });

  describe('audit log presentation helpers', () => {
    it('auditActionCategory maps a known action and falls back to "system" for an unknown one', () => {
      const fixture = create();
      expect(fixture.componentInstance.auditActionCategory('UserRoleChanged')).toBe('admin');
      expect(fixture.componentInstance.auditActionCategory('SomethingMade Up')).toBe('system');
    });

    it('auditBadgeStyle returns the style for the resolved category', () => {
      const fixture = create();
      const style = fixture.componentInstance.auditBadgeStyle('ClaimIntimated');
      expect(style).toEqual({ bg: '#EFF6FF', fg: '#1D4ED8', border: '#BFDBFE' });
    });

    it('formatJson pretty-prints valid JSON and returns invalid input unchanged', () => {
      const fixture = create();
      expect(fixture.componentInstance.formatJson('{"a":1}')).toBe(JSON.stringify({ a: 1 }, null, 2));
      expect(fixture.componentInstance.formatJson('not json')).toBe('not json');
      expect(fixture.componentInstance.formatJson(undefined)).toBe('');
    });

    it('hasJsonDetail is true only when oldValue or newValue is present', () => {
      const fixture = create();
      expect(fixture.componentInstance.hasJsonDetail({ oldValue: '{}' } as AuditLogDto)).toBe(true);
      expect(fixture.componentInstance.hasJsonDetail({ newValue: '{}' } as AuditLogDto)).toBe(true);
      expect(fixture.componentInstance.hasJsonDetail({} as AuditLogDto)).toBe(false);
    });
  });

  describe('config editing', () => {
    it('startEditConfig loads the key/value into the edit fields, blocked while another save is in flight', () => {
      const fixture = create();
      fixture.componentInstance.startEditConfig(config());
      expect(fixture.componentInstance.editingKey()).toBe('MaxLoginAttempts');
      expect(fixture.componentInstance.editValue()).toBe('5');

      fixture.componentInstance.editingKey.set(null);
      fixture.componentInstance.configSavingKey.set('Other');
      fixture.componentInstance.startEditConfig(config());
      expect(fixture.componentInstance.editingKey()).toBeNull();
    });

    it('configInvalid flags blank and overlong values', () => {
      const fixture = create();
      fixture.componentInstance.editValue.set('  ');
      expect(fixture.componentInstance.configInvalid()).toBe(true);
      fixture.componentInstance.editValue.set('a'.repeat(2001));
      expect(fixture.componentInstance.configInvalid()).toBe(true);
      fixture.componentInstance.editValue.set('10');
      expect(fixture.componentInstance.configInvalid()).toBe(false);
    });

    it('saveConfig updates the local list and toasts success', () => {
      const fixture = create();
      fixture.componentInstance.editValue.set('10');
      adminService.updateSystemConfig.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.saveConfig('MaxLoginAttempts');

      expect(adminService.updateSystemConfig).toHaveBeenCalledWith({ configKey: 'MaxLoginAttempts', configValue: '10' });
      expect(fixture.componentInstance.configs()[0].configValue).toBe('10');
      expect(fixture.componentInstance.editingKey()).toBeNull();
      expect(toast.success).toHaveBeenCalled();
    });

    it('saveConfig shows an error toast and clears the saving flag on failure', () => {
      const fixture = create();
      fixture.componentInstance.editValue.set('10');
      adminService.updateSystemConfig.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.saveConfig('MaxLoginAttempts');

      expect(fixture.componentInstance.configSavingKey()).toBeNull();
      expect(toast.error).toHaveBeenCalledWith('Failed to save config');
    });

    it('saveConfig is a no-op when the value is invalid', () => {
      const fixture = create();
      fixture.componentInstance.editValue.set('');
      fixture.componentInstance.saveConfig('MaxLoginAttempts');
      expect(adminService.updateSystemConfig).not.toHaveBeenCalled();
    });
  });

  describe('email template editing and preview', () => {
    const template: EmailTemplateDto = {
      id: 't1', templateKey: 'ClaimApproved', subject: 'Hi {{firstName}}',
      bodyHtml: 'Claim {{claimNumber}} approved. Unknown: {{mystery}}', isActive: true, createdAt: '2026-01-01',
    };

    it('openEditTemplateModal loads the form and opens the edit modal', () => {
      const fixture = create();
      fixture.componentInstance.openEditTemplateModal(template);
      expect(fixture.componentInstance.activeModal()).toBe('editTemplate');
      expect(fixture.componentInstance.templateForm).toEqual({ templateKey: 'ClaimApproved', subject: 'Hi {{firstName}}', bodyHtml: template.bodyHtml });
    });

    it('openPreviewModal substitutes known dummy values and marks unknown placeholders', () => {
      const fixture = create();
      fixture.componentInstance.openPreviewModal(template);
      expect(fixture.componentInstance.activeModal()).toBe('previewTemplate');
      expect(fixture.componentInstance.previewHtml()).toContain('Claim CLM-2026-00001 approved');
      expect(fixture.componentInstance.previewHtml()).toContain('[mystery]');
    });

    it('closeModal clears the active modal unless a save is in flight', () => {
      const fixture = create();
      fixture.componentInstance.activeModal.set('editTemplate');
      fixture.componentInstance.templateSaving.set(true);
      fixture.componentInstance.closeModal();
      expect(fixture.componentInstance.activeModal()).toBe('editTemplate');

      fixture.componentInstance.templateSaving.set(false);
      fixture.componentInstance.closeModal();
      expect(fixture.componentInstance.activeModal()).toBeNull();
    });

    it('templateInvalid flags a blank subject/body or an overlong subject', () => {
      const fixture = create();
      fixture.componentInstance.templateForm = { templateKey: 'X', subject: '', bodyHtml: 'body' };
      expect(fixture.componentInstance.templateInvalid()).toBe(true);
      fixture.componentInstance.templateForm = { templateKey: 'X', subject: 'a'.repeat(201), bodyHtml: 'body' };
      expect(fixture.componentInstance.templateInvalid()).toBe(true);
      fixture.componentInstance.templateForm = { templateKey: 'X', subject: 'Subject', bodyHtml: 'body' };
      expect(fixture.componentInstance.templateInvalid()).toBe(false);
    });

    it('saveTemplate saves, closes the modal, reloads templates, and toasts success', () => {
      const fixture = create();
      fixture.componentInstance.templateForm = { templateKey: 'ClaimApproved', subject: 'Hi', bodyHtml: 'Body' };
      adminService.saveEmailTemplate.mockReturnValue(of({ message: 'ok' }));
      adminService.getEmailTemplates.mockReturnValue(of([template]));

      fixture.componentInstance.saveTemplate();

      expect(adminService.saveEmailTemplate).toHaveBeenCalledWith({ templateKey: 'ClaimApproved', subject: 'Hi', bodyHtml: 'Body' });
      expect(fixture.componentInstance.templateSaving()).toBe(false);
      expect(fixture.componentInstance.activeModal()).toBeNull();
      expect(toast.success).toHaveBeenCalled();
      expect(adminService.getEmailTemplates).toHaveBeenCalled();
    });

    it('saveTemplate shows an error toast and resets the saving flag on failure', () => {
      const fixture = create();
      fixture.componentInstance.templateForm = { templateKey: 'ClaimApproved', subject: 'Hi', bodyHtml: 'Body' };
      adminService.saveEmailTemplate.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.saveTemplate();

      expect(fixture.componentInstance.templateSaving()).toBe(false);
      expect(toast.error).toHaveBeenCalledWith('Failed to save template');
    });
  });

  describe('notification badge color helpers', () => {
    it('map known notification types and fall back for unknown ones', () => {
      const fixture = create();
      expect(fixture.componentInstance.notifTypeBg('Error')).toBe('#FBE9E9');
      expect(fixture.componentInstance.notifTypeFg('Error')).toBe('#D14343');
      expect(fixture.componentInstance.notifTypeBdr('Error')).toBe('#F5B4B4');
      expect(fixture.componentInstance.notifTypeBg('Unknown')).toBe('#F0F1F3');
    });
  });
});
