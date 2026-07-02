import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ClaimDto, ClaimStatusHistoryDto, IntimateClaimRequest, ApiMessage } from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class ClaimService {
  private http = inject(HttpClient);
  private readonly api = '/api/v1/claims';

  getMyClaims(status?: string, type?: string): Observable<ClaimDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    if (type) params = params.set('type', type);
    return this.http.get<ClaimDto[]>(`${this.api}/my`, { params });
  }

  getById(id: string): Observable<ClaimDto> {
    return this.http.get<ClaimDto>(`${this.api}/${id}`);
  }

  getHistory(id: string): Observable<ClaimStatusHistoryDto[]> {
    return this.http.get<ClaimStatusHistoryDto[]>(`${this.api}/${id}/history`);
  }

  intimate(req: IntimateClaimRequest): Observable<ClaimDto> {
    return this.http.post<ClaimDto>(`${this.api}/intimate`, req);
  }

  uploadDocument(claimId: string, documentKey: string, file: File): Observable<ApiMessage> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.put<ApiMessage>(`${this.api}/${claimId}/documents/${documentKey}`, fd);
  }

  withdraw(id: string): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}/withdraw`, {});
  }
}
