import { Component, computed, inject, input, output } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

export interface PreviewDoc {
  /** Server-relative URL, e.g. '/uploads/claims/xxx.pdf' — extension drives image vs PDF rendering. */
  url: string;
  /** Display name shown in the lightbox header (filename or a friendly label like "Aadhaar Front"). */
  label: string;
}

const IMAGE_EXTENSIONS = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'avif'];

@Component({
  selector: 'app-document-preview',
  standalone: true,
  templateUrl: './document-preview.html',
})
export class DocumentPreviewComponent {
  private readonly sanitizer = inject(DomSanitizer);

  doc = input<PreviewDoc | null>(null);
  closed = output<void>();

  private extension = computed(() => this.doc()?.url.split('.').pop()?.toLowerCase() ?? '');
  isImage = computed(() => IMAGE_EXTENSIONS.includes(this.extension()));
  isPdf = computed(() => this.extension() === 'pdf');

  safePreviewUrl = computed<SafeResourceUrl | null>(() => {
    const doc = this.doc();
    // url is always server-generated (LocalStorageService writes uploads/<folder>/<guid>.<ext>
    // with an allowlisted extension) — never a raw user-supplied path or URL.
    return doc ? this.sanitizer.bypassSecurityTrustResourceUrl(doc.url) : null;
  });

  close(): void {
    this.closed.emit();
  }
}
