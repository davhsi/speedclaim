import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { QuoteService } from './quote.service';
import { GenerateQuoteRequest, GenerateQuoteResponse } from '../../../../core/models/api.models';

describe('QuoteService', () => {
  let service: QuoteService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(QuoteService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('generateQuote posts the request and returns the quote response', () => {
    const req = { productId: 'p1' } as unknown as GenerateQuoteRequest;
    const res = { premium: 1000 } as unknown as GenerateQuoteResponse;
    let result: GenerateQuoteResponse | undefined;

    service.generateQuote(req).subscribe(r => (result = r));

    const call = httpMock.expectOne('/api/v1/proposals/quote');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush(res);

    expect(result).toEqual(res);
  });
});
