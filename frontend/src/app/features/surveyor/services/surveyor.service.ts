import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ClaimDto } from '../../../core/models/api.models';

export interface SurveyorProfileDto {
  surveyorId: string;
  userId: string;
  email: string;
  fullName: string;
  phone: string;
  licenseNumber?: string | null;
  licenseExpiry?: string | null;
  specialization: string;
  surveyorType: string;
  isActive: boolean;
}

export interface SubmitSurveyReportForm {
  estimatedRepairCost: number;
  surveyDate: string;
  remarks: string;
  reportDocument: File;
  photos?: File[];
}

@Injectable({ providedIn: 'root' })
export class SurveyorService {
  private readonly http = inject(HttpClient);

  getAssignedClaims(): Observable<ClaimDto[]> {
    return this.http.get<ClaimDto[]>('/api/v1/claims/surveyor/assigned');
  }

  getProfile(): Observable<SurveyorProfileDto> {
    return this.http.get<SurveyorProfileDto>('/api/v1/users/surveyor/profile');
  }

  submitSurveyReport(claimId: string, data: SubmitSurveyReportForm): Observable<{ message: string }> {
    const formData = new FormData();
    formData.append('EstimatedRepairCost', data.estimatedRepairCost.toString());
    formData.append('SurveyDate', data.surveyDate);
    formData.append('Remarks', data.remarks);
    formData.append('ReportDocument', data.reportDocument);
    if (data.photos) {
      data.photos.forEach(p => formData.append('Photos', p));
    }
    return this.http.post<{ message: string }>(`/api/v1/claims/${claimId}/survey-report`, formData);
  }

}
