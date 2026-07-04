import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PremiumScheduleDto, PaymentRecordDto, CreatePaymentIntentRequest,
  CreatePaymentIntentResponse, SavedCardDto,
} from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly api = '/api/v1/payments';

  getSchedule(policyId: string): Observable<PremiumScheduleDto[]> {
    return this.http.get<PremiumScheduleDto[]>(`${this.api}/schedule/${policyId}`);
  }

  createPaymentIntent(scheduleId: string, req: CreatePaymentIntentRequest): Observable<CreatePaymentIntentResponse> {
    return this.http.post<CreatePaymentIntentResponse>(
      `${this.api}/pay/${scheduleId}`,
      req,
      { headers: { 'Idempotency-Key': crypto.randomUUID() } },
    );
  }

  getHistory(): Observable<PaymentRecordDto[]> {
    return this.http.get<PaymentRecordDto[]>(`${this.api}/history`);
  }

  getReceipt(paymentId: string): Observable<PaymentRecordDto> {
    return this.http.get<PaymentRecordDto>(`${this.api}/${paymentId}/receipt`);
  }

  getMethods(): Observable<SavedCardDto[]> {
    return this.http.get<SavedCardDto[]>(`${this.api}/methods`);
  }
}
