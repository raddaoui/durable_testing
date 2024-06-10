using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Company.Function
{
    public class DurableFunctionsOrchestrationCSharp1Tests
    {
        [Fact]
        public async Task RunOrchestrator_Approved_ShouldIncludeLondonGreeting()
        {
            // Arrange
            var contextMock = Substitute.For<TaskOrchestrationContext>();
            var logger = new TestLogger();

            contextMock.CreateReplaySafeLogger(Arg.Any<string>()).Returns(logger);
            contextMock.CallActivityAsync<string>(nameof(DurableFunctionsOrchestrationCSharp1.SayHello), "Tokyo")
                       .Returns("Hello Tokyo!");
            contextMock.CallActivityAsync<string>(nameof(DurableFunctionsOrchestrationCSharp1.SayHello), "Seattle")
                       .Returns("Hello Seattle!");
            contextMock.WaitForExternalEvent<bool>("Approval").Returns(true);
            contextMock.CallActivityAsync<string>(nameof(DurableFunctionsOrchestrationCSharp1.SayHello), "London")
                       .Returns("Hello London!");

            // Act
            var result = await DurableFunctionsOrchestrationCSharp1.RunOrchestrator(contextMock);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Hello Tokyo!", result[0]);
            Assert.Equal("Hello Seattle!", result[1]);
            Assert.Equal("Hello London!", result[2]);
        }

        [Fact]
        public async Task RunOrchestrator_Denied_ShouldLogDenialMessage()
        {
            // Arrange
            var contextMock = Substitute.For<TaskOrchestrationContext>();
            var logger = new TestLogger();

            contextMock.CreateReplaySafeLogger(Arg.Any<string>()).Returns(logger);
            contextMock.CallActivityAsync<string>(nameof(DurableFunctionsOrchestrationCSharp1.SayHello), "Tokyo")
                       .Returns("Hello Tokyo!");
            contextMock.CallActivityAsync<string>(nameof(DurableFunctionsOrchestrationCSharp1.SayHello), "Seattle")
                       .Returns("Hello Seattle!");
            contextMock.WaitForExternalEvent<bool>("Approval").Returns(false);

            // Act
            var result = await DurableFunctionsOrchestrationCSharp1.RunOrchestrator(contextMock);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Hello Tokyo!", result[0]);
            Assert.Equal("Hello Seattle!", result[1]);
            Assert.Contains("The request was denied.", logger.CapturedLogs);
        }

        public class TestLogger : ILogger
        {
            public IList<string> CapturedLogs { get; set; } = new List<string>();

            public IDisposable BeginScope<TState>(TState state) => Substitute.For<IDisposable>();

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                string formattedLog = formatter(state, exception);
                this.CapturedLogs.Add(formattedLog);
            }
        }
    }
}
