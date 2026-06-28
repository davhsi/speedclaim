import { Component, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  templateUrl: './file-upload.html',
})
export class FileUploadComponent {
  accept = input('.pdf,.jpg,.jpeg,.png');
  maxSizeMb = input(5);
  label = input('');
  hint = input('');

  fileSelected = output<File>();
  fileRemoved = output<File>();

  selectedFile = signal<File | null>(null);
  error = signal('');

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.validateAndSet(input.files[0]);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer?.files.length) this.validateAndSet(event.dataTransfer.files[0]);
  }

  removeFile(): void {
    const file = this.selectedFile();
    this.selectedFile.set(null);
    this.error.set('');
    if (file) this.fileRemoved.emit(file);
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / 1048576).toFixed(1) + ' MB';
  }

  private validateAndSet(file: File): void {
    this.error.set('');
    const maxBytes = this.maxSizeMb() * 1048576;
    if (file.size > maxBytes) {
      this.error.set(`File exceeds ${this.maxSizeMb()} MB limit`);
      return;
    }
    const accepted = this.accept().split(',').map(e => e.trim().toLowerCase());
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (accepted.length && !accepted.includes(ext)) {
      this.error.set(`Accepted formats: ${this.accept()}`);
      return;
    }
    this.selectedFile.set(file);
    this.fileSelected.emit(file);
  }
}
