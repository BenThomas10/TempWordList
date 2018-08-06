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

        //this name controls the master rejected list name to look for when rejecting approved words
        private string masterRejectedListName = "Master Rejected Word List";

        private ApplicationDbContext db = new ApplicationDbContext();
        private bool isRedirect = false;
        private static List<int> Lengths = new List<int>();
        private IEnumerable<int> WordLengthsDistinct;
        private IOrderedQueryable<RejectedWord> VersionRejectedWords;
        private List<SelectListItem> emptyList = new List<SelectListItem>();

        // GET: RejectedWords
        public async Task<ActionResult> Index(Guid? ClientFilter, Guid? VersionFilter, int? LengthFilter, string sortOrder)
        {
            //hides the view's add button and title
            ViewBag.CanEdit = false;
            HttpContext.Session["list_canNull"] = true;

            //view dropdown default selection text
            ViewBag.DefaultClientNameRejected = "- Client -";
            ViewBag.DefaultListNameRejected = "- List Name -";

            if (ClientFilter == null && VersionFilter == null && LengthFilter == null &&
                HttpContext.Session["clientGuidRejectedRejected"] as Guid? != Guid.Empty && HttpContext.Session["listGuidRejected"] as Guid? != Guid.Empty)
            {
                ClientFilter = HttpContext.Session["clientGuidRejected"] as Guid?;
                VersionFilter = HttpContext.Session["listGuidRejected"] as Guid?;
                LengthFilter = HttpContext.Session["LengthFromFilterRejected"] as int?;
            }

            if (isRedirect)
            {
                ViewBag.CanEdit = true;
                isRedirect = false;
            }

            if (ClientFilter != null)
            {
                isRedirect = true;
                ViewBag.DefaultClientNameRejected = null;

                if (ClientFilter != HttpContext.Session["clientGuidRejected"] as Guid?)
                {
                    HttpContext.Session["clientGuidRejected"] = new Guid(ClientFilter.ToString());
                    Lengths.Clear();
                    VersionFilter = null;
                    HttpContext.Session["LengthFromFilterRejected"] = null;
                }

                if (VersionFilter != null && ClientFilter != null)
                {
                    ViewBag.DefaultListNameRejected = null;
                    ViewBag.CanEdit = true;
                    if (HttpContext.Session["LengthFromFilterRejected"] as int? != LengthFilter)
                    {
                        ViewBag.DefaultListNameRejected = null;
                    }
                    HttpContext.Session["LengthFromFilterRejected"] = LengthFilter;


                    if (VersionFilter != HttpContext.Session["listGuidRejected"] as Guid? || System.Web.HttpContext.Current.Session["hasAdded_r"] as int? != null)
                    {
                        if(System.Web.HttpContext.Current.Session["hasAdded_r"] as int? == null)
                        HttpContext.Session["listGuidRejected"] = new Guid(VersionFilter.ToString());

                        Lengths.Clear();
                        var list_Guid = HttpContext.Session["listGuidRejected"] as Guid?;
                        foreach (var aw in db.RejectedWords.Where(a => a.ListNameId == list_Guid))
                        {
                            Lengths.Add(aw.Word.Length);
                        }
                    }
                }
                else
                {
                    ViewBag.CanEdit = false;
                    Lengths.Clear();
                    HttpContext.Session["LengthFromFilterRejected"] = null;
                }
            }

            var versions = await db.ListNames.OrderBy(m => m.listName).ToListAsync();
            ViewBag.VersionList = new SelectList(versions.Where(m => m.ClientId == HttpContext.Session["clientGuidRejected"] as Guid? && m.Archive == false && m.IsRejected == true)
                .OrderBy(m => m.listName).ToList(), "Id", "listName", HttpContext.Session["listGuidRejected"] as Guid?);

            if (ClientFilter == null && VersionFilter != null)
            {
                ViewBag.VersionList = new SelectList(items: "");
                HttpContext.Session["LengthFromFilterRejected"] = null;
                Lengths.Clear();
                isRedirect = false;
                ViewBag.CanEdit = false;
                HttpContext.Session["listGuidRejected"] = Guid.Empty;
                HttpContext.Session["clientGuidRejected"] = Guid.Empty;
            }

            System.Web.HttpContext.Current.Session["hasAdded_r"] = null;
            WordLengthsDistinct = Lengths.Distinct();
            ViewBag.WordLengths = new SelectList(WordLengthsDistinct.OrderBy(p => p), HttpContext.Session["LengthFromFilterRejected"] as int?);

            var clients = await db.Clients.Include(m => m.ListNames).Where(m => m.ListNames.Any(l => l.IsRejected == true)).ToListAsync();
            ViewBag.ClientList = new SelectList(clients.OrderBy(m => m.Name), "Id", "Name", HttpContext.Session["clientGuidRejected"] as Guid?);

            ViewBag.WordSort = string.IsNullOrEmpty(sortOrder) ? "wordDesc" : "";

            var listID = HttpContext.Session["listGuidRejected"] as Guid?;
            int? filterLength = HttpContext.Session["LengthFromFilterRejected"] as int?;

            if (sortOrder == "wordDesc")
            {
                VersionRejectedWords = db.RejectedWords.OrderByDescending(m => m.Word).Where(a => a.ListNameId == listID).OrderByDescending(b => b.Word);

                if (HttpContext.Session["LengthFromFilterRejected"] as int? != null)
                {
                    VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listID && a.Word.Length == filterLength).OrderByDescending(b => b.Word);
                }

                if (VersionRejectedWords.Count() < 1)
                {
                    VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listID).OrderByDescending(b => b.Word);
                }

            }
            else
            {

                VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listID).OrderBy(b => b.Word);

                if (HttpContext.Session["LengthFromFilterRejected"] as int? != null)
                {

                    VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listID && a.Word.Length == filterLength).OrderBy(b => b.Word);
                }

                if (VersionRejectedWords.Count() < 1)
                {
                    VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listID).OrderBy(b => b.Word);
                }
            }

            //isRedirect = true;
            ViewBag.wordCount = "(" + VersionRejectedWords.Count().ToString() + ")";
            return View(await VersionRejectedWords.ToListAsync());
        }


        // GET: RejectedWords/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RejectedWord rejectedWord = await db.RejectedWords.FindAsync(id);
            if (rejectedWord == null)
            {
                return HttpNotFound();
            }
            isRedirect = true;
            return View(rejectedWord);
        }

        // GET: RejectedWords/Create/5
        public ActionResult Create()
        {
           
            isRedirect = true;
            if (HttpContext.Session["listGuidRejected"] as Guid? == Guid.Empty)
            {
                return RedirectToAction("Index");
            }
            var ListGuid = HttpContext.Session["listGuidRejected"] as Guid?;
            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == ListGuid && p.Archive == false && p.IsRejected == true).OrderBy(m => m.listName), "Id", "listName");
            return View();
        }

        // POST: RejectedWords/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,ListNameId,Word")] RejectedWord rejectedWord)
        {
            RejectedWord tempLNameID = new RejectedWord();
            tempLNameID.ListNameId = rejectedWord.ListNameId;

            if (ModelState.IsValid)
            {
                System.Web.HttpContext.Current.Session["hasAdded_r"] = 1;
                string text = rejectedWord.Word;

                MatchCollection matches = Regex.Matches(text, @"[\w\d_]+", RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        bool canSave = true;
                        RejectedWord parsedWord = new RejectedWord();
                        parsedWord.Id = Guid.NewGuid();
                        parsedWord.Word = match.Value.ToUpperInvariant();
                        parsedWord.ListNameId = rejectedWord.ListNameId;
                        rejectedWord = parsedWord;

                        //don't save the word to anything if it already belongs to this list
                        var listID = HttpContext.Session["listGuidRejected"] as Guid?;
                        if (await db.RejectedWords.Where(m => m.ListNameId == listID).AnyAsync(p => p.Word == rejectedWord.Word))
                        {
                            canSave = false;
                        }

                       
                        //remove word from all approved lists
                        List<ApprovedWord> foundWords = await db.ApprovedWords.Where(x => x.Word == rejectedWord.Word).ToListAsync();
                        if (foundWords != null)
                        {
                            db.ApprovedWords.RemoveRange(foundWords);
                            await db.SaveChangesAsync();
                        }


                        //save the word to both this rejected list...
                        if (canSave)
                        {
                            rejectedWord.ListNameId = tempLNameID.ListNameId;
                            System.Web.HttpContext.Current.Session["hasAdded_r"] = 1;
                            db.RejectedWords.Add(rejectedWord);
                            await db.SaveChangesAsync();
                        }

                         //...and only to the master rejected list if it's not already in there
                        if ( canSave && await db.RejectedWords.Where(m => m.ListName.listName == masterRejectedListName).AllAsync(p => p.Word != rejectedWord.Word))
                        {
                            var masterListID = await db.ListNames.Where(m => m.listName == masterRejectedListName).FirstAsync();
                            if (rejectedWord.ListNameId != masterListID.Id)
                            {
                                rejectedWord.ListNameId = masterListID.Id;
                                db.RejectedWords.Add(rejectedWord);
                                await db.SaveChangesAsync();
                            }
                          
                        }
                    }
                }

                return RedirectToAction("Index");
            }

            ViewBag.ListNameId = new SelectList(db.ListNames.OrderBy(m => m.listName), "Id", "listName", rejectedWord.ListNameId);
            return View(rejectedWord);
        }

        // GET: RejectedWords/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            isRedirect = true;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RejectedWord rejectedWord = await db.RejectedWords.FindAsync(id);
            if (rejectedWord == null)
            {
                return HttpNotFound();
            }
            Guid? list_ID = HttpContext.Session["listGuidRejected"] as Guid?;
            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == list_ID && p.Archive == false && p.IsRejected == true).OrderBy(m => m.listName), "Id", "listName");
            return View(rejectedWord);
        }

        // POST: RejectedWords/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,ListNameId,Word")] RejectedWord rejectedWord)
        {

            Guid? list_ID = HttpContext.Session["listGuidRejected"] as Guid?;
            if (ModelState.IsValid)
            {
                bool canSave = true;

                if (await db.RejectedWords.Where(m => m.ListNameId == list_ID).AnyAsync(p => p.Word == rejectedWord.Word))
                {
                    canSave = false;
                }

                List<ApprovedWord> foundWords = await db.ApprovedWords.Where(x => x.Word == rejectedWord.Word).ToListAsync();

                if (foundWords != null)
                {
                    db.ApprovedWords.RemoveRange(foundWords);
                    await db.SaveChangesAsync();
                 }

                //save the word to both this rejected list...
                if (canSave)
                {
                    System.Web.HttpContext.Current.Session["hasAdded_r"] = 1;
                    rejectedWord.Word = rejectedWord.Word.ToUpperInvariant();
                    db.Entry(rejectedWord).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }

                //...and only to the master rejected list if it's not already in there
                if (canSave && await db.RejectedWords.Where(m => m.ListName.listName == masterRejectedListName).AllAsync(p => p.Word != rejectedWord.Word))
                {
                    var masterListID = await db.ListNames.Where(m => m.listName == masterRejectedListName).FirstAsync();
                    if (rejectedWord.ListNameId != masterListID.Id)
                    {

                        rejectedWord.ListNameId = masterListID.Id;
                        db.RejectedWords.Add(rejectedWord);
                        await db.SaveChangesAsync();
                    }
                }
                    return RedirectToAction("Index");
            }


            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == list_ID && p.Archive == false && p.IsRejected == true).OrderBy(m => m.listName), "Id", "listName");
            return View(rejectedWord);
        }

        // GET: RejectedWords/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            isRedirect = true;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RejectedWord rejectedWord = await db.RejectedWords.FindAsync(id);
            if (rejectedWord == null)
            {
                return HttpNotFound();
            }
            return View(rejectedWord);
        }

        // POST: RejectedWords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            System.Web.HttpContext.Current.Session["hasAdded_r"] = 1;
            RejectedWord rejectedWord = await db.RejectedWords.FindAsync(id);
            db.RejectedWords.Remove(rejectedWord);
            await db.SaveChangesAsync();
            isRedirect = true;
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
