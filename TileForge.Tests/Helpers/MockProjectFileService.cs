using System.Collections.Generic;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Infrastructure;

namespace TileForge.Tests.Helpers;

/// <summary>
/// In-memory mock for IProjectFileService. Tracks Load/Save calls.
/// </summary>
public class MockProjectFileService : IProjectFileService
{
    public ProjectFile.ProjectData LoadResult { get; set; }
    public string LastLoadPath { get; set; }
    public string LastSavePath { get; set; }
    public int SaveCallCount { get; set; }

    public ProjectFile.ProjectData Load(string projectPath)
    {
        LastLoadPath = projectPath;
        if (LoadResult != null)
            return LoadResult;
        throw new System.IO.FileNotFoundException("Mock: no LoadResult configured", projectPath);
    }

    public void Save(string projectPath, string sheetPath, ISpriteSheet sheet,
                     List<TileGroup> groups, MapData map,
                     ProjectFile.EditorStateData editorState)
    {
        LastSavePath = projectPath;
        SaveCallCount++;
    }

    public void Save(string projectPath, string sheetPath, ISpriteSheet sheet,
                     List<TileGroup> groups, List<MapDocumentState> mapDocuments,
                     ProjectFile.EditorStateData editorState,
                     WorldLayout worldLayout = null)
    {
        LastSavePath = projectPath;
        SaveCallCount++;
    }
}
