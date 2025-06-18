using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("episodeListCold")]
public class EpisodeCold
{
    [Column("subjectId")]
    public int SubjectId { get; set; }

    [Column("episode")]
    public float? EpisodeNum { get; set; }

    [Column("isBilibili")]
    public bool IsBilibili { get; set; }
}
