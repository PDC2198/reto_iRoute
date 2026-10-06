# Reto iRoute - .NET + Angular + SQL Server

Solución simple para carga, validación, procesamiento y cuarentena de comercios.

## Decisión sobre el CSV
El documento no adjunta la estructura real del archivo. Por eso se propone un contrato mínimo coherente con los campos mencionados en la prueba:
`pc_processdate, pc_nomcomred, pc_numdoc, pc_address, pc_city`.

El backend es la validación definitiva: rechaza archivo vacío, extensión distinta de CSV, cabecera ausente, columnas requeridas faltantes y fechas inválidas. Esto evita que un CSV con otro formato termine generando errores SQL. El frontend repite una validación básica solo para dar respuesta rápida al usuario.

## Base de datos
Ejecutar `database/database.sql` en SQL Server. Crea `IRouteCommerceDb`, `commerce`, `commerce_quarantine`, `sp_create_commerce` y `sp_process_commerce`.

## Backend
Requiere .NET 8. Ajustar `backend/CommerceApi/appsettings.json` según la instancia SQL Server.

```bash
cd backend/CommerceApi
dotnet restore
dotnet run --urls http://localhost:5000
```
Swagger: `http://localhost:5000/swagger`.

Endpoints:
- `POST /api/commerce/upload`: recibe multipart/form-data con `file`.
- `POST /api/commerce/process/{date}`: procesa registros de esa fecha.
- `GET /api/commerce/quarantine`: lista errores y motivos.

## Frontend
Requiere Node/npm.

```bash
cd frontend
npm install
npm start
```
Abrir `http://localhost:4200`.

## Casos para demostrar
1. Subir `sample-commerce.csv` y previsualizarlo.
2. Procesar `2026-10-06`: dos filas deben pasar a cuarentena.
3. Consultar Errores y mostrar el motivo.
4. Cambiar/eliminar una cabecera del CSV: el frontend avisa y el backend también lo rechaza.
5. Probar un archivo vacío o no CSV.

## Nota
Si se entrega el CSV oficial, se deben sustituir/agregar las columnas del modelo, tabla, procedimiento `sp_create_commerce` y lista `Required`. La estrategia de validación permanece igual.
