using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Tests.TestData
{
    /// <summary>
    /// Generators for creating realistic test data for comprehensive testing
    /// </summary>
    public static class TestDataGenerators
    {
        private static readonly Random _random = new Random(42); // Fixed seed for reproducible tests
        
        #region AI Model Configuration Generators

        /// <summary>
        /// Generates a collection of AI model configurations for testing
        /// </summary>
        public static IEnumerable<AIModelConfiguration> GenerateAIModelConfigurations(int count = 10)
        {
            var providers = Enum.GetValues<AIProvider>().ToArray();
            var models = new Dictionary<AIProvider, string[]>
            {
                [AIProvider.ChatGPT] = new[] { "gpt-3.5-turbo", "gpt-4", "gpt-4-turbo", "gpt-4o" },
                [AIProvider.Claude] = new[] { "claude-3-haiku", "claude-3-sonnet", "claude-3-opus", "claude-3.5-sonnet" },
                [AIProvider.Ollama] = new[] { "llama2", "codellama", "mistral", "mixtral", "llama3" }
            };

            for (int i = 0; i < count; i++)
            {
                var provider = providers[i % providers.Length];
                var availableModels = models[provider];
                var model = availableModels[_random.Next(availableModels.Length)];

                yield return new AIModelConfiguration
                {
                    Provider = provider,
                    ApiKey = provider == AIProvider.Ollama ? null : GenerateApiKey(provider),
                    ModelName = model,
                    EndpointUrl = provider == AIProvider.Ollama ? "http://localhost:11434" : null,
                    TimeoutSeconds = _random.Next(30, 180),
                    MaxRetries = _random.Next(1, 6),
                    IsEnabled = _random.NextDouble() > 0.2, // 80% enabled
                    Temperature = _random.NextDouble() * 2.0,
                    MaxTokens = _random.Next(100, 4000),
                    TopP = _random.NextDouble(),
                    FrequencyPenalty = _random.NextDouble() * 2.0 - 1.0, // -1.0 to 1.0
                    PresencePenalty = _random.NextDouble() * 2.0 - 1.0 // -1.0 to 1.0
                };
            }
        }

        /// <summary>
        /// Generates a realistic API key for testing
        /// </summary>
        public static string GenerateApiKey(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.ChatGPT => "sk-" + GenerateRandomString(48, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"),
                AIProvider.Claude => "sk-ant-api03-" + GenerateRandomString(32, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_"),
                _ => GenerateRandomString(32, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
            };
        }

        #endregion

        #region Conversation Generators

        /// <summary>
        /// Generates realistic conversations for testing
        /// </summary>
        public static IEnumerable<Conversation> GenerateConversations(int count = 20)
        {
            var conversationTopics = new[]
            {
                "Debug JavaScript Error", "Refactor C# Code", "SQL Query Optimization", "React Component Design",
                "API Integration Help", "Unit Testing Strategy", "Code Review Feedback", "Algorithm Implementation",
                "Performance Optimization", "Database Schema Design", "Authentication Setup", "Error Handling",
                "Code Documentation", "Deployment Issues", "Library Recommendations", "Best Practices Discussion"
            };

            var codeLanguages = new[] { "csharp", "javascript", "typescript", "python", "sql", "java", "cpp", "html", "css" };
            var models = new[] { "gpt-4", "gpt-3.5-turbo", "claude-3-opus", "claude-3-sonnet", "llama2" };

            for (int i = 0; i < count; i++)
            {
                var topic = conversationTopics[_random.Next(conversationTopics.Length)];
                var messageCount = _random.Next(3, 20);
                var createdAt = DateTime.UtcNow.AddDays(-_random.Next(0, 30)).AddHours(-_random.Next(0, 24));
                var model = models[_random.Next(models.Length)];

                var conversation = new Conversation
                {
                    Id = $"conv-{i:D4}-{Guid.NewGuid().ToString("N")[..8]}",
                    Title = topic,
                    CreatedAt = createdAt,
                    LastModified = createdAt.AddMinutes(_random.Next(1, 120)),
                    Messages = GenerateConversationMessages(messageCount, topic, codeLanguages, model, createdAt).ToList()
                };

                yield return conversation;
            }
        }

        /// <summary>
        /// Generates messages for a conversation
        /// </summary>
        private static IEnumerable<ChatMessage> GenerateConversationMessages(int count, string topic, string[] codeLanguages, string model, DateTime startTime)
        {
            var userQuestions = new[]
            {
                "Can you help me with this issue?", "I'm getting an error when I try to run this code.",
                "How can I improve this implementation?", "What's the best way to handle this scenario?",
                "Could you review this code for me?", "I need help debugging this problem.",
                "What are some alternatives to this approach?", "How can I optimize this for better performance?"
            };

            var assistantResponses = new[]
            {
                "I'd be happy to help you with that. Let me take a look at your code.",
                "I see the issue. The problem is in the way you're handling the data.",
                "Here's a better approach that should solve your problem:",
                "You can improve this by implementing the following changes:",
                "I've identified several areas where you can optimize this code:",
                "Let me walk you through the solution step by step."
            };

            for (int i = 0; i < count; i++)
            {
                var isUser = i % 2 == 0;
                var timestamp = startTime.AddMinutes(i * _random.Next(1, 15));
                
                var message = new ChatMessage
                {
                    Content = isUser ? 
                        GenerateUserMessage(userQuestions, topic, codeLanguages) :
                        GenerateAssistantMessage(assistantResponses, topic, codeLanguages),
                    Type = isUser ? MessageType.User : MessageType.Assistant,
                    Timestamp = timestamp,
                    ModelUsed = isUser ? null : model
                };

                // Add file references to some user messages
                if (isUser && _random.NextDouble() < 0.3) // 30% chance
                {
                    message.FileReferences = GenerateFileReferences(_random.Next(1, 4)).ToList();
                }

                yield return message;
            }
        }

        private static string GenerateUserMessage(string[] templates, string topic, string[] codeLanguages)
        {
            var template = templates[_random.Next(templates.Length)];
            var message = template + " ";

            // Add context about the topic
            if (topic.Contains("Debug"))
                message += "I'm getting this error when I run my application.";
            else if (topic.Contains("Refactor"))
                message += "This code works but feels messy. How can I clean it up?";
            else if (topic.Contains("Optimization"))
                message += "My code is running slowly. Can you help me make it faster?";

            // Sometimes add code snippets
            if (_random.NextDouble() < 0.4) // 40% chance
            {
                var language = codeLanguages[_random.Next(codeLanguages.Length)];
                message += $"\n\n```{language}\n{GenerateCodeSnippet(language)}\n```";
            }

            return message;
        }

        private static string GenerateAssistantMessage(string[] templates, string topic, string[] codeLanguages)
        {
            var template = templates[_random.Next(templates.Length)];
            var message = template + " ";

            // Add detailed explanation
            message += "The issue you're experiencing is common in this type of scenario. ";

            // Add solution
            if (_random.NextDouble() < 0.6) // 60% chance of code solution
            {
                var language = codeLanguages[_random.Next(codeLanguages.Length)];
                message += $"Here's the corrected code:\n\n```{language}\n{GenerateCodeSnippet(language)}\n```\n\n";
                message += "This approach is better because it handles edge cases and follows best practices.";
            }
            else
            {
                message += "Here are the steps you should follow:\n\n";
                message += "1. First, check your input validation\n";
                message += "2. Ensure proper error handling is in place\n";
                message += "3. Consider performance implications\n";
                message += "4. Test with various scenarios";
            }

            return message;
        }

        #endregion

        #region File Reference Generators

        /// <summary>
        /// Generates file references for testing
        /// </summary>
        public static IEnumerable<FileReference> GenerateFileReferences(int count = 5)
        {
            var fileExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".html", ".css", ".sql" };
            var fileNames = new[] 
            { 
                "Program", "Service", "Controller", "Model", "Helper", "Utility", "Manager", "Provider",
                "Component", "Handler", "Processor", "Factory", "Builder", "Validator", "Repository"
            };

            for (int i = 0; i < count; i++)
            {
                var extension = fileExtensions[_random.Next(fileExtensions.Length)];
                var name = fileNames[_random.Next(fileNames.Length)];
                var filePath = $"/src/{name}{extension}";
                
                var startLine = _random.Next(1, 100);
                var endLine = startLine + _random.Next(1, 50);

                yield return new FileReference
                {
                    FilePath = filePath,
                    Content = GenerateFileContent(extension, endLine - startLine + 1),
                    StartLine = startLine,
                    EndLine = endLine,
                    Type = _random.NextDouble() < 0.7 ? ReferenceType.Selection : ReferenceType.FullFile
                };
            }
        }

        /// <summary>
        /// Generates realistic file content
        /// </summary>
        public static string GenerateFileContent(string extension, int lineCount = 20)
        {
            return extension switch
            {
                ".cs" => GenerateCSharpCode(lineCount),
                ".js" => GenerateJavaScriptCode(lineCount),
                ".ts" => GenerateTypeScriptCode(lineCount),
                ".py" => GeneratePythonCode(lineCount),
                ".java" => GenerateJavaCode(lineCount),
                ".html" => GenerateHtmlCode(lineCount),
                ".css" => GenerateCssCode(lineCount),
                ".sql" => GenerateSqlCode(lineCount),
                _ => GenerateGenericCode(lineCount)
            };
        }

        #endregion

        #region Diff Change Generators

        /// <summary>
        /// Generates diff changes for testing
        /// </summary>
        public static IEnumerable<DiffChange> GenerateDiffChanges(int count = 10)
        {
            var changeTypes = Enum.GetValues<ChangeType>().ToArray();
            var codeLines = new[]
            {
                "public class Example {", "    private string _field;", "    public void Method() {",
                "        Console.WriteLine(\"Hello\");", "        return result;", "    }", "}",
                "function example() {", "    const value = 42;", "    return value * 2;", "}",
                "if (condition) {", "    processData();", "} else {", "    handleError();", "}"
            };

            for (int i = 0; i < count; i++)
            {
                var changeType = changeTypes[_random.Next(changeTypes.Length)];
                var lineNumber = i + 1;
                var content = codeLines[_random.Next(codeLines.Length)];

                var diffChange = new DiffChange
                {
                    LineNumber = lineNumber,
                    ChangeType = changeType,
                    Content = changeType switch
                    {
                        ChangeType.Added => $"+ {content}",
                        ChangeType.Removed => $"- {content}",
                        ChangeType.Modified => $"~ {content}",
                        _ => $"  {content}"
                    },
                    OldContent = changeType == ChangeType.Added ? "" : content,
                    NewContent = changeType == ChangeType.Removed ? "" : content
                };

                yield return diffChange;
            }
        }

        #endregion

        #region Configuration Generators

        /// <summary>
        /// Generates configuration profiles for testing
        /// </summary>
        public static IEnumerable<ConfigurationProfile> GenerateConfigurationProfiles(int count = 10)
        {
            var profileNames = new[]
            {
                "Development", "Production", "Testing", "Staging", "Personal", "Team",
                "Project-Alpha", "Project-Beta", "Client-Work", "Experimental"
            };

            for (int i = 0; i < count; i++)
            {
                var name = i < profileNames.Length ? profileNames[i] : $"Profile-{i}";
                var createdAt = DateTime.UtcNow.AddDays(-_random.Next(0, 100));

                yield return new ConfigurationProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Description = $"Configuration profile for {name.ToLower()} environment",
                    Version = $"1.{_random.Next(0, 10)}.{_random.Next(0, 10)}",
                    CreatedAt = createdAt,
                    LastModified = createdAt.AddDays(_random.Next(0, 10)),
                    CreatedBy = GenerateUserName(),
                    IsActive = _random.NextDouble() > 0.7, // 30% active
                    IsDefault = i == 0, // First one is default
                    Settings = GenerateConfigurationSettings(_random.Next(5, 20)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    AIModelConfiguration = GenerateAIModelConfigurations(1).First(),
                    AdvancedParameters = GenerateAdvancedParameters()
                };
            }
        }

        /// <summary>
        /// Generates configuration settings
        /// </summary>
        public static IEnumerable<KeyValuePair<string, object>> GenerateConfigurationSettings(int count = 10)
        {
            var settingNames = new[]
            {
                "ChatWindowWidth", "ChatWindowHeight", "AutoSaveInterval", "MaxHistoryEntries",
                "EnableSyntaxHighlighting", "ThemeName", "FontSize", "ShowLineNumbers",
                "EnableAutoComplete", "EnableSpellCheck", "SaveLocation", "BackupEnabled"
            };

            for (int i = 0; i < count && i < settingNames.Length; i++)
            {
                var settingName = settingNames[i];
                object value = settingName switch
                {
                    var s when s.Contains("Width") || s.Contains("Height") => _random.Next(200, 1000),
                    var s when s.Contains("Interval") => _random.Next(30, 300),
                    var s when s.Contains("Entries") => _random.Next(50, 500),
                    var s when s.Contains("Enable") => _random.NextDouble() > 0.5,
                    "ThemeName" => new[] { "Dark", "Light", "Auto", "HighContrast" }[_random.Next(4)],
                    "FontSize" => _random.Next(8, 24),
                    "SaveLocation" => $"C:\\Users\\{GenerateUserName()}\\Documents\\Aider",
                    _ => GenerateRandomString(10)
                };

                yield return new KeyValuePair<string, object>(settingName, value);
            }
        }

        /// <summary>
        /// Generates advanced AI parameters
        /// </summary>
        public static AdvancedParameters GenerateAdvancedParameters()
        {
            return new AdvancedParameters
            {
                Temperature = Math.Round(_random.NextDouble() * 2.0, 2),
                MaxTokens = _random.Next(100, 4000),
                TopP = Math.Round(_random.NextDouble(), 2),
                FrequencyPenalty = Math.Round(_random.NextDouble() * 2.0 - 1.0, 2),
                PresencePenalty = Math.Round(_random.NextDouble() * 2.0 - 1.0, 2),
                StopSequences = _random.NextDouble() > 0.5 ? new[] { "\n", "###" }.ToList() : new List<string>(),
                SystemPrompt = GenerateSystemPrompt(),
                CustomParameters = GenerateCustomParameters()
            };
        }

        #endregion

        #region Test File Generators

        /// <summary>
        /// Generates test files on disk for file system testing
        /// </summary>
        public static async Task<List<string>> GenerateTestFilesAsync(string directory, int count = 10)
        {
            Directory.CreateDirectory(directory);
            var createdFiles = new List<string>();

            var fileExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".html", ".css", ".sql", ".txt", ".md" };
            
            for (int i = 0; i < count; i++)
            {
                var extension = fileExtensions[_random.Next(fileExtensions.Length)];
                var fileName = $"TestFile{i:D3}{extension}";
                var filePath = Path.Combine(directory, fileName);
                
                var content = GenerateFileContent(extension, _random.Next(10, 100));
                await File.WriteAllTextAsync(filePath, content);
                
                createdFiles.Add(filePath);
            }

            return createdFiles;
        }

        /// <summary>
        /// Generates a test project structure
        /// </summary>
        public static async Task<string> GenerateTestProjectAsync(string baseDirectory, string projectName = "TestProject")
        {
            var projectDir = Path.Combine(baseDirectory, projectName);
            Directory.CreateDirectory(projectDir);

            // Create typical project structure
            var srcDir = Path.Combine(projectDir, "src");
            var testDir = Path.Combine(projectDir, "tests");
            var docDir = Path.Combine(projectDir, "docs");
            
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(testDir);
            Directory.CreateDirectory(docDir);

            // Generate source files
            await GenerateTestFilesAsync(srcDir, 15);
            
            // Generate test files
            await GenerateTestFilesAsync(testDir, 8);
            
            // Generate documentation
            await GenerateTestFilesAsync(docDir, 3);
            
            // Create project file
            var projectFile = Path.Combine(projectDir, $"{projectName}.csproj");
            await File.WriteAllTextAsync(projectFile, GenerateProjectFileContent(projectName));

            // Create README
            var readmeFile = Path.Combine(projectDir, "README.md");
            await File.WriteAllTextAsync(readmeFile, GenerateReadmeContent(projectName));

            return projectDir;
        }

        #endregion

        #region Private Helper Methods

        private static string GenerateRandomString(int length, string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        private static string GenerateUserName()
        {
            var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Eve", "Frank" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
            
            var firstName = firstNames[_random.Next(firstNames.Length)];
            var lastName = lastNames[_random.Next(lastNames.Length)];
            
            return $"{firstName}.{lastName}";
        }

        private static string GenerateCodeSnippet(string language)
        {
            return language switch
            {
                "csharp" => "public class Example\n{\n    public void Method()\n    {\n        Console.WriteLine(\"Hello World\");\n    }\n}",
                "javascript" => "function example() {\n    const message = 'Hello World';\n    console.log(message);\n    return message;\n}",
                "python" => "def example():\n    message = 'Hello World'\n    print(message)\n    return message",
                "sql" => "SELECT TOP 10 *\nFROM Users\nWHERE IsActive = 1\nORDER BY CreatedDate DESC",
                _ => "// Example code snippet\nfunction example() {\n    return 'Hello World';\n}"
            };
        }

        private static string GenerateCSharpCode(int lineCount)
        {
            var lines = new List<string>
            {
                "using System;",
                "using System.Collections.Generic;",
                "using System.Linq;",
                "",
                "namespace TestNamespace",
                "{",
                "    public class TestClass",
                "    {",
                "        private readonly IService _service;",
                "",
                "        public TestClass(IService service)",
                "        {",
                "            _service = service ?? throw new ArgumentNullException(nameof(service));",
                "        }",
                "",
                "        public async Task<string> ProcessDataAsync(string input)",
                "        {",
                "            if (string.IsNullOrEmpty(input))",
                "                return string.Empty;",
                "",
                "            var result = await _service.ProcessAsync(input);",
                "            return result.ToUpperInvariant();",
                "        }",
                "    }",
                "}"
            };

            return string.Join(Environment.NewLine, lines.Take(lineCount));
        }

        private static string GenerateJavaScriptCode(int lineCount)
        {
            var lines = new List<string>
            {
                "/**",
                " * Test JavaScript class",
                " */",
                "class TestClass {",
                "    constructor(options = {}) {",
                "        this.options = options;",
                "        this.data = [];",
                "    }",
                "",
                "    async processData(input) {",
                "        if (!input) return null;",
                "",
                "        try {",
                "            const result = await this.fetchData(input);",
                "            return this.transformData(result);",
                "        } catch (error) {",
                "            console.error('Processing failed:', error);",
                "            throw error;",
                "        }",
                "    }",
                "",
                "    transformData(data) {",
                "        return data.map(item => ({",
                "            ...item,",
                "            processed: true",
                "        }));",
                "    }",
                "}"
            };

            return string.Join(Environment.NewLine, lines.Take(lineCount));
        }

        private static string GenerateTypeScriptCode(int lineCount)
        {
            var lines = new List<string>
            {
                "interface IDataProcessor {",
                "    processData(input: string): Promise<ProcessedData>;",
                "}",
                "",
                "interface ProcessedData {",
                "    id: string;",
                "    value: any;",
                "    timestamp: Date;",
                "}",
                "",
                "export class DataProcessor implements IDataProcessor {",
                "    private readonly apiClient: ApiClient;",
                "",
                "    constructor(apiClient: ApiClient) {",
                "        this.apiClient = apiClient;",
                "    }",
                "",
                "    async processData(input: string): Promise<ProcessedData> {",
                "        const response = await this.apiClient.post('/process', { input });",
                "        return {",
                "            id: response.id,",
                "            value: response.data,",
                "            timestamp: new Date()",
                "        };",
                "    }",
                "}"
            };

            return string.Join(Environment.NewLine, lines.Take(lineCount));
        }

        private static string GeneratePythonCode(int lineCount)
        {
            var lines = new List<string>
            {
                "from typing import List, Optional",
                "import asyncio",
                "import logging",
                "",
                "class DataProcessor:",
                "    \"\"\"Processes data using various algorithms.\"\"\"",
                "",
                "    def __init__(self, config: dict):",
                "        self.config = config",
                "        self.logger = logging.getLogger(__name__)",
                "",
                "    async def process_data(self, data: List[str]) -> Optional[List[str]]:",
                "        \"\"\"Process a list of data items.\"\"\"",
                "        if not data:",
                "            return None",
                "",
                "        processed = []",
                "        for item in data:",
                "            try:",
                "                result = await self._process_item(item)",
                "                processed.append(result)",
                "            except Exception as e:",
                "                self.logger.error(f\"Failed to process {item}: {e}\")",
                "",
                "        return processed"
            };

            return string.Join(Environment.NewLine, lines.Take(lineCount));
        }

        private static string GenerateJavaCode(int lineCount)
        {
            var lines = new List<string>
            {
                "package com.example.test;",
                "",
                "import java.util.List;",
                "import java.util.concurrent.CompletableFuture;",
                "",
                "public class DataProcessor {",
                "    private final ServiceClient serviceClient;",
                "",
                "    public DataProcessor(ServiceClient serviceClient) {",
                "        this.serviceClient = serviceClient;",
                "    }",
                "",
                "    public CompletableFuture<String> processDataAsync(String input) {",
                "        return CompletableFuture.supplyAsync(() -> {",
                "            if (input == null || input.isEmpty()) {",
                "                throw new IllegalArgumentException(\"Input cannot be null or empty\");",
                "            }",
                "",
                "            return serviceClient.process(input.toUpperCase());",
                "        });",
                "    }",
                "}"
            };

            return string.Join(Environment.NewLine, lines.Take(lineCount));
        }

        private static string GenerateHtmlCode(int lineCount)
        {
            var lines = new List<string>
            {
                "<!DOCTYPE html>",
                "<html lang=\"en\">",
                "<head>",
                "    <meta charset=\"UTF-8\">",
                "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">",
                "    <title>Test Page</title>",
                "    <link rel=\"stylesheet\" href=\"styles.css\">",
                "</head>",
                "<body>",
                "    <header>",
                "        <h1>Welcome to Test Page</h1>",
                "        <nav>",
                "            <ul>",
                "                <li><a href=\"#home\">Home</a></li>",
                "                <li><a href=\"#about\">About</a></li>",
                "            </ul>",
                "        </nav>",
                "    </header>",
                "    <main>",
                "        <section id=\"content\">",
                "            <p>This is test content.</p>",
                "        </section>",
                "    </main>",
                "</body>",
                "</html>"
            };

            return string.Join(Environment.NewLine, lines.Take(lineCount));
        }

        private static string GenerateCssCode(int lineCount)
        {
            var lines = new List<string>
            {
                "/* Test CSS Styles */",
                "",
                "body {",
                "    font-family: Arial, sans-serif;",
                "    margin: 0;",
                "    padding: 0;",
                "    background-color: #f5f5f5;",
                "}",
                "",
                "header {",
                "    background-color: #333;",
                "    color: white;",
                "    padding: 1rem;",
                "}",
                "",
                "nav ul {",
                "    list-style: none;",
                "    padding: 0;",
                "    display: flex;",
                "}",
                "",
                "nav li {",
                "    margin-right: 1rem;",
                "}",
                "",
                "main {",
                "    padding: 2rem;",
                "}"
            };

            return string.Join(Environment.NewLine, lines.Take(lineCount));
        }

        private static string GenerateSqlCode(int lineCount)
        {
            var lines = new List<string>
            {
                "-- Test SQL Query",
                "",
                "CREATE TABLE Users (",
                "    Id INT IDENTITY(1,1) PRIMARY KEY,",
                "    Username NVARCHAR(50) NOT NULL,",
                "    Email NVARCHAR(100) NOT NULL,",
                "    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),",
                "    IsActive BIT DEFAULT 1",
                ");",
                "",
                "INSERT INTO Users (Username, Email) VALUES",
                "    ('john.doe', 'john@example.com'),",
                "    ('jane.smith', 'jane@example.com');",
                "",
                "SELECT TOP 10",
                "    u.Username,",
                "    u.Email,",
                "    u.CreatedDate",
                "FROM Users u",
                "WHERE u.IsActive = 1",
                "ORDER BY u.CreatedDate DESC;"
            };

            return string.Join(Environment.NewLine, lines.Take(lineCount));
        }

        private static string GenerateGenericCode(int lineCount)
        {
            var lines = Enumerable.Range(1, lineCount)
                .Select(i => $"// Line {i}: {GenerateRandomString(20)}")
                .ToList();

            return string.Join(Environment.NewLine, lines);
        }

        private static string GenerateSystemPrompt()
        {
            var prompts = new[]
            {
                "You are a helpful AI assistant that specializes in software development.",
                "You are an expert programmer who provides clear, concise code solutions.",
                "You are a senior developer who helps with code reviews and best practices.",
                "You are a technical mentor who guides developers through complex problems."
            };

            return prompts[_random.Next(prompts.Length)];
        }

        private static Dictionary<string, object> GenerateCustomParameters()
        {
            var parameters = new Dictionary<string, object>();
            
            if (_random.NextDouble() > 0.5)
                parameters["stream"] = true;
            
            if (_random.NextDouble() > 0.7)
                parameters["logprobs"] = _random.Next(1, 6);
            
            if (_random.NextDouble() > 0.8)
                parameters["seed"] = _random.Next(1, 10000);

            return parameters;
        }

        private static string GenerateProjectFileContent(string projectName)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AssemblyName>{projectName}</AssemblyName>
    <RootNamespace>{projectName}</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""7.0.0"" />
  </ItemGroup>

</Project>";
        }

        private static string GenerateReadmeContent(string projectName)
        {
            return $@"# {projectName}

This is a test project generated for testing purposes.

## Features

- Feature 1: Data processing
- Feature 2: Configuration management
- Feature 3: Error handling

## Getting Started

1. Clone the repository
2. Build the project: `dotnet build`
3. Run tests: `dotnet test`

## License

MIT License
";
        }

        #endregion
    }
}