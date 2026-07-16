import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { AdminBrochuresComponent } from './admin-brochures';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

describe('AdminBrochuresComponent', () => {
  const product = { id: 'product-1', productName: 'Health Shield', uin: 'UIN-1', domain: 'Health' } as any;
  const brochure = { id: 'brochure-1', productId: 'product-1', version: '1', originalFilename: 'brochure.pdf', status: 'Ready', effectiveFrom: '2026-07-16', createdAt: '2026-07-16' } as any;
  let admin: any;

  beforeEach(() => {
    admin = { getAdminProducts: vi.fn(() => of([product])), getProductBrochures: vi.fn(() => of([brochure])), uploadProductBrochure: vi.fn(), publishProductBrochure: vi.fn(() => of({ ...brochure, status: 'Published' })), archiveProductBrochure: vi.fn(), retryProductBrochure: vi.fn() };
    TestBed.configureTestingModule({ imports: [AdminBrochuresComponent], providers: [ { provide: AdminService, useValue: admin }, { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } }, { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => 'product-1' } } } }, { provide: Router, useValue: { navigate: vi.fn() } } ] });
  });

  it('loads the requested product brochure history without touching checklists', () => {
    const fixture = TestBed.createComponent(AdminBrochuresComponent); fixture.detectChanges();
    expect(admin.getProductBrochures).toHaveBeenCalledWith('product-1');
    expect(fixture.componentInstance.brochures()).toHaveLength(1);
  });

  it('only exposes publishing for a ready brochure', () => {
    const fixture = TestBed.createComponent(AdminBrochuresComponent); fixture.detectChanges();
    fixture.componentInstance.update('publish', brochure);
    expect(admin.publishProductBrochure).toHaveBeenCalledWith('product-1', 'brochure-1');
  });
});
