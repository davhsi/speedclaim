import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../services/admin.service';
import { ProductBrochureDto, ProductDto } from '../../../core/models/api.models';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({ selector: 'app-admin-brochures', standalone: true, imports: [FormsModule], templateUrl: './admin-brochures.html' })
export class AdminBrochuresComponent implements OnInit {
  private readonly admin = inject(AdminService); private readonly route = inject(ActivatedRoute); private readonly router = inject(Router); private readonly toast = inject(ToastService);
  @ViewChild('brochureFileInput') private fileInput?: ElementRef<HTMLInputElement>;
  products = signal<ProductDto[]>([]); brochures = signal<ProductBrochureDto[]>([]); selectedProductId = signal(''); loading = signal(true); uploading = signal(false); selectedFile = signal<File | null>(null);
  effectiveFrom = new Date().toISOString().slice(0, 10); version = '';
  ngOnInit(): void { this.admin.getAdminProducts().subscribe({ next: products => { this.products.set(products); const requested = this.route.snapshot.queryParamMap.get('productId'); this.selectProduct(requested && products.some(p => p.id === requested) ? requested : (products[0]?.id ?? '')); this.loading.set(false); }, error: () => this.loading.set(false) }); }
  selectProduct(id: string): void { this.selectedProductId.set(id); this.brochures.set([]); if (id) this.admin.getProductBrochures(id).subscribe({ next: x => this.brochures.set(x), error: () => this.toast.error('Could not load brochure versions') }); }
  onFile(event: Event): void { const file = (event.target as HTMLInputElement).files?.[0] ?? null; this.selectedFile.set(file?.type === 'application/pdf' || file?.name.toLowerCase().endsWith('.pdf') ? file : null); if (file && !this.selectedFile()) { this.resetFileInput(); this.toast.error('Select a PDF brochure.'); } }
  fileSize(bytes: number): string { return bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / (1024 * 1024)).toFixed(1)} MB`; }
  private resetFileInput(): void { if (this.fileInput) this.fileInput.nativeElement.value = ''; }
  upload(): void { const file = this.selectedFile(), product = this.selectedProductId(); if (!file || !product || this.uploading()) return; this.uploading.set(true); this.admin.uploadProductBrochure(product, file, this.effectiveFrom, this.version).subscribe({ next: x => { this.brochures.update(items => [x, ...items]); this.selectedFile.set(null); this.resetFileInput(); this.version = ''; this.uploading.set(false); this.toast.success('Brochure uploaded and indexed.'); }, error: err => { this.uploading.set(false); this.toast.error(err?.error?.message ?? 'Brochure upload failed'); } }); }
  update(action: 'publish' | 'archive' | 'retry', brochure: ProductBrochureDto): void {
    if ((action === 'publish' || action === 'archive') && !window.confirm(
      action === 'publish'
        ? `Publish brochure version ${brochure.version}? Existing policies will continue using their bound version.`
        : `Archive brochure version ${brochure.version}? Existing policy access will be preserved.`)) return;
    const product = this.selectedProductId(); const request = action === 'publish' ? this.admin.publishProductBrochure(product, brochure.id) : action === 'archive' ? this.admin.archiveProductBrochure(product, brochure.id) : this.admin.retryProductBrochure(product, brochure.id); request.subscribe({ next: updated => { this.brochures.update(items => items.map(x => x.id === updated.id ? updated : x)); this.toast.success(`Brochure ${action}d.`); }, error: err => this.toast.error(err?.error?.message ?? `Could not ${action} brochure`) }); }
  statusClass(status: string): string { return ({ Ready: 'bg-success-bg text-success', Published: 'bg-[#FFF4DE] text-[#9A6500]', Processing: 'bg-info-bg text-info', Failed: 'bg-danger-bg text-danger', Archived: 'bg-surface text-muted' } as Record<string, string>)[status] ?? 'bg-surface text-muted'; }
  back(): void { this.router.navigate(['/admin/products']); }
}
