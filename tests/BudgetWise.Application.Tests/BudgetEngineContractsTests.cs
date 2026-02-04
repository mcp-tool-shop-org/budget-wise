using System.Text.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using BudgetWise.Application.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Services;
using BudgetWise.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Application.Tests;

public class BudgetEngineContractsTests
{
    [Fact]
    public void Contracts_AreCreatable_AndJsonSerializable()
    {
        // Compile-time & basic runtime sanity checks for Phase 2 contracts.
        var snapshot = new BudgetSnapshotDto
        {
            Year = 2026,
            Month = 2,
            CarriedOver = Money.Zero,
            TotalIncome = new Money(100m),
            TotalAllocated = new Money(60m),
            TotalSpent = new Money(20m),
            ReadyToAssign = new Money(40m)
        };

        var result = BudgetOperationResult.Ok(snapshot);

        var json = JsonSerializer.Serialize(result);
        json.Should().NotBeNullOrWhiteSpace();

        // Ensure the interface type is visible/compilable from the test project.
        typeof(IBudgetEngine).Should().NotBeNull();

        // Ensure budget engine surface uses existing request DTOs.
        typeof(AllocateToEnvelopeRequest).Should().NotBeNull();
        typeof(MoveMoneyRequest).Should().NotBeNull();
        typeof(ReconcileAccountRequest).Should().NotBeNull();
        typeof(ReconcileAccountResultDto).Should().NotBeNull();
        typeof(CsvImportPreviewRequest).Should().NotBeNull();
        typeof(CsvImportCommitRequest).Should().NotBeNull();
        typeof(CsvImportPreviewResultDto).Should().NotBeNull();
        typeof(CsvImportCommitResultDto).Should().NotBeNull();

        // Ensure structured errors compile/serialize.
        var errorResult = BudgetOperationResult.Fail(BudgetOperationError.Create("test_error", "Test error"));
        JsonSerializer.Serialize(errorResult).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ContractFreeze_BudgetSnapshotDto_PublicShape_IsStable()
    {
        GetPublicInstancePropertyMap(typeof(BudgetSnapshotDto)).Should().BeEquivalentTo(
            new Dictionary<string, Type>
            {
                ["Year"] = typeof(int),
                ["Month"] = typeof(int),
                ["IsClosed"] = typeof(bool),
                ["CarriedOver"] = typeof(Money),
                ["TotalIncome"] = typeof(Money),
                ["TotalAllocated"] = typeof(Money),
                ["TotalSpent"] = typeof(Money),
                ["ReadyToAssign"] = typeof(Money)
            });
    }

    [Fact]
    public void ContractFreeze_BudgetOperationResult_PublicShape_IsStable()
    {
        GetPublicInstancePropertyMap(typeof(BudgetOperationResult)).Should().BeEquivalentTo(
            new Dictionary<string, Type>
            {
                ["Success"] = typeof(bool),
                ["Errors"] = typeof(IReadOnlyList<BudgetOperationError>),
                ["Snapshot"] = typeof(BudgetSnapshotDto),
                ["AllocationChanges"] = typeof(IReadOnlyList<AllocationChangeDto>)
            });
    }

    [Fact]
    public void ContractFreeze_BudgetOperationError_PublicShape_IsStable()
    {
        GetPublicInstancePropertyMap(typeof(BudgetOperationError)).Should().BeEquivalentTo(
            new Dictionary<string, Type>
            {
                ["Code"] = typeof(string),
                ["Message"] = typeof(string),
                ["Target"] = typeof(string)
            });

        var create = typeof(BudgetOperationError)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(BudgetOperationError.Create));

        create.ReturnType.Should().Be(typeof(BudgetOperationError));
        create.GetParameters().Select(p => p.ParameterType).Should().Equal(
            typeof(string),
            typeof(string),
            typeof(string));
    }

