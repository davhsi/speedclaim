import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { SystemConfigDto, AuditLogDto, NotificationDto, EmailTemplateDto } from '../../../core/models/api.models';

// Sample values substituted when previewing a template in the admin panel.
// Unknown placeholders fall back to [variableName] so the admin can see what's missing.
const TEMPLATE_DUMMIES: Record<string, Record<string, string>> = {
  EmailVerification: { verifyUrl: '#' },
  PasswordReset: { resetUrl: '#' },
  PolicyActivated: {
    firstName: 'Arjun',
    policyNumber: 'POL-2026-00001',
    product: 'SpeedCare Platinum Health',
    sumAssured: '5,00,000.00',
    premiumAmount: '12,500.00',
    frequency: 'Monthly',
    startDate: '01 Jul 2026',
    endDate: '30 Jun 2027',
    status: 'Active',
  },
  KycApproved: { firstName: 'Arjun' },
  KycRejected: { firstName: 'Arjun', rejectionReason: 'Aadhaar image is blurry and unreadable.' },
  ProposalApproved: { firstName: 'Arjun', proposalNumber: 'PRO-2026-00001' },
  ProposalRejected: { firstName: 'Arjun', proposalNumber: 'PRO-2026-00001', rejectionReason: 'Applicant does not meet the minimum health criteria for this product.' },
  ClaimApproved: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001' },
  ClaimRejected: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001', rejectionReason: 'Damage pre-dates the policy effective date.' },
  ClaimSettled: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001', payoutAmount: '45,000.00' },
  PolicyCancelled: { firstName: 'Arjun', policyNumber: 'POL-2026-00001' },
  ClaimIntimated: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001', policyNumber: 'POL-2026-00001' },
  EndorsementApproved: { firstName: 'Arjun', policyNumber: 'POL-2026-00001', endorsementType: 'SumAssuredChange' },
  EndorsementRejected: { firstName: 'Arjun', policyNumber: 'POL-2026-00001', endorsementType: 'SumAssuredChange', rejectionReason: 'Requested sum assured exceeds the maximum limit for this product.' },
  GrievanceFiled: { firstName: 'Arjun', grievanceNumber: 'GRV-2026-00001' },
  GrievanceResolved: { firstName: 'Arjun', grievanceNumber: 'GRV-2026-00001', resolutionNotes: 'We have reviewed your concern and issued a full refund of the duplicate charge.' },
  PremiumOverdue: { firstName: 'Arjun', policyNumber: 'POL-2026-00001', amount: '12,500.00', dueDate: '01 Jun 2026' },
  // tempPassword is display-only sample data for the preview, not a real credential.
  AgentWelcome: { firstName: 'Priya', agentCode: 'AGT-2026-001', email: 'priya@example.com', tempPassword: 'TempP@ss123' },
  KycSubmitted: { firstName: 'Arjun' },
  ProposalSubmitted: { firstName: 'Arjun', proposalNumber: 'PRO-2026-00001', productName: 'SpeedCare Platinum Health' },
  ProposalDocumentsPending: { firstName: 'Arjun', proposalNumber: 'PRO-2026-00001', details: 'Please upload a recent medical certificate.' },
  PremiumPaymentConfirmed: { firstName: 'Arjun', policyNumber: 'POL-2026-00001', installmentNumber: '3', amount: '12,500.00' },
  EndorsementRequested: { firstName: 'Arjun', policyNumber: 'POL-2026-00001', endorsementType: 'SumAssuredChange' },
  ClaimDocumentsPending: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001', details: 'Please upload the original hospital discharge summary.' },
  ClaimUnderReview: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001' },
  ClaimPreAuthRequested: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001', hospitalName: 'Apollo Hospitals' },
  ClaimPreAuthApproved: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001', approvedAmount: '80,000.00' },
  CommissionCredited: { firstName: 'Priya', policyNumber: 'POL-2026-00001', commissionAmount: '6,250.00' },
  PolicyIssued: { firstName: 'Arjun', proposalNumber: 'PRO-2026-00001', policyNumber: 'POL-2026-00001' },
  GrievanceEscalated: { firstName: 'Arjun', grievanceNumber: 'GRV-2026-00001' },
  ClaimWithdrawn: { firstName: 'Arjun', claimNumber: 'CLM-2026-00001' },
  ProposalWithdrawn: { firstName: 'Arjun', proposalNumber: 'PRO-2026-00001' },
};

