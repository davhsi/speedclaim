import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GrievanceDetailComponent } from './grievance-detail';
import { ClaimsOfficerService } from '../services/claims-officer.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthUserDto, GrievanceDto } from '../../../core/models/api.models';

describe('GrievanceDetailComponent', () => {
  let claimsService: { getGrievanceById: ReturnType<typeof vi.fn>; assignGrievance: ReturnType<typeof vi.fn>; updateGrievanceStatus: ReturnType<typeof vi.fn> };
  let authService: { currentUser: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const officer = { id: 'officer-1' } as AuthUserDto;
  const baseGrievance: GrievanceDto = {
    id: 'g1', grievanceNumber: 'GR-1', customerId: 'c1', category: 'ClaimDelay',
    description: 'delayed', status: 'Open', createdAt: '2026-01-01',
  } as GrievanceDto;

  function create(grievance: GrievanceDto = baseGrievance) {
    claimsService = {
      getGrievanceById: vi.fn(() => of(grievance)),
      assignGrievance: vi.fn(),
      updateGrievanceStatus: vi.fn(),
    };
    authService = { currentUser: vi.fn(() => officer) };
    toast = { success: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      imports: [GrievanceDetailComponent],
      providers: [
        { provide: ClaimsOfficerService, useValue: claimsService },
        { provide: AuthService, useValue: authService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'g1' }) } } },
      ],
    });
    const fixture = TestBed.createComponent(GrievanceDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  describe('ngOnInit', () => {
    it('loads the grievance and preselects its status', () => {
      const fixture = create({ ...baseGrievance, status: 'InProgress' });
      expect(fixture.componentInstance.grievance()?.status).toBe('InProgress');
      expect(fixture.componentInstance.selectedStatus).toBe('InProgress');
    });
  });

  describe('formatCategory', () => {
    it('maps known categories to display labels and passes through unknown ones', () => {
      const fixture = create();
      expect(fixture.componentInstance.formatCategory('MisSelling')).toBe('Mis-selling');
      expect(fixture.componentInstance.formatCategory('SomethingElse')).toBe('SomethingElse');
    });
  });

  describe('isTerminal', () => {
    it('is true for Resolved/Closed and false otherwise', () => {
      const fixture = create();
      expect(fixture.componentInstance.isTerminal({ ...baseGrievance, status: 'Resolved' })).toBe(true);
      expect(fixture.componentInstance.isTerminal({ ...baseGrievance, status: 'Open' })).toBe(false);
    });
  });

  describe('onAssignToSelf', () => {
    it('assigns the grievance to the current user and reloads it', () => {
      const fixture = create();
      claimsService.assignGrievance.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onAssignToSelf();

      expect(claimsService.assignGrievance).toHaveBeenCalledWith('g1', { assignedToId: 'officer-1' });
      expect(toast.success).toHaveBeenCalledWith('Grievance assigned to you');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });

    it('does nothing when the grievance is already terminal', () => {
      const fixture = create({ ...baseGrievance, status: 'Closed' });
      fixture.componentInstance.onAssignToSelf();
      expect(claimsService.assignGrievance).not.toHaveBeenCalled();
    });

    it('shows an error toast on failure', () => {
      const fixture = create();
      claimsService.assignGrievance.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.onAssignToSelf();
      expect(toast.error).toHaveBeenCalledWith('Failed to assign grievance');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });
  });

  describe('onUpdateStatus', () => {
    it('updates the status with the selected value and optional notes', () => {
      const fixture = create();
      claimsService.updateGrievanceStatus.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.selectedStatus = 'Resolved';
      fixture.componentInstance.notes = 'resolved via refund';

      fixture.componentInstance.onUpdateStatus();

      expect(claimsService.updateGrievanceStatus).toHaveBeenCalledWith('g1', { status: 'Resolved', resolutionNotes: 'resolved via refund' });
      expect(toast.success).toHaveBeenCalledWith('Grievance status updated');
    });

    it('does nothing when already terminal', () => {
      const fixture = create({ ...baseGrievance, status: 'Resolved' });
      fixture.componentInstance.onUpdateStatus();
      expect(claimsService.updateGrievanceStatus).not.toHaveBeenCalled();
    });

    it('shows an error toast on failure', () => {
      const fixture = create();
      claimsService.updateGrievanceStatus.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.onUpdateStatus();
      expect(toast.error).toHaveBeenCalledWith('Failed to update status');
    });
  });

  describe('onSaveNotes', () => {
    it('saves notes against the current status and clears the field', () => {
      const fixture = create();
      claimsService.updateGrievanceStatus.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.notes = 'called customer';

      fixture.componentInstance.onSaveNotes();

      expect(claimsService.updateGrievanceStatus).toHaveBeenCalledWith('g1', { status: 'Open', resolutionNotes: 'called customer' });
      expect(toast.success).toHaveBeenCalledWith('Notes saved');
      expect(fixture.componentInstance.notes).toBe('');
    });

    it('does nothing when notes are blank', () => {
      const fixture = create();
      fixture.componentInstance.notes = '   ';
      fixture.componentInstance.onSaveNotes();
      expect(claimsService.updateGrievanceStatus).not.toHaveBeenCalled();
    });

    it('shows an error toast on failure', () => {
      const fixture = create();
      claimsService.updateGrievanceStatus.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.notes = 'x';
      fixture.componentInstance.onSaveNotes();
      expect(toast.error).toHaveBeenCalledWith('Failed to save notes');
    });
  });
});
