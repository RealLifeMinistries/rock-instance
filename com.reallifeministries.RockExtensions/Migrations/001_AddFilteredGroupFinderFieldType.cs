namespace com.reallifeministries.RockExtensions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    using Rock.Migrations;
    using Rock.Plugin;
    
    [MigrationNumber(1, "1.0.0")]
    public partial class AddFilteredGroupFinderFieldType : Migration
    {
        private string _fieldTypeGuid = "2AC9283D-7F82-4DBF-A4B2-B19D3A83C372";
        public override void Up()
        {
            RockMigrationHelper.UpdateFieldType( "Filtered Group Finder",
                "Context aware group selector/finder for workflows",
                "com.reallifeministries.RockExtensions",
                "com.reallifeministries.RockExtensions.Field.Types.FilteredGroupFinderFieldType",
                _fieldTypeGuid, false );
        }
        
        public override void Down()
        {
            RockMigrationHelper.DeleteFieldType( _fieldTypeGuid );
        }
    }
}
