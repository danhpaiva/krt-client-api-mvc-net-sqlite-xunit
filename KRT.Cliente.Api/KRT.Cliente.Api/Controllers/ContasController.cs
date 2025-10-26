using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KRT.Cliente.Api.Data;
using KRT.Cliente.Api.Models;

namespace KRT.Cliente.Api.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class ContasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Conta>>> GetConta()
        {
            return await _context.TB_Contas.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Conta>> GetConta(Guid id)
        {
            var conta = await _context.TB_Contas.FindAsync(id);

            if (conta == null)
            {
                return NotFound();
            }

            return conta;
        }

        [HttpGet("EndPointSistemaTerceiros/{id}")]
        public async Task<ActionResult<ContaDTO>> GetContasSistemasTerceiros(Guid id)
        {
            var conta = await _context.TB_Contas.FindAsync(id);

            if (conta == null)
            {
                return NotFound();
            }

            var contaDTO = new ContaDTO
            {
                Id = conta.Id,
                Nome = conta.NomeTitular,
                CPF = conta.CPF,
                StatusConta = conta.Status ? "Ativa" : "Inativa"
            };

            return contaDTO;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutConta(Guid id, Conta conta)
        {
            if (id != conta.Id)
            {
                return BadRequest();
            }

            _context.Entry(conta).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Conta>> PostConta(Conta conta)
        {
            _context.TB_Contas.Add(conta);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetConta", new { id = conta.Id }, conta);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConta(Guid id)
        {
            var conta = await _context.TB_Contas.FindAsync(id);
            if (conta == null)
            {
                return NotFound();
            }

            _context.TB_Contas.Remove(conta);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ContaExists(Guid id)
        {
            return _context.TB_Contas.Any(e => e.Id == id);
        }
    }
}
