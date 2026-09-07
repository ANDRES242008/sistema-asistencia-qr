namespace AsistenciaQR.Modelos
{
    /// <summary>
    /// Los resultados posibles al intentar registrar una asistencia,
    /// ya sea por QR o por el respaldo manual con NIE. Se separan
    /// CodigoNoValido (QR) y EstudianteNoEncontrado (NIE manual)
    /// para que el mensaje en pantalla tenga sentido en cada caso.
    /// </summary>
    public enum ResultadoEscaneo
    {
        Exitoso,
        YaRegistradoHoy,
        CodigoNoValido,
        EstudianteNoEncontrado
    }
}
