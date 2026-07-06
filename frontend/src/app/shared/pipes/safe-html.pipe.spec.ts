import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { SafeHtmlPipe } from './safe-html.pipe';

describe('SafeHtmlPipe', () => {
  it('delegates to DomSanitizer.bypassSecurityTrustHtml with the given value', () => {
    const bypassSecurityTrustHtml = vi.fn((value: string) => `SAFE(${value})`);

    TestBed.configureTestingModule({
      providers: [{ provide: DomSanitizer, useValue: { bypassSecurityTrustHtml } }],
    });

    const pipe = TestBed.runInInjectionContext(() => new SafeHtmlPipe());
    const svg = '<svg><path d="M0 0"/></svg>';

    const result = pipe.transform(svg);

    expect(bypassSecurityTrustHtml).toHaveBeenCalledWith(svg);
    expect(result).toBe(`SAFE(${svg})`);
  });
});
