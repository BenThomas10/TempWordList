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
using System.Web.UI.WebControls;
using System.IO;
using System.Web.UI;
using System.Text.RegularExpressions;

namespace WordLists.Controllers
{
    public class ROListsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private string masterRejectedListName = "Master Rejected Word List";

        // GET: ListNames
        public async Task<ActionResult> Index(Guid? ClientFilter, bool? ShowArchive, string sortOrder)
        {

            if (ClientFilter == null && HttpContext.Session["list_clientGuid"] as Guid? != Guid.Empty)
            {
                ClientFilter = HttpContext.Session["list_clientGuid"] as Guid?;
                if(HttpContext.Session["clientSelected"] as bool? != true)
                {
                    ViewBag.DefaultClientName = " - Client - ";
                }
                
            }
            else
            {
                ViewBag.DefaultClientName = null;
                HttpContext.Session["clientSelected"] = true;
            }

            if (ShowArchive == null)
            {
                if (HttpContext.Session["showArchive"] as bool? != null)
                {
                    ShowArchive = HttpContext.Session["showArchive"] as bool?;
                }else
                {
                    ShowArchive = false;
                }
            }
           
            HttpContext.Session["showArchive"] = ShowArchive;

            if (HttpContext.Session["showArchive"] as bool? == false)
            {
                ViewBag.Archive = false;
            }
            else
            {
                ViewBag.Archive = true;
            }

            ViewBag.Warning = null;

            List<ListName> filteredLists = new List<ListName>();
            
            if (ViewBag.Archive)
            {
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
            }
            else
            {
                if (ClientFilter == null && HttpContext.Session["list_clientGuid"] as Guid? == null)
                {
                    filteredLists = await db.ListNames.Where(c => c.Archive == false).OrderBy(p => p.Client.Name).ThenBy(p => p.listName).ToListAsync();
                    HttpContext.Session["list_canNull"] = false;
                }
                else if (ClientFilter == null && HttpContext.Session["list_clientGuid"] as Guid? != null)
                {
                    if (HttpContext.Session["list_canNull"] as bool? == true)
                    {
                        var filtered_giud = HttpContext.Session["list_clientGuid"] as Guid?;
                        filteredLists = await db.ListNames.Where(p => p.ClientId == filtered_giud).Where(x => x.Archive == false).OrderBy(p => p.listName).ToListAsync();
                        HttpContext.Session["list_canNull"] = false;
                    }
                    else
                    {
                        filteredLists = await db.ListNames.Where(x => x.Archive == false).OrderBy(p => p.Client.Name).ThenBy(p => p.listName).ToListAsync();
                        HttpContext.Session["list_clientGuid"] = null;
                    }

                }
                else
                {
                    filteredLists = await db.ListNames.Where(p => p.ClientId == ClientFilter).Where(x => x.Archive == false).OrderBy(p => p.Client.Name).ToListAsync();
                    HttpContext.Session["list_clientGuid"] = new Guid(ClientFilter.ToString());
                }
            }

            ViewBag.NameSort = string.IsNullOrEmpty(sortOrder) ? "nameDesc" : "";
            ViewBag.ArchiveSort = sortOrder == "Archive" ? "archiveDesc" : "Archive";
            ViewBag.RejectedSort = sortOrder == "Rejected" ? "rejectedDesc" : "Rejected";
            ViewBag.CreatedSort = sortOrder == "Created" ? "createdDesc" : "Created";
            ViewBag.ModifiedSort = sortOrder == "Modified" ? "modifiedDesc" : "Modified";
            ViewBag.ID_Sort = sortOrder == "List ID" ? "listIdDesc" : "List ID";
            switch (sortOrder)
            {
                case "nameDesc":
                    filteredLists = filteredLists.OrderByDescending(m => m.listName).ThenBy(m => m.DateModified).ToList();
                    break;
                case "archiveDesc":
                    filteredLists = filteredLists.OrderByDescending(m => m.Archive).ThenBy(m => m.listName).ToList();
                    break;
                case "rejectedDesc":
                    filteredLists = filteredLists.OrderByDescending(m => m.IsRejected).ThenBy(m => m.listName).ToList();
                    break;
                case "createdDesc":
                    filteredLists = filteredLists.OrderByDescending(m => m.DateCreated).ThenBy(m => m.listName).ToList();
                    break;
                case "modifiedDesc":
                    filteredLists = filteredLists.OrderByDescending(m => m.DateModified).ThenBy(m => m.listName).ToList();
                    break;
                case "listIdDesc":
                    filteredLists = filteredLists.OrderByDescending(m => m.DateModified).ThenBy(m => m.listName).ToList();
                    break;
                case "Archive":
                    filteredLists = filteredLists.OrderBy(m => m.Archive).ThenBy(m => m.DateModified).ToList();
                    break;
                case "Rejected":
                    filteredLists = filteredLists.OrderBy(m => m.IsRejected).ThenBy(m => m.DateModified).ToList();
                    break;
                case "Created":
                    filteredLists = filteredLists.OrderBy(m => m.DateCreated).ThenBy(m => m.DateModified).ToList();
                    break;
                case "Modified":
                    filteredLists = filteredLists.OrderBy(m => m.DateModified).ThenBy(m => m.DateModified).ToList();
                    break;
                case "List ID":
                    filteredLists = filteredLists.OrderBy(m => m.ListID).ThenBy(m => m.DateModified).ToList();
                    break;
                default:
                    filteredLists = filteredLists.OrderBy(m => m.listName).ThenBy(m => m.DateModified).ToList();
                    break;
            }
            ViewBag.ClientList = new SelectList(db.Clients.OrderBy(m => m.Name), "Id", "Name", HttpContext.Session["list_clientGuid"] as Guid?);
            ViewBag.ListCount = "(" + filteredLists.Count().ToString() + ")";
            return View(filteredLists);
        }


