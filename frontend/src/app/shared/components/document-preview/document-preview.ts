import { Component, computed, inject, input, output } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

export interface PreviewDoc {
  /** Server-relative URL, e.g. '/uploads/claims/xxx.pdf' — extension drives image vs PDF rendering. */
  url: string;
  /** Display name shown in the lightbox header (filename or a friendly label like "Aadhaar Front"). */
  label: string;
}

const IMAGE_EXTENSIONS = new Set(['jpg', 'jpeg', 'png', 'gif', 'webp', 'avif']);

// Matches LocalStorageService's own output shape: uploads/<folder>/<guid>.<allowlisted-ext>
const SERVER_UPLOAD_PATH = /^\/uploads\/[\w-]+(?:\/[\w-]+)*\/[\w-]+\.(?:jpe?g|png|webp|pdf)$/i;

@Component({
  selector: 'app-document-preview',
  standalone: true,
  templateUrl: './document-preview.html',
})
export class DocumentPreviewComponent {
  private readonly sanitizer = inject(DomSanitizer);

  doc = input<PreviewDoc | null>(null);
  closed = output<void>();

  private readonly extension = computed(() => this.doc()?.url.split('.').pop()?.toLowerCase() ?? '');
  isImage = computed(() => IMAGE_EXTENSIONS.has(this.extension()));
  isPdf = computed(() => this.extension() === 'pdf');

  safePreviewUrl = computed<SafeResourceUrl | null>(() => {
    const doc = this.doc();
    if (!doc || !SERVER_UPLOAD_PATH.test(doc.url)) {
      return null;
    }
    return this.sanitizer.bypassSecurityTrustResourceUrl(doc.url);
  });

  close(): void {
    this.closed.emit();
  }
}
