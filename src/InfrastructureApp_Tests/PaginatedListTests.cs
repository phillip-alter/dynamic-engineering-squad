using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp_Tests;

[TestFixture]
public class PaginatedListTests
{
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


