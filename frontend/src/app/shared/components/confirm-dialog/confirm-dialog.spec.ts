import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ConfirmDialogComponent } from './confirm-dialog';

describe('ConfirmDialogComponent', () => {
  function create(overrides: Partial<{ title: string; message: string; confirmLabel: string; cancelLabel: string; variant: 'danger' | 'default'; disabled: boolean }> = {}) {
    const fixture = TestBed.createComponent(ConfirmDialogComponent);
    for (const [key, value] of Object.entries(overrides)) {
      fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders the default title, message, and button labels', () => {
    const fixture = create();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Confirm');
    expect(el.textContent).toContain('Are you sure?');
  });

  it('renders custom title, message, and labels', () => {
    const fixture = create({ title: 'Delete claim', message: 'This cannot be undone.', confirmLabel: 'Delete', cancelLabel: 'Keep' });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Delete claim');
    expect(el.textContent).toContain('This cannot be undone.');
    expect(el.textContent).toContain('Delete');
    expect(el.textContent).toContain('Keep');
  });

  it('emits confirmed when the confirm button is clicked', () => {
    const fixture = create({ confirmLabel: 'Delete' });
    const emitted = vi.fn();
    fixture.componentInstance.confirmed.subscribe(emitted);

    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
    const confirmBtn = buttons.find(b => b.textContent?.trim() === 'Delete') as HTMLButtonElement;
    confirmBtn.click();

    expect(emitted).toHaveBeenCalled();
  });

  it('emits cancelled when the cancel button is clicked', () => {
    const fixture = create({ cancelLabel: 'Keep' });
    const emitted = vi.fn();
    fixture.componentInstance.cancelled.subscribe(emitted);

    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
    const cancelBtn = buttons.find(b => b.textContent?.trim() === 'Keep') as HTMLButtonElement;
    cancelBtn.click();

    expect(emitted).toHaveBeenCalled();
  });

  it('emits cancelled when the backdrop is clicked', () => {
    const fixture = create();
    const emitted = vi.fn();
    fixture.componentInstance.cancelled.subscribe(emitted);

    const backdrop = (fixture.nativeElement as HTMLElement).querySelector('[aria-label="Close dialog"]') as HTMLButtonElement;
    backdrop.click();

    expect(emitted).toHaveBeenCalled();
  });

  it('disables both action buttons when disabled is true', () => {
    const fixture = create({ disabled: true, confirmLabel: 'Delete', cancelLabel: 'Keep' });
    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
    const confirmBtn = buttons.find(b => b.textContent?.trim() === 'Delete') as HTMLButtonElement;
    const cancelBtn = buttons.find(b => b.textContent?.trim() === 'Keep') as HTMLButtonElement;

    expect(confirmBtn.disabled).toBe(true);
    expect(cancelBtn.disabled).toBe(true);
  });
});
