import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  UserDto, SingleAddressRequest, FamilyMemberDto, AddFamilyMemberRequest,
  UpdateFamilyMemberRequest, KycRecordDto, ApiMessage,
} from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly api = '/api/v1/users';

  getProfile(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.api}/profile`);
  }

  updateProfile(dto: Partial<UserDto>): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.api}/profile`, dto);
  }

  addAddress(req: SingleAddressRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.api}/addresses`, req);
  }

  updateAddress(id: string, req: SingleAddressRequest): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.api}/addresses/${id}`, req);
  }

  deleteAddress(id: string): Observable<ApiMessage> {
    return this.http.delete<ApiMessage>(`${this.api}/addresses/${id}`);
  }

  getFamilyMembers(): Observable<FamilyMemberDto[]> {
    return this.http.get<FamilyMemberDto[]>(`${this.api}/family`);
  }

  addFamilyMember(req: AddFamilyMemberRequest): Observable<FamilyMemberDto> {
    return this.http.post<FamilyMemberDto>(`${this.api}/family`, req);
  }

  updateFamilyMember(id: string, req: UpdateFamilyMemberRequest): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.api}/family/${id}`, req);
  }

  deleteFamilyMember(id: string): Observable<ApiMessage> {
    return this.http.delete<ApiMessage>(`${this.api}/family/${id}`);
  }

  getKyc(): Observable<KycRecordDto> {
    return this.http.get<KycRecordDto>(`${this.api}/kyc`);
  }

  uploadAadhaar(file: File, aadhaarNumber: string): Observable<KycRecordDto> {
    const fd = new FormData();
    fd.append('document', file);
    fd.append('aadhaarNumber', aadhaarNumber);
    return this.http.post<KycRecordDto>(`${this.api}/kyc/aadhaar`, fd);
  }

  uploadPan(file: File, panNumber: string): Observable<KycRecordDto> {
    const fd = new FormData();
    fd.append('document', file);
    fd.append('panNumber', panNumber);
    return this.http.post<KycRecordDto>(`${this.api}/kyc/pan`, fd);
  }

  uploadAvatar(file: File): Observable<{ avatarUrl: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ avatarUrl: string }>(`${this.api}/profile/avatar`, fd);
  }
}
