// using NUnit.Framework;
// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using InfrastructureApp.Services;
//
// namespace InfrastructureApp.Tests;
//
// [TestFixture]
// public class LeaderboardServiceTests
// {
//     private LeaderboardService _service = null!;
//
//     [SetUp]
//     public void setUp()
//     {
//         var repo = new LeaderboardRepositoryInMemory();
//         _service = new LeaderboardService(repo);
//     }
//
//     [Test]
//     public async Task AddPoints_AddsNewEntry_WhenNameNotFound()
//     {
//         await _service.AddPointsAsync("Erin", 10);
//
//         var top = await _service.GetTopAsync(25);
//
//         Assert.That(top.Count, Is.EqualTo(1));
//         Assert.That(top[0].DisplayName, Is.EqualTo("Erin"));
//         Assert.That(top[0].ContributionPoints, Is.EqualTo(10));
//     }
//
//     [Test]
//     public async Task AddPoints_IncrementsPoints_WhenNameExists()
//     {
//         await _service.AddPointsAsync("Erin", 10);
//         await _service.AddPointsAsync("Erin", 7);
//
//         var top = await _service.GetTopAsync(25);
//
//         Assert.That(top.Count, Is.EqualTo(1));
//         Assert.That(top[0].ContributionPoints, Is.EqualTo(17));
//     }
//
//     [Test]
//     public void AddPoints_Throws_WhenDisplayNameIsBlank()
//     {
//         var ex = Assert.ThrowsAsync<ArgumentException>(
//             async () => await _service.AddPointsAsync("  ", 5));
//
//         Assert.That(ex!.Message, Does.Contain("DisplayName"));
//
//     }
//
//     [Test]
//     public void AddPoints_Throws_WhenPointsAreZeroOrNegative()
//     {
//         var ex1 = Assert.ThrowsAsync<ArgumentException>(() =>
//             _service.AddPointsAsync("Erin", 0));
//
//         Assert.That(ex1, Is.Not.Null);
//
//         var ex2 = Assert.ThrowsAsync<ArgumentException>(() =>
//             _service.AddPointsAsync("Erin", -3));
//
//         Assert.That(ex2, Is.Not.Null);
//     }
//
//
//
//
//     [Test]
//     public async Task GetTop_SortByPointsDesc_ThenNameAsc()
//     {
//         await _service.AddPointsAsync("Briana", 50);
//         await _service.AddPointsAsync("Alex", 50);
//         await _service.AddPointsAsync("Chris", 80);
//
//         var top = await _service.GetTopAsync(25);
//
//         Assert.That(top[0].DisplayName, Is.EqualTo("Chris")); //Highest points
//         Assert.That(top[1].DisplayName, Is.EqualTo("Alex")); //Tie, name ascending
//         Assert.That(top[2].DisplayName, Is.EqualTo("Briana"));
//     }
//
//     [Test]
//     public async Task GetTop_RespectsTopNLimit()
//     {
//         for (int i = 1; i <= 50; i++)
//         {
//             await _service.AddPointsAsync($"User{i}", i);
//         }
//
//         var top10 = await _service.GetTopAsync(10);
//
//         Assert.That(top10.Count, Is.EqualTo(10));
//         Assert.That(top10[0].ContributionPoints, Is.EqualTo(50));
//     }
// }