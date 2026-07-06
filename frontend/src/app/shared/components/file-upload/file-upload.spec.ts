import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { FileUploadComponent } from './file-upload';

function fakeFile(name: string, sizeBytes: number, type = 'application/octet-stream'): File {
  const file = new File(['x'], name, { type });
  Object.defineProperty(file, 'size', { value: sizeBytes });
  return file;
}

function fileInputEvent(file: File): Event {
  const input = document.createElement('input');
  Object.defineProperty(input, 'files', { value: [file] });
  return { target: input } as unknown as Event;
}

describe('FileUploadComponent', () => {
  function create(overrides: { accept?: string; maxSizeMb?: number } = {}) {
    const fixture = TestBed.createComponent(FileUploadComponent);
    if (overrides.accept !== undefined) fixture.componentRef.setInput('accept', overrides.accept);
    if (overrides.maxSizeMb !== undefined) fixture.componentRef.setInput('maxSizeMb', overrides.maxSizeMb);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  describe('onFileSelected', () => {
    it('accepts a valid file within size and type limits', () => {
      const component = create();
      const emitted = vi.fn();
      component.fileSelected.subscribe(emitted);
      const file = fakeFile('receipt.pdf', 1024);

      component.onFileSelected(fileInputEvent(file));

      expect(component.selectedFile()).toBe(file);
      expect(component.error()).toBe('');
      expect(emitted).toHaveBeenCalledWith(file);
    });

    it('rejects a file over the max size and does not select it', () => {
      const component = create({ maxSizeMb: 5 });
      const emitted = vi.fn();
      component.fileSelected.subscribe(emitted);
      const file = fakeFile('big.pdf', 6 * 1048576);

      component.onFileSelected(fileInputEvent(file));

      expect(component.selectedFile()).toBeNull();
      expect(component.error()).toBe('File exceeds 5 MB limit');
      expect(emitted).not.toHaveBeenCalled();
    });

    it('rejects a file with a disallowed extension', () => {
      const component = create({ accept: '.pdf,.jpg' });
      const file = fakeFile('malware.exe', 1024);

      component.onFileSelected(fileInputEvent(file));

      expect(component.selectedFile()).toBeNull();
      expect(component.error()).toBe('Accepted formats: .pdf,.jpg');
    });

    it('accepts an extension regardless of case', () => {
      const component = create({ accept: '.pdf,.jpg' });
      const file = fakeFile('receipt.PDF', 1024);

      component.onFileSelected(fileInputEvent(file));

      expect(component.selectedFile()).toBe(file);
      expect(component.error()).toBe('');
    });

    it('clears a previous error when a subsequent valid file is selected', () => {
      const component = create({ accept: '.pdf' });
      component.onFileSelected(fileInputEvent(fakeFile('bad.exe', 100)));
      expect(component.error()).not.toBe('');

      component.onFileSelected(fileInputEvent(fakeFile('good.pdf', 100)));
      expect(component.error()).toBe('');
      expect(component.selectedFile()?.name).toBe('good.pdf');
    });
  });

  describe('onDragOver / onDrop', () => {
    it('prevents default and stops propagation on dragover', () => {
      const component = create();
      const event = { preventDefault: vi.fn(), stopPropagation: vi.fn() } as unknown as DragEvent;
      component.onDragOver(event);
      expect(event.preventDefault).toHaveBeenCalled();
      expect(event.stopPropagation).toHaveBeenCalled();
    });

    it('validates and selects the dropped file', () => {
      const component = create();
      const file = fakeFile('dropped.png', 100);
      const event = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      } as unknown as DragEvent;

      component.onDrop(event);

      expect(event.preventDefault).toHaveBeenCalled();
      expect(component.selectedFile()).toBe(file);
    });
  });

  describe('removeFile', () => {
    it('clears the selected file and error, and emits the removed file', () => {
      const component = create();
      const emitted = vi.fn();
      component.fileRemoved.subscribe(emitted);
      const file = fakeFile('receipt.pdf', 100);
      component.onFileSelected(fileInputEvent(file));

      component.removeFile();

      expect(component.selectedFile()).toBeNull();
      expect(component.error()).toBe('');
      expect(emitted).toHaveBeenCalledWith(file);
    });

    it('does not emit when there is no file to remove', () => {
      const component = create();
      const emitted = vi.fn();
      component.fileRemoved.subscribe(emitted);

      component.removeFile();

      expect(emitted).not.toHaveBeenCalled();
    });
  });

  describe('formatSize', () => {
    it('formats bytes under 1KB', () => {
      const component = create();
      expect(component.formatSize(512)).toBe('512 B');
    });

    it('formats kilobytes', () => {
      const component = create();
      expect(component.formatSize(2048)).toBe('2.0 KB');
    });

    it('formats megabytes', () => {
      const component = create();
      expect(component.formatSize(2 * 1048576)).toBe('2.0 MB');
    });
  });
});
