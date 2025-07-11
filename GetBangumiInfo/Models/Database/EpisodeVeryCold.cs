using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("episodeListVeryCold")]
public class EpisodeVeryCold
{
    [Column("subjectId")]
    public int SubjectId { get; set; }

    [Column("episode")]
    public float EpisodeNum { get; set; }

    [Column("addInDate")]
    public DateTimeOffset AddInDate { get; set; }

    [Column("Id")]
    public int Id { get; set; }
}
