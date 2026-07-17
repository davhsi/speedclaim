import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SpeedyAssistantResponse } from '../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class SpeedyAssistantService {
  private readonly http = inject(HttpClient);

  ask(question: string): Observable<SpeedyAssistantResponse> {
    return this.http.post<SpeedyAssistantResponse>('/api/v1/assistant/messages', { question });
  }
}
