import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { DocumentPreviewComponent, PreviewDoc } from './document-preview';

describe('DocumentPreviewComponent', () => {
  function create(doc: PreviewDoc | null = null) {
    const fixture = TestBed.createComponent(DocumentPreviewComponent);
    fixture.componentRef.setInput('doc', doc);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('detects image extensions case-insensitively', () => {
    const component = create({ url: '/uploads/kyc/x.JPG', label: 'x.JPG' });
    expect(component.isImage()).toBe(true);
    expect(component.isPdf()).toBe(false);
  });

  it('detects pdf extension', () => {
    const component = create({ url: '/uploads/claims/x.pdf', label: 'x.pdf' });
    expect(component.isPdf()).toBe(true);
    expect(component.isImage()).toBe(false);
  });

  it('treats an unrecognized extension as neither image nor pdf', () => {
    const component = create({ url: '/uploads/claims/x.docx', label: 'x.docx' });
    expect(component.isImage()).toBe(false);
    expect(component.isPdf()).toBe(false);
  });

  it('produces a sanitized preview url only when a doc is set', () => {
    const component = create(null);
    expect(component.safePreviewUrl()).toBeNull();

    const withDoc = create({ url: '/uploads/claims/x.pdf', label: 'x.pdf' });
    expect(withDoc.safePreviewUrl()).not.toBeNull();
  });

  it('emits closed when close() is called', () => {
    const component = create({ url: '/uploads/claims/x.pdf', label: 'x.pdf' });
    const emitted = vi.fn();
    component.closed.subscribe(emitted);

    component.close();

    expect(emitted).toHaveBeenCalled();
  });
});
