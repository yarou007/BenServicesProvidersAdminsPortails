import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ReportType } from '../../../shared/models/report.model';
import { ReportService } from '../../../shared/services/report.service';

@Component({
  selector: 'app-reports-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSnackBarModule
  ],
  templateUrl: './reports-page.component.html',
  styleUrl: './reports-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly reportService = inject(ReportService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly providerReportCards$ = this.reportService.providerReportCards$;
  protected readonly regionReportCards$ = this.reportService.regionReportCards$;
  protected readonly applicationReportCards$ = this.reportService.applicationReportCards$;

  protected readonly customReportResults = signal<Array<{ label: string; value: number; trend: string }>>([]);

  protected readonly reportForm = this.formBuilder.nonNullable.group({
    dateFrom: '',
    dateTo: '',
    reportType: 'Providers' as ReportType
  });

  protected exportCard(title: string): void {
    this.snackBar.open(`${title} exported as CSV (demo).`, 'Close', { duration: 1700 });
  }

  protected generateCustomReport(): void {
    const { dateFrom, dateTo, reportType } = this.reportForm.getRawValue();
    const results = this.reportService.generateCustomReport(reportType, dateFrom, dateTo);
    this.customReportResults.set(results);
  }
}
