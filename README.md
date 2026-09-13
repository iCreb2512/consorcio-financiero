# Consorcio financiero

Aplicación académica en **C# / Windows Forms / .NET 8 / SQL Server**, preparada para Visual Studio y SSMS. Implementa el enunciado de `Proyecto Consorcio-1.pdf`. Los datos iniciales son ficticios.

## Descarga desde GitHub

El repositorio contiene el código fuente, los scripts SQL y la documentación. El ejecutable con el runtime incluido se distribuye en **Releases**, dentro de `Consorcio.zip`. Descarga y extrae ese paquete para ejecutar sin compilar. Si clonas el repositorio, puedes abrir `Consorcio.sln` y compilarlo en Visual Studio.

## Inicio rápido

1. Abre SSMS y conéctate a tu instancia de SQL Server.
2. Ejecuta `database/01_Estructura.sql` y luego `database/02_DatosDemo.sql`, en ese orden. Los scripts crean `ConsorcioAcademico`; se detienen si detectan estructura o datos previos, sin sobrescribirlos.
3. Edita `ejecutable/conexion.txt` para indicar tu instancia. La configuración entregada usa `.\SQLEXPRESS` y autenticación de Windows.
4. Ejecuta `INICIAR.cmd` o `ejecutable/Consorcio.WinForms.exe`.
5. Entra con `admin` y contraseña `ConsorcioDemo!2026`.

En el equipo donde se preparó esta entrega, la base ya está instalada en `.\SQLEXPRESS`. No repitas los scripts para usar esa instalación.

## Abrir y modificar en Visual Studio

Abre `Consorcio.sln`. Instala la carga de trabajo **Desarrollo de escritorio de .NET** y el SDK .NET 8 si Visual Studio lo solicita. Usa Visual Studio 2022 con soporte para .NET 8 o una versión posterior compatible. Restaura NuGet y selecciona `Consorcio.WinForms` como proyecto de inicio. Pulsa F5.

La conexión usada al compilar está en `src/Consorcio.WinForms/conexion.txt`; Visual Studio la copia a la salida. La variable de entorno `CONSORCIO_CONNECTION`, si existe, tiene prioridad. No se incluyen contraseñas de SQL Server.

Los formularios están construidos en C# mediante controles y layouts nativos. Son editables desde código; **no incluyen archivos `.Designer.cs` para el diseñador visual de arrastrar y soltar**.

## Cuentas de demostración

| Cuenta | Rol | Acceso |
|---|---|---|
| admin | Administrador | Registros, eliminaciones, usuarios y auditoría |
| operador | Operador | Alta y edición de clientes e instituciones, asociaciones |
| consulta | Consulta | Listados, fichas, asociaciones y exportación de página |
| operador01 a operador07 | Operador | Cuentas adicionales para prácticas |

Todas usan `ConsorcioDemo!2026`. Cambia las contraseñas en **Usuarios / Ver o editar** antes de usar datos propios. Las contraseñas se almacenan con PBKDF2-SHA256, sal individual y 260,000 iteraciones. Los usuarios de aplicación son distintos de los inicios de sesión de SQL Server.

## Contenido

| Carpeta / archivo | Contenido |
|---|---|
| `Consorcio.sln` | Solución de Visual Studio |
| `src/Consorcio.WinForms` | Código C# y configuración |
| `ejecutable` | Aplicación compilada para Windows x64, con runtime incluido |
| `database` | Estructura, datos iniciales, consultas e índices |
| `docs/Manual.md` | Índice de uso, guía funcional y solución de problemas |
| `docs/Arquitectura.md` | Modelo, diccionario, validaciones, índices y escalabilidad |
| `docs/Defensa.pptx` | Presentación de 18 diapositivas con notas para exponer |
| `docs/Guion-defensa.md` | Demostración y respuestas para la defensa |
| `docs/Pruebas.txt` | Resultado de pruebas de integración |
| `docs/capturas` | Capturas renderizadas desde los formularios reales |

## Datos iniciales

3 consorcios, 12 instituciones (6 bancos y 6 cooperativas), 1,200 clientes, 2,400 afiliaciones, 10 cuentas, 14 departamentos y 44 municipios. Los nombres, direcciones y DUI de clientes son sintéticos. La validación de DUI comprueba formato y unicidad, no autenticidad ni dígito verificador oficial.

## Compilar y probar

```powershell
dotnet restore Consorcio.sln
dotnet build Consorcio.sln -c Release
dotnet publish src/Consorcio.WinForms -c Release -r win-x64 --self-contained true -o ejecutable
```

La prueba `--self-test` necesita la base de demostración y las contraseñas iniciales. Crea registros con prefijo `Test`, los retira al terminar y conserva su auditoría. Ejecútala solamente contra una base de pruebas:

```powershell
Start-Process ./ejecutable/Consorcio.WinForms.exe -ArgumentList '--self-test' -Wait
Get-Content ./ejecutable/test-results.txt
```

## Alcance empresarial

La entrega es funcional y probada en SQL Server local. Usa transacciones, índices y páginas de 50 registros. No certifica capacidad de producción ni incluye pruebas de carga de cientos de usuarios. Antes de un despliegue empresarial se requieren aislamiento de acceso mediante API o procedimientos con permisos restringidos, certificados válidos, respaldo operativo y medición de carga. El detalle se encuentra en `docs/Arquitectura.md`.
