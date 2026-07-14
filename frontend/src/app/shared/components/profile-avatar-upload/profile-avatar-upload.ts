import { Component, ElementRef, Input, ViewChild, inject, signal } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { AvatarUploadService } from '../../../core/services/avatar-upload.service';
import { ToastService } from '../toast/toast.service';

@Component({
  selector: 'app-profile-avatar-upload',
  standalone: true,
  template: `
    <div class="relative shrink-0">
      <button
        type="button"
        [class]="containerClass"
        [disabled]="uploading()"
        [attr.title]="uploading() ? 'Uploading profile picture' : 'Change profile picture'"
        (click)="avatarInput.click()"
      >
        @if (avatarUrl()) {
          <img [src]="avatarUrl()!" [alt]="alt" class="w-full h-full object-cover" />
        } @else {
          <span>{{ initials }}</span>
        }
        <span
          class="absolute inset-0 bg-black/45 text-white flex items-center justify-center opacity-0 hover:opacity-100 focus-within:opacity-100 transition-opacity"
          [class.opacity-100]="uploading()"
          aria-hidden="true"
        >
          @if (uploading()) {
            <span class="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin"></span>
          } @else {
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"/>
              <circle cx="12" cy="13" r="4"/>
            </svg>
          }
        </span>
      </button>
      <input
        #avatarInput
        type="file"
        accept="image/jpeg,image/png,image/webp"
        class="hidden"
        aria-label="Change profile picture"
        (change)="onAvatarSelected($event)"
      />
    </div>
  `,
})
export class ProfileAvatarUploadComponent {
  private readonly authService = inject(AuthService);
  private readonly avatarUpload = inject(AvatarUploadService);
  private readonly toast = inject(ToastService);

  @Input() initials = '?';
  @Input() alt = 'Profile';
  @Input() sizeClass = 'w-14 h-14';
  @Input() roundedClass = 'rounded-card';
  @Input() colorClass = 'bg-primary text-white';

  @ViewChild('avatarInput') avatarInput!: ElementRef<HTMLInputElement>;

  avatarPreview = signal<string | null>(null);
  uploading = signal(false);

  get containerClass(): string {
    return [
      this.sizeClass,
      this.roundedClass,
      this.colorClass,
      'relative overflow-hidden flex items-center justify-center text-[22px] font-bold shrink-0 border-none cursor-pointer disabled:cursor-wait',
    ].join(' ');
  }

  avatarUrl(): string | null {
    return this.avatarPreview() ?? this.authService.currentUser()?.avatarUrl ?? null;
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => this.avatarPreview.set(reader.result as string);
    reader.readAsDataURL(file);

    this.uploading.set(true);
    this.avatarUpload.uploadAvatar(file).subscribe({
      next: (res) => {
        this.uploading.set(false);
        this.avatarPreview.set(null);
        this.authService.patchCurrentUser({ avatarUrl: res.avatarUrl });
        input.value = '';
        this.toast.success('Profile picture updated');
      },
      error: () => {
        this.uploading.set(false);
        this.avatarPreview.set(null);
        input.value = '';
        this.toast.error('Upload failed');
      },
    });
  }
}
