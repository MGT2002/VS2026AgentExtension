namespace OllamaCommunicationService.Tests
{
    [TestClass]
    public sealed class OllamaManagerTests
    {
        [TestMethod]
        public async Task ExplainCodeAsync()
        {
            // Arrange
            var ollamaManager = new OllamaManager();
            var prompt = "int a = 12";
            // Act
            var response = await ollamaManager.ExplainCodeAsync(prompt);
            // Assert
            Assert.IsNotNull(response);
            
            Console.WriteLine("Ollama response: " + response);
        }
    }
}
