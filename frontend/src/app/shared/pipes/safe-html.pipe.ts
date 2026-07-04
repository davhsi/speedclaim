import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Pipe({
  name: 'safeHtml',
  standalone: true,
})
export class SafeHtmlPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(value: string): SafeHtml {
    // Every call site passes a hardcoded SVG/icon literal from app code (nav config,
    // domainIcon()-style maps) — never user- or server-supplied data.
    return this.sanitizer.bypassSecurityTrustHtml(value);
  }
}
