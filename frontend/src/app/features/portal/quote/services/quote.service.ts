import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GenerateQuoteRequest, GenerateQuoteResponse } from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class QuoteService {
  private readonly http = inject(HttpClient);

  generateQuote(req: GenerateQuoteRequest): Observable<GenerateQuoteResponse> {
    return this.http.post<GenerateQuoteResponse>('/api/v1/proposals/quote', req);
  }
}
