/* Ejecutar en SSMS. Crea una base nueva sin modificar otras bases. */
USE master;
GO
IF DB_ID(N'ConsorcioAcademico') IS NULL CREATE DATABASE ConsorcioAcademico;
GO
USE ConsorcioAcademico;
GO
SET XACT_ABORT ON;
IF OBJECT_ID('dbo.Consorcios') IS NOT NULL
 THROW 50000, 'La estructura ya existe. No se ha eliminado ni sobrescrito información.', 1;
GO
BEGIN TRANSACTION;
CREATE TABLE dbo.Consorcios (
 Id int IDENTITY PRIMARY KEY,
 Nombre nvarchar(100) NOT NULL UNIQUE, Version rowversion NOT NULL,
 CONSTRAINT CK_Consorcio_Nombre CHECK(LEN(TRIM(Nombre)) BETWEEN 3 AND 100)
);
CREATE TABLE dbo.Instituciones (
 Id int IDENTITY PRIMARY KEY,
 ConsorcioId int NOT NULL REFERENCES dbo.Consorcios(Id),
 Nombre nvarchar(100) NOT NULL UNIQUE,
 Tipo nvarchar(15) NOT NULL CHECK(Tipo IN (N'Banco',N'Cooperativa')),
 Fundacion date NOT NULL CHECK(Fundacion BETWEEN '17530101' AND CONVERT(date,GETDATE())), Version rowversion NOT NULL,
 CONSTRAINT CK_Institucion_Nombre CHECK(LEN(TRIM(Nombre)) BETWEEN 3 AND 100)
);
CREATE INDEX IX_Instituciones_Consorcio ON dbo.Instituciones(ConsorcioId) INCLUDE(Nombre,Tipo);
CREATE TABLE dbo.Departamentos(Id int IDENTITY PRIMARY KEY, Nombre nvarchar(60) NOT NULL UNIQUE);
CREATE TABLE dbo.Municipios(
 Id int IDENTITY PRIMARY KEY, DepartamentoId int NOT NULL REFERENCES dbo.Departamentos(Id),
 Nombre nvarchar(80) NOT NULL, UNIQUE(DepartamentoId,Nombre)
);
CREATE TABLE dbo.Clientes (
 Id int IDENTITY PRIMARY KEY,
 DUI varchar(10) NOT NULL UNIQUE,
 Nombre nvarchar(120) NOT NULL,
 Nacimiento date NOT NULL CHECK(Nacimiento BETWEEN '19000101' AND CONVERT(date,GETDATE())),
 Genero nvarchar(25) NOT NULL CHECK(Genero IN (N'Femenino',N'Masculino',N'Otro',N'Prefiero no indicarlo')),
 MunicipioId int NOT NULL REFERENCES dbo.Municipios(Id),
 Complemento nvarchar(250) NOT NULL, Version rowversion NOT NULL,
 CONSTRAINT CK_Cliente_DUI CHECK(LEN(DUI)=10 AND DUI COLLATE Latin1_General_100_BIN2 LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]-[0-9]'),
 CONSTRAINT CK_Cliente_Nombre CHECK(LEN(TRIM(Nombre)) BETWEEN 3 AND 120),
 CONSTRAINT CK_Cliente_Direccion CHECK(LEN(TRIM(Complemento)) BETWEEN 5 AND 250)
);
CREATE INDEX IX_Clientes_Nombre ON dbo.Clientes(Nombre,Id) INCLUDE(DUI,MunicipioId);
CREATE INDEX IX_Clientes_Municipio ON dbo.Clientes(MunicipioId);
CREATE TABLE dbo.Afiliaciones (
 ClienteId int NOT NULL REFERENCES dbo.Clientes(Id),
 InstitucionId int NOT NULL REFERENCES dbo.Instituciones(Id),
 Fecha datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT PK_Afiliaciones PRIMARY KEY(ClienteId,InstitucionId)
);
CREATE INDEX IX_Afiliaciones_Institucion ON dbo.Afiliaciones(InstitucionId,ClienteId);
CREATE TABLE dbo.Usuarios (
 Id int IDENTITY PRIMARY KEY, Nombre nvarchar(40) NOT NULL UNIQUE,
 ClaveHash varbinary(32) NOT NULL, Salt varbinary(16) NOT NULL,
 Rol nvarchar(20) NOT NULL CHECK(Rol IN(N'Administrador',N'Operador',N'Consulta')), Version rowversion NOT NULL
);
CREATE TABLE dbo.Auditoria (
 Id bigint IDENTITY PRIMARY KEY, Fecha datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 Usuario nvarchar(40) NOT NULL, Accion nvarchar(20) NOT NULL,
 Entidad nvarchar(30) NOT NULL, RegistroId int NULL
);
CREATE INDEX IX_Auditoria_Fecha ON dbo.Auditoria(Fecha DESC);
COMMIT;
GO
CREATE VIEW dbo.vw_Cartera AS
SELECT c.Id,c.DUI,c.Nombre,c.Nacimiento,c.Genero,d.Nombre Departamento,
 m.Nombre Municipio,c.Complemento,i.Id InstitucionId,i.Nombre Institucion,
 co.Id ConsorcioId,co.Nombre Consorcio,a.Fecha
FROM dbo.Clientes c JOIN dbo.Municipios m ON m.Id=c.MunicipioId
JOIN dbo.Departamentos d ON d.Id=m.DepartamentoId
LEFT JOIN dbo.Afiliaciones a ON a.ClienteId=c.Id
LEFT JOIN dbo.Instituciones i ON i.Id=a.InstitucionId
LEFT JOIN dbo.Consorcios co ON co.Id=i.ConsorcioId;
GO
