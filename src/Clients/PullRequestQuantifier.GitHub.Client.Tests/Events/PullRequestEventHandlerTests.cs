    using System.Linq;
    using System.Net;
    using PullRequestQuantifier.Common;
        private readonly Mock<IGitHubClientAdapter> gitHubClientAdapter;
        private readonly Mock<IGitHubClientAdapterFactory> gitHubClientAdapterFactory;
        private readonly Mock<IAppTelemetry> appTelemetry;

        public PullRequestEventHandlerTests()
            appTelemetry = new Mock<IAppTelemetry>();
            gitHubClientAdapter = new Mock<IGitHubClientAdapter>();
            gitHubClientAdapterFactory = new Mock<IGitHubClientAdapterFactory>();
        }

        [Fact]
        public async Task HandleEventTest()
        {
            // setup
            var installationEventHandler = new PullRequestEventHandler(
                gitHubClientAdapterFactory.Object,
                appTelemetry.Object,
                new Mock<ILogger<PullRequestEventHandler>>().Object);

            // act
            await installationEventHandler.HandleEvent(await File.ReadAllTextAsync(@"Data/PulRequestPayload.txt"));

            // assert
            appTelemetry.Verify(
                f =>
                    f.RecordMetric(It.IsAny<string>(), It.IsAny<long>()),
                Times.Once);
            Assert.Single(gitHubClientAdapter.Invocations.Where(i => i.Method.Name.Equals(nameof(gitHubClientAdapter.Object.GetRawFileAsync))));
        }

        [Fact]
        public async Task HandleEvent_WithGitHubFolderContext()
        {
            // setup
            gitHubClientAdapter.Setup(
                    g => g.GetRawFileAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.Is<string>(p => p.Equals($"/{Constants.RootContextFilePath}"))))
                .Throws(new NotFoundException("File not found", HttpStatusCode.NotFound));

            var installationEventHandler = new PullRequestEventHandler(
                gitHubClientAdapterFactory.Object,
                appTelemetry.Object,
                new Mock<ILogger<PullRequestEventHandler>>().Object);

            // act
            await installationEventHandler.HandleEvent(await File.ReadAllTextAsync(@"Data/PulRequestPayload.txt"));

            // assert
            appTelemetry.Verify(
                f =>
                    f.RecordMetric(It.IsAny<string>(), It.IsAny<long>()),
                Times.Once);
            Assert.Equal(2, gitHubClientAdapter.Invocations.Count(i => i.Method.Name.Equals(nameof(gitHubClientAdapter.Object.GetRawFileAsync))));
        }

        [Fact]
        public async Task HandleEvent_WithNoContext_DoesNotFail()
        {
            // setup
            gitHubClientAdapter.Setup(
                    g => g.GetRawFileAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                .Throws(new NotFoundException("File not found", HttpStatusCode.NotFound));
            Assert.Equal(2, gitHubClientAdapter.Invocations.Count(i => i.Method.Name.Equals(nameof(gitHubClientAdapter.Object.GetRawFileAsync))));
        }

        [Fact]
        public async Task HandleEvent_FromNoDiffToDiff_SetNewLabelCorrectly()
        {
            var previousLabelName = "No Changes";
            var previousLabel = new Label(1L, string.Empty, previousLabelName, "1", string.Empty, string.Empty, true);
            var newLabelName = "Extra Small";
            var diff = @"diff --git a/a.txt b/a.txt
new file mode 100644
index 0000000..9daeafb
--- /dev/null
+++ b/a.txt
@@ -0,0 +1 @@
+test";
            var addedFile = new PullRequestFile(string.Empty, "addedFile", string.Empty, 1, 0, 0, string.Empty, string.Empty, string.Empty, diff, "addedFile");

            // setup
            var installationEventHandler = new PullRequestEventHandler(
                gitHubClientAdapterFactory.Object,
                appTelemetry.Object,
                new Mock<ILogger<PullRequestEventHandler>>().Object);

            gitHubClientAdapter.Setup(f => f.GetIssueLabelsAsync(It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Label> { previousLabel });

            gitHubClientAdapter.Setup(f => f.GetPullRequestFilesAsync(It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(new List<PullRequestFile> { addedFile });

            // act
            await installationEventHandler.HandleEvent(await File.ReadAllTextAsync(@"Data/PulRequestPayload.txt"));

            // assert
            gitHubClientAdapter.Verify(
                f =>
                    f.RemoveLabelFromIssueAsync(It.IsAny<long>(), It.IsAny<int>(), previousLabelName),
                Times.Once);

            gitHubClientAdapter.Verify(
                f =>
                    f.ApplyLabelAsync(It.IsAny<long>(), It.IsAny<int>(), new[] { newLabelName }), Times.Once);
        }

        [Fact]
        public async Task HandleEvent_LabelSizeChanges_SetNewLabelCorrectly()
        {
            var previousLabelName = "Small";
            var previousLabel = new Label(1L, string.Empty, previousLabelName, "1", string.Empty, string.Empty, true);
            var newLabelName = "Extra Small";
            var diff = @"diff --git a/a.txt b/a.txt
new file mode 100644
index 0000000..9daeafb
--- /dev/null
+++ b/a.txt
@@ -0,0 +1 @@
+test";

            var addedFile = new PullRequestFile(string.Empty, "addedFile", string.Empty, 1, 0, 0, string.Empty, string.Empty, string.Empty, diff, "addedFile");

            // setup
            var installationEventHandler = new PullRequestEventHandler(
                gitHubClientAdapterFactory.Object,
                appTelemetry.Object,
                new Mock<ILogger<PullRequestEventHandler>>().Object);

            gitHubClientAdapter.Setup(f => f.GetIssueLabelsAsync(It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Label> { previousLabel });

            gitHubClientAdapter.Setup(f => f.GetPullRequestFilesAsync(It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(new List<PullRequestFile> { addedFile });

            // act
            await installationEventHandler.HandleEvent(await File.ReadAllTextAsync(@"Data/PulRequestPayload.txt"));

            // assert
            gitHubClientAdapter.Verify(
                f =>
                    f.RemoveLabelFromIssueAsync(It.IsAny<long>(), It.IsAny<int>(), previousLabelName),
                Times.Once);

            gitHubClientAdapter.Verify(
                f =>
                    f.ApplyLabelAsync(It.IsAny<long>(), It.IsAny<int>(), new[] { newLabelName }), Times.Once);