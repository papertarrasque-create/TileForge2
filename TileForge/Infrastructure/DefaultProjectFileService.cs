using System.Collections.Generic;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;

namespace TileForge.Infrastructure;

/// <summary>
/// Default IProjectFileService that delegates to the existing static ProjectFile methods.
/// </summary>
public class DefaultProjectFileService : IProjectFileService
{
    public ProjectFile.ProjectData Load(string projectPath)
        => ProjectFile.Load(projectPath);

    public void Save(string projectPath, string sheetPath, ISpriteSheet sheet,
                     List<TileGroup> groups, MapData map,
                     ProjectFile.EditorStateData editorState)
        => ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

    public void Save(string projectPath, string sheetPath, ISpriteSheet sheet,
                     List<TileGroup> groups, List<MapDocumentState> mapDocuments,
                     ProjectFile.EditorStateData editorState,
                     WorldLayout worldLayout = null)
        => ProjectFile.Save(projectPath, sheetPath, sheet, groups, mapDocuments, editorState, worldLayout);
}
