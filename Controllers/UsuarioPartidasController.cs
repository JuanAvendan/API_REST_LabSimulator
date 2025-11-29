using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabSimulatorAPI.Context;
using LabSimulatorAPI.Models;

namespace LabSimulatorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioPartidasController : ControllerBase
    {
        private readonly LabSimulatorContext _context;

        public UsuarioPartidasController(LabSimulatorContext context)
        {
            _context = context;
        }

        // GET: api/UsuarioPartidas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<clsUsuarioPartida>>> Gettbl_UsuarioPartida()
        {
            return await _context.tbl_UsuarioPartida.ToListAsync();
        }

        // GET: api/UsuarioPartidas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<clsUsuarioPartida>> GetclsUsuarioPartida(int id)
        {
            var clsUsuarioPartida = await _context.tbl_UsuarioPartida.FindAsync(id);

            if (clsUsuarioPartida == null)
            {
                return NotFound();
            }

            return clsUsuarioPartida;
        }

        // PUT: api/UsuarioPartidas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutclsUsuarioPartida(int id, clsUsuarioPartida clsUsuarioPartida)
        {
            if (id != clsUsuarioPartida.idUsuarioPartida)
            {
                return BadRequest();
            }

            _context.Entry(clsUsuarioPartida).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!clsUsuarioPartidaExists(id))
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

        // POST: api/UsuarioPartidas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<clsUsuarioPartida>> PostclsUsuarioPartida(clsUsuarioPartida clsUsuarioPartida)
        {
            _context.tbl_UsuarioPartida.Add(clsUsuarioPartida);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetclsUsuarioPartida", new { id = clsUsuarioPartida.idUsuarioPartida }, clsUsuarioPartida);
        }

        // DELETE: api/UsuarioPartidas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteclsUsuarioPartida(int id)
        {
            var clsUsuarioPartida = await _context.tbl_UsuarioPartida.FindAsync(id);
            if (clsUsuarioPartida == null)
            {
                return NotFound();
            }

            _context.tbl_UsuarioPartida.Remove(clsUsuarioPartida);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool clsUsuarioPartidaExists(int id)
        {
            return _context.tbl_UsuarioPartida.Any(e => e.idUsuarioPartida == id);
        }
    }
}
