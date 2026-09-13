# Manual e índice de uso

1. [Instalación](#instalación)
2. [Inicio de sesión](#inicio-de-sesión)
3. [Consorcios e instituciones](#consorcios-e-instituciones)
4. [Clientes](#clientes)
5. [Asociaciones](#asociaciones)
6. [Búsqueda y exportación](#búsqueda-y-exportación)
7. [Usuarios y auditoría](#usuarios-y-auditoría)
8. [Respaldo](#respaldo)
9. [Solución de problemas](#solución-de-problemas)

## Instalación

Requiere Windows x64 y acceso a SQL Server. SSMS sirve para crear y administrar la base; el sistema se ejecuta fuera de SSMS. Ejecuta los dos primeros scripts de `database` en orden. No requieren eliminar bases existentes. El ejecutable distribuido incluye el runtime de .NET.

La cadena de conexión está en `ejecutable/conexion.txt`. Ejemplo para Express:

```text
Server=.\SQLEXPRESS;Database=ConsorcioAcademico;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=8;
```

`TrustServerCertificate=True` facilita la conexión local de laboratorio. Para un servidor empresarial, instala un certificado confiable y usa `TrustServerCertificate=False`.

## Inicio de sesión

Ingresa la cuenta y contraseña de demostración indicadas en README. Cinco intentos incorrectos bloquean nuevos intentos por un minuto en esa ejecución del programa. Cerrar la aplicación reinicia este contador, por lo que no equivale a un control centralizado de autenticación.

El resumen muestra totales reales de la base y la cartera por institución. Usa el menú izquierdo para cambiar de módulo. Cerrar sesión vuelve al formulario de acceso.

## Consorcios e instituciones

Solo un administrador crea o modifica consorcios. En **Consorcios / Nuevo**, ingresa el nombre y guarda. El ID se genera automáticamente.

En **Instituciones / Nuevo**, registra nombre, consorcio, tipo y fecha de fundación. Marca la casilla de fecha para confirmar que la elegiste. Solo se permite Banco o Cooperativa. Una institución tiene un único `ConsorcioId` obligatorio. Puedes cambiar el consorcio desde su ficha sin duplicar la institución.

Para eliminar, selecciona una fila y pulsa **Eliminar**. La aplicación solicita confirmación. Si existen relaciones, SQL Server impide la eliminación. Un consorcio con instituciones no se puede borrar.

## Clientes

En **Clientes / Nuevo** completa:

| Campo | Regla |
|---|---|
| Nombre | De 3 a 120 caracteres |
| DUI | Ocho dígitos, guion y un dígito; único |
| Nacimiento | Fecha desde 1900 hasta hoy; confirma la casilla |
| Género | Selección del catálogo, incluida opción de no indicarlo |
| Departamento | Selección obligatoria |
| Municipio | Lista dependiente del departamento |
| Complemento | De 5 a 250 caracteres |

Al cambiar el departamento debes seleccionar nuevamente un municipio. El sistema guarda el municipio y deriva el departamento a través de su relación, evitando combinaciones incompatibles.

Pulsa **Guardar registro**. Los campos vacíos reciben un indicador de error y el formulario conserva lo escrito para corregirlo. **Ver / editar** o un doble clic abre la ficha completa. El rol Consulta puede verla, con controles deshabilitados para edición.

La fecha de nacimiento no aplica una regla de mayoría de edad porque el enunciado no la exige. El formato DUI no certifica que el documento pertenezca a una persona real.

## Asociaciones

Selecciona un cliente y pulsa **Instituciones del cliente**. La tabla muestra su cartera actual. La lista inferior ofrece solo instituciones aún no asociadas. Elige una y pulsa **Asociar**. Repite para vincular varias instituciones, incluso de consorcios distintos.

Selecciona una asociación y pulsa **Retirar seleccionada** para desvincularla. Retirar una asociación conserva tanto el cliente como la institución. Un cliente sin afiliaciones es válido. Para eliminar un cliente, retira primero todas sus afiliaciones.

## Búsqueda y exportación

Escribe el inicio del nombre o DUI y pulsa **Buscar** o Enter. Por ejemplo, `Ana` encuentra nombres que empiezan con Ana. La búsqueda no busca palabras intermedias. En Clientes también puedes seleccionar una institución para filtrar su cartera. Limpia el campo y selecciona Todas las instituciones para restablecer el listado.

**Anterior** y **Siguiente** recorren páginas de 50 filas. **Exportar página CSV** exporta exactamente la página visible, incluyendo los campos de detalle de esa página. El archivo usa UTF-8 y separador punto y coma. Puede abrirse desde Excel mediante importación CSV. No es una exportación completa de toda la base.

## Usuarios y auditoría

El administrador puede crear cuentas, modificar roles o cambiar contraseñas. Una contraseña vacía al editar conserva la existente. Una nueva debe tener entre 10 y 128 caracteres. El administrador no puede eliminar su propia cuenta ni quitarse ese rol.

El historial registra usuario, acción, entidad, ID y fecha UTC. La modificación del dato y su entrada de auditoría comparten una transacción. No almacena contraseñas ni valores personales anteriores y posteriores. Las acciones de SSMS fuera de la aplicación no pasan por esa auditoría.

## Respaldo

En SSMS, clic derecho sobre **ConsorcioAcademico / Tasks / Back Up**. Elige respaldo completo y una ruta donde la cuenta de servicio de SQL Server pueda escribir. Verifica el respaldo mediante **Restore / Verify Backup Media** cuando esa opción esté disponible, y ensaya la restauración con un nombre de base diferente.

Conserva por separado el código fuente y la configuración. Copiar archivos del ejecutable no respalda la base. Los scripts iniciales reconstruyen únicamente los datos ficticios originales, no los cambios posteriores.

## Solución de problemas

| Síntoma | Acción |
|---|---|
| No conecta | Comprueba que SQL Server esté iniciado y que la instancia de conexion.txt coincida con SSMS |
| No existe la base | Ejecuta 01 y 02 en esa misma instancia |
| Acceso denegado | La identidad de Windows que abre la aplicación debe tener permisos en la base |
| Error al crear la estructura | Si ya existe, no la repitas. Usa la instalación existente o cambia el nombre de base en una copia de los scripts |
| No aparece un municipio | Selecciona primero el departamento |
| Guardar señala fecha faltante | Activa la casilla del selector de fecha |
| Nombre o DUI duplicado | Busca el registro existente y edítalo |
| No permite eliminar | Retira las relaciones dependientes con una cuenta autorizada |
| No restaura NuGet | Revisa acceso a nuget.org y la carga de trabajo .NET de Visual Studio |

Para la demostración visual se recomienda una pantalla de 1366 × 900 o superior. Los formularios incluyen desplazamiento para acceder a los campos en ventanas más pequeñas.
