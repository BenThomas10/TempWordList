namespace WordLists.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class requireFields : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ApprovedWords", "Word", c => c.String(nullable: false));
            AlterColumn("dbo.ListNames", "listName", c => c.String(nullable: false));
            AlterColumn("dbo.Clients", "Name", c => c.String(nullable: false));
            AlterColumn("dbo.RejectedWords", "Word", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.RejectedWords", "Word", c => c.String());
            AlterColumn("dbo.Clients", "Name", c => c.String());
            AlterColumn("dbo.ListNames", "listName", c => c.String());
            AlterColumn("dbo.ApprovedWords", "Word", c => c.String());
        }
    }
}
