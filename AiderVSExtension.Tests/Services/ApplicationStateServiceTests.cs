using System;
using System.IO;
using System.Threading.Tasks;
using AiderVSExtension.Services;
using FluentAssertions;
using Xunit;

namespace AiderVSExtension.Tests.Services
{
    public class ApplicationStateServiceTests : IDisposable
    {
        private readonly ApplicationStateService _service;
        private readonly string _tempDirectory;
        private readonly string _stateFilePath;

        public ApplicationStateServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "ApplicationStateServiceTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _stateFilePath = Path.Combine(_tempDirectory, "state.json");
            
            _service = new ApplicationStateService(_stateFilePath);
        }

        public void Dispose()
        {
            _service?.Dispose();
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidPath_CreatesInstance()
        {
            // Act
            var service = new ApplicationStateService(_stateFilePath);

            // Assert
            service.Should().NotBeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidPath_ThrowsArgumentException(string invalidPath)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ApplicationStateService(invalidPath));
        }

        #endregion

        #region InitializeAsync Tests

        [Fact]
        public async Task InitializeAsync_WithNoExistingState_CreatesDefaultState()
        {
            // Act
            await _service.InitializeAsync();

            // Assert
            var isInitialized = await _service.GetStateAsync<bool>("IsInitialized");
            isInitialized.Should().BeTrue();
        }

        [Fact]
        public async Task InitializeAsync_WithExistingState_LoadsState()
        {
            // Arrange
            await _service.SetStateAsync("TestKey", "TestValue");
            await _service.SaveStateAsync();
            
            var newService = new ApplicationStateService(_stateFilePath);

            // Act
            await newService.InitializeAsync();

            // Assert
            var testValue = await newService.GetStateAsync<string>("TestKey");
            testValue.Should().Be("TestValue");
            
            newService.Dispose();
        }

