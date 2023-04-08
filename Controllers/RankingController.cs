using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjectAmaterasu.Extensions;
using ProjectAmaterasu.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ProjectAmaterasu.Controllers
{
    public class RankingController : Controller
    {
        #region Inject
        private readonly IConfiguration configuration;

        public RankingController(IConfiguration _configuration)
        {
            configuration = _configuration;
        }
        #endregion

        [Authorize(Roles = "Usuario_Comum")]
        [Route("ranking")]
        public IActionResult Index()
        {
            return View();
        }

        public JsonResult getRankings()
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
                    pontuacao = x.Pontuacao * x.Vitorias / (x.Vitorias + x.Derrotas),
                }).Distinct().ToList();
                return Json(new { data = usuario });
            }
        }


        [Authorize(Roles = "Usuario_Comum")]
        [Route("historico")]
        public IActionResult Historico()

        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var usuario = connection.Query<UsuarioViewModels>(@"SELECT DISTINCT Data FROM Historico").OrderByDescending(x => x.Data).ToList();

                return View(usuario);
            }
        }

        [Authorize(Roles = "Usuario_Comum")]
        [Route("historico/{data}")]
        public IActionResult HistoricoFiltrado(DateTime data)
        {

            TempData["Data"] = data;
            return View();
        }

        public JsonResult getHistoricoPorData(DateTime data)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var historico = connection.Query<HistoricoModels>(@"SELECT * FROM Historico WHERE Data = @Data", new { Data = data }).Select(x => new
                {
                    id = x.Id,
                    nome = x.Nome_Usuario,
                    vitorias = x.Vitorias,
                    jogos = x.Jogos,
                    derrotas = x.Derrotas,
                    pontuacao = x.Pontuacao * x.Vitorias / (x.Vitorias + x.Derrotas),
                    desempenho = x.Desempenho,
                }).ToList();

                return Json(new { data = historico });
            }
        }

        public JsonResult getCampeoes()
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var datas = connection.Query<HistoricoModels>(@"SELECT DISTINCT Data FROM Historico").ToList();


                var historico = connection.Query<HistoricoModels>(@"SELECT * FROM Historico").Select(x => new
                {
                    id = x.Id,
                    nome = x.Nome_Usuario,
                    vitorias = x.Vitorias,
                    jogos = x.Jogos,
                    derrotas = x.Derrotas,
                    pontuacao = x.Pontuacao * x.Vitorias / (x.Vitorias + x.Derrotas),
                    desempenho = x.Desempenho,
                    data = x.Data
                }).ToList();

                var lista = new List<HistoricoModels>();

                foreach (var item in datas)
                {
                    var mensal = historico.Where(x => x.data == item.Data).OrderByDescending(x => x.pontuacao).FirstOrDefault();

                    var campeao = new HistoricoModels()
                    {
                        Id = mensal.id,
                        Nome_Usuario = mensal.nome,
                        Vitorias = mensal.vitorias,
                        Jogos = mensal.jogos,
                        Derrotas = mensal.derrotas,
                        Pontuacao = mensal.pontuacao,
                        Desempenho = mensal.desempenho,
                        Data = mensal.data
                    };



                    lista.Add(campeao);
                }
                var campeaomensal = lista.Select(x => new
                {
                    id = x.Id,
                    nome = x.Nome_Usuario,
                    vitorias = x.Vitorias,
                    jogos = x.Jogos,
                    derrotas = x.Derrotas,
                    pontuacao = x.Pontuacao,
                    desempenho = x.Desempenho,
                    data = x.Data.ToString("yyyy/MM").ToUpper()
                }).ToList();

                return Json(new { data = campeaomensal });
            }
        }
    }
}
