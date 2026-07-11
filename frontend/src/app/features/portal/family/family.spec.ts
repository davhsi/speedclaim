import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError, Subject } from 'rxjs';
import { FamilyComponent } from './family';
import { ProfileService } from '../profile/services/profile.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FamilyMemberDto } from '../../../core/models/api.models';

describe('FamilyComponent', () => {
  let profileService: {
    getFamilyMembers: ReturnType<typeof vi.fn>;
    addFamilyMember: ReturnType<typeof vi.fn>;
    updateFamilyMember: ReturnType<typeof vi.fn>;
    deleteFamilyMember: ReturnType<typeof vi.fn>;
  };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const member: FamilyMemberDto = {
    id: 'm1', salutation: 'Mr', firstName: 'Sam', lastName: 'Doe', fullName: 'Sam Doe',
    dateOfBirth: '2010-01-01', gender: 'Male', relationship: 'Son', isDependent: true,
  };

  function create(members: FamilyMemberDto[] = [member]) {
    profileService.getFamilyMembers.mockReturnValue(of(members));
    TestBed.configureTestingModule({
      imports: [FamilyComponent],
      providers: [
        { provide: ProfileService, useValue: profileService },
        { provide: ToastService, useValue: toast },
      ],
    });
    const fixture = TestBed.createComponent(FamilyComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    profileService = {
      getFamilyMembers: vi.fn(),
      addFamilyMember: vi.fn(),
      updateFamilyMember: vi.fn(),
      deleteFamilyMember: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn() };
  });

  it('loads family members on init', () => {
    const fixture = create();
    expect(fixture.componentInstance.members()).toEqual([member]);
  });

  describe('addMember', () => {
    it('adds the member, appends it, closes the form, and resets defaults', () => {
      const fixture = create([]);
      fixture.componentInstance.memberForm.setValue({
        firstName: 'Amy', lastName: 'Doe', dateOfBirth: '2015-01-01', relationship: 'Daughter', gender: 'Female', salutation: 'Ms',
      });
      const created = { id: 'm2', firstName: 'Amy' } as FamilyMemberDto;
      profileService.addFamilyMember.mockReturnValue(of(created));
      fixture.componentInstance.showForm.set(true);

      fixture.componentInstance.addMember();

      expect(profileService.addFamilyMember).toHaveBeenCalledWith(expect.objectContaining({ firstName: 'Amy', isDependent: true }));
      expect(fixture.componentInstance.members()).toContainEqual(created);
      expect(toast.success).toHaveBeenCalledWith('Family member added');
      expect(fixture.componentInstance.showForm()).toBe(false);
      expect(fixture.componentInstance.memberForm.controls.relationship.value).toBe('Spouse');
    });

    it('shows an error toast when adding fails', () => {
      const fixture = create([]);
      fixture.componentInstance.memberForm.setValue({
        firstName: 'Amy', lastName: 'Doe', dateOfBirth: '2015-01-01', relationship: 'Daughter', gender: 'Female', salutation: 'Ms',
      });
      profileService.addFamilyMember.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.addMember();
      expect(toast.error).toHaveBeenCalledWith('Failed to add member');
    });

    it('sets addingMember while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create([]);
      const c = fixture.componentInstance;
      c.memberForm.setValue({
        firstName: 'Amy', lastName: 'Doe', dateOfBirth: '2015-01-01', relationship: 'Daughter', gender: 'Female', salutation: 'Ms',
      });
      const subject = new Subject<any>();
      profileService.addFamilyMember.mockReturnValue(subject);

      c.addMember();
      expect(c.addingMember()).toBe(true);

      c.addMember();
      expect(profileService.addFamilyMember).toHaveBeenCalledTimes(1);

      subject.next({ id: 'm2', firstName: 'Amy' });
      subject.complete();

      expect(c.addingMember()).toBe(false);
      expect(c.showForm()).toBe(false);
    });

    it('clears addingMember on error too', () => {
      const fixture = create([]);
      const c = fixture.componentInstance;
      c.memberForm.setValue({
        firstName: 'Amy', lastName: 'Doe', dateOfBirth: '2015-01-01', relationship: 'Daughter', gender: 'Female', salutation: 'Ms',
      });
      const subject = new Subject<any>();
      profileService.addFamilyMember.mockReturnValue(subject);

      c.addMember();
      subject.error({ status: 500 });

      expect(c.addingMember()).toBe(false);
      expect(toast.error).toHaveBeenCalledWith('Failed to add member');
    });
  });

  describe('toggleAddForm', () => {
    it('toggles showForm, but is a no-op while a submission is in flight', () => {
      const fixture = create([]);
      const c = fixture.componentInstance;
      c.toggleAddForm();
      expect(c.showForm()).toBe(true);

      c.addingMember.set(true);
      c.toggleAddForm();
      expect(c.showForm()).toBe(true);
    });
  });

  describe('startEdit / cancelEdit', () => {
    it('sets the edit target and patches the edit form with the member values', () => {
      const fixture = create();
      fixture.componentInstance.startEdit(member);
      expect(fixture.componentInstance.editTarget()).toEqual(member);
      expect(fixture.componentInstance.editForm.controls.firstName.value).toBe('Sam');
      expect(fixture.componentInstance.editForm.controls.relationship.value).toBe('Son');
    });

    it('clears the edit target on cancel', () => {
      const fixture = create();
      fixture.componentInstance.startEdit(member);
      fixture.componentInstance.cancelEdit();
      expect(fixture.componentInstance.editTarget()).toBeNull();
    });
  });

  describe('saveEdit', () => {
    it('does nothing when there is no edit target', () => {
      const fixture = create();
      fixture.componentInstance.saveEdit();
      expect(profileService.updateFamilyMember).not.toHaveBeenCalled();
    });

    it('does nothing when the edit form is invalid', () => {
      const fixture = create();
      fixture.componentInstance.startEdit(member);
      fixture.componentInstance.editForm.controls.firstName.setValue('');
      fixture.componentInstance.saveEdit();
      expect(profileService.updateFamilyMember).not.toHaveBeenCalled();
    });

    it('updates the member in the list and clears the edit target on success', () => {
      const fixture = create();
      fixture.componentInstance.startEdit(member);
      fixture.componentInstance.editForm.patchValue({ firstName: 'Samuel' });
      profileService.updateFamilyMember.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.saveEdit();

      expect(profileService.updateFamilyMember).toHaveBeenCalledWith('m1', expect.objectContaining({ firstName: 'Samuel' }));
      expect(fixture.componentInstance.members()[0].firstName).toBe('Samuel');
      expect(fixture.componentInstance.members()[0].fullName).toBe('Samuel Doe');
      expect(toast.success).toHaveBeenCalledWith('Member updated');
      expect(fixture.componentInstance.editTarget()).toBeNull();
    });

    it('shows an error toast when the update fails', () => {
      const fixture = create();
      fixture.componentInstance.startEdit(member);
      profileService.updateFamilyMember.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.saveEdit();
      expect(toast.error).toHaveBeenCalledWith('Update failed');
    });

    it('sets savingEdit while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.startEdit(member);
      const subject = new Subject<any>();
      profileService.updateFamilyMember.mockReturnValue(subject);

      c.saveEdit();
      expect(c.savingEdit()).toBe(true);

      c.saveEdit();
      expect(profileService.updateFamilyMember).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(c.savingEdit()).toBe(false);
      expect(c.editTarget()).toBeNull();
    });

    it('clears savingEdit on error too', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.startEdit(member);
      const subject = new Subject<any>();
      profileService.updateFamilyMember.mockReturnValue(subject);

      c.saveEdit();
      subject.error({ status: 500 });

      expect(c.savingEdit()).toBe(false);
      expect(toast.error).toHaveBeenCalledWith('Update failed');
    });

    it('cancelEdit is a no-op while saving is in flight', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.startEdit(member);
      c.savingEdit.set(true);
      c.cancelEdit();
      expect(c.editTarget()).toEqual(member);
    });
  });

  describe('confirmDelete', () => {
    it('does nothing when there is no delete target', () => {
      const fixture = create();
      fixture.componentInstance.confirmDelete();
      expect(profileService.deleteFamilyMember).not.toHaveBeenCalled();
    });

    it('removes the member from the list on success', () => {
      const fixture = create();
      profileService.deleteFamilyMember.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.deleteTarget.set('m1');

      fixture.componentInstance.confirmDelete();

      expect(profileService.deleteFamilyMember).toHaveBeenCalledWith('m1');
      expect(fixture.componentInstance.members()).toEqual([]);
      expect(toast.success).toHaveBeenCalledWith('Member removed');
      expect(fixture.componentInstance.deleteTarget()).toBeNull();
    });

    it('shows an error toast when deletion fails', () => {
      const fixture = create();
      profileService.deleteFamilyMember.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.deleteTarget.set('m1');
      fixture.componentInstance.confirmDelete();
      expect(toast.error).toHaveBeenCalledWith('Delete failed');
    });

    it('sets deleting while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const subject = new Subject<any>();
      profileService.deleteFamilyMember.mockReturnValue(subject);
      c.deleteTarget.set('m1');

      c.confirmDelete();
      expect(c.deleting()).toBe(true);
      expect(c.deleteTarget()).toBe('m1');

      c.confirmDelete();
      expect(profileService.deleteFamilyMember).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(c.deleting()).toBe(false);
      expect(c.deleteTarget()).toBeNull();
      expect(c.members()).toEqual([]);
    });

    it('clears deleting on error too and closes the dialog', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const subject = new Subject<any>();
      profileService.deleteFamilyMember.mockReturnValue(subject);
      c.deleteTarget.set('m1');

      c.confirmDelete();
      subject.error({ status: 500 });

      expect(c.deleting()).toBe(false);
      expect(c.deleteTarget()).toBeNull();
      expect(toast.error).toHaveBeenCalledWith('Delete failed');
    });
  });

  describe('badgeColor', () => {
    it('returns a distinct color set for Spouse, Son/Daughter, Father/Mother, and an unknown relationship', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const spouse = c.badgeColor('Spouse');
      const son = c.badgeColor('Son');
      const daughter = c.badgeColor('Daughter');
      const father = c.badgeColor('Father');
      const mother = c.badgeColor('Mother');
      const other = c.badgeColor('Sibling');

      expect(son).toEqual(daughter);
      expect(father).toEqual(mother);
      expect(new Set([JSON.stringify(spouse), JSON.stringify(son), JSON.stringify(father), JSON.stringify(other)]).size).toBe(4);
    });
  });
});
