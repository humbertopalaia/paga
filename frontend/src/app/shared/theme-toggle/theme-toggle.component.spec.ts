import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal, Signal, WritableSignal } from '@angular/core';
import { ThemeToggleComponent } from './theme-toggle.component';
import { ThemeService, Theme } from '../../core/theme/theme.service';

describe('ThemeToggleComponent', () => {
  let component: ThemeToggleComponent;
  let fixture: ComponentFixture<ThemeToggleComponent>;
  let mockTheme: WritableSignal<Theme>;
  let mockThemeService: { theme: Signal<Theme>; toggle: jasmine.Spy };

  beforeEach(async () => {
    mockTheme = signal<Theme>('light');
    mockThemeService = {
      theme: mockTheme.asReadonly(),
      toggle: jasmine.createSpy('toggle')
    };

    await TestBed.configureTestingModule({
      imports: [ThemeToggleComponent],
      providers: [
        { provide: ThemeService, useValue: mockThemeService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ThemeToggleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display dark_mode icon when theme is light', () => {
    const iconEl = fixture.nativeElement.querySelector('mat-icon');
    expect(iconEl.textContent.trim()).toBe('dark_mode');
  });

  it('should display light_mode icon when theme is dark', () => {
    mockTheme.set('dark');
    fixture.detectChanges();

    const iconEl = fixture.nativeElement.querySelector('mat-icon');
    expect(iconEl.textContent.trim()).toBe('light_mode');
  });

  it('should have aria-label "Mudar para tema escuro" when theme is light', () => {
    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-label')).toBe('Mudar para tema escuro');
  });

  it('should have aria-label "Mudar para tema claro" when theme is dark', () => {
    mockTheme.set('dark');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-label')).toBe('Mudar para tema claro');
  });

  it('should call ThemeService.toggle() when button is clicked', () => {
    const button = fixture.nativeElement.querySelector('button');
    button.click();

    expect(mockThemeService.toggle).toHaveBeenCalledTimes(1);
  });
});
