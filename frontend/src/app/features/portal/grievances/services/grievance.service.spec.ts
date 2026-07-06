import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GrievanceService } from './grievance.service';
import { GrievanceDto, RaiseGrievanceRequest } from '../../../../core/models/api.models';

describe('GrievanceService', () => {
  let service: GrievanceService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(GrievanceService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getMyGrievances performs a GET to /api/v1/grievances/my', () => {
    const grievances = [{ id: 'g1' }] as GrievanceDto[];
    let result: GrievanceDto[] | undefined;

    service.getMyGrievances().subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/grievances/my');
    expect(call.request.method).toBe('GET');
    call.flush(grievances);

    expect(result).toEqual(grievances);
  });

  it('getById performs a GET to /api/v1/grievances/:id', () => {
    const grievance = { id: 'g1' } as GrievanceDto;
    let result: GrievanceDto | undefined;

    service.getById('g1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/grievances/g1');
    expect(call.request.method).toBe('GET');
    call.flush(grievance);

    expect(result).toEqual(grievance);
  });

  it('raise performs a POST to /api/v1/grievances with the request body', () => {
    const req: RaiseGrievanceRequest = { category: 'PremiumIssue', description: 'Overcharged on premium' };
    const created = { id: 'g2' } as GrievanceDto;
    let result: GrievanceDto | undefined;

    service.raise(req).subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/grievances');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush(created);

    expect(result).toEqual(created);
  });

  it('uploadAttachment performs a POST to /api/v1/grievances/:id/document with a FormData body containing the file', () => {
    const file = new File(['x'], 'proof.png', { type: 'image/png' });
    let result: { filePath: string } | undefined;

    service.uploadAttachment('g1', file).subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/grievances/g1/document');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toBeInstanceOf(FormData);
    expect((call.request.body as FormData).get('file')).toBe(file);
    call.flush({ filePath: 'uploads/grievances/proof.png' });

    expect(result).toEqual({ filePath: 'uploads/grievances/proof.png' });
  });
});
