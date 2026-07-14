import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AvatarUploadService {
  private readonly http = inject(HttpClient);
  private readonly api = '/api/v1/users';

  uploadAvatar(file: File): Observable<{ avatarUrl: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ avatarUrl: string }>(`${this.api}/profile/avatar`, fd);
  }
}
