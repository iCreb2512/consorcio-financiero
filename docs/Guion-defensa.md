# Guion de exposición y defensa

Duración sugerida: 12 a 15 minutos. La presentación incluye 18 diapositivas y notas editables. Completa los nombres de los integrantes al exponer o en una copia de la portada.

## Recorrido de la presentación

1. Presentar Consorcio y la tecnología solicitada por el curso.
2. Explicar el problema: varias instituciones y clientes compartidos.
3. Relacionar la solución con la rúbrica del PDF.
4. Explicar las dos cardinalidades sin lenguaje de implementación.
5. Mostrar la separación entre interfaz, servicio y base de datos.
6. Explicar el esquema y por qué existe Afiliaciones.
7. Explicar la dirección normalizada y catálogo dependiente.
8. Mostrar el resumen de datos iniciales.
9. Mostrar alta y edición de clientes.
10. Demostrar el vínculo con más de una institución.
11. Mostrar mensajes de validación y restricciones.
12. Explicar roles y contraseña con hash.
13. Explicar transacciones y conflicto de edición.
14. Relacionar cada índice con una búsqueda concreta.
15. Comentar las pruebas ejecutadas y su alcance.
16. Explicar la instalación desde SSMS y Visual Studio.
17. Distinguir capacidad implementada y trabajo de producción pendiente.
18. Cerrar con la demostración y responder preguntas.

## Demostración de cinco minutos

1. Iniciar con admin y abrir Resumen. Señalar 1,200 clientes y 2,400 asociaciones: no son 2,400 clientes.
2. Abrir Clientes y buscar `Ana`. Filtrar por institución. Limpiar ambos filtros.
3. Crear un cliente con DUI de prueba que no exista, fecha confirmada y dirección completa. Guardar.
4. Abrir Instituciones del cliente y asociar dos instituciones de consorcios distintos.
5. Volver a su ficha y comprobar que el nombre, dirección y asociaciones persisten.
6. Intentar crear otra persona con el mismo DUI. Explicar el mensaje y la restricción UNIQUE.
7. Abrir Auditoría para mostrar las acciones. Cerrar sesión y entrar como consulta para mostrar acceso sin edición.

## Preguntas previsibles

**¿Por qué una tabla intermedia?** Porque un cliente puede tener varias instituciones y una institución varios clientes. Guardar listas de IDs en una columna impediría aplicar claves foráneas y consultar correctamente.

**¿Cómo aseguras una sola pertenencia institucional?** Instituciones tiene una sola columna ConsorcioId obligatoria. La FK impide apuntar a un consorcio inexistente.

**¿Por qué no guardas departamento y municipio juntos en el cliente?** El municipio ya determina el departamento. Se guarda la referencia y se consulta el nombre con JOIN, evitando datos contradictorios.

**¿Qué significa programar orientado a objetos aquí?** Cada clase encapsula una responsabilidad. Los formularios heredan de Form y usan un servicio compuesto con Database. Session modela la identidad de forma inmutable. No se necesita herencia para cada tabla.

**¿Qué ocurre si se interrumpe el guardado?** Los cambios y la auditoría comparten una transacción. Si no se confirma, SQL Server revierte esa operación.

**¿Y si dos personas editan a la vez?** La primera confirma su versión y la segunda recibe un conflicto mediante rowversion. Debe revisar el registro actualizado.

**¿Un hash equivale a cifrar la contraseña?** No. Se verifica calculando PBKDF2 con la sal guardada y comparando resultados. No existe una función de recuperar la contraseña original.

**¿Por qué un índice en ambos sentidos de Afiliaciones?** La PK sirve al recorrido por cliente. El índice inverso permite consultar la cartera de una institución.

**¿Está listo para una gran empresa?** Es una entrega funcional probada localmente. Tiene estructura, índices y controles útiles, pero no certifica carga ni cubre toda la seguridad operativa de producción. Explicar la ruta indicada en Arquitectura.md.

**¿Es válido oficialmente cada DUI?** Solo se valida su forma y unicidad dentro de la base. Los datos iniciales son ficticios y no se consulta un registro oficial.

## Antes de exponer

Comprueba que SQL Server esté iniciado, abre la solución o el ejecutable, prueba el acceso y conserva una copia de los scripts. No ejecutes la carga inicial sobre una base ya instalada. No elimines registros de demostración para reiniciarla durante la exposición.
