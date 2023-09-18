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
                // Obtenha os dados do usuário atual
                var usuarioAtual = connection.Query<UsuarioModels>("SELECT * FROM Usuario WHERE Id = @Id", new { Id = User.Identity.GetSessionID() }).FirstOrDefault();

                // Verifique se o novo email é único
                var emailExistente = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Email = @Email AND Id != @Id", new { Email = usuario.Email, Id = User.Identity.GetSessionID() }).FirstOrDefault();

                // Verifique se o novo nome é único
                var nomeExistente = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Nome = @Nome AND Id != @Id", new { Nome = usuario.Nome, Id = User.Identity.GetSessionID() }).FirstOrDefault();

                // Verifique se o novo apelido é único
                var apelidoExistente = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Apelido = @Apelido AND Id != @Id", new { Apelido = "+" + usuario.IntlNumber + " " + usuario.Apelido, Id = User.Identity.GetSessionID() }).FirstOrDefault();

                if (usuarioAtual != null &&
                    (emailExistente == null || emailExistente.Id == usuarioAtual.Id) &&
                    (nomeExistente == null || nomeExistente.Id == usuarioAtual.Id) &&
                    (apelidoExistente == null || apelidoExistente.Id == usuarioAtual.Id))
                {
                    var nome = char.ToUpper(usuario.Nome.Split(' ')[0][0]) + usuario.Nome.Split(' ')[0].Substring(1).ToLower();
                    var sobrenome = char.ToUpper(usuario.Nome.Split(' ')[usuario.Nome.Split(' ').Count() - 1][0]) +
                        usuario.Nome.Split(' ')[usuario.Nome.Split(' ').Count() - 1].Substring(1).ToLower();
                    usuario.Nome = nome + " " + sobrenome;
                    usuario.Apelido = "+" + usuario.IntlNumber + " " + usuario.Apelido;

                    // Verifique se algum dado foi modificado
                    if (usuario.Nome != usuarioAtual.Nome || usuario.Apelido != usuarioAtual.Apelido || usuario.Email != usuarioAtual.Email)
                    {
                        // Atualize os campos que foram modificados
                        var updateQuery = "UPDATE Usuario SET Nome = @Nome, Apelido = @Apelido, Email = @Email WHERE Id = @Id";
                        connection.Execute(updateQuery, new { usuario.Nome, Id = User.Identity.GetSessionID(), Apelido = usuario.Apelido, Email = usuario.Email });
                        _ = User.AddUpdateClaimAsync("Name", usuario.Nome);
                        TempData["AtualizaCadastro"] = "Cadastro atualizado com sucesso.";
                    }
                    else
                    {
                        TempData["DadosIguais"] = "Nenhum dado foi modificado.";
                    }
                }
                else if (emailExistente != null)
                {
                    TempData["CadastroExistente"] = "O email já está cadastrado.";
                }
                else if (nomeExistente != null)
                {
                    TempData["CadastroExistente"] = "O nome já está cadastrado.";
                }
                else
                {
                    TempData["CadastroExistente"] = "O apelido já está cadastrado.";
                }
                return Redirect("~/perfil");
            }
        }

    }
}
