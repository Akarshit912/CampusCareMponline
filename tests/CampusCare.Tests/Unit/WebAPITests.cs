using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using CampusCare.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CampusCare.Tests.Unit
{
    public class WebAPITests
    {
        private readonly Mock<IComplaintRepository> _repoMock;
        private readonly Mock<IEscalationService> _escalationMock;
        private readonly ApplicationDbContext _dbContext;

        public WebAPITests()
        {
            _repoMock = new Mock<IComplaintRepository>();
            _escalationMock = new Mock<IEscalationService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetComplaints_ShouldReturnOkResult_WithListOfComplaints()
        {
            // Arrange
            var testComplaints = new List<Complaint>
            {
                new Complaint
                {
                    Id = 1,
                    ComplaintNumber = "CMP-2026-00001",
                    Title = "Wi-Fi Issue in Lab 1",
                    Description = "Internet connection dropped",
                    Location = "Lab 1",
                    Status = ComplaintStatus.Submitted,
                    Priority = PriorityLevel.High,
                    CreatedAt = DateTime.UtcNow
                },
                new Complaint
                {
                    Id = 2,
                    ComplaintNumber = "CMP-2026-00002",
                    Title = "Water Pipe Leak in Hostel A",
                    Description = "Pipe leaking near room 102",
                    Location = "Hostel A",
                    Status = ComplaintStatus.InProgress,
                    Priority = PriorityLevel.Medium,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(testComplaints);

            var controller = new ComplaintsApiController(_repoMock.Object, _escalationMock.Object, _dbContext);

            // Act
            var result = await controller.GetComplaints(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var items = System.Linq.Enumerable.ToList((IEnumerable<object>)okResult.Value);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task GetComplaintDetails_ShouldReturnOkResult_WhenComplaintExists()
        {
            // Arrange
            var complaint = new Complaint
            {
                Id = 1,
                ComplaintNumber = "CMP-2026-00001",
                Title = "Library Air Conditioner Defective",
                Description = "A/C unit making loud noise",
                Location = "Central Library 2nd Floor",
                Status = ComplaintStatus.Assigned,
                Priority = PriorityLevel.Medium,
                CreatedAt = DateTime.UtcNow,
                History = new List<ComplaintHistory>()
            };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(complaint);

            var controller = new ComplaintsApiController(_repoMock.Object, _escalationMock.Object, _dbContext);

            // Act
            var result = await controller.GetComplaintDetails(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetComplaintDetails_ShouldReturnNotFound_WhenComplaintDoesNotExist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Complaint?)null);

            var controller = new ComplaintsApiController(_repoMock.Object, _escalationMock.Object, _dbContext);

            // Act
            var result = await controller.GetComplaintDetails(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task TriggerEscalationCheck_ShouldReturnOkResult_WithEscalatedCount()
        {
            // Arrange
            _escalationMock.Setup(e => e.ProcessOverdueComplaintsAsync(48)).ReturnsAsync(3);

            var controller = new ComplaintsApiController(_repoMock.Object, _escalationMock.Object, _dbContext);

            // Act
            var result = await controller.TriggerEscalationCheck(48);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _escalationMock.Verify(e => e.ProcessOverdueComplaintsAsync(48), Times.Once);
        }

        [Fact]
        public async Task DeleteComplaint_ShouldReturnOkResult_WhenComplaintExists()
        {
            // Arrange
            var complaint = new Complaint
            {
                Id = 5,
                ComplaintNumber = "CMP-2026-00005",
                Title = "Broken Desk",
                Description = "Leg broken on desk",
                Location = "Room 304"
            };

            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(complaint);
            _repoMock.Setup(r => r.DeleteAsync(5)).Returns(Task.CompletedTask);

            var controller = new ComplaintsApiController(_repoMock.Object, _escalationMock.Object, _dbContext);

            // Act
            var result = await controller.DeleteComplaint(5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _repoMock.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task DeleteComplaint_ShouldReturnNotFound_WhenComplaintDoesNotExist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Complaint?)null);

            var controller = new ComplaintsApiController(_repoMock.Object, _escalationMock.Object, _dbContext);

            // Act
            var result = await controller.DeleteComplaint(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ReceiveN8nWebhookCallback_ShouldReturnOkResult()
        {
            // Arrange
            var controller = new ComplaintsApiController(_repoMock.Object, _escalationMock.Object, _dbContext);
            var payload = new { Event = "ComplaintEscalated", ComplaintId = 10 };

            // Act
            var result = controller.ReceiveN8nWebhookCallback(payload);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
