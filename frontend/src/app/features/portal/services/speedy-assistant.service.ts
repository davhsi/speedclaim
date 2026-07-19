import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SpeedyAssistantResponse, SpeedyWorkspaceResponse } from '../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class SpeedyAssistantService {
  private readonly http = inject(HttpClient);

  ask(question: string): Observable<SpeedyAssistantResponse> {
    return this.http.post<SpeedyAssistantResponse>('/api/v1/assistant/messages', { question });
  }

  askWorkspace(question: string, conversationId?: string | null): Observable<SpeedyWorkspaceResponse> {
    return this.http.post<SpeedyWorkspaceResponse>('/api/v1/assistant/workspace/messages', { question, conversationId: conversationId ?? null });
  }
}
