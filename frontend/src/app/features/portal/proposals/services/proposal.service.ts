import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProposalDto, SubmitProposalRequest, GenerateQuoteRequest, GenerateQuoteResponse, ApiMessage } from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class ProposalService {
  private http = inject(HttpClient);
  private readonly api = '/api/v1/proposals';

  getMyProposals(): Observable<ProposalDto[]> {
    return this.http.get<ProposalDto[]>(`${this.api}/my`);
  }

  getById(id: string): Observable<ProposalDto> {
    return this.http.get<ProposalDto>(`${this.api}/${id}`);
  }

  generateQuote(req: GenerateQuoteRequest): Observable<GenerateQuoteResponse> {
    return this.http.post<GenerateQuoteResponse>(`${this.api}/quote`, req);
  }

  submit(req: SubmitProposalRequest): Observable<ProposalDto> {
    return this.http.post<ProposalDto>(this.api, req);
  }

  uploadDocument(proposalId: string, documentKey: string, file: File): Observable<ApiMessage> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.put<ApiMessage>(`${this.api}/${proposalId}/documents/${documentKey}`, fd);
  }

  withdraw(id: string): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}/withdraw`, {});
  }
}
