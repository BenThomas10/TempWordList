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
        private string masterRejectedListName = "Master Rejected Word List";

        // GET: ListNames
        public async Task<ActionResult> Index(Guid? ClientFilter)
        {
            ViewBag.Warning = null;
            List<ListName> filteredLists = new List<ListName>();
            ViewBag.DefaultClientName = "- Client -";
           
            if (ClientFilter == null && HttpContext.Session["list_clientGuid"] as Guid? == null)
            {
                filteredLists = await db.ListNames.OrderBy(p => p.Client.Name).ThenBy(p => p.listName).ToListAsync();
                HttpContext.Session["list_canNull"] = false;
            }
            else if (ClientFilter == null && HttpContext.Session["list_clientGuid"] as Guid? != null)
            {
                if (HttpContext.Session["list_canNull"] as bool? == true)
                {
                    var filtered_giud = HttpContext.Session["list_clientGuid"] as Guid?;
                    filteredLists = await db.ListNames.Where(p => p.ClientId == filtered_giud).OrderBy(p => p.listName).ToListAsync();
                    HttpContext.Session["list_canNull"] = false;
                }
                else
                {
                    filteredLists = await db.ListNames.OrderBy(p => p.Client.Name).ThenBy(p => p.listName).ToListAsync();
                    HttpContext.Session["list_clientGuid"] = null;
                }
               
            }
            else
            {
                filteredLists = await db.ListNames.Where(p => p.ClientId == ClientFilter).OrderBy(p => p.Client.Name).ToListAsync();
                HttpContext.Session["list_clientGuid"] = new Guid(ClientFilter.ToString());
            }

            ViewBag.ClientList = new SelectList(db.Clients.OrderBy(m => m.Name), "Id", "Name", HttpContext.Session["list_clientGuid"] as Guid?);
            ViewBag.ListCount = "(" + filteredLists.Count().ToString() + ")";
            return View(filteredLists);
        }

        // GET: ListNames/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            HttpContext.Session["list_canNull"] = true;
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
            HttpContext.Session["list_canNull"] = true;
            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name", HttpContext.Session["list_clientGuid"] as Guid?);
            return View();
        }

        // POST: ListNames/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,ClientId,listName,IsRejected,Archive")] ListName Name)
        {
            bool canCreate = true;
            if (ModelState.IsValid)
            {
                if (Name.listName == masterRejectedListName)
                {
                  if(db.ListNames.Where(p => p.listName == masterRejectedListName).Count() > 0)
                  {
                        canCreate = false;
                        ViewBag.Warning = masterRejectedListName.ToString() + " has already been created.  There can only be one " + masterRejectedListName.ToString();
                  }
                }

                if (canCreate)
                {
                    ViewBag.Warning = null;
                    Name.Id = Guid.NewGuid();
                    db.ListNames.Add(Name);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                
            }

            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name", Name.ClientId);
            return View(Name);
        }

        // GET: ListNames/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            HttpContext.Session["list_canNull"] = true;
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
        public async Task<ActionResult> Edit([Bind(Include = "Id,ClientId,listName,IsRejected,Archive")] ListName Name)
        {
            bool canCreate = true;
            if (ModelState.IsValid)
            {
                if (Name.listName == masterRejectedListName)
                {
                    if (db.ListNames.Where(p => p.listName == masterRejectedListName).Count() > 0)
                    {
                        canCreate = false;
                        ViewBag.Warning = masterRejectedListName.ToString() + " has already been created.  There can only be one " + masterRejectedListName.ToString() + ".";
                    }
                }

                if (canCreate)
                {
                    ViewBag.Warning = null;
                    db.Entry(Name).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

            }
            
            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name", Name.ClientId);
            return View(Name);
        }

        // GET: ListNames/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            HttpContext.Session["list_canNull"] = true;
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
