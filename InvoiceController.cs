using Flipped.Data;
using Flipped.Public.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Flipped.Shared;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace Flipped.Public.Areas.Admin.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly GlobalDbContext dbContext;

        public InvoiceController(GlobalDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

	// GET: Admin/Invoices/
        public ActionResult Index()
        {
             return View(await _context.Invoices
                 .OrderByDescending(t => t.InvoiceDate)
                 .ToListAsync());
        }

        // GET: Admin/Invoices/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Invoices == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Admin/Invoices/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InvoiceId")] Invoice invoice)
        {
            if (ModelState.IsValid)
            {
                invoice.Id = Guid.NewGuid();
                _context.Add(invoice);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(invoice);
        }

        // GET: Admin/Invoices/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Invoices == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }
            return View(invoice);
        }

        // POST: Admin/Invoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,InvoiceId")] Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvoiceExists(invoice.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(invoice);
        }

        // GET: Admin/Invoices/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Invoice == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(m => m.Id == id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Admin/Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Invoices == null)
            {
                return Problem("Entity set 'GlobalDbContext.Invoices'  is null.");
            }
            var invoice = await _context.Invoice.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoice.Remove(invoice);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InvoiceExists(Guid id)
        {
          return _context.Invoice.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetFile(string contractID, string invoiceDate)
        {
            // The final front end will request an actual invoice ID
            var rg = new ReportGenerator(dbContext);
            Guid? contractGuid;
            try
            {
                contractGuid = Guid.Parse(contractID);
            }
            catch(Exception ex)
            {
                return NotFound();
            }
            var contract = dbContext.Contracts
                .Include(c => c.Product)
                .Include(c => c.BillingEntity)
                .ThenInclude(b => b.Site)
                .Include(c => c.BillingEntity)
                .ThenInclude(b => b.Meter)
                .Include(c => c.ContractUser)
            .ThenInclude(cu => cu.User)
                .FirstOrDefault(c => c.Id == contractGuid);
            if (contract == null)
            {
                return NotFound();
            }
            var invoiceEntryGenerator = new InvoiceEntryGenerator(dbContext);
            var nmi = contract.BillingEntity.FirstOrDefault().Meter.NMI;
            var startDate = DateTime.Parse(invoiceDate);
            var endDate = startDate.AddDays(29);
            // Generate an invoice for testing
            var invoice = invoiceEntryGenerator.Generate(contract, nmi, startDate, endDate);


            byte[] fileBytes = rg.Run(invoice);
            return File(fileBytes, "application/octet-stream", "Invoice.pdf");

        }

    }
}
