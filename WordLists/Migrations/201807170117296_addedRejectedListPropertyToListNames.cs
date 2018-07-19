namespace WordLists.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedRejectedListPropertyToListNames : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ListNames", "IsRejected", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ListNames", "IsRejected");
        }
    }
}
