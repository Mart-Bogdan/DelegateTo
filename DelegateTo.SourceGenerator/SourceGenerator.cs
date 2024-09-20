namespace DelegateTo.SourceGenerator;

[Generator]
public class SourceGenerator : ISourceGenerator
{
    const string InlineAttr = "[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
 
    public void Execute(GeneratorExecutionContext context)
    {
        // retrieve the populated receiver
        if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
            return;
        var compilation = context.Compilation;

        var fieldsByClass = receiver
            .Fields
            .GroupBy(f => f.symbol.ContainingType.ToDisplayString())
            .ToDictionary(f => f.Key);

        foreach (var (container, properties) in fieldsByClass)
        {
            var source = CreateDelegates(compilation, container, properties);
            context.AddSource(container, source);
        }
    }

    private string CreateDelegates(Compilation compilation, string _, IGrouping<string, (ISymbol symbol, bool inline)> properties)
    {
        var interfaces = "";
        var container = properties.First().symbol.ContainingType;
        var delegates = properties.Select(p => CreateDelegate(compilation, p));
        var access = container.DeclaredAccessibility >= Accessibility.Internal ? container.DeclaredAccessibility.ToString().ToLower() : "";
        var typeKind =  container.TypeKind == TypeKind.Struct ? "struct" : "class";
        var result = @$"
namespace {container.ContainingNamespace.ToDisplayString()}
{{
    {access} partial {typeKind} {container.Name} {interfaces}
    {{
{string.Join("\n", delegates)}
    }}
}}
";
        return result;
    }

    private string CreateDelegate(Compilation compilation, (ISymbol symbol, bool inline) item)
    {
        ISymbol property = item.symbol;
        if (!(property is IFieldSymbol || property is IPropertySymbol))
            throw new Exception("Invalid property type, must be field or property");

        var type = property is IPropertySymbol prop
            ? prop.Type
            : ((IFieldSymbol)property).Type;
        
        
        var publicMembers = type.GetMembers()
            .Where(m => m.CanBeReferencedByName && m.DeclaredAccessibility > Accessibility.Internal);

        var methods = publicMembers.Where(m => m is IMethodSymbol).Cast<IMethodSymbol>();
        var properties = publicMembers.Where(m => m is IPropertySymbol).Cast<IPropertySymbol>();

        var methodExpressions = methods.Select(m =>
        {
            var parameters = m.Parameters.Join(", ");
            var parameterNames = m.Parameters.Select(p => p.Name).Join(", ");
            var methodName = m.Name;
            var returnType = m.ReturnType.ToDisplayString();
            return (item.inline ? $"        {InlineAttr}\n" : "") +
                   $"        public {returnType} {methodName}({parameters}) => {property.Name}.{methodName}({parameterNames});";
        });

        var propertyExpressions = properties.Select(m =>
        {
            var methodName = m.Name;
            var returnType = m.Type.ToDisplayString();
            var sb = new StringBuilder(1024);
            sb.AppendLine($"        public {returnType} {methodName}\n" +
                          $"        {{");
            if (m.GetMethod is not null)
            {
                if (item.inline)
                    sb.Append("            ").AppendLine(InlineAttr);
                sb.AppendLine($"            get => {property.Name}.{methodName};");
            }
            if (m.SetMethod is not null)
            {
                if (item.inline)
                    sb.Append("            ").AppendLine(InlineAttr);
                sb.AppendLine($"            set => {property.Name}.{methodName} = value;");
            }
            sb.Append("        }");
            return sb.ToString();
        });

        return methodExpressions
            .Concat(propertyExpressions)
            .Join("\n\n");
    }

    public void Initialize(GeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            // Debugger.Launch();
        }
#endif
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }
}

/// <summary>
/// Created on demand before each generation pass
/// </summary>
class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<(ISymbol symbol, bool inline)> Fields { get; } = new();

    /// <summary>
    /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
    /// </summary>
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        // any field with at least one attribute is a candidate for property generation
        if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax
            && propertyDeclarationSyntax.AttributeLists.Count > 0)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
            var attribute = symbol!.GetAttributes().FirstOrDefault(ad => ad.AttributeClass!.ToDisplayString().Contains("GenerateDelegate"));
            if (attribute != null)
            {
                Fields.Add((symbol, attribute.NamedArguments.Length == 1 && attribute.NamedArguments[0].Key == "Inline" && (attribute.NamedArguments[0].Value.Value?.Equals((object)true)??false)));
            }
        }
        if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
            && fieldDeclarationSyntax.AttributeLists.Count > 0
            && fieldDeclarationSyntax.Declaration.Variables.Count == 1)
        {
            ISymbol? symbol = context.SemanticModel.GetDeclaredSymbol(fieldDeclarationSyntax.Declaration.Variables[0]);
            if (symbol == null)
                return;
            var attribute = symbol!.GetAttributes().FirstOrDefault(ad => ad.AttributeClass!.ToDisplayString().Contains("GenerateDelegate"));
            if (attribute != null)
            {
                Fields.Add((symbol, attribute.NamedArguments.Length == 1 && attribute.NamedArguments[0].Key == "Inline" && (attribute.NamedArguments[0].Value.Value?.Equals((object)true)??false)));
            }
        }
    }
}
