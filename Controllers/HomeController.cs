using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectAmaterasu.Models;
using System.Data.SqlClient;
using System.Diagnostics;
using ProjectAmaterasu.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System;
using Microsoft.AspNetCore.Http;

namespace ProjectAmaterasu.Controllers
{
    [Authorize(Roles = "Usuario_Comum")]
    public class HomeController : Controller
    {
        #region Inject
        private readonly IConfiguration configuration;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IConfiguration _configuration)
        {
            _logger = logger;
            configuration = _configuration;
        }
        #endregion

        public IActionResult Index()
        {
            return View();
        }

        #region Troca de Temas
        [Route("troca-de-tema/{tema}")]
        public IActionResult TrocaDeTemaAsync(int tema)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                _ = User.AddUpdateClaimAsync("Tema", tema.ToString());

                connection.Execute("Update Usuario SET Tema = @Tema WHERE Id = @Id", new { Tema = tema, id = User.Identity.GetSessionID() });

                AtualizaCookies();
            }
            return Redirect("~/Painel");
        }
        #endregion

        #region Atualiza Cookies
        protected async void AtualizaCookies()
        {
            var propriedadesDeAutenticacao = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTime.Now.ToLocalTime().AddHours(2),
                IsPersistent = true
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, HttpContext.User, propriedadesDeAutenticacao);
        }
        #endregion

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
