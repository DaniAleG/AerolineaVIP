/* =====================================================================================
   Script_Complemento_Identity.sql  (OPCIONAL)
   Amplía AspNetUsers con las columnas Nombres y Apellidos que usa ApplicationUser
   (pantalla 3, registro). La aplicación lo hace automáticamente al iniciar
   (InicializadorIdentity.PrepararBase); este script permite hacerlo a mano en SSMS.
   Ejecutar DESPUÉS de Script_BD_AerolineaVIP.sql. Es re-ejecutable.
===================================================================================== */
USE AerolineaVIP_Identity;
GO
IF COL_LENGTH(N'dbo.AspNetUsers', N'Nombres') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD Nombres NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'Apellidos') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD Apellidos NVARCHAR(100) NULL;
GO
PRINT N'OK  AspNetUsers ampliada con Nombres y Apellidos.';
GO
