import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
  MatDialogTitle,
  MatDialogContent,
  MatDialogActions,
  MatDialogClose,
} from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  type?: 'danger' | 'warning' | 'info';
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialogComponent {
  data: ConfirmDialogData = inject(MAT_DIALOG_DATA);
  private dialogRef = inject(MatDialogRef<ConfirmDialogComponent, boolean>);

  get confirmLabel(): string {
    return this.data.confirmLabel ?? 'Confirmar';
  }

  get cancelLabel(): string {
    return this.data.cancelLabel ?? 'Cancelar';
  }

  get iconName(): string {
    switch (this.data.type) {
      case 'danger':
        return 'warning';
      case 'warning':
        return 'error_outline';
      case 'info':
        return 'info';
      default:
        return '';
    }
  }

  confirm(): void {
    this.dialogRef.close(true);
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
