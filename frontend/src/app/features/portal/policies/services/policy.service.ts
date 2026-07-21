import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PolicyDto, PolicyStatusHistoryDto, EndorsementDto, PolicyNomineeDto,
  RequestEndorsementRequest, UpdateNomineeRequest, ApiMessage, PremiumScheduleDto,
  PolicyAssistantAnswer, PolicyAssistantAvailability, PolicyAssistantConversation,
} from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class PolicyService {
  private readonly http = inject(HttpClient);
  private readonly api = '/api/v1/policies';

  getMyPolicies(status?: string): Observable<PolicyDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<PolicyDto[]>(`${this.api}/my`, { params });
  }

  getById(id: string): Observable<PolicyDto> {
    return this.http.get<PolicyDto>(`${this.api}/${id}`);
  }

  getHistory(id: string): Observable<PolicyStatusHistoryDto[]> {
    return this.http.get<PolicyStatusHistoryDto[]>(`${this.api}/${id}/history`);
  }

  getEndorsements(id: string): Observable<EndorsementDto[]> {
    return this.http.get<EndorsementDto[]>(`${this.api}/${id}/endorsements`);
  }

  requestEndorsement(id: string, req: RequestEndorsementRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.api}/${id}/endorsements`, req);
  }

  getNominees(id: string): Observable<PolicyNomineeDto[]> {
    return this.http.get<PolicyNomineeDto[]>(`${this.api}/${id}/nominees`);
  }

  updateNominee(nomineeId: string, req: UpdateNomineeRequest): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.api}/nominees/${nomineeId}`, req);
  }

  cancelPolicy(id: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.api}/${id}/cancel`, {});
  }

  getSchedule(id: string): Observable<PremiumScheduleDto[]> {
    return this.http.get<PremiumScheduleDto[]>(`/api/v1/payments/schedule/${id}`);
  }

  downloadCertificate(id: string): Observable<Blob> {
    return this.http.get(`${this.api}/${id}/download`, { responseType: 'blob' });
  }

  getGuideAvailability(id: string): Observable<PolicyAssistantAvailability> {
    return this.http.get<PolicyAssistantAvailability>(`${this.api}/${id}/assistant/availability`);
  }

  createGuideConversation(id: string): Observable<PolicyAssistantConversation> {
    return this.http.post<PolicyAssistantConversation>(`${this.api}/${id}/assistant/conversations`, {});
  }

  askGuide(id: string, conversationId: string, question: string): Observable<PolicyAssistantAnswer> {
    return this.http.post<PolicyAssistantAnswer>(`${this.api}/${id}/assistant/conversations/${conversationId}/messages`, { question });
  }

}
