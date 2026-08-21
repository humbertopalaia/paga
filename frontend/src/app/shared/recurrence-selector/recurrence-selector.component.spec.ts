import { Component } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { RecurrenceSelectorComponent, RecurrenceValue } from './recurrence-selector.component';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RecurrenceSelectorComponent],
  template: `<app-recurrence-selector [formControl]="control" />`,
})
class TestHostComponent {
  control = new FormControl<RecurrenceValue>({ isRecurring: false, frequency: null });
}

describe('RecurrenceSelectorComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [provideAnimationsAsync()],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    const selector = fixture.nativeElement.querySelector('app-recurrence-selector');
    expect(selector).toBeTruthy();
  });

  describe('toggle behavior', () => {
    it('should hide frequency select when toggle is OFF', () => {
      const frequencyField = fixture.nativeElement.querySelector('.frequency-field');
      expect(frequencyField).toBeNull();
    });

    it('should show frequency select when toggle is ON', fakeAsync(() => {
      toggleRecurrence(true);

      const frequencyField = fixture.nativeElement.querySelector('.frequency-field');
      expect(frequencyField).toBeTruthy();
    }));

    it('should hide frequency select when toggle is turned OFF', fakeAsync(() => {
      toggleRecurrence(true);
      expect(fixture.nativeElement.querySelector('.frequency-field')).toBeTruthy();

      toggleRecurrence(false);
      expect(fixture.nativeElement.querySelector('.frequency-field')).toBeNull();
    }));
  });

  describe('value emission', () => {
    it('should emit { isRecurring: false, frequency: null } when toggle is OFF', fakeAsync(() => {
      // Start ON then toggle OFF to trigger change
      hostComponent.control.setValue({ isRecurring: true, frequency: 'monthly' });
      fixture.detectChanges();
      tick();

      toggleRecurrence(false);

      expect(hostComponent.control.value).toEqual({ isRecurring: false, frequency: null });
    }));

    it('should emit correct value when frequency is selected', fakeAsync(() => {
      toggleRecurrence(true);

      selectFrequency('monthly');

      expect(hostComponent.control.value).toEqual({ isRecurring: true, frequency: 'monthly' });
    }));

    it('should emit { isRecurring: true, frequency: "weekly" } for weekly selection', fakeAsync(() => {
      toggleRecurrence(true);
      selectFrequency('weekly');

      expect(hostComponent.control.value).toEqual({ isRecurring: true, frequency: 'weekly' });
    }));

    it('should emit { isRecurring: true, frequency: "yearly" } for yearly selection', fakeAsync(() => {
      toggleRecurrence(true);
      selectFrequency('yearly');

      expect(hostComponent.control.value).toEqual({ isRecurring: true, frequency: 'yearly' });
    }));

    it('should clear frequency when toggle is turned OFF after selecting frequency', fakeAsync(() => {
      toggleRecurrence(true);
      selectFrequency('monthly');
      expect(hostComponent.control.value).toEqual({ isRecurring: true, frequency: 'monthly' });

      toggleRecurrence(false);
      expect(hostComponent.control.value).toEqual({ isRecurring: false, frequency: null });
    }));
  });

  describe('ControlValueAccessor contract', () => {
    it('should populate component when form control value is set programmatically', fakeAsync(() => {
      hostComponent.control.setValue({ isRecurring: true, frequency: 'yearly' });
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const selectorComponent = getRecurrenceSelectorInstance();
      expect(selectorComponent.isRecurring()).toBeTrue();
      expect(selectorComponent.frequency()).toBe('yearly');

      const frequencyField = fixture.nativeElement.querySelector('.frequency-field');
      expect(frequencyField).toBeTruthy();
    }));

    it('should set isRecurring=false and frequency=null when writeValue receives null', fakeAsync(() => {
      hostComponent.control.setValue({ isRecurring: true, frequency: 'monthly' });
      fixture.detectChanges();
      tick();

      hostComponent.control.setValue(null as any);
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const selectorComponent = getRecurrenceSelectorInstance();
      expect(selectorComponent.isRecurring()).toBeFalse();
      expect(selectorComponent.frequency()).toBeNull();
    }));

    it('should propagate disabled state from form control', fakeAsync(() => {
      hostComponent.control.disable();
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const selectorComponent = getRecurrenceSelectorInstance();
      expect(selectorComponent.disabled()).toBeTrue();
    }));

    it('should re-enable when form control is enabled', fakeAsync(() => {
      hostComponent.control.disable();
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      hostComponent.control.enable();
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const selectorComponent = getRecurrenceSelectorInstance();
      expect(selectorComponent.disabled()).toBeFalse();
    }));

    it('should mark form control as touched when interaction occurs', fakeAsync(() => {
      expect(hostComponent.control.touched).toBeFalse();

      toggleRecurrence(true);

      expect(hostComponent.control.touched).toBeTrue();
    }));
  });

  describe('frequency options', () => {
    it('should display three frequency options: Semanal, Mensal, Anual', fakeAsync(() => {
      toggleRecurrence(true);

      const selectorComponent = getRecurrenceSelectorInstance();
      expect(selectorComponent.frequencyOptions).toEqual([
        { value: 'weekly', label: 'Semanal' },
        { value: 'monthly', label: 'Mensal' },
        { value: 'yearly', label: 'Anual' },
      ]);
    }));
  });

  // --- Helpers ---

  function getRecurrenceSelectorInstance(): RecurrenceSelectorComponent {
    const debugEl = fixture.debugElement.query(
      el => el.componentInstance instanceof RecurrenceSelectorComponent
    );
    return debugEl.componentInstance as RecurrenceSelectorComponent;
  }

  function toggleRecurrence(checked: boolean): void {
    const selectorComponent = getRecurrenceSelectorInstance();
    selectorComponent.onToggleChange(checked);
    fixture.detectChanges();
  }

  function selectFrequency(value: string): void {
    const selectorComponent = getRecurrenceSelectorInstance();
    selectorComponent.onFrequencyChange(value);
    fixture.detectChanges();
  }
});
