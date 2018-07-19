using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WordLists.Models;

namespace WordLists.Controllers
{
    public class ListNamesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: ListNames
        public async Task<ActionResult> Index()
        {
            var listNames = db.ListNames.Include(l => l.Client);
            return View(await listNames.ToListAsync());
        }

        // GET: ListNames/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ListName listName = await db.ListNames.FindAsync(id);
            if (listName == null)
            {
                return HttpNotFound();
            }
            return View(listName);
        }

        // GET: ListNames/Create
        public ActionResult Create()
        {
            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name");
            return View();
        }

        // POST: ListNames/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,ClientId,listName,IsRejected,Live")] ListName Name)
        {
            if (ModelState.IsValid)
            {
                Name.Id = Guid.NewGuid();
                db.ListNames.Add(Name);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name", Name.ClientId);
            return View(Name);
        }

        // GET: ListNames/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ListName listName = await db.ListNames.FindAsync(id);
            if (listName == null)
            {
                return HttpNotFound();
            }
            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name", listName.ClientId);
            return View(listName);
        }

        // POST: ListNames/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,ClientId,listName,IsRejected,Live")] ListName Name)
        {
           
            if (ModelState.IsValid)
            {
                db.Entry(Name).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name", Name.ClientId);
            return View(Name);
        }

        // GET: ListNames/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ListName listName = await db.ListNames.FindAsync(id);
            if (listName == null)
            {
                return HttpNotFound();
            }
            return View(listName);
        }

        // POST: ListNames/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            ListName listName = await db.ListNames.FindAsync(id);
            db.ListNames.Remove(listName);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
