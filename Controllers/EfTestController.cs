using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount;
using LuqinOfficialAccount.Models;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EfTestController : ControllerBase
    {
        private readonly AppDBContext _context;

        public EfTestController(AppDBContext context)
        {
            _context = context;
        }

        // GET: api/EfTest
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EfTest>>> GetEfTest()
        {
            return await _context.EfTest.ToListAsync();
        }

        // GET: api/EfTest/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EfTest>> GetEfTest(int id)
        {
            var efTest = await _context.EfTest.FindAsync(id);

            if (efTest == null)
            {
                return NotFound();
            }

            return efTest;
        }

        // PUT: api/EfTest/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEfTest(int id, EfTest efTest)
        {
            if (id != efTest.id)
            {
                return BadRequest();
            }

            _context.Entry(efTest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EfTestExists(id))
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

        // POST: api/EfTest
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<EfTest>> PostEfTest(EfTest efTest)
        {
            _context.EfTest.Add(efTest);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEfTest", new { id = efTest.id }, efTest);
        }

        // DELETE: api/EfTest/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEfTest(int id)
        {
            var efTest = await _context.EfTest.FindAsync(id);
            if (efTest == null)
            {
                return NotFound();
            }

            _context.EfTest.Remove(efTest);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EfTestExists(int id)
        {
            return _context.EfTest.Any(e => e.id == id);
        }
    }
}
