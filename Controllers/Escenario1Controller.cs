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
    public class Escenario1Controller : ControllerBase
    {
        private readonly LabSimulatorContext _context;

        public Escenario1Controller(LabSimulatorContext context)
        {
            _context = context;
        }

        // GET: api/Escenario1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<clsEscenario1>>> Gettbl_Escenario1BombaInfusion()
        {
            var escenario1 = await _context.tbl_Escenario1BombaInfusion.ToListAsync();
            return Ok(new { escenario1 });
        }

        // GET: api/Escenario1/5
        [HttpGet("{id}")]
        public async Task<ActionResult<clsEscenario1>> GetclsEscenario1(int id)
        {
            var clsEscenario1 = await _context.tbl_Escenario1BombaInfusion.FindAsync(id);

            if (clsEscenario1 == null)
            {
                return NotFound();
            }

            return clsEscenario1;
        }

        // PUT: api/Escenario1/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutclsEscenario1(int id, clsEscenario1 clsEscenario1)
        {
            if (id != clsEscenario1.IdEscenario1)
            {
                return BadRequest();
            }

            _context.Entry(clsEscenario1).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!clsEscenario1Exists(id))
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

        // POST: api/Escenario1
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<clsEscenario1>> PostclsEscenario1(clsEscenario1 clsEscenario1)
        {
            _context.tbl_Escenario1BombaInfusion.Add(clsEscenario1);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetclsEscenario1", new { id = clsEscenario1.IdEscenario1 }, clsEscenario1);
        }

        // DELETE: api/Escenario1/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteclsEscenario1(int id)
        {
            var clsEscenario1 = await _context.tbl_Escenario1BombaInfusion.FindAsync(id);
            if (clsEscenario1 == null)
            {
                return NotFound();
            }

            _context.tbl_Escenario1BombaInfusion.Remove(clsEscenario1);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool clsEscenario1Exists(int id)
        {
            return _context.tbl_Escenario1BombaInfusion.Any(e => e.IdEscenario1 == id);
        }
    }
}
