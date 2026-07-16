import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PolicyGuideComponent } from './policy-guide';
import { PolicyService } from '../services/policy.service';

describe('PolicyGuideComponent', () => {
  let policies: any;
  beforeEach(() => {
    policies = {
      getAssistantAvailability: vi.fn(() => of({ available: true, state: 'Ready', brochureVersion: '3', effectiveFrom: '2026-07-01' })),
      getAssistantConversations: vi.fn(() => of([])), createAssistantConversation: vi.fn(() => of({ id: 'c1', policyId: 'p1', brochureId: 'b1', brochureVersion: '3', createdAt: '2026-07-01', updatedAt: '2026-07-01' })),
      getAssistantConversation: vi.fn(), sendAssistantMessage: vi.fn(() => of({ requestId: 'r1', conversationId: 'c1', messageId: 'm1', answer: 'Coverage starts after the stated waiting period.', evidenceStatus: 'Grounded', brochureVersion: '3', citations: [{ index: 1, pageNumber: 6, excerpt: 'Initial waiting period.' }] })),
    };
    TestBed.configureTestingModule({ imports: [PolicyGuideComponent], providers: [{ provide: PolicyService, useValue: policies }] });
  });

  it('loads only the policy-bound availability and creator history', () => {
    const fixture = TestBed.createComponent(PolicyGuideComponent); fixture.componentRef.setInput('policyId', 'p1'); fixture.detectChanges();
    expect(policies.getAssistantAvailability).toHaveBeenCalledWith('p1');
    expect(policies.getAssistantConversations).toHaveBeenCalledWith('p1');
  });

  it('creates a conversation then sends a suggested question through .NET', () => {
    const fixture = TestBed.createComponent(PolicyGuideComponent); fixture.componentRef.setInput('policyId', 'p1'); fixture.detectChanges();
    fixture.componentInstance.ask('What is the waiting period?');
    expect(policies.createAssistantConversation).toHaveBeenCalledWith('p1');
    expect(policies.sendAssistantMessage).toHaveBeenCalledWith('p1', 'c1', 'What is the waiting period?');
  });
});
