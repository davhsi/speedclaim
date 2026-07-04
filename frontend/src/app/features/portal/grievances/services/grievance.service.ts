import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GrievanceDto, RaiseGrievanceRequest } from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class GrievanceService {
  private readonly http = inject(HttpClient);
  private readonly api = '/api/v1/grievances';

  getMyGrievances(): Observable<GrievanceDto[]> {
    return this.http.get<GrievanceDto[]>(`${this.api}/my`);
  }

  getById(id: string): Observable<GrievanceDto> {
    return this.http.get<GrievanceDto>(`${this.api}/${id}`);
  }

  raise(req: RaiseGrievanceRequest): Observable<GrievanceDto> {
    return this.http.post<GrievanceDto>(this.api, req);
  }

  uploadAttachment(id: string, file: File): Observable<{ filePath: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ filePath: string }>(`${this.api}/${id}/document`, fd);
  }
}
