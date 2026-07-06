import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SurveyorService, SurveyorProfileDto, SubmitSurveyReportForm } from './surveyor.service';
import { ClaimDto } from '../../../core/models/api.models';

describe('SurveyorService', () => {
  let service: SurveyorService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SurveyorService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAssignedClaims fetches the assigned claims list', () => {
    const claims = [{ id: 'c1' }] as unknown as ClaimDto[];
    let result: ClaimDto[] | undefined;

    service.getAssignedClaims().subscribe(r => (result = r));

    const call = httpMock.expectOne('/api/v1/claims/surveyor/assigned');
    expect(call.request.method).toBe('GET');
    call.flush(claims);

    expect(result).toEqual(claims);
  });

  it('getProfile fetches the surveyor profile', () => {
    const profile = { surveyorId: 's1' } as unknown as SurveyorProfileDto;
    let result: SurveyorProfileDto | undefined;

    service.getProfile().subscribe(r => (result = r));

    const call = httpMock.expectOne('/api/v1/users/surveyor/profile');
    expect(call.request.method).toBe('GET');
    call.flush(profile);

    expect(result).toEqual(profile);
  });

  describe('submitSurveyReport', () => {
    it('builds FormData with the report fields (no photos)', () => {
      const reportDocument = new File(['x'], 'report.pdf');
      const data: SubmitSurveyReportForm = {
        estimatedRepairCost: 15000,
        surveyDate: '2026-01-05',
        remarks: 'Minor damage',
        reportDocument,
      };

      service.submitSurveyReport('claim-1', data).subscribe();

      const call = httpMock.expectOne('/api/v1/claims/claim-1/survey-report');
      expect(call.request.method).toBe('POST');
      const body = call.request.body as FormData;
      expect(body.get('EstimatedRepairCost')).toBe('15000');
      expect(body.get('SurveyDate')).toBe('2026-01-05');
      expect(body.get('Remarks')).toBe('Minor damage');
      expect(body.get('ReportDocument')).toBe(reportDocument);
      expect(body.getAll('Photos')).toEqual([]);

      call.flush({ message: 'submitted' });
    });

    it('appends each photo under the Photos field when provided', () => {
      const reportDocument = new File(['x'], 'report.pdf');
      const photo1 = new File(['a'], 'photo1.jpg');
      const photo2 = new File(['b'], 'photo2.jpg');
      const data: SubmitSurveyReportForm = {
        estimatedRepairCost: 5000,
        surveyDate: '2026-01-05',
        remarks: 'Scratches',
        reportDocument,
        photos: [photo1, photo2],
      };

      service.submitSurveyReport('claim-2', data).subscribe();

      const call = httpMock.expectOne('/api/v1/claims/claim-2/survey-report');
      const body = call.request.body as FormData;
      expect(body.getAll('Photos')).toEqual([photo1, photo2]);

      call.flush({ message: 'submitted' });
    });
  });
});
