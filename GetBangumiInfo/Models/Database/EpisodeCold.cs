using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("episodeListCold")]
public class EpisodeCold
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("subjectId")]
    public long SubjectId { get; set; }

    [Column("episode")]
    public float? EpisodeNum { get; set; }
}
