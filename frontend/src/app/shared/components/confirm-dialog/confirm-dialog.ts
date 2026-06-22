import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.html',
})
export class ConfirmDialogComponent {
  title = input('Confirm');
  message = input('Are you sure?');
  confirmLabel = input('Confirm');
  cancelLabel = input('Cancel');
  variant = input<'danger' | 'default'>('default');

  confirmed = output<void>();
  cancelled = output<void>();
}
