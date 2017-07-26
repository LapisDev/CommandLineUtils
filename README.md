# CommandLineUtils
`Lapis.CommandLineUtils` is a .NET Core library for creating neat command line application. It denpends on package `Microsoft.Extensions.CommandlineUtils` and `Microsoft.Extensions.DependencyInjection`.

## Examples
An example command line app using `CommandLineUtils` is available (under [CommandLineSampleApp](/CommandLineSampleApp)). Below is a quickstart to explore the featuresn of `CommandLineUtils`.

### Create an Application
We create our application by creating a `CommandLineApplication`.
```csharp
var app = new CommandLineApplication();
app.Name = "sample";
app.Description = "Sample app.";
app.HelpOption("-?|-h|--help");

app.Execute(args);
```
So far the app doesn't actually do anything, because we haven't create any command.

### Create a Command from a Class
A type for creating a command must meet the following requirements:
* It is a class.
* It is not abstract. (Except static classes, because they are regarded both abstract and sealed)
* It is public. (Nested types are not regarded "public".)
* It doesn't contain generic parameters.
* A `NonCommandAttribute` is defined on it.

Here we define a class `MathCommands`:
```csharp
[Command("math", Description = "Math operation.")]
public class MathCommands
{
}
```

A `CommandAttribute` is defined on it, setting the name and description of the command to create. 

Next, we register the command using the `Command(CommandLineSampleApp, Type)` extension method:
```csharp
app.Command(typeof(MathCommands));
```

### Create a Command from a Method

To make the `math` command actually do something, we add methods to the `MathCommands` class as subcommands.

A method for creating a command must meet the following requirements:
* It is not of the special names (e.g. operators, property getters and setters, etc.).
* It is not an override method of `System.Object` (e.g. `Object.Equals`, `Object.GetHashCode`, etc.).
* It is not a method defined by `System.IDisposable` interface (`IDisposable.Dispose`).
* It is not abstract.
* It is not a constructor.
* It doesn't contain generic parameters.
* It is public.
* A `NonCommandAttribute` is defined on it.

We create a method `Add` that returns the sum of two integers:
```csharp
[Command("add", Description = "Add two integers.")]
public int Add([Argument("a", "An integer.")] int a, 
    [Argument("b", "Another integer.")] int b)
{
    return a + b;
}
```

The `ArgumentAttribute`s are used to bind the parameters to command line arguments. 

Now we can call this method by using the following command:
```powershell
dotnet run -- math add 1 2
# Output: 3
```

An `OptionAttribute` can be used to bind the parameter to a command line option. 
```csharp
[Command]
public double Log([Argument] double value, 
    [Option("-b|--base <base>", "Base, default E.", CommandOptionType.SingleValue)] double b = Math.E)
{
    return Math.Log(value, b);
}
```
```powershell
dotnet run -- math log 10 
# Output: 2.30258509299405
dotnet run -- math log 10 -b 10
# Output: 1
```

### Use a Converter for Arguments or Options
For methods with parameters of complex types, converters can be used for conversion from `System.String` to the parameter type.

A converter must implement the `IConverter` interface. Here we define a converter for type `System.Text.RegularExpressions.Regex`:
```csharp
public class StringRegexConverter : IConverter
{
    public bool CanConvert(Type sourceType, Type targetType)
    {
        return targetType == typeof(Regex) && sourceType == typeof(string);
    }

    public object Convert(object value, Type targetType)
    {
        var s = value as string;
        if (s == null)
            throw new InvalidCastException();
        return new Regex(s);
    }
}
```

The `ConverterAttribute` can be used to specify a converter for an argument, option or command.
```csharp
[Command]
public class TextCommands
{
    [Command]
    [Converter(typeof(StringRegexConverter))]
    public string Match(Regex pattern, string s)
    {
        return pattern.Match(s).Value;
    }
}
```
```powershell
dotnet run -- text match "(?:#|0x)?(?:[0-9A-F]{2}){3,4}" `
    "Tomato (#FF6347). RGB value is (255,99,71). " 
# Output: #FF6347
```

The pre-defined converters are listed bellow:
* `SystemConvertConverter` uses `System.Convert.ChangeType`.
* `TypeConverterConverter` uses `System.ComponentModel.TypeConverter`.
* `MethodConverter` uses operators (`op_Implicit`, `op_Explicit`), `FromXX` and `ToXX` methods.
* `ConstrctorConverter` uses the public constructor of the target type with one parameter of the source type.
* `EnumNameConverter` uses `System.Enum.Parse`.

### Use a ResultHandler for the Return Value
Result handlers can be used for handling the return value of the Method, and returning a result code that the command line app should return when exited.

A converter must implement the `IResultHandler` interface. Here we define a result handler for writing a file:
```csharp
public class FileWritingResultHandler : IResultHandler
{
    public int Handle(object value)
    {
        using (var writer = File.CreateText(System.IO.Path.GetRandomFileName()))
            writer.WriteLine(value);
        return 0;
    }
}
```

The `ResultHandlerAttribute` can be used to specify a result handler for a command.
```csharp
[Command]
[Converter(typeof(PathFileConverter))]
public class FileCommands
{
    [return: ResultHandler(typeof(FileWritingResultHandler))]
    public string Base64(FileInfo file)
    {
        using (var reader = file.OpenRead())
        {
            var bytes = new byte[file.Length];
            reader.Read(bytes, 0, bytes.Length);
            return Convert.ToBase64String(bytes);
        }
    }
```
```powershell
dotnet run -- file base-64 "test_data/text.txt"
```
A file containing the base64 string will be generated.

The pre-defined result handlers are listed bellow:
* `ConsoleOutResultHandler` prints the result to the standard output using `System.Console.WriteLine`.