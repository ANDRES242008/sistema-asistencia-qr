namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Los 3 resultados posibles al intentar registrar una asistencia,
    /// ya sea por QR o por el respaldo manual con NIE.
    /// </summary>
    public enum ResultadoEscaneo
    {
        Exitoso,
        YaRegistradoHoy,
        CodigoNoValido
    }
}
