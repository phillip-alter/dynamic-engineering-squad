using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace InfrastructureApp_Tests;

[TestFixture]
public class PaginatedListTests
{
    
    private SqliteConnection _conn = null!;
    private TestDbContext _context = null!;

    [SetUp]
    public async Task SetUp()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        await _conn.OpenAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_conn)
            .Options;
        
        _context = new TestDbContext(options);
        
        await _context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
        await _conn.DisposeAsync();
    }
    
    [TestCase(10, 1, 3, 4, false, true)]
    [TestCase(10, 2, 3, 4, true, true)]
    [TestCase(10, 4, 3, 4, true, false)]
    [TestCase(0, 1, 5, 0, false, false)]
    [TestCase(5, 1, 5, 1, false, false)]
    public void Constructor_CalculatesPropertiesCorrectly(
        int totalCount, int pageIndex, int pageSize,
        int expectedTotalPages, bool expectedHasPrev,
        bool expectedHasNext)
    {
        var items = new List<int>();
        
        var paginatedList = new PaginatedList<int>(items, totalCount, pageIndex, pageSize);
        
        Assert.Multiple(() =>
        {
            Assert.That(paginatedList.PageIndex, Is.EqualTo(pageIndex));
            Assert.That(paginatedList.TotalPages, Is.EqualTo(expectedTotalPages));
            Assert.That(paginatedList.HasPreviousPage, Is.EqualTo(expectedHasPrev));
            Assert.That(paginatedList.HasNextPage, Is.EqualTo(expectedHasNext));
        });
    }

}

// following are used for in-memory database.

public class TestEntity
{
    public int Id { get; set; }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<TestEntity> TestEntities { get; set; }
}