using System.Windows;

// Deliberately NO InternalsVisibleTo.
//
// The XAML compiler emits a *public* XamlGeneratedNamespace.GeneratedInternalTypeHelper into any
// assembly that has one, and Dynamo turns every public type into a library category — so a single
// test convenience would put a meaningless "XamlGeneratedNamespace" entry in front of every user.
// FormWindow is public instead, which the renderer extensibility story wanted anyway.

// Themes/*.xaml is merged into each form window's own resource dictionary at run time.
// This attribute is what lets those dictionaries be found by pack URI inside this assembly.
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
