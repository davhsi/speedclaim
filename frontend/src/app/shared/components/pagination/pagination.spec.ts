import { TestBed } from '@angular/core/testing';
import { PaginationComponent } from './pagination';

describe('PaginationComponent', () => {
  function create(currentPage: number, totalPages: number) {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('currentPage', currentPage);
    fixture.componentRef.setInput('totalPages', totalPages);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('shows every page with no ellipsis when totalPages is 7 or fewer', () => {
    const component = create(1, 7);
    expect(component.visiblePages()).toEqual([1, 2, 3, 4, 5, 6, 7]);
  });

  it('shows a single page when there is only one page', () => {
    const component = create(1, 1);
    expect(component.visiblePages()).toEqual([1]);
  });

  it('omits the leading ellipsis when near the start', () => {
    const component = create(1, 10);
    expect(component.visiblePages()).toEqual([1, 2, -1, 10]);
  });

  it('shows both ellipses with a window around the current page in the middle', () => {
    const component = create(5, 10);
    expect(component.visiblePages()).toEqual([1, -1, 4, 5, 6, -1, 10]);
  });

  it('omits the trailing ellipsis when near the end', () => {
    const component = create(10, 10);
    expect(component.visiblePages()).toEqual([1, -1, 9, 10]);
  });

  it('handles the boundary just above the no-ellipsis threshold (8 pages)', () => {
    const component = create(1, 8);
    expect(component.visiblePages()).toEqual([1, 2, -1, 8]);
  });
});
