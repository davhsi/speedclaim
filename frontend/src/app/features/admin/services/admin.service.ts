import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  UserDto, ProductDto, NotificationDto, ApiMessage, PagedResponse,
  SessionDto, SystemConfigDto, AuditLogDto, BranchDto, CreateBranchRequest,
  UpdateAgentLicenseRequest, AgentProfileDto, CreateProductRequest,
  PremiumRateDto, DocumentRequirementResponseDto, DocumentRequirementUpdateDto,
  AdminResetPasswordRequest, ManageEmailTemplateRequest, UpdateSystemConfigRequest,
  EmailTemplateDto, RegisterAgentRequest,
} from '../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);

  // ── Users ──

  getAllUsers(page = 1, pageSize = 100): Observable<PagedResponse<UserDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<UserDto>>('/api/v1/users/all', { params });
  }

  changeUserRole(userId: number, role: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/users/${userId}/role`, JSON.stringify(role), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  toggleUserStatus(userId: number, isActive: boolean): Observable<ApiMessage> {
    const params = new HttpParams().set('isActive', isActive);
    return this.http.put<ApiMessage>(`/api/v1/users/${userId}/status`, {}, { params });
  }

  getAllSessions(): Observable<SessionDto[]> {
    return this.http.get<SessionDto[]>('/api/v1/users/sessions');
  }

  resetPassword(userId: number, req: AdminResetPasswordRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/auth/admin/reset-password/${userId}`, req);
  }

  // ── Agents ──

  getAgentProfiles(): Observable<AgentProfileDto[]> {
    return this.http.get<AgentProfileDto[]>('/api/v1/agents/all');
  }

  getBranches(): Observable<BranchDto[]> {
    return this.http.get<BranchDto[]>('/api/v1/agents/branches');
  }

  createBranch(req: CreateBranchRequest): Observable<BranchDto> {
    return this.http.post<BranchDto>('/api/v1/agents/branches', req);
  }

  assignAgentToBranch(agentId: number, branchId: number): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/agents/${agentId}/branch/${branchId}`, {});
  }

  updateAgentLicense(agentId: number, req: UpdateAgentLicenseRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/agents/${agentId}/license`, req);
  }

  toggleAgentStatus(agentId: number, isActive: boolean): Observable<ApiMessage> {
    const params = new HttpParams().set('isActive', isActive);
    return this.http.put<ApiMessage>(`/api/v1/agents/${agentId}/status`, {}, { params });
  }

  registerAgent(req: RegisterAgentRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>('/api/v1/auth/admin/register-agent', req);
  }

  // ── Products ──

  getProducts(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>('/api/v1/products');
  }

  createProduct(req: CreateProductRequest): Observable<ProductDto> {
    return this.http.post<ProductDto>('/api/v1/products', req);
  }

  updateProductRates(productId: number, rates: PremiumRateDto[]): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/products/${productId}/rates`, { rates });
  }

  getProductDocuments(productId: number): Observable<DocumentRequirementResponseDto[]> {
    return this.http.get<DocumentRequirementResponseDto[]>(`/api/v1/products/${productId}/documents`);
  }

  updateProductDocuments(productId: number, requirements: DocumentRequirementUpdateDto[]): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/products/${productId}/documents`, { requirements });
  }

  toggleProductStatus(productId: number, isActive: boolean): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/products/${productId}/status`, isActive);
  }

  // ── System ──

  getSystemConfigs(): Observable<SystemConfigDto[]> {
    return this.http.get<SystemConfigDto[]>('/api/v1/system/configs');
  }

  updateSystemConfig(req: UpdateSystemConfigRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>('/api/v1/system/configs', req);
  }

  getAuditLogs(): Observable<AuditLogDto[]> {
    return this.http.get<AuditLogDto[]>('/api/v1/system/audit-logs');
  }

  getNotificationLogs(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>('/api/v1/system/notifications-logs');
  }

  getEmailTemplates(): Observable<EmailTemplateDto[]> {
    return this.http.get<EmailTemplateDto[]>('/api/v1/system/email-templates');
  }

  saveEmailTemplate(req: ManageEmailTemplateRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>('/api/v1/system/email-templates', req);
  }

}
