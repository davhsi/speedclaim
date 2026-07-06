import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProposalService } from './proposal.service';
import { ProposalDto, SubmitProposalRequest, GenerateQuoteRequest, GenerateQuoteResponse, ApiMessage } from '../../../../core/models/api.models';

describe('ProposalService', () => {
  let service: ProposalService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProposalService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getMyProposals issues a GET to /api/v1/proposals/my', () => {
    const proposals = [{ id: 'p1' } as ProposalDto];
    let result: ProposalDto[] | undefined;

    service.getMyProposals().subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/proposals/my');
    expect(call.request.method).toBe('GET');
    call.flush(proposals);

    expect(result).toEqual(proposals);
  });

  it('getById issues a GET to /api/v1/proposals/:id', () => {
    const proposal = { id: 'p1' } as ProposalDto;
    let result: ProposalDto | undefined;

    service.getById('p1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/proposals/p1');
    expect(call.request.method).toBe('GET');
    call.flush(proposal);

    expect(result).toEqual(proposal);
  });

  it('generateQuote issues a POST to /api/v1/proposals/quote with the given body', () => {
    const req = { productId: 'prod1' } as GenerateQuoteRequest;
    const res: GenerateQuoteResponse = { premiumAmount: 1000, paymentFrequency: 'Annually', sumAssured: 100000, tenureYears: 10 };
    let result: GenerateQuoteResponse | undefined;

    service.generateQuote(req).subscribe(r => (result = r));

    const call = httpMock.expectOne('/api/v1/proposals/quote');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush(res);

    expect(result).toEqual(res);
  });

  it('submit issues a POST to /api/v1/proposals with the given body', () => {
    const req = { productId: 'prod1' } as SubmitProposalRequest;
    const created = { id: 'p-new' } as ProposalDto;
    let result: ProposalDto | undefined;

    service.submit(req).subscribe(r => (result = r));

    const call = httpMock.expectOne('/api/v1/proposals');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush(created);

    expect(result).toEqual(created);
  });

  it('uploadDocument PUTs multipart form data with the file under "file"', () => {
    const file = new File(['x'], 'doc.pdf');
    let result: ApiMessage | undefined;

    service.uploadDocument('p1', 'proofOfAddress', file).subscribe(r => (result = r));

    const call = httpMock.expectOne('/api/v1/proposals/p1/documents/proofOfAddress');
    expect(call.request.method).toBe('PUT');
    const body = call.request.body as FormData;
    expect(body).toBeInstanceOf(FormData);
    expect(body.get('file')).toBe(file);
    call.flush({ message: 'ok' });

    expect(result).toEqual({ message: 'ok' });
  });

  it('withdraw issues a PUT to /api/v1/proposals/:id/withdraw', () => {
    service.withdraw('p1').subscribe();

    const call = httpMock.expectOne('/api/v1/proposals/p1/withdraw');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({});
    call.flush(null);
  });
});