@Component({
  selector: 'app-admin-system',
  standalone: true,
  imports: [FormsModule, DateFormatPipe],
  templateUrl: './admin-system.html',
})
export class AdminSystemComponent implements OnInit {
  private adminService = inject(AdminService);
  private toastService = inject(ToastService);
  private sanitizer = inject(DomSanitizer);

  activeTab = signal<'configs' | 'audit' | 'notifications' | 'templates'>('configs');

  configs = signal<SystemConfigDto[]>([]);
  editingKey = signal<string | null>(null);
  editValue = signal('');
  configSavingKey = signal<string | null>(null);

  auditLogs = signal<AuditLogDto[]>([]);
  auditTotal = signal(0);
  auditTotalPages = signal(0);
  auditPage = signal(1);
  auditPageSize = signal(25);
  auditSearch = signal('');
  auditFrom = signal('');
  auditTo = signal('');
  expandedLogId = signal<string | null>(null);

  notifLogs = signal<NotificationDto[]>([]);
  templates = signal<EmailTemplateDto[]>([]);

  activeModal = signal<'editTemplate' | 'previewTemplate' | null>(null);
  templateForm = { templateKey: '', subject: '', bodyHtml: '' };
  selectedTemplate = signal<EmailTemplateDto | null>(null);
  templateSaving = signal(false);

  previewHtml = signal<SafeHtml>('');

  tabs = [
    { key: 'configs' as const, label: 'Configuration' },
    { key: 'audit' as const, label: 'Audit logs' },
    { key: 'notifications' as const, label: 'Notification logs' },
    { key: 'templates' as const, label: 'Email templates' },
  ];

  ngOnInit(): void {
    this.loadConfigs();
  }

  onTabChange(tab: 'configs' | 'audit' | 'notifications' | 'templates'): void {
    this.activeTab.set(tab);
    if (tab === 'audit' && this.auditLogs().length === 0) this.loadAuditLogs();
    if (tab === 'notifications' && this.notifLogs().length === 0) this.loadNotifLogs();
    if (tab === 'templates' && this.templates().length === 0) this.loadTemplates();
  }

  private loadConfigs(): void {
    this.adminService.getSystemConfigs().subscribe({ next: c => this.configs.set(c) });
  }

  private loadAuditLogs(): void {
    this.adminService.getAuditLogs(
      this.auditPage(), this.auditPageSize(),
      this.auditSearch() || undefined,
      this.auditFrom() || undefined,
      this.auditTo() || undefined
    ).subscribe({
      next: r => {
        this.auditLogs.set(r.data);
        this.auditTotal.set(r.totalRecords);
        this.auditTotalPages.set(r.totalPages);
      }
    });
  }

  applyAuditFilter(): void {
    this.auditPage.set(1);
    this.expandedLogId.set(null);
    this.loadAuditLogs();
  }

  clearAuditFilter(): void {
    this.auditSearch.set('');
    this.auditFrom.set('');
    this.auditTo.set('');
    this.auditPage.set(1);
    this.expandedLogId.set(null);
    this.loadAuditLogs();
  }

  auditPageChange(delta: number): void {
    const next = this.auditPage() + delta;
    if (next < 1 || next > this.auditTotalPages()) return;
    this.auditPage.set(next);
    this.expandedLogId.set(null);
    this.loadAuditLogs();
  }

  auditGoToPage(page: number): void {
    if (page < 1 || page > this.auditTotalPages()) return;
    this.auditPage.set(page);
    this.expandedLogId.set(null);
    this.loadAuditLogs();
  }

