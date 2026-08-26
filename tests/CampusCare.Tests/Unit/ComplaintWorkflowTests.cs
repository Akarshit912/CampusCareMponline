using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CampusCare.Tests.Unit
{
    public class ComplaintWorkflowTests
    {
        [Fact]
        public void UniqueComplaintNumberFormat_ShouldMatchPattern()
        {
            // Arrange
            int year = DateTime.UtcNow.Year;
            int sequence = 1;

            // Act
            string complaintNumber = $"CMP-{year}-{sequence:D5}";

            // Assert
            Assert.StartsWith($"CMP-{year}-", complaintNumber);
            Assert.Equal(14, complaintNumber.Length);
            Assert.Equal($"CMP-{year}-00001", complaintNumber);
        }

        [Theory]
        [InlineData(ComplaintStatus.Submitted, ComplaintStatus.Assigned, true)]
        [InlineData(ComplaintStatus.Submitted, ComplaintStatus.InProgress, true)]
        [InlineData(ComplaintStatus.Submitted, ComplaintStatus.Rejected, true)]
        [InlineData(ComplaintStatus.Submitted, ComplaintStatus.Closed, false)]
        [InlineData(ComplaintStatus.InProgress, ComplaintStatus.Resolved, true)]
        [InlineData(ComplaintStatus.InProgress, ComplaintStatus.Escalated, true)]
        [InlineData(ComplaintStatus.Resolved, ComplaintStatus.Closed, true)]
        [InlineData(ComplaintStatus.Closed, ComplaintStatus.InProgress, false)]
        public void WorkflowStateTransition_ValidationRules(ComplaintStatus current, ComplaintStatus target, bool expectedResult)
        {
            // Act
            bool isValid = IsValidStateTransition(current, target);

            // Assert
            Assert.Equal(expectedResult, isValid);
        }

        [Fact]
        public async Task AIService_ShouldFallbackToRuleEngine_WhenAPIKeyIsEmpty()
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            configMock.Setup(c => c["AISettings:ApiKey"]).Returns(string.Empty);
            configMock.Setup(c => c["AISettings:Endpoint"]).Returns(string.Empty);

            var httpClient = new System.Net.Http.HttpClient();
            var aiService = new AIService(configMock.Object, httpClient);

            // Act
            var result = await aiService.AnalyzeComplaintAsync(
                "Wi-Fi down in Lab 3",
                "Students cannot access online exam system",
                "Computer Lab 3"
            );

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal("IT / Wi-Fi", result.Category);
            Assert.Equal("Information Technology", result.Department);
            Assert.Equal(PriorityLevel.High, result.Priority);
            Assert.Equal("CampusCare-RuleEngine-v1", result.ModelUsed);
        }

        [Theory]
        [InlineData("Water pipe leakage in Hostel Block B", "Bathroom pipe burst", "Facility Maintenance", "Maintenance")]
        [InlineData("Hostel room bed is broken", "Mess food quality issue", "Hostel Administration", "Hostel")]
        [InlineData("Stolen laptop in library", "Security guard emergency", "Campus Security", "Security")]
        [InlineData("College bus route delayed", "Shuttle driver missing", "Transport & Fleet", "Transportation")]
        public async Task AIService_ShouldRouteToCorrectNonITDepartment(string title, string description, string expectedDept, string expectedCat)
        {
            // Arrange
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            configMock.Setup(c => c["AISettings:ApiKey"]).Returns(string.Empty);
            configMock.Setup(c => c["AISettings:Endpoint"]).Returns(string.Empty);

            var httpClient = new System.Net.Http.HttpClient();
            var aiService = new AIService(configMock.Object, httpClient);

            // Act
            var result = await aiService.AnalyzeComplaintAsync(title, description, "Main Campus");

            // Assert
            Assert.Equal(expectedDept, result.Department);
            Assert.Equal(expectedCat, result.Category);
        }

        [Fact]
        public async Task EscalationService_ShouldMarkOverdueComplaintsAsEscalated()
        {
            // Arrange
            var complaint = new Complaint
            {
                Id = 10,
                ComplaintNumber = "CMP-2026-00010",
                Title = "Broken water pipe",
                Status = ComplaintStatus.Submitted,
                CreatedAt = DateTime.UtcNow.AddHours(-72),
                IsEscalated = false
            };

            var repoMock = new Mock<IComplaintRepository>();
            repoMock.Setup(r => r.GetOverdueComplaintsAsync(48))
                .ReturnsAsync(new[] { complaint });

            var notifMock = new Mock<INotificationService>();

            var escalationService = new EscalationService(repoMock.Object, notifMock.Object);

            // Act
            int count = await escalationService.ProcessOverdueComplaintsAsync(48);

            // Assert
            Assert.Equal(1, count);
            Assert.True(complaint.IsEscalated);
            Assert.Equal(ComplaintStatus.Escalated, complaint.Status);
            repoMock.Verify(r => r.UpdateAsync(It.Is<Complaint>(c => c.Id == 10)), Times.Once);
        }

        private bool IsValidStateTransition(ComplaintStatus current, ComplaintStatus target)
        {
            if (current == target) return true;
            return current switch
            {
                ComplaintStatus.Submitted => target == ComplaintStatus.Assigned || target == ComplaintStatus.InProgress || target == ComplaintStatus.Rejected,
                ComplaintStatus.Assigned => target == ComplaintStatus.InProgress || target == ComplaintStatus.Rejected || target == ComplaintStatus.Escalated,
                ComplaintStatus.InProgress => target == ComplaintStatus.Resolved || target == ComplaintStatus.Escalated || target == ComplaintStatus.Rejected,
                ComplaintStatus.Escalated => target == ComplaintStatus.InProgress || target == ComplaintStatus.Resolved,
                ComplaintStatus.Resolved => target == ComplaintStatus.Closed || target == ComplaintStatus.InProgress,
                ComplaintStatus.Closed => false,
                ComplaintStatus.Rejected => false,
                _ => false
            };
        }
    }
}
