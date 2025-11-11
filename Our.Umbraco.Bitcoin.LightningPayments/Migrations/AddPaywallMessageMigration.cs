using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;

namespace Our.Umbraco.Bitcoin.LightningPayments.Migrations
{

    //public class PackageMigrationPlan : AutomaticPackageMigrationPlan
    //{B
    //    public PackageMigrationPlan() : base("Custom Welcome Dashboard")
    //    {
    //    }
    //}

    ///// <summary>
    ///// Migration to add paywall message support (POC).
    ///// </summary>
    //public class AddPaywallMessageMigration : PackageMigrationBase
    //{
    //    public AddPaywallMessageMigration(IPackagingService context) : base(context) { }

    //    protected override void Migrate()
    //    {
    //        // POC: Add a custom table or column for paywall messages if persistence is needed
    //        // Example: Create a table for storing default messages
    //        if (!TableExists("PaywallMessages"))
    //        {
    //            Create.Table("PaywallMessages")
    //                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
    //                .WithColumn("Message").AsString(500).NotNullable()
    //                .WithColumn("IsDefault").AsBoolean().NotNullable().WithDefaultValue(false)
    //                .Do();
    //        }
    //    }
    //}
}