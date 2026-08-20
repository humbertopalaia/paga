import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SidebarComponent } from './sidebar.component';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render 5 navigation items', () => {
    const items = fixture.debugElement.queryAll(By.css('a[mat-list-item]'));
    expect(items.length).toBe(5);
  });

  it('should render items in correct order: Dashboard, Usuários, Tipos de Despesa, Receitas, Despesas', () => {
    const items = fixture.debugElement.queryAll(By.css('a[mat-list-item]'));
    const labels = items.map(item => item.nativeElement.textContent.trim());

    expect(labels[0]).toContain('Dashboard');
    expect(labels[1]).toContain('Usuários');
    expect(labels[2]).toContain('Tipos de Despesa');
    expect(labels[3]).toContain('Receitas');
    expect(labels[4]).toContain('Despesas');
  });

  it('should render correct icons for each item', () => {
    const icons = fixture.debugElement.queryAll(By.css('a[mat-list-item] mat-icon'));
    const iconTexts = icons.map(icon => icon.nativeElement.textContent.trim());

    expect(iconTexts[0]).toBe('dashboard');
    expect(iconTexts[1]).toBe('people');
    expect(iconTexts[2]).toBe('category');
    expect(iconTexts[3]).toBe('trending_up');
    expect(iconTexts[4]).toBe('trending_down');
  });

  it('should render correct routerLink for each item', () => {
    const items = fixture.debugElement.queryAll(By.css('a[mat-list-item]'));
    const routes = items.map(item => item.attributes['ng-reflect-router-link']);

    expect(routes[0]).toBe('/dashboard');
    expect(routes[1]).toBe('/users');
    expect(routes[2]).toBe('/expense-types');
    expect(routes[3]).toBe('/incomes');
    expect(routes[4]).toBe('/expenses');
  });

  it('should emit navigated event when a nav item is clicked', () => {
    let emitted = false;
    component.navigated.subscribe(() => emitted = true);

    const firstItem = fixture.debugElement.query(By.css('a[mat-list-item]'));
    firstItem.nativeElement.click();

    expect(emitted).toBeTrue();
  });

  it('should display PAGA logo text', () => {
    const logoEl = fixture.debugElement.query(By.css('.logo-text'));
    expect(logoEl).toBeTruthy();
    expect(logoEl.nativeElement.textContent.trim()).toBe('PAGA');
  });

  it('should display logo icon with letter P', () => {
    const logoIcon = fixture.debugElement.query(By.css('.logo-icon'));
    expect(logoIcon).toBeTruthy();
    expect(logoIcon.nativeElement.textContent.trim()).toBe('P');
  });
});
