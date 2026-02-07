using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Maysternya.SiiUnit.Parsing;

namespace Maysternya.SiiUnit;

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

    public ReadOnlyDictionary<string, object> Load(string source)
    {
        return this.LoadImpl(source, null);
    }

    public ReadOnlyDictionary<string, object> LoadFile(string path, Encoding encoding = null)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        var source = File.ReadAllText(path, encoding ?? Encoding.UTF8);
        return this.Load(source);
    }

    private ReadOnlyDictionary<string, object> LoadImpl(string source, string fileName)
    {
        var lexer = new Lexer(source, fileName);
        var parser = new Parser(lexer);

        return this.Definitions = parser.Parse(this.documentTypes);
    }

    public T GetDefinition<T>(string name)
    {
        return (T)this.Definitions[name];
    }
}
