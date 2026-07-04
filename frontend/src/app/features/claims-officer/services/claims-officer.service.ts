import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ClaimDto, ClaimStatusHistoryDto, GrievanceDto,
  PagedResponse, ApiMessage,
} from '../../../core/models/api.models';
import { ClaimStatus, ClaimType, GrievanceStatus } from '../../../core/models/enums';

export interface UpdateClaimStatusRequest {
  status: ClaimStatus;
  remarks: string;
  approvedAmount?: number;
}

export interface ApproveRejectRequest {
  isApproved: boolean;
  approvedAmount?: number;
  reason: string;
}

export interface AssignSurveyorRequest {
  surveyorId: string;
  notes?: string;
}

export interface AssignGrievanceRequest {
  assignedToId: string;
}

export interface UpdateGrievanceStatusRequest {
  status: GrievanceStatus;
  resolutionNotes?: string;
}

export interface SurveyorDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
}

@Injectable({ providedIn: 'root' })
export class ClaimsOfficerService {
  private readonly http = inject(HttpClient);

  getAllClaims(page = 1, pageSize = 20, status?: ClaimStatus, type?: ClaimType): Observable<PagedResponse<ClaimDto>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (type) params = params.set('type', type);
    return this.http.get<PagedResponse<ClaimDto>>('/api/v1/claims/all', { params });
  }

  getClaimById(id: string): Observable<ClaimDto> {
    return this.http.get<ClaimDto>(`/api/v1/claims/${id}`);
  }

  getClaimHistory(id: string): Observable<ClaimStatusHistoryDto[]> {
    return this.http.get<ClaimStatusHistoryDto[]>(`/api/v1/claims/${id}/history`);
  }

  assignToSelf(id: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/claims/${id}/assign`, {});
  }

  approveReject(id: string, request: ApproveRejectRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/claims/${id}/approve`, request);
  }

  settleClaim(id: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/claims/${id}/settle`, {});
  }

  updateStatus(id: string, request: UpdateClaimStatusRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/claims/${id}/status`, request);
  }

  assignSurveyor(id: string, request: AssignSurveyorRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/claims/${id}/assign-surveyor`, request);
  }

  requestDocs(id: string, details: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/claims/${id}/request-docs`, JSON.stringify(details), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  approvePreAuth(id: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/claims/${id}/approve-preauth`, {});
  }

  getAllGrievances(page = 1, pageSize = 20): Observable<PagedResponse<GrievanceDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<GrievanceDto>>('/api/v1/grievances/all', { params });
  }

  getGrievanceById(id: string): Observable<GrievanceDto> {
    return this.http.get<GrievanceDto>(`/api/v1/grievances/${id}`);
  }

  assignGrievance(id: string, request: AssignGrievanceRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/grievances/${id}/assign`, request);
  }

  updateGrievanceStatus(id: string, request: UpdateGrievanceStatusRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/grievances/${id}/status`, request);
  }

  getSurveyors(): Observable<SurveyorDto[]> {
    return this.http.get<SurveyorDto[]>('/api/v1/users/surveyors');
  }
}
