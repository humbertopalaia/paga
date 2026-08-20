import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ShellComponent } from './shell.component';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { BehaviorSubject } from 'rxjs';
import { By } from '@angular/platform-browser';

describe('ShellComponent', () => {
  let component: ShellComponent;
  let fixture: ComponentFixture<ShellComponent>;
  let breakpointSubject: BehaviorSubject<BreakpointState>;

  function createComponent(mobile: boolean): void {
    breakpointSubject = new BehaviorSubject<BreakpointState>({
      matches: mobile,
      breakpoints: { '(max-width: 767px)': mobile }
    });

    const mockBreakpointObserver = {
      observe: () => breakpointSubject.asObservable()
    };

    TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideRouter([]),
        provideAnimationsAsync(),
        { provide: BreakpointObserver, useValue: mockBreakpointObserver }
      ]
    });

    fixture = TestBed.createComponent(ShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  describe('desktop viewport (>= 768px)', () => {
    beforeEach(() => {
      createComponent(false);
    });

    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should render app-sidebar element', () => {
      const sidebar = fixture.debugElement.query(By.css('app-sidebar'));
      expect(sidebar).toBeTruthy();
    });

    it('should render app-header element', () => {
      const header = fixture.debugElement.query(By.css('app-header'));
      expect(header).toBeTruthy();
    });

    it('should render router-outlet', () => {
      const outlet = fixture.debugElement.query(By.css('router-outlet'));
      expect(outlet).toBeTruthy();
    });

    it('should set sidebar mode to side when viewport >= 768px', () => {
      expect(component.sidenavMode).toBe('side');
    });

    it('should have sidebar opened when not mobile', () => {
      expect(component.sidenavOpened).toBeTrue();
    });
  });

  describe('mobile viewport (< 768px)', () => {
    beforeEach(() => {
      createComponent(true);
    });

    it('should set sidebar mode to over when viewport < 768px', () => {
      expect(component.sidenavMode).toBe('over');
    });

    it('should have sidebar closed (not opened) when mobile', () => {
      expect(component.sidenavOpened).toBeFalse();
    });
  });
});
