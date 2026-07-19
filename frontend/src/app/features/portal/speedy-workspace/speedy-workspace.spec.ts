import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../../../core/services/auth.service';
import { ProfileService } from '../profile/services/profile.service';
import { SpeedyAssistantService } from '../services/speedy-assistant.service';
import { SpeedyWorkspaceComponent } from './speedy-workspace';

describe('SpeedyWorkspaceComponent', () => {
  const speedy = {
    askWorkspace: vi.fn(() => of({
      requestId: 'request-1', answer: 'Open your KYC checklist.', intent: 'kyc', risk: 'regulated',
      actions: [{ kind: 'guided_kyc', label: 'Complete KYC', route: null, detail: 'Attach both documents.', requiresConfirmation: true }],
    })),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SpeedyWorkspaceComponent],
      providers: [
        provideRouter([]),
        { provide: SpeedyAssistantService, useValue: speedy },
        { provide: ProfileService, useValue: { uploadAadhaar: vi.fn(), uploadPan: vi.fn() } },
        { provide: AuthService, useValue: { currentUser: signal(null) } },
      ],
    });
  });

  it('renders the typed action returned by the supervised workspace endpoint', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.ask('Help me complete KYC');

    expect(speedy.askWorkspace).toHaveBeenCalledWith('Help me complete KYC', null);
    expect(fixture.componentInstance.messages()[1].actions?.[0].kind).toBe('guided_kyc');
  });

  it('opens the labelled KYC composer rather than navigating to an untrusted route', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.runAction({ kind: 'guided_kyc', label: 'Complete KYC', route: null, detail: 'Attach both documents.', requiresConfirmation: true });

    expect(fixture.componentInstance.showKyc()).toBe(true);
  });

  it('renders Speedy Markdown while escaping unsafe HTML', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);

    expect(fixture.componentInstance.renderMarkdown('**Health cover**\n\n1. Compare products\n- Check waiting period\n<script>alert(1)</script>'))
      .toBe('<p><strong>Health cover</strong></p><ol><li>Compare products</li></ol><ul><li>Check waiting period</li></ul><p>&lt;script&gt;alert(1)&lt;/script&gt;</p>');
  });
});
