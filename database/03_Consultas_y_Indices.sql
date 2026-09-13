USE ConsorcioAcademico;
GO
-- Inventario y comprobación de las reglas principales.
SELECT 'Consorcios' Tabla,COUNT(*) Registros FROM dbo.Consorcios
UNION ALL SELECT 'Instituciones',COUNT(*) FROM dbo.Instituciones
UNION ALL SELECT 'Clientes',COUNT(*) FROM dbo.Clientes
UNION ALL SELECT 'Afiliaciones',COUNT(*) FROM dbo.Afiliaciones
UNION ALL SELECT 'Usuarios',COUNT(*) FROM dbo.Usuarios
UNION ALL SELECT 'Departamentos',COUNT(*) FROM dbo.Departamentos
UNION ALL SELECT 'Municipios',COUNT(*) FROM dbo.Municipios;

-- Una fila por afiliación. Incluye clientes sin instituciones mediante LEFT JOIN.
SELECT TOP(50) * FROM dbo.vw_Cartera ORDER BY Nombre,Institucion;

-- Búsqueda por prefijo: admite el índice de nombre. Revisar plan real en SSMS (Ctrl+M).
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
SELECT Id,DUI,Nombre FROM dbo.Clientes WHERE Nombre LIKE N'Ana%' ORDER BY Nombre,Id
OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY;
SELECT c.Id,c.DUI,c.Nombre FROM dbo.Afiliaciones a
JOIN dbo.Clientes c ON c.Id=a.ClienteId WHERE a.InstitucionId=1 ORDER BY c.Nombre;
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

-- Inventario de índices, incluidos PK y restricciones UNIQUE.
SELECT OBJECT_NAME(i.object_id) Tabla,i.name Indice,i.type_desc Tipo,i.is_unique Unico,
 COL_NAME(ic.object_id,ic.column_id) Columna,ic.key_ordinal OrdenClave,ic.is_included_column Incluida
FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
WHERE OBJECT_SCHEMA_NAME(i.object_id)='dbo' AND i.name IS NOT NULL ORDER BY Tabla,Indice,OrdenClave;

-- Uso acumulado desde el arranque del servidor. Puede requerir permiso VIEW SERVER PERFORMANCE STATE.
-- SELECT OBJECT_NAME(i.object_id) Tabla,i.name,ISNULL(u.user_seeks,0) Busquedas,
-- ISNULL(u.user_scans,0) Escaneos,ISNULL(u.user_updates,0) Actualizaciones
-- FROM sys.indexes i LEFT JOIN sys.dm_db_index_usage_stats u
-- ON u.database_id=DB_ID() AND u.object_id=i.object_id AND u.index_id=i.index_id
-- WHERE OBJECT_SCHEMA_NAME(i.object_id)='dbo';

-- Las siguientes comprobaciones deben devolver cero filas.
SELECT DUI,COUNT(*) Repeticiones FROM dbo.Clientes GROUP BY DUI HAVING COUNT(*)>1;
SELECT ClienteId,InstitucionId,COUNT(*) Repeticiones FROM dbo.Afiliaciones
GROUP BY ClienteId,InstitucionId HAVING COUNT(*)>1;
SELECT i.Id FROM dbo.Instituciones i LEFT JOIN dbo.Consorcios c ON c.Id=i.ConsorcioId WHERE c.Id IS NULL;
GO
