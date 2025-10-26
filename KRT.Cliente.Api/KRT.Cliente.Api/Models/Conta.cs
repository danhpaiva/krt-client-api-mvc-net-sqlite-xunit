using System.ComponentModel.DataAnnotations;

namespace KRT.Cliente.Api.Models;

public class Conta
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required(ErrorMessage = "O campo NomeTitular é obrigatorio.")]
    public string NomeTitular { get; set; } = default!;
    [Required(ErrorMessage = "O campo CPF é obrigatorio.")]
    [StringLength(11, ErrorMessage = "O campo CPF deve conter 11 caracteres.")]
    public string CPF { get; set; } = default!;
    [EmailAddress(ErrorMessage = "O campo Email deve ser um endereço de email válido.")]
    public string Email { get; set; } = default!;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
    public bool Status { get; set; } = false;
    public DateTime? DeletadoEm { get; set; }
}
