import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PolicyDto, PolicyStatusHistoryDto, EndorsementDto, PolicyNomineeDto,
  RequestEndorsementRequest, UpdateNomineeRequest, ApiMessage, PremiumScheduleDto,
  PolicyAssistantAvailabilityDto, PolicyAssistantConversationDto, PolicyAssistantAnswerDto,
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

  getAssistantAvailability(id: string): Observable<PolicyAssistantAvailabilityDto> { return this.http.get<PolicyAssistantAvailabilityDto>(`${this.api}/${id}/assistant/availability`); }
  getAssistantConversations(id: string): Observable<PolicyAssistantConversationDto[]> { return this.http.get<PolicyAssistantConversationDto[]>(`${this.api}/${id}/assistant/conversations`); }
  createAssistantConversation(id: string): Observable<PolicyAssistantConversationDto> { return this.http.post<PolicyAssistantConversationDto>(`${this.api}/${id}/assistant/conversations`, {}); }
  getAssistantConversation(id: string, conversationId: string): Observable<PolicyAssistantConversationDto> { return this.http.get<PolicyAssistantConversationDto>(`${this.api}/${id}/assistant/conversations/${conversationId}`); }
  sendAssistantMessage(id: string, conversationId: string, question: string): Observable<PolicyAssistantAnswerDto> { return this.http.post<PolicyAssistantAnswerDto>(`${this.api}/${id}/assistant/conversations/${conversationId}/messages`, { question }); }
}