  toggleLogExpand(id: string): void {
    this.expandedLogId.set(this.expandedLogId() === id ? null : id);
  }

  auditActionCategory(action: string): string {
    const map: Record<string, string> = {
      UserLoggedIn: 'security', UserLoggedOut: 'security', EmailVerified: 'security',
      PasswordReset: 'security', PasswordResetByAdmin: 'security',
      UserRoleChanged: 'admin', UserActivated: 'admin', UserDeactivated: 'admin',
      AgentActivated: 'admin', AgentDeactivated: 'admin',
      AgentBranchAssigned: 'admin', AgentLicenseUpdated: 'admin',
      AadhaarUploaded: 'kyc', PanUploaded: 'kyc', KycApproved: 'kyc', KycRejected: 'kyc',
      ClaimIntimated: 'claim', ClaimStatusChanged: 'claim',
      ClaimPayoutProcessed: 'claim', ClaimFinanciallySettled: 'claim',
      PolicyCancelled: 'policy', EndorsementRequested: 'policy',
      EndorsementApproved: 'policy', EndorsementRejected: 'policy',
      PaymentReconciled: 'financial', RefundProcessed: 'financial', CommissionApproved: 'financial',
      CustomerRegistered: 'registration', AgentRegistered: 'registration',
      GrievanceRaised: 'grievance', GrievanceAssigned: 'grievance',
      GrievanceStatusChanged: 'grievance', GrievanceResolved: 'grievance',
      ProfileUpdated: 'profile', AvatarUploaded: 'profile',
      SystemConfigCreated: 'config', SystemConfigUpdated: 'config',
      EmailTemplateCreated: 'config', EmailTemplateUpdated: 'config',
      ProductCreated: 'config', ProductActivated: 'config', ProductDeactivated: 'config',
      PremiumRatesUpdated: 'config', ProposalApproved: 'policy', ProposalRejected: 'policy',
    };
    return map[action] ?? 'system';
  }

  auditBadgeStyle(action: string): { bg: string; fg: string; border: string } {
    const styles: Record<string, { bg: string; fg: string; border: string }> = {
      security:     { bg: '#FEF0F0', fg: '#C41E3A', border: '#FCA5A5' },
      admin:        { bg: '#F3E8FF', fg: '#7C3AED', border: '#C4B5FD' },
      kyc:          { bg: '#FFFBEB', fg: '#B45309', border: '#FCD34D' },
      claim:        { bg: '#EFF6FF', fg: '#1D4ED8', border: '#BFDBFE' },
      policy:       { bg: '#F0FDF4', fg: '#15803D', border: '#86EFAC' },
      financial:    { bg: '#FFF7ED', fg: '#C2410C', border: '#FDBA74' },
      registration: { bg: '#ECFDF5', fg: '#065F46', border: '#6EE7B7' },
      grievance:    { bg: '#EEF2FF', fg: '#4338CA', border: '#A5B4FC' },
      profile:      { bg: '#F8FAFC', fg: '#475569', border: '#CBD5E1' },
      config:       { bg: '#F1F5F9', fg: '#334155', border: '#CBD5E1' },
      system:       { bg: '#F8FAFC', fg: '#94A3B8', border: '#E2E8F0' },
    };
    return styles[this.auditActionCategory(action)] ?? styles['system'];
  }

  formatJson(raw?: string): string {
    if (!raw) return '';
    try { return JSON.stringify(JSON.parse(raw), null, 2); } catch { return raw; }
  }

  hasJsonDetail(log: AuditLogDto): boolean {
    return !!(log.oldValue || log.newValue);
  }

  private loadNotifLogs(): void {
    this.adminService.getNotificationLogs().subscribe({ next: l => this.notifLogs.set(l) });
  }

  private loadTemplates(): void {
    this.adminService.getEmailTemplates().subscribe({ next: t => this.templates.set(t), error: () => {} });
  }

