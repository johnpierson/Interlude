using System.Runtime.CompilerServices;
using System.Windows;

// The renderer's internals — the form window, the composite controls — are exercised by the STA
// smoke tests. Testing them through reflection or through a public surface nobody else should
// use would be worse than this one line.
[assembly: InternalsVisibleTo("Interlude.Tests")]
[assembly: InternalsVisibleTo("Interlude.Preview")]

// Themes/*.xaml is merged into each form window's own resource dictionary at run time.
// This attribute is what lets those dictionaries be found by pack URI inside this assembly.
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
