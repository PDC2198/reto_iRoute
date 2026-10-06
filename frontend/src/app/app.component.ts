import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CommerceService } from './services/commerce.service';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [CommonModule, FormsModule],
    template: `
    <main class="container">

      <header>
        <h1>Procesamiento de comercios</h1>
        <p>Carga, procesamiento y consulta de registros.</p>
      </header>

      <nav>
        <button
          [class.active]="tab === 'upload'"
          (click)="tab = 'upload'">
          Carga CSV
        </button>

        <button
          [class.active]="tab === 'process'"
          (click)="tab = 'process'">
          Procesar
        </button>

        <button
          [class.active]="tab === 'errors'"
          (click)="loadQuarantine()">
          Errores
        </button>
      </nav>

      <section *ngIf="tab === 'upload'">
        <h2>Cargar archivo CSV</h2>

        <p>
          Selecciona un archivo para visualizar sus registros
          antes de enviarlos.
        </p>

        <input
          type="file"
          accept=".csv"
          (change)="selectFile($event)">

        <p class="error" *ngIf="previewError">
          {{ previewError }}
        </p>

        <div class="table-wrapper" *ngIf="preview.length">
          <table>
            <thead>
              <tr>
                <th *ngFor="let header of headers">
                  {{ header }}
                </th>
              </tr>
            </thead>

            <tbody>
              <tr *ngFor="let row of preview">
                <td *ngFor="let header of headers">
                  {{ row[header] }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <button
          class="primary"
          [disabled]="!file || !!previewError"
          (click)="upload()">
          Enviar al backend
        </button>
      </section>

      <section *ngIf="tab === 'process'">
        <h2>Procesar por fecha</h2>

        <p>
          Selecciona la fecha de proceso de los registros.
        </p>

        <div class="form-row">
          <input
            type="date"
            [(ngModel)]="date">

          <button
            class="primary"
            [disabled]="!date"
            (click)="process()">
            Procesar
          </button>
        </div>
      </section>

      <section *ngIf="tab === 'errors'">
        <h2>Registros en cuarentena</h2>

        <p>
          Registros que no cumplieron las validaciones del proceso.
        </p>

        <div class="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Comercio</th>
                <th>Documento</th>
                <th>Motivo</th>
              </tr>
            </thead>

            <tbody>
              <tr *ngFor="let row of quarantine">
                <td>
                  {{ row.pcProcessDate | date:'yyyy-MM-dd' }}
                </td>
                <td>
                  {{ row.pcNomcomred || '-' }}
                </td>
                <td>
                  {{ row.pcNumdoc || '-' }}
                </td>
                <td>
                  {{ row.motivo }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <p class="message" *ngIf="message">
        {{ message }}
      </p>

    </main>
  `
})
export class AppComponent {

    tab = 'upload';

    file?: File;

    headers: string[] = [];

    preview: Record<string, string>[] = [];

    previewError = '';

    message = '';

    date = '';

    quarantine: any[] = [];

    private readonly expectedHeaders = [
        'pc_processdate',
        'pc_nomcomred',
        'pc_numdoc',
        'pc_address',
        'pc_city'
    ];

    constructor(private api: CommerceService) { }

    selectFile(event: Event): void {

        this.file =
            (event.target as HTMLInputElement).files?.[0];

        this.preview = [];
        this.previewError = '';
        this.message = '';

        if (!this.file) {
            return;
        }

        const reader = new FileReader();

        reader.onload = () => {

            const lines = String(reader.result)
                .split(/\r?\n/)
                .filter(line => line.trim() !== '');

            this.headers = (lines[0] || '')
                .split(',')
                .map(header => header.trim());

            const missingColumns =
                this.expectedHeaders.filter(
                    expected => !this.headers.includes(expected)
                );

            if (missingColumns.length > 0) {
                this.previewError =
                    'Formato incorrecto. Faltan columnas: ' +
                    missingColumns.join(', ');

                return;
            }

            this.preview = lines
                .slice(1, 6)
                .map(line => {

                    const values = line.split(',');

                    return Object.fromEntries(
                        this.headers.map(
                            (header, index) =>
                                [header, values[index] ?? '']
                        )
                    );
                });
        };

        reader.readAsText(this.file);
    }

    upload(): void {

        if (!this.file) {
            return;
        }

        this.api.upload(this.file).subscribe({

            next: response => {
                this.message =
                    `Se cargaron ${response.inserted} registros.`;
            },

            error: error => {
                this.message =
                    error.error?.message ||
                    error.error ||
                    'Error al cargar el archivo.';
            }

        });
    }

    process(): void {

        if (!this.date) {
            return;
        }

        this.api.process(this.date).subscribe({

            next: response => {
                this.message =
                    `Registros enviados a cuarentena: ${response.quarantined}`;
            },

            error: () => {
                this.message =
                    'Error al procesar los registros.';
            }

        });
    }

    loadQuarantine(): void {

        this.tab = 'errors';

        this.api.quarantine().subscribe({

            next: response => {
                this.quarantine = response;
            },

            error: () => {
                this.message =
                    'No se pudieron consultar los registros en cuarentena.';
            }

        });
    }
}