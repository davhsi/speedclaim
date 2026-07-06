import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SurveyReportComponent } from './survey-report';
import { SurveyorService } from '../services/surveyor.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ClaimDto } from '../../../core/models/api.models';

describe('SurveyReportComponent', () => {
  let surveyorService: { getAssignedClaims: ReturnType<typeof vi.fn>; submitSurveyReport: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let storage: Storage;

  function createStorage(): Storage {
    const values = new Map<string, string>();
    return {
      get length() { return values.size; },
      clear: vi.fn(() => values.clear()),
      getItem: vi.fn((key: string) => values.get(key) ?? null),
      key: vi.fn((index: number) => Array.from(values.keys())[index] ?? null),
      removeItem: vi.fn((key: string) => values.delete(key)),
      setItem: vi.fn((key: string, value: string) => values.set(key, value)),
    };
  }

  function create(routeId = 'c1') {
    TestBed.configureTestingModule({
      imports: [SurveyReportComponent],
      providers: [
        { provide: SurveyorService, useValue: surveyorService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { params: { id: routeId } } } },
      ],
    });
    const fixture = TestBed.createComponent(SurveyReportComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    storage = createStorage();
    vi.stubGlobal('localStorage', storage);
    localStorage.clear();
    surveyorService = { getAssignedClaims: vi.fn(), submitSurveyReport: vi.fn() };
    toast = { success: vi.fn(), warning: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn() };
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('ngOnInit', () => {
    it('loads the claim matching the route id and sets it', () => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto, { id: 'c2', status: 'UnderReview' } as ClaimDto]));
      const fixture = create('c1');
      expect(fixture.componentInstance.claim()?.id).toBe('c1');
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('redirects to the claims list when no assigned claim matches the route id', () => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'other', status: 'UnderReview' } as ClaimDto]));
      create('missing');
      expect(router.navigate).toHaveBeenCalledWith(['/surveyor/claims']);
    });

    it('redirects with a warning when the matched claim is already locked (e.g. Settled)', () => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'Settled' } as ClaimDto]));
      create('c1');
      expect(toast.warning).toHaveBeenCalledWith('This claim no longer accepts survey reports.');
      expect(router.navigate).toHaveBeenCalledWith(['/surveyor/claims']);
    });

    it('redirects when the assigned-claims request fails', () => {
      surveyorService.getAssignedClaims.mockReturnValue(throwError(() => ({ status: 500 })));
      create('c1');
      expect(router.navigate).toHaveBeenCalledWith(['/surveyor/claims']);
    });
  });

  describe('localStorage draft autosave/restore', () => {
    it('persists field changes under a claim-specific key via saveDraft-triggering setters', () => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));
      const fixture = create('c1');

      fixture.componentInstance.selectDamageType('Total loss');
      fixture.componentInstance.setDriveable(false);

      const raw = localStorage.getItem('survey_draft_c1');
      expect(raw).not.toBeNull();
      const saved = JSON.parse(raw!);
      expect(saved.dmgType).toBe('Total loss');
      expect(saved.driveable).toBe(false);
    });

    it('restores a matching draft on init and shows a restored-draft toast', () => {
      localStorage.setItem('survey_draft_c1', JSON.stringify({
        dmgType: 'Partial loss', desc: 'Front bumper damage', cost: '5000',
        pav: '200000', driveable: true, workshop: 'ABC Motors', notes: 'n/a',
      }));
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));

      const fixture = create('c1');

      expect(fixture.componentInstance.dmgType()).toBe('Partial loss');
      expect(fixture.componentInstance.desc).toBe('Front bumper damage');
      expect(fixture.componentInstance.driveable()).toBe(true);
      expect(fixture.componentInstance.workshop).toBe('ABC Motors');
      expect(toast.success).toHaveBeenCalledWith('Draft restored — pick up where you left off.');
    });

    it('does not restore a draft saved under a different claim id', () => {
      localStorage.setItem('survey_draft_c2', JSON.stringify({ dmgType: 'Theft', desc: 'stolen', cost: '1000', pav: '', driveable: false, workshop: '', notes: '' }));
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));

      const fixture = create('c1');

      expect(fixture.componentInstance.dmgType()).toBe('');
      expect(fixture.componentInstance.desc).toBe('');
    });

    it('ignores a corrupted draft without throwing', () => {
      localStorage.setItem('survey_draft_c1', 'not-valid-json{{{');
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));

      expect(() => create('c1')).not.toThrow();
      expect(toast.success).not.toHaveBeenCalled();
    });

    it('clears the draft after a successful submit', () => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));
      const fixture = create('c1');
      const c = fixture.componentInstance;
      c.selectDamageType('Total loss');
      expect(localStorage.getItem('survey_draft_c1')).not.toBeNull();

      c.desc = 'Front damage';
      c.cost = '5000';
      c.setDriveable(true);
      c.photos.set([new File(['x'], 'p1.jpg', { type: 'image/jpeg' })]);
      c.reportDocument.set(new File(['x'], 'report.pdf', { type: 'application/pdf' }));
      surveyorService.submitSurveyReport.mockReturnValue(of({ message: 'ok' }));

      c.submit();

      expect(localStorage.getItem('survey_draft_c1')).toBeNull();
    });
  });

  describe('onPhotoChange / onDocChange validation', () => {
    function fileInputEvent(files: File[]): Event {
      const input = document.createElement('input');
      Object.defineProperty(input, 'files', { value: files });
      return { target: input } as unknown as Event;
    }

    beforeEach(() => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));
    });

    it('accepts a valid JPEG/PNG photo', () => {
      const fixture = create('c1');
      const file = new File(['x'], 'a.jpg', { type: 'image/jpeg' });
      fixture.componentInstance.onPhotoChange(fileInputEvent([file]));
      expect(fixture.componentInstance.photos()).toEqual([file]);
      expect(fixture.componentInstance.photoErr()).toBe('');
    });

    it('rejects a photo with a disallowed MIME type', () => {
      const fixture = create('c1');
      const file = new File(['x'], 'a.gif', { type: 'image/gif' });
      fixture.componentInstance.onPhotoChange(fileInputEvent([file]));
      expect(fixture.componentInstance.photos()).toEqual([]);
      expect(fixture.componentInstance.photoErr()).toBe('Only JPG or PNG photos up to 5 MB are allowed.');
    });

    it('rejects an oversized photo', () => {
      const fixture = create('c1');
      const file = new File(['x'], 'a.jpg', { type: 'image/jpeg' });
      Object.defineProperty(file, 'size', { value: 6 * 1024 * 1024 });
      fixture.componentInstance.onPhotoChange(fileInputEvent([file]));
      expect(fixture.componentInstance.photos()).toEqual([]);
    });

    it('caps accumulated photos at 10', () => {
      const fixture = create('c1');
      const batch1 = Array.from({ length: 6 }, (_, i) => new File(['x'], `p${i}.jpg`, { type: 'image/jpeg' }));
      const batch2 = Array.from({ length: 6 }, (_, i) => new File(['x'], `q${i}.jpg`, { type: 'image/jpeg' }));
      fixture.componentInstance.onPhotoChange(fileInputEvent(batch1));
      fixture.componentInstance.onPhotoChange(fileInputEvent(batch2));
      expect(fixture.componentInstance.photos()).toHaveLength(10);
    });

    it('removePhoto removes by index', () => {
      const fixture = create('c1');
      const files = [new File(['x'], 'a.jpg', { type: 'image/jpeg' }), new File(['x'], 'b.jpg', { type: 'image/jpeg' })];
      fixture.componentInstance.onPhotoChange(fileInputEvent(files));
      fixture.componentInstance.removePhoto(0);
      expect(fixture.componentInstance.photos()).toEqual([files[1]]);
    });

    it('accepts a valid PDF report document', () => {
      const fixture = create('c1');
      const file = new File(['x'], 'report.pdf', { type: 'application/pdf' });
      fixture.componentInstance.onDocChange(fileInputEvent([file]));
      expect(fixture.componentInstance.reportDocument()).toBe(file);
    });

    it('rejects a report document with a disallowed type', () => {
      const fixture = create('c1');
      const file = new File(['x'], 'report.docx', { type: 'application/msword' });
      fixture.componentInstance.onDocChange(fileInputEvent([file]));
      expect(fixture.componentInstance.reportDocument()).toBeNull();
      expect(fixture.componentInstance.reportDocumentErr()).toBe('Only PDF, JPG, or PNG files up to 5 MB are allowed.');
    });

    it('removeDoc clears the selected document', () => {
      const fixture = create('c1');
      fixture.componentInstance.onDocChange(fileInputEvent([new File(['x'], 'report.pdf', { type: 'application/pdf' })]));
      fixture.componentInstance.removeDoc();
      expect(fixture.componentInstance.reportDocument()).toBeNull();
    });
  });

  describe('validate', () => {
    beforeEach(() => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));
    });

    it('fails on a completely empty form and sets every error message', () => {
      const fixture = create('c1');
      expect(fixture.componentInstance.validate()).toBe(false);
      expect(fixture.componentInstance.dmgTypeErr()).not.toBe('');
      expect(fixture.componentInstance.descErr()).not.toBe('');
      expect(fixture.componentInstance.costErr()).not.toBe('');
      expect(fixture.componentInstance.driveableErr()).not.toBe('');
      expect(fixture.componentInstance.photoErr()).not.toBe('');
      expect(fixture.componentInstance.reportDocumentErr()).not.toBe('');
    });

    it('passes when every required field is filled', () => {
      const fixture = create('c1');
      const c = fixture.componentInstance;
      c.selectDamageType('Total loss');
      c.desc = 'Front damage';
      c.cost = '5000';
      c.setDriveable(true);
      c.photos.set([new File(['x'], 'p.jpg', { type: 'image/jpeg' })]);
      c.reportDocument.set(new File(['x'], 'r.pdf', { type: 'application/pdf' }));

      expect(c.validate()).toBe(true);
    });

    it('rejects a zero or negative cost', () => {
      const fixture = create('c1');
      fixture.componentInstance.cost = '0';
      fixture.componentInstance.validate();
      expect(fixture.componentInstance.costErr()).toBe('Please enter a valid estimated repair cost.');
    });
  });

  describe('submit', () => {
    beforeEach(() => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));
    });

    function fillValidForm(c: SurveyReportComponent) {
      c.selectDamageType('Total loss');
      c.desc = 'Front damage';
      c.cost = '5000';
      c.setDriveable(true);
      c.photos.set([new File(['x'], 'p.jpg', { type: 'image/jpeg' })]);
      c.reportDocument.set(new File(['x'], 'r.pdf', { type: 'application/pdf' }));
    }

    it('shows a warning and does not call the service when the form is invalid', () => {
      const fixture = create('c1');
      fixture.componentInstance.submit();
      expect(toast.warning).toHaveBeenCalledWith('Please fill in all required fields.');
      expect(surveyorService.submitSurveyReport).not.toHaveBeenCalled();
    });

    it('submits with the mapped payload on a valid form and shows success', () => {
      const fixture = create('c1');
      fillValidForm(fixture.componentInstance);
      surveyorService.submitSurveyReport.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.submit();

      expect(surveyorService.submitSurveyReport).toHaveBeenCalledWith('c1', expect.objectContaining({
        estimatedRepairCost: 5000,
        remarks: expect.stringContaining('Damage type: Total loss'),
      }));
      expect(fixture.componentInstance.showSuccess()).toBe(true);
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('shows an error toast when submission fails', () => {
      const fixture = create('c1');
      fillValidForm(fixture.componentInstance);
      surveyorService.submitSurveyReport.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.submit();

      expect(toast.error).toHaveBeenCalledWith('Failed to submit report. Please try again.');
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('is a no-op while already submitting', () => {
      const fixture = create('c1');
      fillValidForm(fixture.componentInstance);
      fixture.componentInstance.submitting.set(true);

      fixture.componentInstance.submit();

      expect(surveyorService.submitSurveyReport).not.toHaveBeenCalled();
    });
  });

  describe('formatINR', () => {
    it('formats with Indian digit grouping', () => {
      surveyorService.getAssignedClaims.mockReturnValue(of([{ id: 'c1', status: 'UnderReview' } as ClaimDto]));
      const fixture = create('c1');
      expect(fixture.componentInstance.formatINR(1234567)).toBe('₹12,34,567.00');
    });
  });
});
