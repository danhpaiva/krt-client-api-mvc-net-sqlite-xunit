namespace KRT.Cliente.Api.Models;

public class ContaDTO
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = default!;
    public string CPF { get; set; } = default!;
    public string StatusConta { get; set; } = default!;
}