        [Fact]
        public async Task InitializeAsync_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            await _service.InitializeAsync();
            await _service.InitializeAsync(); // Should not throw
        }

        #endregion

        #region GetStateAsync Tests

        [Fact]
        public async Task GetStateAsync_WithExistingKey_ReturnsValue()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("TestKey", 42);

            // Act
            var result = await _service.GetStateAsync<int>("TestKey");

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public async Task GetStateAsync_WithNonExistentKey_ReturnsDefault()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act
            var result = await _service.GetStateAsync<string>("NonExistentKey");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetStateAsync_WithDefaultValue_ReturnsDefaultForNonExistentKey()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act
            var result = await _service.GetStateAsync("NonExistentKey", "DefaultValue");

            // Assert
            result.Should().Be("DefaultValue");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetStateAsync_WithInvalidKey_ThrowsArgumentException(string invalidKey)
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.GetStateAsync<string>(invalidKey));
        }

        #endregion

        #region SetStateAsync Tests

        [Fact]
        public async Task SetStateAsync_WithValidKeyValue_SetsValue()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act
            await _service.SetStateAsync("TestKey", "TestValue");

            // Assert
            var result = await _service.GetStateAsync<string>("TestKey");
            result.Should().Be("TestValue");
        }

        [Fact]
        public async Task SetStateAsync_WithExistingKey_OverwritesValue()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("TestKey", "OriginalValue");

            // Act
            await _service.SetStateAsync("TestKey", "NewValue");

            // Assert
            var result = await _service.GetStateAsync<string>("TestKey");
            result.Should().Be("NewValue");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task SetStateAsync_WithInvalidKey_ThrowsArgumentException(string invalidKey)
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.SetStateAsync(invalidKey, "value"));
        }

        [Fact]
        public async Task SetStateAsync_WithNullValue_SetsNull()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act
            await _service.SetStateAsync<string>("TestKey", null);

            // Assert
            var result = await _service.GetStateAsync<string>("TestKey");
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetStateAsync_WithComplexObject_SerializesCorrectly()
        {
            // Arrange
            await _service.InitializeAsync();
            var complexObject = new { Name = "Test", Value = 42, Items = new[] { 1, 2, 3 } };

            // Act
            await _service.SetStateAsync("ComplexKey", complexObject);

            // Assert
            var result = await _service.GetStateAsync<object>("ComplexKey");
            result.Should().NotBeNull();
        }

        #endregion

        #region RemoveStateAsync Tests

        [Fact]
        public async Task RemoveStateAsync_WithExistingKey_RemovesKey()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("TestKey", "TestValue");

            // Act
            var result = await _service.RemoveStateAsync("TestKey");

            // Assert
            result.Should().BeTrue();
            var value = await _service.GetStateAsync<string>("TestKey");
            value.Should().BeNull();
        }

        [Fact]
        public async Task RemoveStateAsync_WithNonExistentKey_ReturnsFalse()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act
            var result = await _service.RemoveStateAsync("NonExistentKey");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task RemoveStateAsync_WithInvalidKey_ThrowsArgumentException(string invalidKey)
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.RemoveStateAsync(invalidKey));
        }

        #endregion

        #region ClearStateAsync Tests

        [Fact]
        public async Task ClearStateAsync_RemovesAllState()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("Key1", "Value1");
            await _service.SetStateAsync("Key2", "Value2");

            // Act
            await _service.ClearStateAsync();

            // Assert
            var value1 = await _service.GetStateAsync<string>("Key1");
            var value2 = await _service.GetStateAsync<string>("Key2");
            
            value1.Should().BeNull();
            value2.Should().BeNull();
        }

        #endregion

        #region SaveStateAsync Tests

        [Fact]
        public async Task SaveStateAsync_PersistsStateToDisk()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("TestKey", "TestValue");

            // Act
            await _service.SaveStateAsync();

            // Assert
            File.Exists(_stateFilePath).Should().BeTrue();
            
            var newService = new ApplicationStateService(_stateFilePath);
            await newService.InitializeAsync();
            var value = await newService.GetStateAsync<string>("TestKey");
            value.Should().Be("TestValue");
            
            newService.Dispose();
        }

        [Fact]
        public async Task SaveStateAsync_WithNoChanges_DoesNotThrow()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            await _service.SaveStateAsync(); // Should not throw
        }

        #endregion

        #region LoadStateAsync Tests

        [Fact]
        public async Task LoadStateAsync_WithExistingFile_LoadsState()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("TestKey", "TestValue");
            await _service.SaveStateAsync();
            
            // Clear current state
            await _service.ClearStateAsync();

            // Act
            await _service.LoadStateAsync();

            // Assert
            var value = await _service.GetStateAsync<string>("TestKey");
            value.Should().Be("TestValue");
        }

        [Fact]
        public async Task LoadStateAsync_WithNonExistentFile_DoesNotThrow()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.json");
            var service = new ApplicationStateService(nonExistentPath);

            // Act & Assert
            await service.LoadStateAsync(); // Should not throw
            
            service.Dispose();
        }

        [Fact]
        public async Task LoadStateAsync_WithCorruptedFile_HandlesGracefully()
        {
            // Arrange
            await File.WriteAllTextAsync(_stateFilePath, "invalid json content");

            // Act & Assert
            await _service.LoadStateAsync(); // Should not throw
        }

        #endregion

        #region GetAllKeysAsync Tests

        [Fact]
        public async Task GetAllKeysAsync_WithMultipleKeys_ReturnsAllKeys()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("Key1", "Value1");
            await _service.SetStateAsync("Key2", "Value2");
            await _service.SetStateAsync("Key3", "Value3");

            // Act
            var keys = await _service.GetAllKeysAsync();

            // Assert
            keys.Should().Contain("Key1");
            keys.Should().Contain("Key2");
            keys.Should().Contain("Key3");
        }

        [Fact]
        public async Task GetAllKeysAsync_WithNoKeys_ReturnsEmptyCollection()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.ClearStateAsync();

            // Act
            var keys = await _service.GetAllKeysAsync();

            // Assert
            keys.Should().BeEmpty();
        }

        #endregion

        #region ContainsKeyAsync Tests

        [Fact]
        public async Task ContainsKeyAsync_WithExistingKey_ReturnsTrue()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("TestKey", "TestValue");

            // Act
            var result = await _service.ContainsKeyAsync("TestKey");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ContainsKeyAsync_WithNonExistentKey_ReturnsFalse()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act
            var result = await _service.ContainsKeyAsync("NonExistentKey");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ContainsKeyAsync_WithInvalidKey_ThrowsArgumentException(string invalidKey)
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.ContainsKeyAsync(invalidKey));
        }

        #endregion

        #region Event Tests

        [Fact]
        public async Task StateChanged_FiresWhenStateIsModified()
        {
            // Arrange
            await _service.InitializeAsync();
            
            string changedKey = null;
            object changedValue = null;
            
            _service.StateChanged += (sender, args) =>
            {
                changedKey = args.Key;
                changedValue = args.NewValue;
            };

            // Act
            await _service.SetStateAsync("TestKey", "TestValue");

            // Assert
            changedKey.Should().Be("TestKey");
            changedValue.Should().Be("TestValue");
        }

        [Fact]
        public async Task StateChanged_FiresWhenStateIsRemoved()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("TestKey", "TestValue");
            
            string changedKey = null;
            object changedValue = null;
            
            _service.StateChanged += (sender, args) =>
            {
                changedKey = args.Key;
                changedValue = args.NewValue;
            };

            // Act
            await _service.RemoveStateAsync("TestKey");

            // Assert
            changedKey.Should().Be("TestKey");
            changedValue.Should().BeNull();
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            _service.Dispose();
            _service.Dispose(); // Should not throw
        }

        [Fact]
        public async Task Dispose_SavesStateBeforeDisposing()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("TestKey", "TestValue");

            // Act
            _service.Dispose();

            // Assert
            File.Exists(_stateFilePath).Should().BeTrue();
            
            var newService = new ApplicationStateService(_stateFilePath);
            await newService.InitializeAsync();
            var value = await newService.GetStateAsync<string>("TestKey");
            value.Should().Be("TestValue");
            
            newService.Dispose();
        }

        #endregion

        #region Type Safety Tests

        [Fact]
        public async Task GetStateAsync_WithWrongType_ReturnsDefault()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.SetStateAsync("IntKey", 42);

            // Act
            var result = await _service.GetStateAsync<string>("IntKey");

            // Assert
            result.Should().BeNull(); // Wrong type should return default
        }

        [Fact]
        public async Task SetStateAsync_WithDifferentTypes_HandlesCorrectly()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act
            await _service.SetStateAsync("Key", 42);
            await _service.SetStateAsync("Key", "String Value"); // Overwrite with different type

            // Assert
            var result = await _service.GetStateAsync<string>("Key");
            result.Should().Be("String Value");
        }

        #endregion
    }
}