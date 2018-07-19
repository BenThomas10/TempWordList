﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WordLists.Models
{
    public class Client
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        [Display(Name = "Client Name")]
        public string Name { get; set; }

        public ICollection<ListName> ListNames { get; set; }
    }

    public class ListName
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }

        [ForeignKey("ClientId")]
        public Client Client { get; set; }

        [Display(Name = "List Name")]
        public string listName { get; set; }
        public bool Live { get; set; }
        [Display(Name = "Rejected Words List")]
        public bool IsRejected { get; set; } = false;

        public ICollection<ApprovedWord> ApprovedWords { get; set; }
        public ICollection<RejectedWord> RejectedWords { get; set; }
    }

    public class ApprovedWord
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public Guid ListNameId { get; set; }

        [ForeignKey("ListNameId")]
        [Display(Name = "List Name")]
        public ListName ListName { get; set; }
        
        [Display(Name = "Approved Words")]
        public string Word { get; set; }
    }

    public class RejectedWord
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public Guid ListNameId { get; set; }

        [ForeignKey("ListNameId")]
        public ListName ListName { get; set; }

        [Display(Name = "Rejected Words")]
        public string Word { get; set; }
    }
}