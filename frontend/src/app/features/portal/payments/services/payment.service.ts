import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PremiumScheduleDto, PaymentRecordDto, CreatePaymentIntentRequest,
  CreatePaymentIntentResponse, SavedCardDto,
} from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private readonly api = '/api/v1/payments';

  getSchedule(policyId: number): Observable<PremiumScheduleDto[]> {
    return this.http.get<PremiumScheduleDto[]>(`${this.api}/schedule/${policyId}`);
  }

  createPaymentIntent(scheduleId: number, req: CreatePaymentIntentRequest): Observable<CreatePaymentIntentResponse> {
    return this.http.post<CreatePaymentIntentResponse>(`${this.api}/pay/${scheduleId}`, req);
  }

  getHistory(): Observable<PaymentRecordDto[]> {
    return this.http.get<PaymentRecordDto[]>(`${this.api}/history`);
  }

  getReceipt(paymentId: number): Observable<PaymentRecordDto> {
    return this.http.get<PaymentRecordDto>(`${this.api}/${paymentId}/receipt`);
  }

  getMethods(): Observable<SavedCardDto[]> {
    return this.http.get<SavedCardDto[]>(`${this.api}/methods`);
  }
}
