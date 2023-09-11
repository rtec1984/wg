using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjectAmaterasu.Extensions;
using ProjectAmaterasu.Models;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace ProjectAmaterasu.Controllers
{
    public class PartidaController : Controller
    {
        #region Inject
        private readonly IConfiguration configuration;

        public PartidaController(IConfiguration _configuration)
        {
            configuration = _configuration;
        }
        #endregion

        [Authorize(Roles = "Usuario_Comum")]
        [Route("partida")]
        public IActionResult Index()
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var Partidas = connection.Query<PartidaModels>(@"SELECT
                                                                Id, Data, Duracao, Id_Usuario,
                                                                (SELECT Nome FROM Usuario WHERE Usuario.Id = Id_Usuario) as Nome,
                                                                (SELECT Nome FROM Usuario WHERE Usuario.Id = Id_Usuario_2) as Nome_2,
                                                                (SELECT Nome FROM Usuario WHERE Usuario.Id = Id_Usuario_3) as Nome_3,
                                                                (SELECT Nome FROM Usuario WHERE Usuario.Id = Id_Usuario_4) as Nome_4,
                                                                (SELECT Nome FROM Usuario WHERE Usuario.Id = Id_Usuario_5) as Nome_5,
                                                                (SELECT Nome FROM Usuario WHERE Usuario.Id = Id_Usuario_6) as Nome_6
                                                                FROM Partida").OrderByDescending(x => x.Id).ToList();

                return View(Partidas);
            }
        }


        [Authorize(Roles = "Usuario_Comum")]
        [Route("criar-partida")]
        [Route("alterar-partida/{id}")]
        public IActionResult CriarPartida(int id)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var Partida = connection.Query<PartidaModels>(@"SELECT * FROM Partida WHERE Id = @Id AND Id_Usuario = @Id_Usuario", new { Id = id, Id_Usuario = User.Identity.GetSessionID() }).FirstOrDefault();
                var Usuario = connection.Query<UsuarioModels>(@"SELECT Id, Nome, Apelido FROM Usuario").OrderBy(x => x.Nome).ToList();
                ViewData["Usuario"] = Usuario;
                if (Partida != null)
                {
                    return View(Partida);
                }
                else
                {
                    return View();
                }
            }
        }

        [Authorize(Roles = "Usuario_Comum")]
        public IActionResult SalvarPartida(PartidaModels partida)
        {
            if (partida.Duracao < 0)
            {
                TempData["ErrorMessage"] = "A duração da partida não pode ser negativa.";
                return RedirectToAction("Index");
            }

            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                partida.Id_Usuario = User.Identity.GetSessionID();
                partida.Pontuacao = CalcularPontuacao(partida.Duracao);

                var Partida = connection.Query<PartidaModels>(@"SELECT * FROM Partida WHERE Id = @Id AND Id_Usuario = @Id_Usuario", new { Id = partida.Id, Id_Usuario = partida.Id_Usuario }).FirstOrDefault();
                if (Partida != null)
                {
                    connection.Execute(@"UPDATE Partida SET Id_Usuario_2 = @Id_Usuario_2, Id_Usuario_3 = @Id_Usuario_3, Id_Usuario_4 = @Id_Usuario_4, Id_Usuario_5 = @Id_Usuario_5, Id_Usuario_6 = @Id_Usuario_6, Duracao = @Duracao, Pontuacao = @Pontuacao WHERE Id = @Id", partida);
                    TempData["SuccessMessage"] = "Partida atualizada com sucesso.";

                }
                else if (partida.Data.AddMonths(1) > DateTime.Now && partida.Data <= DateTime.Now)
                {
                    connection.Execute(@"INSERT INTO Partida (Id_Usuario, Id_Usuario_2, Id_Usuario_3, Id_Usuario_4, Id_Usuario_5, Id_Usuario_6, Data, Duracao, Pontuacao) 
                                VALUES (@Id_Usuario, @Id_Usuario_2, @Id_Usuario_3, @Id_Usuario_4, @Id_Usuario_5, @Id_Usuario_6, @Data, @Duracao, @Pontuacao)", partida);
                    TempData["SuccessMessage"] = "Partida criada com sucesso.";

                }
                else
                {
                    TempData["ErrorMessage"] = "A data da partida não é válida.";

                }
            }

            return RedirectToAction("Index");
        }


        protected int CalcularPontuacao(int duracao)
        {
            int pontuacao = 0;
            if (duracao == 0)
            {
                pontuacao = 0;
            }
            else if (duracao <= 30)
            {
                pontuacao = 40;
            }
            else if (duracao > 30 && duracao <= 60)
            {
                pontuacao = 30;
            }
            else if (duracao > 60 && duracao <= 90)
            {
                pontuacao = 20;
            }
            else
            {
                pontuacao = 10;
            }
            return pontuacao;
        }

        [Authorize(Roles = "Usuario_Comum")]
        [Route("deletar-partida/{id}")]
        public IActionResult DeletarPartida(int id)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var Partida = connection.Query<PartidaModels>(@"SELECT * FROM Partida WHERE Id_Usuario = @Id_Usuario AND Id = @Id", new { Id_Usuario = User.Identity.GetSessionID(), Id = id }).FirstOrDefault();
                if (Partida != null)
                {
                    connection.Execute(@"DELETE FROM Partida WHERE Id = @id", new { id });
                    TempData["SuccessMessage"] = "Partida deletada com sucesso.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Partida não encontrada ou você não tem permissão para excluí-la.";

                }
            }

            return RedirectToAction("Index");
        }

    }
}
