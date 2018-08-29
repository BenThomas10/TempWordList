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
    public class RORejectedController : Controller
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
                        System.Web.HttpContext.Current.Session["LengthFromFilter"] = null;
                    }
                }

                var versions = await db.ListNames.OrderBy(m => m.listName).ToListAsync();
              
                ListName tempList = versions.Where(m => m.Id == System.Web.HttpContext.Current.Session["listGuid"] as Guid?).First();
                ViewBag.RO_List = tempList.listName;

                ViewBag.VersionList = new SelectList(versions.Where(m => m.ClientId == System.Web.HttpContext.Current.Session["clientGuid"] as Guid? && m.Archive == false && m.IsRejected == true)
                    .OrderBy(m => m.listName).ToList(), "Id", "listName", System.Web.HttpContext.Current.Session["listGuid"] as Guid?);

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

                var clients = await db.Clients.Include(m => m.ListNames).Where(m => m.ListNames.Any(l => l.IsRejected == true)).ToListAsync();
                ViewBag.ClientList = new SelectList(clients.OrderBy(m => m.Name), "Id", "Name", System.Web.HttpContext.Current.Session["clientGuid"] as Guid?);

                Client tempClient = clients.Where(m => m.Id == System.Web.HttpContext.Current.Session["clientGuid"] as Guid?).First();
                ViewBag.RO_Client = tempClient.Name;

                ViewBag.WordSort = string.IsNullOrEmpty(sortOrder) ? "wordDesc" : "";

                var listID = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;

                int? filterLength = System.Web.HttpContext.Current.Session["LengthFromFilter"] as int?;

                if (sortOrder == "wordDesc")
                {
                    VersionRejectedWords = db.RejectedWords.OrderByDescending(m => m.Word).Where(a => a.ListNameId == listID).OrderByDescending(b => b.Word);

                    if (System.Web.HttpContext.Current.Session["LengthFromFilter"] as int? != null)
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

                    if (System.Web.HttpContext.Current.Session["LengthFromFilter"] as int? != null)
                    {

                        VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listID && a.Word.Length == filterLength).OrderBy(b => b.Word);
                    }

                    if (VersionRejectedWords.Count() < 1)
                    {
                        VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == listID).OrderBy(b => b.Word);
                    }
                }

                ViewBag.wordCount = "(" + VersionRejectedWords.Count().ToString() + ")";
                return View(await VersionRejectedWords.ToListAsync());
            }
            else
            {
                System.Web.HttpContext.Current.Session["clientGuid"] = new Guid(ClientFilter.ToString());
                System.Web.HttpContext.Current.Session["listGuid"] = new Guid(VersionFilter.ToString());
                System.Web.HttpContext.Current.Session["hasAdded"] = 1;
                ViewBag.DefaultClientName = null;
                ViewBag.DefaultListName = null;
                Response.Redirect("~/RORejected");
                Lengths.Clear();
                var list_Guid = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
                foreach (var aw in db.RejectedWords.Where(a => a.ListNameId == list_Guid))
                {
                    Lengths.Add(aw.Word.Length);
                }

                var clients = await db.Clients.Include(m => m.ListNames).Where(m => m.ListNames.Any(l => l.IsRejected == true)).ToListAsync();
                ViewBag.ClientList = new SelectList(clients.OrderBy(m => m.Name), "Id", "Name");

                
                Client tempClient = clients.Where(m => m.Id == System.Web.HttpContext.Current.Session["clientGuid"] as Guid?).First();
                ViewBag.RO_Client = tempClient.Name;

                var versions = await db.ListNames.OrderBy(m => m.listName).ToListAsync();

                ListName tempList = versions.Where(m => m.Id == list_Guid).First();
                ViewBag.RO_List = tempList.listName;

                ViewBag.VersionList = new SelectList(versions.Where(m => m.ClientId == ClientFilter &&
                                      m.Archive == false && m.IsRejected == true).OrderBy(m => m.listName).ToList(), "Id", "listName", VersionFilter);

                WordLengthsDistinct = Lengths.Distinct();
                ViewBag.WordLengths = new SelectList(WordLengthsDistinct.OrderBy(p => p));

                VersionRejectedWords = db.RejectedWords.Where(a => a.ListNameId == list_Guid).OrderBy(b => b.Word);
                ViewBag.wordCount = "(" + VersionRejectedWords.Count().ToString() + ")";

                System.Web.HttpContext.Current.Session["LengthFromFilter"] = LengthFilter;
                ViewBag.CanEdit = true;

                return View(await VersionRejectedWords.ToListAsync());
            }

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
        public async Task<ActionResult> Create()
        {
            ViewBag.Warning = string.Empty;               
            //ViewBag.OnMaster = false;
            isRedirect = true;
            ViewBag.MRL_name = masterRejectedListName;
            var masterListID = await db.ListNames.Where(m => m.listName == masterRejectedListName).FirstAsync();
            var ListGuid = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;

            #region //Hide View Option if Master
            //If viewing the current Master Rejected List hide options and show warining
            //if (ListGuid == masterListID.Id)
            //{
            //    ViewBag.OnMaster = true;
            //    System.Web.HttpContext.Current.Session["OnMaster"] = true;
            //    ViewBag.Warning = "All words added to " + masterRejectedListName + " will be removed from all approved lists";
            //}
            #endregion

            //Don't allow this view if no list is available to add to
            var lists = new SelectList(db.ListNames.Where(p => p.Id == ListGuid && p.Archive == false && p.IsRejected == true).OrderBy(m => m.listName), "Id", "listName");
            if (lists.Count() < 1 || System.Web.HttpContext.Current.Session["listGuid"] as Guid? == Guid.Empty || System.Web.HttpContext.Current.Session["listGuid"] as Guid? == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.ListNameId = lists;

            //populate client name
            ListName LName = db.ListNames.Where(p => p.Id == ListGuid).First();
            if (LName != null)
            {
                Client CName = db.Clients.Where(p => p.Id == LName.ClientId).First();
                ViewBag.ClientName = CName.Name;
            }
            else
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        // POST: RejectedWords/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,ListNameId,Word")] RejectedWord rejectedWord)
        {

            ViewBag.Warning = string.Empty;

            #region //Hide View Options If Master
            //Hide options if master list
            //if (System.Web.HttpContext.Current.Session["OnMaster"] as bool? == true)
            //{
            //    ViewBag.OnMaster = true;
            //}
            //else
            //{
            //    ViewBag.OnMaster = false;
            //}
            #endregion

            //create current rejected word object for parsing
            RejectedWord tempLNameID = new RejectedWord();
            tempLNameID.ListNameId = rejectedWord.ListNameId;

            if (ModelState.IsValid)
            {
                var masterListID = await db.ListNames.Where(m => m.listName == masterRejectedListName).FirstAsync();
               
                System.Web.HttpContext.Current.Session["hasAdded_r"] = 1;
                string text = rejectedWord.Word;

                MatchCollection matches = Regex.Matches(text, @"[\w\d_]+", RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        bool canSave = true;
                        //bool canMasterList = false;
                        RejectedWord parsedWord = new RejectedWord();
                        parsedWord.Id = Guid.NewGuid();
                        parsedWord.Word = match.Value.ToUpperInvariant();
                        parsedWord.ListNameId = rejectedWord.ListNameId;
                        rejectedWord = parsedWord;

                        //don't save the word to anything if it already belongs to this list
                        var listID = HttpContext.Session["listGuid"] as Guid?;
                        if (await db.RejectedWords.Where(m => m.ListNameId == listID).AnyAsync(p => p.Word == rejectedWord.Word))
                        {
                            canSave = false;
                        }

                        #region //remove from approved
                        //option: remove word from all approved lists and enable gloabal master list save
                        //List<ApprovedWord> foundWords = await db.ApprovedWords.Where(x => x.Word == rejectedWord.Word).ToListAsync();
                        //if (foundWords != null)
                        //{
                        //    if (ViewBag.RemoveAllApproved)
                        //    {
                        //        canMasterList = true;
                        //        db.ApprovedWords.RemoveRange(foundWords);
                        //        await db.SaveChangesAsync();
                        //    }
                        //}

                        //option: remove words from client's approved lists
                        //List<ApprovedWord> foundWordsSome = await db.ApprovedWords.Where(x => x.Word == rejectedWord.Word).Where(p => p.ListName.ClientId == rejectedWord.ListName.ClientId).ToListAsync();
                        //if (foundWordsSome != null)
                        //{
                        //    if (ViewBag.RemoveSomeApproved && !ViewBag.RemoveAllApproved)
                        //    {
                        //        db.ApprovedWords.RemoveRange(foundWordsSome);
                        //        await db.SaveChangesAsync();
                        //    }
                        //}
                        #endregion

                        //save the word to both this rejected list...
                        if (canSave)
                        {
                            rejectedWord.ListNameId = tempLNameID.ListNameId;
                            System.Web.HttpContext.Current.Session["hasAdded_r"] = 1;
                            db.RejectedWords.Add(rejectedWord);
                            await db.SaveChangesAsync();
                        }

                        //...and only to the master rejected list if it's not already in there
                        if (canSave && await db.RejectedWords.Where(m => m.ListName.listName == masterRejectedListName).AllAsync(p => p.Word != rejectedWord.Word))
                        {
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
            Guid? list_ID = System.Web.HttpContext.Current.Session["listGuid"] as Guid?;
            ViewBag.ListNameId = new SelectList(db.ListNames.Where(p => p.Id == list_ID && p.Archive == false && p.IsRejected == true).OrderBy(m => m.listName), "Id", "listName");
            return View(rejectedWord);
        }

        // POST: RejectedWords/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
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

                //don't save the word to anything if it already belongs to this list
                if (await db.RejectedWords.Where(m => m.ListNameId == list_ID).AnyAsync(p => p.Word == rejectedWord.Word))
                {
                    canSave = false;
                }

                #region //remove approved
                //List<ApprovedWord> foundWords = await db.ApprovedWords.Where(x => x.Word == rejectedWord.Word).ToListAsync();

                //if (foundWords != null)
                //{
                //    db.ApprovedWords.RemoveRange(foundWords);
                //    await db.SaveChangesAsync();
                //}
                #endregion

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
            RejectedWord rejectedWord = await db.RejectedWords.FindAsync(id);
            db.RejectedWords.Remove(rejectedWord);
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

        // GET: RejectedWords/_PartialWaiting
        public ActionResult _PartialWaiting()
        {
            ViewBag.Warning = "Please Wait...";
            return View();
        }
    }
}
