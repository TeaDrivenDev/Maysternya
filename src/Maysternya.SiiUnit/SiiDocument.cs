using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

using TeaDriven.Maysternya.SiiUnit.Parsing;

namespace TeaDriven.Maysternya.SiiUnit;

public sealed class SiiDocument
{
    private readonly ReadOnlyCollection<Type> documentTypes;

    public SiiDocument(params Type[] classTypes) : this(classTypes as IEnumerable<Type>)
    {
    }

    public SiiDocument(IEnumerable<Type> classTypes)
    {
        this.documentTypes = classTypes.ToList().AsReadOnly();
    }

    public ReadOnlyDictionary<string, object> Definitions { get; private set; }

    public ReadOnlyDictionary<string, object> Load(string source, SiiParsingOptions options)
    {
        return this.LoadImpl(source, fileName: null, options);
    }

    public ReadOnlyDictionary<string, object> LoadFile(
        string path,
        SiiParsingOptions options,
        Encoding encoding = null)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        var source = File.ReadAllText(path, encoding ?? Encoding.UTF8);
        return this.Load(source, options);
    }

    private ReadOnlyDictionary<string, object> LoadImpl(
        string source,
        string fileName,
        SiiParsingOptions options)
    {
        var lexer = new Lexer(source, fileName);
        var parser = new Parser(lexer);

        return this.Definitions = parser.Parse(this.documentTypes, options);
    }

    public T GetDefinition<T>(string name)
    {
        return (T)this.Definitions[name];
    }
}
