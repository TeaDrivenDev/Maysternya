using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace TeaDriven.Maysternya.SiiUnit.Parsing;

internal sealed class Parser
{
    /// <summary>
    ///     Flags for loading MemberInfo's
    /// </summary>
    private const BindingFlags Flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly ReadOnlyCollection<Type> IntegralTypes;
    private static readonly ReadOnlyCollection<Type> FloatTypes;
    private static readonly ReadOnlyCollection<Type> NumericTypes;
    private static readonly ReadOnlyCollection<Type> VectorTypes;
    private readonly Dictionary<string, object> classMap = new Dictionary<string, object>();
    private readonly Token endOfInput;
    private readonly int length;
    private readonly Lexer lexer;

    private readonly ReadOnlyCollection<Token> tokens;
    private int index;

    static Parser()
    {
        IntegralTypes =
        [
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
        ];

        FloatTypes = [typeof(float), typeof(double), typeof(decimal)];

        NumericTypes = IntegralTypes.Concat(FloatTypes).ToList().AsReadOnly();

        VectorTypes = [typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion)];
    }

    public Parser(Lexer lexer)
    {
        this.lexer = lexer;
        this.tokens = lexer.Tokenize();
        this.endOfInput = this.tokens.Last();
        this.length = this.tokens.Count;
    }

    /// <summary>
    ///     Parses the SiiDocument using the specified C# types
    /// </summary>
    /// <param name="types">An array of custom object types, which can be parsed into from this SiiDocument</param>
    /// <returns></returns>
    public ReadOnlyDictionary<string, object> Parse(IEnumerable<Type> types)
    {
        var map = new Dictionary<string, object>();
        var first = default(Token);

        // Take the initial SiiNunit line
        if (!this.MatchAndTake(TokenKind.Identifier, out first) || first.Text != "SiiNunit")
        {
            throw new SiiSyntaxException(this.Peek(), "SII unit files must begin with 'SiiNunit'");
        }

        // Take the Brace on line 2
        this.Take(TokenKind.LeftBrace);

        // Compile our Class List, mapping name => type
        var classes =
            types
                .Where(t => t.GetCustomAttribute<SiiUnitAttribute>() is not null)
                .ToDictionary(t => t.GetCustomAttribute<SiiUnitAttribute>().ClassName, t => t);
        var classDict = new ReadOnlyDictionary<string, Type>(classes);

        // Grab all the structs in the document, and create their
        // initial instance value, so they can be used as attribute values,
        // no matter where in the file they are defined
        foreach (var item in classDict)
        {
            var pattern = @"^[\s\t]*" + item.Key + @"[\s\t]*:[\s\t]*(?<name>[\.a-z0-9_]+)[\s\t]*$";
            var reg = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var matches = reg.Matches(this.lexer.Source);

            // Add each struct to the ClassMap Dictionary
            foreach (Match m in matches)
            {
                if (!this.classMap.ContainsKey(m.Groups["name"].Value))
                {
                    var instance = Activator.CreateInstance(item.Value);
                    this.classMap.Add(m.Groups["name"].Value, instance);
                }
            }
        }

        // Parse all the objects in the Sii file
        foreach (var cl in classDict.Keys)
        {
            // If we hit a right brace here, we are at the EOF
            while (!this.Match(TokenKind.RightBrace))
            {
                var pair = this.ParseDefinition(classDict);
                if (pair is not null)
                {
                    map.Add(pair.Value.Key, pair.Value.Value);
                }
            }
        }

        // Take the last brace, and the EOF token
        this.Take(TokenKind.RightBrace);
        this.Take(TokenKind.EndOfInput);

        return new ReadOnlyDictionary<string, object>(map);
    }

    /// <summary>
    ///     Parses an entire object and its properties
    /// </summary>
    /// <param name="classes">className => C# InstanceType</param>
    /// <returns></returns>
    private KeyValuePair<string, object>? ParseDefinition(ReadOnlyDictionary<string, Type> classes)
    {
        // Check for directives
        if (this.MatchAndTake(TokenKind.Directive))
        {
            return null;
        }

        // Grab the classname
        var className = this.Take(TokenKind.Identifier).Text;
        this.Take(TokenKind.Colon);

        // is this an anonymous classs?
        var nameless = this.Match(TokenKind.Dot);

        // Begin fetching the class name
        var builder = new StringBuilder();
        if (nameless)
        {
            builder.Append(this.Take().Text);
        }

        // Parse the object name, taking all sections seperated by dots
        builder.Append(this.Take(new[] { TokenKind.Identifier, TokenKind.Number }).Text);
        var dot = default(Token);
        while (this.MatchAndTake(TokenKind.Dot, out dot))
        {
            builder.Append(dot.Text);
            builder.Append(this.Take(new[] { TokenKind.Identifier, TokenKind.Number }).Text);
        }

        // Fetch the C# class type, so we can create an object instance
        var name = builder.ToString();
        var type = default(Type);
        if (!classes.TryGetValue(className, out type))
        {
            throw new SiiException($"No type for {name} (class {className})");
        }

        // Start fetching the class properties
        var instance = this.classMap[name];
        var members =
            type.GetFields(Flags)
                .Where(f => f.GetCustomAttribute<SiiAttributeAttribute>() is not null)
                .Where(f => !f.IsSpecialName)
                .Cast<MemberInfo>()
                .Concat(
                    type.GetProperties(Flags)
                        .Where(p => p.GetCustomAttribute<SiiAttributeAttribute>() is not null)
                        .Where(p => !p.IsSpecialName))
                .ToArray();

        // Array of [C# ArrayMember => ListOfArrayValues]
        var arrays = new Dictionary<MemberInfo, List<object>>();
        var memberType = default(Type);
        this.Take(TokenKind.LeftBrace);

        // Parse through until we find the Right Brace
        while (!this.MatchAndTake(TokenKind.RightBrace))
        {
            // Check for directives
            if (this.MatchAndTake(TokenKind.Directive))
            {
                continue;
            }

            // Grab the attribute name
            var attribute = this.Take(TokenKind.Identifier).Text;
            // Check for array
            var isArray = this.MatchAndTake(TokenKind.LeftSquare);

            var member = default(MemberInfo);
            var value = default(object);

            if (isArray)
            {
                // Attempt to grab the array index. We cant actually use it (as of yet) since
                // we are using a list (since most array's don't define size)
                var arrayIndex = default(Token);
                this.MatchAndTake(TokenKind.Number, out arrayIndex);

                // Grab the right square and colon
                this.Take(TokenKind.RightSquare);
                this.Take(TokenKind.Colon);

                // First we find the proper C# property for this array value
                // We search the cache'd array members first
                var list = new List<object>();
                foreach (var pair in arrays)
                {
                    if (pair.Key.GetCustomAttribute<SiiAttributeAttribute>()?.Name == attribute)
                    {
                        member = pair.Key;
                        list = pair.Value;
                        break;
                    }
                }

                // If we didnt find the property, this is our first access
                if (member is null)
                {
                    // Search all members
                    foreach (var m in members)
                    {
                        if (m.GetCustomAttribute<SiiAttributeAttribute>()?.Name == attribute)
                        {
                            member = m;
                            break;
                        }
                    }
                }

                // If there is no member to this attribute, throw an exception
                if (member is null)
                {
                    throw new SiiException(
                        $"No property for {attribute} found in (type {type.Name}) for (class {className})");
                }

                // Ensure that our C# member is an array type
                memberType = this.GetDeclaredType(member);
                if (!memberType.IsArray)
                {
                    throw new SiiException($"{member.Name} is not an array");
                }

                // Grab the value of this attribute from the Sii object
                value = this.ParseValue(memberType.GetElementType());
                list.Add(value);

                // Add this member to the arrayMembers cache
                arrays.TryAdd(member, list);

                continue;
            }

            // Non-array.. Grab the colon and move on
            this.Take(TokenKind.Colon);
            foreach (var m in members)
            {
                if (m.GetCustomAttribute<SiiAttributeAttribute>()?.Name == attribute)
                {
                    member = m;
                    break;
                }
            }

            // If we forgot to assign a member to this attribute, throw it up
            if (member is null)
            {
                throw new SiiException(
                    $"No property for {attribute} found in (type {type.Name}) for (class {className})");
            }

            // Grab the C# attribute (Field or Property) type, and parse the value
            memberType = this.GetDeclaredType(member);
            value = this.ParseValue(memberType);

            // Apply the parsed value from the Sii Object into the C# member
            if (member is PropertyInfo property)
            {
                var setter = property.GetSetMethod(true);
                setter?.Invoke(instance, [value]);
            }
            else
            {
                var field = member as FieldInfo;
                field.SetValue(instance, value);
            }
        }

        // Now that we have parsed this Object completly, Its time to
        // fill the array values on the C# members
        foreach (var pair in arrays)
        {
            memberType = this.GetDeclaredType(pair.Key);
            var length = pair.Value.Count;
            var elementType = memberType.GetElementType();

            // Create the Array
            var array = Array.CreateInstance(elementType, length);

            // Set each array index value
            for (var i = 0; i < length; ++i) array.SetValue(pair.Value[i], i);

            // Set the C# member value to the newly filled array
            if (pair.Key is PropertyInfo property)
            {
                var setter = property.GetSetMethod(true);
                setter?.Invoke(instance, new[] { array });
            }
            else
            {
                var field = pair.Key as FieldInfo;
                field.SetValue(instance, array);
            }
        }

        // Do not return nameless objects
        return nameless ? null : new KeyValuePair<string, object>(name, instance);
    }

    /// <summary>
    ///     Parses the next token based on the C# member type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private object ParseValue(Type type)
    {
        var token = default(Token);

        // If we can take a left parentesis, then we have a Vector
        if (this.MatchAndTake(TokenKind.LeftParen, out token))
        {
            if (!VectorTypes.Contains(type))
            {
                throw new SiiException($"Type mismatch. Expected {type.Name} but found vector");
            }

            var fieldType =
                type.GetField("X", BindingFlags.Public | BindingFlags.Instance).FieldType;

            var values = new List<object> { this.ParseScalarValue(fieldType) };

            // Grab initial seperator
            var kind = this.Take([TokenKind.Comma, TokenKind.SemiColon]).Kind;
            if (kind is TokenKind.SemiColon && type != typeof(Quaternion))
            {
                throw new SiiException($"Type mismatch. Expected Quaternion but found {type.Name}");
            }

            // Grab all the remaining values
            do
            {
                values.Add(this.ParseScalarValue(fieldType));
            }
            while (this.MatchAndTake(TokenKind.Comma));

            // End
            this.Take(TokenKind.RightParen);

            // Make sure the sizes of the Vectors match
            var size = Marshal.SizeOf(type) / Marshal.SizeOf(fieldType);
            if (values.Count != size)
            {
                throw new SiiException(
                    $"Too {(values.Count > size ? "many" : "few")} values for {type.Name}, expected {size}, got {values.Count}");
            }

            return Activator.CreateInstance(type, values.ToArray());
        }

        return this.ParseScalarValue(type);
    }

    /// <summary>
    ///     Returns the C# Member type
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private Type GetDeclaredType(MemberInfo member)
    {
        return member is PropertyInfo info ? info.PropertyType : (member as FieldInfo).FieldType;
    }

    /// <summary>
    ///     Parses and converts a token value from the Sii attribute into a C# data type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private object ParseScalarValue(Type type)
    {
        var token = this.Take();
        switch (token.Kind)
        {
            case TokenKind.String:
                if (type != typeof(string))
                {
                    throw new SiiException(
                        $"Type mistmatch. Expected string, got {type.Name} on line {token.Span.Start.Line}");
                }

                return token.Text;

            case TokenKind.Number:
                var format = (NumberFormat)token.Tag;

                // Check for arrays
                if (type.IsArray && format is NumberFormat.Integer)
                {
                    return null;
                }

                // Ensure supported numeric type
                if (!NumericTypes.Contains(type))
                {
                    throw new SiiException(
                        $"Type mismatch. Expected numeric type, got {type.Name} on line {token.Span.Start.Line}");
                }

                // Parse Hex Floats using the SiiConverter class
                if (format is NumberFormat.HexFloat)
                {
                    return SiiConverter.FromHexString(token.Text);
                }

                // Check for type mismatch
                if (format is NumberFormat.Float && !FloatTypes.Contains(type))
                {
                    throw new SiiException(
                        $"Type mismatch. Expected {type.Name}, got float on line {token.Span.Start.Line}");
                }

                // Grab the Parse method from the numeric type
                var style =
                    format is NumberFormat.Float ? NumberStyles.Float : NumberStyles.Integer;
                var parser =
                    type.GetMethod(
                        "Parse",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        [typeof(string), typeof(NumberStyles), typeof(IFormatProvider)],
                        null);
                return parser.Invoke(null, [token.Text, style, CultureInfo.InvariantCulture]);

            case TokenKind.True:
            case TokenKind.False:
                if (type != typeof(bool))
                {
                    throw new SiiException(
                        $"Type mismatch. Expected bool, got {type.Name} on line {token.Span.Start.Line}");
                }

                return token.Kind is TokenKind.True;

            case TokenKind.Dot:
                // Grab the dot
                var builder = new StringBuilder(token.Text);

                // Parse the object name, taking all sections seperated by dots
                builder.Append(this.Take([TokenKind.Identifier, TokenKind.Number]).Text);
                var dot = default(Token);
                while (this.MatchAndTake(TokenKind.Dot, out dot))
                {
                    builder.Append(dot.Text);
                    builder.Append(this.Take([TokenKind.Identifier, TokenKind.Number]).Text);
                }

                // Fetch the C# class type, so we can return an object instance
                var name = builder.ToString();
                if (!this.classMap.TryGetValue(name, out var value))
                {
                    throw new SiiException(
                        $"Access to an undefined object \"{name}\" found on line {token.Span.Start.Line}");
                }
                else
                {
                    return value;
                }

            default:
                throw new SiiException(
                    $"Unsupported value type {token.Kind.ToString().ToLowerInvariant()} on line {token.Span.Start.Line}");
        }
    }

    /// <summary>
    ///     Returns the <see cref="Token" /> at the secified index, offset by
    ///     the current index
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    private Token Peek(int distance = 0)
    {
        var newIndex = this.index + distance;
        if (newIndex < 0 || newIndex >= this.length)
        {
            return this.endOfInput;
        }

        return this.tokens[newIndex];
    }

    /// <summary>
    ///     Takes the next token
    /// </summary>
    /// <param name="kind">If the new token does not match the specified token, an parser error occurs</param>
    /// <returns></returns>
    private Token Take(TokenKind? kind = null)
    {
        if (kind is null)
        {
            return this.tokens[this.index++];
        }

        var current = this.Peek();
        if (current.Kind != kind.Value)
        {
            throw new SiiSyntaxException(current, $"Unexpected {current}");
        }

        ++this.index;
        return current;
    }

    /// <summary>
    ///     Takes the next token
    /// </summary>
    /// <param name="kinds">
    ///     Specifies the expected following Token. If these token do
    ///     not match the specified token, an parser error occurs
    /// </param>
    /// <returns></returns>
    private Token Take(TokenKind[] kinds)
    {
        var current = this.Peek();
        foreach (var token in kinds)
        {
            if (token == current.Kind)
            {
                ++this.index;
                return current;
            }
        }

        throw new SiiSyntaxException(current, $"Unexpected {current}");
    }

    /// <summary>
    ///     Returns if the Next token matches the specified token type
    /// </summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    private bool Match(TokenKind kind)
    {
        return this.Peek().Kind == kind;
    }

    /// <summary>
    ///     Takes the next token if the specified tokenkind matches.
    /// </summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    private bool MatchAndTake(TokenKind kind)
    {
        var dummy = default(Token);
        return this.MatchAndTake(kind, out dummy);
    }

    /// <summary>
    ///     Takes the next token if the specified tokenkind matches.
    /// </summary>
    /// <param name="kind"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private bool MatchAndTake(TokenKind kind, out Token token)
    {
        if (this.Match(kind))
        {
            token = this.Take();
            return true;
        }

        token = null;
        return false;
    }
}
