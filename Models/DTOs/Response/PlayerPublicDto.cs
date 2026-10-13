namespace Rolayther.Models.DTOs.Response
{
    // Vista PUBBLICA di un Player: solo ciò che serve per mostrarlo come partecipante.
    // Niente nome/cognome, email, data di nascita, password: quelli li vedono solo l'Admin e il Player stesso.
    public class PlayerPublicDto
    {
        public Guid PlayerId { get; set; }
        public string NickName { get; set; } = string.Empty;
        public string? AvatarImgUrl { get; set; }
    }
}
