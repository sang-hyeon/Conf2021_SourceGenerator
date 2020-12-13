namespace ToStringGenerator
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Attribute = ToStringGeneratorAttributeDescription;

    [Generator]
    public class ToStringGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            Compilation compilation = 
                GenerateSourceCodeFromResource(Attribute.EmebeddedResourceName, context, context.Compilation);

            IEnumerable<ClassDeclarationSyntax> targetClasses =
                receiver.TargetClasses.Where(q => HaveGeneratorAttribute(q, compilation));

            foreach (ClassDeclarationSyntax target in targetClasses)
            {
                var havePartialKeyword = target.Modifiers.Any(q => q.IsKind(SyntaxKind.PartialKeyword));
                if (havePartialKeyword)
                {
                    string sourceCode = GenerateToStringMethod(target, compilation);
                    context.AddSource("Generated_" + target.Identifier.Text, SourceText.From(sourceCode, Encoding.UTF8));
                }
                else
                {
                    var diagnostic = new DiagnosticDescriptor("MYSG1001",
                                                                                "Partial 키워드를 찾을 수 없습니다.",
                                                                                "ToString 메소드 생성을 위해 Class는 partial 키워드를 포함해야합니다.",
                                                                                "Syntax",
                                                                                DiagnosticSeverity.Error, true);
                    context.ReportDiagnostic(Diagnostic.Create(diagnostic, Location.None));
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        #region

        protected string GenerateToStringMethod(ClassDeclarationSyntax target, Compilation compilation)
        {
            CompilationUnitSyntax root = target.SyntaxTree.GetCompilationUnitRoot();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"using {Attribute.Namespace};");
            builder.AppendLine($"namespace {root.ChildNodes().OfType<NamespaceDeclarationSyntax>().First().Name}");
            builder.AppendLine("{");
                builder.AppendLine($"{target.Modifiers} class {target.Identifier.Text}");
                builder.AppendLine("{");
                builder.AppendLine("public override string ToString()");
                    builder.AppendLine("{");
                    builder.AppendLine("var builder = new System.Text.StringBuilder();");

                    foreach (PropertyDeclarationSyntax property in target.Members.OfType<PropertyDeclarationSyntax>())
                    {
                        string name = property.Identifier.Text;
                        builder.AppendLine($"builder.AppendLine(\"{name}:\" + {name}.ToString());");
                    }

                    builder.AppendLine("return builder.ToString();");
                    builder.AppendLine("}");
                builder.AppendLine("}");
            builder.AppendLine("}");

            return builder.ToString();
        }

        protected static Compilation GenerateSourceCodeFromResource(
            string resourceName, GeneratorExecutionContext context, Compilation compilation)
        {
            string sourceCode = EmbbededResourceReader.GetResource(resourceName);
            context.AddSource("Generated__" + resourceName, SourceText.From(sourceCode, Encoding.UTF8));

            var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var generatedCodeSyntax = CSharpSyntaxTree.ParseText(SourceText.From(sourceCode, Encoding.UTF8), options);
            compilation = compilation.AddSyntaxTrees(generatedCodeSyntax);

            return compilation;
        }

        protected static bool HaveGeneratorAttribute(ClassDeclarationSyntax targetClass, Compilation compilation)
        {
            var model = compilation.GetSemanticModel(targetClass.SyntaxTree);
            var haveAttribute = targetClass.AttributeLists.SelectMany(q => q.Attributes)
                                                                     .Select(q => model.GetTypeInfo(q))
                                                                     .Any(q => q.Type.ToDisplayString().Equals(Attribute.FullName));

            return haveAttribute;
        }
        #endregion

        #region ISyntaxReceiver
        /// <summary>
        /// Roslyn은 <see cref="ISyntaxReceiver"/>를 통해 해석된 SyntaxNode를 알림으로 받을 수 있도록 하는 기능을 제공합니다.
        /// 일반적으로 생성에 대상이 될 클래스들을 이곳에서 색출합니다.
        /// <see cref="ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode)"/>는 방문자 패턴으로 컴파일러에 의해 호출됩니다.
        /// </summary>
        private class SyntaxReceiver : ISyntaxReceiver
        {
            protected List<ClassDeclarationSyntax> _targetClasses = new List<ClassDeclarationSyntax>();

            public IEnumerable<ClassDeclarationSyntax> TargetClasses => this._targetClasses;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classNode && classNode.AttributeLists.Any())
                {
                    this._targetClasses.Add(classNode);
                }
            }
        }
        #endregion
    }
}
