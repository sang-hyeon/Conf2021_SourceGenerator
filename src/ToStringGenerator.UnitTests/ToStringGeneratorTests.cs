namespace ToStringGenerator.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Xunit;
    using FluentAssertions;

    public class ToStringGeneratorTests
    {
        [Theory]
        [InlineData("Testset.InputSourceCode.cs", "Testset.ExpectedSourceCode.cs")]
        public void Generator_does_generate_ToString_method(
            string inputSourceCodeResourceName, string expectedSourceCodeResourceName)
        {
            // Arrange
            string attributeSourceCode = GetResource(ToStringGeneratorAttributeDescription.EmebeddedResourceName, typeof(ToStringGenerator).Assembly);
            string inputSourceCode = GetResource(inputSourceCodeResourceName);
            string expectedSourceCode = GetResource(expectedSourceCodeResourceName);

            string[] expectedSourceCodes = new[] { attributeSourceCode, expectedSourceCode };

            var compilation = CreateCompilation(inputSourceCode);

            // Act
            var sut = new ToStringGenerator();
            compilation = RunGenerators(compilation, out var diagnostics, sut);

            // Assert
            diagnostics.Should().BeEmpty();

            var generatedSourceCode = GetGeneratedCode(sut, compilation);
            AssertSourceCodesEquals(generatedSourceCode, expectedSourceCodes);
        }

        #region Helpers

        protected static void AssertSourceCodesEquals(string[] actual, string[] expected)
        {
            var normalizedActual = actual.Select(NormalizeSourceCode);
            var normalizedExpected = expected.Select(NormalizeSourceCode);

            normalizedActual.Should().Equal(normalizedExpected, (x, y) => x.Equals(y, StringComparison.InvariantCultureIgnoreCase));
        }

        protected static string NormalizeSourceCode(string sourceCode)
        {
            return Regex.Replace(sourceCode.Trim(), @"\s+", string.Empty);
        }

        protected static string GetResource(string endWith, Assembly assembly = null)
        {
            assembly = assembly ?? typeof(ToStringGeneratorTests).Assembly;
            IEnumerable<string> resources = assembly.GetManifestResourceNames().Where(r => r.EndsWith(endWith));

            if (!resources.Any())
                throw new InvalidOperationException($"There is no resources that ends with '{endWith}'");
            if (resources.Count() > 1)
                throw new InvalidOperationException($"There is more then one resource that ends with '{endWith}'");

            string resourceName = resources.Single();

            using var stream = assembly.GetManifestResourceStream(resourceName);
            byte[] buf = new byte[stream.Length];
            stream.Read(buf, 0, buf.Length);

            return Encoding.Default.GetString(buf);
        }

        protected static Compilation CreateCompilation(string inputSourceCode)
            => CSharpCompilation.Create("compilation",
                                                        new[] { CSharpSyntaxTree.ParseText(inputSourceCode, new CSharpParseOptions(LanguageVersion.LatestMajor)) },
                                                        new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                                                        new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        protected static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => CSharpGeneratorDriver.Create(generators, parseOptions: (CSharpParseOptions)c.SyntaxTrees.First().Options);

        protected static Compilation RunGenerators(
            Compilation c, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(c, generators).RunGeneratorsAndUpdateCompilation(c, out Compilation d, out diagnostics);
            return d;
        }

        protected static string[] GetGeneratedCode(ISourceGenerator generators, Compilation outputCompilation)
        {
            return outputCompilation.SyntaxTrees
                .Where(file => file.FilePath.IndexOf(generators.GetType().Name) > -1)
                .Select(file => file.ToString())
                .ToArray();
        }
        #endregion
    }
}
