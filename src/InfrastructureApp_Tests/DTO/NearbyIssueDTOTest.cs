//this unit test tests that the DTO is being mapped correctly

using InfrastructureApp.Dtos;
using NUnit.Framework;
using System;

namespace InfrastructureApp.Tests.Dtos
{
    [TestFixture]
    public class NearbyIssueDTOTests
    {
        [Test]
        public void NearbyIssueDTO_StoresValuesCorrectly()
        {
            // Arrange
            var created = DateTime.UtcNow;

            var dto = new NearbyIssueDTO
            {
                Id = 10,
                Status = "Approved",
                CreatedAt = created,
                Latitude = 44.85,
                Longitude = -123.19,
                DistanceMiles = 2.5
            };

            // Assert
            Assert.That(dto.Id, Is.EqualTo(10));
            Assert.That(dto.Status, Is.EqualTo("Approved"));
            Assert.That(dto.CreatedAt, Is.EqualTo(created));
            Assert.That(dto.Latitude, Is.EqualTo(44.85));
            Assert.That(dto.Longitude, Is.EqualTo(-123.19));
            Assert.That(dto.DistanceMiles, Is.EqualTo(2.5));
        }
    }
}