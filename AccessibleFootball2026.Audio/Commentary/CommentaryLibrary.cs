using System.Text.Json;
namespace AccessibleFootball2026.Audio.Commentary;

/// <summary>All source recordings, including review holds and promos. Browsing is separate from automatic event selection.</summary>
public sealed class CommentaryLibrary
{
    public sealed class Recording
    {
        public string Slot {get;set;}="";
        public string File {get;set;}="";
        public string ReviewStatus {get;set;}="";
        public string Transcript {get;set;}="";
        public string[] Events {get;set;}=Array.Empty<string>();
        public string[] OriginalSlots {get;set;}=Array.Empty<string>();
        public string SourceMd5 {get;set;}="";
    }
    public sealed class Document {public int Version {get;set;} public Recording[] Recordings {get;set;}=Array.Empty<Recording>();}
    public IReadOnlyList<Recording> Recordings {get;}
    public string Root {get;}
    public string? Error {get;}
    public CommentaryLibrary(string root)
    {
        Root=Path.GetFullPath(root);
        try
        {
            var doc=JsonSerializer.Deserialize<Document>(System.IO.File.ReadAllText(Path.Combine(Root,"commentary_library.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
            if(doc?.Version!=1)throw new InvalidDataException("Invalid library version");
            if(doc.Recordings.Select(r=>r.Slot).Distinct().Count()!=doc.Recordings.Length)throw new InvalidDataException("Duplicate library slot");
            var slots=doc.Recordings.SelectMany(r=>r.OriginalSlots).ToArray();
            if(slots.Length!=4918 || slots.Distinct().Count()!=4918 || !Enumerable.Range(0,4918).Select(i=>i.ToString("D5")).ToHashSet().SetEquals(slots))throw new InvalidDataException("Incomplete original-slot accounting");
            if(doc.Recordings.Any(r=>Resolve(r.File)==null||r.OriginalSlots.Length==0))throw new InvalidDataException("Missing or unsafe library source");
            Recordings=Array.AsReadOnly(doc.Recordings);
        }
        catch(Exception ex){Recordings=Array.Empty<Recording>();Error=ex.Message;}
    }
    public string? Resolve(string file)
    {
        if(string.IsNullOrWhiteSpace(file)||Path.IsPathRooted(file)||file.Contains('\\')||file.Split('/').Any(x=>x is ".." or "." or ""))return null;
        var path=Path.GetFullPath(Path.Combine(Root,file));
        if(!path.StartsWith(Root+Path.DirectorySeparatorChar,StringComparison.Ordinal)||!System.IO.File.Exists(path))return null;
        if(!path.EndsWith(".mp3",StringComparison.OrdinalIgnoreCase)&&!path.EndsWith(".wav",StringComparison.OrdinalIgnoreCase))return null;
        return path;
    }
}
