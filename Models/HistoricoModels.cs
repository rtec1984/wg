using System;

namespace ProjectAmaterasu.Models
{
    public class HistoricoModels
    {
        public int Id { get; set; }
        public int Id_Usuario { get; set; }
        public string Nome_Usuario { get; set; }
        public int Vitorias { get; set; }
        public int Derrotas { get; set; }
        public int Jogos { get; set; }
        public int Pontuacao { get; set; }
        public int Desempenho { get; set; }
        public DateTime Data { get; set; }
    }
}
