using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("episodeList")]
public class Episode
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("subjectId")]
    public int SubjectId { get; set; }

    [Column("episode")]
    public decimal EpisodeNum { get; set; }
}
