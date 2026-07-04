import {
  Component, Input, signal, computed, HostListener,
  ElementRef, inject, ViewChild, ElementRef as ElRef
} from '@angular/core';
import { ControlValueAccessor, NgControl, FormsModule } from '@angular/forms';

@Component({
  selector: 'app-select',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './app-select.html',
  styles: `
    :host { display: block; position: relative; }

    .sc-trigger {
      width: 100%;
      padding: 8px 12px;
      border: 1px solid var(--color-line);
      border-radius: var(--radius-control);
      background: white;
      color: var(--color-ink);
      font-size: 14px;
      line-height: 22px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      cursor: pointer;
      text-align: left;
      transition: border-color 150ms ease, box-shadow 150ms ease;
      font-family: inherit;
    }
    .sc-trigger:focus { outline: none; }
    .sc-trigger.is-open,
    .sc-trigger:focus-visible {
      border-color: var(--color-gold);
      box-shadow: 0 0 0 3px rgba(245,166,35,.18);
      outline: none;
    }
    .sc-trigger.is-invalid {
      border-color: var(--color-danger);
    }
    .sc-trigger.is-invalid.is-open {
      border-color: var(--color-danger);
      box-shadow: 0 0 0 3px rgba(209,67,67,.18);
    }
    .sc-trigger:disabled { opacity: 0.5; cursor: not-allowed; background: var(--color-surface); }

    .sc-chevron { flex-shrink: 0; transition: transform 180ms ease; color: var(--color-muted); }
    .sc-chevron.rotated { transform: rotate(180deg); }

    .sc-panel {
      position: absolute;
      top: calc(100% + 5px);
      left: 0; right: 0;
      background: white;
      border: 1px solid var(--color-line);
      border-radius: var(--radius-card);
      box-shadow: var(--shadow-dropdown);
      z-index: 500;
      overflow: hidden;
      animation: sc-dd-in 120ms ease-out;
    }
    @keyframes sc-dd-in {
      from { opacity: 0; transform: translateY(-4px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    .sc-search {
      padding: 8px 8px 6px;
      border-bottom: 1px solid var(--color-line);
    }
    .sc-search input {
      width: 100%;
      padding: 6px 10px;
      font-size: 13px;
      border: 1px solid var(--color-line);
      border-radius: 6px;
      background: var(--color-surface);
      color: var(--color-ink);
      outline: none;
      font-family: inherit;
      transition: border-color 120ms;
    }
    .sc-search input:focus { border-color: var(--color-gold); }

    .sc-list { max-height: 220px; overflow-y: auto; padding: 4px; }

    .sc-option {
      width: 100%; padding: 8px 10px;
      display: flex; align-items: center; gap: 8px;
      font-size: 13.5px; font-family: inherit;
      background: transparent; border: none; border-radius: 6px;
      cursor: pointer; color: var(--color-ink); text-align: left;
      transition: background 80ms;
    }
    .sc-option:hover  { background: var(--color-surface-alt); }
    .sc-option.is-selected {
      background: var(--color-gold-light);
      color: var(--color-gold-dark);
      font-weight: 600;
    }

    .sc-empty { padding: 16px 12px; text-align: center; font-size: 13px; color: var(--color-muted); }
  `
})
export class AppSelectComponent implements ControlValueAccessor {
  @Input() options: string[] = [];
  @Input() placeholder = 'Select';
  @Input() searchable = false;

  @ViewChild('searchInput') searchInput?: ElRef<HTMLInputElement>;

  private readonly el = inject(ElementRef);
  private readonly ngControl = inject(NgControl, { optional: true, self: true });

  isOpen = signal(false);
  private readonly _value = signal('');
  query = signal('');
  isDisabled = false;

  private onChange: (v: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    if (this.ngControl) this.ngControl.valueAccessor = this;
  }

  get displayValue() { return this._value() || ''; }
  get isInvalid() { return this.ngControl?.invalid && this.ngControl?.touched; }

  filtered = computed(() => {
    if (!this.searchable || !this.query().trim()) return this.options;
    const q = this.query().toLowerCase();
    return this.options.filter(o => o.toLowerCase().includes(q));
  });

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent) {
    if (!this.el.nativeElement.contains(e.target as Node)) this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape() { this.close(); }

  toggle() {
    if (this.isDisabled) return;
    const opening = !this.isOpen();
    this.isOpen.set(opening);
    if (opening) {
      this.query.set('');
      if (this.searchable) {
        setTimeout(() => this.searchInput?.nativeElement.focus(), 50);
      }
    }
    if (!this.ngControl?.touched) this.onTouched();
  }

  pick(opt: string) {
    this._value.set(opt);
    this.onChange(opt);
    this.isOpen.set(false);
  }

  private close() { this.isOpen.set(false); }

  writeValue(v: string) { this._value.set(v ?? ''); }
  registerOnChange(fn: (v: string) => void) { this.onChange = fn; }
  registerOnTouched(fn: () => void) { this.onTouched = fn; }
  setDisabledState(d: boolean) { this.isDisabled = d; }
}
