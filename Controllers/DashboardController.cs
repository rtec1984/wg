using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjectAmaterasu.Extensions;
using ProjectAmaterasu.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ProjectAmaterasu.Controllers
{
    public class DashboardController : Controller
    {
        #region Inject
        private readonly IConfiguration configuration;

        public DashboardController(IConfiguration _configuration)
        {
            configuration = _configuration;
        }
        #endregion
        [Authorize(Roles = "Usuario_Comum")]
        [Route("painel")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Administrador")]
        [Route("painel-administrador")]
        public IActionResult PainelAdministrador()
        {
            return View();
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult ResetarRanking()
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var usuario = connection.Query<UsuarioViewModels>(@"SELECT Usuario.Id, Usuario.Nome,
                                                                    (SELECT COUNT(Partida.Id) FROM Partida WHERE Id_Usuario_2 = Usuario.Id 
                                                                     OR Id_Usuario_3 = Usuario.Id OR Id_Usuario_4 = Usuario.Id OR Id_Usuario_5 = Usuario.Id OR Id_Usuario_6 = Usuario.Id) as Derrotas,
                                                                    (SELECT COUNT(Partida.Id) FROM Partida WHERE Id_Usuario = Usuario.Id) as Vitorias,
                                                                    (SELECT SUM(Partida.Pontuacao) FROM Partida WHERE Id_Usuario = Usuario.Id) as Pontuacao
                                                                    FROM Usuario
                                                                    INNER JOIN Partida on Id_Usuario = Usuario.Id OR Id_Usuario_2 = Usuario.Id 
                                                                     OR Id_Usuario_3 = Usuario.Id OR Id_Usuario_4 = Usuario.Id OR Id_Usuario_5 = Usuario.Id OR Id_Usuario_6 = Usuario.Id").Select(x => new
                {
                    id = x.Id,
                    nome = x.Nome,
                    vitorias = x.Vitorias,
                    jogos = x.Vitorias + x.Derrotas,
                    derrotas = x.Derrotas,
                    pontuacao = x.Pontuacao,
                    desempenho = CalcularDesempenho(x.Vitorias, x.Derrotas)
                }).Distinct().ToList().OrderByDescending(x => x.pontuacao).ThenByDescending(x => x.desempenho).ThenByDescending(x => x.vitorias).ThenBy(x => x.jogos);

                var inserir = connection.Execute(@"INSERT INTO Historico (Id_Usuario, Nome_Usuario, Vitorias, Jogos, Derrotas, Pontuacao, Desempenho) 
                                     VALUES (@id, @nome, @vitorias, @jogos, @derrotas, @pontuacao, @desempenho)", usuario);

                if (inserir == usuario.Count())
                {
                    connection.Execute(@"TRUNCATE TABLE Partida");
                }

                return Redirect("~/painel-administrador");
            }
        }

        protected int CalcularDesempenho(double vitorias, double derrotas)
        {
            double jogos = vitorias + derrotas;
            double parcial= vitorias / jogos;
            int desempenho = Convert.ToInt32(parcial * 100);
            return desempenho;
        }
    }
}
