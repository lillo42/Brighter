using Paramore.Brighter.Analyzers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    Paramore.Brighter.Analyzers.SuggestImplementMapperAsyncAndSyncAnalyzer, 
    Paramore.Brighter.Analyzers.SuggestImplementMapperSyncOrAsyncCodeFix,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using Xunit;

namespace Paramore.Brighter.Analyzer.Tests;

public class MapperSuggestion
{
    private const string MapperInterfaces = 
        """
        namespace Paramore.Brighter
        {
            public interface IAmAMessageMapper<TRequest >
            {
            }

            public interface IAmAMessageMapperAsync<T>
            {
            }
        }
        """;

    [Fact]
    public async Task ClassImplementsSyncOnly_ShouldReportDiagnostic()
    {
        const string code =
            """
            using Paramore.Brighter;
            using System.Threading.Tasks;

            namespace TestApp
            {
                class {|#0:MySyncMapper|} : IAmAMessageMapper<MyCommand>
                {
                }

                public class MyCommand { }
            }
            """ + MapperInterfaces; // Append the interface definitions
        
        var expected = VerifyCS.Diagnostic(SuggestImplementMapperAsyncAndSyncAnalyzer.SyncDiagnosticId)
            .WithLocation(6, 11)
            .WithArguments("MySyncMapper", "TestApp.MyCommand");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }
    
    [Fact]
    public async Task ClassImplementsAsyncOnly_ShouldReportDiagnostic()
    {
        const string testCode =
            """
            using Paramore.Brighter;
            using System.Threading.Tasks;

            namespace TestApp
            {
                class {|#0:MyAsyncMapper|} : IAmAMessageMapperAsync<MyCommand>
                {
                }

                public class MyCommand { }
            }
            """ + MapperInterfaces; // Append the interface definitions
        
        var expected = VerifyCS.Diagnostic(SuggestImplementMapperAsyncAndSyncAnalyzer.AsyncDiagnosticId)
            .WithLocation(6, 11)
            .WithArguments("MyAsyncMapper", "TestApp.MyCommand");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }
    
    [Fact]
    public async Task ClassImplementsBoth_ShouldNotReportDiagnostic()
    {
        const string testCode = 
            """
            using Paramore.Brighter;
            using System.Threading.Tasks;

            namespace TestApp
            {
                class MyFullMapper : IAmAMessageMapper<MyCommand>, IAmAMessageMapperAsync<MyCommand>
                {
                }

                public class MyCommand { }
            }
            """ + MapperInterfaces;

        await VerifyCS.VerifyAnalyzerAsync(testCode); // No expected diagnostics
    }
    
    [Fact]
    public async Task ClassImplementsNeither_ShouldNotReportDiagnostic()
    {
        const string testCode = 
            """
            using Paramore.Brighter;
            using System.Threading.Tasks;

            namespace TestApp
            {
                class MyOtherClass
                {
                    // Does not implement any mapper interfaces
                }

                public class MyCommand { }
            }
            """ + MapperInterfaces;

        await VerifyCS.VerifyAnalyzerAsync(testCode); // No expected diagnostics
    }

    [Fact]
    public async Task CodeFix_SyncToAsync_ShouldAddAsyncInterfaceAndMembers()
    {
        const string testCode = 
            """
            using Paramore.Brighter;
            using System.Threading.Tasks;

            namespace TestApp
            {
                class {|#0:MySyncMapper|} : IAmAMessageMapper<MyCommand>
                {
                }

                public class MyCommand { }
            }
            """ + MapperInterfaces;

        // Define the code after the fix is applied
        const string fixedCode = 
            """
            using Paramore.Brighter;
            using System.Threading.Tasks;

            namespace TestApp
            {
                class MySyncMapper : IAmAMessageMapper<MyCommand>, Paramore.Brighter.IAmAMessageMapperAsync<TestApp.MyCommand>    {
                }

                public class MyCommand { }
            }
            """ + MapperInterfaces;

        var expected = VerifyCS.Diagnostic(SuggestImplementMapperAsyncAndSyncAnalyzer.SyncDiagnosticId)
            .WithLocation(6, 11)
            .WithArguments("MySyncMapper", "TestApp.MyCommand");

        await VerifyCS.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
