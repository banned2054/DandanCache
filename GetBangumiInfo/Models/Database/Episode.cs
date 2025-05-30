using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("episodeList")]
public class Episode
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("subjectId")]
    public long SubjectId { get; set; }

    [Column("episode")]
    public float? EpisodeNum { get; set; }
}