# AiderVSExtension Test Suite

## Overview

This test suite provides comprehensive unit tests for the validation methods in the AiderVSExtension models. The tests focus on validating the core model classes and their validation logic.

## Test Files Created

### 1. ChatMessageTests.cs
Tests for the `ChatMessage` model validation methods:
- ✅ Valid and invalid message properties
- ✅ Content length validation (50,000 character limit)
- ✅ Edge cases for content with special characters, Unicode, and multiline content
- ✅ File reference validation within messages
- ✅ JSON serialization/deserialization
- ✅ Validation error message accuracy
- ✅ Default value initialization
- ✅ All message types (User, Assistant, System)

**Test Coverage:** 20+ test methods covering all major validation scenarios

### 2. FileReferenceTests.cs
Tests for the `FileReference` model validation methods:
- ✅ File path validation (null, empty, invalid formats)
- ✅ Line number constraints (must be >= 1)
- ✅ Start/end line validation (end >= start)
- ✅ All reference types (File, Selection, Error, Clipboard, GitBranch, WebSearch, Documentation)
- ✅ Edge cases for numeric ranges (including int.MaxValue)
- ✅ Helper methods (GetDisplayName, IsSingleLine, GetLineCount)
- ✅ JSON serialization/deserialization
- ✅ Path format validation using Path.GetFullPath()

**Test Coverage:** 28+ test methods covering all validation scenarios

### 3. AIModelConfigurationTests.cs
Tests for the `AIModelConfiguration` model validation methods:
- ✅ Provider-specific validation rules (ChatGPT, Claude, Ollama)
- ✅ API key requirements for ChatGPT and Claude
- ✅ Endpoint URL validation for Ollama (HTTP/HTTPS only)
- ✅ Timeout constraints (1-300 seconds)
- ✅ Max retries constraints (0-10)
- ✅ Helper methods (GetDisplayName, RequiresApiKey, SupportsCustomEndpoint)
- ✅ Clone method for deep copying
- ✅ JSON serialization/deserialization
- ✅ Default value initialization

**Test Coverage:** 25+ test methods covering all provider-specific validation

### 4. DiffChangeTests.cs
Tests for the `DiffChange` model validation methods:
- ✅ Change type validation (Added, Removed, Modified)
- ✅ Content requirements based on change type
- ✅ File path validation
- ✅ Line number constraints
- ✅ Content comparison for modified changes
- ✅ Helper methods (GetDisplayName, GetChangeSummary, AffectsSameLine)
- ✅ Clone method for deep copying
- ✅ JSON serialization/deserialization
- ✅ Case-insensitive file path comparison

**Test Coverage:** 27+ test methods covering all change type scenarios

## Key Testing Focus Areas

### Valid and Invalid Property Combinations
- All tests validate both positive and negative scenarios
- Edge cases for boundary conditions
- Null, empty, and whitespace handling

### Edge Cases for Numeric Ranges
- Line numbers: 1 to int.MaxValue
- Timeout seconds: 1 to 300
- Max retries: 0 to 10
- Content length: up to 50,000 characters

### JSON Serialization/Deserialization
- Round-trip serialization testing
- Enum serialization as strings
- Missing optional fields handling
- Default value preservation

### Validation Error Message Accuracy
- Specific error messages for each validation failure
- Multiple error accumulation
- Provider-specific error messages
- Content-specific error messages

## Test Framework and Dependencies

- **xUnit**: Primary testing framework
- **FluentAssertions**: Assertion library for readable tests
- **System.Text.Json**: JSON serialization testing
- **System.ComponentModel.Annotations**: Data annotation support

## Build Status

✅ **Compilation**: All tests compile successfully with C# 11 language features
⚠️ **Execution**: Tests require .NET 6.0 runtime for execution

## Running the Tests

```bash
# Navigate to the test project directory
cd AiderVSExtension.Tests

# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run tests for specific class
dotnet test --filter "ClassName=ChatMessageTests"
```

## Test Structure

Each test follows the AAA pattern:
- **Arrange**: Set up test data and conditions
- **Act**: Execute the method being tested
- **Assert**: Verify the expected outcome

## Coverage Summary

- **Total Test Methods**: 100+ comprehensive test methods
- **Model Classes Tested**: 4 core model classes
- **Validation Scenarios**: All major validation paths covered
- **Edge Cases**: Boundary conditions and error scenarios
- **JSON Serialization**: Round-trip testing for all models
- **Error Messages**: Validation error accuracy testing

## Notes

- Tests use local copies of model files to avoid Visual Studio dependency issues
- All tests focus on pure validation logic without external dependencies
- Comprehensive coverage of both happy path and error scenarios
- Tests validate the exact error messages produced by validation methods
