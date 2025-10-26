using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using KRT.Cliente.Api.Data;
using KRT.Cliente.Api.Models;
using KRT.Cliente.Api.Controllers;
using System.Text.Json;
using System.Text;

namespace KRT.Cliente.API.Test.Controllers
{
    public class ContasControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly ContasController _controller;
        private readonly List<Conta> _contasIniciais;
        private readonly Encoding _encoding = Encoding.UTF8;

        public ContasControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDatabase_{Guid.NewGuid()}")
                .Options;
            _context = new AppDbContext(options);

            _contasIniciais = new List<Conta>
            {
                new Conta { Id = Guid.NewGuid(), NomeTitular = "Alice Teste", CPF = "11122233344", Status = true, CriadoEm = new DateTime(2024, 01, 15), Email = "alice@teste.com", DeletadoEm = null },
                new Conta { Id = Guid.NewGuid(), NomeTitular = "Bob Teste", CPF = "55566677788", Status = false, CriadoEm = new DateTime(2024, 02, 20), Email = "bob@teste.com", DeletadoEm = null },
                new Conta { Id = Guid.NewGuid(), NomeTitular = "Charlie Teste", CPF = "99900011122", Status = true, CriadoEm = new DateTime(2023, 11, 05), Email = "charlie@teste.com", DeletadoEm = null },
                new Conta { Id = Guid.NewGuid(), NomeTitular = "David Teste (SoftDelete)", CPF = "00011122233", Status = false, CriadoEm = new DateTime(2024, 03, 10), Email = "david@teste.com", DeletadoEm = DateTime.UtcNow.AddHours(-1) }
            };

            _context.TB_Contas.AddRange(_contasIniciais);
            _context.SaveChanges();

            _mockCache = new Mock<IDistributedCache>();

