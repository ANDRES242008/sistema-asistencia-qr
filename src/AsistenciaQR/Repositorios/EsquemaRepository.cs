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
                    constraint df_Usuarios_DebeCambiarContrasena default (0);",

            // 007_bitacora.sql
            @"if object_id('Bitacora', 'U') is null
              begin
                  create table Bitacora (
                      BitacoraId int identity(1,1) not null,
                      Fecha datetime not null constraint df_Bitacora_Fecha default (getdate()),
                      Usuario varchar(50) not null,
                      Accion varchar(60) not null,
                      Detalle nvarchar(500) not null,
                      constraint pk_Bitacora primary key (BitacoraId)
                  );
                  create index ix_Bitacora_Fecha on Bitacora (Fecha desc);
              end",

            // 008_institucion.sql
            @"if object_id('ConfiguracionInstitucion', 'U') is null
                create table ConfiguracionInstitucion (
                    ConfiguracionInstitucionId int not null
                        constraint pk_ConfiguracionInstitucion primary key
                        constraint ck_ConfiguracionInstitucion_UnaFila check (ConfiguracionInstitucionId = 1),
                    Nombre nvarchar(80) not null,
                    Logo varbinary(max) null,
                    ColorPrincipal char(7) null,
                    FechaActualizacion datetime not null
                        constraint df_ConfiguracionInstitucion_Fecha default (getdate())
                );"
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
