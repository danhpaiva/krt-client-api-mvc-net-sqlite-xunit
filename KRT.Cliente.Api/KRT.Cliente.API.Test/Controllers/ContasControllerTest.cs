using KRT.Cliente.Api.Controllers;
using KRT.Cliente.Api.Models;
using KRT.Cliente.API.Test.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KRT.Cliente.API.Test.Controllers;

public class ContasControllerTest
{
    private Conta CriarContaTeste(string nome, string cpf, bool status = false, DateTime? criadoEm = null, DateTime? deletadoEm = null)
    {
        return new Conta
        {
            NomeTitular = nome,
            CPF = cpf,
            Email = $"{nome.Replace(" ", "").ToLower()}@teste.com",
            Status = status,
            CriadoEm = criadoEm ?? DateTime.UtcNow,
            DeletadoEm = deletadoEm
        };
    }

    [Fact]
    public async Task PostConta_DeveCriarNovaConta()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var controller = new ContasController(context);

        var novaConta = CriarContaTeste("Daniel Paiva", "12345678900", true);

        var result = await controller.PostConta(novaConta);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);

        var contaCriada = Assert.IsType<Conta>(createdResult.Value);
        Assert.Equal("Daniel Paiva", contaCriada.NomeTitular);
        Assert.True(contaCriada.Status);
    }

    [Fact]
    public async Task GetConta_DeveRetornarContaExistente()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var conta = CriarContaTeste("Cliente X", "99988877700");
        context.TB_Contas.Add(conta);
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.GetConta(conta.Id);

        var contaResult = Assert.IsType<Conta>(result.Value);
        Assert.Equal("Cliente X", contaResult.NomeTitular);
    }

    [Fact]
    public async Task GetConta_DeveRetornarNotFound_QuandoNaoExistir()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var controller = new ContasController(context);

        var result = await controller.GetConta(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteConta_DeveRemoverConta()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var conta = CriarContaTeste("Cliente Y", "11122233344");
        context.TB_Contas.Add(conta);
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.DeleteConta(conta.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.TB_Contas);
    }

    [Fact]
    public async Task PutConta_DeveAtualizarConta()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var conta = CriarContaTeste("Antigo Nome", "00011122233");
        context.TB_Contas.Add(conta);
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        conta.NomeTitular = "Novo Nome";

        var result = await controller.PutConta(conta.Id, conta);
        Assert.IsType<NoContentResult>(result);

        var contaAtualizada = await context.TB_Contas.FirstAsync();
        Assert.Equal("Novo Nome", contaAtualizada.NomeTitular);
    }

    [Fact]
    public async Task GetContasAtivas_DeveRetornarApenasAtivas()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        context.TB_Contas.AddRange(
            CriarContaTeste("Ativa1", "111", true),
            CriarContaTeste("Inativa1", "222", false)
        );
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.GetContasAtivas();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var contas = Assert.IsType<List<ContaDTO>>(okResult.Value);

        Assert.Single(contas);
        Assert.Equal("Ativa1", contas[0].Nome);
    }

    [Fact]
    public async Task GetContasInativas_DeveRetornarApenasInativas()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        context.TB_Contas.AddRange(
            CriarContaTeste("Ativa1", "111", true),
            CriarContaTeste("Inativa1", "222", false)
        );
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.GetContasInativas();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var contas = Assert.IsType<List<ContaDTO>>(okResult.Value);

        Assert.Single(contas);
        Assert.Equal("Inativa1", contas[0].Nome);
    }

    [Fact]
    public async Task GetTotaisPorAno_DeveRetornarContagemPorAno()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        context.TB_Contas.AddRange(
            CriarContaTeste("C1", "111", criadoEm: new DateTime(2025, 1, 1)),
            CriarContaTeste("C2", "222", criadoEm: new DateTime(2026, 2, 1)),
            CriarContaTeste("C3", "333", criadoEm: new DateTime(2025, 3, 1))
        );
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.GetTotaisPorAno(new int[] { 2025, 2026 });
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var totais = Assert.IsType<List<TotalAnoDTO>>(okResult.Value);

        Assert.Equal(2, totais.Count);
        Assert.Contains(totais, t => t.Ano == 2025 && t.TotalClientes == 2);
        Assert.Contains(totais, t => t.Ano == 2026 && t.TotalClientes == 1);
    }

    [Fact]
    public async Task AtivarConta_DeveMudarStatusParaTrue()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var conta = CriarContaTeste("Cliente", "111", false);
        context.TB_Contas.Add(conta);
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.AtivarConta(conta.Id);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True(context.TB_Contas.First().Status);
    }

    [Fact]
    public async Task InativarConta_DeveMudarStatusParaFalse()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var conta = CriarContaTeste("Cliente", "111", true);
        context.TB_Contas.Add(conta);
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.InativarConta(conta.Id);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.False(context.TB_Contas.First().Status);
    }

    [Fact]
    public async Task GetContaPorCPF_DeveRetornarConta()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var conta = CriarContaTeste("Cliente", "123456");
        context.TB_Contas.Add(conta);
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.GetContaPorCPF("123456");
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ContaDTO>(okResult.Value);

        Assert.Equal("Cliente", dto.Nome);
    }

    [Fact]
    public async Task GetResumoStatus_DeveRetornarTotais()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        context.TB_Contas.AddRange(
            CriarContaTeste("C1", "111", true),
            CriarContaTeste("C2", "222", false)
        );
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.GetResumoStatus();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var resumo = Assert.IsType<ResumoStatusDTO>(okResult.Value);

        Assert.Equal(1, resumo.TotalAtivas);
        Assert.Equal(1, resumo.TotalInativas);
        Assert.Equal(2, resumo.TotalGeral);
    }

    [Fact]
    public async Task GetContasPorPeriodo_DeveRetornarApenasNoIntervalo()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        context.TB_Contas.AddRange(
            CriarContaTeste("C1", "111", criadoEm: new DateTime(2025, 1, 1)),
            CriarContaTeste("C2", "222", criadoEm: new DateTime(2026, 1, 1))
        );
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.GetContasPorPeriodo(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var contas = Assert.IsType<List<ContaDTO>>(okResult.Value);

        Assert.Single(contas);
        Assert.Equal("C1", contas[0].Nome);
    }

    [Fact]
    public async Task SoftDelete_DeveMarcarComoDeletada()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var conta = CriarContaTeste("Cliente", "111");
        context.TB_Contas.Add(conta);
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.SoftDelete(conta.Id);
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(context.TB_Contas.First().DeletadoEm);
    }

    [Fact]
    public async Task RestaurarConta_DeveRemoverDeletadoEm()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        var conta = CriarContaTeste("Cliente", "111", deletadoEm: DateTime.UtcNow);
        context.TB_Contas.Add(conta);
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.RestaurarConta(conta.Id);
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Null(context.TB_Contas.First().DeletadoEm);
    }

    [Fact]
    public async Task GetContasDeletadas_DeveRetornarApenasDeletadas()
    {
        var context = FakeDbContextFactory.CreateInMemoryContext();
        context.TB_Contas.AddRange(
            CriarContaTeste("C1", "111", deletadoEm: DateTime.UtcNow),
            CriarContaTeste("C2", "222")
        );
        await context.SaveChangesAsync();

        var controller = new ContasController(context);
        var result = await controller.GetContasDeletadas();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var contas = Assert.IsType<List<ContaDTO>>(okResult.Value);

        Assert.Single(contas);
        Assert.Equal("C1", contas[0].Nome);
    }
}