import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProfileService } from './profile.service';
import {
  UserDto, SingleAddressRequest, FamilyMemberDto, AddFamilyMemberRequest,
  UpdateFamilyMemberRequest, KycRecordDto, ApiMessage,
} from '../../../../core/models/api.models';

describe('ProfileService', () => {
  let service: ProfileService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProfileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getProfile issues a GET to /api/v1/users/profile', () => {
    const dto = { id: 'u1' } as UserDto;
    let result: UserDto | undefined;

    service.getProfile().subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/users/profile');
    expect(call.request.method).toBe('GET');
    call.flush(dto);

    expect(result).toEqual(dto);
  });

  it('updateProfile issues a PATCH to /api/v1/users/profile with the given body', () => {
    const patch: Partial<UserDto> = { firstName: 'Jane' };
    let result: ApiMessage | undefined;

    service.updateProfile(patch).subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/users/profile');
    expect(call.request.method).toBe('PATCH');
    expect(call.request.body).toEqual(patch);
    call.flush({ message: 'ok' });

    expect(result).toEqual({ message: 'ok' });
  });

  it('addAddress issues a POST to /api/v1/users/addresses with the given body', () => {
    const req = { line1: 'x' } as SingleAddressRequest;

    service.addAddress(req).subscribe();

    const call = httpMock.expectOne('/api/v1/users/addresses');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush({ message: 'ok' });
  });

  it('updateAddress issues a PATCH to /api/v1/users/addresses/:id with the given body', () => {
    const req = { line1: 'y' } as SingleAddressRequest;

    service.updateAddress('a1', req).subscribe();

    const call = httpMock.expectOne('/api/v1/users/addresses/a1');
    expect(call.request.method).toBe('PATCH');
    expect(call.request.body).toEqual(req);
    call.flush({ message: 'ok' });
  });

  it('deleteAddress issues a DELETE to /api/v1/users/addresses/:id', () => {
    service.deleteAddress('a1').subscribe();

    const call = httpMock.expectOne('/api/v1/users/addresses/a1');
    expect(call.request.method).toBe('DELETE');
    call.flush({ message: 'ok' });
  });

  it('getFamilyMembers issues a GET to /api/v1/users/family', () => {
    const members = [{ id: 'm1' } as FamilyMemberDto];
    let result: FamilyMemberDto[] | undefined;

    service.getFamilyMembers().subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/users/family');
    expect(call.request.method).toBe('GET');
    call.flush(members);

    expect(result).toEqual(members);
  });

  it('addFamilyMember issues a POST to /api/v1/users/family with the given body', () => {
    const req = { firstName: 'Sam' } as AddFamilyMemberRequest;
    const created = { id: 'm-new' } as FamilyMemberDto;
    let result: FamilyMemberDto | undefined;

    service.addFamilyMember(req).subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/users/family');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush(created);

    expect(result).toEqual(created);
  });

  it('updateFamilyMember issues a PATCH to /api/v1/users/family/:id with the given body', () => {
    const req = { firstName: 'Sam' } as UpdateFamilyMemberRequest;

    service.updateFamilyMember('m1', req).subscribe();

    const call = httpMock.expectOne('/api/v1/users/family/m1');
    expect(call.request.method).toBe('PATCH');
    expect(call.request.body).toEqual(req);
    call.flush({ message: 'ok' });
  });

  it('deleteFamilyMember issues a DELETE to /api/v1/users/family/:id', () => {
    service.deleteFamilyMember('m1').subscribe();

    const call = httpMock.expectOne('/api/v1/users/family/m1');
    expect(call.request.method).toBe('DELETE');
    call.flush({ message: 'ok' });
  });

  it('getKyc issues a GET to /api/v1/users/kyc', () => {
    const kyc = { id: 'k1' } as KycRecordDto;
    let result: KycRecordDto | undefined;

    service.getKyc().subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/users/kyc');
    expect(call.request.method).toBe('GET');
    call.flush(kyc);

    expect(result).toEqual(kyc);
  });

  it('uploadAadhaar POSTs multipart form data with frontDocument and aadhaarNumber', () => {
    const file = new File(['x'], 'aadhaar.jpg');

    service.uploadAadhaar(file, '123456789012').subscribe();

    const call = httpMock.expectOne('/api/v1/users/kyc/aadhaar');
    expect(call.request.method).toBe('POST');
    const body = call.request.body as FormData;
    expect(body).toBeInstanceOf(FormData);
    expect(body.get('frontDocument')).toBe(file);
    expect(body.get('aadhaarNumber')).toBe('123456789012');
    call.flush({ id: 'k1' } as KycRecordDto);
  });

  it('uploadPan POSTs multipart form data with frontDocument and panNumber', () => {
    const file = new File(['x'], 'pan.jpg');

    service.uploadPan(file, 'ABCDE1234F').subscribe();

    const call = httpMock.expectOne('/api/v1/users/kyc/pan');
    expect(call.request.method).toBe('POST');
    const body = call.request.body as FormData;
    expect(body).toBeInstanceOf(FormData);
    expect(body.get('frontDocument')).toBe(file);
    expect(body.get('panNumber')).toBe('ABCDE1234F');
    call.flush({ id: 'k1' } as KycRecordDto);
  });

  it('uploadAvatar POSTs multipart form data with the file under "file"', () => {
    const file = new File(['x'], 'avatar.png');
    let result: { avatarUrl: string } | undefined;

    service.uploadAvatar(file).subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/users/profile/avatar');
    expect(call.request.method).toBe('POST');
    const body = call.request.body as FormData;
    expect(body).toBeInstanceOf(FormData);
    expect(body.get('file')).toBe(file);
    call.flush({ avatarUrl: 'new.jpg' });

    expect(result).toEqual({ avatarUrl: 'new.jpg' });
  });
});
