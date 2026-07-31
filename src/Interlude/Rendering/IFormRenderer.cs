using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Runtime;

namespace Interlude.Rendering;

/// <summary>
/// Shows a form and returns what the user answered.
///
/// This interface exists to keep the seam honest rather than because a second renderer ships
/// today. Everything above it — the model, the condition engine, the session — is already free
/// of WPF, so a headless renderer that answers forms from a supplied dictionary, or an Avalonia
/// one for a non-Windows host, is a new implementation of this one method and nothing else.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public interface IFormRenderer
{
    /// <summary>
    /// Shows the form and blocks until the user finishes with it. Never returns null: a
    /// cancelled form comes back with every field's default and <c>WasSubmitted</c> false.
    /// </summary>
    FormResult ShowModal(FormDefinition definition, FormSession session);
}