        // GET: ExportData
        public async Task<ActionResult> ExportToWord(Guid? ListFilter, bool Rejected = false)
        {
            // get the data from database
            List<string> parsedSet = new List<string>();
            GridView gridview = new GridView();
            if (!Rejected)
            {
                List<ApprovedWord> data = await db.ApprovedWords.Where(p => p.ListNameId == ListFilter).ToListAsync();               
                foreach(var wordset in data)
                {
                    parsedSet.Add(wordset.Word.ToString());
                }
                parsedSet = parsedSet.OrderBy(p => p.Length).ThenBy(x => x).ToList();
                gridview.DataSource = parsedSet;
            }
            else
            {
                List<RejectedWord> data = await db.RejectedWords.Where(p => p.ListNameId == ListFilter).ToListAsync();
                foreach (var wordset in data)
                {
                    parsedSet.Add(wordset.Word.ToString());
                }
                parsedSet = parsedSet.OrderBy(p => p.Length).ThenBy(x => x).ToList();
                gridview.DataSource = parsedSet;
            }

            // set the data source
            ListName _list = db.ListNames.Where(p => p.Id == ListFilter).First();
            gridview.BorderWidth = 0;
            gridview.CaptionAlign = TableCaptionAlign.Left;
            gridview.HorizontalAlign = HorizontalAlign.Left;
            gridview.Caption ="<b>" + _list.listName.ToString() + "</b>";
            gridview.ShowHeader = false; 
            gridview.DataBind();
            //gridview.Style.Add("Color", "White");
            // Clear all the content from the current response
            Response.ClearContent();
            Response.Buffer = true;
            // set the header
            
            Response.AddHeader("content-disposition", "attachment; filename = "+ _list.listName.ToString() + ".doc");
            Response.ContentType = "application/ms-word";
            Response.Charset = "";
            // create HtmlTextWriter object with StringWriter
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                {
                    // render the GridView to the HtmlTextWriter
                    gridview.RenderControl(htw);
                    // Output the GridView content saved into StringWriter
                    Response.Output.Write(sw.ToString());
                    Response.Flush();
                    Response.End();
                }
            }
            return View();
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
        public async Task<ActionResult> Create([Bind(Include = "Id,ClientId,listName,IsRejected,Archive,DateCreated,DateModified")] ListName Name, string Word)
        {
            
            bool canCreate = true;
            bool canSave = true;
            if (ModelState.IsValid)
            {
                if (Name.listName == masterRejectedListName)
                {
                    if (db.ListNames.Where(p => p.listName == masterRejectedListName).Count() > 0)
                    {
                        canCreate = false;
                        ViewBag.Warning = masterRejectedListName.ToString() + " has already been created.";
                    }
                }

                //if (db.ListNames.Where(p => p.listName == Name.listName).Count() > 0)
                //{
                //    canCreate = false;
                //    ViewBag.Warning = Name.listName + " has already been created.";
                //}

                if (canCreate)
                {
                    Name.DateCreated = DateTime.Now;
                    Name.DateModified = DateTime.Now;
                    ViewBag.Warning = null;
                    Name.Id = Guid.NewGuid();
                    db.ListNames.Add(Name);
                    await db.SaveChangesAsync();
                }
            }


            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name", Name.ClientId);
            System.Web.HttpContext.Current.Session["hasAdded"] = 1;
            
            if (Name.IsRejected == false)
            {

                if (ModelState.IsValid)
                {
                    ViewBag.Warning = "Working. Please wait...";
                    System.Web.HttpContext.Current.Session["hasAdded"] = 1;
                    ViewBag.Processing = true;
                    ApprovedWord approvedWord = new ApprovedWord();
                    approvedWord.Id = Guid.NewGuid();
                    approvedWord.ListNameId = Name.Id;
                    approvedWord.Word = Word;
                    string textAW = approvedWord.Word;
                    MatchCollection matches = Regex.Matches(textAW, @"[\w\d_]+", RegexOptions.Singleline);
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            canSave = true;
                            ApprovedWord parsedWord = new ApprovedWord();
                            parsedWord.Id = Guid.NewGuid();
                            parsedWord.Word = match.Value.ToUpperInvariant();
                            parsedWord.ListNameId = approvedWord.ListNameId;
                            approvedWord = parsedWord;

                            if (await db.ApprovedWords.Where(m => m.ListNameId == Name.Id).AnyAsync(p => p.Word == approvedWord.Word))
                            {
                                canSave = false;
                            }

                            //color if on master rejected
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

                ViewBag.ListNameId = new SelectList(db.ListNames.OrderBy(m => m.listName), "Id", "listName", Name.Id);
                return View(Name);
            }
            else
            {
                ViewBag.Warning = "Working. Please wait...";

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
                tempLNameID.ListNameId = Name.Id;

                if (ModelState.IsValid)
                {
                    var masterListID = await db.ListNames.Where(m => m.listName == masterRejectedListName).FirstAsync();

                    System.Web.HttpContext.Current.Session["hasAdded_r"] = 1;
                    RejectedWord rejectedWord = new RejectedWord();
                    rejectedWord.Id = Guid.NewGuid();
                    rejectedWord.ListNameId = Name.Id;
                    rejectedWord.Word = Word;
                    string text = rejectedWord.Word;

                    MatchCollection matches = Regex.Matches(text, @"[\w\d_]+", RegexOptions.Singleline);
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            canSave = true;
                            //bool canMasterList = false;
                            RejectedWord parsedWord = new RejectedWord();
                            parsedWord.Id = Guid.NewGuid();
                            parsedWord.Word = match.Value.ToUpperInvariant();
                            parsedWord.ListNameId = Name.Id;
                            rejectedWord = parsedWord;

                            //don't save the word to anything if it already belongs to this list
                            
                            if (await db.RejectedWords.Where(m => m.ListNameId == Name.Id).AnyAsync(p => p.Word == rejectedWord.Word))
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

                ViewBag.ListNameId = new SelectList(db.ListNames.OrderBy(m => m.listName), "Id", "listName", Name.Id);
            }

            return View(Name);
        }

        // GET: ListNames/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            ViewBag.HasData = false;
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

            //Prevent changing a word list to rejected/approved if it contains words
            ViewBag.ClientId = new SelectList(db.Clients, "Id", "Name", listName.ClientId);
            int hasData = db.ApprovedWords.Where(z => z.ListNameId == id).Count();
            hasData += db.RejectedWords.Where(z => z.ListNameId == id).Count();
            if (hasData > 0)
            {
                ViewBag.HasData = true;
            }
            return View(listName);
        }

        // POST: ListNames/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,ClientId,listName,IsRejected,Archive,DateCreated,DateModified")] ListName Name)
        {
            ViewBag.hasData = true;
            bool canCreate = true;
            if (ModelState.IsValid)
            {
                if (Name.listName == masterRejectedListName)
                {
                    if (db.ListNames.Where(p => p.listName == masterRejectedListName).Count() > 0)
                    {
                        canCreate = false;
                        ViewBag.Warning = masterRejectedListName.ToString() + " has already been created.";
                    }
                }

                //Only one list name per client
                //if (db.ListNames.Where(p => p.listName == Name.listName).Where(p => p.ClientId == Name.ClientId).Where(p => p.Archive == false).Count() > 0)
                //{
                //        canCreate = false;
                //        ViewBag.Warning = Name.listName + " has already been created for this client";
                //}

                if (canCreate)
                {
                    //ViewBag.hasData = false;
                    Name.DateModified = DateTime.Now;
                    Name.IsRejected = Name.IsRejected;
                    //ListName tempName = new ListName();
                    //tempName = db.ListNames.Where(p => p.Id == Name.Id).First();
                    //Name.IsRejected = tempName.IsRejected;
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
