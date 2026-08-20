import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
import { HeaderComponent } from './header.component';

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HeaderComponent],
      providers: [provideRouter([
        { path: 'dashboard', component: HeaderComponent },
        { path: 'users', component: HeaderComponent },
      ])]
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display user name "Administrador"', () => {
    const userNameEl = fixture.debugElement.query(By.css('.user-name'));
    expect(userNameEl).toBeTruthy();
    expect(userNameEl.nativeElement.textContent.trim()).toBe('Administrador');
  });

  it('should render app-theme-toggle element', () => {
    const themeToggle = fixture.debugElement.query(By.css('app-theme-toggle'));
    expect(themeToggle).toBeTruthy();
  });

  it('should render logout button with aria-label "Sair"', () => {
    const logoutBtn = fixture.debugElement.query(
      By.css('button[aria-label="Sair"]')
    );
    expect(logoutBtn).toBeTruthy();
  });

  it('should render logout button with text "Sair"', () => {
    const logoutBtn = fixture.debugElement.query(
      By.css('button[aria-label="Sair"]')
    );
    expect(logoutBtn.nativeElement.textContent).toContain('Sair');
  });

  it('should display page title', () => {
    const pageTitleEl = fixture.debugElement.query(By.css('.page-title'));
    expect(pageTitleEl).toBeTruthy();
    expect(pageTitleEl.nativeElement.textContent.trim()).toBeTruthy();
  });

  it('should show "Dashboard" as default page title for root URL', () => {
    const pageTitleEl = fixture.debugElement.query(By.css('.page-title'));
    expect(pageTitleEl.nativeElement.textContent.trim()).toBe('Dashboard');
  });

  it('should NOT show hamburger menu button when isMobile is false', () => {
    fixture.componentRef.setInput('isMobile', false);
    fixture.detectChanges();

    const menuBtn = fixture.debugElement.query(
      By.css('button[aria-label="Abrir menu"]')
    );
    expect(menuBtn).toBeNull();
  });

  it('should show hamburger menu button when isMobile is true', () => {
    fixture.componentRef.setInput('isMobile', true);
    fixture.detectChanges();

    const menuBtn = fixture.debugElement.query(
      By.css('button[aria-label="Abrir menu"]')
    );
    expect(menuBtn).toBeTruthy();
  });

  it('should emit menuToggle when hamburger button is clicked', () => {
    fixture.componentRef.setInput('isMobile', true);
    fixture.detectChanges();

    spyOn(component.menuToggle, 'emit');

    const menuBtn = fixture.debugElement.query(
      By.css('button[aria-label="Abrir menu"]')
    );
    menuBtn.nativeElement.click();

    expect(component.menuToggle.emit).toHaveBeenCalled();
  });
});
