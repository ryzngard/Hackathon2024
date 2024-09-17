# Hackathon 2024

This project is intended to be a hackathon project. As such, it is neither a good representation of quality or maintainable code. Instead it was made to onboard an idea as quickly as possible to learn and prove viability.

# The Idea

As of now, it's possible to write analyzers for ASP.NET/Blazor, but only to analyze the generated C#. Analyzers like that already exist today reporting as BL#### from the asp.net repo. While that has been okay, it doesn't easily allow 3rd parties to make analyzers. Furthermore, it also does not allow enforcement of certain practices such as preference of using `@code` or a `.razor.cs` file, naming styling, or warning of invalid scenarios based on the markup structure rather than symbolic information. This project aims to solve that.

# Execution

As of writing, this project has unit tests that function and a few analyzers with very basic implementation and testing. It does prove that diagnostics are being created. The structure is this: 

1. Custom built `dotnet/razor` compiler binaries with syntax made public so that analyzers can easily use the syntax tree.
2. Custom port of the `RazorSourceGenerator` called `SourceGenerator` that hooks analyzers into generation and provides a `RazorCodeDocument` for usage. 
3. All analyzers report diagnostics through the code generator.

The unit tests are the best place to start since they are working and allow debugging through the code. The source generator itself probably isn't doing the right thing, but it works for the purposes here. Note that the asp.net tag helpers are not provided in the unit tests so some analyzers have been broadened to handle markup that would have been a valid tag helper but is instead treated as html.