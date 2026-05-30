using System;
using Xunit;
using TripleDetection.App.Services.Detection;

namespace TripleDetection.App.Tests
{
    public class ParseResultTests
    {
        [Fact]
        public void ParseResult_NormalOkResult_ReturnsCorrectFields()
        {
            var result = VmIntegrationService.ParseResult("1,BATCH001,20250530,20260530");

            Assert.True(result.IsOK);
            Assert.Equal("BATCH001", result.BatchNumber);
            Assert.Equal("20250530", result.ProductionDate);
            Assert.Equal("20260530", result.ExpirationDate);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ParseResult_NgResult_IsOKFalse()
        {
            var result = VmIntegrationService.ParseResult("0,BATCH002,20250601,20260601");

            Assert.False(result.IsOK);
            Assert.Equal("BATCH002", result.BatchNumber);
            Assert.Equal("20250601", result.ProductionDate);
            Assert.Equal("20260601", result.ExpirationDate);
        }

        [Fact]
        public void ParseResult_WithNewline_TrimsAndParsesCorrectly()
        {
            var result = VmIntegrationService.ParseResult("1,BATCH001,20250530,20260530\r\n");

            Assert.True(result.IsOK);
            Assert.Equal("BATCH001", result.BatchNumber);
            Assert.Equal("20250530", result.ProductionDate);
            Assert.Equal("20260530", result.ExpirationDate);
        }

        [Fact]
        public void ParseResult_WithSpacesAroundFields_TrimsEachField()
        {
            var result = VmIntegrationService.ParseResult(" 1 , BATCH001 , 20250530 , 20260530 ");

            Assert.True(result.IsOK);
            Assert.Equal("BATCH001", result.BatchNumber);
            Assert.Equal("20250530", result.ProductionDate);
            Assert.Equal("20260530", result.ExpirationDate);
        }

        [Fact]
        public void ParseResult_NullInput_ReturnsError()
        {
            var result = VmIntegrationService.ParseResult(null);

            Assert.False(result.IsOK);
            Assert.Equal("Empty result string", result.ErrorMessage);
        }

        [Fact]
        public void ParseResult_EmptyString_ReturnsError()
        {
            var result = VmIntegrationService.ParseResult("");

            Assert.False(result.IsOK);
            Assert.Equal("Empty result string", result.ErrorMessage);
        }

        [Fact]
        public void ParseResult_WhitespaceOnly_ReturnsError()
        {
            var result = VmIntegrationService.ParseResult("   ");

            Assert.False(result.IsOK);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void ParseResult_OnlyOneField_ReturnsError()
        {
            var result = VmIntegrationService.ParseResult("1");

            Assert.False(result.IsOK);
            Assert.Contains("expected 4 fields, got 1", result.ErrorMessage);
        }

        [Fact]
        public void ParseResult_TwoFields_ReturnsError()
        {
            var result = VmIntegrationService.ParseResult("1,BATCH001");

            Assert.False(result.IsOK);
            Assert.Contains("expected 4 fields, got 2", result.ErrorMessage);
        }

        [Fact]
        public void ParseResult_ThreeFields_ReturnsError()
        {
            var result = VmIntegrationService.ParseResult("1,BATCH001,20250530");

            Assert.False(result.IsOK);
            Assert.Contains("expected 4 fields, got 3", result.ErrorMessage);
        }

        [Fact]
        public void ParseResult_EmptyBatchNumber_ReturnsEmptyString()
        {
            var result = VmIntegrationService.ParseResult("1,,20250530,20260530");

            Assert.True(result.IsOK);
            Assert.Equal("", result.BatchNumber);
            Assert.Equal("20250530", result.ProductionDate);
            Assert.Equal("20260530", result.ExpirationDate);
        }
    }
}