  startEditConfig(cfg: SystemConfigDto): void {
    if (this.configSavingKey()) return;
    this.editingKey.set(cfg.configKey);
    this.editValue.set(cfg.configValue);
  }

  cancelEditConfig(): void {
    if (this.configSavingKey()) return;
    this.editingKey.set(null);
  }

  configInvalid(): boolean {
    return !this.editValue().trim() || this.editValue().length > 2000;
  }

  saveConfig(key: string): void {
    if (this.configSavingKey() || this.configInvalid()) return;
    const value = this.editValue().trim();
    this.configSavingKey.set(key);
    this.adminService.updateSystemConfig({ configKey: key, configValue: value }).subscribe({
      next: () => {
        this.configs.update(list => list.map(c => c.configKey === key ? { ...c, configValue: value } : c));
        this.editingKey.set(null);
        this.configSavingKey.set(null);
        this.toastService.success('Config "' + key + '" saved');
      },
      error: () => {
        this.configSavingKey.set(null);
        this.toastService.error('Failed to save config');
      },
    });
  }

  notifTypeBg(type: string): string {
    const m: Record<string, string> = { Info: '#E6F4F8', Warning: '#FEF6E6', Success: '#E8F7F1', Error: '#FBE9E9' };
    return m[type] ?? '#F0F1F3';
  }

  notifTypeFg(type: string): string {
    const m: Record<string, string> = { Info: '#0F6E8C', Warning: '#D9920A', Success: '#1F9D6B', Error: '#D14343' };
    return m[type] ?? '#6B7685';
  }

  notifTypeBdr(type: string): string {
    const m: Record<string, string> = { Info: '#B3D9E6', Warning: '#FAD88A', Success: '#B2E4CE', Error: '#F5B4B4' };
    return m[type] ?? '#D1D5DB';
  }

  openEditTemplateModal(tpl: EmailTemplateDto): void {
    if (this.templateSaving()) return;
    this.selectedTemplate.set(tpl);
    this.templateForm = { templateKey: tpl.templateKey, subject: tpl.subject, bodyHtml: tpl.bodyHtml };
    this.activeModal.set('editTemplate');
  }

  openPreviewModal(tpl: EmailTemplateDto): void {
    this.selectedTemplate.set(tpl);
    const dummies = { year: new Date().getFullYear().toString(), ...(TEMPLATE_DUMMIES[tpl.templateKey] ?? {}) };
    let subject = tpl.subject;
    let body = tpl.bodyHtml;
    for (const [key, value] of Object.entries(dummies)) {
      subject = subject.replaceAll(`{{${key}}}`, value);
      body = body.replaceAll(`{{${key}}}`, value);
    }
    // Show any remaining unknown placeholders as [variableName]
    body = body.replace(/\{\{(\w+)\}\}/g, '[$1]');
    // bodyHtml is an admin-authored email template (admin-only write access); this is the
    // template's own HTML source rendering as a preview, not third-party/user-supplied markup.
    this.previewHtml.set(this.sanitizer.bypassSecurityTrustHtml(body));
    this.activeModal.set('previewTemplate');
  }

  closeModal(): void {
    if (this.templateSaving()) return;
    this.activeModal.set(null);
  }

  templateInvalid(): boolean {
    return !this.templateForm.subject.trim()
      || !this.templateForm.bodyHtml.trim()
      || this.templateForm.subject.length > 200;
  }

  saveTemplate(): void {
    if (this.templateSaving() || this.templateInvalid()) return;
    this.templateSaving.set(true);
    const request = {
      templateKey: this.templateForm.templateKey.trim(),
      subject: this.templateForm.subject.trim(),
      bodyHtml: this.templateForm.bodyHtml.trim(),
    };
    this.adminService.saveEmailTemplate(request).subscribe({
      next: () => {
        this.templateSaving.set(false);
        this.toastService.success('Template saved');
        this.closeModal();
        this.loadTemplates();
      },
      error: () => {
        this.templateSaving.set(false);
        this.toastService.error('Failed to save template');
      },
    });
  }
}
