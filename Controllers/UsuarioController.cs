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
                // Verifique se o nome ou apelido já estão em uso
                var usuarioExistente = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Nome = @Nome OR Apelido = @Apelido", new { Nome = usuario.Nome, Apelido = usuario.Apelido }).FirstOrDefault();

                if (usuarioExistente == null || usuarioExistente.Id == User.Identity.GetSessionID())
                {
                    var nome = char.ToUpper(usuario.Nome.Split(' ')[0][0]) + usuario.Nome.Split(' ')[0].Substring(1).ToLower();
                    var sobrenome = char.ToUpper(usuario.Nome.Split(' ')[usuario.Nome.Split(' ').Count() - 1][0]) +
                        usuario.Nome.Split(' ')[usuario.Nome.Split(' ').Count() - 1].Substring(1).ToLower();
                    usuario.Nome = nome + " " + sobrenome;
                    usuario.Apelido = "+" + usuario.IntlNumber + " " + usuario.Apelido;

                    // Agora, verifique se o nome e o apelido são diferentes antes de atualizar
                    if (usuarioExistente == null || usuario.Nome != usuarioExistente.Nome || usuario.Apelido != usuarioExistente.Apelido)
                    {
                        connection.Execute("UPDATE Usuario SET Nome = @Nome, Apelido = @Apelido WHERE Id = @Id", new { usuario.Nome, Id = User.Identity.GetSessionID(), Apelido = usuario.Apelido });
                        _ = User.AddUpdateClaimAsync("Name", usuario.Nome);
                        TempData["AtualizaCadastro"] = "Cadastro atualizado com sucesso.";
                    }
                    else
                    {
                        TempData["DadosIguais"] = "Nenhum dado foi modificado.";
                    }
                }
                else
                {
                    TempData["CadastroExistente"] = "O nome ou apelido já está cadastrado.";
                }
                return Redirect("~/perfil");
            }
        }

    }
}
