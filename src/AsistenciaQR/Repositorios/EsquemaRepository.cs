using Microsoft.Data.SqlClient;

namespace AsistenciaQR.Repositorios
{
    /// <summary>
    /// Aplica al arrancar los cambios de base de datos de versiones
    /// nuevas, para que un colegio que actualiza el programa no tenga
    /// que correr scripts a mano. Cada cambio revisa primero si ya
    /// existe, asi que ejecutarlo varias veces no hace dano. Debe
    /// coincidir con los scripts numerados de la carpeta database/.
    /// </summary>
    public static class EsquemaRepository
    {
        private static readonly string[] Actualizaciones =
        {
            // 006_seguridad_usuarios.sql
            @"if col_length('Usuarios', 'IntentosFallidos') is null
                alter table Usuarios add IntentosFallidos int not null
                    constraint df_Usuarios_IntentosFallidos default (0);",
            @"if col_length('Usuarios', 'BloqueadoHasta') is null
                alter table Usuarios add BloqueadoHasta datetime null;",
            @"if col_length('Usuarios', 'DebeCambiarContrasena') is null
                alter table Usuarios add DebeCambiarContrasena bit not null
                    constraint df_Usuarios_DebeCambiarContrasena default (0);"
        };

        public static void AplicarActualizaciones()
        {
            using var conexion = ConexionSQL.ObtenerConexion();
            conexion.Open();

            foreach (string sql in Actualizaciones)
            {
                using var comando = new SqlCommand(sql, conexion);
                comando.ExecuteNonQuery();
            }
        }
    }
}
