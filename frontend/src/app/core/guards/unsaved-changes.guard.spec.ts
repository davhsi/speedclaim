import { vi } from 'vitest';
import { of } from 'rxjs';
import { unsavedChangesGuard, CanComponentDeactivate } from './unsaved-changes.guard';

describe('unsavedChangesGuard', () => {
  it('delegates to the component canDeactivate method and returns its result', () => {
    const component: CanComponentDeactivate = { canDeactivate: vi.fn(() => true) };

    const result = unsavedChangesGuard(component, {} as never, {} as never, {} as never);

    expect(component.canDeactivate).toHaveBeenCalled();
    expect(result).toBe(true);
  });

  it('passes through an observable result from the component', () => {
    const observable = of(false);
    const component: CanComponentDeactivate = { canDeactivate: vi.fn(() => observable) };

    const result = unsavedChangesGuard(component, {} as never, {} as never, {} as never);

    expect(result).toBe(observable);
  });
});
