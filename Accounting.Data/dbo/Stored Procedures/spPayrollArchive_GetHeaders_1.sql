﻿CREATE PROCEDURE [dbo].[spPayrollArchive_GetHeaders]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, UniqueId, Opis, SatiRada, SatiPraznika, DatumOd, DatumDo, DatumObracuna
	FROM PayrollArchiveHeader
END