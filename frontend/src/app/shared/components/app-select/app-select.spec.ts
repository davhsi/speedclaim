import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { AppSelectComponent } from './app-select';

describe('AppSelectComponent', () => {
  function create(options: string[] = ['Alpha', 'Beta', 'Gamma'], searchable = false) {
    const fixture = TestBed.createComponent(AppSelectComponent);
    fixture.componentInstance.options = options;
    fixture.componentInstance.searchable = searchable;
    fixture.detectChanges();
    return fixture;
  }

  it('starts closed with an empty display value', () => {
    const fixture = create();
    expect(fixture.componentInstance.isOpen()).toBe(false);
    expect(fixture.componentInstance.displayValue).toBe('');
  });

  describe('writeValue (ControlValueAccessor)', () => {
    it('sets the display value', () => {
      const fixture = create();
      fixture.componentInstance.writeValue('Beta');
      expect(fixture.componentInstance.displayValue).toBe('Beta');
    });

    it('falls back to an empty string for null/undefined', () => {
      const fixture = create();
      fixture.componentInstance.writeValue(null as unknown as string);
      expect(fixture.componentInstance.displayValue).toBe('');
    });
  });

  describe('toggle', () => {
    it('opens the dropdown and resets the search query', () => {
      const fixture = create(undefined, true);
      fixture.componentInstance.query.set('stale query');
      fixture.componentInstance.toggle();
      expect(fixture.componentInstance.isOpen()).toBe(true);
      expect(fixture.componentInstance.query()).toBe('');
    });

    it('closes the dropdown on a second call', () => {
      const fixture = create();
      fixture.componentInstance.toggle();
      fixture.componentInstance.toggle();
      expect(fixture.componentInstance.isOpen()).toBe(false);
    });

    it('does nothing when disabled', () => {
      const fixture = create();
      fixture.componentInstance.setDisabledState(true);
      fixture.componentInstance.toggle();
      expect(fixture.componentInstance.isOpen()).toBe(false);
    });

    it('calls onTouched the first time it is toggled', () => {
      const fixture = create();
      const onTouched = vi.fn();
      fixture.componentInstance.registerOnTouched(onTouched);
      fixture.componentInstance.toggle();
      expect(onTouched).toHaveBeenCalled();
    });
  });

  describe('pick', () => {
    it('sets the value, notifies onChange, and closes the dropdown', () => {
      const fixture = create();
      const onChange = vi.fn();
      fixture.componentInstance.registerOnChange(onChange);
      fixture.componentInstance.toggle();

      fixture.componentInstance.pick('Gamma');

      expect(fixture.componentInstance.displayValue).toBe('Gamma');
      expect(onChange).toHaveBeenCalledWith('Gamma');
      expect(fixture.componentInstance.isOpen()).toBe(false);
    });
  });

  describe('filtered', () => {
    it('returns all options when not searchable, ignoring the query', () => {
      const fixture = create(['Alpha', 'Beta', 'Gamma'], false);
      fixture.componentInstance.query.set('beta');
      expect(fixture.componentInstance.filtered()).toEqual(['Alpha', 'Beta', 'Gamma']);
    });

    it('returns all options when searchable but the query is blank', () => {
      const fixture = create(['Alpha', 'Beta', 'Gamma'], true);
      expect(fixture.componentInstance.filtered()).toEqual(['Alpha', 'Beta', 'Gamma']);
    });

    it('filters case-insensitively when searchable with a query', () => {
      const fixture = create(['Alpha', 'Beta', 'Gamma'], true);
      fixture.componentInstance.query.set('GA');
      expect(fixture.componentInstance.filtered()).toEqual(['Gamma']);
    });
  });

  describe('onEscape', () => {
    it('closes the dropdown', () => {
      const fixture = create();
      fixture.componentInstance.toggle();
      fixture.componentInstance.onEscape();
      expect(fixture.componentInstance.isOpen()).toBe(false);
    });
  });

  describe('onDocClick', () => {
    it('closes the dropdown when the click target is outside the component', () => {
      const fixture = create();
      fixture.componentInstance.toggle();
      const outsideNode = document.createElement('div');
      document.body.appendChild(outsideNode);

      fixture.componentInstance.onDocClick({ target: outsideNode } as unknown as MouseEvent);

      expect(fixture.componentInstance.isOpen()).toBe(false);
      outsideNode.remove();
    });

    it('leaves the dropdown open when the click target is inside the component', () => {
      const fixture = create();
      fixture.componentInstance.toggle();

      fixture.componentInstance.onDocClick({ target: fixture.nativeElement } as unknown as MouseEvent);

      expect(fixture.componentInstance.isOpen()).toBe(true);
    });
  });

  describe('setDisabledState', () => {
    it('tracks the disabled flag', () => {
      const fixture = create();
      fixture.componentInstance.setDisabledState(true);
      expect(fixture.componentInstance.isDisabled).toBe(true);
      fixture.componentInstance.setDisabledState(false);
      expect(fixture.componentInstance.isDisabled).toBe(false);
    });
  });
});
