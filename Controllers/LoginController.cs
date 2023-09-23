using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjectAmaterasu.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using MimeKit.Text;
using ProjectAmaterasu.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace ProjectAmaterasu.Controllers
{
    public class LoginController : Controller
    {
        #region Inject
        private readonly IConfiguration configuration;

        public LoginController(IConfiguration _configuration)
        {
            configuration = _configuration;
        }
        #endregion

        #region Index
        [Route("/")]
        [Route("/{ReturnURL}")]
        public IActionResult Index(string ReturnURL)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/painel");
            }
            else
            {
                return View();
            }
        }
        #endregion

        #region Login Administrador
        [Route("login-administrador")]
        [Route("login-administrador/{ReturnURL}")]
        public IActionResult Admin(string ReturnURL)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/painel");
            }
            else
            {
                return View();
            }
        }
        #endregion

        #region Verificar Administrador (Login)
        public IActionResult VerificarAdministrador(UsuarioModels login, string returnUrl)
        {
            try
            {
                login.Email = login.Email.ToLower();

                // Validar o modelo
                if (!ModelState.IsValid)
                {
                    TempData["ContaExistente"] = "Usuário ou senha inválidos!";
                    return Redirect("~/login-administrador");
                }

                using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    var usuario = connection.QueryFirstOrDefault<UsuarioModels>(
                        @"SELECT * FROM Usuario WHERE Email = @Email",
                        new { Email = login.Email });

                    if (usuario != null)
                    {
                        if (usuario.SenhaTemporaria)
                        {
                            TempData["EmailCadastrado"] = usuario.Email;
                            TempData["ContaExistente"] = "Esta conta se encontra em processo de mudança de senha, verifique o e-mail:<br> " +
                                "<label id='EmailCadastrado'></label>, para mais instruções ou altere sua senha <a href='/esqueci-a-senha'>clicando aqui!</a>";
                            return Redirect("~/login-administrador");
                        }

                        bool verificado = BCrypt.Net.BCrypt.Verify(login.Email + "salt" + login.Senha, usuario.Senha);
                        if (verificado)
                        {
                            if (usuario.Administrador == true) // Verifica se o usuário é um administrador
                            {
                                AutenticaUsuario(usuario);
                                if (!String.IsNullOrEmpty(returnUrl))
                                {
                                    return Redirect(System.Net.WebUtility.HtmlEncode(returnUrl));
                                }
                                else
                                {
                                    return Redirect("~/painel-administrador");
                                }
                            }
                            else
                            {
                                TempData["ContaExistente"] = "Você não tem permissão de administrador para fazer login.";
                                return Redirect("~/login-administrador");
                            }
                        }
                    }
                }

                TempData["ContaExistente"] = "Usuário ou senha inválidos!";
                return Redirect("~/login-administrador");
            }
            catch
            {
                if (!String.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(System.Net.WebUtility.HtmlEncode(returnUrl));
                }
                else
                {
                    return Redirect("/");
                }
            }
        }
        #endregion

        #region Verificar Usuário (Login)
        public IActionResult VerificarUsuario(UsuarioModels Login, string ReturnUrl)
        {
            try
            {
                using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    Login.Email = Login.Email.ToLower();
                    var Usuario = connection.Query<UsuarioModels>(@"SELECT *, LOWER(Email) FROM Usuario where @Email = Email", new { email = Login.Email }).FirstOrDefault();

                    if (Usuario.SenhaTemporaria)
                    {
                        TempData["EmailCadastrado"] = Usuario.Email;
                        TempData["ContaExistente"] = "Esta conta se encontra em processo de mudança de senha, verifique o e-mail:<br> " +
                            "<label id='EmailCadastrado'></label>, para mais instruções!";
                        return Redirect("/");
                    }
                    else if (Usuario != null)
                    {
                        bool verificado = BCrypt.Net.BCrypt.Verify(Login.Email.ToLower() + "salt" + Login.Senha, Usuario.Senha);
                        if (verificado)
                        {
                            Usuario.Administrador = false;
                            AutenticaUsuario(Usuario);
                            if (!String.IsNullOrEmpty(ReturnUrl))
                            {
                                return Redirect(System.Net.WebUtility.HtmlEncode(ReturnUrl));
                            }
                            else
                            {
                                return Redirect("~/painel");
                            }
                        }
                        else
                        {
                            TempData["ContaExistente"] = "Usuário ou senha inválidos!";
                            return Redirect("/");
                        }
                    }
                    else
                    {
                        TempData["ContaExistente"] = "Usuário ou senha inválidos!";
                        return Redirect("/");
                    }
                }
            }
            catch
            {
                if (!String.IsNullOrEmpty(ReturnUrl))
                {
                    return Redirect(System.Net.WebUtility.HtmlEncode(ReturnUrl));

                }
                else
                {
                    return Redirect("/");
                }
            }

        }
        #endregion

        #region Função de autentificação de usuário
        protected async void AutenticaUsuario(UsuarioModels Login)
        {
            var NomeSeparado = Login.Nome.Trim().Split(' ');
            var Nome = "";
            var Sobrenome = "";
            var Iniciais = "";
            if (NomeSeparado.Count() > 1)
            {
                Nome = NomeSeparado[0].ToCharArray()[0].ToString();
                Sobrenome = NomeSeparado[1].ToCharArray()[0].ToString();
                Iniciais = Nome + Sobrenome;
            }
            else
            {
                Iniciais = NomeSeparado[0].ToCharArray()[0].ToString();
            }

            if (!Login.Administrador)
            {
                var claims = new List<System.Security.Claims.Claim>
                {
                    new Claim(ClaimTypes.Email, Login.Email),
                    new Claim(ClaimTypes.Name, Login.Nome),
                    new Claim(ClaimTypes.Role, "Usuario_Comum"),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, Login.Email)
                };
                claims.Add(new Claim(type: "Assinatura", value: ""));
                claims.Add(new Claim(type: "SessionID", value: Login.Id.ToString()));
                claims.Add(new Claim(type: "Iniciais", value: Iniciais));
                claims.Add(new Claim(type: "Tema", value: Login.Tema));

                var identidadeDeUsuario = new ClaimsIdentity(claims, "Login");
                ClaimsPrincipal claimPrincipal = new ClaimsPrincipal(identidadeDeUsuario);

                var propriedadesDeAutenticacao = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTime.Now.ToLocalTime().AddDays(3),
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal, propriedadesDeAutenticacao);
            }
            else
            {
                var claims = new List<System.Security.Claims.Claim>
                {
                    new Claim(ClaimTypes.Email, Login.Email),
                    new Claim(ClaimTypes.Name, Login.Nome),
                    new Claim(ClaimTypes.Role, "Administrador"),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, Login.Email)
                };
                claims.Add(new Claim(type: "Assinatura", value: ""));
                claims.Add(new Claim(type: "SessionID", value: Login.Id.ToString()));
                claims.Add(new Claim(type: "Iniciais", value: Iniciais));
                claims.Add(new Claim(type: "Tema", value: Login.Tema));

                var identidadeDeUsuario = new ClaimsIdentity(claims, "Login");
                ClaimsPrincipal claimPrincipal = new ClaimsPrincipal(identidadeDeUsuario);

                var propriedadesDeAutenticacao = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTime.Now.ToLocalTime().AddHours(2),
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal, propriedadesDeAutenticacao);
            }
        }
        #endregion

        #region Cadastre-se
        [Route("cadastre-se")]
        public IActionResult Cadastro()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/painel");
            }
            else
            {
                return View();
            }
        }
        #endregion

        #region Salvar
        [HttpPost]
        public IActionResult Salvar(UsuarioModels usuario)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/painel");
            }
            else
            {
                if (usuario.Nome.Split(' ').Count() > 1)
                {
                    var nome = char.ToUpper(usuario.Nome.Split(' ')[0][0]) + usuario.Nome.Split(' ')[0].Substring(1).ToLower();
                    var sobrenome = char.ToUpper(usuario.Nome.Split(' ')[usuario.Nome.Split(' ').Count() - 1][0]) +
                        usuario.Nome.Split(' ')[usuario.Nome.Split(' ').Count() - 1].Substring(1).ToLower();
                    usuario.Nome = nome + " " + sobrenome;
                    usuario.Apelido = "+" + usuario.IntlNumber + " " + usuario.Apelido;
                }
                else
                {
                    return Redirect("/cadastre-se");
                }

                using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    usuario.Email = usuario.Email.ToLower();

                    // Verificar se o nome já existe no banco de dados
                    var nomeExistente = connection.Query<UsuarioModels>(@"SELECT 1 FROM Usuario WHERE Nome = @Nome", new { nome = usuario.Nome }).FirstOrDefault();

                    if (nomeExistente != null)
                    {
                        TempData["NomeExistente"] = "Esse nome já está em uso, escolha outro.";
                        return Redirect("/cadastre-se");
                    }

                    // Verificar se o email ou apelido já existem no banco de dados
                    var UsuarioVerificacao = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Email = @Email OR Apelido = @Apelido", new { email = usuario.Email, apelido = usuario.Apelido }).FirstOrDefault();

                    if (UsuarioVerificacao == null)
                    {
                        var criptografia = BCrypt.Net.BCrypt.HashPassword(usuario.Email + "salt" + usuario.Senha, workFactor: 12);
                        usuario.Senha = criptografia;
                        connection.Execute("INSERT INTO Usuario (Nome, Email, Senha, Tema, Apelido) Values (@Nome, @Email, @Senha, '2', @Apelido)", usuario);
                        // Recupere o modelo de email de boas-vindas do banco de dados.
                        var emailBoasVindas = connection.Query<SMTPModels>(@"SELECT * FROM SMTP WHERE TipoEmail = 'Boas Vindas'").FirstOrDefault();

                        if (emailBoasVindas != null)
                        {
                            var email = new MimeMessage();
                            email.From.Add(new MailboxAddress("Contato WG", emailBoasVindas.UsuarioServidor)); // Nome do remetente como "Suporte WG"
                            email.To.Add(MailboxAddress.Parse(usuario.Email));
                            email.Subject = "Boas vindas ao Sistema WG";

                            // Construa o corpo do email de boas-vindas (personalize conforme necessário).
                            email.Body = new TextPart(TextFormat.Html)
                            {
                                Text = $"<h1><center><b>Olá, {usuario.Nome}! 👋</b><br /><br /><b>Boas vindas ao Sistema WG! 🎉<br /><br />Agradecemos por se cadastrar em nosso <a href='https://wgfm.azurewebsites.net/'>Sistema</a>. Esperamos que tenha uma ótima experiência! 😎<br /><br />Siga nosso <a href=\"https://www.instagram.com/war_grow\">Instagram</a> e se ainda não faz parte do nosso grupo no <a href=\"https://chat.whatsapp.com/IdVAJ8OQHnJ7bXYRdXGQIr\">WhatsApp</a> participe! 🌟<br /><br />Atenciosamente,<br /><br />🎲 WAR-GROW 💣</b></center></h1>"
                            };

                            using (var smtp = new SmtpClient())
                            {
                                smtp.Connect(emailBoasVindas.HostServidor, emailBoasVindas.Porta);
                                smtp.Authenticate(emailBoasVindas.UsuarioServidor, emailBoasVindas.Senha);
                                smtp.Send(email);
                                smtp.Disconnect(true);
                            }
                        }

                        TempData["ValidacaoSenha"] = "Cadastro realizado com sucesso!";
                    }
                    else
                    {
                        TempData["ContaExistente"] = "Esse e-mail ou WhatsApp já está cadastrado, faça login ou tente novamente!";
                    }
                    return Redirect("/");
                }
            }
        }
        #endregion

        #region Esqueci Minha Senha
        [Route("esqueci-a-senha")]
        public IActionResult EsqueciASenha()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/painel");
            }
            else
            {
                return View();
            }
        }
        #endregion

        #region Função de esqueci minha senha
        [HttpPost]
        public IActionResult EsqueciASenha(string Email)
        {
            var chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789_-=!@";
            var random = new Random();
            var codigoverificacao = new string(Enumerable.Repeat(chars, 40).Select(s => s[random.Next(s.Length)]).ToArray());

            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var SMTP = connection.Query<SMTPModels>(@"SELECT * FROM SMTP where TipoEmail = 'Esqueci Minha Senha'").FirstOrDefault();
                var Usuario = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario where email = @email", new { email = Email }).FirstOrDefault();
                if (Usuario != null)
                {
                    connection.Execute("Update Usuario SET senha = @senha, SenhaTemporaria = 1 where Id = @Id", new { senha = codigoverificacao, Id = Usuario.Id });

                    var email = new MimeMessage();

                    // Cria um objeto MailboxAddress personalizado para o remetente
                    var remetente = new MailboxAddress("Suporte WG", "suporte@wg.com");
                    email.From.Add(remetente);

                    // Define o destinatário do e-mail com base no endereço de e-mail do usuário.
                    email.To.Add(MailboxAddress.Parse(Usuario.Email));

                    // Define o assunto do e-mail.
                    email.Subject = "Solicitação de troca de senha.";

                    // Substitua a URL no corpo do e-mail pelo texto de link amigável.
                    string corpoEmail = SMTP.CorpoEmail;

                    string linkAmigavel = $"<h1><center><b>Olá, {Usuario.Nome}! 👋<br /><br />Você solicitou a troca de senha. 😬<br /><br /><a href='https://wgfm.azurewebsites.net/esqueci-a-senha-confirmacao/{codigoverificacao}'>Clique aqui</a> para definir sua nova senha. 😎<br /><br />Atenciosamente,<br /><br /><b>🎲 WAR-GROW 💣</b></center></h1>";

                    // Agora substitua a parte do corpo do e-mail onde você deseja que o link seja exibido.
                    corpoEmail = corpoEmail.Replace("{1}", linkAmigavel);

                    // Defina o corpo do e-mail como HTML com o texto processado acima.
                    email.Body = new TextPart(TextFormat.Html)
                    {
                        Text = corpoEmail
                    };

                    using (var smtp = new SmtpClient())
                    {
                        smtp.Connect(SMTP.HostServidor, SMTP.Porta);
                        smtp.Authenticate(SMTP.UsuarioServidor, SMTP.Senha);
                        smtp.Send(email);
                        smtp.Disconnect(true);
                    }
                    TempData["ValidacaoSenha"] = "Um e-mail com as instruções para troca de senha foi encaminhado para seu e-mail: <label id='EmailCadastrado'></label>.";
                    TempData["EmailCadastrado"] = Usuario.Email;
                }
                else
                {
                    TempData["ContaExistente"] = "Email não cadastrado, favor digitar novamente ou avisar o administrador do grupo WAR-GROW.";
                }
            }

            return Redirect("/");
        }
        #endregion

        #region Esqueci Minha Senha Confirmação
        [Route("esqueci-a-senha-confirmacao/{CodigoConfirmacao}")]
        public IActionResult EsqueciASenhaConfirmacao(string CodigoConfirmacao)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/painel");
            }
            else
            {
                using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    var Usuario = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario where senha = @senha", new { senha = CodigoConfirmacao }).FirstOrDefault();
                    if (Usuario != null)
                    {
                        TempData["EmailMudarSenha"] = Usuario.Email;
                        TempData["CodigoConfirmacao"] = CodigoConfirmacao;
                        ViewData["Validacao"] = "true";
                        return Redirect("~/mudar-senha");
                    }
                    else
                    {
                        return Redirect("~/");
                    }

                }
            }
        }
        #endregion

        #region Mudar a Senha
        [Route("mudar-senha")]
        public IActionResult MudarSenha()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/painel");
            }
            else if (ViewData["Validacao"] != null)
            {
                return Redirect("~/");
            }
            else
            {
                return View();
            }
        }
        #endregion  

        #region Salvar Senha
        [HttpPost]
        public IActionResult SalvarSenha(SenhaViewModels mudarsenha)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/painel");
            }
            else
            {
                using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {

                    var UsuarioVerificacao = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Senha = @Senha", new { senha = mudarsenha.CodigoConfirmacao }).FirstOrDefault();
                    if (UsuarioVerificacao != null)
                    {
                        var criptografia = BCrypt.Net.BCrypt.HashPassword(mudarsenha.Email + "salt" + mudarsenha.Senha, workFactor: 12);
                        connection.Execute("UPDATE Usuario SET Senha = @NovaSenha, SenhaTemporaria = 0 WHERE Senha = @Senha", new { NovaSenha = criptografia, Senha = mudarsenha.CodigoConfirmacao });

                        var SMTP = connection.Query<SMTPModels>(@"SELECT * FROM SMTP WHERE TipoEmail = 'Senha Alterada'").FirstOrDefault();
                        var email = new MimeMessage();
                        email.From.Add(new MailboxAddress("Suporte WG", SMTP.UsuarioServidor)); // Nome do remetente como "Suporte WG"
                        email.To.Add(MailboxAddress.Parse(mudarsenha.Email));
                        email.Subject = "Troca de senha realizada com sucesso!";
                        email.Body = new TextPart(TextFormat.Html)
                        {
                            Text = $"<h1><center><b>Olá, {UsuarioVerificacao.Nome}! 👋</b><br /><br /><b>A sua senha foi alterada com sucesso! 🎉<br /><br /><a href='https://wgfm.azurewebsites.net/'>Clique aqui</a> para acessar o sistema. 🌟<br /><br />Atenciosamente,<br /><br />🎲 WAR-GROW 💣</b></center></h1>"
                        };

                        using (var smtp = new SmtpClient())
                        {
                            smtp.Connect(SMTP.HostServidor, SMTP.Porta);
                            smtp.Authenticate(SMTP.UsuarioServidor, SMTP.Senha);
                            smtp.Send(email);
                            smtp.Disconnect(true);
                        }
                        TempData["ValidacaoSenha"] = "Senha alterada com sucesso!";
                    }
                    else
                    {
                        return Redirect("/");
                    }
                    return Redirect("/");
                }
            }
        }
        #endregion

        #region Logout
        [Route("sair")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/");
        }
        #endregion

        #region Acessar com ID
        [Authorize(Roles = "Administrador")]
        [Route("login-administrador-como-usuario/{usuarioId}")]
        public IActionResult LoginAdministradorComoUsuario(int usuarioId)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                var Usuario = connection.Query<UsuarioModels>(@"SELECT * FROM Usuario WHERE Id = @Id", new { Id = usuarioId }).FirstOrDefault();
                AutenticaUsuario(Usuario);
                return Redirect("~/painel");
            }
        }
        #endregion

    }

}
