namespace WordLists.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class changeVersionToName : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.ListVersions", newName: "ListNames");
            RenameColumn(table: "dbo.ApprovedWords", name: "ListVersionId", newName: "ListNameId");
            RenameColumn(table: "dbo.RejectedWords", name: "ListVersionId", newName: "ListNameId");
            RenameIndex(table: "dbo.ApprovedWords", name: "IX_ListVersionId", newName: "IX_ListNameId");
            RenameIndex(table: "dbo.RejectedWords", name: "IX_ListVersionId", newName: "IX_ListNameId");
            AddColumn("dbo.ListNames", "listName", c => c.String());
            DropColumn("dbo.ListNames", "listName");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ListNames", "listName", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            DropColumn("dbo.ListNames", "listName");
            RenameIndex(table: "dbo.RejectedWords", name: "IX_ListNameId", newName: "IX_ListVersionId");
            RenameIndex(table: "dbo.ApprovedWords", name: "IX_ListNameId", newName: "IX_ListVersionId");
            RenameColumn(table: "dbo.RejectedWords", name: "ListNameId", newName: "ListVersionId");
            RenameColumn(table: "dbo.ApprovedWords", name: "ListNameId", newName: "ListVersionId");
            RenameTable(name: "dbo.ListNames", newName: "ListVersions");
        }
    }
}
