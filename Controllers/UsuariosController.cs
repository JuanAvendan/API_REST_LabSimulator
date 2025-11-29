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
    public class UsuariosController : ControllerBase
    {
        private readonly LabSimulatorContext _context;

        public UsuariosController(LabSimulatorContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<clsUsuario>>> GetUsuarios()
        {
            return await _context.tbl_Usuario.ToListAsync();
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<clsUsuario>> GetclsUsuario(string id)
        {
            var clsUsuario = await _context.tbl_Usuario.FindAsync(id);

            if (clsUsuario == null)
            {
                return NotFound();
            }

            return clsUsuario;
        }

        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutclsUsuario(string id, clsUsuario clsUsuario)
        {
            if (id != clsUsuario.Usuario)
            {
                return BadRequest();
            }

            _context.Entry(clsUsuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!clsUsuarioExists(id))
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

        // POST: api/Usuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<clsUsuario>> PostclsUsuario(clsUsuario clsUsuario)
        {
            _context.tbl_Usuario.Add(clsUsuario);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (clsUsuarioExists(clsUsuario.Usuario))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetclsUsuario", new { id = clsUsuario.Usuario }, clsUsuario);
        }

        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteclsUsuario(string id)
        {
            var clsUsuario = await _context.tbl_Usuario.FindAsync(id);
            if (clsUsuario == null)
            {
                return NotFound();
            }

            _context.tbl_Usuario.Remove(clsUsuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool clsUsuarioExists(string id)
        {
            return _context.tbl_Usuario.Any(e => e.Usuario == id);
        }
    }
}
