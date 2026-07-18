import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminService } from './admin.service';

describe('AdminService', () => {
  let service: AdminService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdminService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Users', () => {
    it('getAllUsers sends page/pageSize params and returns the paged response', () => {
      const response = { items: [], total: 0 };
      service.getAllUsers(2, 50).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne(r => r.url === '/api/v1/users/all');
      expect(call.request.method).toBe('GET');
      expect(call.request.params.get('page')).toBe('2');
      expect(call.request.params.get('pageSize')).toBe('50');
      call.flush(response);
    });

    it('getAllUsers defaults to page=1, pageSize=100', () => {
      service.getAllUsers().subscribe();
      const call = httpMock.expectOne(r => r.url === '/api/v1/users/all');
      expect(call.request.params.get('page')).toBe('1');
      expect(call.request.params.get('pageSize')).toBe('100');
      call.flush({});
    });

    it('changeUserRole PUTs the role as a JSON string body with an explicit content-type', () => {
      const response = { message: 'ok' };
      service.changeUserRole('u1', 'Admin').subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/users/u1/role');
      expect(call.request.method).toBe('PUT');
      expect(call.request.body).toBe(JSON.stringify('Admin'));
      expect(call.request.headers.get('Content-Type')).toBe('application/json');
      call.flush(response);
    });

    it('toggleUserStatus PUTs an empty body with isActive as a query param', () => {
      const response = { message: 'ok' };
      service.toggleUserStatus('u1', true).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne(r => r.url === '/api/v1/users/u1/status');
      expect(call.request.method).toBe('PUT');
      expect(call.request.body).toEqual({});
      expect(call.request.params.get('isActive')).toBe('true');
      call.flush(response);
    });

    it('getAllSessions GETs the sessions list', () => {
      const response = [{ id: 's1' }];
      service.getAllSessions().subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/users/sessions');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('resetPassword POSTs the request body', () => {
      const response = { message: 'ok' };
      const req = { newPassword: 'Secret123!' };
      service.resetPassword('u1', req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/auth/admin/reset-password/u1');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });

    it('inviteUser POSTs the invite payload', () => {
      const response = { message: 'invited' };
      const req = { firstName: 'Jane', lastName: 'Doe', email: 'jane@example.com', phone: '9876543210', role: 'Agent' };
      service.inviteUser(req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/auth/admin/invite-user');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });
  });

  describe('Agents', () => {
    it('getAgentProfiles GETs all agent profiles', () => {
      const response = [{ agentId: 'a1' }];
      service.getAgentProfiles().subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/agents/all');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('getBranches GETs the branch list', () => {
      const response = [{ id: 'b1' }];
      service.getBranches().subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/agents/branches');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('createBranch POSTs the branch payload', () => {
      const req = { name: 'Mumbai HQ' } as never;
      const response = { id: 'b1', name: 'Mumbai HQ' };
      service.createBranch(req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/agents/branches');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });

    it('updateBranch PATCHes the branch payload by id', () => {
      const req = { name: 'Updated' } as never;
      const response = { id: 'b1', name: 'Updated' };
      service.updateBranch('b1', req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/agents/branches/b1');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });

    it('assignAgentToBranch PUTs an empty body to the agent/branch path', () => {
      const response = { message: 'ok' };
      service.assignAgentToBranch('a1', 'b1').subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/agents/a1/branch/b1');
      expect(call.request.method).toBe('PUT');
      expect(call.request.body).toEqual({});
      call.flush(response);
    });

    it('updateAgentLicense PATCHes the license payload', () => {
      const req = { licenseNumber: 'LIC1', licenseExpiry: '2030-01-01' } as never;
      const response = { message: 'ok' };
      service.updateAgentLicense('a1', req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/agents/a1/license');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });

    it('toggleAgentStatus PUTs an empty body with isActive as a query param', () => {
      const response = { message: 'ok' };
      service.toggleAgentStatus('a1', false).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne(r => r.url === '/api/v1/agents/a1/status');
      expect(call.request.method).toBe('PUT');
      expect(call.request.body).toEqual({});
      expect(call.request.params.get('isActive')).toBe('false');
      call.flush(response);
    });

    it('registerAgent POSTs the registration payload', () => {
      const req = { email: 'agent@example.com' } as never;
      const response = { message: 'ok' };
      service.registerAgent(req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/auth/admin/register-agent');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });
  });

  describe('Products', () => {
    it('getProducts GETs the public products list', () => {
      const response = [{ id: 'p1' }];
      service.getProducts().subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('getAdminProducts GETs the admin products list', () => {
      const response = [{ id: 'p1' }];
      service.getAdminProducts().subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/admin/products');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('createProduct POSTs the product payload', () => {
      const req = { name: 'Motor Basic' } as never;
      const response = { id: 'p1', name: 'Motor Basic' };
      service.createProduct(req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });

    it('getProductRates GETs the rates for a product', () => {
      const response = [{ id: 'r1' }];
      service.getProductRates('p1').subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products/p1/rates');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('updateProductRates PATCHes the rates wrapped in a { rates } envelope', () => {
      const rates = [{ id: 'r1' }] as never;
      const response = { message: 'ok' };
      service.updateProductRates('p1', rates).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products/p1/rates');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual({ rates });
      call.flush(response);
    });

    it('updateProduct PATCHes editable product details', () => {
      const request = { productName: 'SpeedTest Health', minAge: 18, maxAge: 65 } as never;
      const response = { id: 'p1' };
      service.updateProduct('p1', request).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products/p1');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual(request);
      call.flush(response);
    });

    it('getProductDocuments GETs the document requirements for a product', () => {
      const response = [{ documentKey: 'RC' }];
      service.getProductDocuments('p1').subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products/p1/documents');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('updateProductDocuments PATCHes requirements wrapped in a { requirements } envelope', () => {
      const requirements = [{ documentKey: 'RC' }] as never;
      const response = { message: 'ok' };
      service.updateProductDocuments('p1', requirements).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products/p1/documents');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual({ requirements });
      call.flush(response);
    });

    it('toggleProductStatus PUTs the raw boolean as the body', () => {
      const response = { message: 'ok' };
      service.toggleProductStatus('p1', true).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products/p1/status');
      expect(call.request.method).toBe('PUT');
      expect(call.request.body).toBe(true);
      call.flush(response);
    });

    it('toggleProductSaleAvailability PUTs the raw boolean as the body', () => {
      const response = { message: 'ok' };
      service.toggleProductSaleAvailability('p1', false).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/products/p1/sale-availability');
      expect(call.request.method).toBe('PUT');
      expect(call.request.body).toBe(false);
      call.flush(response);
    });
  });

  describe('System', () => {
    it('getSystemConfigs GETs the config list', () => {
      const response = [{ key: 'k1' }];
      service.getSystemConfigs().subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/system/configs');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('updateSystemConfig PATCHes the config payload', () => {
      const req = { key: 'k1', value: 'v1' } as never;
      const response = { message: 'ok' };
      service.updateSystemConfig(req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/system/configs');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });

    it('getAuditLogs sends page/pageSize and includes optional filters when provided', () => {
      const response = { items: [], total: 0 };
      service.getAuditLogs(2, 10, 'search-term', '2026-01-01', '2026-02-01').subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne(r => r.url === '/api/v1/system/audit-logs');
      expect(call.request.params.get('page')).toBe('2');
      expect(call.request.params.get('pageSize')).toBe('10');
      expect(call.request.params.get('search')).toBe('search-term');
      expect(call.request.params.get('from')).toBe('2026-01-01');
      expect(call.request.params.get('to')).toBe('2026-02-01');
      call.flush(response);
    });

    it('getAuditLogs omits optional filter params when not provided', () => {
      service.getAuditLogs().subscribe();
      const call = httpMock.expectOne(r => r.url === '/api/v1/system/audit-logs');
      expect(call.request.params.get('page')).toBe('1');
      expect(call.request.params.get('pageSize')).toBe('25');
      expect(call.request.params.has('search')).toBe(false);
      expect(call.request.params.has('from')).toBe(false);
      expect(call.request.params.has('to')).toBe(false);
      call.flush({});
    });

    it('getNotificationLogs GETs the notification log list', () => {
      const response = [{ id: 'n1' }];
      service.getNotificationLogs().subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/system/notifications-logs');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('getEmailTemplates GETs the template list', () => {
      const response = [{ templateKey: 'PasswordReset' }];
      service.getEmailTemplates().subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/system/email-templates');
      expect(call.request.method).toBe('GET');
      call.flush(response);
    });

    it('saveEmailTemplate PATCHes the template payload', () => {
      const req = { templateKey: 'PasswordReset', subject: 'Reset', htmlBody: '<p>hi</p>' } as never;
      const response = { message: 'ok' };
      service.saveEmailTemplate(req).subscribe(res => expect(res).toEqual(response));
      const call = httpMock.expectOne('/api/v1/system/email-templates');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual(req);
      call.flush(response);
    });
  });
});
