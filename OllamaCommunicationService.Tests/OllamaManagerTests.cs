namespace OllamaCommunicationService.Tests
{
    [TestClass]
    public sealed class OllamaManagerTests
    {
        [TestMethod]
        public void GetOllamaResponse()
        {
            // Arrange
            var ollamaManager = new OllamaManager();
            var prompt = "Test prompt";
            // Act
            var response = ollamaManager.GetOllamaResponse(prompt);
            // Assert
            Assert.IsNotNull(response);
            Assert.Contains(prompt, response);
        }
    }
}
