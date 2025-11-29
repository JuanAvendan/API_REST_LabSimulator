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
    public class PartidasController : ControllerBase
    {
        private readonly LabSimulatorContext _context;

        public PartidasController(LabSimulatorContext context)
        {
            _context = context;
        }

        // GET: api/Partidas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<clsPartida>>> Gettbl_Partida()
        {
            return await _context.tbl_Partida.ToListAsync();
        }

        // GET: api/Partidas/5
        [HttpGet("{user}")]
        public async Task<ActionResult<IEnumerable<clsUsuariosPartidas>>> GetclsPartida(string user)
        {
            var query = from p in _context.tbl_Partida
                        join up in _context.tbl_UsuarioPartida
                        on p.IdPartida equals up.idPartida
                        where up.Usuario == user
                        orderby p.IdPartida
                        select new clsUsuariosPartidas
                        {
                            Usuario = up.Usuario,
                            NombreSala = p.NombreSala,
                            NombreDocente = p.NombreDocente,
                            CantidadJugadores = p.CantidadJugadores,
                            FechaFinal = p.FechaFinal
                        };

            return await query.ToListAsync();
        }

        // PUT: api/Partidas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutclsPartida(int id, clsPartida clsPartida)
        {
            if (id != clsPartida.IdPartida)
            {
                return BadRequest();
            }

            _context.Entry(clsPartida).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!clsPartidaExists(id))
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

        // POST: api/Partidas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<clsPartida>> PostclsPartida(clsPartida clsPartida)
        {
            _context.tbl_Partida.Add(clsPartida);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetclsPartida", new { id = clsPartida.IdPartida }, clsPartida);
        }

        // DELETE: api/Partidas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteclsPartida(int id)
        {
            var clsPartida = await _context.tbl_Partida.FindAsync(id);
            if (clsPartida == null)
            {
                return NotFound();
            }

            _context.tbl_Partida.Remove(clsPartida);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool clsPartidaExists(int id)
        {
            return _context.tbl_Partida.Any(e => e.IdPartida == id);
        }
    }
}