    [Fact]
    public void ContractFreeze_AllocationChangeDto_PublicShape_IsStable()
    {
        GetPublicInstancePropertyMap(typeof(AllocationChangeDto)).Should().BeEquivalentTo(
            new Dictionary<string, Type>
            {
                ["EnvelopeId"] = typeof(Guid),
                ["EnvelopeName"] = typeof(string),
                ["BeforeAllocated"] = typeof(Money),
                ["AfterAllocated"] = typeof(Money),
                ["Delta"] = typeof(Money)
            });
    }

    [Fact]
    public void ContractFreeze_BudgetEngine_ErrorCodes_AreStable()
    {
        var mapError = typeof(BudgetEngine)
            .GetMethod("MapError", BindingFlags.NonPublic | BindingFlags.Static);
        mapError.Should().NotBeNull("engine error code mapping must remain testable");

        var validation = (BudgetOperationError)mapError!.Invoke(null, new object[] { new ArgumentException("bad", "amount") })!;
        validation.Code.Should().Be("VALIDATION");
        validation.Target.Should().Be("amount");

        var invalidOperation = (BudgetOperationError)mapError.Invoke(null, new object[] { new InvalidOperationException("nope") })!;
        invalidOperation.Code.Should().Be("INVALID_OPERATION");

        var notImplemented = (BudgetOperationError)mapError.Invoke(null, new object[] { new NotImplementedException("todo") })!;
        notImplemented.Code.Should().Be("NOT_IMPLEMENTED");

        var unexpected = (BudgetOperationError)mapError.Invoke(null, new object[] { new Exception("boom") })!;
        unexpected.Code.Should().Be("UNEXPECTED");

        new[] { validation.Code, invalidOperation.Code, notImplemented.Code, unexpected.Code }
            .Distinct(StringComparer.Ordinal)
            .Should().BeEquivalentTo(new[] { "VALIDATION", "INVALID_OPERATION", "NOT_IMPLEMENTED", "UNEXPECTED" });
    }

    [Fact]
    public void ContractFreeze_AllEmittedErrorCodes_AreDocumented()
    {
        var repoRoot = FindRepoRoot();

        var srcDir = Path.Combine(repoRoot, "src");
        Directory.Exists(srcDir).Should().BeTrue($"expected src directory at '{srcDir}'");

        var codeRegex = new Regex(
            "BudgetOperationError\\s*\\.\\s*Create\\s*\\(\\s*\"(?<code>[^\"]+)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        var discoveredCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in codeRegex.Matches(text))
            {
                var code = match.Groups["code"].Value.Trim();
                if (!string.IsNullOrWhiteSpace(code))
                    discoveredCodes.Add(code);
            }
        }

        discoveredCodes.Should().NotBeEmpty("at least one error code should be emitted by the engine");

        var docPath = Path.Combine(repoRoot, "ENGINE_ERROR_CODES.md");
        File.Exists(docPath).Should().BeTrue($"expected error code catalog at '{docPath}'");

        var docText = File.ReadAllText(docPath);
        var docCodeRegex = new Regex(
            "^###\\s+`(?<code>[A-Za-z0-9_]+)`\\s*$",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

        var documentedCodes = docCodeRegex
            .Matches(docText)
            .Select(m => m.Groups["code"].Value)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.Ordinal);

        documentedCodes.Should().NotBeEmpty("ENGINE_ERROR_CODES.md should list codes as '### `CODE`' headings");

        var undocumented = discoveredCodes.Where(c => !documentedCodes.Contains(c)).OrderBy(c => c, StringComparer.Ordinal).ToList();
        undocumented.Should().BeEmpty(
            $"all emitted codes must be documented in ENGINE_ERROR_CODES.md (missing: {string.Join(", ", undocumented)})");
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var sln = Path.Combine(current.FullName, "BudgetWise.sln");
            if (File.Exists(sln))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root (BudgetWise.sln not found)." );
    }

    private static Dictionary<string, Type> GetPublicInstancePropertyMap(Type type)
        => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, p => p.PropertyType);
}
