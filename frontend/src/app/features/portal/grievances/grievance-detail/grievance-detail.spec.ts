import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GrievanceDetailComponent } from './grievance-detail';
import { GrievanceService } from '../services/grievance.service';
import { GrievanceDto } from '../../../../core/models/api.models';

describe('GrievanceDetailComponent', () => {
  let grievanceService: { getById: ReturnType<typeof vi.fn> };

  function create(id: string | null = 'g1') {
    TestBed.configureTestingModule({
      imports: [GrievanceDetailComponent],
      providers: [
        { provide: GrievanceService, useValue: grievanceService },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: (key: string) => (key === 'id' ? id : null) } } } },
      ],
    });
    const fixture = TestBed.createComponent(GrievanceDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    grievanceService = { getById: vi.fn() };
  });

  it('loads the grievance by the id route param and stops loading', () => {
    const grievance = { id: 'g1', description: 'x' } as GrievanceDto;
    grievanceService.getById.mockReturnValue(of(grievance));

    const fixture = create('g1');

    expect(grievanceService.getById).toHaveBeenCalledWith('g1');
    expect(fixture.componentInstance.grievance()).toEqual(grievance);
    expect(fixture.componentInstance.loading()).toBe(false);
  });

  it('stops loading (without a grievance) when the fetch fails', () => {
    grievanceService.getById.mockReturnValue(throwError(() => ({ status: 404 })));

    const fixture = create('missing');

    expect(fixture.componentInstance.grievance()).toBeNull();
    expect(fixture.componentInstance.loading()).toBe(false);
  });

  it('falls back to an empty id when there is no route param', () => {
    grievanceService.getById.mockReturnValue(of({} as GrievanceDto));
    create(null);
    expect(grievanceService.getById).toHaveBeenCalledWith('');
  });

  describe('document preview', () => {
    it('openPreview/closePreview toggle the previewed attachment with a fixed label', () => {
      grievanceService.getById.mockReturnValue(of({} as GrievanceDto));
      const fixture = create('g1');
      fixture.componentInstance.openPreview('uploads/grievances/x.pdf');
      expect(fixture.componentInstance.previewDoc()).toEqual({ url: '/uploads/grievances/x.pdf', label: 'Supporting document' });
      fixture.componentInstance.closePreview();
      expect(fixture.componentInstance.previewDoc()).toBeNull();
    });
  });
});
