import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PlaceholderComponent } from './placeholder.component';

describe('PlaceholderComponent', () => {
  let fixture: ComponentFixture<PlaceholderComponent>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlaceholderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PlaceholderComponent);
    fixture.detectChanges();
    nativeElement = fixture.nativeElement;
  });

  it('should create the component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render "Em construção" heading', () => {
    const heading = nativeElement.querySelector('h2');
    expect(heading).toBeTruthy();
    expect(heading!.textContent).toContain('Em construção');
  });

  it('should render the construction icon', () => {
    const icon = nativeElement.querySelector('mat-icon');
    expect(icon).toBeTruthy();
    expect(icon!.textContent?.trim()).toBe('construction');
  });

  it('should render the description text', () => {
    const paragraph = nativeElement.querySelector('p');
    expect(paragraph).toBeTruthy();
    expect(paragraph!.textContent).toContain('Esta funcionalidade estará disponível em breve.');
  });
});
