import { TestBed } from '@angular/core/testing';
import { DataTableComponent } from './data-table';

describe('DataTableComponent', () => {
  function create(data: unknown[] = []) {
    const fixture = TestBed.createComponent(DataTableComponent);
    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  describe('toggleSort', () => {
    it('sorts ascending on the first click of a column', () => {
      const component = create();
      component.toggleSort('name');
      expect(component.sortKey()).toBe('name');
      expect(component.sortDir()).toBe('asc');
    });

    it('flips to descending on a second click of the same column', () => {
      const component = create();
      component.toggleSort('name');
      component.toggleSort('name');
      expect(component.sortDir()).toBe('desc');
    });

    it('flips back to ascending on a third click of the same column', () => {
      const component = create();
      component.toggleSort('name');
      component.toggleSort('name');
      component.toggleSort('name');
      expect(component.sortDir()).toBe('asc');
    });

    it('resets to ascending when switching to a different column', () => {
      const component = create();
      component.toggleSort('name');
      component.toggleSort('name'); // now desc
      component.toggleSort('age');
      expect(component.sortKey()).toBe('age');
      expect(component.sortDir()).toBe('asc');
    });
  });

  describe('sortedData', () => {
    it('returns the data unchanged when no sort key is set', () => {
      const data = [{ name: 'c' }, { name: 'a' }, { name: 'b' }];
      const component = create(data);
      expect(component.sortedData()).toEqual(data);
    });

    it('sorts ascending by the given key', () => {
      const component = create([{ name: 'c' }, { name: 'a' }, { name: 'b' }]);
      component.toggleSort('name');
      expect(component.sortedData().map(r => r.name)).toEqual(['a', 'b', 'c']);
    });

    it('sorts descending after toggling twice', () => {
      const component = create([{ name: 'c' }, { name: 'a' }, { name: 'b' }]);
      component.toggleSort('name');
      component.toggleSort('name');
      expect(component.sortedData().map(r => r.name)).toEqual(['c', 'b', 'a']);
    });

    it('always pushes null/undefined values to the end, regardless of sort direction', () => {
      const component = create([{ name: 'b' }, { name: null }, { name: 'a' }, { name: undefined }]);

      component.toggleSort('name'); // asc
      expect(component.sortedData().map(r => r.name)).toEqual(['a', 'b', null, undefined]);

      component.toggleSort('name'); // desc
      expect(component.sortedData().map(r => r.name)).toEqual(['b', 'a', null, undefined]);
    });

    it('does not mutate the original data array', () => {
      const data = [{ name: 'c' }, { name: 'a' }, { name: 'b' }];
      const component = create(data);
      component.toggleSort('name');
      component.sortedData();
      expect(data.map(r => r.name)).toEqual(['c', 'a', 'b']);
    });
  });
});
