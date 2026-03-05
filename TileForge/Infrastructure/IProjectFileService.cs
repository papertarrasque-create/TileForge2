using System.Collections.Generic;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;

namespace TileForge.Infrastructure;

/// <summary>
/// Abstraction over project file I/O (Load/Save).
/// Pure-logic helpers (RestoreGroups, RestoreMap) remain on ProjectFile as static methods.
/// </summary>
public interface IProjectFileService
{
    ProjectFile.ProjectData Load(string projectPath);

    void Save(string projectPath, string sheetPath, ISpriteSheet sheet,
              List<TileGroup> groups, MapData map,
              ProjectFile.EditorStateData editorState);

    void Save(string projectPath, string sheetPath, ISpriteSheet sheet,
              List<TileGroup> groups, List<MapDocumentState> mapDocuments,
              ProjectFile.EditorStateData editorState,
              WorldLayout worldLayout = null);
}
