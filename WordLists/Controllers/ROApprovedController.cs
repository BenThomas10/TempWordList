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
    public class ROApprovedController : Controller
    {

        //this name controls the master rejected list name to look for when rejecting approved words
        private string masterRejectedListName = "Master Rejected Word List";

        private ApplicationDbContext db = new ApplicationDbContext();
        private bool isRedirect = false;
        private static List<int> Lengths = new List<int>();
        private IEnumerable<int> WordLengthsDistinct;
        private IOrderedQueryable<ApprovedWord> VersionApprovedWords;
        private List<SelectListItem> emptyList = new List<SelectListItem>();
        // GET: ApprovedWords
        public async Task<ActionResult> Index(Guid? ClientFilter, Guid? VersionFilter, int? LengthFilter, string sortOrder, bool LN = false)
        {
            //HttpContext.Session["list_canNull"] = true;

            //hides the view's add button and title
            ViewBag.CanEdit = false;
            ViewBag.DisableDD = false;

            //view dropdown default selection text
            ViewBag.DefaultClientName = "- Client -";
            ViewBag.DefaultListName = "- List Name -";
            ViewBag.RO_Client = "";
            if (!LN)
            {

                if (ClientFilter == null && VersionFilter == null && LengthFilter == null &&
                    System.Web.HttpContext.Current.Session["clientGuid"] as Guid? != Guid.Empty && System.Web.HttpContext.Current.Session["listGuid"] as Guid? != Guid.Empty)
                {
                    ClientFilter = System.Web.HttpContext.Current.Session["clientGuid"] as Guid?;
                    VersionFilter = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
                    LengthFilter = System.Web.HttpContext.Current.Session["LengthFromFilter"] as int?;
                }

                if (isRedirect)
                {
                    ViewBag.CanEdit = true;
                    isRedirect = false;
                }

                if (ClientFilter != null)
                {
                    isRedirect = true;
                    ViewBag.DefaultClientName = null;

                    if (ClientFilter != System.Web.HttpContext.Current.Session["clientGuid"] as Guid?)
                    {
                        System.Web.HttpContext.Current.Session["clientGuid"] = new Guid(ClientFilter.ToString());
                        Lengths.Clear();
                        VersionFilter = null;
                        System.Web.HttpContext.Current.Session["LengthFromFilter"] = null;
                    }

                    if (VersionFilter != null && ClientFilter != null)
                    {
                        ViewBag.DefaultListName = null;
                        ViewBag.CanEdit = true;
                        if (System.Web.HttpContext.Current.Session["LengthFromFilter"] as int? != LengthFilter)
                        {
                            ViewBag.DefaultListName = null;
                        }
                        System.Web.HttpContext.Current.Session["LengthFromFilter"] = LengthFilter;


                        if (VersionFilter != System.Web.HttpContext.Current.Session["listGuid"] as Guid? || System.Web.HttpContext.Current.Session["hasAdded"] as int? != null)
                        {
                            if (System.Web.HttpContext.Current.Session["hasAdded"] as int? == null)
                                System.Web.HttpContext.Current.Session["listGuid"] = new Guid(VersionFilter.ToString());

                            Lengths.Clear();
                            var list_Guid = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
                            foreach (var aw in db.ApprovedWords.Where(a => a.ListNameId == list_Guid))
                            {
                                Lengths.Add(aw.Word.Length);
                            }
                        }
                    }
                    else
                    {
                        ViewBag.CanEdit = false;
                        Lengths.Clear();
                        System.Web.HttpContext.Current.Session["LengthFromFilter"] = null;
                    }
                }

                var versions = await db.ListNames.OrderBy(m => m.listName).ToListAsync();
                ViewBag.VersionList = new SelectList(versions.Where(m => m.ClientId == System.Web.HttpContext.Current.Session["clientGuid"] as Guid? && m.Archive == false && m.IsRejected == false)
                    .OrderBy(m => m.listName).ToList(), "Id", "listName", System.Web.HttpContext.Current.Session["listGuid"] as Guid?);

                ListName tempList = versions.Where(m => m.Id == System.Web.HttpContext.Current.Session["listGuid"] as Guid?).First();
                ViewBag.RO_List = tempList.listName;

                //disable archive dropdowns
                if (versions.Where(z => z.Archive == true).Where(p => p.Id == VersionFilter).Count() > 0)
                {
                    ViewBag.DisableDD = true;
                }

                if (ClientFilter == null && VersionFilter != null)
                {
                    ViewBag.VersionList = new SelectList(items: "");
                    System.Web.HttpContext.Current.Session["LengthFromFilter"] = null;
                    Lengths.Clear();
                    isRedirect = false;
                    ViewBag.CanEdit = false;
                    System.Web.HttpContext.Current.Session["listGuid"] = Guid.Empty;
                    System.Web.HttpContext.Current.Session["clientGuid"] = Guid.Empty;
                }

                System.Web.HttpContext.Current.Session["hasAdded"] = null;
                WordLengthsDistinct = Lengths.Distinct();
                ViewBag.WordLengths = new SelectList(WordLengthsDistinct.OrderBy(p => p), System.Web.HttpContext.Current.Session["LengthFromFilter"] as int?);

                var clients = await db.Clients.Include(m => m.ListNames).Where(m => m.ListNames.Any(l => l.IsRejected == false)).ToListAsync();
                ViewBag.ClientList = new SelectList(clients.OrderBy(m => m.Name), "Id", "Name", System.Web.HttpContext.Current.Session["clientGuid"] as Guid?);

                Client tempClient = clients.Where(m => m.Id == System.Web.HttpContext.Current.Session["clientGuid"] as Guid?).First();
                ViewBag.RO_Client = tempClient.Name;

                ViewBag.WordSort = string.IsNullOrEmpty(sortOrder) ? "wordDesc" : "";

                var listID = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
                int? filterLength = System.Web.HttpContext.Current.Session["LengthFromFilter"] as int?;

                if (sortOrder == "wordDesc")
                {
                    VersionApprovedWords = db.ApprovedWords.OrderByDescending(m => m.Word).Where(a => a.ListNameId == listID).OrderByDescending(b => b.Word);

                    if (System.Web.HttpContext.Current.Session["LengthFromFilter"] as int? != null)
                    {
                        VersionApprovedWords = db.ApprovedWords.Where(a => a.ListNameId == listID && a.Word.Length == filterLength).OrderByDescending(b => b.Word);
                    }

                    if (VersionApprovedWords.Count() < 1)
                    {
                        VersionApprovedWords = db.ApprovedWords.Where(a => a.ListNameId == listID).OrderByDescending(b => b.Word);
                    }

                }
                else
                {

                    VersionApprovedWords = db.ApprovedWords.Where(a => a.ListNameId == listID).OrderBy(b => b.Word);

                    if (System.Web.HttpContext.Current.Session["LengthFromFilter"] as int? != null)
                    {

                        VersionApprovedWords = db.ApprovedWords.Where(a => a.ListNameId == listID && a.Word.Length == filterLength).OrderBy(b => b.Word);
                    }

                    if (VersionApprovedWords.Count() < 1)
                    {
                        VersionApprovedWords = db.ApprovedWords.Where(a => a.ListNameId == listID).OrderBy(b => b.Word);
                    }
                }

                ViewBag.wordCount = "(" + VersionApprovedWords.Count().ToString() + ")";
                return View(await VersionApprovedWords.ToListAsync());
                }
            else
            {
                System.Web.HttpContext.Current.Session["clientGuid"] = new Guid(ClientFilter.ToString());
                System.Web.HttpContext.Current.Session["listGuid"] = new Guid(VersionFilter.ToString());
                System.Web.HttpContext.Current.Session["hasAdded"] = 1;
                ViewBag.DefaultClientName = null;
                ViewBag.DefaultListName = null;
                Response.Redirect("~/ROApproved");
                Lengths.Clear();
                var list_Guid = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
                foreach (var aw in db.ApprovedWords.Where(a => a.ListNameId == list_Guid))
                {
                    Lengths.Add(aw.Word.Length);
                }

                var clients = await db.Clients.Include(m => m.ListNames).Where(m => m.ListNames.Any(l => l.IsRejected == false)).ToListAsync();
                ViewBag.ClientList = new SelectList(clients.OrderBy(m => m.Name), "Id", "Name");

                Client tempClient = clients.Where(m => m.Id == System.Web.HttpContext.Current.Session["clientGuid"] as Guid?).First();
                ViewBag.RO_Client = tempClient.Name;

                var versions = await db.ListNames.OrderBy(m => m.listName).ToListAsync();
                ViewBag.VersionList = new SelectList(versions.Where(m => m.ClientId == ClientFilter &&
                                      m.Archive == false && m.IsRejected == false).OrderBy(m => m.listName).ToList(), "Id", "listName", VersionFilter);

                ListName tempList = versions.Where(m => m.Id == System.Web.HttpContext.Current.Session["listGuid"] as Guid?).First();
                ViewBag.RO_List = tempList.listName;

                WordLengthsDistinct = Lengths.Distinct();
                ViewBag.WordLengths = new SelectList(WordLengthsDistinct.OrderBy(p => p));

                VersionApprovedWords = db.ApprovedWords.Where(a => a.ListNameId == list_Guid).OrderBy(b => b.Word);
                ViewBag.wordCount = "(" + VersionApprovedWords.Count().ToString() + ")";

                System.Web.HttpContext.Current.Session["LengthFromFilter"] = LengthFilter;
                ViewBag.CanEdit = true;
                
                return View(await VersionApprovedWords.ToListAsync());
            }

        }

        // GET: ApprovedWords/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApprovedWord approvedWord = await db.ApprovedWords.FindAsync(id);
            if (approvedWord == null)
            {
                return HttpNotFound();
            }
            isRedirect = true;
            return View(approvedWord);
        }

        // GET: ApprovedWords/Create/5
        public ActionResult Create()
        {
            isRedirect = true;
            ViewBag.Warning = null;

            var ListGuid = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
            var lists = new SelectList(db.ListNames.Where(p => p.Id == ListGuid && p.Archive == false && p.IsRejected == false).OrderBy(m => m.listName), "Id", "listName");
            if (lists.Count() < 1 || System.Web.HttpContext.Current.Session["listGuid"] as Guid? == Guid.Empty || System.Web.HttpContext.Current.Session["listGuid"] as Guid? == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.ListNameId = lists;

            return View();
        }

        // POST: ApprovedWords/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,ListNameId,Word")] ApprovedWord approvedWord)
        {
            ViewBag.Warning = "Please wait...";
            if (ModelState.IsValid)
            {

                System.Web.HttpContext.Current.Session["hasAdded"] = 1;
                ViewBag.Processing = true;
                string text = approvedWord.Word;
                MatchCollection matches = Regex.Matches(text, @"[\w\d_]+", RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        bool canSave = true;
                        ApprovedWord parsedWord = new ApprovedWord();
                        parsedWord.Id = Guid.NewGuid();
                        parsedWord.Word = match.Value.ToUpperInvariant();
                        parsedWord.ListNameId = approvedWord.ListNameId;
                        approvedWord = parsedWord;

                        var listID = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
                        if (await db.ApprovedWords.Where(m => m.ListNameId == listID).AnyAsync(p => p.Word == approvedWord.Word))
                        {
                            canSave = false;
                        }

                        //Color (true) if on master rejected word list
                        if (await db.RejectedWords.Where(m => m.ListName.listName == masterRejectedListName).AnyAsync(p => p.Word == approvedWord.Word))
                        {
                            approvedWord.onRejected = true;
                        }

                        if (canSave)
                        {
                            db.ApprovedWords.Add(approvedWord);
                            await db.SaveChangesAsync();
                        }
                    }
                }


                return RedirectToAction("Index");
            }

            ViewBag.ListNameId = new SelectList(db.ListNames.OrderBy(m => m.listName), "Id", "listName", approvedWord.ListNameId);
            return View(approvedWord);
        }

        // GET: ApprovedWords/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            isRedirect = true;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApprovedWord approvedWord = await db.ApprovedWords.FindAsync(id);
            if (approvedWord == null)
            {
                return HttpNotFound();
            }
            Guid? list_ID = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == list_ID && p.Archive == false && p.IsRejected == false).OrderBy(m => m.listName), "Id", "listName");
            return View(approvedWord);
        }

        // POST: ApprovedWords/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,ListNameId,Word")] ApprovedWord approvedWord)
        {

            Guid? list_ID = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
            if (ModelState.IsValid)
            {
                bool canSave = true;

                if (await db.ApprovedWords.Where(m => m.ListNameId == list_ID).AnyAsync(p => p.Word == approvedWord.Word))
                {
                    canSave = false;
                }

                if (await db.RejectedWords.Where(m => m.ListName.listName == masterRejectedListName).AnyAsync(p => p.Word == approvedWord.Word))
                {
                    canSave = false;
                }

                if (canSave)
                {
                    approvedWord.Word = approvedWord.Word.ToUpperInvariant();
                    System.Web.HttpContext.Current.Session["hasAdded"] = 1;
                    db.Entry(approvedWord).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }
                return RedirectToAction("Index");
            }
            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == list_ID && p.Archive == false && p.IsRejected == false).OrderBy(m => m.listName), "Id", "listName");
            return View(approvedWord);
        }

        // GET: ApprovedWords/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            isRedirect = true;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApprovedWord approvedWord = await db.ApprovedWords.FindAsync(id);
            if (approvedWord == null)
            {
                return HttpNotFound();
            }
            return View(approvedWord);
        }

        // POST: ApprovedWords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            ApprovedWord approvedWord = await db.ApprovedWords.FindAsync(id);
            db.ApprovedWords.Remove(approvedWord);
            await db.SaveChangesAsync();
            isRedirect = true;
            System.Web.HttpContext.Current.Session["hasAdded"] = 1;
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

        // GET: ApprovedWords/_PartialWaiting
        public ActionResult _PartialWaiting()
        {
            ViewBag.Warning = "Please Wait...";
            return View();
        }
    }
}
