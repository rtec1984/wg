using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectAmaterasu.Extensions;
using ProjectAmaterasu.Models;
using System.Data.SqlClient;
using System.Linq;

namespace ProjectAmaterasu.Controllers
{
    public class UsuarioController : Controller
    {
        #region Inject
        private readonly IConfiguration configuration;

        public UsuarioController(IConfiguration _configuration)
        {
            configuration = _configuration;
        }
        #endregion

        [Authorize(Roles = "Usuario_Comum")]
        [Route("perfil")]
        public IActionResult Index()
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var Usuario = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Id = @Id", new { Id = User.Identity.GetSessionID() }).FirstOrDefault();

                return View(Usuario);
            }
        }

        [Authorize(Roles = "Usuario_Comum")]
        public IActionResult SalvarUsuario(UsuarioModels usuario)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var UsuarioVerificacao = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Apelido = @Apelido", new { apelido = usuario.Apelido }).FirstOrDefault();

                if (UsuarioVerificacao == null && usuario.Nome.Split(' ').Count() > 1)
                {
                    usuario.Nome = usuario.Nome.Split(' ')[0] + " " + usuario.Nome.Split(' ')[usuario.Nome.Split(' ').Count() - 1];

                    connection.Execute("UPDATE Usuario SET Nome = @Nome, Apelido = @Apelido WHERE Id = @Id", new { usuario.Nome, Id = User.Identity.GetSessionID(), Apelido = usuario.Apelido });

                    _ = User.AddUpdateClaimAsync("Name", usuario.Nome);
                }

                return Redirect("~/perfil");
            }
        }
    }
}
