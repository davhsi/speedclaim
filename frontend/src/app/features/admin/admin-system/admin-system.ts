import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { SystemConfigDto, AuditLogDto, NotificationDto, EmailTemplateDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-admin-system',
  standalone: true,
  imports: [FormsModule, DateFormatPipe],
  templateUrl: './admin-system.html',
})
export class AdminSystemComponent implements OnInit {
  private adminService = inject(AdminService);
  private toastService = inject(ToastService);

  activeTab = signal<'configs' | 'audit' | 'notifications' | 'templates'>('configs');

  configs = signal<SystemConfigDto[]>([]);
  editingKey = signal<string | null>(null);
  editValue = signal('');

  auditLogs = signal<AuditLogDto[]>([]);
  notifLogs = signal<NotificationDto[]>([]);
  templates = signal<EmailTemplateDto[]>([]);

  activeModal = signal<'editTemplate' | null>(null);
  templateForm = { templateKey: '', subject: '', bodyHtml: '' };
  selectedTemplate = signal<EmailTemplateDto | null>(null);

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
    this.adminService.getAuditLogs().subscribe({ next: l => this.auditLogs.set(l) });
  }

  private loadNotifLogs(): void {
    this.adminService.getNotificationLogs().subscribe({ next: l => this.notifLogs.set(l) });
  }

  private loadTemplates(): void {
    this.adminService.getEmailTemplates().subscribe({ next: t => this.templates.set(t), error: () => {} });
  }

  startEditConfig(cfg: SystemConfigDto): void {
    this.editingKey.set(cfg.configKey);
    this.editValue.set(cfg.configValue);
  }

  cancelEditConfig(): void {
    this.editingKey.set(null);
  }

  saveConfig(key: string): void {
    this.adminService.updateSystemConfig({ configKey: key, configValue: this.editValue() }).subscribe({
      next: () => {
        this.configs.update(list => list.map(c => c.configKey === key ? { ...c, configValue: this.editValue() } : c));
        this.editingKey.set(null);
        this.toastService.success('Config "' + key + '" saved');
      },
      error: () => this.toastService.error('Failed to save config'),
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
    this.selectedTemplate.set(tpl);
    this.templateForm = { templateKey: tpl.templateKey, subject: tpl.subject, bodyHtml: tpl.bodyHtml };
    this.activeModal.set('editTemplate');
  }

  closeModal(): void {
    this.activeModal.set(null);
  }

  saveTemplate(): void {
    this.adminService.saveEmailTemplate(this.templateForm).subscribe({
      next: () => { this.toastService.success('Template saved'); this.closeModal(); this.loadTemplates(); },
      error: () => this.toastService.error('Failed to save template'),
    });
  }
}
