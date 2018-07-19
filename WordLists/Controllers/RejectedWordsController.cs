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
using System.Text.RegularExpressions;

namespace WordLists.Controllers
{
    public class RejectedWordsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public static Guid listGuid;
        public static Guid clientGuid;
        public static int? LengthFromFilter;
        public static List<int> Lengths = new List<int>();
        public IEnumerable<int> WordLengthsDistinct;
        public IOrderedQueryable<RejectedWord> VersionRejectedWords;
       

        // GET: RejectedWords
        public async Task<ActionResult> Index(Guid? ClientFilter, Guid? VersionFilter, int? LengthFilter)
        {
            ViewBag.CanEdit = true;
            if (clientGuid == null)
            {
                ViewBag.CanEdit = false;
                Lengths.Clear();
            }


            if (ClientFilter != null)
            {
                clientGuid = new Guid(ClientFilter.ToString());
                LengthFromFilter = LengthFilter;
                Lengths.Clear();

                if (VersionFilter != null)
                {

                    listGuid = new Guid(VersionFilter.ToString());
                    ViewBag.CanEdit = true;

                    foreach (var rw in db.RejectedWords.Where(a => a.ListNameId == listGuid))
                    {
                        Lengths.Add(rw.Word.Length);
                    }
                }
                else
                {
                    ViewBag.CanEdit = false;
                    Lengths.Clear();
                }
            }
            WordLengthsDistinct = Lengths.Distinct();


            if (LengthFromFilter != null && listGuid != null && clientGuid != null)
            {
                VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listGuid && a.Word.Length == LengthFromFilter).OrderBy(b => b.Word);
            }
            else
            {
                VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listGuid).OrderBy(b => b.Word);
            }

            // Viewbags for Client and Version dropdown filters
            var clients = await db.Clients.Include(m => m.ListNames).Where(m => m.ListNames.Any(l => l.IsRejected == true)).ToListAsync();
            var versions = await db.ListNames.OrderBy(m => m.listName).ToListAsync();
            ViewBag.WordLengths = new SelectList(WordLengthsDistinct.OrderBy(p => p), LengthFromFilter);
            ViewBag.ClientList = new SelectList(clients.OrderBy(m => m.Name), "Id", "Name", clientGuid);
            ViewBag.VersionList = new SelectList(versions.Where(m => m.ClientId == clientGuid && m.Live && m.IsRejected == true).ToList(), "Id", "listName", listGuid);

            return View(await VersionRejectedWords.ToListAsync());
        }



        // GET: RejectedWords/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RejectedWord RejectedWord = await db.RejectedWords.FindAsync(id);
            if (RejectedWord == null)
            {
                return HttpNotFound();
            }
            return View(RejectedWord);
        }

        // GET: RejectedWords/Create/5
        public ActionResult Create()
        {
            if (listGuid == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == listGuid && p.Live && p.IsRejected == true).OrderBy(m => m.listName), "Id", "listName");
            return View();
        }

        // POST: RejectedWords/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,ListNameId,Word")] RejectedWord RejectedWord)
        {
            if (ModelState.IsValid)
            {
                string text = RejectedWord.Word;
                MatchCollection matches = Regex.Matches(text, @"[\w\d_]+", RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        RejectedWord parsedWord = new RejectedWord();
                        parsedWord.Id = Guid.NewGuid();
                        parsedWord.Word = match.Value.ToUpperInvariant();
                        parsedWord.ListNameId = RejectedWord.ListNameId;
                        RejectedWord = parsedWord;
                        db.RejectedWords.Add(RejectedWord);
                        await db.SaveChangesAsync();
                    }

                }

                return RedirectToAction("Index");
            }

            ViewBag.ListNameId = new SelectList(db.ListNames.OrderBy(m => m.listName), "Id", "listName", RejectedWord.ListNameId);
            return View(RejectedWord);
        }

        // GET: RejectedWords/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RejectedWord RejectedWord = await db.RejectedWords.FindAsync(id);
            if (RejectedWord == null)
            {
                return HttpNotFound();
            }
            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == listGuid && p.Live && p.IsRejected == true).OrderBy(m => m.listName), "Id", "listName");
            return View(RejectedWord);
        }

        // POST: RejectedWords/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,ListNameId,Word")] RejectedWord RejectedWord)
        {
            if (ModelState.IsValid)
            {
                db.Entry(RejectedWord).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == listGuid && p.Live && p.IsRejected == true).OrderBy(m => m.listName), "Id", "listName");
            return View(RejectedWord);
        }

        // GET: RejectedWords/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RejectedWord RejectedWord = await db.RejectedWords.FindAsync(id);
            if (RejectedWord == null)
            {
                return HttpNotFound();
            }
            return View(RejectedWord);
        }

        // POST: RejectedWords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            RejectedWord RejectedWord = await db.RejectedWords.FindAsync(id);
            db.RejectedWords.Remove(RejectedWord);
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