            _controller = new ContasController(_context, _mockCache.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetConta_DeveRetornarTodasAsContas()
        {
            var result = await _controller.GetConta();

            var okResult = Assert.IsType<ActionResult<IEnumerable<Conta>>>(result);
            var returnedList = Assert.IsAssignableFrom<IEnumerable<Conta>>(okResult.Value);
            Assert.Equal(4, returnedList.Count());
        }

        [Fact]
        public async Task GetConta_DeveRetornarConta_QuandoIdExiste()
        {
            var contaExistente = _contasIniciais.First();

            var result = await _controller.GetConta(contaExistente.Id);

            var okResult = Assert.IsType<ActionResult<Conta>>(result);
            var returnedConta = Assert.IsType<Conta>(okResult.Value);
            Assert.Equal(contaExistente.Id, returnedConta.Id);
        }

        [Fact]
        public async Task GetConta_DeveRetornarNotFound_QuandoIdInexistente()
        {
            var result = await _controller.GetConta(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetContasSistemasTerceiros_DeveRetornarContaDTO_QuandoIdExiste()
        {
            var contaExistente = _contasIniciais.First(c => c.Status);

            var result = await _controller.GetContasSistemasTerceiros(contaExistente.Id);

            var okResult = Assert.IsType<ActionResult<ContaDTO>>(result);
            var returnedDto = Assert.IsType<ContaDTO>(okResult.Value);
            Assert.Equal(contaExistente.NomeTitular, returnedDto.Nome);
            Assert.Equal("Ativa", returnedDto.StatusConta);
        }

        [Fact]
        public async Task GetContasSistemasTerceiros_DeveRetornarNotFound_QuandoIdInexistente()
        {
            var result = await _controller.GetContasSistemasTerceiros(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetContaPorCPF_DeveRetornarDoCache_QuandoExistir()
        {
            var contaOriginal = _contasIniciais.First(c => c.Status);
            var expectedDto = new ContaDTO
            {
                Id = contaOriginal.Id,
                Nome = contaOriginal.NomeTitular,
                CPF = contaOriginal.CPF,
                StatusConta = "Ativa"
            };
            var cacheKey = $"conta:{contaOriginal.CPF}";
            var cachedJson = JsonSerializer.Serialize(expectedDto);
            var cachedBytes = _encoding.GetBytes(cachedJson);

            _mockCache
                .Setup(c => c.GetAsync(cacheKey, default))
                .ReturnsAsync(cachedBytes);

            var result = await _controller.GetContaPorCPF(contaOriginal.CPF);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDto = Assert.IsType<ContaDTO>(okResult.Value);
            Assert.Equal(expectedDto.CPF, returnedDto.CPF);

            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Never);
        }

        [Fact]
        public async Task GetContaPorCPF_DeveRetornarDoDB_E_SalvarNoCache_QuandoNaoExistirNoCache()
        {
            var contaOriginal = _contasIniciais.First(c => c.Status);
            var cacheKey = $"conta:{contaOriginal.CPF}";

            _mockCache
                .Setup(c => c.GetAsync(cacheKey, default))
                .ReturnsAsync((byte[])null);

            var result = await _controller.GetContaPorCPF(contaOriginal.CPF);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDto = Assert.IsType<ContaDTO>(okResult.Value);
            Assert.Equal(contaOriginal.CPF, returnedDto.CPF);

            _mockCache.Verify(c => c.SetAsync(
                cacheKey,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(opts => opts.AbsoluteExpirationRelativeToNow != null),
                default
            ), Times.Once);
        }

        [Fact]
        public async Task GetContasAtivas_DeveRetornarDoDB_E_SalvarNoCache()
        {
            var cacheKey = "ContasAtivas";
            _mockCache.Setup(c => c.GetAsync(cacheKey, default)).ReturnsAsync((byte[])null);

            var result = await _controller.GetContasAtivas();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<ContaDTO>>(okResult.Value);

            Assert.Equal(2, returnedList.Count);

            _mockCache.Verify(c => c.SetAsync(cacheKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task GetContasInativas_DeveRetornarDoCache_QuandoExistir()
        {
            var cacheKey = "ContasInativas";
            var expectedList = new List<ContaDTO> { new ContaDTO { Id = Guid.NewGuid(), Nome = "Cache Inativo", StatusConta = "Inativa" } };
            var cachedJson = JsonSerializer.Serialize(expectedList);
            var cachedBytes = _encoding.GetBytes(cachedJson);

            _mockCache
                .Setup(c => c.GetAsync(cacheKey, default))
                .ReturnsAsync(cachedBytes);

            var result = await _controller.GetContasInativas();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<ContaDTO>>(okResult.Value);
            Assert.NotEmpty(returnedList);
            Assert.Equal("Inativa", returnedList.First().StatusConta);
        }

        [Fact]
        public async Task GetResumoStatus_DeveRetornarDoDB_E_SalvarNoCache()
        {
            var cacheKey = "ResumoStatus";
            _mockCache.Setup(c => c.GetAsync(cacheKey, default)).ReturnsAsync((byte[])null);

            var result = await _controller.GetResumoStatus();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resumo = Assert.IsType<ResumoStatusDTO>(okResult.Value);

            Assert.Equal(2, resumo.TotalAtivas);
            Assert.Equal(2, resumo.TotalInativas);
            Assert.Equal(4, resumo.TotalGeral);

            _mockCache.Verify(c => c.SetAsync(cacheKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task PostConta_DeveAdicionarConta_E_InvalidarCaches()
        {
            var newConta = new Conta { Id = Guid.NewGuid(), NomeTitular = "Nova Conta", CPF = "12345678900", Status = true, Email = "nova@conta.com", CriadoEm = DateTime.UtcNow };

            var result = await _controller.PostConta(newConta);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(await _context.TB_Contas.FindAsync(newConta.Id));

            _mockCache.Verify(c => c.RemoveAsync($"conta:{newConta.CPF}", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ResumoStatus", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasAtivas", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasInativas", default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task PutConta_DeveAtualizarContaExistente_E_InvalidarCaches()
        {
            var contaOriginal = _contasIniciais.First(c => c.Status);
            var cpfOriginal = contaOriginal.CPF;

            _context.Entry(contaOriginal).State = EntityState.Detached;

            var contaAtualizadaArg = new Conta
            {
                Id = contaOriginal.Id,
                NomeTitular = "Alice Nova",
                CPF = cpfOriginal,
                Status = false,
                Email = contaOriginal.Email,
                CriadoEm = contaOriginal.CriadoEm,
                AtualizadoEm = DateTime.UtcNow
            };

            var result = await _controller.PutConta(contaOriginal.Id, contaAtualizadaArg);

            Assert.IsType<NoContentResult>(result);

            var dbConta = await _context.TB_Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contaOriginal.Id);
            Assert.Equal("Alice Nova", dbConta.NomeTitular);
            Assert.False(dbConta.Status);

            _mockCache.Verify(c => c.RemoveAsync($"conta:{cpfOriginal}", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ResumoStatus", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasAtivas", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasInativas", default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task PutConta_DeveRetornarBadRequest_QuandoIdDiferenteDaConta()
        {
            var contaOriginal = _contasIniciais.First();
            var contaComIdErrado = new Conta
            {
                Id = Guid.NewGuid(),
                NomeTitular = contaOriginal.NomeTitular,
                CPF = contaOriginal.CPF,
                Email = contaOriginal.Email
            };

            var result = await _controller.PutConta(contaOriginal.Id, contaComIdErrado);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task AtivarConta_DeveAlterarStatus_E_InvalidarCaches()
        {
            var contaInativa = _contasIniciais.First(c => !c.Status && c.DeletadoEm == null);
            var originalCpf = contaInativa.CPF;

            var result = await _controller.AtivarConta(contaInativa.Id);

            Assert.IsType<OkObjectResult>(result);

            var contaAtualizada = await _context.TB_Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contaInativa.Id);
            Assert.True(contaAtualizada.Status);

            _mockCache.Verify(c => c.RemoveAsync($"conta:{originalCpf}", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ResumoStatus", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasAtivas", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasInativas", default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task AtivarConta_DeveRetornarBadRequest_QuandoContaJaAtiva()
        {
            var contaAtiva = _contasIniciais.First(c => c.Status);

            var result = await _controller.AtivarConta(contaAtiva.Id);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task InativarConta_DeveAlterarStatus_E_InvalidarCaches()
        {
            var contaAtiva = _contasIniciais.First(c => c.Status);
            var originalCpf = contaAtiva.CPF;

            var result = await _controller.InativarConta(contaAtiva.Id);

            Assert.IsType<OkObjectResult>(result);

            var contaAtualizada = await _context.TB_Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contaAtiva.Id);
            Assert.False(contaAtualizada.Status);

            _mockCache.Verify(c => c.RemoveAsync($"conta:{originalCpf}", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ResumoStatus", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasAtivas", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasInativas", default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task InativarConta_DeveRetornarBadRequest_QuandoContaJaInativa()
        {
            var contaInativa = _contasIniciais.First(c => !c.Status && c.DeletadoEm == null);

            var result = await _controller.InativarConta(contaInativa.Id);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteConta_DeveRemoverConta_E_InvalidarCaches()
        {
            var contaParaDeletar = _contasIniciais.First(c => c.DeletadoEm == null);
            var id = contaParaDeletar.Id;
            var originalCpf = contaParaDeletar.CPF;

            var result = await _controller.DeleteConta(id);

            Assert.IsType<NoContentResult>(result);

            Assert.Null(await _context.TB_Contas.FindAsync(id));

            _mockCache.Verify(c => c.RemoveAsync($"conta:{originalCpf}", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ResumoStatus", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasAtivas", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasInativas", default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task SoftDelete_DeveMarcarComoDeletada_E_InvalidarCaches()
        {
            var contaAtiva = _contasIniciais.First(c => c.Status);
            var originalCpf = contaAtiva.CPF;

            var result = await _controller.SoftDelete(contaAtiva.Id);

            Assert.IsType<OkObjectResult>(result);

            var contaAtualizada = await _context.TB_Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contaAtiva.Id);
            Assert.NotNull(contaAtualizada.DeletadoEm);

            _mockCache.Verify(c => c.RemoveAsync($"conta:{originalCpf}", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ResumoStatus", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasAtivas", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasInativas", default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task RestaurarConta_DeveRemoverMarcaDeDeletada_E_InvalidarCaches()
        {
            var contaDeletada = _contasIniciais.First(c => c.DeletadoEm != null);
            var originalCpf = contaDeletada.CPF;

            var result = await _controller.RestaurarConta(contaDeletada.Id);

            Assert.IsType<OkObjectResult>(result);

            var contaRestaurada = await _context.TB_Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contaDeletada.Id);
            Assert.Null(contaRestaurada.DeletadoEm);

            _mockCache.Verify(c => c.RemoveAsync($"conta:{originalCpf}", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ResumoStatus", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasAtivas", default(CancellationToken)), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("ContasInativas", default(CancellationToken)), Times.Once);
        }

        [Fact]
        public async Task RestaurarConta_DeveRetornarBadRequest_QuandoNaoEstiverDeletada()
        {
            var contaNaoDeletada = _contasIniciais.First(c => c.DeletadoEm == null);

            var result = await _controller.RestaurarConta(contaNaoDeletada.Id);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetContasPorPeriodo_DeveRetornarContasCorretas()
        {
            var inicio = new DateTime(2024, 01, 01);
            var fim = new DateTime(2024, 12, 31);

            var result = await _controller.GetContasPorPeriodo(inicio, fim);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<ContaDTO>>(okResult.Value);
            Assert.Equal(3, returnedList.Count);
        }

        [Fact]
        public async Task GetContasPorPeriodo_DeveRetornarNotFound_QuandoNenhumaContaEncontrada()
        {
            var inicio = new DateTime(2000, 01, 01);
            var fim = new DateTime(2000, 12, 31);

            var result = await _controller.GetContasPorPeriodo(inicio, fim);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetContasDeletadas_DeveRetornarSomenteContasComDeletadoEmPreenchido()
        {
            var result = await _controller.GetContasDeletadas();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<ContaDTO>>(okResult.Value);
            Assert.Single(returnedList);
            Assert.Equal("David Teste (SoftDelete)", returnedList.First().Nome);
        }

        [Fact]
        public async Task GetContasDeletadas_DeveRetornarNotFound_QuandoNenhumaContaDeletada()
        {
            var contaDeletada = _contasIniciais.First(c => c.DeletadoEm != null);
            contaDeletada.DeletadoEm = null;
            await _context.SaveChangesAsync();

            var result = await _controller.GetContasDeletadas();

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetTotaisPorAno_DeveRetornarDoDB_E_SalvarNoCache()
        {
            var anos = new int[] { 2023, 2024 };
            var cacheKey = "TotaisPorAno:2023,2024";
            _mockCache.Setup(c => c.GetAsync(cacheKey, default)).ReturnsAsync((byte[])null);

            var result = await _controller.GetTotaisPorAno(anos);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<TotalAnoDTO>>(okResult.Value);

            Assert.Equal(2, returnedList.Count);
            Assert.Contains(returnedList, dto => dto.Ano == 2023 && dto.TotalClientes == 1);
            Assert.Contains(returnedList, dto => dto.Ano == 2024 && dto.TotalClientes == 3);

            _mockCache.Verify(c => c.SetAsync(cacheKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task GetTotaisPorAno_DeveRetornarBadRequest_QuandoAnosVazio()
        {
            var result = await _controller.GetTotaisPorAno(Array.Empty<int>());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}