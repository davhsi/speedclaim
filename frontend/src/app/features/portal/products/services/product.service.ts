import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProductDto, DocumentRequirementDto } from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/products';

  getAll(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>(this.apiUrl);
  }

  getById(id: number): Observable<ProductDto> {
    return this.http.get<ProductDto>(`${this.apiUrl}/${id}`);
  }

  getDocumentRequirements(id: number): Observable<DocumentRequirementDto[]> {
    return this.http.get<DocumentRequirementDto[]>(`${this.apiUrl}/${id}/documents`);
  }
}
