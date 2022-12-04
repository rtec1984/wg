namespace ProjectAmaterasu.Models
{
    public class SMTPModels
    {
        public int Id { get; set; }
        public string HostServidor { get; set; }
        public string UsuarioServidor { get; set; }
        public string Senha { get; set; }
        public string CorpoEmail { get; set; }
        public string TipoEmail { get; set; }
        public int Porta { get; set; }
    }
}
