using System.Runtime.CompilerServices;
using System.Windows;

// Dynamo imports EVERY public type in a zero-touch assembly, and it imports their base types and
// signature types too. A public class deriving from InvalidOperationException drags Exception,
// SystemException and InvalidOperationException into the library; a public method returning a
// FrameworkElement drags in System.Windows. The result is a "System" category sitting next to
// "Interlude", full of types nobody asked for and nobody can use.
//
// [IsVisibleInDynamoLibrary(false)] cannot help: it hides OUR type, not the framework type behind
// it. So the rendering layer, the exceptions and the live-state types are internal, and the
// public surface is limited to types that are either Interlude's own or ones Dynamo already maps
// natively. LibrarySurfaceTests enforces it.
//
// The XAML compiler emits a public XamlGeneratedNamespace.GeneratedInternalTypeHelper into any
// assembly with InternalsVisibleTo; the csproj drops it from compilation for the same reason.
[assembly: InternalsVisibleTo("Interlude.Tests")]
[assembly: InternalsVisibleTo("Interlude.Preview")]

// Themes/*.xaml is merged into each form window's own resource dictionary at run time.
// This attribute is what lets those dictionaries be found by pack URI inside this assembly.
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
