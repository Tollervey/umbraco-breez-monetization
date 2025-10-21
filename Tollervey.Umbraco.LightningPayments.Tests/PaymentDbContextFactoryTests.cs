using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;

[TestClass]
public class PaymentDbContextFactoryTests
{
    [TestMethod]
    public void CreateDbContext_CreatesContextWithSqlite()
    {
        // Arrange
        var factory = new PaymentDbContextFactory();

        // Act
        var context = factory.CreateDbContext(null);

        // Assert
        Assert.IsNotNull(context);
        Assert.IsInstanceOfType(context, typeof(PaymentDbContext));
    }
}
