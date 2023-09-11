using System;

namespace ProjectAmaterasu.Models
{
    public class UsuarioModels
    {
        public int Id { get; set; }
        public string Name { get; set; }    
        public string Nome { get; set; }
        public string Apelido { get; set; }
        public string IntlNumber { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string Tema { get; set; }
        public bool SenhaTemporaria { get; set; }
        public bool Administrador { get; set; }
    }

    public class UsuarioViewModels
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public int Vitorias { get; set; }
        public int Derrotas { get; set; }
        public int Pontuacao { get; set; }
        public int Posicao { get; set; }
        public DateTime Data { get; set; }        
    }
}
