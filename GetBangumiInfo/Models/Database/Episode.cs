using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("episodeList")]
public class Episode
{
    [Column("subjectId")]
    public int SubjectId { get; set; }

    [Column("episode")]
    public float EpisodeNum { get; set; }

    [Column("isBilibili")]
    public bool IsBilibili { get; set; }
}
