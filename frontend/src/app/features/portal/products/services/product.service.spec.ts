import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductService } from './product.service';
import { ProductDto, DocumentRequirementDto } from '../../../../core/models/api.models';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAll issues a GET to /api/v1/products and returns the list', () => {
    const products = [{ id: 'p1' } as ProductDto];
    let result: ProductDto[] | undefined;

    service.getAll().subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/products');
    expect(call.request.method).toBe('GET');
    call.flush(products);

    expect(result).toEqual(products);
  });

  it('getById issues a GET to /api/v1/products/:id and returns the product', () => {
    const product = { id: 'p1' } as ProductDto;
    let result: ProductDto | undefined;

    service.getById('p1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/products/p1');
    expect(call.request.method).toBe('GET');
    call.flush(product);

    expect(result).toEqual(product);
  });

  it('getDocumentRequirements issues a GET to /api/v1/products/:id/documents and returns the list', () => {
    const docs = [{ id: 'd1' } as DocumentRequirementDto];
    let result: DocumentRequirementDto[] | undefined;

    service.getDocumentRequirements('p1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/products/p1/documents');
    expect(call.request.method).toBe('GET');
    call.flush(docs);

    expect(result).toEqual(docs);
  });
});
