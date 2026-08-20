import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog.component';

describe('ConfirmDialogComponent', () => {
  let component: ConfirmDialogComponent;
  let fixture: ComponentFixture<ConfirmDialogComponent>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>;

  function createComponent(data: ConfirmDialogData): void {
    dialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);

    TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent],
      providers: [
        provideAnimationsAsync(),
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: dialogRef },
      ],
    });

    fixture = TestBed.createComponent(ConfirmDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('should display the title and message', () => {
    createComponent({
      title: 'Confirmar ExclusÃ£o',
      message: 'Deseja excluir este item?',
    });

    const title = fixture.nativeElement.querySelector('[mat-dialog-title]');
    const message = fixture.nativeElement.querySelector('.message');

    expect(title.textContent).toContain('Confirmar ExclusÃ£o');
    expect(message.textContent).toContain('Deseja excluir este item?');
  });

  it('should use default labels when not provided', () => {
    createComponent({
      title: 'Teste',
      message: 'Mensagem',
    });

    expect(component.confirmLabel).toBe('Confirmar');
    expect(component.cancelLabel).toBe('Cancelar');
  });

  it('should use custom labels when provided', () => {
    createComponent({
      title: 'Excluir',
      message: 'Mensagem',
      confirmLabel: 'Excluir',
      cancelLabel: 'Voltar',
    });

    expect(component.confirmLabel).toBe('Excluir');
    expect(component.cancelLabel).toBe('Voltar');
  });

  it('should close with true when confirm is clicked', () => {
    createComponent({ title: 'T', message: 'M' });

    component.confirm();

    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('should close without value when cancel is clicked', () => {
    createComponent({ title: 'T', message: 'M' });

    component.cancel();

    expect(dialogRef.close).toHaveBeenCalledWith();
  });

  it('should display danger icon circle when type is danger', () => {
    createComponent({ title: 'T', message: 'M', type: 'danger' });

    const iconCircle = fixture.nativeElement.querySelector('.icon-circle--danger');
    expect(iconCircle).toBeTruthy();
    expect(component.iconName).toBe('warning');
  });

  it('should display warning icon circle when type is warning', () => {
    createComponent({ title: 'T', message: 'M', type: 'warning' });

    const iconCircle = fixture.nativeElement.querySelector('.icon-circle--warning');
    expect(iconCircle).toBeTruthy();
    expect(component.iconName).toBe('error_outline');
  });

  it('should display info icon circle when type is info', () => {
    createComponent({ title: 'T', message: 'M', type: 'info' });

    const iconCircle = fixture.nativeElement.querySelector('.icon-circle--info');
    expect(iconCircle).toBeTruthy();
    expect(component.iconName).toBe('info');
  });

  it('should not display icon container when type is not provided', () => {
    createComponent({ title: 'T', message: 'M' });

    const iconContainer = fixture.nativeElement.querySelector('.icon-container');
    expect(iconContainer).toBeNull();
  });

  it('should apply primary color to confirm button for danger type', () => {
    createComponent({ title: 'T', message: 'M', type: 'danger' });

    const confirmBtn = fixture.nativeElement.querySelector('.confirm-button');
    expect(confirmBtn.getAttribute('ng-reflect-color')).toBe('primary');
  });
});
